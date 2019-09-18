using DotNetForHtml5.PrivateTools.AssemblyAnalysisCommon;
using Mono.Cecil;
using StubGenerator.Common.Options;
using System;
using System.Collections.Generic;

namespace StubGenerator.Common.Analyzer
{
    public class MethodAnalyzer : IElementAnalyzer<MethodDefinition>
    {
        private OutputOptions _outputOptions;
        private Dictionary<string, Dictionary<string, HashSet<string>>> _unsupportedMethods;
        private HashSet<string> _unsupportedMethodsInCurrentType;
        private List<ModuleDefinition> _modules;
        private MethodType _isMethodOrPropertyOrEvent;
        private ClassAnalyzer _parentClassAnalyzer;
        private Dictionary<string, Dictionary<string, HashSet<MethodSignature>>> _implementedMethods;
        private bool _declaringTypeHasAnIndexer;
        private bool _isInitialized = false;

        public MethodDefinition Element { get; set; }

        internal MethodAnalyzer(Dictionary<string, Dictionary<string, HashSet<string>>> unsupportedMethods, List<ModuleDefinition> modules, ClassAnalyzer parentClassAnalyzer, OutputOptions outputOptions = null)
        {
            Init(unsupportedMethods, modules, parentClassAnalyzer, outputOptions);
        }

        /// <summary>
        /// Initialize the MethodAnalyzer. Must be call once.
        /// </summary>
        /// <param name="unsupportedMethods"></param>
        /// <param name="modules"></param>
        /// <param name="parentClassAnalyzer"></param>
        /// <param name="outputOptions"></param>
        private void Init(Dictionary<string, Dictionary<string, HashSet<string>>> unsupportedMethods, List<ModuleDefinition> modules, ClassAnalyzer parentClassAnalyzer, OutputOptions outputOptions = null)
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

