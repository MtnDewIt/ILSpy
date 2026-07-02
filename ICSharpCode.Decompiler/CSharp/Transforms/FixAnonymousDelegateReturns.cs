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

using System.Linq;

using System.Diagnostics.CodeAnalysis;

using ICSharpCode.Decompiler.CSharp.Resolver;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Fixes anonymous methods passed as delegate arguments that return null where a lambda
	/// or an explicit null cast is required for the decompiled code to compile.
	/// </summary>
	public sealed class FixAnonymousDelegateReturns : DepthFirstAstVisitor, IAstTransform
	{
		[AllowNull]
		TransformContext context;

		public void Run(AstNode rootNode, TransformContext context)
		{
			try
			{
				this.context = context;
				rootNode.AcceptVisitor(this);
			}
			finally
			{
				this.context = null;
			}
		}

		public override void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
		{
			base.VisitAnonymousMethodExpression(anonymousMethodExpression);
			Expression argumentExpression = anonymousMethodExpression;
			if (anonymousMethodExpression.Parent is CastExpression cast)
				argumentExpression = cast;
			if (argumentExpression.GetParent<InvocationExpression>() == null)
				return;
			if (anonymousMethodExpression.Body is not BlockStatement body)
				return;
			if (body.Statements.Count != 2)
				return;
			if (body.Statements[0] is not ExpressionStatement { Expression: InvocationExpression invoke })
				return;
			if (body.Statements[1] is not ReturnStatement { Expression: Expression returnExpr })
				return;
			if (!IsNullLiteral(returnExpr))
				return;
			if (anonymousMethodExpression.Parameters.Count > 0)
				return;

			IType? delegateReturnType = GetTargetDelegateReturnType(anonymousMethodExpression);
			if (delegateReturnType == null || delegateReturnType.Kind == TypeKind.Void)
				return;

			var invokeResult = invoke.GetResolveResult();
			bool isVoidInvoke = invokeResult?.Type.Kind == TypeKind.Void;
			if (!isVoidInvoke && invokeResult is InvocationResolveResult irr)
			{
				isVoidInvoke = irr.Member.ReturnType.Kind == TypeKind.Void;
			}
			if (isVoidInvoke)
			{
				IType objectType = context.TypeSystem.MainModule.Compilation.FindType(KnownTypeCode.Object);
				var nullRR = new ConstantResolveResult(SpecialType.NullType, null);
				body.Statements[1] = new ReturnStatement {
					Expression = new CastExpression(
						new PrimitiveType("object"),
						new NullReferenceExpression().WithRR(nullRR))
						.WithRR(new ConversionResolveResult(objectType, nullRR, Conversion.NullLiteralConversion))
				};
				return;
			}

			if (invokeResult != null
				&& CSharpConversions.Get(context.TypeSystem.MainModule.Compilation)
					.ImplicitConversion(invokeResult, delegateReturnType).IsValid)
			{
				var lambda = new LambdaExpression {
					Body = invoke.Detach()
				};
				anonymousMethodExpression.ReplaceWith(lambda.CopyAnnotationsFrom(anonymousMethodExpression));
			}
		}

		static bool IsNullLiteral(Expression expression)
		{
			return expression is NullReferenceExpression
				|| expression is CastExpression { Expression: NullReferenceExpression };
		}

		static IType? GetTargetDelegateReturnType(AnonymousMethodExpression anonymousMethodExpression)
		{
			Expression argumentExpression = anonymousMethodExpression;
			if (anonymousMethodExpression.Parent is CastExpression cast)
				argumentExpression = cast;
			if (argumentExpression.GetParent<InvocationExpression>() is not InvocationExpression invocation
				|| invocation.GetResolveResult() is not InvocationResolveResult irr)
			{
				return null;
			}
			int argIndex = invocation.Arguments.IndexOf(argumentExpression);
			if (argIndex >= 0 && argIndex < irr.Member.Parameters.Count)
			{
				return irr.Member.Parameters[argIndex].Type.GetDelegateInvokeMethod()?.ReturnType;
			}
			if (anonymousMethodExpression.GetResolveResult() is ConversionResolveResult crr)
			{
				return crr.Type.GetDelegateInvokeMethod()?.ReturnType;
			}
			return null;
		}
	}
}
