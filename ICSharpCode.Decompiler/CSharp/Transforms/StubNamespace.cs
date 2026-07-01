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
					// Replace the invocation expression with a comment
					string renderedText = RenderNodeToString(invocation);
					if (!string.IsNullOrWhiteSpace(renderedText))
					{
						var commentText = BuildCommentedLines(renderedText, GetNodeIndent(invocation));
						var comment = new Comment(commentText, CommentType.SingleLine);

						// Replace the statement containing this invocation with a comment
						Statement? stmt = invocation.GetParent<Statement>();
						if (stmt != null)
						{
							ReplaceStatementWithComment(stmt, comment);
						}
						else
						{
							// Fallback: just comment out the expression
							CommentOutNode(invocation);
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets the indentation level (in spaces) of the given node based on its start location.
		/// </summary>
		static string GetNodeIndent(AstNode node)
		{
			int column = node.StartLocation.Column;
			// Column is 1-based, convert to 0-based indent
			int indent = Math.Max(0, column - 1);
			return new string(' ', indent);
		}

		static void ReplaceStatementWithComment(Statement stmt, Comment comment)
		{
			string renderedText = RenderNodeToString(stmt);
			if (string.IsNullOrWhiteSpace(renderedText))
				return;

			var indent = GetNodeIndent(stmt);
			var commentText = BuildCommentedLines(renderedText, indent);
			var blockComment = new Comment(commentText, CommentType.SingleLine);

			AstNode? nextSibling = stmt.NextSibling;
			if (nextSibling != null)
			{
				nextSibling.AddLeadingTrivia(blockComment);
			}
			else if (stmt.PrevSibling != null)
			{
				stmt.PrevSibling.AddTrailingTrivia(blockComment);
			}
			else if (stmt.Parent != null)
			{
				stmt.Parent.AddTrailingTrivia(blockComment);
			}

			stmt.Remove();
		}

		static void CommentOutNode(AstNode node)
		{
			string renderedText = RenderNodeToString(node);

			if (string.IsNullOrWhiteSpace(renderedText))
				return;

			var indent = GetNodeIndent(node);
			var commentText = BuildCommentedLines(renderedText, indent);

			var comment = new Comment(commentText, CommentType.SingleLine);

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

		static string BuildCommentedLines(string text, string indent)
		{
			var lines = text.Replace("\r\n", "\n").Split('\n');
			var sb = new System.Text.StringBuilder();
			for (int i = 0; i < lines.Length; i++)
			{
				if (i > 0)
					sb.Append('\n');
				sb.Append(indent);
				sb.Append("//");
				sb.Append(lines[i]);
			}
			return sb.ToString();
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