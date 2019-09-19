using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using StubGenerator.Common.Options;
using DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer;
using DotNetForHtml5.PrivateTools.AssemblyAnalysisCommon;
using System.Reflection;
using StubGenerator.Common.Builder;

namespace StubGenerator.Common.Analyzer
{
    public class ClassAnalyzer : IElementAnalyzer<TypeDefinition>//, IElementWriter<TypeDefinition>
    {
        public static AnalyzeHelper analyzeHelpher = new AnalyzeHelper();
        private OutputOptions _outputOptions;
        private Dictionary<string, Dictionary<string, HashSet<string>>> _unsupportedMethods;
        private HashSet<string> _unsupportedMethodsInCurrentType;
        private List<ModuleDefinition> _modules;
        //private int _indentation;
        private TypeBuilder _typeBuilder;

        // Implemented types
        private HashSet<string> _alreadyImplementedTypes;

        // interfaces to implement
        internal HashSet<TypeReference> _implementedInterfaces;

        // DependencyProperties to implement
        private HashSet<string> _dependencyProperties;

        // Namespaces used in the current type
        private HashSet<string> _usings;

        // Custom attributes
        private HashSet<string> _customAttributes;

        private bool _hasAnIndexer = false;

        private bool _isInitialized = false;

        internal Dictionary<TypeReference, HashSet<string>> _additionalTypesToImplement;

        internal string _assemblyName;

        public TypeDefinition Element { get; set; }

        private MethodAnalyzer MethodAnalyzer { get; set; }

        internal ClassAnalyzer(Dictionary<string, Dictionary<string, HashSet<string>>> unsupportedMethods, List<ModuleDefinition> modules, OutputOptions outputOptions = null)
        {
            Init(unsupportedMethods, modules, outputOptions);
        }

        /// <summary>
        /// Initialyze the ClassAnalyzer. Must be call once.
        /// </summary>
        /// <param name="unsupportedMethods"></param>
        /// <param name="modules"></param>
        /// <param name="outputOptions"></param>
        private void Init(Dictionary<string, Dictionary<string, HashSet<string>>> unsupportedMethods, List<ModuleDefinition> modules, OutputOptions outputOptions = null)
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
                if (!analyzeHelpher._initialized)
                {
                    CoreSupportedMethodsContainer coreSupportedMethods = new CoreSupportedMethodsContainer(System.IO.Path.Combine(AnalysisUtils.GetProgramFilesX86Path(), @"MSBuild\CSharpXamlForHtml5\InternalStuff\Compiler\SLMigration"));
                    analyzeHelpher.Initialize(coreSupportedMethods, Configuration.supportedElementsPath);
                }

