using System;
using System.Collections.Generic;
using System.Linq;
using DotNetForHtml5.PrivateTools.AssemblyAnalysisCommon;
using Mono.Cecil;
using StubGenerator.Common.Options;

namespace StubGenerator.Common.Builder
{
    public class DefaultValueGenerator
    {
        public bool IsInitialized { get; set; }

        private TypeSystem _instanceOfTypeSystem;

        public TypeSystem InstanceOfTypeSystem
        {
            get
            {
                if (_instanceOfTypeSystem == null)
                {
                    int i = 0;
                    while (i < _modules.Count && _modules[i].TypeSystem == null)
                    {
                        i++;
                    }
                    _instanceOfTypeSystem = _modules[i].TypeSystem;
                }
                return _instanceOfTypeSystem;
            }
        }
       
        private OutputOptions _outputOptions;
        private List<ModuleDefinition> _modules;

        private Dictionary<TypeReference, HashSet<MethodDefinition>> _constructors;

        private Dictionary<TypeReference, HashSet<MethodDefinition>> Constructors
        {
            get
            {
                if (_constructors == null)
                {
                    _constructors = new Dictionary<TypeReference, HashSet<MethodDefinition>>();
                }
                return _constructors;
            }
            set
            {
                _constructors = value;
            }
        }

        public void Init(OutputOptions outputOptions, List<ModuleDefinition> modules)
        {
            if (!IsInitialized)
            {
                _instanceOfTypeSystem = null;
                _constructors = null;
                _outputOptions = outputOptions;
                _modules = modules;
                IsInitialized = true;
            }
            else
            {
                throw new Exception("DefaultValueGenerator can only be initialized once.");
            }
        }

        private bool TryGetConstructors(TypeReference type, out HashSet<MethodDefinition> constructorsIfAny)
        {
            if (Constructors.ContainsKey(type))
            {
                constructorsIfAny = Constructors[type];
                return (constructorsIfAny != null && constructorsIfAny.Count > 0);
            }
            else
            {
                bool isTypeWithNoConstructor;
                if (AnalysisUtils.TryGetConstructors(type, _modules, out constructorsIfAny, out isTypeWithNoConstructor))
                {
                    Constructors.Add(type, constructorsIfAny);
                    return true;
                }
                else
                {
                    constructorsIfAny = null;
                    Constructors.Add(type, null);
                    return false;
                }
            }
        }

