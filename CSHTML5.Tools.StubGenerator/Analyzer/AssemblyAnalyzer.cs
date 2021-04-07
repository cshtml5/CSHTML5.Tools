using DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer;
using Mono.Cecil;
using StubGenerator.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StubGenerator.Common.Analyzer
{
    public class AssemblyAnalyzer
    {
        private OutputOptions _outputOptions;
        private Dictionary<string, Dictionary<string, HashSet<string>>> _unsupportedMethods { get; set; }
        private Dictionary<string, HashSet<string>> _unsupportedMethodsInCurrentAssembly { get; set; }
        private List<ModuleDefinition> _modules;
        private bool _isInitialized = false;

        public ClassAnalyzer ClassAnalyzer { get; set; }

        public AssemblyDefinition Assembly { get; set; }

        internal AssemblyAnalyzer(string coreASsemblyFolder, Dictionary<string, Dictionary<string, HashSet<string>>> unsupportedMethods, List<ModuleDefinition> modules, OutputOptions outputOptions)
        {
            Init(coreASsemblyFolder, unsupportedMethods, modules, outputOptions);
        }

        /// <summary>
        /// Initialize the AssemblyAnalyzer. Must be call once (and only once).
        /// </summary>
        /// <param name="unsupportedMethods"></param>
        /// <param name="modules"></param>
        /// <param name="outputOptions"></param>
        private void Init(string coreASsemblyFolder, Dictionary<string, Dictionary<string, HashSet<string>>> unsupportedMethods, List<ModuleDefinition> modules, OutputOptions outputOptions = null)
        {
            if (!_isInitialized)
            {
                if (outputOptions == null)
                {
                    _outputOptions = new OutputOptions();
                }
                else
                {
                    _outputOptions = outputOptions;
                }

                _unsupportedMethods = unsupportedMethods;
                _modules = modules;
                ClassAnalyzer = new ClassAnalyzer(coreASsemblyFolder, _unsupportedMethods, _modules, _outputOptions);

                _isInitialized = true;
            }
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
            if (!_isInitialized)
            {
                throw new Exception("AssemblyAnalyzer must be initialized. Please Call Init() first.");
            }

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
