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
#if TESTING
			string generatedNamespacesRoot = @"C:\Projects\2019\CSHTML5.Tools\toMerge\Generated";
			string CSHTML5RootPath = @"C:\Projects\2019\CSHTML5.Tools\toMerge\Dummy\CSHTML5";
#else
			if (args.Length < 2)
			{
				Console.WriteLine("\nUsage: \nCSHTML5.Tools.StubMerger.exe generatedNamespacesRoot CSHTML5RootPath\n");
				return;
			}

			string generatedNamespacesRoot = args[0];
			string CSHTML5RootPath = args[1];
#endif
			
			string includeLogPath = Path.Combine(Directory.GetCurrentDirectory(), "copy_to_csproj.log");

			string CSHTML5NamespacesRoot = Path.Combine(CSHTML5RootPath, @"src\CSHTML5.Runtime");

			Run(generatedNamespacesRoot, CSHTML5NamespacesRoot, includeLogPath);
		}

		private static void Run(string generatedNamespacesRoot, string CSHTML5NamespacesRoot, string includeLogPath)
		{
			HashSet<Namespace> generatedNamespaces = Namespace.GetGeneratedNamespaces(generatedNamespacesRoot);
			
			HashSet<string> includes = new HashSet<string>();

			// For each generated namespace
			foreach (Namespace generatedNamespace in generatedNamespaces)
			{
				// Retrieve the CSHTML5-equivalent of the generated namespace
				// If the generate namespace starts with System.Windows and a Windows.UI.Xaml-equivalent namespace exists in CSHTML5, work with this one
				bool needsRemap = generatedNamespace.Name.StartsWith("System.Windows") &&
				                  Namespace.Exists(CSHTML5NamespacesRoot, generatedNamespace.Name.Replace("System.Windows", "Windows.UI.Xaml"));
				
				Namespace existingNamespace;
				if (needsRemap)
				{
					existingNamespace = Namespace.GetOrCreateExistingNamespace(
						CSHTML5NamespacesRoot,
						generatedNamespace.Name.Replace("System.Windows", "Windows.UI.Xaml"));
				}
				else
				{
					existingNamespace = Namespace.GetOrCreateExistingNamespace(CSHTML5NamespacesRoot, generatedNamespace.Name);
				}

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
						if (needsRemap)
						{
							Merger.FixSystemWindowsNamespace(stubClassPart, existingNamespace);
						}
						else
						{
							Merger.CopyWithWIP(stubClassPart, existingNamespace);
						}

						includes.Add($"<Compile Include=\"{Path.Combine(existingNamespace.Name, "WORKINPROGRESS", stubClassPart.FileName)}\" />");
					}
				}
			}
			
			File.WriteAllLines(includeLogPath, includes);
		}
	}
}