                _implementedInterfaces = new HashSet<TypeReference>();
                _usings = new HashSet<string>();
                _customAttributes = new HashSet<string>();
                _alreadyImplementedTypes = new HashSet<string>();
                _dependencyProperties = new HashSet<string>();
                _additionalTypesToImplement = new Dictionary<TypeReference, HashSet<string>>();
                _unsupportedMethods = unsupportedMethods;
                _modules = modules;
                MethodAnalyzer = new MethodAnalyzer(_unsupportedMethods, _modules, this, _outputOptions);
                _typeBuilder = new TypeBuilder(_outputOptions, _modules);
                _isInitialized = true;
            }

        }

        /// <summary>
        /// Set the ClassAnalyzer with a new type and new methods to look for.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="unsupportedMethods"></param>
        public void Set(TypeDefinition type, HashSet<string> unsupportedMethods)
        {
            Element = type;
            _unsupportedMethodsInCurrentType = unsupportedMethods;
            _assemblyName = (type != null ? type.Scope.Name.Replace(".dll", "") : "");
            _implementedInterfaces.Clear();
            _usings.Clear();
            _customAttributes.Clear();
            _hasAnIndexer = false;
            _dependencyProperties.Clear();
        }

        /// <summary>
        /// Analyze the current type and save the type generated from it.
        /// </summary>
        public void Execute()
        {
            if (AnalysisUtils.IsTypeADelegate(Element))
            {
                GetUsingsForDelegate(Element);
            }
            else if(!Element.IsEnum)
            {
                AnalyzeParentAndInterfacesType(Element);
                if (Element.HasMethods)
                {
                    AddConstructorToUnsupportedMethod();
                    HashSet<string> methodsFromInterfaces = GetMethodsInheritedFromInterfaces(Element);
                    _unsupportedMethodsInCurrentType.UnionWith(methodsFromInterfaces);
                    UpdateUnsupportedMethodsInfo(methodsFromInterfaces);
                    _unsupportedMethodsInCurrentType.UnionWith(GetAbstractMethodsThatWouldntBeImlemented(Element));
                    GetCustomAttributes(_unsupportedMethodsInCurrentType);
                    MethodAnalyzer.SetForType(_unsupportedMethodsInCurrentType, _hasAnIndexer);
                    foreach (MethodDefinition method in Element.Methods)
                    {
                        MethodAnalyzer.SetForMethod(method);
                        MethodAnalyzer.Run();
                    }
                }
            }
            _typeBuilder.SetAndRun(Element, GetAdditionalCodeIfAny(_assemblyName, Element.Name), _implementedInterfaces, _hasAnIndexer, _assemblyName, 0);            
            //Used to make sure we don't implement twice the same type. A type could be generated once with a few methods and could be override afterward with less methods defined, but it can't be the other way.
            _alreadyImplementedTypes.Add(_assemblyName + '.' + Element.FullName);
        }

        public void Run()
        {
            if (!_isInitialized)
            {
                throw new Exception("ClassAnalyzer must be initialized. Please call Init() first.");
            }

            if (CanWorkOnElement())
            {
                Stack<TypeDefinition> typesToAnalyze = new Stack<TypeDefinition>();
                // We don't directly analyze the selected type because we want to start with its "oldest" parent.
                // Sometimes, we have to update our set of unsupported methods, so we need to start with the oldest parent to make sure we don't forget to implement a method from an interface or an abstract class which has been added during the execution of the program.
                do
                {
                    typesToAnalyze.Push(Element);
                    Element = AnalysisUtils.GetTypeDefinitionFromTypeReference(Element.BaseType, _modules);
                }
                while (Element != null && (Element.FullName != "System.Object" || Element.FullName != "System.ValueType") && CanWorkOnElement(Element));
                while (typesToAnalyze.Count > 0)
                {
                    TypeDefinition firstParent = typesToAnalyze.Pop();
                    string parentAssembly = firstParent.Scope.Name.Replace(".dll", "");
                    HashSet<string> unsupportedMethodsInParent;
                    if (_unsupportedMethods.ContainsKey(parentAssembly))
                    {
                        if (_unsupportedMethods[parentAssembly].TryGetValue(firstParent.Name, out unsupportedMethodsInParent))
                        {
                            Set(firstParent, unsupportedMethodsInParent);
                        }
                        else
                        {
                            Set(firstParent, new HashSet<string>());
                        }
                        Execute();
                    }
                }
            }
        }

        /// <summary>
        /// We want to add a constructor for each type if one is defined in the current type.
        /// </summary>
        private void AddConstructorToUnsupportedMethod()
        {
            _unsupportedMethodsInCurrentType.Add(".ctor");
            UpdateUnsupportedMethodsInfo(new HashSet<string>() { ".ctor" });
        }
        
        /// <summary>
        /// Add the ContentProperty to the list of properties that need to be generated.
        /// As the ContentProperty can be defined in child type when the type is abstract, we can't find the property every time.
        /// </summary>
        /// <returns>
        /// return true if the property has been found, false otherwise.
        /// </returns>
        private bool AddContentPropertyIfDefinedInCurrentType(TypeDefinition currentType, string methodName)
        {
            throw new NotImplementedException();
        }

        private void GetCustomAttributes(HashSet<string> unsupportedMethodsInCurrentType)
        {
            if (Element.HasCustomAttributes)
            {
                foreach (CustomAttribute customAttribute in Element.CustomAttributes)
                {
                    if (customAttribute.AttributeType.FullName == "System.Windows.Markup.ContentPropertyAttribute")
                    {
                        if (customAttribute.HasConstructorArguments)
                        {
                            string propertyName = customAttribute.ConstructorArguments[0].Value.ToString();
                            // we need to add this property anyway because it can be used in the xaml without ever mentionned 
                            // and it could also never be used in the c# code.
                            if (!unsupportedMethodsInCurrentType.Contains("get_" + propertyName) && !unsupportedMethodsInCurrentType.Contains("set_" + propertyName))
                            {
                                AddUsing(customAttribute.AttributeType.Namespace);
                                AddCustomAttribute(customAttribute, false); //false because not indexer
                            }
                        }
                    }
                    else if(customAttribute.AttributeType.FullName == "System.ComponentModel.TypeConverterAttribute")
                    {
                        AddCustomAttribute(customAttribute, false);
                        AddUsing("System.Windows.Markup");
                        AddUsing("DotNetForHtml5.Core");
                    }
                    else if (customAttribute.AttributeType.FullName == "System.Reflection.DefaultMemberAttribute")
                    {
                        if (customAttribute.HasConstructorArguments)
                        {
                            string propertyName = customAttribute.ConstructorArguments[0].Value.ToString();
                            if (propertyName == "Item")
                            {
                                AddCustomAttribute(customAttribute, true); // true because is indexer
                            }
                            // DefaultMemberAttribute is not supported at the moment
                            //else
                            //{
                            //    if (_unsupportedMethodsInCurrentType.Contains("get_" + propertyName) || _unsupportedMethodsInCurrentType.Contains("set_" + propertyName))
                            //    {
                            //        _parentClassAnalyzer.AddUsing(customAttribute.AttributeType.Namespace);
                            //        _parentClassAnalyzer.AddCustomAttribute("[DefaultMember(\"" + propertyName + "\")]");
                            //    }
                            //}
                        }
                    }
                }
            }
            if (Element.HasInterfaces)
            {
                foreach (InterfaceImplementation @interface in Element.Interfaces)
                {
                    TypeDefinition interfaceTypeDefinition = AnalysisUtils.GetTypeDefinitionFromTypeReference(@interface.InterfaceType, _modules);
                    if (interfaceTypeDefinition != null)
                    {
                        if (interfaceTypeDefinition.HasCustomAttributes)
                        {
                            foreach (CustomAttribute customAttribute in interfaceTypeDefinition.CustomAttributes)
                            {
                                if (customAttribute.AttributeType.FullName == "System.Reflection.DefaultMemberAttribute")
                                {
                                    if (customAttribute.HasConstructorArguments)
                                    {
                                        string propertyName = customAttribute.ConstructorArguments[0].Value.ToString();
                                        if (propertyName == "Item")
                                        {
                                            AddCustomAttribute(customAttribute, true);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal bool TryGetDependencyPropertyFromFieldName(string fieldName, TypeDefinition type, out FieldDefinition dependencyProperty)
        {
            dependencyProperty = null;
            if(type != null)
            {
                if (type.HasFields)
                {
                    foreach(FieldDefinition field in type.Fields)
                    {
                        if(field.Name == fieldName && field.FieldType.FullName == "System.Windows.DependencyProperty")
                        {
                            dependencyProperty = field;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        internal void AddTypeToImplement(TypeReference typeToAdd, HashSet<string> methodsToImplement = null)
        {
            if (_additionalTypesToImplement.ContainsKey(typeToAdd))
            {
                if (methodsToImplement != null)
                {
                    _additionalTypesToImplement[typeToAdd].UnionWith(methodsToImplement);
                }
            }
            else
            {
                _additionalTypesToImplement.Add(typeToAdd, methodsToImplement ?? new HashSet<string>());
            }
        }

        /// <summary>
        /// Retrieve all methods that must be implemented because they are defined in an interface that the type implements.
        /// Needed because it's unlikely that every single method from an interface is called at least once in the analyzed assemblies.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private HashSet<string> GetMethodsInheritedFromInterfaces(TypeDefinition type)
        {
            HashSet<string> res = new HashSet<string>();
            if (type.HasInterfaces)
            {
                foreach (InterfaceImplementation @interface in type.Interfaces)
                {
                    TypeDefinition interfaceTypeDefinition = AnalysisUtils.GetTypeDefinitionFromTypeReference(@interface.InterfaceType, _modules);
                    if (interfaceTypeDefinition != null)
                    {
                        string typeName = interfaceTypeDefinition.Name;
                        bool isInterfaceTypeUnsupported = IsTypeGoingToBeImplemented(interfaceTypeDefinition);
                        string assemblyName = interfaceTypeDefinition.Scope.Name.Replace(".dll", "");
                        //interface is not used in client code, but can still be defined in Core/mscorlib
                        if (!isInterfaceTypeUnsupported)
                        {
                            if (interfaceTypeDefinition.HasMethods)
                            {
                                if (analyzeHelpher._coreSupportedMethods.ContainsType(interfaceTypeDefinition.Name, interfaceTypeDefinition.Namespace))
                                {
                                    _implementedInterfaces.Add(@interface.InterfaceType);
                                    AddUsing(interfaceTypeDefinition.Namespace);
                                    foreach (MethodDefinition method in interfaceTypeDefinition.Methods)
                                    {
                                        //Check if method is supported and implement it if it is
                                        if (analyzeHelpher._coreSupportedMethods.Contains(interfaceTypeDefinition.Namespace, interfaceTypeDefinition.Name, method.Name))
                                        {
                                            res.Add(method.Name);
                                        }
                                    }
                                }
                                else if (analyzeHelpher.IsTypeSupported(@interface.InterfaceType))
                                {
                                    _implementedInterfaces.Add(@interface.InterfaceType);
                                    AddUsing(interfaceTypeDefinition.Namespace);

                                    //isInterfaceRequired = true;
                                    _implementedInterfaces.Add(@interface.InterfaceType);
                                    foreach (MethodDefinition method in interfaceTypeDefinition.Methods)
                                    {
                                        //Check if method is supported and implement it if it is
                                        if (analyzeHelpher.IsMethodSupported(method))
                                        {
                                            res.Add(method.Name);
                                        }
                                    }
                                }
                                else
                                {
                                    if ((_outputOptions.OutputOnlyPublicAndProtectedMembers && (interfaceTypeDefinition.IsPublic || interfaceTypeDefinition.IsNestedPublic || interfaceTypeDefinition.IsNestedFamily))
                                        || (!_outputOptions.OutputOnlyPublicAndProtectedMembers))
                                    {
                                        _implementedInterfaces.Add(@interface.InterfaceType);
                                        AddUsing(interfaceTypeDefinition.Namespace);
                                        AddTypeToImplement(interfaceTypeDefinition);
                                    }
                                }
                            }
                        }
                        //interface is used in client code or needed because of inheritance
                        else
                        {
                            //isInterfaceRequired = true;
                            HashSet<string> methodsToImplements;
                            //Check if the interface is used in the client code
                            if (_unsupportedMethods.ContainsKey(assemblyName))
                            {
                                if (!_unsupportedMethods[assemblyName].TryGetValue(interfaceTypeDefinition.Name, out methodsToImplements))
                                {
                                    methodsToImplements = _additionalTypesToImplement[interfaceTypeDefinition];
                                }
                            }
                            else
                            {
                                methodsToImplements = _additionalTypesToImplement[interfaceTypeDefinition];
                            }
                            foreach (MethodDefinition method in interfaceTypeDefinition.Methods)
                            {
                                if (methodsToImplements.Contains(method.Name))
                                {
                                    res.Add(method.Name);
                                }
                            }
                            if ((_outputOptions.OutputOnlyPublicAndProtectedMembers && (interfaceTypeDefinition.IsPublic || interfaceTypeDefinition.IsNestedPublic || interfaceTypeDefinition.IsNestedFamily))
                                || (!_outputOptions.OutputOnlyPublicAndProtectedMembers))
                            {
                                _implementedInterfaces.Add(interfaceTypeDefinition);
                                AddUsing(interfaceTypeDefinition.Namespace);
                            }
                        }
                    }
                }
            }
            return res;
        }

        internal void AddUsing(string @namespace)
        {
            if (!String.IsNullOrEmpty(@namespace))
            {
                if (@namespace != Element.Namespace)
                {
                    _typeBuilder.AddUsing(@namespace);
                }
            }
        }

        private void AddCustomAttribute(CustomAttribute customAttribute, bool isIndexer)
        {
            _typeBuilder.CustomAttributes.Add(customAttribute);
            if (isIndexer)
            {
                _hasAnIndexer = true;
            }
        }

        /// <summary>
        /// Method used to update the set of unsupported methods.
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="typeName"></param>
        /// <param name="methodName"></param>
        private void UpdateUnsupportedMethodsInfo(HashSet<string> methodsToAdd)
        {
            if (methodsToAdd != null)
            {
                foreach (string method in methodsToAdd)
                {
                    string assemblyName = _assemblyName;
                    string typeName = Element.Name;
                    string methodName = method;
                    if (_unsupportedMethods.ContainsKey(assemblyName))
                    {
                        if (_unsupportedMethods[assemblyName].ContainsKey(typeName))
                        {
                            _unsupportedMethods[assemblyName][typeName].Add(methodName);
                        }
                        else
                        {
                            _unsupportedMethods[assemblyName].Add(typeName, new HashSet<string>() { methodName });
                        }
                    }
                    else
                    {
                        Dictionary<string, HashSet<string>> typeMethodsPairs = new Dictionary<string, HashSet<string>>
                        {
                            { typeName, new HashSet<string>() { methodName } }
                        };
                        _unsupportedMethods.Add(assemblyName, typeMethodsPairs);
                    }
                }
            }
        }

        /// <summary>
        /// Check if parent type if abstract and get abstract methods that would have not been implemented if it is the case
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private HashSet<string> GetAbstractMethodsThatWouldntBeImlemented(TypeDefinition type)
        {
            HashSet<string> res = new HashSet<string>();
            if (type.BaseType != null)
            {
                AddUsing(type.BaseType.Namespace);
                TypeDefinition baseType = AnalysisUtils.GetTypeDefinitionFromTypeReference(type.BaseType, _modules);
                if (baseType != null)
                {
                    if (baseType.IsAbstract)
                    {
                        if (baseType.HasMethods)
                        {
                            foreach (MethodDefinition method in baseType.Methods)
                            {
                                if (AnalysisUtils.IsMethodAbstract(method))
                                {
                                    if (MethodAnalyzer.IsMethodUnsupported(method)
                                        || analyzeHelpher._coreSupportedMethods.Contains(baseType.Namespace, baseType.Name, method.Name)
                                        || analyzeHelpher.IsMethodSupported(method))
                                    {
                                        res.Add(method.Name);
                                    }
                                }
                            }
                        }
                        res.UnionWith(GetAbstractMethodsThatWouldntBeImlemented(baseType));
                    }
                }
            }
            return res;
        }

        private void AddField(Builder.FieldInfo field)
        {
            _typeBuilder.AddField(field);
        }

        internal void AddField(string fieldName, TypeReference type, bool isDependencyProperty, bool isAttached, bool isStatic, string attachedPropertyNameIfAny, TypeReference dependencyPropertyTypeIfAny, out string newFieldNameIfAlreadyUsed)
        {
            _typeBuilder.IsFieldNameTaken(fieldName, out newFieldNameIfAlreadyUsed);
            AddField(new Builder.FieldInfo(newFieldNameIfAlreadyUsed, type, isStatic: isStatic, isDependencyProperty: isDependencyProperty, isAttachedProperty: isAttached, dependencyPropertyTypeIfAny: dependencyPropertyTypeIfAny, propertyName: attachedPropertyNameIfAny));
        }

        internal void AddField(FieldDefinition field, bool isDependencyProperty, bool isAttachedProperty, TypeReference dependencyPropertyTypeIfAny, bool isStatic, string attachedPropertyNameIfAny)
        {
            AddField(new Builder.FieldInfo(field, isDependencyProperty: isDependencyProperty, isAttachedProperty: isAttachedProperty, dependencyPropertyTypeIfAny: dependencyPropertyTypeIfAny, propertyName: attachedPropertyNameIfAny));
        }

        internal void AddProperty(Builder.PropertyInfo property)
        {
            _typeBuilder.AddProperty(property);
        }

        internal void AddMethod(Builder.MethodInfo method)
        {
            _typeBuilder.AddMethod(method);
        }

        internal void AddEvent(EventDefinition @event)
        {
            _typeBuilder.AddEvent(@event);
        }

        private void GetUsingsForDelegate(TypeDefinition @delegate)
        {
            if (@delegate.HasMethods)
            {
                foreach (MethodDefinition method in @delegate.Methods)
                {
                    if (method.Name == "Invoke")
                    {
                        AddUsing(method.ReturnType.Namespace);
                        if (method.HasParameters)
                        {
                            foreach (ParameterDefinition param in method.Parameters)
                            {
                                AddUsing(param.ParameterType.Namespace);
                            }
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Check if we are allowed to used the current type.
        /// </summary>
        /// <returns></returns>
        public bool CanWorkOnElement()
        {
            if (Element == null)
            {
                return false;
            }
            bool isCoreSupported = analyzeHelpher._coreSupportedMethods.ContainsType(Element.Name, Element.Namespace);
            bool isMscorlibSupported = analyzeHelpher.IsTypeSupported(Element);
            bool isNotAlreadyImplemented = !_alreadyImplementedTypes.Contains(_assemblyName + '.' + Element.FullName);
            bool isAccessible = (_outputOptions.OutputOnlyPublicAndProtectedMembers && (Element.IsPublic || Element.IsNestedPublic || Element.IsNestedFamily))
                || (!_outputOptions.OutputOnlyPublicAndProtectedMembers);
            return isNotAlreadyImplemented && isAccessible && !isCoreSupported && !isMscorlibSupported;
        }

        /// <summary>
        /// Check if we are allowed to use a given type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool CanWorkOnElement(TypeDefinition type)
        {
            if (type == null)
            {
                return false;
            }
            bool isCoreSupported = analyzeHelpher._coreSupportedMethods.ContainsType(type.Name, type.Namespace);
            bool isMscorlibSupported = analyzeHelpher.IsTypeSupported(type);
            string assemblyName = type.Scope.Name.Replace(".dll", "");
            bool isNotAlreadyImplemented = !_alreadyImplementedTypes.Contains(assemblyName + '.' + type.FullName);
            bool isAccessible = (_outputOptions.OutputOnlyPublicAndProtectedMembers && (type.IsPublic || type.IsNestedPublic || type.IsNestedFamily))
                || (!_outputOptions.OutputOnlyPublicAndProtectedMembers);
            return isNotAlreadyImplemented && isAccessible && !isCoreSupported && !isMscorlibSupported;
        }

        private HashSet<string> GetAdditionalCodeIfAny(string assemblyName, string typeName)
        {
            if (Configuration.CodeToAddManuallyBecauseItIsUndetected.ContainsKey(assemblyName))
            {
                if (Configuration.CodeToAddManuallyBecauseItIsUndetected[assemblyName].ContainsKey(typeName))
                {
                    return Configuration.CodeToAddManuallyBecauseItIsUndetected[assemblyName][typeName];
                }
            }
            return null;
        }

        /// <summary>
        /// Generate unsupported types which have been detected during the execution of the program.
        /// </summary>
        internal void GenerateAddtionalUnsupportedTypes()
        {
            TypeReference type;
            HashSet<string> methods;
            while (_additionalTypesToImplement.Count > 0)
            {
                var keyValuePair = _additionalTypesToImplement.First();
                type = keyValuePair.Key;
                methods = keyValuePair.Value;
                if(PrepareForAdditionalType(type, methods))
                {
                    Execute();
                }
                _additionalTypesToImplement.Remove(type);
            }
        }

        private bool PrepareForAdditionalType(TypeReference type, HashSet<string> methods)
        {
            var typeDef = AnalysisUtils.GetTypeDefinitionFromTypeReference(type, _modules);
            if(typeDef == null)
            {
                //we can't find the type.
                return false;
            }
            bool isCoreSupported = analyzeHelpher._coreSupportedMethods.ContainsType(type.Name, type.Namespace);
            bool isMscorlibSupported = analyzeHelpher.IsTypeSupported(type);
            if(isCoreSupported || isMscorlibSupported)
            {
                //type is already supported.
                return false;
            }
            Set(typeDef, methods);
            return true;
        }

        /// <summary>
        /// Analyze base type and interfaces with AnalyzeFullType method.
        /// </summary>
        /// <param name="type"></param>
        private void AnalyzeParentAndInterfacesType(TypeDefinition type)
        {
            if (type.BaseType != null && (type.BaseType.Name != "System.Object" && type.BaseType.Name != "System.ValueType"))
            {
                AnalyzeFullType(type.BaseType);
            }
            if (type.HasInterfaces)
            {
                foreach (InterfaceImplementation @interface in type.Interfaces)
                {
                    AnalyzeFullType(@interface.InterfaceType, false);
                }
            }
        }

        /// <summary>
        /// Check if every types used in the defintion of a type is supported (or about to be). If not, it makes sur they are implemented after.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="addUsing"></param>
        /// <returns></returns>
        internal bool AnalyzeFullType(TypeReference type, bool addUsing = true)
        {
            if (addUsing)
            {
                AddUsing(type.Namespace);
            }
            if (type.IsGenericInstance)
            {
                GenericInstanceType instanceType = (GenericInstanceType)type;
                bool isTypeKnown = analyzeHelpher._coreSupportedMethods.ContainsType(instanceType.ElementType.Name, instanceType.ElementType.Namespace) || analyzeHelpher.IsTypeSupported(type) || IsTypeGoingToBeImplemented(instanceType.ElementType) || AnalysisUtils.IsDefaultValueType(instanceType.ElementType.FullName);
                if (!isTypeKnown)
                {
                    AddTypeToImplement(instanceType.ElementType);
                }
                IEnumerable<TypeReference> genericArgsTypes = instanceType.GenericArguments;
                foreach (TypeReference genericArgType in genericArgsTypes)
                {
                    bool isCurrentGParamKnown = AnalyzeFullType(genericArgType);
                }
                return isTypeKnown;
            }
            else if (type.IsGenericParameter)
            {
                return false;
            }
            else
            {
                bool isTypeKnown = analyzeHelpher._coreSupportedMethods.ContainsType(type.Name, type.Namespace) || analyzeHelpher.IsTypeSupported(type) || IsTypeGoingToBeImplemented(type) || AnalysisUtils.IsDefaultValueType(type.FullName);
                if (!isTypeKnown)
                {
                    AddTypeToImplement(type);
                }
                return isTypeKnown;
            }
        }

        /// <summary>
        /// Check if a type is about to be implemented.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal bool IsTypeGoingToBeImplemented(TypeReference type)
        {
            string assemblyName = type.Scope.Name.Replace(".dll", "");
            string typeName = type.Name;
            if (_unsupportedMethods.ContainsKey(assemblyName))
            {
                if (_unsupportedMethods[assemblyName].ContainsKey(typeName))
                {
                    return true;
                }
            }
            return _additionalTypesToImplement.ContainsKey(type);
        }
    }
}