        private MethodDefinition ConvertConstructorToGenericInstanceMethod(MethodDefinition constructor, TypeReference declaringType)
        {
            if (!declaringType.IsGenericInstance)
            {
                return constructor;
            }
            else
            {
                GenericInstanceType type = (GenericInstanceType)declaringType;
                if (type.HasGenericArguments)
                {
                    MethodAttributes attributes = MethodAttributes.RTSpecialName
                                                  | MethodAttributes.SpecialName
                                                  | MethodAttributes.Assembly;
                    MethodDefinition newConstructor = new MethodDefinition(".ctor", attributes, InstanceOfTypeSystem.Void);
                    foreach (ParameterDefinition param in constructor.Parameters)
                    {
                        if (param.ParameterType.IsGenericParameter)
                        {
                            int indexOfGenericType = 0;
                            foreach (GenericParameter genericParameter in type.ElementType.GenericParameters)
                            {
                                if (genericParameter.Name == param.ParameterType.Name)
                                {
                                    break;
                                }
                                else
                                {
                                    indexOfGenericType++;
                                }
                            }
                            if (indexOfGenericType < type.ElementType.GenericParameters.Count)
                            {
                                newConstructor.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, type.GenericArguments[indexOfGenericType]));
                            }
                            else
                            {
                                // this should not happen and will create an error in the generated code.
                                newConstructor.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, InstanceOfTypeSystem.Void));
                            }
                        }
                        else if (param.ParameterType.HasGenericParameters)
                        {
                            newConstructor.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, AnalysisUtils.MakeInstanceTypeFromTypeReference(param.ParameterType, type.GenericArguments.ToArray())));
                        }
                        else if (param.ParameterType.IsGenericInstance)
                        {
                            TypeReference paramElementType = ((GenericInstanceType)param.ParameterType).ElementType;
                            if (paramElementType.HasGenericParameters)
                            {
                                newConstructor.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, AnalysisUtils.MakeInstanceTypeFromTypeReference(paramElementType, type.GenericArguments.ToArray())));
                            }
                            else
                            {
                                newConstructor.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, param.ParameterType));
                            }
                        }
                        else
                        {
                            newConstructor.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, param.ParameterType));
                        }
                    }
                    return newConstructor;
                }
                else
                {
                    return constructor;
                }
            }
        }

        private bool TryGetConstructor(TypeReference type, out MethodDefinition constructor, bool returnConstructorIfAbstractType = false)
        {
            if (!returnConstructorIfAbstractType)
            {
                TypeDefinition td = AnalysisUtils.GetTypeDefinitionFromTypeReference(type, _modules);
                if (td == null || AnalysisUtils.IsTypeAbstract(td))
                {
                    constructor = null;
                    return false;
                }
            }
            HashSet<MethodDefinition> constructors;
            if (TryGetConstructors(type, out constructors))
            {
                constructor = ConvertConstructorToGenericInstanceMethod(GetConstructorWithMinimumParameters(constructors), type);
                return true;
            }
            // we should never be in this situation
            else
            {
                constructor = null;
                return false;
            }
        }

        private MethodDefinition GetConstructorWithMinimumParameters(HashSet<MethodDefinition> constructors)
        {
            if (constructors != null && constructors.Count > 0)
            {
                MethodDefinition bestCandidate = constructors.ElementAt(0);
                int numberOfParameters = constructors.ElementAt(0).Parameters.Count;
                foreach (MethodDefinition constructor in constructors)
                {
                    if (!constructor.HasParameters)
                    {
                        return constructor;
                    }
                    else
                    {
                        if (constructor.Parameters.Count < numberOfParameters)
                        {
                            bestCandidate = constructor;
                            numberOfParameters = constructor.Parameters.Count;
                        }
                    }
                }
                return bestCandidate;
            }
            else
            {
                return null;
            }
        }

        private int GetDistanceBetweenTwoMethodsParameters(MethodDefinition m1, MethodDefinition m2)
        {
            int dist = Math.Abs(m1.Parameters.Count - m2.Parameters.Count);
            int[] m2UsedParams = new int[m2.Parameters.Count];
            foreach (ParameterDefinition paramM1 in m1.Parameters)
            {
                int index = 0;
                foreach (ParameterDefinition paramM2 in m2.Parameters)
                {
                    if (paramM1.ParameterType.FullName == paramM2.ParameterType.FullName)
                    {
                        m2UsedParams[index] = 1;
                        break;
                    }
                    index++;
                }
            }
            dist += (m2UsedParams.Length - m2UsedParams.Sum());
            return dist;
        }

        private bool TryGetBaseConstructorMatchingBestThisContructor(MethodDefinition constructor, out MethodDefinition baseConstructor)
        {
            HashSet<MethodDefinition> constructorsIfAny;
            if (TryGetBaseConstructors(constructor, out constructorsIfAny))
            {
                baseConstructor = constructorsIfAny.ElementAt(0);
                if (!baseConstructor.HasParameters)
                {
                    return true;
                }
                int minDist = GetDistanceBetweenTwoMethodsParameters(constructor, baseConstructor);
                int dist;
                MethodDefinition currentConstructor;
                for (int i = 1; i < constructorsIfAny.Count; i++)
                {
                    currentConstructor = constructorsIfAny.ElementAt(i);
                    if (!currentConstructor.HasParameters)
                    {
                        baseConstructor = currentConstructor;
                        return true;
                    }
                    else
                    {
                        if ((dist = GetDistanceBetweenTwoMethodsParameters(constructor, currentConstructor)) < minDist)
                        {
                            minDist = dist;
                            baseConstructor = currentConstructor;
                        }
                    }
                }
                return true;
            }
            else
            {
                baseConstructor = null;
                return false;
            }
        }

        /// <summary>
        /// Try to generate a value for a given type that is not null (by calling the constructor). 
        /// It returns null only if we can't find a constructor to call or a know value to return 
        /// (like 0 for an int, string.Empty for a string ...).
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string GenerateDefaultValue(TypeReference type, bool isRecursiveCall = false)
        {
            MethodDefinition constructorIfAny;
            if (TryGetConstructor(type, out constructorIfAny))
            {
                string res = "new " + AnalysisUtils.GetFullTypeName(type, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName) + "(";
                bool isFirstParam = true;
                foreach (ParameterDefinition param in constructorIfAny.Parameters)
                {
                    if (!isFirstParam)
                    {
                        res += ", ";
                    }
                    else
                    {
                        isFirstParam = false;
                    }
                    res += GenerateDefaultValue(param.ParameterType, true);
                }
                res += ")";
                return res;
            }
            else
            {
                return AnalysisUtils.GetSafeTypeReferenceDefaultValueAsString(type, _outputOptions.OutputFullTypeName);
            }
        }

        private bool TryGetBaseConstructors(MethodDefinition constructor, out HashSet<MethodDefinition> baseContructors)
        {
            if (constructor.DeclaringType.BaseType != null)
            {
                return TryGetConstructors(constructor.DeclaringType.BaseType, out baseContructors);
            }
            else
            {
                baseContructors = null;
                return false;
            }
        }

        public string CallBaseConstructorIfAny(MethodDefinition constructor, TypeReference declaringType = null)
        {
            MethodDefinition baseConstructor;
            if (declaringType == null)
            {
                declaringType = constructor.DeclaringType;
            }
            if (TryGetBaseConstructorMatchingBestThisContructor(constructor, out baseConstructor))
            {
                if (!baseConstructor.HasParameters)
                {
                    return "";
                }
                else
                {
                    string res = " : base(";
                    bool isFirstParameter = true;
                    ParameterDefinition matchingParameter;
                    foreach (ParameterDefinition param in baseConstructor.Parameters)
                    {
                        matchingParameter = null;
                        if (!isFirstParameter)
                        {
                            res += ", ";
                        }
                        else
                        {
                            isFirstParameter = false;
                        }
                        if (AnalysisUtils.TryGetParameterMatchingVariableInMethodArguments(param.Name, param.ParameterType, constructor.Parameters, out matchingParameter))
                        {
                            res += "@" + matchingParameter.Name;
                        }
                        else
                        {
                            if (param.ParameterType.IsGenericParameter)
                            {
                                if (constructor.DeclaringType.BaseType.IsGenericInstance)
                                {
                                    int genericParameterIndex = -1;
                                    if (baseConstructor.DeclaringType.HasGenericParameters)
                                    {
                                        int index = 0;
                                        foreach (GenericParameter gp in baseConstructor.DeclaringType.GenericParameters)
                                        {
                                            if (gp.Name == param.ParameterType.Name)
                                            {
                                                genericParameterIndex = index;
                                                break;
                                            }
                                            index++;
                                        }
                                    }
                                    if (genericParameterIndex > -1)
                                    {
                                        res += GenerateDefaultValue(((GenericInstanceType)constructor.DeclaringType.BaseType).GenericArguments[genericParameterIndex]);
                                    }
                                }
                            }
                            else
                            {
                                res += GenerateDefaultValue(param.ParameterType);
                            }
                        }
                    }
                    res += ")";
                    return res;
                }
            }
            else
            {
                return "";
            }
        }

        public bool HasConstructor(TypeReference type, out MethodDefinition defaultConstructorIfNoConstructor)
        {
            HashSet<MethodDefinition> constructors;
            if (TryGetConstructors(type, out constructors))
            {
                if(constructors != null && constructors.Count == 1 && constructors.ElementAt(0).IsAssembly)
                {
                    defaultConstructorIfNoConstructor = constructors.ElementAt(0);
                    return false;
                }
                else
                {
                    defaultConstructorIfNoConstructor = null;
                    return true;
                }
            }
            else
            {
                defaultConstructorIfNoConstructor = null;
                return true;
            }
        }
    }
}
