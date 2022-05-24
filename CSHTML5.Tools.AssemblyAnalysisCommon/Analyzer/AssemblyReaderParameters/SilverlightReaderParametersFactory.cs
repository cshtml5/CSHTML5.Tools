using System.IO;
using Mono.Cecil;

namespace CSHTML5.Tools.AssemblyAnalysisCommon.Analyzer.AssemblyReaderParameters
{
    public class SilverlightReaderParametersFactory : IReaderParametersFactory
    {
        private readonly string _mscorlibFolderPath;
        private readonly string _sdkFolderPath;
        private readonly string _otherFoldersPath;
        private readonly string _additionalFolderWhereToResolveAssemblies;

        public SilverlightReaderParametersFactory(string mscorlibFolderPath = null,
            string sdkFolderPath = null, string otherFoldersPath = null,
            string additionalFolderWhereToResolveAssemblies = null)
        {
            _mscorlibFolderPath = mscorlibFolderPath;
            _sdkFolderPath = sdkFolderPath;
            _otherFoldersPath = otherFoldersPath;
            _additionalFolderWhereToResolveAssemblies = additionalFolderWhereToResolveAssemblies;
        }

        public ReaderParameters GetReaderParameters(string path)
        {
            var resolver = new DefaultAssemblyResolver();

            // Tell the resolver to look for referenced assemblies in the same folder where the loaded assembly is located:
            string containingFolderPath = Path.GetDirectoryName(path);
            resolver.AddSearchDirectory(containingFolderPath);

            // Tell the resolver to look for referenced Mscorlib and other framework assemblies in the "mscorlibFolderPath" directory:
            if (!string.IsNullOrEmpty(_mscorlibFolderPath))
                resolver.AddSearchDirectory(_mscorlibFolderPath);

            // Tell the resolver to look for other framework assemblies in the "sdkFolderPath" directory:
            if (!string.IsNullOrEmpty(_sdkFolderPath))
                resolver.AddSearchDirectory(_sdkFolderPath);

            // Tell the resolver to look for other assemblies in the "otherFoldersPath" directory:
            if (!string.IsNullOrWhiteSpace(_otherFoldersPath))
            {
                foreach (string p in _otherFoldersPath.Split(','))
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

            return new ReaderParameters
            {
                ReadingMode = ReadingMode.Immediate,
                ReadSymbols = false,
                AssemblyResolver = resolver
            };
        }
    }
}
