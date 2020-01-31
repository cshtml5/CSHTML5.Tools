using DotNetForHtml5.PrivateTools.AssemblyAnalysisCommon;
using Mono.Cecil;
using StubGenerator.Common.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace StubGenerator.Common.Builder
{
    public class TypeBuilder
    {
        private HashSet<MethodInfo> _methods;
        private HashSet<PropertyInfo> _properties;
        private HashSet<EventDefinition> _events;
        private HashSet<FieldInfo> _fields;

        private List<TypeDefinition> _nestedTypes;

        private HashSet<string> _usings;
        private HashSet<TypeReference> _implementedInterfaces;
        private List<CustomAttribute> _customAttributes;
        private HashSet<string> _codeToAddManually;

        private DefaultValueGenerator _defaultValueGenerator;

        private int _indentation;
        private string _currentDirectory;

        private OutputOptions _outputOptions;
        private TypeEnum _typeEnum;
        private int _suffixToAddToVariableNameInCaseOfDuplicate;
        private bool _hasAnIndexer = false;

        /// <summary>
        /// Used to generate the unsupported classes and methods
        /// </summary>
        /// <param name="outputOptions"></param>
        /// <param name="modules"></param>
        public TypeBuilder(OutputOptions outputOptions, List<ModuleDefinition> modules)
        {
            _outputOptions = outputOptions;
            _suffixToAddToVariableNameInCaseOfDuplicate = 1;
            _defaultValueGenerator = new DefaultValueGenerator(outputOptions, modules);
        }

        public void Set(TypeDefinition typeToWrite, HashSet<string> additionalCodeIfAny, HashSet<TypeReference> implementedInterfaces, bool hasAnIndexer, string directoryPath, int indentation)
        {
            Type = typeToWrite;
            ImplementedInterfaces = implementedInterfaces;
            CodeToAddManually = additionalCodeIfAny;
            _hasAnIndexer = hasAnIndexer;
            _currentDirectory = directoryPath;
            _indentation = indentation;
        }

        internal TypeDefinition Type { get; private set; }

        internal HashSet<MethodInfo> Methods
        {
            get
            {
                if (_methods == null)
                {
                    _methods = new HashSet<MethodInfo>();
                }
                return _methods;
            }
            private set
            {
                _methods = value;
            }
        }

        internal HashSet<PropertyInfo> Properties
        {
            get => _properties ?? (_properties = new HashSet<PropertyInfo>());
            private set => _properties = value;
        }

        internal HashSet<EventDefinition> Events
        {
            get
            {
                if (_events == null)
                {
                    _events = new HashSet<EventDefinition>();
                }
                return _events;
            }
            private set
            {
                _events = value;
            }
        }

        internal HashSet<FieldInfo> Fields
        {
            get
            {
                if (_fields == null)
                {
                    _fields = new HashSet<FieldInfo>();
                }
                return _fields;
            }
            private set
            {
                _fields = value;
            }
        }

        internal List<TypeDefinition> NestedTypes
        {
            get
            {
                if (_nestedTypes == null)
                {
                    _nestedTypes = new List<TypeDefinition>();
                }
                return _nestedTypes;
            }
            private set
            {
                _nestedTypes = value;
            }
        }

        internal HashSet<string> Usings
        {
            get
            {
                if (_usings == null)
                {
                    _usings = new HashSet<string>();
                }
                return _usings;
            }
            private set
            {
                _usings = value;
            }
        }

        internal HashSet<TypeReference> ImplementedInterfaces
        {
            get
            {
                if (_implementedInterfaces == null)
                {
                    _implementedInterfaces = new HashSet<TypeReference>();
                }
                return _implementedInterfaces;
            }
            private set
            {
                _implementedInterfaces = value;
            }
        }

        internal List<CustomAttribute> CustomAttributes
        {
            get
            {
                if (_customAttributes == null)
                {
                    _customAttributes = new List<CustomAttribute>();
                }
                return _customAttributes;
            }
            private set
            {
                _customAttributes = value;
            }
        }

        internal HashSet<string> CodeToAddManually
        {
            get
            {
                if (_codeToAddManually == null)
                {
                    _codeToAddManually = new HashSet<string>();
                }
                return _codeToAddManually;
            }
            private set
            {
                _codeToAddManually = value;
            }
        }

        private TypeEnum TypeEnum
        {
            get
            {
                if (_typeEnum == TypeEnum.None)
                {
                    if (Type != null)
                    {
                        _typeEnum = AnalysisUtils.GetTypeEnum(Type);
                    }
                }
                return _typeEnum;
            }
            set
            {
                _typeEnum = value;
            }
        }

        internal void AddMethod(MethodInfo method)
        {
            Methods.Add(method);
        }

        internal void AddField(FieldInfo field)
        {
            Fields.Add(field);
        }

        internal bool IsFieldNameTaken(string fieldName, out string newName)
        {
            newName = fieldName;
            foreach (FieldInfo field in Fields)
            {
                if (field.Field.Name == fieldName)
                {
                    newName = fieldName + _suffixToAddToVariableNameInCaseOfDuplicate++;
                    return true;
                }
            }
            return false;
        }

        internal void AddProperty(PropertyInfo property)
        {
            Properties.Add(property);
        }

        internal void AddEvent(EventDefinition @event)
        {
            Events.Add(@event);
        }

        internal void AddUsing(string @using)
        {
            Usings.Add(@using);
        }

        internal void AddCode(string codeToAdd)
        {
            CodeToAddManually.Add(codeToAdd);
        }

        // ---------------------------------------------------------- //

        // ----------------------- FIELD SECTION --------------------- //

        private string WriteField(FieldInfo field, string declaringTypeAsString, int indentation)
        {
            string res = AnalysisUtils.Indent(indentation);
            if (!field.IsDependencyProperty)
            {
                res += "private " + (field.Field.IsStatic ? "static " : "") + AnalysisUtils.GetFullTypeName(field.Field.FieldType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName)
                    + " " + field.Field.Name + ";\n";
            }
            else
            {
                res += "public static readonly DependencyProperty " + field.Field.Name + " = DependencyProperty.Register" + (field.IsAttachedProperty ? "Attached" : "") + "(\""
                    + field.PropertyNameIfDependencyProperty + "\", typeof(" + AnalysisUtils.GetFullTypeName(field.DependencyPropertyTypeIfAny, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName) + ")"
                    + ", typeof(" + declaringTypeAsString + "), new System.Windows.PropertyMetadata());\n";
            }
            return res;
        }

        private string WriteFields(HashSet<FieldInfo> fields, TypeReference declaringType, int indentation)
        {
            if (fields.Count == 0)
            {
                return "";
            }
            string indent = AnalysisUtils.Indent(indentation);
            string res = "";
            res += indent + "#region Fields" + "\n";
            string declaringTypeAsString = AnalysisUtils.GetFullTypeName(declaringType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName);
            foreach (FieldInfo field in fields)
            {
                res += WriteField(field, declaringTypeAsString, indentation);
            }
            res += indent + "#endregion" + "\n\n";
            return res;
        }

        // ----------------------------------------------------- //

        // --------------- PROPERTY SECTION -------------------- //

        private string WriteProperty(PropertyInfo propertyInfo, TypeEnum typeEnum, int indentation)
        {
            PropertyDefinition property = propertyInfo.Property;
            bool isPropertyAbstract = false;
            bool isExplicitImplementation = AnalysisUtils.IsMethodExplicitlyImplemented(property);
            string res = AnalysisUtils.Indent(indentation);
            if (typeEnum != TypeEnum.Interface)
            {

                string accessModifier = AnalysisUtils.AccessModifierEnumToString(AnalysisUtils.GetMethodAccessModifier(property), !isExplicitImplementation);
                res += accessModifier + (isExplicitImplementation ? "" : " ");
                if (AnalysisUtils.IsMethodStatic(property))
                {
                    res += "static ";
                }
                else
                {
                    bool isMethodSealedIfOverride;
                    if (AnalysisUtils.IsMethodAbstract(property))
                    {
                        isPropertyAbstract = true;
                        res += "abstract ";
                    }
                    else if (AnalysisUtils.IsMethodVirtual(property))
                    {
                        res += "virtual ";
                    }
                    else if (AnalysisUtils.IsMethodOverride(property, out isMethodSealedIfOverride))
                    {
                        res += "override ";
                        if (isMethodSealedIfOverride)
                        {
                            res += "sealed ";
                        }
                    }
                }
            }
            string returnType = AnalysisUtils.GetFullTypeName(property.PropertyType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName);
            res += returnType + " ";
            string name = "";
            if (isExplicitImplementation)
            {
                name += property.Name.Substring(0, property.Name.LastIndexOf('.') + 1);
            }
            string elementName = isExplicitImplementation ? property.Name.Substring(property.Name.LastIndexOf('.') + 1) : property.Name;
            if (_hasAnIndexer && elementName == "Item")
            {
                name += "this";
                res += name;
                res += "[";
                if (property.HasParameters)
                {
                    bool isFirstParameter = true;
                    int numberOfParam = property.Parameters.Count;
                    ParameterDefinition param;
                    for (int i = 0; i < numberOfParam; i++)
                    {
                        param = property.Parameters[i];
                        if (!isFirstParameter)
                        {
                            res += ", ";
                        }
                        else
                        {
                            isFirstParameter = false;
                        }
                        res += AnalysisUtils.GetFullTypeName(param.ParameterType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName) + " " + param.Name;
                    }
                }
                res += "]";
            }
            else
            {
                name += elementName;
                res += name;
            }
            res += "\n" + AnalysisUtils.Indent(indentation) + "{" + "\n";
            res += OutputPropertyBody(propertyInfo, isPropertyAbstract || typeEnum == TypeEnum.Interface, _outputOptions.OutputPropertyOptions, !isExplicitImplementation, indentation + 1);
            res += AnalysisUtils.Indent(indentation) + "}\n";
            return res;
        }

        private string OutputPropertyBody(PropertyInfo propertyInfo, bool isPropertyAbstractOrDeclaredInInterface, OutputPropertyOptions implementationOptions, bool isPropertyPublic, int indentation)
        {
            string res = "";
            PropertyDefinition property = propertyInfo.Property;
            bool isDependencyProperty = propertyInfo.IsDependencyProperty;
            TypeReference returnType = property.PropertyType;
            AccessModifierEnum getMethodAM = AnalysisUtils.GetMethodAccessModifier(property.GetMethod);
            AccessModifierEnum setMethodAM = AnalysisUtils.GetMethodAccessModifier(property.SetMethod);
            if (isPropertyAbstractOrDeclaredInInterface)
            {
                if (property.GetMethod != null)
                {
                    res += AnalysisUtils.Indent(indentation) + "get;\n";
                }
                if (property.SetMethod != null)
                {
                    res += AnalysisUtils.Indent(indentation) + "set;\n";
                }
            }
            else
            {
                if(implementationOptions == OutputPropertyOptions.OUTPUT_NOT_IMPLEMENTED)
                {
                    if (property.GetMethod != null)
                    {
                        res += AnalysisUtils.Indent(indentation);
                        if (getMethodAM != AccessModifierEnum.PUBLIC && isPropertyPublic)
                        {
                            res += "private ";
                        }
                        res += "get { " + AnalysisUtils.NEW_NOT_IMPLEMENTED_EXCEPTION + " }\n";
                    }
                    if (property.SetMethod != null)
                    {
                        res += AnalysisUtils.Indent(indentation);
                        if (setMethodAM != AccessModifierEnum.PUBLIC && isPropertyPublic)
                        {
                            res += "private ";
                        }
                        res += "set { " + AnalysisUtils.NEW_NOT_IMPLEMENTED_EXCEPTION + " }\n";
                    }
                }
                else if (isDependencyProperty)
                {
                    if (property.GetMethod != null)
                    {
                        res += AnalysisUtils.Indent(indentation);
                        if (getMethodAM != AccessModifierEnum.PUBLIC && isPropertyPublic)
                        {
                            res += "private ";
                        }
                        res += "get { return (" + AnalysisUtils.GetFullTypeName(returnType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName)
                            + ")" + "this.GetValue(" + AnalysisUtils.GetFullTypeName(property.DeclaringType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName)
                            + "." + propertyInfo.FieldNameIfAny + "); }\n";
                    }
                    if (property.SetMethod != null)
                    {
                        res += AnalysisUtils.Indent(indentation);
                        if (setMethodAM != AccessModifierEnum.PUBLIC && isPropertyPublic)
                        {
                            res += "private ";
                        }
                        res += "set { this.SetValue(" + AnalysisUtils.GetFullTypeName(property.DeclaringType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName)
                            + "." + propertyInfo.FieldNameIfAny + ", value); }\n";
                    }
                }
                else
                {
                    string getterBody = "";
                    string setterBody = "";
                    if (implementationOptions == OutputPropertyOptions.OUTPUT_PRIVATE_FIELD)
                    {
                        string propertyName = property.Name;
                        string privateFieldName;
                        if (propertyInfo.FieldNameIfAny == null)
                        {
                            privateFieldName = "_" + propertyName.Substring(0, 1).ToLower() + propertyName.Substring(1);
                        }
                        else
                        {
                            privateFieldName = propertyInfo.FieldNameIfAny;
                        }
                        getterBody = "return " + privateFieldName + "; ";
                        setterBody = privateFieldName + " = value; ";
                    }
                    else
                    {
                        string defaultReturnValue = GetDefaultValueAsString(returnType, (int)implementationOptions);
                        if (defaultReturnValue != null)
                        {
                            getterBody = "return " + defaultReturnValue + ";";
                            setterBody = "";
                        }
                    }

                    if (property.GetMethod != null)
                    {
                        res += AnalysisUtils.Indent(indentation);
                        if(getMethodAM != AccessModifierEnum.PUBLIC && getMethodAM != AnalysisUtils.GetMethodAccessModifier(property))
                        {
                            res += getMethodAM == AccessModifierEnum.PUBLIC || getMethodAM == AccessModifierEnum.PROTECTED ? AnalysisUtils.AccessModifierEnumToString(getMethodAM) : "private";
                            res += " ";
                        }
                        res += "get { " + getterBody + "}\n";
                        //res += AnalysisUtils.Indent(indentation) + (getMethodAM != AccessModifierEnum.PUBLIC && isPropertyPublic ? "private " : "") + "get { " + getterBody + "}\n";
                    }
                    if (property.SetMethod != null)
                    {
                        res += AnalysisUtils.Indent(indentation);
                        if (setMethodAM != AccessModifierEnum.PUBLIC && setMethodAM != AnalysisUtils.GetMethodAccessModifier(property))
                        {
                            res += setMethodAM == AccessModifierEnum.PUBLIC || setMethodAM == AccessModifierEnum.PROTECTED ? AnalysisUtils.AccessModifierEnumToString(setMethodAM) : "private";
                            res += " ";
                        }
                        res += "set { " + setterBody + "}\n";
                        //res += AnalysisUtils.Indent(indentation) + (setMethodAM != AccessModifierEnum.PUBLIC && isPropertyPublic ? "private " : "") + "set { " + setterBody + "}\n";
                    }
                }
            }
            return res;
        }

        private string WriteProperties(HashSet<PropertyInfo> properties, TypeEnum typeEnum, int indentation)
        {
            if (properties.Count == 0)
            {
                return "";
            }
            string indent = AnalysisUtils.Indent(indentation);
            string res = "";
            res += indent + "#region Properties" +"\n";
            foreach (PropertyInfo property in properties)
            {
                res += WriteProperty(property, typeEnum, indentation);
            }
            res += indent + "#endregion" + "\n\n";
            return res;
        }

        // ---------------------------------------------//


        // ------------ EVENT SECTION ------------------//

        private string WriteEvent(EventDefinition @event, TypeEnum typeEnum, int indentation)
        {
            bool isExplicitlyImplemented = AnalysisUtils.IsMethodExplicitlyImplemented(@event);
            string res = AnalysisUtils.Indent(indentation);
            if (typeEnum != TypeEnum.Interface)
            {
                string accessModifier = AnalysisUtils.AccessModifierEnumToString(AnalysisUtils.GetMethodAccessModifier(@event), !isExplicitlyImplemented);
                res += accessModifier + (isExplicitlyImplemented ? "" : " ");
                if (AnalysisUtils.IsMethodStatic(@event))
                {
                    res += "static ";
                }
                else
                {
                    bool isMethodSealedIfOverride;
                    if (AnalysisUtils.IsMethodAbstract(@event))
                    {
                        res += "abstract ";
                    }
                    else if (AnalysisUtils.IsMethodVirtual(@event))
                    {
                        res += "virtual ";
                    }
                    else if (AnalysisUtils.IsMethodOverride(@event, out isMethodSealedIfOverride))
                    {
                        res += "override ";
                        if (isMethodSealedIfOverride)
                        {
                            res += "sealed ";
                        }
                    }
                }
            }
            res += "event " + AnalysisUtils.GetFullTypeName(@event.EventType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName) + " ";
            string name = @event.Name;
            res += name;
            res += OutputEventBody(@event, isExplicitlyImplemented ? OutputEventOptions.OUTPUT_EMPTY_IMPLEMENTATION : _outputOptions.OutputEventOptions, indentation);
            res += "\n";
            return res;
        }

        private string OutputEventBody(EventDefinition @event, OutputEventOptions options, int indentation)
        {
            string body = "";
            switch (options)
            {
                case OutputEventOptions.AUTO_IMPLEMENT:
                    body = ";";
                    return body;
                case OutputEventOptions.OUTPUT_EMPTY_IMPLEMENTATION:
                    body = "";
                    body += "\n" + AnalysisUtils.Indent(indentation) + "{";
                    body += "\n" + AnalysisUtils.Indent(indentation + 1);
                    body += "add { }";
                    body += "\n" + AnalysisUtils.Indent(indentation + 1);
                    body += "remove { }";
                    body += "\n" + AnalysisUtils.Indent(indentation) + "}\n";
                    return body;
                case OutputEventOptions.OUTPUT_NOT_IMPLEMENTED:
                    body = "";
                    body += "\n" + AnalysisUtils.Indent(indentation) + "{";
                    body += "\n" + AnalysisUtils.Indent(indentation + 1);
                    body += "add { " + AnalysisUtils.NEW_NOT_IMPLEMENTED_EXCEPTION + " }";
                    body += "\n" + AnalysisUtils.Indent(indentation + 1);
                    body += "remove { " + AnalysisUtils.NEW_NOT_IMPLEMENTED_EXCEPTION + " }";
                    body += "\n" + AnalysisUtils.Indent(indentation) + "}\n";
                    return body;
            }
            return body;
        }

        private string WriteEvents(HashSet<EventDefinition> events, TypeEnum typeEnum, int indentation)
        {
            if (events.Count == 0)
            {
                return "";
            }
            string indent = AnalysisUtils.Indent(indentation);
            string res = "";
            res += indent + "#region Events" + "\n";
            //res += indent + "//-------------------------------------//" + "\n" +
            //             indent + "//--------------- EVENTS --------------//" + "\n" +
            //             indent + "//-------------------------------------//" + "\n";
            foreach (EventDefinition @event in events)
            {
                res += WriteEvent(@event, typeEnum, indentation);
            }
            res += indent + "#endregion" + "\n\n";
            //res += indent + "//-------------------------------------//" + "\n" +
            //       indent + "//-------------------------------------//" + "\n" +
            //       indent + "//-------------------------------------//" + "\n\n";
            return res;
        }

        // ------------------------------------- //

        // ------------------------- METHOD SECTION ---------------------- //

        private string WriteMethod(MethodInfo methodInfo, TypeEnum typeEnum, HashSet<FieldInfo> fields, int indentation)
        {
            string res = AnalysisUtils.Indent(indentation);
            bool mustSetPrivateFields = false;
            MethodDefinition method = methodInfo.Method;
            bool isExplicitImplementation = AnalysisUtils.IsMethodExplicitlyImplemented(method);
            if (typeEnum != TypeEnum.Interface)
            {
                string accessModifier = AnalysisUtils.AccessModifierEnumToString(AnalysisUtils.GetMethodAccessModifier(method), !isExplicitImplementation);
                res += accessModifier + (isExplicitImplementation ? "" : " ");
                if (AnalysisUtils.IsMethodStatic(method))
                {
                    res += "static ";
                }
                else
                {
                    bool isMethodSealedIfOverride;
                    if (AnalysisUtils.IsMethodAbstract(method))
                    {
                        res += "abstract ";
                    }
                    else if (AnalysisUtils.IsMethodVirtual(method))
                    {
                        res += "virtual ";
                    }
                    else if (AnalysisUtils.IsMethodOverride(method, out isMethodSealedIfOverride))
                    {
                        res += "override ";
                        if (isMethodSealedIfOverride)
                        {
                            res += "sealed ";
                        }
                    }
                }
            }
            string elementName;
            if ((elementName = AnalysisUtils.IsMethodAnOperator(method)) != null)
            {
                string returnType = AnalysisUtils.GetFullTypeName(method.ReturnType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName);
                res += returnType + " ";
                res += "operator" + elementName;
            }
            else if (method.Name == "op_Implicit")
            {
                res += "implicit operator ";
                elementName = AnalysisUtils.GetFullTypeName(method.ReturnType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, false);
                res += elementName;
            }
            else if (method.Name == "op_Explicit")
            {
                res += "explicit operator ";
                elementName = AnalysisUtils.GetFullTypeName(method.ReturnType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, false);
                res += elementName;
            }
            else if (method.IsConstructor)
            {
                //mustSetPrivateFields = (typeEnum == TypeEnum.Struct);
                mustSetPrivateFields = true;
                elementName = method.DeclaringType.Name;
                if (method.DeclaringType.HasGenericParameters)
                {
                    string genericParametersSuffix = "`" + method.DeclaringType.GenericParameters.Count;
                    if (elementName.EndsWith(genericParametersSuffix))
                    {
                        elementName = elementName.Substring(0, elementName.Length - genericParametersSuffix.Length);
                    }
                }
                res += elementName;
            }
            else
            {
                string returnType = AnalysisUtils.GetFullTypeName(method.ReturnType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName);
                res += returnType + " ";
                elementName = method.Name;
                res += elementName;
            }
            if (method.HasGenericParameters)
            {
                res += "<";
                bool isFirstParam = true;
                foreach (GenericParameter param in method.GenericParameters)
                {
                    if (!isFirstParam)
                    {
                        res += ", ";
                    }
                    else
                    {
                        isFirstParam = false;
                    }
                    res += param.Name;
                }
                res += ">";
            }
            res += "(";
            bool isExtensionMethod = false;
            if (method.HasCustomAttributes)
            {
                foreach (CustomAttribute cAttribute in method.CustomAttributes)
                {
                    if (cAttribute.AttributeType.FullName == "System.Runtime.CompilerServices.ExtensionAttribute")
                    {
                        isExtensionMethod = true;
                    }
                }
            }
            if (isExtensionMethod)
            {
                res += "this ";
            }
            bool hasOutParameters = false;
            if (method.HasParameters)
            {
                bool isFirstParameter = true;
                foreach (ParameterDefinition p in method.Parameters)
                {
                    if (!isFirstParameter)
                    {
                        res += ", ";
                    }
                    else
                    {
                        isFirstParameter = false;
                    }
                    if (p.IsOut)
                    {
                        hasOutParameters = true;
                    }
                    res += (p.IsOut ? "out " : "") + AnalysisUtils.GetFullTypeName(p.ParameterType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName) + " @" + p.Name;
                }
            }
            res += ")";
            if (method.IsConstructor)
            {
                res += _defaultValueGenerator.CallBaseConstructorIfAny(method);
            }
            if (method.HasBody)
            {
                res += "\n" + AnalysisUtils.Indent(indentation) + "{";
                res += OutputMethodBody(methodInfo, fields, _outputOptions.OutputMethodOptions, hasOutParameters, mustSetPrivateFields, indentation + 1);
                res += "\n" + AnalysisUtils.Indent(indentation) + "}\n";
            }
            else
            {
                res += ";\n";
            }
            return res;
        }

        private string InitializePrivateFieldsInString(HashSet<FieldInfo> fields, OutputMethodOptions outputOptions, MethodDefinition method, int indentation)
        {
            string res = "";
            foreach (FieldInfo field in fields)
            {
                if (field.Field.IsPrivate)
                {
                    ParameterDefinition bestMatch;
                    if (AnalysisUtils.TryGetParameterMatchingVariableInMethodArguments(field.Field, method.Parameters, out bestMatch))
                    {
                        res += "\n" + AnalysisUtils.Indent(indentation) + field.Field.Name + " = " + "@" + bestMatch.Name + ";";
                    }
                    else
                    {
                        res += "\n" + AnalysisUtils.Indent(indentation) + field.Field.Name + " = "
                            + GetDefaultValueAsString(field.Field.FieldType, (int)outputOptions) + ";";
                    }
                }
            }
            return res;
        }

        private string GetDefaultValueAsString(TypeReference type, int outputOptions)
        {
            if (outputOptions == 1)
            {
                return _defaultValueGenerator.GenerateDefaultValue(type);
            }
            else
            {
                return AnalysisUtils.GetSafeTypeReferenceDefaultValueAsString(type, _outputOptions.OutputFullTypeName);
            }
        }

        private string OutputMethodBody(MethodInfo methodInfo, HashSet<FieldInfo> fields, OutputMethodOptions outputBodyOptions, bool hasOutParameters, bool setPrivateFields, int indentation)
        {
            string res = "";
            MethodDefinition method = methodInfo.Method;
            bool implementBody = (outputBodyOptions == OutputMethodOptions.OUTPUT_RETURN_TYPE
                               || outputBodyOptions == OutputMethodOptions.OUTPUT_RETURN_TYPE_NOT_NULL);
            if (implementBody)
            {
                if (method.HasBody)
                {
                    bool isImplemented = false;
                    if (setPrivateFields)
                    {
                        //bool isPAValueType;
                        //foreach (FieldInfo field in fields)
                        //{
                        //    if (field.Field.IsPrivate)
                        //    {
                        //        res += "\n" + GenericTools.Indent(indentation) + field.Field.Name + " = " + GenericTools.GetSafeTypeReferenceDefaultValueAsString(field.Field.FieldType, out isPAValueType, _modules) + ";";
                        //    }
                        //}
                        res += InitializePrivateFieldsInString(fields, outputBodyOptions, methodInfo.Method, indentation);
                    }
                    if (methodInfo.IsDependencyPropertyGetter)
                    {
                        res += "\n" + AnalysisUtils.Indent(indentation);
                        string fieldName = methodInfo.FieldNameIfAny ?? method.Name.Substring(3);
                        res += "return (" + AnalysisUtils.GetFullTypeName(method.ReturnType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName) + ")@" + method.Parameters[0].Name + ".GetValue(" + AnalysisUtils.GetFullTypeName(method.DeclaringType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName) + "." + fieldName + ");";
                        isImplemented = true;
                    }
                    else if (methodInfo.IsDependencyPropertySetter)
                    {
                        res += "\n" + AnalysisUtils.Indent(indentation);
                        string fieldName = methodInfo.FieldNameIfAny ?? method.Name.Substring(3);
                        res += "@" + method.Parameters[0].Name + ".SetValue(" + AnalysisUtils.GetFullTypeName(method.DeclaringType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName) + "." + fieldName + ", " + "(object) value);";
                        isImplemented = true;
                    }
                    if (!isImplemented)
                    {
                        if (hasOutParameters)
                        {
                            //bool isPAValueType;
                            foreach (ParameterDefinition p in method.Parameters)
                            {
                                if (p.IsOut)
                                {
                                    res += "\n" + AnalysisUtils.Indent(indentation);
                                    //res += "@" + p.Name + " = " + GenericTools.GetSafeTypeReferenceDefaultValueAsString(p.ParameterType, out isPAValueType, _modules) + ";";
                                    res += "@" + p.Name + " = " + GetDefaultValueAsString(p.ParameterType, (int)outputBodyOptions) + ";";
                                }
                            }
                        }
                        //bool isValueType;
                        //string defaultReturnValue = GenericTools.GetSafeTypeReferenceDefaultValueAsString(method.ReturnType, out isValueType, _modules);
                        string defaultReturnValue = GetDefaultValueAsString(method.ReturnType, (int)outputBodyOptions);
                        if (defaultReturnValue != null)
                        {
                            //if (isValueType)
                            //{
                            //    res += "\n" + GenericTools.Indent(indentation) + "return new " + defaultReturnValue + "();";
                            //}
                            //else
                            //{
                            //    res += "\n" + GenericTools.Indent(indentation) + "return " + defaultReturnValue + ";";
                            //}
                            res += "\n" + AnalysisUtils.Indent(indentation) + "return " + defaultReturnValue + ";";
                        }
                    }
                }
            }
            else
            {
                res += "\n" + AnalysisUtils.Indent(indentation) + AnalysisUtils.NEW_NOT_IMPLEMENTED_EXCEPTION;
            }
            return res;
        }

        private string WriteMethods(HashSet<MethodInfo> methods, TypeDefinition declaringType, TypeEnum typeEnum, HashSet<FieldInfo> fields, int indentation)
        {
            MethodDefinition constructorIfNone;
            //bool needDefaultConstructor = !DefaultValueGenerator.HasConstructor(declaringType, out constructorIfNone) 
            //                           && (TypeEnum == TypeEnum.Class || TypeEnum == TypeEnum.Struct);
            bool needDefaultConstructor = !_defaultValueGenerator.HasConstructor(declaringType, out constructorIfNone);
            if (!needDefaultConstructor && methods.Count == 0)
            {
                return "";
            }
            string indent = AnalysisUtils.Indent(indentation);
            string res = "";
            res += indent + "#region Methods" + "\n";
            if (needDefaultConstructor)
            {
                if (constructorIfNone != null)
                {
                    res += WriteMethod(new MethodInfo(constructorIfNone), TypeEnum, fields, indentation);
                }
            }
            foreach (MethodInfo method in methods)
            {
                res += WriteMethod(method, typeEnum, fields, indentation);
            }
            res += indent + "#endregion" + "\n\n";
            return res;
        }

        // ---------------------------------------------------------------- //

        private string WriteUsing(string @using, int indentation)
        {
            return AnalysisUtils.Indent(indentation) + "using " + @using + ";\n";
        }

        private string WriteUsings(HashSet<string> usings, int indentation, bool dontWrite = false)
        {
            if (dontWrite)
            {
                return "";
            }
            else
            {
                if (usings.Count == 0)
                {
                    return "";
                }
                string indent = AnalysisUtils.Indent(indentation);
                string res = "";
                foreach (string @using in usings)
                {
                    res += WriteUsing(@using, indentation);
                }
                res += "\n";
                return res;
            }
        }

        private string WriteNamespace(TypeDefinition type, int indentation, out bool hasNamespace)
        {
            hasNamespace = !string.IsNullOrWhiteSpace(type.Namespace);
            if (hasNamespace)
            {
                return AnalysisUtils.Indent(indentation) + "namespace " + type.Namespace + "\n";
            }
            else
            {
                return "";
            }
        }

        private void AddTypeFromStringConverterMethods(int indentation)
        {
            MethodDefinition INTERNAL_ConvertFromString = new MethodDefinition("INTERNAL_ConvertFromString", Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.Assembly, _defaultValueGenerator.InstanceOfTypeSystem.Object);
            INTERNAL_ConvertFromString.Parameters.Add(new ParameterDefinition(Type.Name.Substring(0, 1).ToLower() + Type.Name.Substring(1) + "AsString", Mono.Cecil.ParameterAttributes.None, _defaultValueGenerator.InstanceOfTypeSystem.String));
            AddMethod(new MethodInfo(INTERNAL_ConvertFromString));
            string staticConstructorAsString = "static " + Type.Name + "()\n"
                                             + AnalysisUtils.Indent(indentation + 1) + "{\n"
                                             + AnalysisUtils.Indent(indentation + 2) + "TypeFromStringConverters.RegisterConverter(typeof("
                                             + Type.Name + "), INTERNAL_ConvertFromString);\n"
                                             + AnalysisUtils.Indent(indentation + 1) + "}";
            AddCode(staticConstructorAsString);
        }

        private string WriteCustomAttribute(CustomAttribute customAttribute, int indentation)
        {
            string customAttributeAsString = "";
            if (customAttribute.AttributeType.FullName == "System.Windows.Markup.ContentPropertyAttribute")
            {
                if (customAttribute.HasConstructorArguments)
                {
                    string propertyName = customAttribute.ConstructorArguments[0].Value.ToString();
                    customAttributeAsString = AnalysisUtils.Indent(indentation) + "[ContentProperty(\"" + propertyName + "\")]\n";
                }
            }
            else if (customAttribute.AttributeType.FullName == "System.ComponentModel.TypeConverterAttribute")
            {
                customAttributeAsString = AnalysisUtils.Indent(indentation) + "[SupportsDirectContentViaTypeFromStringConverters]\n";
                AddTypeFromStringConverterMethods(indentation);
            }
            return customAttributeAsString;
        }

        private List<string> _supportedCustomAttributes;
        private List<string> SupportedCustomAttributes
        {
            get
            {
                if(_supportedCustomAttributes == null)
                {
                    _supportedCustomAttributes = new List<string>(new string[] { "System.Windows.Markup.ContentPropertyAttribute", "System.ComponentModel.TypeConverterAttribute" });
                }
                return _supportedCustomAttributes;
            }
        }

        private int GetCustomAttributesCount(List<CustomAttribute> customAttributes)
        {
            int count = 0;
            if (customAttributes != null)
            {
                foreach (CustomAttribute attribute in customAttributes)
                {
                    if (SupportedCustomAttributes.Contains(attribute.AttributeType.FullName))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private string WriteCustomAttributes(List<CustomAttribute> customAttributes, int indentation)
        {
            if (GetCustomAttributesCount(customAttributes) == 0)
            {
                return "";
            }
            string indent = AnalysisUtils.Indent(indentation);
            string res = "";
            foreach (CustomAttribute customAttribute in customAttributes)
            {
                res += WriteCustomAttribute(customAttribute, indentation);
            }
            return res;
        }

        private string WriteCodeToAddManually(HashSet<string> codeToAddManually, int indentation)
        {
            if (codeToAddManually.Count == 0)
            {
                return "";
            }
            string indent = AnalysisUtils.Indent(indentation);
            string res = "";
            foreach (string blockOfCode in codeToAddManually)
            {
                res += indent + blockOfCode + "\n";
            }
            res += "\n";
            return res;
        }

        private string WriteTypeNameAndInheritance(TypeDefinition type, TypeEnum typeEnum, int indentation)
        {
            string res = AnalysisUtils.Indent(indentation);
            //Access modifier
            res += AnalysisUtils.AccessModifierEnumToString(AnalysisUtils.GetTypeAccessModifier(type));
            // static / abstract / sealed
            if (typeEnum == TypeEnum.Class)
            {
                if (AnalysisUtils.IsTypeStatic(type))
                {
                    res += " static";
                }
                else if (AnalysisUtils.IsTypeAbstract(type))
                {
                    res += " abstract";
                }
                else if (AnalysisUtils.IsTypeSealed(type))
                {
                    res += " sealed";
                }
            }

            res += " partial";

            res += " " + AnalysisUtils.TypeEnumToString(typeEnum);
            // type name
            res += " " + AnalysisUtils.GetFullTypeName(type, false);
            bool hasBaseType = false;
            if (typeEnum == TypeEnum.Class)
            {
                if ((hasBaseType = (type.BaseType != null && type.BaseType.FullName != "System.Object")))
                {
                    res += " : " + AnalysisUtils.GetFullTypeName(type.BaseType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName);
                }
            }
            // Write implemented interfaces
            if (type.HasInterfaces)
            {
                bool isFirstInterface = !hasBaseType;
                foreach (InterfaceImplementation @interface in type.Interfaces)
                {
                    if (ImplementedInterfaces.Contains(@interface.InterfaceType))
                    {
                        if (!isFirstInterface)
                        {
                            res += ", ";
                        }
                        else
                        {
                            isFirstInterface = false;
                            res += " : ";
                        }
                        res += AnalysisUtils.GetFullTypeName(@interface.InterfaceType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName);
                    }
                }
            }

            res += "\n";
            return res;
        }

        private string WriteTypeBody(TypeDefinition declaringType, TypeEnum typeEnum, HashSet<MethodInfo> methods, HashSet<PropertyInfo> properties, HashSet<EventDefinition> events, HashSet<FieldInfo> fields, HashSet<string> codeToAddManually, List<TypeDefinition> nestedTypes, int indentation)
        {
            string body = "";
            body += WriteFields(fields, declaringType, indentation);
            body += WriteProperties(properties, typeEnum, indentation);
            body += WriteEvents(events, typeEnum, indentation);
            body += WriteMethods(methods, declaringType, typeEnum, fields, indentation);
            body += WriteCodeToAddManually(codeToAddManually, indentation);
            return body;
        }

        private string WriteCompilerDirective(string name)
        {
            return $"#if {name}\n";
        }

        private string EndCompilerDirective()
        {
            return "#endif\n";
        }
        

        private string WriteType(TypeDefinition type, TypeEnum typeEnum, HashSet<FieldInfo> fields, HashSet<PropertyInfo> properties, HashSet<EventDefinition> events, HashSet<MethodInfo> methods, List<TypeDefinition> nestedTypes, List<CustomAttribute> customAttributes, HashSet<string> codeToAddManually, HashSet<string> usings, OutputOptions outputOptions, int indentation)
        {
            if (typeEnum == TypeEnum.Enum)
            {
                return WriteEnum(type, usings, outputOptions, indentation);
            }
            else if (typeEnum == TypeEnum.Delegate)
            {
                return WriteDelegate(type, usings, outputOptions, indentation);
            }
            else
            {
                string res = "";
                // Write Usings
                res += WriteUsings(usings, indentation, outputOptions.OutputFullTypeName);

                // Write Namespace
                bool hasNamespace;
                res += WriteNamespace(type, indentation, out hasNamespace);
                if (hasNamespace)
                {
                    res += AnalysisUtils.Indent(indentation) + "{\n";
                    indentation++;
                }

                res += WriteCompilerDirective("WORKINPROGRESS");

                // Write custom attributes
                res += WriteCustomAttributes(customAttributes, indentation);

                // Write class name and inheritance
                res += WriteTypeNameAndInheritance(type, typeEnum, indentation);

                res += AnalysisUtils.Indent(indentation) + "{\n";
                indentation++;

                //body
                res += WriteTypeBody(type, typeEnum, methods, properties, events, fields, codeToAddManually, nestedTypes, indentation);

                indentation--;
                res += AnalysisUtils.Indent(indentation) + "}\n";

                res += EndCompilerDirective();

                if (hasNamespace)
                {
                    indentation--;
                    res += AnalysisUtils.Indent(indentation) + "}\n";
                }
                return res;
            }
        }

        private string WriteEnum(TypeDefinition enumeration, HashSet<string> usings, OutputOptions outputOptions, int indentation)
        {
            string res = AnalysisUtils.Indent(indentation);
            if (!outputOptions.OutputFullTypeName)
            {
                res += WriteUsings(usings, indentation);
            }
            bool hasNameSpace;
            res += WriteNamespace(enumeration, indentation, out hasNameSpace);
            if (hasNameSpace)
            {
                res += AnalysisUtils.Indent(indentation) + "{\n";
                indentation++;
            }
            res += AnalysisUtils.Indent(indentation);
            string accessModifier = AnalysisUtils.AccessModifierEnumToString(AnalysisUtils.GetTypeAccessModifier(enumeration));
            res += accessModifier;
            res += " enum";
            res += " " + AnalysisUtils.GetFullTypeName(enumeration, false);
            res += "\n" + AnalysisUtils.Indent(indentation) + "{";
            ////Add code manually (via Configuration.cs file) (commented because there should not be any in an enum.
            //res += OutputAdditionalCodeIfAny(_indentation + indentationOffset + 1);
            res += "\n" + GetEnumContent(enumeration, indentation + 1);
            res += "\n" + AnalysisUtils.Indent(indentation) + "}";
            if (hasNameSpace)
            {
                indentation--;
                res += "\n" + AnalysisUtils.Indent(indentation) + "}";
            }
            return res;
        }

        /// <summary>
        /// Get content if an enum and returns it as a string.
        /// </summary>
        /// <param name="enumeration"></param>
        /// <returns></returns>
        private string GetEnumContent(TypeDefinition enumeration, int indentation)
        {
            string res = "";
            bool isFirstField = true;
            if (enumeration.HasFields)
            {
                string assemblyPath = Path.Combine(Configuration.ReferencedAssembliesFolderPath, enumeration.Module.Assembly.Name.Name + ".dll");
                Type type = null;
                if (File.Exists(assemblyPath))
                {
                    Assembly typeAssembly = Assembly.LoadFile(assemblyPath);
                    type = typeAssembly.GetType(enumeration.FullName);
                }
                //Type type = Type.GetType(enumeration.FullName + ", " + enumeration.Module.Assembly.FullName);
                Type underlyingType = type != null ? Enum.GetUnderlyingType(type) : null;
                foreach (FieldDefinition field in enumeration.Fields)
                {
                    if (field.Name != "value__")
                    {
                        if (!isFirstField)
                        {
                            res += ",\n";
                        }
                        else
                        {
                            isFirstField = false;
                        }
                        res += AnalysisUtils.Indent(indentation) + field.Name;
                        if (underlyingType != null)
                        {
                            if (underlyingType.Name == "Int32")
                            {
                                res += " = " + ((int)field.Constant).ToString();
                            }
                            else if (underlyingType.Name == "Int64")
                            {
                                res += " = " + ((long)field.Constant).ToString();
                            }
                            else if (underlyingType.Name == "Int16")
                            {
                                res += " = " + ((short)field.Constant).ToString();
                            }
                        }
                        else
                        {
                            res += " = " + ((int)field.Constant).ToString();
                        }
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Write a delegate into a string.
        /// </summary>
        /// <returns></returns>
        private string WriteDelegate(TypeDefinition @delegate, HashSet<string> usings, OutputOptions outputOptions, int indentation)
        {
            string res = AnalysisUtils.Indent(indentation);
            if (!outputOptions.OutputFullTypeName)
            {
                res += WriteUsings(usings, indentation);
            }
            bool hasNameSpace;
            res += WriteNamespace(@delegate, indentation, out hasNameSpace);
            if (hasNameSpace)
            {
                res += AnalysisUtils.Indent(indentation) + "{" + "\n";
                indentation++;
            }
            res += AnalysisUtils.Indent(indentation);
            string accessModifier = AnalysisUtils.AccessModifierEnumToString(AnalysisUtils.GetTypeAccessModifier(@delegate));
            res += accessModifier;
            res += " delegate ";
            res += GetDelegateContent(@delegate);
            if (hasNameSpace)
            {
                indentation--;
            }
            res += "\n" + AnalysisUtils.Indent(indentation) + "}";
            return res;
        }

        /// <summary>
        /// Get content of a delegate and returns it as a string.
        /// </summary>
        /// <param name="delegate"></param>
        /// <returns></returns>
        private string GetDelegateContent(TypeDefinition @delegate)
        {
            string res = "";
            if (@delegate.HasMethods)
            {
                foreach (MethodDefinition method in @delegate.Methods)
                {
                    if (method.Name == "Invoke")
                    {
                        res += AnalysisUtils.GetFullTypeName(method.ReturnType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName);
                        AddUsing(method.ReturnType.Namespace);
                        res += " " + @delegate.Name + "(";
                        if (method.HasParameters)
                        {
                            bool isFirstParameter = true;
                            foreach (ParameterDefinition param in method.Parameters)
                            {
                                if (!isFirstParameter)
                                {
                                    res += ", ";
                                }
                                else
                                {
                                    isFirstParameter = false;
                                }
                                res += AnalysisUtils.GetFullTypeName(param.ParameterType, Configuration.TypesThatNeedToBeRenamed, Configuration.TypesThatNeedFullName, _outputOptions.OutputFullTypeName) + " @" + param.Name;
                                AddUsing(param.ParameterType.Namespace);
                            }
                        }
                        break;
                    }
                }
            }
            res += ");";
            return res;
        }

        public void SetAndRun(TypeDefinition typeToWrite, HashSet<string> additionalCodeIfAny, HashSet<TypeReference> implementedInterfaces, bool hasAnIndexer, string directoryPath, int indentation)
        {
            Set(typeToWrite, additionalCodeIfAny, implementedInterfaces, hasAnIndexer, directoryPath, indentation);
            string typeAsString = WriteType(Type, TypeEnum, Fields, Properties, Events, Methods, NestedTypes, CustomAttributes, CodeToAddManually, Usings, _outputOptions, _indentation);
            Save(typeAsString);
            Reset();
        }

        private void Save(string typeAsString)
        {
            string directoryPath;
            if (String.IsNullOrEmpty(Configuration.PathOfDirectoryWhereFileAreGenerated))
            {
                directoryPath = _currentDirectory + '\\' + Type.Namespace + '\\';
            }
            else
            {
                directoryPath = Path.Combine(Configuration.PathOfDirectoryWhereFileAreGenerated, _currentDirectory + '\\' + Type.Namespace);
            }
            Directory.CreateDirectory(directoryPath);
            string filePath = Path.Combine(directoryPath, Type.Name.Replace('`', '_') + ".cs");

            StreamWriter writer = new StreamWriter(filePath, false);
            writer.Write(typeAsString);
            writer.Close();
        }

        private void Reset()
        {
            Type = null;
            Usings.Clear();
            Fields.Clear();
            Events.Clear();
            Properties.Clear();
            Methods.Clear();
            CustomAttributes.Clear();
            CodeToAddManually.Clear();
            NestedTypes.Clear();
            ImplementedInterfaces.Clear();
            //Constructors.Clear();
            _hasAnIndexer = false;
            TypeEnum = TypeEnum.None;
            _suffixToAddToVariableNameInCaseOfDuplicate = 1;
        }
    }
}
