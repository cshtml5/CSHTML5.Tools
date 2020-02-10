using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSHTML5.Tools.StubMerger
{
	internal static class Program
	{
		private static HashSet<string> _validDirectoriesPrefixes = new HashSet<string> {"System", "Windows"};

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
			HashSet<Namespace> existingNamespaces = Namespace.GetExistingNamespaces(CSHTML5NamespacesRoot);
			
			// For each generated namespace
			foreach (Namespace generatedNamespace in generatedNamespaces)
			{
				// Retrieve the CSHTML5-equivalent of the generated namespace
				Namespace existingNamespace = existingNamespaces.First(ns => ns.Name == generatedNamespace.Name);
				
				// If CSHTML5 doesn't have this namespace yet, create it
				if (existingNamespace == null)
				{
					existingNamespace = CreateNamespace(CSHTML5NamespacesRoot, generatedNamespace);
					existingNamespaces.Add(existingNamespace);
				}

				// Make sure the WORKINPROGRESS folder exists in the CSHTML5 namespace, and create it if it doesn't.
				Directory.CreateDirectory(Path.Combine(existingNamespace.FullPath, "WORKINPROGRESS"));

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
							Path.Combine(generatedNamespacesRoot, generatedNamespace.Name, stubClassPart.FileName),
							Path.Combine(CSHTML5NamespacesRoot, existingNamespace.Name, "WORKINPROGRESS", stubClassPart.FileName)
							);
					}
				}
			}
		}

		private static void MergeStubs(ClassPart class1, ClassPart class2)
		{
			//TODO
		}

		private static Namespace CreateNamespace(string namespaceRoot, Namespace namespaces)
		{
			string namespacePath = Path.Combine(namespaceRoot, namespaces.Name);
			Directory.CreateDirectory(namespacePath);
			return new Namespace(namespacePath, namespaceRoot);
		}
	}
}