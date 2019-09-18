using Mono.Cecil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace DotNetForHtml5.PrivateTools.AssemblyAnalysisCommon
{
    public static class AnalysisUtils
    {
        public const string NOT_IMPLEMENTED_EXCEPTION = "global::System.NotImplementedException";
        public const string NEW_NOT_IMPLEMENTED_EXCEPTION = "throw new " + NOT_IMPLEMENTED_EXCEPTION + "();";

        private static List<ModuleDefinition> _modules;

        public static void SetModules(List<ModuleDefinition> modules)
        {
            _modules = modules;
        }

        public static string GetFullTypeName(TypeReference type, bool addNamespace = true)
        {
            if (type == null)
            {
                return null;
            }
            else
            {
                string namespaceName = type.Namespace;
                string typeName = type.Name;
                if (type is GenericInstanceType)
                {
                    GenericInstanceType _type = (GenericInstanceType)type;
                    if (_type.HasGenericArguments)
                    {
                        Regex rgx = new Regex("`[0-9]");
                        typeName = rgx.Replace(typeName, "<");
                        bool isFirstGenericParameter = true;
                        foreach (TypeReference tr in _type.GenericArguments)
                        {
                            if (isFirstGenericParameter)
                            {
                                isFirstGenericParameter = false;
                            }
                            else
                            {
                                typeName += ", ";
                            }
                            typeName += GetFullTypeName(tr, addNamespace);
                        }
                        typeName += ">";
                    }
                }
                else
                {
                    if (type.HasGenericParameters)
                    {
                        Regex rgx = new Regex("`[0-9]");
                        typeName = rgx.Replace(typeName, "<");
                        bool isFirstGenericParameter = true;
                        foreach (GenericParameter gp in type.GenericParameters)
                        {
                            if (isFirstGenericParameter)
                            {
                                isFirstGenericParameter = false;
                            }
                            else
                            {
                                typeName += ", ";
                            }
                            typeName += gp.Name;
                        }
                        typeName += ">";
                    }
                }
                if (typeName.EndsWith("&"))
                {
                    typeName = typeName.TrimEnd('&');
                }
                string fullName = "";
                bool hasBeenFormated = FormatTypeName(ref typeName);
                if (string.IsNullOrEmpty(namespaceName) || !addNamespace || hasBeenFormated)
                {
                    fullName += typeName;
                }
                else
                {
                    fullName = namespaceName + "." + typeName;
                }
                return fullName;
            }
        }

        public static string GetFullTypeName(TypeReference type, Dictionary<string, string> typesThatNeedToBeRenamed, string[] typesThatNeedFullName, bool addNamespace = true)
        {
            if (type == null)
            {
                return null;
            }
            else
            {
                bool outputNamespace = addNamespace || typesThatNeedFullName.Contains(type.FullName);
                string namespaceName = type.Namespace;
                string typeName = type.Name;
                if (typesThatNeedToBeRenamed != null)
                {
                    string typeFullName = type.Namespace + "." + type.Name;
                    if (typesThatNeedToBeRenamed.ContainsKey(typeFullName))
                    {
                        typeName = typesThatNeedToBeRenamed[typeFullName];
                    }
                }
                if (type is GenericInstanceType)
                {
                    GenericInstanceType _type = (GenericInstanceType)type;
                    if (_type.HasGenericArguments)
                    {
                        Regex rgx = new Regex("`[0-9]");
                        string tmpName = rgx.Replace(typeName, "");
                        if(typeName != tmpName)
                        {
                            typeName = tmpName + "<";
                            bool isFirstGenericParameter = true;
                            foreach (TypeReference tr in _type.GenericArguments)
                            {
                                if (isFirstGenericParameter)
                                {
                                    isFirstGenericParameter = false;
                                }
                                else
                                {
                                    typeName += ", ";
                                }
                                typeName += GetFullTypeName(tr, typesThatNeedToBeRenamed, typesThatNeedFullName, addNamespace);
                            }
                            typeName += ">";
                        }
                    }
                }
                else
                {
                    if (type.HasGenericParameters)
                    {
                        Regex rgx = new Regex("`[0-9]");
                        typeName = rgx.Replace(typeName, "<");
                        bool isFirstGenericParameter = true;
                        foreach (GenericParameter gp in type.GenericParameters)
                        {
                            if (isFirstGenericParameter)
                            {
                                isFirstGenericParameter = false;
                            }
                            else
                            {
                                typeName += ", ";
                            }
                            typeName += gp.Name;
                        }
                        typeName += ">";
                    }
                }
                if (typeName.EndsWith("&"))
                {
                    typeName = typeName.TrimEnd('&');
                }
                string fullName = "";
                bool hasBeenFormated = FormatTypeName(ref typeName);
                if (String.IsNullOrEmpty(namespaceName) || !outputNamespace || hasBeenFormated)
                {
                    fullName += typeName;
                }
                else
                {
                    fullName = namespaceName + "." + typeName;
                }
                return fullName;
            }
        }

        public static bool IsMethodExplicitlyImplemented(MethodDefinition method)
        {
            return method.IsPrivate && method.IsVirtual && method.IsNewSlot;
        }

        public static bool IsMethodExplicitlyImplemented(MemberReference method)
        {
            if (method is MethodDefinition)
            {
                MethodDefinition methodDef = (MethodDefinition)method;
                return IsMethodExplicitlyImplemented(methodDef);
            }
            else if (method is PropertyDefinition)
            {
                PropertyDefinition propertyDef = (PropertyDefinition)method;
                MethodDefinition getMethod = propertyDef.GetMethod;
                MethodDefinition setMethod = propertyDef.SetMethod;
                if ((getMethod != null && !getMethod.IsPrivate) || (setMethod != null && !setMethod.IsPrivate))
                {
                    return false;
                }
                else
                {
                    if (getMethod != null)
                    {
                        return IsMethodExplicitlyImplemented(getMethod);
                    }
                    else
                    {
                        return IsMethodExplicitlyImplemented(setMethod);
                    }
                }
            }
            else if (method is EventDefinition)
            {
                EventDefinition eventDef = (EventDefinition)method;
                MethodDefinition addMethod = eventDef.AddMethod;
                MethodDefinition removeMethod = eventDef.RemoveMethod;
                if ((addMethod != null && !addMethod.IsPrivate) || (removeMethod != null && !removeMethod.IsPrivate))
                {
                    return false;
                }
                else
                {
                    if (addMethod != null)
                    {
                        return IsMethodExplicitlyImplemented(addMethod);
                    }
                    else
                    {
                        return IsMethodExplicitlyImplemented(removeMethod);
                    }
                }
            }
            else
            {
                return false;
            }
        }

        public static bool IsMethodVirtual(MethodDefinition method)
        {
            return method == null ? false : (method.IsVirtual && !method.IsReuseSlot && !method.IsFinal);
        }

        public static bool IsMethodVirtual(MemberReference member)
        {
            if (member is MethodDefinition)
            {
                return IsMethodVirtual((MethodDefinition)member);
            }
            else if (member is PropertyDefinition)
            {
                PropertyDefinition propertyDef = (PropertyDefinition)member;
                return (IsMethodVirtual(propertyDef.GetMethod) || IsMethodVirtual(propertyDef.SetMethod));
            }
            else if (member is EventDefinition)
            {
                EventDefinition eventDef = (EventDefinition)member;
                return (IsMethodVirtual(eventDef.AddMethod) || IsMethodVirtual(eventDef.RemoveMethod));
            }
            else
            {
                return false;
            }
        }

        public static bool IsMethodAbstract(MethodDefinition method)
        {
            return method == null ? false : method.IsAbstract;
        }

        public static bool IsMethodAbstract(MemberReference member)
        {
            if (member is MethodDefinition)
            {
                return IsMethodAbstract((MethodDefinition)member);
            }
            else if (member is PropertyDefinition)
            {
                PropertyDefinition propertyDef = (PropertyDefinition)member;
                return (IsMethodAbstract(propertyDef.GetMethod) || IsMethodAbstract(propertyDef.SetMethod));
            }
            else if (member is EventDefinition)
            {
                EventDefinition eventDef = (EventDefinition)member;
                return (IsMethodAbstract(eventDef.AddMethod) || IsMethodAbstract(eventDef.RemoveMethod));
            }
            else
            {
                return false;
            }
        }

        public static bool IsMethodOverride(MethodDefinition method)
        {
            if (method == null)
            {
                return false;
            }
            else
            {

                return (method.IsVirtual && method.IsReuseSlot);
            }
        }

        public static bool IsMethodOverride(MethodDefinition method, out bool isSealed)
        {
            isSealed = false;
            if (method == null)
            {
                return false;
            }
            else
            {
                bool isOverride = (method.IsVirtual && method.IsReuseSlot);
                if (isOverride && method.IsFinal)
                {
                    isSealed = true;
                }
                return isOverride;
            }
        }

        public static bool IsMethodOverride(MemberReference member, out bool isSealed)
        {
            if (member is MethodDefinition)
            {
                return IsMethodOverride((MethodDefinition)member, out isSealed);
            }
            else if (member is PropertyDefinition)
            {
                PropertyDefinition propertyDef = (PropertyDefinition)member;
                return (IsMethodOverride(propertyDef.GetMethod, out isSealed) || IsMethodOverride(propertyDef.SetMethod, out isSealed));
            }
            else if (member is EventDefinition)
            {
                EventDefinition eventDef = (EventDefinition)member;
                return (IsMethodOverride(eventDef.AddMethod, out isSealed) || IsMethodOverride(eventDef.RemoveMethod, out isSealed));
            }
            else
            {
                isSealed = false;
                return false;
            }
        }

        public static bool IsMethodInheritedFromAnInterface(MethodDefinition method)
        {
            return method.DeclaringType.HasInterfaces && method.IsFinal && method.IsVirtual && method.IsNewSlot;
        }

        public static bool IsMethodStatic(MethodDefinition method)
        {
            return method == null ? false : method.IsStatic;
        }

        public static bool IsMethodStatic(MemberReference member)
        {
            if (member is MethodDefinition)
            {
                return IsMethodStatic((MethodDefinition)member);
            }
            else if (member is PropertyDefinition)
            {
                PropertyDefinition propertyDef = (PropertyDefinition)member;
                return (IsMethodStatic(propertyDef.GetMethod) || IsMethodStatic(propertyDef.SetMethod));
            }
            else if (member is EventDefinition)
            {
                EventDefinition eventDef = (EventDefinition)member;
                return (IsMethodStatic(eventDef.AddMethod) || IsMethodStatic(eventDef.RemoveMethod));
            }
            else if (member is FieldDefinition)
            {
                return ((FieldDefinition)member).IsStatic;
            }
            else
            {
                return false;
            }
        }

        public static bool IsTypeStatic(TypeDefinition type)
        {
            return type.IsSealed && type.IsAbstract;
        }

        public static bool IsTypeSealed(TypeDefinition type)
        {
            return type.IsSealed && !type.IsAbstract;
        }

        public static bool IsTypeAbstract(TypeDefinition type)
        {
            return type.IsAbstract && !type.IsSealed;
        }

        public static TypeEnum GetTypeEnum(TypeDefinition type)
        {
            if (type == null)
            {
                return TypeEnum.None;
            }

            if (type.IsEnum)
            {
                return TypeEnum.Enum;
            }
            else if (type.IsValueType)
            {
                return TypeEnum.Struct;
            }
            else if (type.IsInterface)
            {
                return TypeEnum.Interface;
            }
            else if (IsTypeADelegate(type))
            {
                return TypeEnum.Delegate;
            }
            else
            {
                return TypeEnum.Class;
            }
        }

        public static string TypeEnumToString(TypeEnum type)
        {
            switch (type)
            {
                case TypeEnum.Class:
                    return "class";
                case TypeEnum.Struct:
                    return "struct";
                case TypeEnum.Enum:
                    return "enum";
                case TypeEnum.Interface:
                    return "interface";
                case TypeEnum.Delegate:
                    return "delegate";
                default:
                    return "";
            }
        }

        public static AccessModifierEnum GetMethodAccessModifier(MethodDefinition method)
        {
            if (method == null)
            {
                return AccessModifierEnum.NONE;
            }

            if (method.IsFamily)
            {
                return AccessModifierEnum.PROTECTED;
            }
            else if (method.IsAssembly)
            {
                return AccessModifierEnum.INTERNAL;
            }
            else if (method.IsFamilyOrAssembly)
            {
                return AccessModifierEnum.PROTECTEDINTERNAL;
            }
            else if (method.IsPrivate)
            {
                return AccessModifierEnum.PRIVATE;
            }
            else
            {
                return AccessModifierEnum.PUBLIC;
            }
        }

        public static AccessModifierEnum GetMethodAccessModifier(MemberReference method)
        {
            if (method is MethodDefinition)
            {
                return GetMethodAccessModifier((MethodDefinition)method);
            }
            else if (method is PropertyDefinition)
            {
                PropertyDefinition propertyDef = (PropertyDefinition)method;
                AccessModifierEnum getMethodAM = GetMethodAccessModifier(propertyDef.GetMethod);
                AccessModifierEnum setMethodAM = GetMethodAccessModifier(propertyDef.SetMethod);
                return (getMethodAM >= setMethodAM ? getMethodAM : setMethodAM);
            }
            else if (method is EventDefinition)
            {
                EventDefinition eventDef = (EventDefinition)method;
                AccessModifierEnum addMethodAM = GetMethodAccessModifier(eventDef.AddMethod);
                AccessModifierEnum removeMethodAM = GetMethodAccessModifier(eventDef.RemoveMethod);
                return (addMethodAM >= removeMethodAM ? addMethodAM : removeMethodAM);
            }
            else if (method is FieldDefinition)
            {
                FieldDefinition field = (FieldDefinition)method;
                if (field.IsPublic)
                {
                    return AccessModifierEnum.PUBLIC;
            }
                else
                {
                    return AccessModifierEnum.PRIVATE;
                }
            }
            else
            {
                return AccessModifierEnum.NONE;
            }
        }

        public static AccessModifierEnum GetTypeAccessModifier(TypeDefinition type)
        {
            if (type.IsNestedFamily)
            {
                return AccessModifierEnum.PROTECTED;
            }
            else if (type.IsNestedAssembly)
            {
                return AccessModifierEnum.INTERNAL;
            }
            else if (type.IsNestedPrivate)
            {
                return AccessModifierEnum.PRIVATE;
            }
            else
            {
                return AccessModifierEnum.PUBLIC;
            }
        }

        public static string AccessModifierEnumToString(AccessModifierEnum accessModifier, bool showAccessModifier = true)
        {
            if (!showAccessModifier)
            {
                return "";
            }
            switch (accessModifier)
            {
                case AccessModifierEnum.PUBLIC:
                    return "public";
                case AccessModifierEnum.PROTECTED:
                    return "protected";
                case AccessModifierEnum.INTERNAL:
                    return "internal";
                case AccessModifierEnum.PROTECTEDINTERNAL:
                    return "protected internal";
                case AccessModifierEnum.PRIVATE:
                    return "private";
                default:
                    return "";
            }
        }

        public static string IsMethodAnOperator(MethodDefinition methodDefinition)
        {
            switch (methodDefinition.Name)
            {
                case "op_UnaryPlus":
                    return "+";

                case "op_UnaryNegation":
                    return "-";

                case "op_LogicalNot":
                    return "!";

                case "op_OnesComplement":
                    return "~";

                case "op_Increment":
                    return "++";

                case "op_Decrement":
                    return "--";

                case "op_True":
                    return "true";

                case "op_False":
                    return "false";

                case "op_Addition":
                    return "+";

                case "op_Subtraction":
                    return "-";

                case "op_Multiply":
                    return "*";

                case "op_Division":
                    return "/";

                case "op_Modulus":
                    return "%";

                case "op_BitwiseAnd":
                    return "&";

                case "op_BitwiseOr":
                    return "|";

                case "op_ExclusiveOr":
                    return "^";

                case "op_LeftShift":
                    return "<<";

                case "op_RightShift":
                    return ">>";

                case "op_Equality":
                    return "==";

                case "op_Inequality":
                    return "!=";

                case "op_GreaterThan":
                    return ">";

                case "op_LessThan":
                    return "<";

                case "op_GreaterThanOrEqual":
                    return ">=";

                case "op_LessThanOrEqual":
                    return "<=";
            }

            return null;
        }

        public static bool IsTypeADelegate(TypeDefinition typeDefinition)
        {
            return (typeDefinition.BaseType != null && typeDefinition.BaseType.FullName == "System.MulticastDelegate");
        }

        public static bool IsTypeAnArray(TypeReference typeReference)
        {
            if (typeReference.IsArray)
            {
                if (typeReference.FullName.Contains('[') && typeReference.FullName.Contains(']'))
                {
                    if (typeReference.FullName.IndexOf('[') < typeReference.FullName.LastIndexOf(']'))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static string FormatArray(string array)
        {
            if (array == null)
                return null;

            int commaCount = array.Count(c => c == ',');

            if (commaCount > 0)
            {
                array = "[";
                for (int i = 0; i < commaCount; ++i)
                {
                    array += ',';
                }
                array += ']';
            }
            else
                array = "[]";

            return array;
        }

        public static string GetFormatedArrayFromFullTypeName(string fullTypeName)
        {
            if (fullTypeName == null)
                return null;

            int firstBracketID = fullTypeName.IndexOf('[');
            if (firstBracketID != -1)
            {
                string array = fullTypeName.Remove(0, firstBracketID);
                int secondBracketID = array.IndexOf(']', 0);

                if (secondBracketID != -1) // Remove the '&' character if present.
                    array = array.Remove(secondBracketID, array.Length - secondBracketID);

                return FormatArray(array);
            }

            return null;
        }

        public static string RemoveArrayFromTypeName(string typeName)
        {
            if (typeName == null)
                return null;

            int firstBracketID = typeName.IndexOf('[');
            if (firstBracketID != -1)
            {
                int secondBracketID = typeName.IndexOf(']');
                if (secondBracketID != -1)
                {
                    return typeName.Remove(firstBracketID, secondBracketID - firstBracketID + 1);
                }
            }

            return typeName;
        }


        private static Dictionary<string, string> DefaultValueOfSystemTypes = new Dictionary<string, string>()
        {
            {"System.Double", "0.0D"},
            {"System.String", "\"\""},
            {"System.Int16", "0"},
            {"System.Int32", "0"},
            {"System.Int64", "0L"},
            {"System.IntPtr", "0"},
            {"System.Boolean", "false"},
            {"System.Single", "0"},
            {"System.Byte", "0"},
            {"System.Char", "'\0'"},
            {"System.Decimal", "0M"},
            {"System.Float", "0.0F"},
            {"System.SByte", "0"},
            {"System.UInt16", "0"},
            {"System.UInt32", "0"},
            {"System.UInt64", "0"},
            {"System.UIntPtr", "0"},
            {"System.Object", "null"},
            {"System.Void", null},
        };

        public static bool IsSystemType(TypeReference type)
        {
            return DefaultValueOfSystemTypes.ContainsKey(type.FullName);
        }

        public static bool IsSystemType(string typeFullName)
        {
            return DefaultValueOfSystemTypes.ContainsKey(typeFullName);
        }

        public static string GetDefaultValueOfSystemTypeAsString(TypeReference type)
        {
            return GetDefaultValueOfSystemTypeAsString(type.FullName);
        }

        public static string GetDefaultValueOfSystemTypeAsString(string typeFullName)
        {
            if (IsSystemType(typeFullName))
            {
                return DefaultValueOfSystemTypes[typeFullName];
            }
            else
            {
                throw new Exception("\"" + typeFullName + "\" is not a System type.");
            }
        }

        public static string GetDefaultValueOfArrayTypeAsString(TypeReference type, bool outputFullTypeName = true)
        {
            if (!IsTypeAnArray(type))
            {
                throw new Exception("\"" + type.FullName + "\"" + " is not an array.");
            }
            else
            {
                int indexOfFirstOpeningSquareBracket = type.Name.IndexOf('[');
                string elementTypeAsString = GetFullTypeName(type.GetElementType(), outputFullTypeName);
                string arrayExtension = "";
                int indexOfFirstClosingSquareBracket = type.Name.IndexOf(']');
                string contentBetweenFirstPairOfBrackets = type.Name.Substring(indexOfFirstOpeningSquareBracket + 1, indexOfFirstClosingSquareBracket - indexOfFirstOpeningSquareBracket - 1);
                arrayExtension += "[";
                foreach (char c in contentBetweenFirstPairOfBrackets)
                {
                    if (c == ',')
                    {
                        arrayExtension += "0, ";
                    }
                }

                arrayExtension += "0]";
                if (indexOfFirstClosingSquareBracket < type.Name.Length - 1)
                {
                    arrayExtension += type.Name.Substring(indexOfFirstClosingSquareBracket + 1);
                }
                return "new " + elementTypeAsString + arrayExtension;
            }
        }

        public static bool IsDefaultValueType(string typeName)
        {
            switch (typeName)
            {
                case "System.Void":
                    return true;

                case "System.Char":
                    return true;

                case "System.Int16":
                    return true;

                case "System.Int32":
                    return true;

                case "System.Int64":
                    return true;

                case "System.IntPtr":
                    return true;

                case "System.UInt16":
                    return true;

                case "System.UInt32":
                    return true;

                case "System.UInt64":
                    return true;

                case "System.UIntPtr":
                    return true;

                case "System.Double":
                    return true;

                case "System.Single":
                    return true;

                case "System.Boolean":
                    return true;

                case "System.Byte":
                    return true;

                case "System.SByte":
                    return true;

                case "System.CodeDom.Compiler.CodeDomProvider":
                    return true;

                case "System.Object":
                    return true;

                case "System.String":
                    return true;

                case "Accessibility.IAccessible":
                    return true;

                default:
                    return false;
            }
        }

        public static bool FormatTypeName(string typeName, out string formattedTypeName)
        {
            switch (typeName)
            {
                case "Void":
                    formattedTypeName = "void";
                    return true;

                case "Char":
                    formattedTypeName = "char";
                    return true;

                case "Int16":
                    formattedTypeName = "short";
                    return true;

                case "Int32":
                    formattedTypeName = "int";
                    return true;

                case "Int64":
                    formattedTypeName = "long";
                    return true;

                case "IntPtr":
                    formattedTypeName = "System.IntPtr";
                    return true;

                case "UInt16":
                    formattedTypeName = "ushort";
                    return true;

                case "UInt32":
                    formattedTypeName = "uint";
                    return true;

                case "UInt64":
                    formattedTypeName = "ulong";
                    return true;

                case "UIntPtr":
                    formattedTypeName = "System.UIntPtr";
                    return true;

                case "Double":
                    formattedTypeName = "double";
                    return true;

                case "Single":
                    formattedTypeName = "float";
                    return true;

                case "Boolean":
                    formattedTypeName = "bool";
                    return true;

                case "Byte":
                    formattedTypeName = "byte";
                    return true;

                case "SByte":
                    formattedTypeName = "sbyte";
                    return true;

                case "CodeDomProvider":
                    formattedTypeName = "object";
                    return true;

                case "Object":
                    formattedTypeName = "object";
                    return true;

                case "String":
                    formattedTypeName = "string";
                    return true;

                case "IAccessible":
                    formattedTypeName = "__IDontImplement__";
                    return true;

                default:
                    formattedTypeName = typeName;
                    return false;
            }
        }

        public static bool FormatTypeName(ref string typeName)
        {
            switch (typeName)
            {
                case "Void":
                    typeName = "void";
                    return true;

                case "Char":
                    typeName = "char";
                    return true;

                case "Int16":
                    typeName = "short";
                    return true;

                case "Int32":
                    typeName = "int";
                    return true;

                case "Int64":
                    typeName = "long";
                    return true;

                case "IntPtr":
                    typeName = "System.IntPtr";
                    return true;

                case "UInt16":
                    typeName = "ushort";
                    return true;

                case "UInt32":
                    typeName = "uint";
                    return true;

                case "UInt64":
                    typeName = "ulong";
                    return true;

                case "UIntPtr":
                    typeName = "System.UIntPtr";
                    return true;

                case "Double":
                    typeName = "double";
                    return true;

                case "Single":
                    typeName = "float";
                    return true;

                case "Boolean":
                    typeName = "bool";
                    return true;

                case "Byte":
                    typeName = "byte";
                    return true;

                case "SByte":
                    typeName = "sbyte";
                    return true;

                case "CodeDomProvider":
                    typeName = "object";
                    return true;

                case "Object":
                    typeName = "object";
                    return true;

                case "String":
                    typeName = "string";
                    return true;

                case "IAccessible":
                    typeName = "__IDontImplement__";
                    return true;

                default:
                    return false;
            }
        }

        public static TypeReference MakeInstanceTypeFromTypeReference(TypeReference genericType, TypeReference[] genericArguments)
        {
            if (genericType.GenericParameters.Count != genericArguments.Length)
            {
                throw new ArgumentException();
            }
            GenericInstanceType instance = new GenericInstanceType(genericType);
            foreach (var arg in genericArguments)
            {
                instance.GenericArguments.Add(arg);
            }
            return instance;
        }

        public static bool TryGetConstructors(TypeReference type, List<ModuleDefinition> modules, out HashSet<MethodDefinition> constructors, out bool isTypeWithNoConstructor)
        {
            constructors = new HashSet<MethodDefinition>();
            isTypeWithNoConstructor = false;
            if (modules == null)
            {
                modules = _modules;
            }
            TypeDefinition typeDefinition = GetTypeDefinitionFromTypeReference(type, modules);
            if (typeDefinition == null || IsSystemType(type) || IsTypeAnArray(type) || (int)GetTypeEnum(typeDefinition) > 1)
            {
                isTypeWithNoConstructor = true;
                return false;
            }
            foreach (MethodDefinition method in typeDefinition.Methods)
            {
                if (method.IsConstructor && (method.IsPublic || method.IsFamily))
                {
                    constructors.Add(method);
        }
            }
            // At this point we can create a constructor because we know that type eather is a class or a struct.
            if (constructors.Count == 0)
            {
                MethodDefinition defaultConstructor = GenerateDefaultConstructor(typeDefinition);
                if (defaultConstructor != null)
                {
                    constructors.Add(defaultConstructor);
                }
            }
            return constructors.Count > 0;
        }

        private static MethodDefinition GenerateDefaultConstructor(TypeDefinition type)
        {
            MethodDefinition constructor;
            if (IsTypeStatic(type))
            {
                constructor = null;
            }
            else
            {
                MethodAttributes attributes = MethodAttributes.RTSpecialName
                                                   | MethodAttributes.SpecialName
                                                   | MethodAttributes.Assembly;
                constructor = new MethodDefinition(".ctor", attributes, type.Module.TypeSystem.Void)
                {
                    DeclaringType = type,
                };
            }
            return constructor;
        }

        public static string GetSafeTypeReferenceDefaultValueAsString(TypeReference type, bool outputFullName = true)
        {
            if (IsSystemType(type))
            {
                return GetDefaultValueOfSystemTypeAsString(type);
            }
            else if (IsTypeAnArray(type))
            {
                return GetDefaultValueOfArrayTypeAsString(type, outputFullName);
            }
            else
            {
                bool isValueType;
                return GetSafeTypeReferenceDefaultValueAsString(type, out isValueType, _modules, outputFullName);
            }
        }

        public static string GetSafeTypeReferenceDefaultValueAsString(TypeReference typeReference, out bool isValueType, List<ModuleDefinition> modules, bool outputFullName = true)
        {
            if (modules == null)
            {
                modules = _modules;
            }
            isValueType = false;
            if (typeReference == null)
            {
                return null;
            }

            string formatedTypeName = typeReference.Name;
            if (formatedTypeName.Contains('&')) // out or reference type.
            {
                formatedTypeName = formatedTypeName.Remove(formatedTypeName.Length - 1, 1);
            }

            if (IsTypeAnArray(typeReference))
            {
                return "null";
            }

            if (typeReference.IsGenericParameter)
            {
                return "default (" + typeReference.Name + ")";
            }

            if (FormatTypeName(ref formatedTypeName))
            {
                switch (formatedTypeName)
                {
                    case "short":
                        return default(short).ToString();

                    case "int":
                        return default(int).ToString();

                    case "long":
                        return default(long).ToString();

                    case "System.IntPtr":
                        isValueType = true;
                        return "System.IntPtr";

                    case "ushort":
                        return default(ushort).ToString();

                    case "uint":
                        return default(uint).ToString();

                    case "ulong":
                        return default(ulong).ToString();

                    case "System.UIntPtr":
                        isValueType = true;
                        return "System.UIntPtr";

                    case "double":
                        return default(double).ToString();

                    case "float":
                        return default(float).ToString();

                    case "byte":
                        return default(byte).ToString();

                    case "sbyte":
                        return default(sbyte).ToString();

                    case "bool":
                        return "false";

                    case "string":
                        return "null";

                    case "object":
                        return "null";

                    case "void":
                        return null;
                }
            }

            string safeFullTypeName = null;
            TypeDefinition asTypeDefinition = GetTypeDefinitionFromTypeReference(typeReference, modules);

            if (typeReference.IsValueType)
            {
                isValueType = true;
                safeFullTypeName = GetFullTypeName(typeReference, outputFullName);
            }
            // TypeReference is not considered as a value type (struct) but it is one, so check it using typeDefinition.
            else if (!typeReference.IsValueType && asTypeDefinition != null && asTypeDefinition.IsValueType)
            {
                isValueType = true;
                safeFullTypeName = GetFullTypeName(typeReference, outputFullName);
            }
            else
                return "null";

            if (typeReference.FullName.EndsWith("&") && safeFullTypeName != null) // Is a ref or out parameter, remove the 'ref ' or 'out ' added by the  StubGeneratorTypeNameOperations.GetSafeFullTypeName(typeReference).
            {
                safeFullTypeName = safeFullTypeName.TrimStart("ref ".ToCharArray());
                safeFullTypeName = safeFullTypeName.TrimStart("out ".ToCharArray());
            }

            return "new " + safeFullTypeName + "()";
        }
        public static string RemoveBracketAndElementsBetweenFromFullTypeName(string typeName)
        {
            if (typeName == null || !typeName.Contains('<') || !typeName.Contains('>'))
            {
                return typeName;
            }

            int firstBracketID = typeName.IndexOf('<');
            return typeName.Remove(firstBracketID, typeName.Length - firstBracketID);
        }

        public static string GetGenericTypeNameFromTypeName(string typeName)
        {
            int genericParametersCount = 0;
            int openLeftBracketsCount = 0;
            int firstIndexOfLeftBracket = typeName.IndexOf('<');
            string resultName = "";
            if (firstIndexOfLeftBracket > -1)
            {
                genericParametersCount++;
                resultName = typeName.Substring(0, firstIndexOfLeftBracket);
                string inBetweenBrackets = typeName.Substring(firstIndexOfLeftBracket + 1, typeName.Length - firstIndexOfLeftBracket - 2);
                foreach (char c in inBetweenBrackets)
                {
                    if (c == '<')
                    {
                        openLeftBracketsCount++;
                    }
                    else if (c == '>')
                    {
                        openLeftBracketsCount--;
                    }
                    else if (c == ',')
                    {
                        if (openLeftBracketsCount == 1)
                        {
                            genericParametersCount++;
                        }
                    }
                }
                resultName += "`" + genericParametersCount.ToString() + '<' + inBetweenBrackets + '>';
            }
            else
            {
                resultName = typeName;
            }
            return resultName;
        }

        public static TypeDefinition GetTypeDefinitionFromModules(string fullTypeName, List<ModuleDefinition> modules)
        {
            if (modules == null)
            {
                modules = _modules;
            }

            if (fullTypeName == null)
                return null;

            TypeDefinition typeDefinition = null;
            int modulesCount = modules != null ? modules.Count : 0;
            for (int i = 0; i < modulesCount; ++i)
            {
                if ((typeDefinition = modules[i].GetType(RemoveBracketAndElementsBetweenFromFullTypeName(fullTypeName))) != null)
                    return typeDefinition;
            }

            return null;
        }

        public static TypeDefinition GetTypeDefinitionFromTypeReference(TypeReference typeReference, List<ModuleDefinition> modules)
        {
            if (modules == null)
            {
                modules = _modules;
            }

            if (typeReference == null)
                return null;

            TypeDefinition typeDefinition = null;
            try
            {
                typeDefinition = typeReference.Resolve();
            }
            catch (NotSupportedException)
            {
                return GetTypeDefinitionFromModules(typeReference.FullName, modules);
            }
            catch (AssemblyResolutionException)
            {
                return GetTypeDefinitionFromModules(typeReference.FullName, modules);
            }

            if (typeDefinition == null)
                return GetTypeDefinitionFromModules(typeReference.FullName, modules);

            return typeDefinition;
        }

        public static MethodDefinition LookForMethodInParents(MethodDefinition method, TypeDefinition currentType)
        {
            TypeDefinition baseType = GetTypeDefinitionFromTypeReference(currentType.BaseType, null);
            if (baseType != null)
            {
                if (baseType.HasMethods)
                {
                    foreach (MethodDefinition m in baseType.Methods)
                    {
                        if (IsSameSignature(method, m))
                        {
                            if (IsMethodAbstract(m) || IsMethodVirtual(m))
                            {
                                return m;
                            }
                        }
                    }
                }
                return LookForMethodInParents(method, baseType);
            }
            else
            {
                return null;
            }
        }

        public static string GetProgramFilesX86Path()
        {
            // Credits: http://stackoverflow.com/questions/194157/c-sharp-how-to-get-program-files-x86-on-windows-vista-64-bit

            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        public static bool DoesMethodsHaveSameParameters(MethodDefinition firstMethod, MethodDefinition secondMethod)
        {
            if (firstMethod.HasParameters == false && secondMethod.HasParameters == false)
                return true;

            if (firstMethod.HasParameters && secondMethod.HasParameters == false || firstMethod.HasParameters == false && secondMethod.HasParameters)
                return false;

            if (firstMethod.Parameters.Count != secondMethod.Parameters.Count)
                return false;

            for (int i = 0; i < firstMethod.Parameters.Count; ++i)
            {
                if (firstMethod.Parameters[i].ParameterType.FullName != secondMethod.Parameters[i].ParameterType.FullName)
                    return false;
            }

            return true;
        }

        public static string GetMethodName(string name, char separator)
        {
            string[] methodNameSplitedOverDots = name.Split(separator);
            return methodNameSplitedOverDots[methodNameSplitedOverDots.Length - 1];
        }

        public static bool IsSameSignature(MethodDefinition method1, MethodDefinition method2)
        {
            //todo: compare the type of the parameters too.
            bool isSameReturnType = method1.ReturnType.GetType().Equals(method2.ReturnType.GetType());
            bool isSameName = GetMethodName(method1.Name, '.') == GetMethodName(method2.Name, '.');
            bool isSameParameters = method1.Parameters.Count == method2.Parameters.Count;
            return isSameReturnType && isSameName && isSameParameters;
        }

        public static string Indent(int tabulation)
        {
            string res = "";
            for (int i = 0; i < tabulation; i++)
            {
                res += "    ";
            }
            return res;
        }

        /// <summary>
        ///     Calculate the difference between 2 strings using the Levenshtein distance algorithm
        /// </summary>
        /// <param name="source1">First string</param>
        /// <param name="source2">Second string</param>
        /// <returns></returns>
        public static int LevenstheinDistance(string source1, string source2) //O(n*m)
        {
            int source1Length = source1.Length;
            int source2Length = source2.Length;

            int[,] matrix = new int[source1Length + 1, source2Length + 1];

            // First calculation, if one entry is empty return full length
            if (source1Length == 0)
            {
                return source2Length;
            }

            if (source2Length == 0)
            {
                return source1Length;
            }

            // Initialization of matrix with row size source1Length and columns size source2Length
            for (int i = 0; i <= source1Length; matrix[i, 0] = i++) { }
            for (int j = 0; j <= source2Length; matrix[0, j] = j++) { }

            // Calculate rows and collumns distances
            for (int i = 1; i <= source1Length; i++)
            {
                for (int j = 1; j <= source2Length; j++)
                {
                    int cost = (source2[j - 1] == source1[i - 1]) ? 0 : 1;

                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }
            return matrix[source1Length, source2Length];
        }

        public static bool TryGetParameterMatchingVariableInMethodArguments(FieldDefinition field, Collection<ParameterDefinition> methodArgs, out ParameterDefinition match)
        {
            return TryGetParameterMatchingVariableInMethodArguments(field.Name, field.FieldType, methodArgs, out match);
        }

        public static bool TryGetParameterMatchingVariableInMethodArguments(string name, TypeReference type, Collection<ParameterDefinition> methodArgs, out ParameterDefinition match)
        {
            match = null;
            if (methodArgs == null || methodArgs.Count == 0)
            {
                return false;
            }
            else
            {
                int minDist = int.MaxValue;
                foreach (ParameterDefinition param in methodArgs)
                {
                    if (param.ParameterType.FullName == type.FullName)
                    {
                        int dist = LevenstheinDistance(name, param.Name);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            match = param;
                        }
                    }
                }
                return (match != null);
            }
        }

    }
}
