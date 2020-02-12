using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSHTML5.Tools.StubMerger
{
	public static class MemberComparer
	{
		/// <summary>
		/// <para>Performs a signature comparison on supported member types.</para>
		/// <para>Supported types are:
		/// <br/><see cref="PropertyDeclarationSyntax"/>
		/// <br/><see cref="MethodDeclarationSyntax"/>
		/// <br/><see cref="FieldDeclarationSyntax"/>
		/// <br/><see cref="ConstructorDeclarationSyntax"/></para>
		/// </summary>
		/// <param name="member1">First member</param>
		/// <param name="member2">Second member</param>
		/// <returns>The result of the signature comparison.</returns>
		/// <exception cref="ArgumentException">Members are not the same type or the type is not suported.</exception>
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

			if (member1 is ConstructorDeclarationSyntax const1 && member2 is ConstructorDeclarationSyntax const2)
			{
				return IsConstructorSignatureEqual(const1, const2);
			}

			throw new ArgumentException("Members type comparison isn't implemented");
		}

		/// <summary>
		/// Checks for constructor signature equality.
		/// Doesn't handle generic types the same way <see cref="IsMethodSignatureEqual"/> does.
		/// </summary>
		/// <param name="const1">First constructor</param>
		/// <param name="const2">Second constructor</param>
		/// <returns>The result of the signature comparison.</returns>
		private static bool IsConstructorSignatureEqual(ConstructorDeclarationSyntax const1, ConstructorDeclarationSyntax const2)
		{
			bool parametersEqual = true;
			if (const1.ParameterList.Parameters.Count == const2.ParameterList.Parameters.Count)
			{
				for (int i = 0; i < const1.ParameterList.Parameters.Count; i++)
				{
					if (const1.ParameterList.Parameters[i].Type.IsEquivalentTo(const2.ParameterList.Parameters[i].Type)) continue;

					parametersEqual = false;
					break;
				}
			}
			else
			{
				parametersEqual = false;
			}

			return parametersEqual;
		}

		/// <summary>
		/// Checks for field signature equality.
		/// Doesn't handle generic types the same way <see cref="IsMethodSignatureEqual"/> does.
		/// </summary>
		/// <param name="field1">First field</param>
		/// <param name="field2">Second field</param>
		/// <returns>The result of the signature comparison.</returns>
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

			return nameEqual;
		}

		/// <summary>
		/// Checks for property signature equality.
		/// Doesn't handle generic types the same way <see cref="IsMethodSignatureEqual"/> does.
		/// </summary>
		/// <param name="property1">First property</param>
		/// <param name="property2">Second property</param>
		/// <returns>The result of the signature comparison.</returns>
		private static bool IsPropertySignatureEqual(PropertyDeclarationSyntax property1, PropertyDeclarationSyntax property2)
		{
			bool nameEqual = property1.Identifier.ValueText == property2.Identifier.ValueText;

			return nameEqual;
		}

		/// <summary>
		/// <para>Checks for method signature equality.</para>
		/// <para>Handles generic return type ("T foo&lt;T, T2&gt;()" will be considered to have the same signature as "Tnum foo&lt;Tnum, Ttoto&gt;()").</para>
		/// </summary>
		/// <param name="method1">First method</param>
		/// <param name="method2">Second method</param>
		/// <returns>The result of the signature comparison.</returns>
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