using DotNetForHtml5.PrivateTools.AssemblyAnalysisCommon;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer
{
    public class AnalyzeHelper
    {
        private const string CommonLanguageRuntimeLibrary = "CommonLanguageRuntimeLibrary";
        private const string Mscorlib = "mscorlib";
        private const string Netstandard = "netstandard";
        private const string System = "System";

        Dictionary<string, HashSet<string>> _supportedMscorlibMethods = new Dictionary<string, HashSet<string>>();
        public CoreSupportedMethodsContainer CoreSupportedMethods;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coreSupportedMethods">Methods and classes supported by the currently selected product</param>
        /// <param name="mscorlibImpInfoFile">Path to the dll or xml file containing informations about what is supported from mscorlib</param>
        public AnalyzeHelper(CoreSupportedMethodsContainer coreSupportedMethods, string mscorlibImpInfoFile)
        {
            CoreSupportedMethods = coreSupportedMethods;

            // WTF WHY IS THIS BLOCK BELLOW NEVER USED PLEASE SOMEONE HELP ME
            
            List<Tuple<string, List<string>>> supportedMethods = GetSupportedMethods(mscorlibImpInfoFile);
            
            // Add additional supported methods that are not defined in the proxies (such as "Int32.Parse"):
            foreach (Tuple<string, List<string>> typeToMethods in supportedMethods)
            {
                HashSet<string> typeMethods = GetReferenceToListOfSupportedMethodsForAGivenTypeName(typeToMethods.Item1);
                foreach (string methodName in typeToMethods.Item2)
                {
                    typeMethods.Add(methodName);
                }
            }
        }

        /// <summary>
        /// Get the supported mscorlib methods and classes from a XML or DLL file
        /// </summary>
        /// <param name="mscorlibImpInfoFile">Path to the file from which we retrieve the supported methods and classes</param>
        /// <returns>Returns a List of the supported methods and classes</returns>
        internal static List<Tuple<string, List<string>>> GetSupportedMethods(string mscorlibImpInfoFile)
        {
            if (Path.GetExtension(mscorlibImpInfoFile)?.ToLower() == ".dll")
                return GetSupportedMethodsFromDLL(mscorlibImpInfoFile);
            
            if (Path.GetExtension(mscorlibImpInfoFile)?.ToLower() == ".xml")
                return GetSupportedMethodsFromXML(mscorlibImpInfoFile);
            
            throw new ArgumentException($"This extension is not recognized: {Path.GetExtension(mscorlibImpInfoFile)}");
        }

        internal static List<Tuple<string, List<string>>> GetSupportedMethodsFromDLL(string mscorlibImpInfoFile)
        {
            return new List<Tuple<string, List<string>>>();
        }
        
        /// <summary>
        /// Reads SupportedElements.xml and returns a List&lt;Tuple&lt;string, List&lt;string&gt;&gt;&gt; that contains the supported methods defined in SupportedElements.xml, per type.
        /// </summary>
        /// <returns>A List&lt;Tuple&lt;string, List&lt;string&gt;&gt;&gt; that contains the supported methods defined in SupportedElements.xml, per type.</returns>
        internal static List<Tuple<string, List<string>>> GetSupportedMethodsFromXML(string mscorlibImplInfoPath)
        {
            List<Tuple<string, List<string>>> result = null;

            // Load the XML file located in the Compiler directory:
            XDocument xdoc;
            try
            {
                //var xmlFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SupportedElements.xml");
                //if (!File.Exists(xmlFilePath)) // We also look in the parent folder because we may be in a subfolder of the Compiler folder (such as the "SLMigration" folder).
                //    xmlFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"..\SupportedElements.xml");

                Console.WriteLine(mscorlibImplInfoPath);
                xdoc = XDocument.Load(mscorlibImplInfoPath);
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to load the file that contains the list of supported methods (SupportedElements.xml). Please contact support at support@cshtml5.com" + Environment.NewLine + Environment.NewLine + ex.ToString());
            }

            // Query the document:
            try
            {
                result = (from type in xdoc.Root.Descendants("Type")
                          select new Tuple<string, List<string>>(
                              type.Attribute("Name").Value,
                              (from member in type.Elements("Member")
                               select member.Attribute("Name").Value).ToList())).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while parsing the file that contains the list of supported methods. Please contact support at support@cshtml5.com" + Environment.NewLine + Environment.NewLine + ex.ToString());
            }

            return result;
        }

        /// <summary>
        /// Checks whether the method with the given name should be considered supported or not.
        /// </summary>
        /// <param name="userAssembliesNamesLowercase">
        /// The names of the user's assemblies. Those are used
        /// to make sure we do not consider a user's own method as not supported by us.
        /// </param>
        /// <param name="fileName">The file in which the method was found</param>
        /// <param name="userAssemblyName">The name of the current user assembly</param>
        /// <param name="errorsAlreadyRaised">
        /// A HashSet&lt;string&gt; with the "FullMethodName" of all the methods
        /// for which we have already determined that they were not supported.
        /// </param>
        /// <param name="lineNumber">The line at which the method was found in the file.</param>
        /// <param name="assemblyName">The name of the Assembly that should contain the type.</param>
        /// <param name="namespace"></param>
        /// <param name="methodName">The method Name</param>
        /// <param name="typeName">The name of the type where the method is defined</param>
        /// <param name="callingMethodFullName">(We put the file name here as well)</param>
        /// <param name="addUnsupportedMethod">
        /// A boolean saying if we should add the method to the list of unsupported methods if it is not supported.
        /// It is Usually true but when we do not know if the method should be set_PropertyName of add_PropertyName, we need to try with each and only add the method to the list if nither are supported.
        /// </param>
        /// <param name="whatToDoWhenNotSupportedMethodFound">The method that adds the unsupported method to the list.</param>
        /// <returns>A boolean saying if the method was supported. This is useful when we want to check multiple name possibilities for a single method (see parameter addUnsupportedMethod).</returns>
        internal bool CheckMethodValidity(HashSet<string> userAssembliesNamesLowercase, string fileName, string userAssemblyName, HashSet<string> errorsAlreadyRaised, int lineNumber, string assemblyName, string @namespace, string methodName, string typeName, string callingMethodFullName, bool addUnsupportedMethod, Action<UnsupportedMethodInfo> whatToDoWhenNotSupportedMethodFound)
        {
            //we assume it's ok in the case where the method was defined in one of the user's assemblies:
            if (!userAssembliesNamesLowercase.Contains(assemblyName.ToLowerInvariant())
                && !userAssembliesNamesLowercase.Contains(assemblyName.ToLowerInvariant() + ".dll"))
            {
                string fullMethodName = @namespace + "." + typeName +"." + methodName;
                if (!CoreSupportedMethods.Contains(@namespace, typeName, methodName))//if the core does not contain the definition for the method:
                {
                    if (!errorsAlreadyRaised.Contains(fullMethodName))//if that same error was not raised yet
                    {
                        if (addUnsupportedMethod) //if we want to add the method to the list of the unsupported methods, we do so.
                                                  //we do not want to add it when we do not know if that version of the method name is correct.
                                                  //For example, when we meet Click in Xaml, the method is add_Click
                                                  //             when we meet Width in Xaml, the method is set_Width
                        {
                            //add the method to those that do not work:
                            whatToDoWhenNotSupportedMethodFound(
                                         new UnsupportedMethodInfo()
                                         {
                                             MethodName = methodName,
                                             TypeName = typeName,
                                             CallingMethodFullName = callingMethodFullName,
                                             CallingMethodFileNameWithPath = fileName,
                                             CallingMethodLineNumber = lineNumber,
                                             UserAssemblyName = userAssemblyName,
                                             MethodAssemblyName = assemblyName,
                                             NeedToBeCheckedBecauseOfInheritance = true,
                                         });
                            errorsAlreadyRaised.Add(fullMethodName);
#if LOG
                            System.Diagnostics.Debug.WriteLine(fullMethodName);
#endif
                        }
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Loops through all methods of all types defined in the assembly and returns them as <see cref="MethodDefinition"/>.
        /// </summary>
        /// <param name="assemblyDefinition">The assembly in which to look for the methods.</param>
        /// <returns>An <see cref="IEnumerable"/> of <see cref="MethodDefinition"/> that contains all methods defined in the assembly.</returns>
        public static IEnumerable<MethodDefinition> GetAllMethodsDefinedInAssembly(AssemblyDefinition assemblyDefinition)
        {
            // Iterate through all the members:
            foreach (TypeDefinition type in GetAllTypesDefinedInAssembly(assemblyDefinition))
            {
                foreach (MethodDefinition method in GetAllMethodsDefinedInType(type))
                {
                    yield return method;
                }
            }       
        }

        /// <summary>
        /// Loops through all types defined in the assembly and returns them as <see cref="TypeDefinition"/>.
        /// </summary>
        /// <param name="assemblyDefinition">The assembly in which to look for the types.</param>
        /// <returns>An <see cref="IEnumerable"/> of <see cref="TypeDefinition"/> that contains all types defined in the assembly.</returns>
        public static IEnumerable<TypeDefinition> GetAllTypesDefinedInAssembly(AssemblyDefinition assemblyDefinition)
        {
            foreach (ModuleDefinition module in assemblyDefinition.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    yield return type;
                }
            }
        }
        
        /// <summary>
        /// Loops through all methods defined in the type and returns them as <see cref="MethodDefinition"/>.
        /// </summary>
        /// <param name="type">The type in which to look for the methods.</param>
        /// <returns>An <see cref="IEnumerable"/> of <see cref="MethodDefinition"/> that contains all methods defined in the type.</returns>
        public static IEnumerable<MethodDefinition> GetAllMethodsDefinedInType(TypeDefinition type)
        {
            foreach (MethodDefinition methodDefinition in type.Methods)
            {
                yield return methodDefinition;
            }
            
            foreach (PropertyDefinition propertyDefinition in type.Properties)
            {
                MethodDefinition getMethod = propertyDefinition.GetMethod;
                if (getMethod != null)
                    yield return getMethod;
                
                MethodDefinition setMethod = propertyDefinition.SetMethod;
                if (setMethod != null)
                    yield return setMethod;
            }
            
            foreach(EventDefinition eventDefinition in type.Events)
            {
                var addMethod = eventDefinition.AddMethod;
                var removeMethod = eventDefinition.RemoveMethod;
                if(addMethod != null)
                {
                    yield return addMethod;
                }
                if(removeMethod != null)
                {
                    yield return removeMethod;
                }
            }
            
            if (type.HasNestedTypes)
            {
                foreach (TypeDefinition nestedType in type.NestedTypes)
                {
                    if (nestedType.IsNestedPublic || nestedType.IsNestedFamily)
                    {
                        foreach (MethodDefinition m in GetAllMethodsDefinedInType(nestedType))
                        {
                            yield return m;
                        }
                    }
                }
            }
        }

        public static IEnumerable<AssemblyDefinition> GetAllUserAssemblies(AssemblyDefinition[] assembliesDefinitions, string nameOfAssembliesThatDoNotContainUserCode)
        {
            HashSet<string> listOfNamesOfAssembliesThatDoNotContainUserCode = new HashSet<string>();
            if (nameOfAssembliesThatDoNotContainUserCode != null)
            {
                string[] array = nameOfAssembliesThatDoNotContainUserCode.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string item in array)
                {
                    listOfNamesOfAssembliesThatDoNotContainUserCode.Add(item.ToLower());
                }
            }

            foreach (AssemblyDefinition assemblyDefinition in assembliesDefinitions)
            {
                // Retain only user assemblies:
                if (!listOfNamesOfAssembliesThatDoNotContainUserCode.Contains(assemblyDefinition.Name.Name.ToLower()))
                {
                    yield return assemblyDefinition;
                }
            }
        }

        /// <summary>
        /// Returns all the methods that are called by the given method.
        /// </summary>
        /// <param name="methodDefinition"></param>
        /// <returns></returns>
        public static IEnumerable<MemberReferenceAndCallerInformation> GetAllMethodsReferencedInMethod(MethodDefinition methodDefinition)
        {
            if (methodDefinition.HasBody)
            {
                SequencePoint lastNonNullSequencePoint = null;
                IDictionary<Instruction, SequencePoint> sequencePointMappings = methodDefinition.DebugInformation.GetSequencePointMapping(); // cf. http://cecil.pe/post/149243207656/mono-cecil-010-beta-1
                foreach (Instruction instruction in methodDefinition.Body.Instructions)
                {
                    SequencePoint sequencePoint;
                    if (sequencePointMappings.TryGetValue(instruction, out sequencePoint) && sequencePoint != null)
                        lastNonNullSequencePoint = sequencePoint;

                    MethodReference mRef = instruction.Operand as MethodReference;

                    if (mRef != null && mRef.DeclaringType != null)
                    {
                        var memberReferenceAndCorrespondingInstruction
                            = new MemberReferenceAndCallerInformation(mRef, lastNonNullSequencePoint);

                        yield return memberReferenceAndCorrespondingInstruction;
                    }
                }
            }
        }

        //
        /// <summary>
        /// returns true if the method is defined in supportedElements.xml, otherwise, false.
        /// </summary>
        /// <param name="methodReference">The method to look for.</param>
        /// <returns>True if the method is defined in supportedElements.xml, otherwise, false.</returns>
        public bool IsMethodSupported(MemberReference methodReference)
        {
            string typeName = methodReference.DeclaringType.Name;
            return ((methodReference.DeclaringType.Scope.Name.Replace(".dll","") == Mscorlib 
                || methodReference.DeclaringType.Scope.Name.Replace(".dll", "") == CommonLanguageRuntimeLibrary
                || methodReference.DeclaringType.Scope.Name.Replace(".dll", "") == Netstandard
                || methodReference.DeclaringType.Scope.Name.Replace(".dll", "") == System
                || methodReference.DeclaringType.Scope.Name.Replace(".dll", "").StartsWith("System."))
                && _supportedMscorlibMethods.ContainsKey(typeName)
                && _supportedMscorlibMethods[typeName].Contains(methodReference.Name));
        }

        public bool IsTypeSupported(TypeReference type)
        {
            return ((type.Scope.Name.Replace(".dll", "") == Mscorlib
                || type.Scope.Name.Replace(".dll", "") == CommonLanguageRuntimeLibrary
                || type.Scope.Name.Replace(".dll", "") == Netstandard
                || type.Scope.Name.Replace(".dll", "") == System
                || type.Scope.Name.Replace(".dll", "").StartsWith("System."))
                && _supportedMscorlibMethods.ContainsKey(type.Name));
        }

        private HashSet<string> GetReferenceToListOfSupportedMethodsForAGivenTypeName(string typeName)
        {
            HashSet<string> supportedMethods;
            if (_supportedMscorlibMethods.ContainsKey(typeName))
            {
                supportedMethods = _supportedMscorlibMethods[typeName];
            }
            else
            {
                supportedMethods = new HashSet<string>();
                _supportedMscorlibMethods.Add(typeName, supportedMethods);
            }
            return supportedMethods;
        }
    }
}
