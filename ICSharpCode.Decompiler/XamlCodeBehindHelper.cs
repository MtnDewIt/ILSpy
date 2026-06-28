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

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Detects WPF/XAML generated code-behind members that belong in the XAML partial class
	/// rather than the hand-written code-behind file.
	/// </summary>
	public static class XamlCodeBehindHelper
	{
		static readonly TopLevelTypeName componentConnectorTypeName
			= new TopLevelTypeName("System.Windows.Markup", "IComponentConnector");

		static readonly TopLevelTypeName styleConnectorTypeName
			= new TopLevelTypeName("System.Windows.Markup", "IStyleConnector");

		public static bool IsXamlCodeBehindClass(ITypeDefinition typeDefinition)
		{
			if (typeDefinition == null)
				return false;

			if (typeDefinition.Methods.Any(IsGeneratedMainMethod))
				return true;

			if (typeDefinition.Methods.Any(IsGeneratedInitializeComponent))
				return true;

			var connector = typeDefinition.Compilation.FindType(componentConnectorTypeName).GetDefinition();

			if (connector == null)
				return false;

			var connect = connector.GetMethods(m => m.Name == "Connect").SingleOrDefault();

			if (connect == null)
				return false;

			return typeDefinition.Methods.Any(m => m.ExplicitlyImplementedInterfaceMembers.Any(md => md.MemberDefinition.Equals(connect)));
		}

		public static bool IsGeneratedMember(IEntity entity, ITypeDefinition declaringType)
		{
			switch (entity)
			{
				case IMethod method:
					return IsGeneratedInitializeComponent(method)
						|| IsGeneratedConnectMethod(method, declaringType)
						|| IsGeneratedCreateDelegate(method)
						|| IsGeneratedMainMethod(method);
				case IField field:
					return IsGeneratedField(field, declaringType);
				default:
					return false;
			}
		}

		static bool IsGeneratedMainMethod(IMethod method)
		{
			return method.Parameters.Count == 0
				&& method.ReturnType.Kind == TypeKind.Void
				&& method.IsStatic
				&& method.Name == "Main"
				&& HasGeneratedCodeAttribute(method);
		}

		static bool IsGeneratedInitializeComponent(IMethod method)
		{
			return method.Parameters.Count == 0
				&& method.ReturnType.Kind == TypeKind.Void
				&& !method.IsStatic
				&& method.Name == "InitializeComponent"
				&& HasGeneratedCodeAttribute(method);
		}

		static bool IsGeneratedCreateDelegate(IMethod method)
		{
			return method.Name == "_CreateDelegate"
				&& method.Parameters.Count == 2
				&& method.Parameters[0].Type.IsKnownType(KnownTypeCode.Type)
				&& method.Parameters[1].Type.IsKnownType(KnownTypeCode.String)
				&& method.ReturnType.IsKnownType(KnownTypeCode.Delegate)
				&& !method.IsStatic
				&& HasGeneratedCodeAttribute(method)
				&& HasDebuggerNonUserCodeAttribute(method);
		}

		static bool IsGeneratedConnectMethod(IMethod method, ITypeDefinition declaringType)
		{
			if (!HasGeneratedCodeAttribute(method) && !HasDebuggerNonUserCodeAttribute(method))
				return false;

			var componentConnector = declaringType.Compilation.FindType(componentConnectorTypeName).GetDefinition();
			var styleConnector = declaringType.Compilation.FindType(styleConnectorTypeName).GetDefinition();

			if (componentConnector == null || styleConnector == null)
				return false;

			var componentConnect = componentConnector.GetMethods(m => m.Name == "Connect").SingleOrDefault();
			var styleConnect = styleConnector.GetMethods(m => m.Name == "Connect").SingleOrDefault();

			return (componentConnect != null && method.ExplicitlyImplementedInterfaceMembers.Any(md => md.MemberDefinition.Equals(componentConnect))) || 
				   (styleConnect != null && method.ExplicitlyImplementedInterfaceMembers.Any(md => md.MemberDefinition.Equals(styleConnect)));
		}

		static bool IsGeneratedField(IField field, ITypeDefinition declaringType)
		{
			if (field.Name == "_contentLoaded"
				&& field.Type.IsKnownType(KnownTypeCode.Boolean)
				&& field.Accessibility == Accessibility.Private
				&& !field.IsStatic)
			{
				return true;
			}
			// Named element field assigned in IComponentConnector.Connect (e.g. internal Foo userControl).
			if (field.Accessibility == Accessibility.Internal
				&& !field.IsStatic
				&& declaringType.Equals(field.Type))
			{
				return IsXamlCodeBehindClass(declaringType);
			}
			return false;
		}

		static bool HasGeneratedCodeAttribute(IEntity entity)
		{
			return entity.GetAttributes()
				.Any(a => a.AttributeType.ReflectionName == "System.CodeDom.Compiler.GeneratedCodeAttribute");
		}

		static bool HasDebuggerNonUserCodeAttribute(IEntity entity)
		{
			return entity.GetAttributes()
				.Any(a => a.AttributeType.ReflectionName == "System.Diagnostics.DebuggerNonUserCodeAttribute");
		}
	}
}