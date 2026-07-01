// Copyright (c) 2026 ILSpy contributors
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

using System.Linq;

using ICSharpCode.Decompiler.CSharp.Resolver;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Removes redundant casts that the IL-to-AST translation emits for compatibility but that
	/// are not required in valid C# output.
	/// </summary>
	public class SimplifyRedundantCasts : DepthFirstAstVisitor, IAstTransform
	{
		TransformContext context;

		public override void VisitCastExpression(CastExpression castExpression)
		{
			base.VisitCastExpression(castExpression);

			if (castExpression.Expression is NullReferenceExpression
				&& castExpression.Type.GetResolveResult()?.Type is IType targetType
				&& targetType.IsReferenceType != false)
			{
				if (castExpression.Annotation<OverloadDisambiguationAnnotation>() == null)
				{
					castExpression.ReplaceWith(new NullReferenceExpression().CopyAnnotationsFrom(castExpression));
				}
				return;
			}

			if (castExpression.Expression is CastExpression innerCast
				&& innerCast.Expression is not NullReferenceExpression
				&& innerCast.Type.GetResolveResult()?.Type.IsKnownType(KnownTypeCode.Object) == true
				&& TryResolveDirectCast(castExpression.Type.GetResolveResult()?.Type, innerCast.Expression.GetResolveResult()?.Type))
			{
				innerCast.Type = castExpression.Type.Detach();
				castExpression.ReplaceWith(innerCast.CopyAnnotationsFrom(castExpression));
				return;
			}

			if (castExpression.Expression is InvocationExpression invocation
				&& invocation.Target is MemberReferenceExpression { MemberName: "ToString", Target: Expression toStringTarget }
				&& castExpression.Type.GetResolveResult()?.Type.IsKnownType(KnownTypeCode.Object) == true
				&& toStringTarget.GetResolveResult()?.Type.IsKnownType(KnownTypeCode.Object) != true)
			{
				castExpression.ReplaceWith(invocation.Detach().CopyAnnotationsFrom(castExpression));
			}
		}

		bool TryResolveDirectCast(IType targetType, IType sourceType)
		{
			if (targetType == null || sourceType == null)
				return false;
			if (targetType.IsKnownType(KnownTypeCode.Object) || sourceType.IsKnownType(KnownTypeCode.Object))
				return false;
			if (targetType.IsReferenceType != true || sourceType.IsReferenceType != true)
				return false;

			var compilation = context.TypeSystem.MainModule.Compilation;
			var conversions = CSharpConversions.Get(compilation);
			var rr = new ResolveResult(sourceType);
			return conversions.ExplicitConversion(rr, targetType).IsValid
				|| conversions.ImplicitConversion(rr, targetType).IsValid;
		}

		void IAstTransform.Run(AstNode rootNode, TransformContext context)
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
	}

	/// <summary>
	/// Marks a cast expression as necessary for C# overload disambiguation.
	/// When two or more overloads share the same name and parameter count but differ
	/// in parameter types, a null literal argument needs an explicit cast to resolve
	/// the ambiguity. This annotation prevents the cast from being simplified away.
	/// </summary>
	public class OverloadDisambiguationAnnotation
	{
		internal static readonly OverloadDisambiguationAnnotation Instance = new();
	}
}