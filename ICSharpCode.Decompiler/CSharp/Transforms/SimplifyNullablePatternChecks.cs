// Copyright (c) 2026 Harry Ricketts
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

#nullable enable

using System.Collections.Generic;
using System.Linq;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Simplifies redundant nullable pattern checks such as
	/// <c>v != null &amp;&amp; v is T? x &amp;&amp; x.HasValue</c> to <c>v is T x</c>,
	/// and removes redundant <c>.Value</c> accesses on the matched variable.
	/// </summary>
	public sealed class SimplifyNullablePatternChecks : DepthFirstAstVisitor, IAstTransform
	{
		public void Run(AstNode rootNode, TransformContext context)
		{
			rootNode.AcceptVisitor(this);
		}

		public override void VisitIfElseStatement(IfElseStatement ifElseStatement)
		{
			base.VisitIfElseStatement(ifElseStatement);
			if (TrySimplifyNullablePatternCondition(ifElseStatement.Condition, out var simplifiedCondition, out var patternVariableName))
			{
				ifElseStatement.Condition = simplifiedCondition;
				if (patternVariableName != null)
				{
					RemoveRedundantNullableValueAccess(ifElseStatement.TrueStatement, patternVariableName);
				}
			}
		}

		static bool TrySimplifyNullablePatternCondition(
			Expression condition,
			out Expression simplifiedCondition,
			out string? patternVariableName)
		{
			simplifiedCondition = condition;
			patternVariableName = null;

			var parts = FlattenConditionalAnd(condition);
			if (parts.Count < 2)
				return false;

			int hasValueIndex = -1;
			for (int i = 0; i < parts.Count; i++)
			{
				if (IsNullableHasValueCheck(parts[i], out var variableName))
				{
					hasValueIndex = i;
					patternVariableName = variableName;
					break;
				}
			}
			if (hasValueIndex < 0 || patternVariableName == null)
				return false;

			int patternIndex = -1;
			Expression? testedExpression = null;
			AstType? underlyingType = null;
			for (int i = 0; i < parts.Count; i++)
			{
				if (i == hasValueIndex)
					continue;
				if (TryGetNullableTypePattern(parts[i], patternVariableName, out var expr, out var type))
				{
					patternIndex = i;
					testedExpression = expr;
					underlyingType = type;
					break;
				}
			}
			if (patternIndex < 0 || testedExpression == null || underlyingType == null)
				return false;

			for (int i = 0; i < parts.Count; i++)
			{
				if (i == hasValueIndex || i == patternIndex)
					continue;
				if (!IsRedundantNullCheck(parts[i], testedExpression))
					return false;
			}

			var designation = new SingleVariableDesignation { Identifier = patternVariableName };
			simplifiedCondition = new BinaryOperatorExpression(
				testedExpression.Clone(),
				BinaryOperatorType.IsPattern,
				new DeclarationExpression {
					Type = underlyingType.Clone(),
					Designation = designation
				});
			return true;
		}

		static List<Expression> FlattenConditionalAnd(Expression expression)
		{
			var parts = new List<Expression>();
			FlattenConditionalAnd(expression, parts);
			return parts;
		}

		static void FlattenConditionalAnd(Expression expression, List<Expression> parts)
		{
			if (expression is BinaryOperatorExpression {
				Operator: BinaryOperatorType.ConditionalAnd,
				Left: Expression left,
				Right: Expression right
			})
			{
				FlattenConditionalAnd(left, parts);
				FlattenConditionalAnd(right, parts);
			}
			else
			{
				parts.Add(expression);
			}
		}

		static bool IsRedundantNullCheck(Expression expression, Expression testedExpression)
		{
			if (expression is BinaryOperatorExpression {
				Operator: BinaryOperatorType.InEquality or BinaryOperatorType.Equality,
				Left: Expression left,
				Right: Expression right
			})
			{
				if (right is NullReferenceExpression && ExpressionsMatch(left, testedExpression))
				{
					return expression is BinaryOperatorExpression { Operator: BinaryOperatorType.InEquality };
				}
				if (left is NullReferenceExpression && ExpressionsMatch(right, testedExpression))
				{
					return expression is BinaryOperatorExpression { Operator: BinaryOperatorType.InEquality };
				}
			}
			if (expression is UnaryOperatorExpression {
				Operator: UnaryOperatorType.Not,
				Expression: BinaryOperatorExpression {
					Operator: BinaryOperatorType.Equality,
					Left: Expression nullCheckLeft,
					Right: NullReferenceExpression
				}
			} && ExpressionsMatch(nullCheckLeft, testedExpression))
			{
				return true;
			}
			if (expression is BinaryOperatorExpression {
				Operator: BinaryOperatorType.IsPattern,
				Left: Expression patternLeft,
				Right: UnaryOperatorExpression { Operator: UnaryOperatorType.PatternNot, Expression: NullReferenceExpression }
			} && ExpressionsMatch(patternLeft, testedExpression))
			{
				return true;
			}
			return false;
		}

		static bool ExpressionsMatch(Expression a, Expression b)
		{
			return a.ToString() == b.ToString();
		}

		static bool IsNullableHasValueCheck(Expression expression, out string? variableName)
		{
			variableName = null;
			if (expression is MemberReferenceExpression {
				MemberName: "HasValue",
				Target: IdentifierExpression target
			})
			{
				variableName = target.Identifier;
				return true;
			}
			return false;
		}

		static bool TryGetNullableTypePattern(
			Expression expression,
			string expectedVariableName,
			out Expression testedExpression,
			out AstType? underlyingType)
		{
			testedExpression = null!;
			underlyingType = null;
			if (expression is not BinaryOperatorExpression {
				Operator: BinaryOperatorType.IsPattern,
				Left: Expression patternOperand,
				Right: DeclarationExpression {
					Type: AstType patternType,
					Designation: SingleVariableDesignation designation
				}
			})
			{
				return false;
			}
			if (designation.Identifier != expectedVariableName)
				return false;
			if (!TryGetUnderlyingNullableType(patternType, out underlyingType))
				return false;
			testedExpression = patternOperand;
			return true;
		}

		static bool TryGetUnderlyingNullableType(AstType patternType, out AstType? underlyingType)
		{
			underlyingType = null;
			if (patternType is ComposedType { HasNullableSpecifier: true, BaseType: AstType baseType })
			{
				underlyingType = baseType.Clone();
				return true;
			}
			var type = patternType.GetResolveResult()?.Type;
			if (type != null && type.IsKnownType(KnownTypeCode.NullableOfT))
			{
				underlyingType = patternType.Clone();
				if (underlyingType is ComposedType composedType)
				{
					composedType.HasNullableSpecifier = false;
				}
				return true;
			}
			return false;
		}

		static void RemoveRedundantNullableValueAccess(AstNode node, string variableName)
		{
			foreach (var valueAccess in node.Descendants.OfType<MemberReferenceExpression>().ToList())
			{
				if (valueAccess.MemberName != "Value")
					continue;
				if (valueAccess.Target is not IdentifierExpression ident || ident.Identifier != variableName)
					continue;
				valueAccess.ReplaceWith(ident.Clone());
			}
		}
	}
}