                _implementedMethods = new Dictionary<string, Dictionary<string, HashSet<MethodSignature>>>();
                _parentClassAnalyzer = parentClassAnalyzer;
                _unsupportedMethods = unsupportedMethods;
                _modules = modules;
                _declaringTypeHasAnIndexer = false;
                _isInitialized = true;
            }
        }

        public void SetForType(HashSet<string> unsupportedMethodsInCurrentType, bool hasIndexer)
        {
            _unsupportedMethodsInCurrentType = unsupportedMethodsInCurrentType;
            _declaringTypeHasAnIndexer = hasIndexer;
        }

        public void SetForMethod(MethodDefinition method)
        {
            Element = method;
        }

        /// <summary>
        /// Check if a method is already implemented. We use the signature of the method rather than the name to make sure we don't make a mistake in case of overloaded methods.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        private bool IsMethodAlreadyImplemented(MethodDefinition method)
        {
            if (_implementedMethods.ContainsKey(_parentClassAnalyzer._assemblyName))
            {
                if (_implementedMethods[_parentClassAnalyzer._assemblyName].ContainsKey(_parentClassAnalyzer.Element.FullName))
                {
                    MethodSignature sig = new MethodSignature(method);
                    return _implementedMethods[_parentClassAnalyzer._assemblyName][_parentClassAnalyzer.Element.FullName].Contains(new MethodSignature(method));
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

        //todo: change return type.
        private Tuple<AccessModifierEnum, AccessModifierEnum> AddMethodToImplementedMethodsSet(MethodDefinition method)
        {
            MethodType type = IsMethodOrPropertyOrEvent(method);
            string fromInterface = "";
            if (AnalysisUtils.IsMethodExplicitlyImplemented(method))
            {
                fromInterface = method.Name.Substring(0, method.Name.LastIndexOf('.') + 1);
            }
            if (_implementedMethods.ContainsKey(_parentClassAnalyzer._assemblyName))
            {
                if (_implementedMethods[_parentClassAnalyzer._assemblyName].ContainsKey(_parentClassAnalyzer.Element.FullName))
                {
                    if (type == MethodType.METHOD)
                    {
                        _implementedMethods[_parentClassAnalyzer._assemblyName][_parentClassAnalyzer.Element.FullName].Add(new MethodSignature(method));
                        return new Tuple<AccessModifierEnum, AccessModifierEnum>(AccessModifierEnum.PUBLIC, AccessModifierEnum.PUBLIC);
                    }
                    else if (type == MethodType.PROPERTY)
                    {
                        string propertyName = GetNameOfPropertyOrEvent(method.Name);
                        MethodSignature methodToImplement = new MethodSignature(method);
                        MethodSignature secondMethod = GetSignatureOfSecondMethodOfPropertyFromFirstMethod(method);
                        _implementedMethods[_parentClassAnalyzer._assemblyName][_parentClassAnalyzer.Element.FullName].Add(methodToImplement);
                        AccessModifierEnum secondMethodAccessModifierIfAny;
                        if ((secondMethodAccessModifierIfAny = IsPropertyMethodDefined(secondMethod, _parentClassAnalyzer.Element)) != AccessModifierEnum.NONE)
                        {
                            _implementedMethods[_parentClassAnalyzer._assemblyName][_parentClassAnalyzer.Element.FullName].Add(secondMethod);
                        }
                        if (method.Name.Contains("get_"))
                        {
                            return new Tuple<AccessModifierEnum, AccessModifierEnum>(AccessModifierEnum.PUBLIC, secondMethodAccessModifierIfAny);
                        }
                        else if (method.Name.Contains("set_"))
                        {
                            return new Tuple<AccessModifierEnum, AccessModifierEnum>(secondMethodAccessModifierIfAny, AccessModifierEnum.PUBLIC);
                        }
                    }
                    else if (type == MethodType.EVENT)
                    {
                        string eventName = GetNameOfPropertyOrEvent(method.Name);
                        string returnType = method.Parameters[0].ParameterType.FullName;
                        _implementedMethods[_parentClassAnalyzer._assemblyName][_parentClassAnalyzer.Element.FullName].Add(new MethodSignature(fromInterface + "add_" + eventName, "System.Void", true, new List<string>() { returnType }));
                        _implementedMethods[_parentClassAnalyzer._assemblyName][_parentClassAnalyzer.Element.FullName].Add(new MethodSignature(fromInterface + "remove_" + eventName, "System.Void", true, new List<string>() { returnType }));
                    }
                }
                else
                {
                    if (type == MethodType.METHOD)
                    {
                        _implementedMethods[_parentClassAnalyzer._assemblyName].Add(_parentClassAnalyzer.Element.FullName, new HashSet<MethodSignature>() { new MethodSignature(method) });
                    }
                    else if (type == MethodType.PROPERTY)
                    {
                        string propertyName = GetNameOfPropertyOrEvent(method.Name);
                        MethodSignature methodToImplement = new MethodSignature(method);
                        MethodSignature secondMethod = GetSignatureOfSecondMethodOfPropertyFromFirstMethod(method);
                        _implementedMethods[_parentClassAnalyzer._assemblyName].Add(_parentClassAnalyzer.Element.FullName, new HashSet<MethodSignature>() { methodToImplement });
                        AccessModifierEnum secondMethodAccessModifierIfAny;
                        if ((secondMethodAccessModifierIfAny = IsPropertyMethodDefined(secondMethod, _parentClassAnalyzer.Element)) != AccessModifierEnum.NONE)
                        {
                            _implementedMethods[_parentClassAnalyzer._assemblyName][_parentClassAnalyzer.Element.FullName].Add(secondMethod);
                        }
                        if (method.Name.Contains("get_"))
                        {
                            return new Tuple<AccessModifierEnum, AccessModifierEnum>(AccessModifierEnum.PUBLIC, secondMethodAccessModifierIfAny);
                        }
                        else if (method.Name.Contains("set_"))
                        {
                            return new Tuple<AccessModifierEnum, AccessModifierEnum>(secondMethodAccessModifierIfAny, AccessModifierEnum.PUBLIC);
                        }
                    }
                    else if (type == MethodType.EVENT)
                    {
                        string eventName = GetNameOfPropertyOrEvent(method.Name);
                        string returnType = method.Parameters[0].ParameterType.FullName;
                        _implementedMethods[_parentClassAnalyzer._assemblyName].Add(_parentClassAnalyzer.Element.FullName, new HashSet<MethodSignature>() {
                            new MethodSignature(fromInterface + "add_" + eventName, "System.Void", true, new List<string>() { returnType }),
                            new MethodSignature(fromInterface + "remove_" + eventName, "System.Void", true, new List<string>() { returnType })
                        });
                    }
                }
            }
            else
            {
                Dictionary<string, HashSet<MethodSignature>> typeDict = new Dictionary<string, HashSet<MethodSignature>>
                {
                    { _parentClassAnalyzer.Element.FullName, new HashSet<MethodSignature>() }
                };
                if (type == MethodType.METHOD)
                {
                    typeDict[_parentClassAnalyzer.Element.FullName].Add(new MethodSignature(method));
                    _implementedMethods.Add(_parentClassAnalyzer._assemblyName, typeDict);
                }
                else if (type == MethodType.PROPERTY)
                {
                    string propertyName = GetNameOfPropertyOrEvent(method.Name);
                    MethodSignature methodToImplement = new MethodSignature(method);
                    MethodSignature secondMethod = GetSignatureOfSecondMethodOfPropertyFromFirstMethod(method);
                    typeDict[_parentClassAnalyzer.Element.FullName].Add(methodToImplement);
                    AccessModifierEnum secondMethodAccessModifierIfAny;
                    if ((secondMethodAccessModifierIfAny = IsPropertyMethodDefined(secondMethod, _parentClassAnalyzer.Element)) != AccessModifierEnum.NONE)
                    {
                        typeDict[_parentClassAnalyzer.Element.FullName].Add(secondMethod);
                    }
                    _implementedMethods.Add(_parentClassAnalyzer._assemblyName, typeDict);
                    if (method.Name.Contains("get_"))
                    {
                        return new Tuple<AccessModifierEnum, AccessModifierEnum>(AccessModifierEnum.PUBLIC, secondMethodAccessModifierIfAny);
                    }
                    else if (method.Name.Contains("set_"))
                    {
                        return new Tuple<AccessModifierEnum, AccessModifierEnum>(secondMethodAccessModifierIfAny, AccessModifierEnum.PUBLIC);
                    }
                }
                else if (type == MethodType.EVENT)
                {
                    string eventName = GetNameOfPropertyOrEvent(method.Name);
                    string returnType = method.Parameters[0].ParameterType.FullName;
                    typeDict[_parentClassAnalyzer.Element.FullName].Add(new MethodSignature(fromInterface + "add_" + eventName, "System.Void", true, new List<string>() { returnType }));
                    typeDict[_parentClassAnalyzer.Element.FullName].Add(new MethodSignature(fromInterface + "remove_" + eventName, "System.Void", true, new List<string>() { returnType }));
                    _implementedMethods.Add(_parentClassAnalyzer._assemblyName, typeDict);
                }
            }
            return new Tuple<AccessModifierEnum, AccessModifierEnum>(AccessModifierEnum.PUBLIC, AccessModifierEnum.PUBLIC);
        }

        private TypeReference GetInterfaceImplementedByParentFromFullName(string interfaceFullName)
        {
            foreach (TypeReference type in _parentClassAnalyzer._implementedInterfaces)
            {
                if (type.FullName == interfaceFullName)
                {
                    return type;
                }
            }
            return null;
        }

        /// <summary>
        /// Check if we can use the current method.
        /// </summary>
        /// <returns></returns>
        public bool CanWorkOnElement()
        {
            if (Element == null)
            {
                return false;
            }
            bool isExplicitlyImplemented;
            string methodName;
            if (AnalysisUtils.IsMethodExplicitlyImplemented(Element))
            {
                int lastDotIndex = Element.Name.LastIndexOf('.');
                methodName = Element.Name.Substring(lastDotIndex + 1);
                string interfaceFullName = Element.Name.Substring(0, lastDotIndex);
                TypeReference interfaceWhereMethodIsDefined = GetInterfaceImplementedByParentFromFullName(AnalysisUtils.GetGenericTypeNameFromTypeName(interfaceFullName));
                if (interfaceWhereMethodIsDefined == null)
                {
                    isExplicitlyImplemented = false;
                }
                else
                {
                    string interfaceAssemblyName = interfaceWhereMethodIsDefined.Scope.Name.Replace(".dll", "");
                    HashSet<string> interfaceMethods;
                    if (ClassAnalyzer.analyzeHelpher._coreSupportedMethods.ContainsType(interfaceWhereMethodIsDefined.Name, interfaceWhereMethodIsDefined.Namespace))
                    {
                        isExplicitlyImplemented = true;
                    }
                    else if (ClassAnalyzer.analyzeHelpher.IsTypeSupported(interfaceWhereMethodIsDefined))
                    {
#if BRIDGE
                        //TODO : check if method is supported
                        isExplicitlyImplemented = true;
#else
                        isExplicitlyImplemented = true;
#endif
                    }
                    else if (_unsupportedMethods.ContainsKey(interfaceAssemblyName))
                    {
                        if (_unsupportedMethods[interfaceAssemblyName].TryGetValue(interfaceWhereMethodIsDefined.Name, out interfaceMethods))
                        {
                            isExplicitlyImplemented = interfaceMethods.Contains(methodName);
                        }
                        else
                        {
                            if (_parentClassAnalyzer._additionalTypesToImplement.TryGetValue(interfaceWhereMethodIsDefined, out interfaceMethods))
                            {
                                isExplicitlyImplemented = interfaceMethods.Contains(methodName);
                            }
                            else
                            {
                                isExplicitlyImplemented = false;
                            }
                        }
                    }
                    else
                    {
                        if (_parentClassAnalyzer._additionalTypesToImplement.TryGetValue(interfaceWhereMethodIsDefined, out interfaceMethods))
                        {
                            isExplicitlyImplemented = interfaceMethods.Contains(methodName);
                        }
                        else
                        {
                            isExplicitlyImplemented = false;
                        }
                    }
                }
            }
            else
            {
                methodName = Element.Name;
                isExplicitlyImplemented = false;
            }
            bool isNotAlreadyImplemented = !IsMethodAlreadyImplemented(Element);
            bool isUnsupported = _unsupportedMethodsInCurrentType.Contains(methodName);
            bool isAccessible = (_outputOptions.OutputOnlyPublicAndProtectedMembers && (Element.IsPublic || Element.IsFamily))
                || !_outputOptions.OutputOnlyPublicAndProtectedMembers;
            return isNotAlreadyImplemented && isUnsupported && (isAccessible || isExplicitlyImplemented);
        }

        /// <summary>
        /// Analyze the current method and return a method generated from this one. 
        /// </summary>
        /// <returns></returns>
        public void Run()
        {
            if (!_isInitialized)
            {
                throw new Exception("MethodAnalyzer must be initialized. Please call Init() first.");
            }

            if (CanWorkOnElement())
            {
                _isMethodOrPropertyOrEvent = IsMethodOrPropertyOrEvent(Element);
                CheckParametersAndReturnValueType();
                string dependencyPropertyNameIfAny;
                switch (_isMethodOrPropertyOrEvent)
                {
                    case MethodType.PROPERTY:
                        PropertyDefinition property = FindProperty(Element, Element.DeclaringType);
                        if (AddDependencyPropertyIfDefined(property, out dependencyPropertyNameIfAny))
                        {
                            _parentClassAnalyzer.AddProperty(new Builder.PropertyInfo(property, true, dependencyPropertyNameIfAny));
                        }
                        else
                        {
                            if (!AnalysisUtils.IsMethodAbstract(property) && _outputOptions.OutputPropertyOptions == OutputPropertyOptions.OUTPUT_PRIVATE_FIELD)
                            {
                                string fieldName = property.Name.Contains(".") ? property.Name.Substring(property.Name.LastIndexOf('.') + 1) : property.Name;
                                fieldName = "_" + fieldName.Substring(0, 1).ToLower() + fieldName.Substring(1);
                                string newFieldName;
                                _parentClassAnalyzer.AddField(fieldName, property.PropertyType, false, false, AnalysisUtils.IsMethodStatic(property), null, null, out newFieldName);
                                _parentClassAnalyzer.AddProperty(new Builder.PropertyInfo(property, false, newFieldName));
                            }
                            else
                            {
                                _parentClassAnalyzer.AddProperty(new Builder.PropertyInfo(property));
                            }
                        }
                        break;
                    case MethodType.METHOD:
                        if(AddDependencyPropertyIfDefined(Element, out dependencyPropertyNameIfAny))
                        {
                            _parentClassAnalyzer.AddMethod(new Builder.MethodInfo(Element, Element.Name.StartsWith("Get"), Element.Name.StartsWith("Set"), dependencyPropertyNameIfAny));
                        }
                        else
                        {
                            _parentClassAnalyzer.AddMethod(new Builder.MethodInfo(Element));
                        }
                        break;
                    case MethodType.EVENT:
                        EventDefinition @event = FindEvent(Element, Element.DeclaringType);
                        _parentClassAnalyzer.AddEvent(@event);
                        break;
                }
                AddMethodToImplementedMethodsSet(Element);
            }
        }

        private PropertyDefinition FindProperty(MethodDefinition propertyMethod, TypeDefinition type)
        {
            if(type == null)
            {
                throw new Exception("Can't find property named \"" + GetNameOfPropertyOrEvent(propertyMethod.Name) + "\" in null type");
            }

            foreach(PropertyDefinition property in type.Properties)
            {
                if(property.GetMethod == propertyMethod || property.SetMethod == propertyMethod)
                {
                    return property;
                }
            }
            return null;
        }

        private EventDefinition FindEvent(MethodDefinition eventMethod, TypeDefinition type)
        {
            if(type == null)
            {
                throw new Exception("Can't find event named \"" + GetNameOfPropertyOrEvent(eventMethod.Name) + "\" in null type");
            }

            foreach(EventDefinition @event in type.Events)
            {
                if(@event.AddMethod == eventMethod || @event.RemoveMethod == eventMethod)
                {
                    return @event;
                }
            }
            string eventName = "";
            if (AnalysisUtils.IsMethodExplicitlyImplemented(eventMethod))
            {
                eventName += eventMethod.Name.Substring(0, eventMethod.Name.LastIndexOf('.') + 1);
            }
            eventName += GetNameOfPropertyOrEvent(eventMethod.Name);
            EventDefinition newEvent = new EventDefinition(eventName, EventAttributes.None, eventMethod.Parameters[0].ParameterType);
            if (eventMethod.Name.Contains("add_"))
            {
                newEvent.AddMethod = eventMethod;
                newEvent.RemoveMethod = GetSecondMethodOfPropertyOrEvent(eventMethod, type);
            }
            else
            {
                newEvent.AddMethod = GetSecondMethodOfPropertyOrEvent(eventMethod, type);
                newEvent.RemoveMethod = eventMethod;
            }
            return newEvent;
        }

        private MethodDefinition GetSecondMethodOfPropertyOrEvent(MethodDefinition firstMethod, TypeDefinition type)
        {
            int lastIndexOfDot = firstMethod.Name.LastIndexOf('.');
            string interfaceNameIfAny = "";
            string memberName = "";
            if (lastIndexOfDot > -1)
            {
                interfaceNameIfAny = firstMethod.Name.Substring(0, lastIndexOfDot + 1);
                memberName = GetNameOfPropertyOrEvent(firstMethod.Name.Substring(lastIndexOfDot + 1));
            }
            string methodToLookForPrefix = "";
            if (firstMethod.Name.Contains("add_"))
            {
                methodToLookForPrefix = "remove_";
            }
            else if (firstMethod.Name.Contains("remove_"))
            {
                methodToLookForPrefix = "add_";
            }
            else if (firstMethod.Name.Contains("get_"))
            {
                methodToLookForPrefix = "set_";
            }
            else if (firstMethod.Name.Contains("set_"))
            {
                methodToLookForPrefix = "get_";
            }
            string methodToLookFor = interfaceNameIfAny + methodToLookForPrefix + memberName;
            foreach(MethodDefinition method in type.Methods)
            {
                if(method.Name == methodToLookFor)
                {
                    return method;
                }
            }
            return null;
        }

        private bool CanMethodBeADependencyPropertyAccessor(MethodDefinition method, out TypeReference dependencyPropertyTypeIfAny)
        {
            bool isGetter = method.Name.StartsWith("Get");
            bool isSetter = method.Name.StartsWith("Set");
            bool canBeADependencyProperty = isGetter || isSetter;
            canBeADependencyProperty &= method.HasParameters && method.Parameters[0].ParameterType.FullName == "System.Windows.DependencyObject";
            if (canBeADependencyProperty)
            {
                if (isGetter)
                {
                    dependencyPropertyTypeIfAny = method.ReturnType;
                }
                else
                {
                    dependencyPropertyTypeIfAny = method.Parameters[1].ParameterType;
                }
            }
            else
            {
                dependencyPropertyTypeIfAny = null;
            }
            return canBeADependencyProperty;
        }

        private bool AddDependencyPropertyIfDefined(MemberReference member, out string dependencyPropertyNameIfAny)
        {
            dependencyPropertyNameIfAny = null;
            FieldDefinition dependencyPropertyIfAny = null;
            if(member is MethodDefinition)
            {
                MethodDefinition method = (MethodDefinition)member;
                TypeReference dependencyPropertyTypeIfAny;
                if(CanMethodBeADependencyPropertyAccessor(method, out dependencyPropertyTypeIfAny))
                {
                    string nameOfField = method.Name.Substring(3) + "Property";
                    if(_parentClassAnalyzer.TryGetDependencyPropertyFromFieldName(nameOfField, method.DeclaringType, out dependencyPropertyIfAny))
                    {
                        dependencyPropertyNameIfAny = nameOfField;
                        _parentClassAnalyzer.AddUsing("System.Windows");
                        _parentClassAnalyzer.AddField(dependencyPropertyIfAny, isDependencyProperty: true, isAttachedProperty: true, dependencyPropertyTypeIfAny: dependencyPropertyTypeIfAny, isStatic: true, attachedPropertyNameIfAny: nameOfField);
                        return true;
                    }
                }
            }
            else if(member is PropertyDefinition)
            {
                PropertyDefinition property = (PropertyDefinition)member;
                string nameOfField = property.Name + "Property";
                if(_parentClassAnalyzer.TryGetDependencyPropertyFromFieldName(nameOfField, property.DeclaringType, out dependencyPropertyIfAny))
                {
                    dependencyPropertyNameIfAny = nameOfField;
                    _parentClassAnalyzer.AddUsing("System.Windows");
                    _parentClassAnalyzer.AddField(dependencyPropertyIfAny, isDependencyProperty: true, isAttachedProperty: false, dependencyPropertyTypeIfAny:property.PropertyType, isStatic: true, attachedPropertyNameIfAny: nameOfField);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if the method is an acual method, or a member of a property or event.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        private MethodType IsMethodOrPropertyOrEvent(MethodDefinition method)
        {
            string methodName;
            if (AnalysisUtils.IsMethodExplicitlyImplemented(method))
            {
                methodName = method.Name.Substring(method.Name.LastIndexOf('.') + 1);
            }
            else
            {
                methodName = method.Name;
            }
            if (methodName.StartsWith("get_") || methodName.StartsWith("set_"))
            {
                return MethodType.PROPERTY;
            }
            else if (methodName.StartsWith("add_") || methodName.StartsWith("remove_"))
            {
                return MethodType.EVENT;
            }
            else
            {
                return MethodType.METHOD;
            }
        }

        private MethodSignature GetSignatureOfSecondMethodOfPropertyFromFirstMethod(MethodDefinition method)
        {
            bool isExplicitlyImplemented = AnalysisUtils.IsMethodExplicitlyImplemented(method);
            string name = GetNameOfPropertyOrEvent(method.Name);
            string fromInterface = "";
            string returnType;
            bool hasParameters;
            List<string> parameters = new List<string>();
            if (isExplicitlyImplemented)
            {
                fromInterface = method.Name.Substring(0, method.Name.LastIndexOf('.') + 1);
            }
            string newPrefix;
            if (method.Name.Contains("get_"))
            {
                newPrefix = "set_";
                returnType = "System.Void";
                hasParameters = true;
                if (method.HasParameters)
                {
                    foreach (ParameterDefinition param in method.Parameters)
                    {
                        parameters.Add(param.ParameterType.FullName);
                    }
                }
                parameters.Add(method.ReturnType.FullName);
            }
            else
            {
                newPrefix = "get_";
                returnType = method.Parameters[method.Parameters.Count - 1].ParameterType.FullName;
                hasParameters = (method.Parameters.Count > 1);
                if (hasParameters)
                {
                    for (int i = 0; i < method.Parameters.Count - 1; i++)
                    {
                        parameters.Add(method.Parameters[i].ParameterType.FullName);
                    }
                }
            }
            string fullName = fromInterface + newPrefix + name;
            return new MethodSignature(fullName, returnType, hasParameters, parameters);
        }

        /// <summary>
        /// Check if a type define both get and set for a given property.
        /// </summary>
        /// <param name="methodToLookFor"></param>
        /// <param name="typeToLookInto"></param>
        /// <returns></returns>
        private AccessModifierEnum IsPropertyMethodDefined(MethodSignature methodToLookFor, TypeDefinition typeToLookInto)
        {
            if (typeToLookInto != null)
            {
                if (typeToLookInto.HasMethods)
                {
                    MethodSignature methodToCompare;
                    foreach (MethodDefinition m in typeToLookInto.Methods)
                    {
                        methodToCompare = new MethodSignature(m);
                        if (methodToLookFor == methodToCompare)
                        {
                            return AnalysisUtils.GetMethodAccessModifier(m);
                        }
                    }
                }
            }
            return AccessModifierEnum.NONE;

        }

        /// <summary>
        /// Get the name of a property from the name of one of his methods (get or set).
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        private string GetNameOfPropertyOrEvent(string method)
        {
            string name = method.Substring(method.LastIndexOf('_') + 1);
            return name;
        }

        /// <summary>
        /// Check return type and parameters types of the current method to see if some types need to be implemented.
        /// </summary>
        private void CheckParametersAndReturnValueType()
        {
            _parentClassAnalyzer.AnalyzeFullType(Element.ReturnType);
            if (Element.HasParameters)
            {
                foreach (ParameterDefinition param in Element.Parameters)
                {
                    _parentClassAnalyzer.AnalyzeFullType(param.ParameterType);
                }
            }
        }

        /// <summary>
        /// Check if a method is in the unsupported methods set.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        internal bool IsMethodUnsupported(MethodDefinition method)
        {
            string assemblyName = method.DeclaringType.Scope.Name.Replace(".dll", "");
            if (_unsupportedMethods.ContainsKey(assemblyName))
            {
                string typeName = method.DeclaringType.Name;
                if (_unsupportedMethods[assemblyName].ContainsKey(typeName))
                {
                    if (_unsupportedMethods[assemblyName][typeName].Contains(method.Name))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
