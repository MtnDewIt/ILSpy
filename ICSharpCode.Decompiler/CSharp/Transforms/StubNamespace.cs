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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Comments out all using directives and type/member declarations that reference
	/// types from specified namespaces. This is useful when the assembly containing
	/// those namespaces is not available and the decompiled code would otherwise
	/// produce compilation errors.
	/// </summary>
	public class StubNamespace : IAstTransform
	{
		public void Run(AstNode rootNode, TransformContext context)
		{
			var stubNamespaces = context.Settings.StubNamespaces;
			if (stubNamespaces.Count == 0)
				return;

			CommentOutUsingDirectives(rootNode, stubNamespaces);

			var stubbedMemberSymbols = CommentOutMembersWithStubbedTypes(rootNode, stubNamespaces);

			if (stubbedMemberSymbols.Count > 0)
			{
				CommentOutCallSites(rootNode, stubbedMemberSymbols);
			}
		}

		static void CommentOutUsingDirectives(AstNode rootNode, HashSet<string> stubNamespaces)
		{
			foreach (var usingDecl in rootNode.Descendants.OfType<UsingDeclaration>().ToList())
			{
				if (stubNamespaces.Contains(usingDecl.Namespace))
				{
					var comment = new Comment($"using {usingDecl.Namespace};", CommentType.SingleLine);
					AstNode? nextSibling = usingDecl.NextSibling;
					if (nextSibling != null)
					{
						nextSibling.AddLeadingTrivia(comment);
					}
					else if (usingDecl.PrevSibling != null)
					{
						usingDecl.PrevSibling.AddTrailingTrivia(comment);
					}
					else if (usingDecl.Parent != null)
					{
						usingDecl.Parent.AddTrailingTrivia(comment);
					}
					usingDecl.Remove();
				}
			}
		}

		/// <returns>A set of stubbed member symbols for call-site detection.</returns>
		static HashSet<IMember> CommentOutMembersWithStubbedTypes(AstNode rootNode, HashSet<string> stubNamespaces)
		{
			var nodesToCommentOut = new List<AstNode>();
			var stubbedMemberSymbols = new HashSet<IMember>();

			foreach (var typeDecl in rootNode.Descendants.OfType<TypeDeclaration>().ToList())
			{
				if (TypeReferencesStubbedNamespace(typeDecl, stubNamespaces))
				{
					nodesToCommentOut.Add(typeDecl);
					CollectMemberSymbols(typeDecl, stubbedMemberSymbols);
					continue;
				}

				foreach (var member in typeDecl.Members.ToList())
				{
					if (MemberReferencesStubbedNamespace(member, stubNamespaces))
					{
						nodesToCommentOut.Add(member);
						CollectMemberSymbol(member, stubbedMemberSymbols);
					}
				}
			}

			foreach (var delegateDecl in rootNode.Descendants.OfType<DelegateDeclaration>().ToList())
			{
				if (MemberReferencesStubbedNamespace(delegateDecl, stubNamespaces))
				{
					nodesToCommentOut.Add(delegateDecl);
				}
			}

			foreach (var node in nodesToCommentOut)
			{
				CommentOutNode(node);
			}

			return stubbedMemberSymbols;
		}

		static void CollectMemberSymbols(TypeDeclaration typeDecl, HashSet<IMember> stubbedMemberSymbols)
		{
			foreach (var member in typeDecl.Members)
			{
				CollectMemberSymbol(member, stubbedMemberSymbols);
			}
		}

		static void CollectMemberSymbol(EntityDeclaration member, HashSet<IMember> stubbedMemberSymbols)
		{
			var symbol = member.GetSymbol();
			if (symbol is IMember memberSymbol)
			{
				stubbedMemberSymbols.Add(memberSymbol);
			}
		}

		static bool TypeReferencesStubbedNamespace(TypeDeclaration typeDecl, HashSet<string> stubNamespaces)
		{
			foreach (var baseType in typeDecl.BaseTypes)
			{
				if (AstTypeReferencesStubbedNamespace(baseType, stubNamespaces))
					return true;
			}
			return false;
		}

		static bool MemberReferencesStubbedNamespace(EntityDeclaration member, HashSet<string> stubNamespaces)
		{
			if (member is MethodDeclaration methodDecl)
			{
				if (AstTypeReferencesStubbedNamespace(methodDecl.ReturnType, stubNamespaces))
					return true;
				foreach (var param in methodDecl.Parameters)
				{
					if (AstTypeReferencesStubbedNamespace(param.Type, stubNamespaces))
						return true;
				}
			}

			if (member is PropertyDeclaration propDecl)
			{
				if (AstTypeReferencesStubbedNamespace(propDecl.ReturnType, stubNamespaces))
					return true;
			}

			if (member is FieldDeclaration fieldDecl)
			{
				if (AstTypeReferencesStubbedNamespace(fieldDecl.ReturnType, stubNamespaces))
					return true;
			}

			if (member is EventDeclaration eventDecl)
			{
				if (AstTypeReferencesStubbedNamespace(eventDecl.ReturnType, stubNamespaces))
					return true;
			}

			if (member is DelegateDeclaration delDecl)
			{
				if (AstTypeReferencesStubbedNamespace(delDecl.ReturnType, stubNamespaces))
					return true;
				foreach (var param in delDecl.Parameters)
				{
					if (AstTypeReferencesStubbedNamespace(param.Type, stubNamespaces))
						return true;
				}
			}

			foreach (var astType in member.Descendants.OfType<AstType>())
			{
				if (AstTypeReferencesStubbedNamespace(astType, stubNamespaces))
					return true;
			}

			return false;
		}

		static bool AstTypeReferencesStubbedNamespace(AstType? astType, HashSet<string> stubNamespaces)
		{
			if (astType == null)
				return false;

			var trr = astType.Annotation<TypeResolveResult>();
			if (trr != null)
			{
				var fullTypeName = trr.Type.FullName;
				if (IsStubbedNamespace(fullTypeName, stubNamespaces))
					return true;
			}

			string typeString = astType.ToString();
			if (IsStubbedNamespace(typeString, stubNamespaces))
				return true;

			return false;
		}

		static bool IsStubbedNamespace(string fullTypeName, HashSet<string> stubNamespaces)
		{
			foreach (var ns in stubNamespaces)
			{
				if (fullTypeName == ns || fullTypeName.StartsWith(ns + ".", StringComparison.Ordinal))
					return true;
			}
			return false;
		}

		static void CommentOutCallSites(AstNode rootNode, HashSet<IMember> stubbedMemberSymbols)
		{
			// Find all InvocationExpression nodes whose target is a stubbed member
			foreach (var invocation in rootNode.Descendants.OfType<InvocationExpression>().ToList())
			{
				var rr = invocation.GetResolveResult();
				if (rr is MemberResolveResult mrr && stubbedMemberSymbols.Contains(mrr.Member))
				{
					// Find the containing statement and replace it with a comment in-place
					Statement? stmt = invocation.GetParent<Statement>();
					if (stmt != null)
					{
						ReplaceStatementWithComment(stmt);
					}
					else
					{
						// Fallback: just comment out the expression
						CommentOutNode(invocation);
					}
				}
			}
		}

		/// <summary>
		/// Replaces the given statement with a commented-out version of itself.
		/// The comment stays in the correct position within the block.
		/// </summary>
		static void ReplaceStatementWithComment(Statement stmt)
		{
			string renderedText = RenderNodeToString(stmt);
			if (string.IsNullOrWhiteSpace(renderedText))
				return;

			var commentTrivia = CreateCommentTrivia(renderedText);

			var emptyStmt = new EmptyStatement();
			foreach (var trivia in commentTrivia)
				emptyStmt.AddTrailingTrivia(trivia);
			stmt.ReplaceWith(emptyStmt);
		}

		/// <summary>
		/// Comments out an EntityDeclaration (method, field, property, event, delegate, type)
		/// by removing it from the AST and attaching the commented-out text as a multi-line
		/// block comment to a sibling node.
		/// 
		/// The output visitor renders CommentType.MultiLine as:
		///   {WriteIndentation()}/*{content}*/
		/// 
		/// So we include leading/trailing newlines and indentation in the content
		/// to place /* and */ on their own lines at the correct nesting level.
		/// Blank lines are added only between siblings, never at the end of a block.
		/// </summary>
		static void CommentOutNode(AstNode node)
		{
			string renderedText = RenderNodeToString(node);

			if (string.IsNullOrWhiteSpace(renderedText))
				return;

			bool hasPrevSibling = node.PrevSibling != null;
			bool hasNextSibling = node.NextSibling != null;

			// Compute indent based on AST nesting depth
			int depth = 0;
			AstNode? current = node.Parent;
			while (current != null)
			{
				if (current is TypeDeclaration || current is NamespaceDeclaration || current is BlockStatement)
					depth++;
				current = current.Parent;
			}
			string indent = new string('\t', depth);

			// Build content: [\n]<code>\n<indent>[\n]
			var sb = new System.Text.StringBuilder();
			if (hasPrevSibling)
				sb.Append('\n');
			sb.Append(renderedText);
			sb.Append('\n');
			sb.Append(indent);
			if (hasNextSibling)
				sb.Append('\n');

			var comment = new Comment(sb.ToString(), CommentType.MultiLine);

			// Attach the comment to a sibling, then remove the node.
			AstNode? nextSibling = node.NextSibling;
			if (nextSibling != null)
			{
				nextSibling.AddLeadingTrivia(comment);
			}
			else if (node.PrevSibling != null)
			{
				node.PrevSibling.AddTrailingTrivia(comment);
			}
			else if (node.Parent != null)
			{
				node.Parent.AddTrailingTrivia(comment);
			}

			node.Remove();
		}

		/// <summary>
		/// Creates Comment trivia for each line of the rendered text.
		/// Leading whitespace is stripped because the output visitor adds its
		/// own indentation via <c>WriteIndentation()</c>, and the <c>//</c>
		/// prefix is also added by the visitor.
		/// </summary>
		static List<Trivia> CreateCommentTrivia(string renderedText)
		{
			var lines = renderedText.Replace("\r\n", "\n").Split('\n');
			var trivia = new List<Trivia>(lines.Length);
			foreach (var line in lines)
			{
				trivia.Add(new Comment(line.TrimStart(), CommentType.SingleLine));
			}
			return trivia;
		}

		static string RenderNodeToString(AstNode node)
		{
			using (var writer = new StringWriter())
			{
				var formattingOptions = FormattingOptionsFactory.CreateAllman();
				node.AcceptVisitor(new CSharpOutputVisitor(writer, formattingOptions));
				return writer.ToString().TrimEnd();
			}
		}
	}
}