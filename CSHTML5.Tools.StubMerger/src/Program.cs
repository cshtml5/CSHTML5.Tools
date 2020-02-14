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

						Merger.MergeFiles(stubClassPart, existingStubs.First());
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
	}
}