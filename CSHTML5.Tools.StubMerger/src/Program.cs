using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSHTML5.Tools.StubMerger
{
	internal static class Program
	{
		private static void Main(string[] args)
		{
			// ============ INPUTS ============
			string generatednamespacesRoot = @"C:\Projects\2019\CSHTML5.Tools\toMerge\Generated";
			string CSHTML5Path = @"C:\Projects\2019\CSHTML5.Tools\toMerge\Dummy\CSHTML5";
			// string CSHTML5Path = @"C:\DotNetForHtml5\DotNetForHtml5\_GitHub\CSHTML5";
			// ============ /INPUTS ============

			string CSHTML5NamespacesRoot = Path.Combine(CSHTML5Path, @"src\CSHTML5.Runtime");

			Run(generatednamespacesRoot, CSHTML5NamespacesRoot);
		}

		private static void Run(string generatedNamespacesRoot, string CSHTML5NamespacesRoot)
		{
			HashSet<Namespace> generatedNamespaces = Namespace.GetGeneratedNamespaces(generatedNamespacesRoot);

			// For each generated namespace
			foreach (Namespace generatedNamespace in generatedNamespaces)
			{
				// Retrieve the CSHTML5-equivalent of the generated namespace
				Namespace existingNamespace = Namespace.GetExistingNamespace(CSHTML5NamespacesRoot, generatedNamespace.Name);

				// Copy or merge the generated stub, depending on the situtation
				foreach (ClassPart stubClassPart in generatedNamespace.ClassParts)
				{
					if (existingNamespace.ContainsClassWithName(stubClassPart.Name, ClassFilter.STUB)) // If a stub class with the same name already exists in CSHTML5, merge them
					{
						HashSet<ClassPart> existingStubs = existingNamespace.GetClassPartsWithName(stubClassPart.Name, ClassFilter.STUB);
						if (existingStubs.Count > 1) throw new InvalidDataException($"More than one stub class part with the name \"{stubClassPart.Name}\" has been found in the namespace {existingNamespace.Name} located at {existingNamespace.FullPath}.");

						MergeStubs(stubClassPart, existingStubs.First());
					}
					else // Otherwise, simply copy it
					{
						File.Copy(
							Path.Combine(generatedNamespace.FullPath, stubClassPart.FileName),
							Path.Combine(existingNamespace.FullPath, "WORKINPROGRESS", stubClassPart.FileName)
						);
					}
				}
			}
		}

		/// <summary>
		/// Merge the generated stub <see cref="ClassPart"/> with the existing stub <see cref="ClassPart"/>
		/// </summary>
		/// <param name="generated">The generated stub <see cref="ClassPart"/></param>
		/// <param name="existing">The existing stub <see cref="ClassPart"/></param>
		private static void MergeStubs(ClassPart generated, ClassPart existing)
		{
			CompilationUnitSyntax rootGenerated = CSharpSyntaxTree.ParseText(File.ReadAllText(generated.FullPath)).GetCompilationUnitRoot();
			CompilationUnitSyntax rootExisting = CSharpSyntaxTree.ParseText(File.ReadAllText(existing.FullPath)).GetCompilationUnitRoot();

			// Add generated namespaces to the existing class file
			foreach (UsingDirectiveSyntax node in rootGenerated.Usings)
			{
				if (!rootExisting.Usings.Any(u => u.IsEquivalentTo(node)))
					rootExisting = rootExisting.AddUsings(node);
			}

			NamespaceDeclarationSyntax namespaceGenerated = (NamespaceDeclarationSyntax) rootGenerated.Members[0];
			ClassDeclarationSyntax classGenerated = (ClassDeclarationSyntax) namespaceGenerated.Members[0];

			NamespaceDeclarationSyntax namespaceExisting = (NamespaceDeclarationSyntax) rootExisting.Members[0];
			ClassDeclarationSyntax classExisting = (ClassDeclarationSyntax) namespaceExisting.Members[0];

			// Add generated attributes to the existing class
			foreach (AttributeListSyntax node in classGenerated.AttributeLists)
			{
				if (!classExisting.AttributeLists.Any(u => u.IsEquivalentTo(node)))
					classExisting = classExisting.AddAttributeLists(node);
			}

			// Collect and group members together
			List<MemberDeclarationSyntax> fields = new List<MemberDeclarationSyntax>();
			List<MemberDeclarationSyntax> properties = new List<MemberDeclarationSyntax>();
			List<MemberDeclarationSyntax> constructors = new List<MemberDeclarationSyntax>();
			List<MemberDeclarationSyntax> methods = new List<MemberDeclarationSyntax>();
			List<MemberDeclarationSyntax> others = new List<MemberDeclarationSyntax>();

			fields.AddRange(classExisting.Members.Where(m => m.Kind() == SyntaxKind.FieldDeclaration));
			fields.AddRange(classGenerated.Members.Where(m =>
			{
				if (m.Kind() != SyntaxKind.FieldDeclaration) return false;
				if (!fields.Any(m.IsSignatureEqual)) return true;

				Console.WriteLine($"Field is already present:\n{m.ToString()}\n");
				return false;
			}));

			properties.AddRange(classExisting.Members.Where(m => m.Kind() == SyntaxKind.PropertyDeclaration));
			properties.AddRange(classGenerated.Members.Where(m =>
			{
				if (m.Kind() != SyntaxKind.PropertyDeclaration) return false;
				if (!properties.Any(m.IsSignatureEqual)) return true;

				Console.WriteLine($"Property is already present:\n{m.ToString()}\n");
				return false;
			}));

			constructors.AddRange(classExisting.Members.Where(m => m.Kind() == SyntaxKind.ConstructorDeclaration));
			constructors.AddRange(classGenerated.Members.Where(m =>
			{
				if (m.Kind() != SyntaxKind.ConstructorDeclaration) return false;
				if (!constructors.Any(m.IsSignatureEqual)) return true;

				Console.WriteLine($"Constructor is already present:\n{m.ToString()}\n");
				return false;
			}));

			methods.AddRange(classExisting.Members.Where(m => m.Kind() == SyntaxKind.MethodDeclaration));
			methods.AddRange(classGenerated.Members.Where(m =>
			{
				if (m.Kind() != SyntaxKind.MethodDeclaration) return false;
				if (!methods.Any(m.IsSignatureEqual)) return true;

				Console.WriteLine($"Method is already present:\n{m.ToString()}\n");
				return false;
			}));

			SyntaxKind[] alreadyTreatedMembers =
			{
				SyntaxKind.FieldDeclaration,
				SyntaxKind.PropertyDeclaration,
				SyntaxKind.ConstructorDeclaration,
				SyntaxKind.MethodDeclaration,
			};

			others.AddRange(classExisting.Members.Where(m => !alreadyTreatedMembers.Contains(m.Kind())));
			others.AddRange(classGenerated.Members.Where(m =>
			{
				if (alreadyTreatedMembers.Contains(m.Kind())) return false;
				if (!others.Any(m2 => m2.IsEquivalentTo(m))) return true;

				Console.WriteLine($"Member is already present:\n{m.ToString()}\n");
				return false;
			}));

			// Apply collected members
			classExisting = classExisting
				.WithMembers(new SyntaxList<MemberDeclarationSyntax>(fields))
				.AddMembers(properties.ToArray())
				.AddMembers(constructors.ToArray())
				.AddMembers(methods.ToArray())
				.AddMembers(others.ToArray());

			// Add the new class to the namespace block
			namespaceExisting = namespaceExisting.WithMembers(new SyntaxList<MemberDeclarationSyntax>(classExisting));

			// Add the modified namespace to the root
			rootExisting = rootExisting.WithMembers(new SyntaxList<MemberDeclarationSyntax>(namespaceExisting));

			// Write the merged source code to the WORKINPROGRESS file
			File.WriteAllText(existing.FullPath, rootExisting.NormalizeWhitespace("\t", "\n").ToFullString());
		}
	}
}