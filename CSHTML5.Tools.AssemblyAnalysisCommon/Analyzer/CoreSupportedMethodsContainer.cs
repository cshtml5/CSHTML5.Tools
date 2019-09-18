using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer
{
    public class CoreSupportedMethodsContainer
    {
        string _coreAssemblyFolder;
        string[] _coreAssembliesPaths;

        public CoreSupportedMethodsContainer(string coreAssemblyFolder)
        {
            _coreAssemblyFolder = coreAssemblyFolder;
            _coreAssembliesPaths = new string[]
            {
                Path.Combine(_coreAssemblyFolder, @"SLMigration.CSharpXamlForHtml5.dll"),
                Path.Combine(_coreAssemblyFolder, @"SLMigration.CSharpXamlForHtml5.System.dll.dll"),
                Path.Combine(_coreAssemblyFolder, @"SLMigration.CSharpXamlForHtml5.System.Runtime.Serialization.dll.dll"),
                Path.Combine(_coreAssemblyFolder, @"SLMigration.CSharpXamlForHtml5.System.ServiceModel.dll.dll"),
                Path.Combine(_coreAssemblyFolder, @"SLMigration.CSharpXamlForHtml5.System.Xaml.dll.dll"),
                Path.Combine(_coreAssemblyFolder, @"SLMigration.CSharpXamlForHtml5.System.Xml.dll.dll"),
            };
            Initialize();
        }

        Dictionary<string, Dictionary<string, CoreSupportedMethodTypeItem>> _items = new Dictionary<string, Dictionary<string, CoreSupportedMethodTypeItem>>();
        Dictionary<string, HashSet<string>> _namespacesMap = new Dictionary<string, HashSet<string>>();

        void Add(string @namespace, string typeName, string baseTypeNamespace, string baseType, string methodName)
        {
            if (_items.ContainsKey(@namespace))
            {
                if (_items[@namespace].ContainsKey(typeName))
                {
                    if(methodName != null)
                    {
                        _items[@namespace][typeName].Add(methodName);
                    }
                }
                else
                {
                    CoreSupportedMethodTypeItem item = new CoreSupportedMethodTypeItem(typeName, baseType, @namespace, baseTypeNamespace);
                    if(methodName != null)
                    {
                        item.Add(methodName);
                    }
                    _items[@namespace].Add(typeName, item);
                }
            }
            else
            {
                Dictionary<string, CoreSupportedMethodTypeItem> methods = new Dictionary<string, CoreSupportedMethodTypeItem>();
                CoreSupportedMethodTypeItem item = new CoreSupportedMethodTypeItem(typeName, baseType, @namespace, baseTypeNamespace);
                if (methodName != null)
                {
                    item.Add(methodName);
                }
                methods.Add(typeName, item);
                _items.Add(@namespace, methods);
            }
        }

        internal string ResolveNamespaceName(string @namespace, string typeName)
        {
            if (string.IsNullOrEmpty(@namespace))
            {
                return @namespace;
            }
            else
            {
                if (_namespacesMap.ContainsKey(@namespace))
                {
                    foreach (string ns in _namespacesMap[@namespace])
                    {
                        if (_items.ContainsKey(ns))
                        {
                            foreach (string type in _items[ns].Keys)
                            {
                                if (type == typeName)
                                {
                                    return ns;
                                }
                            }
                        }
                    }
                }
                return @namespace;
            }
        }

        private void FillMapOfXmlNsToCsNs(AssemblyDefinition assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            if (_namespacesMap == null)
            {
                _namespacesMap = new Dictionary<string, HashSet<string>>();
            }

            if (assembly.HasCustomAttributes)
            {
                foreach (var attribute in assembly.CustomAttributes)
                {
                    if (attribute.AttributeType.Name == "XmlnsDefinitionAttribute")
                    {
                        if (!_namespacesMap.ContainsKey(attribute.ConstructorArguments[0].Value.ToString()))
                        {
                            _namespacesMap.Add(attribute.ConstructorArguments[0].Value.ToString(), new HashSet<string>());
                        }
                        _namespacesMap[attribute.ConstructorArguments[0].Value.ToString()].Add(attribute.ConstructorArguments[1].Value.ToString());
                    }
                }
            }
        }

        void Initialize()
        {
            foreach (string assembly in _coreAssembliesPaths)
            {
                AssemblyDefinition coreAssembly = CompatibilityAnalyzer.LoadAssembly(assembly);
                FillMapOfXmlNsToCsNs(coreAssembly);
                //now we go through all the types of this assembly and remember all their public methods:
                foreach (TypeDefinition type in AnalyzeHelper.GetAllTypesDefinedInAssembly(coreAssembly))
                {
                    bool hasPublicMethods = false;
                    string baseTypeName = type.BaseType != null ? type.BaseType.Name : null;
                    string baseTypeNamespace = type.BaseType != null ? type.BaseType.Namespace : null;
                    foreach (MethodDefinition method in AnalyzeHelper.GetAllMethodsDefinedInType(type))
                    {
                        if (method.IsPublic || method.IsFamily) //note: we only add the public methods because those are the only ones the user can use.
                        {
                            hasPublicMethods = true;
                            Add(type.Namespace, type.Name, baseTypeNamespace, baseTypeName, method.Name);
                        }
                    }
                    if (!hasPublicMethods)
                    {
                        Add(type.Namespace, type.Name, baseTypeNamespace, baseTypeName, null);
                    }
                }
            }
        }

        private bool CheckIfTypeIsSupportedWithoutKnowingNamespace(string typeName)
        {
            foreach (Dictionary<string, CoreSupportedMethodTypeItem> @namespace in _items.Values)
            {
                if (@namespace.ContainsKey(typeName))
                {
                    return true;
                }
            }
            return false;
        }

        public bool ContainsType(string typeName, string @namespace)
        {
            if (String.IsNullOrEmpty(@namespace))
            {
                return CheckIfTypeIsSupportedWithoutKnowingNamespace(typeName);
            }
            else
            {
                string fixedNamespace = ResolveNamespaceName(@namespace, typeName);
                if (_items.ContainsKey(fixedNamespace))
                {
                    return _items[fixedNamespace].ContainsKey(typeName);
                }
                else
                {
                    return false;
                }
            }
        }

        public bool Contains(string methodFullName)
        {
            //the methodFullName is Type.Method for now
            int dotIndex = methodFullName.LastIndexOf('.');
            string fullTypeName = methodFullName.Substring(0, dotIndex);
            string methodName = methodFullName.Substring(dotIndex + 1);
            dotIndex = fullTypeName.LastIndexOf('.');
            string typeName = fullTypeName.Substring(dotIndex + 1);
            string @namespace = fullTypeName.Substring(0, dotIndex);
            if (String.IsNullOrEmpty(@namespace))
            {
                return CheckIfMethodIsSupportedWithoutKnowingNamespace(typeName, methodName);
            }
            else
            {
                while (typeName != null)
                {
                    if (_items.ContainsKey(@namespace))
                    {
                        if (_items.ContainsKey(typeName))
                        {
                            CoreSupportedMethodTypeItem item = _items[@namespace][typeName];
                            if (item.Contains(methodName))
                            {
                                return true;
                            }
                            else
                            {
                                typeName = item._baseTypeName;
                                @namespace = item._baseTypeNamespace;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;
            }
        }

        private bool CheckIfMethodIsSupportedWithoutKnowingNamespace(string typeName, string methodName)
        {
            foreach (Dictionary<string, CoreSupportedMethodTypeItem> @namespace in _items.Values)
            {
                if (@namespace.ContainsKey(typeName))
                {
                    string baseTypeName = typeName;
                    string namespaceName = @namespace[typeName]._namespace;
                    while (baseTypeName != null)
                    {
                        if (_items[namespaceName].ContainsKey(baseTypeName))
                        {
                            CoreSupportedMethodTypeItem item = _items[namespaceName][baseTypeName];
                            if (item.Contains(methodName))
                            {
                                return true;
                            }
                            else
                            {
                                baseTypeName = item._baseTypeName;
                                namespaceName = item._baseTypeNamespace;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            return false;
        }

        public bool Contains(string @namespace, string typeName, string methodName)
        {
            if (string.IsNullOrEmpty(methodName))
            {
                return ContainsType(typeName, @namespace);
            }
            else
            {
                if (string.IsNullOrEmpty(@namespace))
                {
                    return CheckIfMethodIsSupportedWithoutKnowingNamespace(typeName, methodName);
                }
                else
                {
                    string fixedNamespace = ResolveNamespaceName(@namespace, typeName);
                    string baseTypeName = typeName;
                    string baseTypeNamespace = fixedNamespace;
                    while (baseTypeName != null)
                    {
                        if (_items.ContainsKey(baseTypeNamespace))
                        {
                            if (_items[baseTypeNamespace].ContainsKey(baseTypeName))
                            {
                                CoreSupportedMethodTypeItem item = _items[baseTypeNamespace][baseTypeName];
                                if (item.Contains(methodName))
                                {
                                    return true;
                                }
                                else
                                {
                                    baseTypeName = item._baseTypeName;
                                    baseTypeNamespace = item._baseTypeNamespace;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return false;
                }
            }
        }
    }
}
