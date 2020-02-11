using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSHTML5.Tools.StubMerger
{
	public static class MemberComparer
	{
		public static bool IsSignatureEqual(this MemberDeclarationSyntax member1, MemberDeclarationSyntax member2)
		{
			if (member1.Kind() != member2.Kind()) throw new ArgumentException("Members are not the same type.");
			
			if (member1 is PropertyDeclarationSyntax property1 && member2 is PropertyDeclarationSyntax property2)
			{
				return IsPropertySignatureEqual(property1, property2);
			}
			if (member1 is MethodDeclarationSyntax method1 && member2 is MethodDeclarationSyntax method2)
			{
				return IsMethodSignatureEqual(method1, method2);
			}
			if (member1 is FieldDeclarationSyntax field1 && member2 is FieldDeclarationSyntax field2)
			{
				return IsFieldSignatureEqual(field1, field2);
			}
			
			throw new ArgumentException("Members type comparison isn't implemented");
		}

		private static bool IsFieldSignatureEqual(FieldDeclarationSyntax field1, FieldDeclarationSyntax field2)
		{
			bool nameEqual = true;
			if (field1.Declaration.Variables.Count == field2.Declaration.Variables.Count)
			{
				if (field1.Declaration.Variables.Where((t, i) => t.Identifier.Text != field2.Declaration.Variables[i].Identifier.Text).Any())
				{
					nameEqual = false;
				}
			}
			else
			{
				nameEqual = false;
			}
			bool typeEqual = field1.Declaration.Type.ToString() == field2.Declaration.Type.ToString();

			return nameEqual && typeEqual;
		}

		private static bool IsPropertySignatureEqual(PropertyDeclarationSyntax property1, PropertyDeclarationSyntax property2)
		{
			bool nameEqual = property1.Identifier.ValueText == property2.Identifier.ValueText;
			bool typeEqual = property1.Type.ToString() == property2.Type.ToString();
				
			return nameEqual && typeEqual;
		}

		private static bool IsMethodSignatureEqual(MethodDeclarationSyntax method1, MethodDeclarationSyntax method2)
		{
			bool nameEqual = method1.Identifier.Text == method2.Identifier.Text;
			
			bool returnTypeEqual;
			if (method1.Arity > 0 && method2.Arity > 0) // If both methods are generic
			{
				// Find if the return type is one of the generic types, and if true, get its index
				int method1ReturnTypeGenericIndex = -1;
				for (int i = 0; i < method1.TypeParameterList.Parameters.Count; i++)
				{
					if (method1.TypeParameterList.Parameters[i].ToString() == method1.ReturnType.ToString())
					{
						method1ReturnTypeGenericIndex = i;
						break;
					}
				}
				
				int method2ReturnTypeGenericIndex = -1;
				for (int i = 0; i < method2.TypeParameterList.Parameters.Count; i++)
				{
					if (method2.TypeParameterList.Parameters[i].Identifier.ValueText == method2.ReturnType.ToString())
					{
						method2ReturnTypeGenericIndex = i;
						break;
					}
				}

				// If both return types are generic and they refer to the same generic parameter index, we can say they are the same
				if (method1ReturnTypeGenericIndex > -1 && method2ReturnTypeGenericIndex > -1 && method1ReturnTypeGenericIndex == method2ReturnTypeGenericIndex)
				{
					returnTypeEqual = true;
				}
				else
				{
					returnTypeEqual = method1.ReturnType.IsEquivalentTo(method2.ReturnType);
				}
			}
			else
			{
				returnTypeEqual = method1.ReturnType.IsEquivalentTo(method2.ReturnType);
			}
			
			bool parametersEqual = true;
			if (method1.ParameterList.Parameters.Count == method2.ParameterList.Parameters.Count && method1.Arity == method2.Arity)
			{
				for (int i = 0; i < method1.ParameterList.Parameters.Count; i++)
				{
					if (method1.ParameterList.Parameters[i].Type.IsEquivalentTo(method2.ParameterList.Parameters[i].Type)) continue;
						
					parametersEqual = false;
					break;
				}
			}
			else
			{
				parametersEqual = false;
			}
				
			return nameEqual && returnTypeEqual && parametersEqual;
		}
	}
}