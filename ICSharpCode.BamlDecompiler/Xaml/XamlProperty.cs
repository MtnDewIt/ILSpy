/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System.Linq;
using System.Xml;
using System.Xml.Linq;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.BamlDecompiler.Xaml
{
	internal class XamlProperty
	{
		public XamlType DeclaringType { get; }
		public string PropertyName { get; }

		public IMember ResolvedMember { get; set; }

		public XamlProperty(XamlType type, string name)
		{
			DeclaringType = type;
			PropertyName = name;
		}

		public void TryResolve()
		{
			if (ResolvedMember != null)
				return;

			var typeDef = DeclaringType.ResolvedType.GetDefinition();
			if (typeDef == null)
				return;

			ResolvedMember = typeDef.GetProperties(p => p.Name == PropertyName).FirstOrDefault();
			if (ResolvedMember != null)
				return;

			ResolvedMember = typeDef.GetFields(f => f.Name == PropertyName + "Property").FirstOrDefault();
			if (ResolvedMember != null)
				return;

			ResolvedMember = typeDef.GetEvents(e => e.Name == PropertyName).FirstOrDefault();
			if (ResolvedMember != null)
				return;

			ResolvedMember = typeDef.GetFields(f => f.Name == PropertyName + "Event").FirstOrDefault();
		}

		public bool IsAttachedTo(XamlType type)
		{
			if (type == null || type.ResolvedType == null)
				return true;

			// Check if the element type itself is the declaring type.
			if (ResolvedMember != null)
			{
				if (type.ResolvedType.FullName == ResolvedMember.DeclaringType.FullName
					&& type.ResolvedType.TypeParameterCount == ResolvedMember.DeclaringType.TypeParameterCount)
					return false;
			}
			else if (DeclaringType.ResolvedType != null)
			{
				if (type.ResolvedType.FullName == DeclaringType.ResolvedType.FullName
					&& type.ResolvedType.TypeParameterCount == DeclaringType.ResolvedType.TypeParameterCount)
					return false;
			}

			// Walk the type hierarchy via DirectBaseTypes. This works for types from
			// fully loaded assemblies (e.g. known WPF types).
			if (ResolvedMember != null)
			{
				var declType = ResolvedMember.DeclaringType;
				var t = type.ResolvedType.DirectBaseTypes.FirstOrDefault();
				if (t == null)
				{
					// DirectBaseTypes is empty — the element is from an assembly
					// that isn't fully loaded (e.g. a third-party control like
					// WPFToolkit's DatePicker). ResolvedMember gives us the key
					// distinction:
					// - IProperty → inherited or directly-declared property
					//   (e.g. FrameworkElement.Width). Return false (not attached).
					// - IField → DependencyProperty backing field (e.g. Grid.RowProperty).
					//   Return true (attached).
					return ResolvedMember is not IProperty;
				}

				do
				{
					if (t.FullName == declType.FullName && t.TypeParameterCount == declType.TypeParameterCount)
						return false;
					t = t.DirectBaseTypes.FirstOrDefault();
				} while (t != null);
				return true;
			}

			if (DeclaringType.ResolvedType != null)
			{
				var declType = DeclaringType.ResolvedType;
				var t = type.ResolvedType.DirectBaseTypes.FirstOrDefault();
				if (t == null)
				{
					var typeDef = type.ResolvedType.GetDefinition();
					if (typeDef != null && typeDef.GetProperties(p => p.Name == PropertyName).Any())
						return false;
					return true;
				}

				do
				{
					if (t.FullName == declType.FullName && t.TypeParameterCount == declType.TypeParameterCount)
						return false;
					t = t.DirectBaseTypes.FirstOrDefault();
				} while (t != null);
			}

			return true;
		}

		public XName ToXName(XamlContext ctx, XElement parent, bool isFullName = true)
		{
			var typeName = DeclaringType.ToXName(ctx);
			XName name;
			if (!isFullName)
				name = XmlConvert.EncodeLocalName(PropertyName);
			else
			{
				name = typeName.LocalName + "." + XmlConvert.EncodeLocalName(PropertyName);
				if (parent == null || parent.GetDefaultNamespace() != typeName.Namespace)
					name = typeName.Namespace + name.LocalName;
			}

			return name;
		}

		public override string ToString() => PropertyName;
	}
}