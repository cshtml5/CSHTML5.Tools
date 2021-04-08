using DotNetForHtml5.PrivateTools.AssemblyAnalysisCommon;
using DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer;
using Mono.Cecil;
using StubGenerator.Common.Analyzer;
using StubGenerator.Common.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StubGenerator.Common
{
    public class StubGenerator
    {
        // Assemblies to analyze
        private string[] _inputAssemblies;

        // Path of the folder containing the assemblies where the stuff (type + methods) we are looking for is defined
        private string _referencedAssembliesFolderPath;

        // List of Assemblies to look into for unsupported methods and types
        private List<string> _referencedAssembliesToLookInto;

        private OutputOptions _outputOptions;

        private List<ModuleDefinition> _modules;

        private AssemblyAnalyzer _assemblyAnalyzer;

        private Dictionary<string, Dictionary<string, HashSet<string>>> _unsupportedMethodsInfo;

        /// <summary>
        /// Retrieve all methods and types used in the _inputAssemblies and organize them in a Dictionary 
        /// </summary>
        private void GetUnsupportedMethods()
        {
            ILogger logger = new LoggerThatAggregatesAllErrors();
            List<UnsupportedMethodInfo> unsupportedMethodInfos = new List<UnsupportedMethodInfo>();
            CoreSupportedMethodsContainer coreSupportedMethods = new CoreSupportedMethodsContainer(Configuration.SLMigrationCoreAssemblyFolderPath);

            foreach (string filename in _inputAssemblies)
            {
                CompatibilityAnalyzer.Analyze(
                    filename,
                    logger,
                    unsupportedMethodInfos,
                    coreSupportedMethods,
                    _inputAssemblies,
                    Configuration.UrlNamespacesThatBelongToUserCode,
                    new HashSet<string>(),
                    Configuration.ExcludedFiles,
                    Configuration.supportedElementsPath,
                    Configuration.mscorlibFolderPath,
                    Configuration.sdkFolderPath,
                    "",
                    skipTypesWhereNoMethodIsActuallyCalled: false,
                    addBothPropertyAndEventWhenNotFound: true,
                    additionalFolderWhereToResolveAssemblies: Configuration.ReferencedAssembliesFolderPath);
            }
            if (_unsupportedMethodsInfo == null)
            {
                _unsupportedMethodsInfo = new Dictionary<string, Dictionary<string, HashSet<string>>>();
            }
            HashSet<MethodInfo> unsupportedMethods = new HashSet<MethodInfo>();
            foreach(UnsupportedMethodInfo method in unsupportedMethodInfos)
            {
                unsupportedMethods.Add(new MethodInfo(method));
            }
            foreach (MethodInfo method in unsupportedMethods)
            {
                Tuple<string, string, string> methodInfo = GetMethodMainInfos(method);
                MethodInfo _method = new MethodInfo(methodInfo.Item1, methodInfo.Item2, methodInfo.Item3, method.NeedToBeCheckedBecauseOfInheritance);
                if (method.NeedToBeCheckedBecauseOfInheritance)
                {
                    _method = GetMethodInfoResolvingInheritance(_method.AssemblyName, _method.TypeName, _method.MethodName);
                }
                if (!_unsupportedMethodsInfo.ContainsKey(_method.AssemblyName))
                {
                    HashSet<string> methodsSet = new HashSet<string>();
                    if (_method.MethodName != "")
                    {
                        methodsSet.Add(_method.MethodName);
                    }
                    Dictionary<string, HashSet<string>> currentTypesDictionary = new Dictionary<string, HashSet<string>>
                    {
                        { _method.TypeName, methodsSet }
                    };
                    _unsupportedMethodsInfo.Add(_method.AssemblyName, currentTypesDictionary);
                }
                else
                {
                    if (!_unsupportedMethodsInfo[_method.AssemblyName].ContainsKey(_method.TypeName))
                    {
                        HashSet<string> methodsSet = new HashSet<string>();
                        if (_method.MethodName != "")
                        {
                            methodsSet.Add(_method.MethodName);
                        }
                        _unsupportedMethodsInfo[_method.AssemblyName].Add(_method.TypeName, methodsSet);
                    }
                    else
                    {
                        if(_method.MethodName != "")
                        {
                            _unsupportedMethodsInfo[_method.AssemblyName][_method.TypeName].Add(_method.MethodName);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resolve inheritance issues
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="typeName"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private MethodInfo GetMethodInfoResolvingInheritance(string assemblyName, string typeName, string methodName)
        {
            TypeDefinition currentType = null;
            int i = 0;
            bool keepSearching = true;
            while(i < _modules.Count && keepSearching)
            {
                ModuleDefinition module = _modules[i];
                if(module.Name.Replace(".dll","") == assemblyName)
                {
                    if (module.HasTypes)
                    {
                        int j = 0;
                        while(j < module.Types.Count && keepSearching)
                        {
                            TypeDefinition type = module.Types[j];
                            if (type.Name == typeName)
                            {
                                currentType = type;
                                keepSearching = false;
                            }
                            j++;
                        }
                    }
                    keepSearching = false;
                }
                i++;
            }
            while(currentType != null)
            {
                if (currentType.HasMethods)
                {
                    foreach(MethodDefinition method in currentType.Methods)
                    {
                        if(method.Name == methodName)
                        {
                            return new MethodInfo(currentType.Scope.Name.Replace(".dll", ""), currentType.Name, methodName, true);
                        }
                    }
                }
                currentType = AnalysisUtils.GetTypeDefinitionFromTypeReference(currentType.BaseType, _modules);
            }
            return new MethodInfo(assemblyName, typeName, methodName, true);
        }

        /// <summary>
        /// Retrieve data in UnsupportedMethodInfo allowing to identify a method/type
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns> A Tuple of 3 strings. The first one is the assembly name, the second one the type name and the third is the method name</returns>
        private Tuple<string, string, string> GetMethodMainInfos(MethodInfo methodInfo)
        {
            string assemblyName = "";
            string className = methodInfo.TypeName;
            string methodName = methodInfo.MethodName;
            //if the method or type has been found in a xaml file, the assembly could be an url, so we have to find what his actual assembly is 
            if (methodInfo.AssemblyName.StartsWith("http://"))
            {
                int i = 0, j = 0;
                bool notFound = true;
                while (i < _modules.Count && notFound)
                {
                    j = 0;
                    while (j < _modules[i].Types.Count && notFound)
                    {
                        if (_modules[i].Types[j].Name == className)
                        {
                            assemblyName = _modules[i].Types[j].Module.Assembly.Name.Name;
                            notFound = false;
                        }
                        j++;
                    }
                    i++;
                }
                if (notFound)
                {
                    assemblyName = methodInfo.AssemblyName;
                }
            }
            else
            {
                assemblyName = methodInfo.AssemblyName;
            }
            return new Tuple<string, string, string>(assemblyName, className, methodName);
        }

        /// <summary>
        /// Initialyze the StubGenerator
        /// </summary>
        private void Init()
        {
            _outputOptions = Configuration.OutputOptions;
            _inputAssemblies = GetAssemblies(Configuration.assembliesToAnalyzePath, true).ToArray<string>();
            _referencedAssembliesFolderPath = Configuration.ReferencedAssembliesFolderPath;
            _referencedAssembliesToLookInto = GetAssemblies(_referencedAssembliesFolderPath);
            GetModules();
            AnalysisUtils.SetModules(_modules);
            GetUnsupportedMethods();
            AddUndetectedMethodToUnsupportedMethods(Configuration.MethodsToAddManuallyBecauseTheyAreUndetected);
            _assemblyAnalyzer = new AssemblyAnalyzer(
                Configuration.SLMigrationCoreAssemblyFolderPath, 
                _unsupportedMethodsInfo,
                _modules, 
                _outputOptions);
        }

        /// <summary>
        /// Main method of the generator. Look in every referenced assemblies to find the unsupported methods and types
        /// </summary>
        public void Run()
        {
            Init();
            foreach (string assembly in _referencedAssembliesToLookInto)
            {
                Dictionary<string, HashSet<string>> unsupportedMethods;
                _unsupportedMethodsInfo.TryGetValue(assembly, out unsupportedMethods);
                if (unsupportedMethods != null)
                {
                    AssemblyDefinition assemblyToLookInto = CompatibilityAnalyzer.LoadAssembly(Path.Combine(_referencedAssembliesFolderPath, assembly + ".dll"), Configuration.mscorlibFolderPath);
                    _assemblyAnalyzer.Set(assemblyToLookInto, unsupportedMethods);
                    _assemblyAnalyzer.Run();
                }
            }
            GenerateAddtionalUnsupportedTypes();
        }

        /// <summary>
        /// Generate types that were not detected as unsupported at first but were detected during the execution of the program. 
        /// This type are most of the time empty and usefull for inheritance only.
        /// </summary>
        private void GenerateAddtionalUnsupportedTypes()
        {
            _assemblyAnalyzer.ClassAnalyzer.GenerateAddtionalUnsupportedTypes();
        }

        /// <summary>
        /// Load all modules. We need the modules because in same cases, we can't get a TypeDefinition from a TypeReference with the .Resolve() method
        /// and need to check in the modules to find it.
        /// </summary>
        private void GetModules()
        {
            _modules = new List<ModuleDefinition>();
            foreach (string assemblyToLookInto in _referencedAssembliesToLookInto)
            {
                _modules.Add(AssemblyDefinition.ReadAssembly(Path.Combine(_referencedAssembliesFolderPath, assemblyToLookInto + ".dll")).MainModule);
            }
        }

        /// <summary>
        /// Get paths of every referenced assemblies
        /// </summary>
        /// <param name="folderPath">path of the folder where referenced assemblies are located</param>
        /// <returns></returns>
        private List<string> GetAssemblies(string folderPath, bool getFullName = false)
        {
            List<string> assemblies = new List<string>();
            DirectoryInfo directory = new DirectoryInfo(folderPath);
            FileInfo[] files = directory.GetFiles("*.dll");
            foreach (FileInfo file in files)
            {
                if (getFullName)
                {
                    assemblies.Add(file.FullName);
                }
                else
                {
                    assemblies.Add(Path.GetFileNameWithoutExtension(file.Name));
                }
            }
            return assemblies;
        }

        private void AddMethodToUnsupportedMethods(string assemblyName, string typeName, string methodName)
        {
            if (_unsupportedMethodsInfo.ContainsKey(assemblyName))
            {
                if (_unsupportedMethodsInfo[assemblyName].ContainsKey(typeName))
                {
                    if (!String.IsNullOrEmpty(methodName))
                    {
                       _unsupportedMethodsInfo[assemblyName][typeName].Add(methodName);
                    }
                }
                else
                {
                    if (String.IsNullOrEmpty(methodName))
                    {
                        _unsupportedMethodsInfo[assemblyName].Add(typeName, new HashSet<string>());
                    }
                    else
                    {
                        _unsupportedMethodsInfo[assemblyName].Add(typeName, new HashSet<string>() { methodName });
                    }
                }
            }
            else
            {
                Dictionary<string, HashSet<string>> typeMethodsPairs = new Dictionary<string, HashSet<string>>();
                if (String.IsNullOrEmpty(methodName))
                {
                    typeMethodsPairs.Add(typeName, new HashSet<string>());
                }
                else
                {
                    typeMethodsPairs.Add(typeName, new HashSet<string>() { methodName });
                }
                _unsupportedMethodsInfo.Add(assemblyName, typeMethodsPairs);
            }
        }

        /// <summary>
        /// Add method that are not detected by the AssemblyCompatibilityAnalyzer to the unsupported methods list to make sure they are generated
        /// </summary>
        /// <param name="undetectedMethods"></param>
        private void AddUndetectedMethodToUnsupportedMethods(Tuple<string, string, string>[] undetectedMethods)
        {
            foreach(Tuple<string, string, string> method in undetectedMethods)
            {
                AddMethodToUnsupportedMethods(method.Item1, method.Item2, method.Item3);
            }
        }
    }
}
