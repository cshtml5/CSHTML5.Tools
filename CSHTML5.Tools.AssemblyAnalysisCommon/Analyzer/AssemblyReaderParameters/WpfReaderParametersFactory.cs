using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace CSHTML5.Tools.AssemblyAnalysisCommon.Analyzer.AssemblyReaderParameters
{
    public class WpfReaderParametersFactory : IReaderParametersFactory
    {
        private readonly string _otherFoldersPath;
        private readonly string _additionalFolderWhereToResolveAssemblies;

        public WpfReaderParametersFactory(string otherFoldersPath = null,
            string additionalFolderWhereToResolveAssemblies = null)
        {
            _otherFoldersPath = otherFoldersPath;
            _additionalFolderWhereToResolveAssemblies = additionalFolderWhereToResolveAssemblies;
        }

        private static TargetDotNetFrameworkVersion GetFrameworkVersion(string path)
        {
            var assembly = Assembly.LoadFrom(path);


            var targetFrameAttribute = assembly.GetCustomAttributes(true)
                .OfType<TargetFrameworkAttribute>().FirstOrDefault();

            if (targetFrameAttribute != null &&
                Enum.TryParse("Version" + targetFrameAttribute.FrameworkName.Replace(".NETFramework,Version=v", "").Replace(".", ""), out TargetDotNetFrameworkVersion resultVersion))
            {
                return resultVersion;
            }

            //Let's guess 3.0 as a first version of the framework with WPF.
            //Also, it is possible version 3.5. We can do some magic to determine the proper one
            return TargetDotNetFrameworkVersion.Version30;
        }

        public ReaderParameters GetReaderParameters(string path)
        {
            var resolver = new DefaultAssemblyResolver();

            // Tell the resolver to look for referenced assemblies in the same folder where the loaded assembly is located:
            string containingFolderPath = Path.GetDirectoryName(path);
            resolver.AddSearchDirectory(containingFolderPath);

            // Tell the resolver to look for other assemblies in the "otherFoldersPath" directory:
            if (!string.IsNullOrWhiteSpace(_otherFoldersPath))
            {
                foreach (var p in _otherFoldersPath.Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(p))
                    {
                        resolver.AddSearchDirectory(p);
                    }
                }
            }

            // Tell the resolver to look for referenced assemblies in the specified additional location:
            if (!string.IsNullOrEmpty(_additionalFolderWhereToResolveAssemblies))
                resolver.AddSearchDirectory(_additionalFolderWhereToResolveAssemblies);

            var frameworkReferenceAssemblies = ToolLocationHelper.GetPathToDotNetFrameworkReferenceAssemblies(
                GetFrameworkVersion(path));

            // Tell the resolver to look for referenced assemblies in the Framework location:
            resolver.AddSearchDirectory(frameworkReferenceAssemblies);

            return new ReaderParameters
            {
                ReadingMode = ReadingMode.Immediate,
                ReadSymbols = false,
                AssemblyResolver = resolver
            };
        }
    }
}
