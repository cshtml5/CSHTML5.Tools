using DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer;
using Mono.Cecil;
using StubGenerator.Common.Options;
using System.Collections.Generic;

namespace StubGenerator.Common.Analyzer
{
    public class AssemblyAnalyzer
    {
        private OutputOptions _outputOptions;
        private Dictionary<string, Dictionary<string, HashSet<string>>> _unsupportedMethods { get; set; }
        private Dictionary<string, HashSet<string>> _unsupportedMethodsInCurrentAssembly { get; set; }
        private List<ModuleDefinition> _modules;

        public ClassAnalyzer ClassAnalyzer { get; set; }

        public AssemblyDefinition Assembly { get; set; }

        /// <param name="unsupportedMethods"></param>
        /// <param name="modules"></param>
        /// <param name="outputOptions"></param>
        internal AssemblyAnalyzer(Dictionary<string, Dictionary<string, HashSet<string>>> unsupportedMethods, AnalyzeHelper analyzeHelper, List<ModuleDefinition> modules, OutputOptions outputOptions)
        {
            _outputOptions = outputOptions;

            _unsupportedMethods = unsupportedMethods;
            _modules = modules;
            ClassAnalyzer = new ClassAnalyzer(_unsupportedMethods, analyzeHelper, _modules, _outputOptions);
        }

        private bool CanWorkOnAssembly()
        {
            return Assembly != null;
        }

        /// <summary>
        /// Set the AssemblyAnalyzer
        /// </summary>
        /// <param name="assembly"> Assembly to analyze</param>
        /// <param name="unsupportedMethods"> methods to look for in the assembly</param>
        public void Set(AssemblyDefinition assembly, Dictionary<string, HashSet<string>> unsupportedMethods)
        {
            Assembly = assembly;
            _unsupportedMethodsInCurrentAssembly = unsupportedMethods;
        }

        /// <summary>
        /// Check every type in the current Assembly.
        /// </summary>
        public void Run()
        {
            foreach (TypeDefinition type in Assembly.MainModule.Types)
            {
                HashSet<string> unsupportedMethodsInCurrentType;
                _unsupportedMethodsInCurrentAssembly.TryGetValue(type.Name, out unsupportedMethodsInCurrentType);
                //we want to check the type only if we know there is unsupported stuff in it.
                if (unsupportedMethodsInCurrentType != null)
                {
                    ClassAnalyzer.Set(type, unsupportedMethodsInCurrentType);//, 0);
                    ClassAnalyzer.Run();
                }
            }
        }
    }
}
