using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSHTML5.Tools.StubMerger
{
	public static class Merger
	{
		/// <summary>
		/// Merge the generated stub <see cref="ClassPart"/> with the existing stub <see cref="ClassPart"/>
		/// </summary>
		/// <param name="generated">The generated stub <see cref="ClassPart"/></param>
		/// <param name="existing">The existing stub <see cref="ClassPart"/></param>
		public static void MergeFiles(ClassPart generated, ClassPart existing)
		{
			CSharpParseOptions options = CSharpParseOptions.Default.WithPreprocessorSymbols("CSHTML5BLAZOR", "CSHTML5NETSTANDARD", "MIGRATION", "WORKINPROGRESS", "OPENSILVER");
			
			CompilationUnitSyntax rootGenerated = CSharpSyntaxTree.ParseText(File.ReadAllText(generated.FullPath), options).GetCompilationUnitRoot();
			CompilationUnitSyntax rootExisting = CSharpSyntaxTree.ParseText(File.ReadAllText(existing.FullPath), options).GetCompilationUnitRoot();

			if (rootGenerated.Members.Count == 0 || rootExisting.Members.Count == 0) return;

			// Add generated namespaces to the existing class file
			foreach (UsingDirectiveSyntax node in rootGenerated.Usings)
			{
				if (!rootExisting.Usings.Any(u => u.IsEquivalentTo(node)))
					rootExisting = rootExisting.AddUsings(node);
			}

			// Getting the namespace block
			NamespaceDeclarationSyntax namespaceGenerated = (NamespaceDeclarationSyntax) rootGenerated.Members[0];
			NamespaceDeclarationSyntax namespaceExisting = (NamespaceDeclarationSyntax) rootExisting.Members[0];
			
			// For each type (class, interface, struct or enum) in the generated namespace block, merging (or copying) to the existing namespace block
			List<MemberDeclarationSyntax> mergedMembers = new List<MemberDeclarationSyntax>(namespaceExisting.Members);
			foreach (MemberDeclarationSyntax generatedMember in namespaceGenerated.Members)
			{
				// Enums don't have members the same way classes, interfaces and struct have. They don't need merging.
				// In case of an existing namespace with the same name, we replace it with the generated one to keep potential new values missing in the existing one.
				if (generatedMember.Kind() == SyntaxKind.EnumDeclaration)
				{
					MemberDeclarationSyntax existingMember = namespaceExisting.Members.FirstOrDefault(m => m.Kind() == SyntaxKind.EnumDeclaration && generatedMember.IsSignatureEqual(m));
					if (existingMember == null)
					{
						mergedMembers.Add(generatedMember);
					}
					else
					{
						Console.WriteLine($"Enum {((EnumDeclarationSyntax)existingMember).Identifier.Text} is already present in file {existing.FileName}.\n");
					}
				}
				else
				{
					MemberDeclarationSyntax existingMember = namespaceExisting.Members.FirstOrDefault(m => m is TypeDeclarationSyntax && generatedMember.IsSignatureEqual(m));
					if (existingMember == null)
					{
						mergedMembers.Add(generatedMember);
					}
					else
					{
						Console.WriteLine($"Type {((TypeDeclarationSyntax)existingMember).Identifier.Text} is already present in file {existing.FileName}. Merging...\n");
						MemberDeclarationSyntax mergedMember = MergeType((TypeDeclarationSyntax) generatedMember, (TypeDeclarationSyntax) existingMember);
						mergedMembers[mergedMembers.IndexOf(existingMember)] = mergedMember;
					}
				}
			}

			// Add the new class to the namespace block
			namespaceExisting = namespaceExisting.WithMembers(new SyntaxList<MemberDeclarationSyntax>(mergedMembers));

			// Add the modified namespace to the root
			rootExisting = rootExisting.WithMembers(new SyntaxList<MemberDeclarationSyntax>(namespaceExisting));

			// Write the merged source code to the WORKINPROGRESS file
			File.WriteAllText(existing.FullPath, rootExisting.NormalizeWhitespace("\t", "\n").ToFullString());
		}
		
		/// <summary>
		/// Merge 2 types together.
		/// </summary>
		/// <param name="generatedType"></param>
		/// <param name="existingType"></param>
		/// <returns>The merged type</returns>
		private static TypeDeclarationSyntax MergeType(TypeDeclarationSyntax generatedType, TypeDeclarationSyntax existingType)
		{
			// Add generated attributes to the existing class
			foreach (AttributeListSyntax node in generatedType.AttributeLists)
			{
				if (!existingType.AttributeLists.Any(u => u.IsEquivalentTo(node)))
					existingType = existingType.AddAttributeLists(node);
			}

			// Collect and group members together
			List<MemberDeclarationSyntax> fields = new List<MemberDeclarationSyntax>();
			List<MemberDeclarationSyntax> properties = new List<MemberDeclarationSyntax>();
			List<MemberDeclarationSyntax> events = new List<MemberDeclarationSyntax>();
			List<MemberDeclarationSyntax> constructors = new List<MemberDeclarationSyntax>();
			List<MemberDeclarationSyntax> methods = new List<MemberDeclarationSyntax>();
			List<MemberDeclarationSyntax> others = new List<MemberDeclarationSyntax>();

			fields.AddRange(existingType.Members.Where(m => m.Kind() == SyntaxKind.FieldDeclaration));
			fields.AddRange(generatedType.Members.Where(m =>
			{
				if (m.Kind() != SyntaxKind.FieldDeclaration) return false;
				if (!fields.Any(m.IsSignatureEqual)) return true;

				Console.WriteLine($"Field is already present in type {existingType.Identifier.Text}:\n{m.ToString()}\n");
				return false;
			}));

			properties.AddRange(existingType.Members.Where(m => m.Kind() == SyntaxKind.PropertyDeclaration));
			properties.AddRange(generatedType.Members.Where(m =>
			{
				if (m.Kind() != SyntaxKind.PropertyDeclaration) return false;
				if (!properties.Any(m.IsSignatureEqual)) return true;

				Console.WriteLine($"Property is already present in type {existingType.Identifier.Text}:\n{m.ToString()}\n");
				return false;
			}));

			events.AddRange(existingType.Members.Where(m => m.Kind() == SyntaxKind.EventDeclaration));
			events.AddRange(generatedType.Members.Where(m =>
			{
				if (m.Kind() != SyntaxKind.EventDeclaration) return false;
				if (!events.Any(m.IsSignatureEqual)) return true;

				Console.WriteLine($"Event is already present in type {existingType.Identifier.Text}:\n{m.ToString()}\n");
				return false;
			}));

			constructors.AddRange(existingType.Members.Where(m => m.Kind() == SyntaxKind.ConstructorDeclaration));
			constructors.AddRange(generatedType.Members.Where(m =>
			{
				if (m.Kind() != SyntaxKind.ConstructorDeclaration) return false;
				if (!constructors.Any(m.IsSignatureEqual)) return true;

				Console.WriteLine($"Constructor is already present in type {existingType.Identifier.Text}:\n{m.ToString()}\n");
				return false;
			}));

			methods.AddRange(existingType.Members.Where(m => m.Kind() == SyntaxKind.MethodDeclaration));
			methods.AddRange(generatedType.Members.Where(m =>
			{
				if (m.Kind() != SyntaxKind.MethodDeclaration) return false;
				if (!methods.Any(m.IsSignatureEqual)) return true;

				Console.WriteLine($"Method is already present in type {existingType.Identifier.Text}:\n{m.ToString()}\n");
				return false;
			}));

			SyntaxKind[] alreadyTreatedMembers =
			{
				SyntaxKind.FieldDeclaration,
				SyntaxKind.PropertyDeclaration,
				SyntaxKind.ConstructorDeclaration,
				SyntaxKind.MethodDeclaration,
				SyntaxKind.EventDeclaration,
			};

			others.AddRange(existingType.Members.Where(m => !alreadyTreatedMembers.Contains(m.Kind())));
			others.AddRange(generatedType.Members.Where(m =>
			{
				if (alreadyTreatedMembers.Contains(m.Kind())) return false;
				if (!others.Any(m2 => m2.IsEquivalentTo(m))) return true;

				Console.WriteLine($"Member is already present:\n{m.ToString()}\n");
				return false;
			}));

			// Apply collected members and return the type
			return existingType
				.WithMembers(new SyntaxList<MemberDeclarationSyntax>(fields))
				.AddMembers(properties.ToArray())
				.AddMembers(events.ToArray())
				.AddMembers(constructors.ToArray())
				.AddMembers(methods.ToArray())
				.AddMembers(others.ToArray());
		}
	}
}