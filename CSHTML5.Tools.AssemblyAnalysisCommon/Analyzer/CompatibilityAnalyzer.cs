using DotNetForHtml5.PrivateTools.AssemblyAnalysisCommon;
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Xml;
using System.Xml.Linq;
namespace DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer
{
    public static class CompatibilityAnalyzer
    {
        static CoreSupportedMethodsContainer _coreSupportedMethods;

        public static void Analyze(
            string path,
            ILogger logger,
            List<UnsupportedMethodInfo> outputListOfUnsupportedMethods,
            CoreSupportedMethodsContainer coreSupportedMethods,
            string[] inputAssemblies,
            HashSet<string> urlNamespacesThatBelongToUserCode,
            HashSet<string> attributesToIgnoreInXamlBecauseTheyAreFromBaseClasses,
            HashSet<string> listOfFilesToIgnore,
            string supportedElementsPath,
            string mscorlibFolderPath,
            bool skipTypesWhereNoMethodIsActuallyCalled,
            bool addBothPropertyAndEventWhenNotFound = false,
            string additionalFolderWhereToResolveAssemblies = null)
        {
            _coreSupportedMethods = coreSupportedMethods;
            AssemblyDefinition assembly = LoadAssembly(path, mscorlibFolderPath, additionalFolderWhereToResolveAssemblies);
            AssemblyDefinition[] assemblies = new AssemblyDefinition[] { assembly };

            HashSet<string> userAssembliesNamesLowercase = new HashSet<string>();
            foreach (string assemblyPath in inputAssemblies)
            {
                string current = assemblyPath.Replace('\\', '/');
                current = current.Substring(current.LastIndexOf('/') + 1); //removes everything before assemblyName.dll
                userAssembliesNamesLowercase.Add(current.ToLowerInvariant());
            }
            foreach (string urlNamespace in urlNamespacesThatBelongToUserCode)
            {
                userAssembliesNamesLowercase.Add(urlNamespace.ToLowerInvariant());
            }

            //--------------------------------------------
            // CHECK FOR UNSUPPORTED METHODS:
            //--------------------------------------------
            AnalyzeHelper analyzeHelper = new AnalyzeHelper();
            analyzeHelper.Initialize(coreSupportedMethods, supportedElementsPath);

            //look if we find methods that are not supported in the xaml files:
            CheckXamlFiles(inputAssemblies, userAssembliesNamesLowercase, coreSupportedMethods, attributesToIgnoreInXamlBecauseTheyAreFromBaseClasses, listOfFilesToIgnore, supportedElementsPath, addBothPropertyAndEventWhenNotFound,
                whatToDoWhenNotSupportedMethodFound: (unsupportedMethodInfo) =>
                {
                    outputListOfUnsupportedMethods.Add(unsupportedMethodInfo);
                });

            //look in C# files:
            Check(assemblies, userAssembliesNamesLowercase, analyzeHelper, listOfFilesToIgnore, "", coreSupportedMethods,
                skipTypesWhereNoMethodIsActuallyCalled: skipTypesWhereNoMethodIsActuallyCalled,
                whatToDoWhenNotSupportedMethodFound: (unsupportedMethodInfo) =>
                {
                    outputListOfUnsupportedMethods.Add(unsupportedMethodInfo);
                });
        }


        #region checking Xaml
        public static void CheckXamlFiles(string[] inputAssemblies, HashSet<string> userAssembliesNamesLowercase, CoreSupportedMethodsContainer coreSupportedMethods, HashSet<string> attributesToIgnoreInXamlBecauseTheyAreFromBaseClasses, HashSet<string> ignoredFiles, string supportedElementsPath, bool addBothPropertyAndEventWhenNotFound, Action<UnsupportedMethodInfo> whatToDoWhenNotSupportedMethodFound)
        {
            HashSet<string> errorsAlreadyRaised = new HashSet<string>(); // This prevents raising multiple times the same error.

            AnalyzeHelper analyzeHelper = new AnalyzeHelper();
            analyzeHelper.Initialize(coreSupportedMethods, supportedElementsPath);

            foreach (string assemblyPath in inputAssemblies)
            {
                var assembly = Assembly.LoadFile(assemblyPath);
                var stream = assembly.GetManifestResourceStream(assembly.GetName().Name + ".g.resources");
                if (stream != null)
                {
                    var resourceReader = new ResourceReader(stream);
                    foreach (DictionaryEntry resource in resourceReader)
                    {
                        var fileInfo = new FileInfo(resource.Key.ToString());
                        var relativePath = new Uri(AppDomain.CurrentDomain.BaseDirectory).MakeRelativeUri(new Uri(fileInfo.FullName)).ToString();
                        if(!ignoredFiles.Contains(relativePath))
                        {
                            if (fileInfo.Extension.Equals(".xaml"))
                            {
                                string fileName = resource.Key.ToString();
                                StreamReader sr = new StreamReader((Stream)resource.Value);
                                XDocument doc = XDocument.Parse(sr.ReadToEnd(), LoadOptions.SetLineInfo);
                                //go through all the tags, check if the types and properties are supported:
                                XNode current = doc.Root;
                                CheckCurrentXNode(current, userAssembliesNamesLowercase, analyzeHelper, resource.Key.ToString(), assembly.GetName().Name, attributesToIgnoreInXamlBecauseTheyAreFromBaseClasses, addBothPropertyAndEventWhenNotFound, whatToDoWhenNotSupportedMethodFound);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether the constructor and the properties of the current Xaml Node are supported, and recursively checks for this node's children.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="userAssembliesNamesLowercase"></param>
        /// <param name="analyzeHelper"></param>
        /// <param name="fileName"></param>
        /// <param name="userAssemblyName"></param>
        /// <param name="whatToDoWhenNotSupportedMethodFound"></param>
        static void CheckCurrentXNode(XNode node, HashSet<string> userAssembliesNamesLowercase, AnalyzeHelper analyzeHelper, string fileName, string userAssemblyName, HashSet<string> attributesToIgnoreInXamlBecauseTheyAreFromBaseClasses, bool addBothPropertyAndEventWhenNotFound, Action<UnsupportedMethodInfo> whatToDoWhenNotSupportedMethodFound) //todo: change inputAssemblies into a HashSet<string>
        {
            //todo: maybe put that as a parameter to this method.
            HashSet<string> errorsAlreadyRaised = new HashSet<string>(); // This prevents raising multiple times the same error.

            //check the node itself:
            if (node is XElement)
            {
                XElement nodeAsXElement = (XElement)node;

                //we try to get the type:
                string eltName = nodeAsXElement.Name.LocalName;
                string eltNamespaceName = nodeAsXElement.Name.NamespaceName;

                string callingMethodFullName = fileName;

                string elementAssemblyName;
                string elementNamespaceName;
                GetXmlNamespaceInfo(eltNamespaceName, out elementAssemblyName, out elementNamespaceName, userAssemblyName, eltName.Split('.')[0]);

                // This means the current element is an object
                if (!eltName.Contains('.'))
                {
                    //we want the constructor for this item:
                    string methodName = ".ctor";

                    analyzeHelper.CheckMethodValidity(userAssembliesNamesLowercase,
                                                        fileName,
                                                        userAssemblyName,
                                                        errorsAlreadyRaised,
                                                        ((IXmlLineInfo)nodeAsXElement).LineNumber,
                                                        elementAssemblyName,
                                                        elementNamespaceName,
                                                        methodName,
                                                        eltName,
                                                        callingMethodFullName,
                                                        true,
                                                        whatToDoWhenNotSupportedMethodFound);

                    foreach (XAttribute attribute in nodeAsXElement.Attributes())
                    {
                        if (!attribute.IsNamespaceDeclaration)
                        {
                            bool isSpecialName = false;
                            //get the assembly and namespace name of the attribute:
                            string attrNamespaceName = attribute.Name.NamespaceName;
                            string attrLocalName = attribute.Name.LocalName;
                            string attributeTypeName;
                            string attributeName;
                            if (attrLocalName.Contains("."))
                            {
                                string[] splittedAttributeLocalName = attrLocalName.Split('.');
                                attributeTypeName = splittedAttributeLocalName[0];
                                attributeName = splittedAttributeLocalName[1];
                            }
                            else
                            {
                                attributeTypeName = eltName;
                                attributeName = attrLocalName;
                            }
                            // if the attribute has no namespace, we check if we need to use the namespace of its parent element (case of a normal property) or the default namespace (in case of an attached property).
                            if (string.IsNullOrWhiteSpace(attrNamespaceName))
                            {
                                //this means that the attribute is a normal property
                                if (attributeTypeName == eltName)
                                {
                                    attrNamespaceName = eltNamespaceName;
                                }
                                // this mean that the attribute is an attached property
                                else
                                {
                                    attrNamespaceName = nodeAsXElement.GetDefaultNamespace().NamespaceName;
                                }
                            }

                            bool isAttached = (attrNamespaceName != eltNamespaceName || attributeTypeName != eltName);

                            string attributeAssemblyName;
                            string attributeNamespaceName;
                            GetXmlNamespaceInfo(attrNamespaceName, out attributeAssemblyName, out attributeNamespaceName, userAssemblyName, attributeTypeName);

                            // Handle special properties (like Style.TargetType and Setter.Property)
                            if (attrLocalName == "TargetType" && attributeTypeName == "Style")
                            {
                                //In this case we want to get the type in <Style TargetType="XXXX" .../>
                                XName targetTypeType = GetTargetTypeValueAsXName(attribute);
                                string targetTypeAssemblyName;
                                string targetTypeNamespaceName;
                                GetXmlNamespaceInfo(targetTypeType.NamespaceName, out targetTypeAssemblyName, out targetTypeNamespaceName, userAssemblyName, targetTypeType.LocalName);

                                analyzeHelper.CheckMethodValidity(userAssembliesNamesLowercase, fileName, userAssemblyName, errorsAlreadyRaised, ((IXmlLineInfo)nodeAsXElement).LineNumber, targetTypeAssemblyName, targetTypeNamespaceName, "", targetTypeType.LocalName, callingMethodFullName, true, whatToDoWhenNotSupportedMethodFound);
                                isSpecialName = true;
                            }
                            else if (attrLocalName == "Property" && attributeTypeName == "Setter")
                            {
                                //In this case we want to get the property in <Setter Property="XXXX" .../>
                                bool? isAttachedProperty;
                                XName setterPropertyFullName = GetSetterPropertyValueAsXName(attribute, out isAttachedProperty);
                                string setterPropertyAssemblyName;
                                string setterPropertyNamespaceName;
                                GetXmlNamespaceInfo(setterPropertyFullName.NamespaceName, out setterPropertyAssemblyName, out setterPropertyNamespaceName, userAssemblyName, setterPropertyFullName.LocalName.Split('.')[0]);

                                string[] splittedSetterLocalName = setterPropertyFullName.LocalName.Split('.');
                                if(isAttachedProperty == null)
                                {
                                    bool checkIsAttachedProperty = analyzeHelper.CheckMethodValidity(userAssembliesNamesLowercase, fileName, userAssemblyName, errorsAlreadyRaised, ((IXmlLineInfo)nodeAsXElement).LineNumber, setterPropertyAssemblyName, setterPropertyNamespaceName, "Set" + splittedSetterLocalName[1], splittedSetterLocalName[0], callingMethodFullName, false, whatToDoWhenNotSupportedMethodFound)
                                                   || analyzeHelper.CheckMethodValidity(userAssembliesNamesLowercase, fileName, userAssemblyName, errorsAlreadyRaised, ((IXmlLineInfo)nodeAsXElement).LineNumber, setterPropertyAssemblyName, setterPropertyNamespaceName, "Set" + splittedSetterLocalName[1], splittedSetterLocalName[0], callingMethodFullName, false, whatToDoWhenNotSupportedMethodFound);
                                    bool checkIsRegularProperty = analyzeHelper.CheckMethodValidity(userAssembliesNamesLowercase, fileName, userAssemblyName, errorsAlreadyRaised, ((IXmlLineInfo)nodeAsXElement).LineNumber, setterPropertyAssemblyName, setterPropertyNamespaceName, "get_" + splittedSetterLocalName[1], splittedSetterLocalName[0], callingMethodFullName, false, whatToDoWhenNotSupportedMethodFound);
                                    if (!checkIsAttachedProperty && !checkIsRegularProperty)
                                    {
                                        whatToDoWhenNotSupportedMethodFound(new UnsupportedMethodInfo()
                                        {
                                            MethodName = "get_" + splittedSetterLocalName[1],
                                            TypeName = splittedSetterLocalName[0],
                                            CallingMethodFullName = callingMethodFullName,
                                            CallingMethodFileNameWithPath = fileName,
                                            CallingMethodLineNumber = ((IXmlLineInfo)nodeAsXElement).LineNumber,
                                            UserAssemblyName = userAssemblyName,
                                            MethodAssemblyName = setterPropertyAssemblyName,
                                            NeedToBeCheckedBecauseOfInheritance = true,
                                        });
                                    }
                                }
                                else
                                {
                                    if (isAttachedProperty.Value)
                                    {
                                        analyzeHelper.CheckMethodValidity(userAssembliesNamesLowercase, fileName, userAssemblyName, errorsAlreadyRaised, ((IXmlLineInfo)nodeAsXElement).LineNumber, setterPropertyAssemblyName, setterPropertyNamespaceName, "Get" + splittedSetterLocalName[1], splittedSetterLocalName[0], callingMethodFullName, true, whatToDoWhenNotSupportedMethodFound);
                                        analyzeHelper.CheckMethodValidity(userAssembliesNamesLowercase, fileName, userAssemblyName, errorsAlreadyRaised, ((IXmlLineInfo)nodeAsXElement).LineNumber, setterPropertyAssemblyName, setterPropertyNamespaceName, "Set" + splittedSetterLocalName[1], splittedSetterLocalName[0], callingMethodFullName, true, whatToDoWhenNotSupportedMethodFound);
                                    }
                                    else
                                    {
                                        analyzeHelper.CheckMethodValidity(userAssembliesNamesLowercase, fileName, userAssemblyName, errorsAlreadyRaised, ((IXmlLineInfo)nodeAsXElement).LineNumber, setterPropertyAssemblyName, setterPropertyNamespaceName, "get_" + splittedSetterLocalName[1], splittedSetterLocalName[0], callingMethodFullName, true, whatToDoWhenNotSupportedMethodFound);
                                    }
                                }
                                isSpecialName = true;
                            }

                            if (!attributesToIgnoreInXamlBecauseTheyAreFromBaseClasses.Contains(attributeName) && !isSpecialName)
                            {
                                bool methodOk;
                                if (isAttached)
                                {
                                    //check if we can find the setter
                                    methodOk = analyzeHelper.CheckMethodValidity(userAssembliesNamesLowercase, fileName, userAssemblyName, errorsAlreadyRaised, ((IXmlLineInfo)nodeAsXElement).LineNumber, attributeAssemblyName, attrNamespaceName, "Set" + attributeName, attributeTypeName, callingMethodFullName, false, whatToDoWhenNotSupportedMethodFound);
                                    if (!methodOk)
                                    {
                                        //we didn't find the setter. At this point we want to check if the attribute is an event.

                                        //Looking for the AddXXXX method.
                                        methodOk = analyzeHelper.CheckMethodValidity(userAssembliesNamesLowercase, fileName, userAssemblyName, errorsAlreadyRaised, ((IXmlLineInfo)nodeAsXElement).LineNumber, attributeAssemblyName, attrNamespaceName, "Add" + attributeName, attributeTypeName, callingMethodFullName, false, whatToDoWhenNotSupportedMethodFound);
                                    }
                                }
                                else
                                {
                                    //check if we can find the setter
                                    methodOk = analyzeHelper.CheckMethodValidity(userAssembliesNamesLowercase, fileName, userAssemblyName, errorsAlreadyRaised, ((IXmlLineInfo)nodeAsXElement).LineNumber, attributeAssemblyName, attrNamespaceName, "set_" + attributeName, attributeTypeName, callingMethodFullName, false, whatToDoWhenNotSupportedMethodFound);
                                    if (!methodOk)
                                    {
                                        // we didn't find the setter, so we try the event.
                                        analyzeHelper.CheckMethodValidity(userAssembliesNamesLowercase, fileName, userAssemblyName, errorsAlreadyRaised, ((IXmlLineInfo)nodeAsXElement).LineNumber, attributeAssemblyName, attrNamespaceName, "add_" + attributeName, attributeTypeName, callingMethodFullName, false, whatToDoWhenNotSupportedMethodFound);
                                    }
                                }

                                // At this point, we didn't find anything, so this means the attribute is not supported currently and we don't know if it is an event or a property.
                                // We have no choice to ignore the attribute or add both possibility (event and property) to the list and check for the good match later.
                                if (!methodOk)
                                {
                                    if (addBothPropertyAndEventWhenNotFound)
                                    {
                                        if (isAttached)
                                        {
                                            //Property Setter
                                            whatToDoWhenNotSupportedMethodFound(new UnsupportedMethodInfo()
                                            {
                                                MethodName = "Set" + attributeName,
                                                TypeName = attributeTypeName,
                                                CallingMethodFullName = callingMethodFullName,
                                                CallingMethodFileNameWithPath = fileName,
                                                CallingMethodLineNumber = ((IXmlLineInfo)nodeAsXElement).LineNumber,
                                                UserAssemblyName = userAssemblyName,
                                                MethodAssemblyName = attributeAssemblyName,
                                                NeedToBeCheckedBecauseOfInheritance = true,
                                            });
                                            //Property Getter
                                            whatToDoWhenNotSupportedMethodFound(new UnsupportedMethodInfo()
                                            {
                                                MethodName = "Get" + attributeName,
                                                TypeName = attributeTypeName,
                                                CallingMethodFullName = callingMethodFullName,
                                                CallingMethodFileNameWithPath = fileName,
                                                CallingMethodLineNumber = ((IXmlLineInfo)nodeAsXElement).LineNumber,
                                                UserAssemblyName = userAssemblyName,
                                                MethodAssemblyName = attributeAssemblyName,
                                                NeedToBeCheckedBecauseOfInheritance = true,
                                            });
                                            //Event Add
                                            whatToDoWhenNotSupportedMethodFound(new UnsupportedMethodInfo()
                                            {
                                                MethodName = "Add" + attributeName,
                                                TypeName = attributeTypeName,
                                                CallingMethodFullName = callingMethodFullName,
                                                CallingMethodFileNameWithPath = fileName,
                                                CallingMethodLineNumber = ((IXmlLineInfo)nodeAsXElement).LineNumber,
                                                UserAssemblyName = userAssemblyName,
                                                MethodAssemblyName = attributeAssemblyName,
                                                NeedToBeCheckedBecauseOfInheritance = true,
                                            });
                                            //Event Remove
                                            whatToDoWhenNotSupportedMethodFound(new UnsupportedMethodInfo()
                                            {
                                                MethodName = "Remove" + attributeName,
                                                TypeName = attributeTypeName,
                                                CallingMethodFullName = callingMethodFullName,
                                                CallingMethodFileNameWithPath = fileName,
                                                CallingMethodLineNumber = ((IXmlLineInfo)nodeAsXElement).LineNumber,
                                                UserAssemblyName = userAssemblyName,
                                                MethodAssemblyName = attributeAssemblyName,
                                                NeedToBeCheckedBecauseOfInheritance = true,
                                            });
                                        }
                                        else
                                        {
                                            // property setter
                                            whatToDoWhenNotSupportedMethodFound(new UnsupportedMethodInfo()
                                            {
                                                MethodName = "set_" + attributeName,
                                                TypeName = attributeTypeName,
                                                CallingMethodFullName = callingMethodFullName,
                                                CallingMethodFileNameWithPath = fileName,
                                                CallingMethodLineNumber = -1,
                                                UserAssemblyName = userAssemblyName,
                                                MethodAssemblyName = attributeAssemblyName,
                                                NeedToBeCheckedBecauseOfInheritance = true,
                                            });
                                            // property getter
                                            whatToDoWhenNotSupportedMethodFound(new UnsupportedMethodInfo()
                                            {
                                                MethodName = "get_" + attributeName,
                                                TypeName = attributeTypeName,
                                                CallingMethodFullName = callingMethodFullName,
                                                CallingMethodFileNameWithPath = fileName,
                                                CallingMethodLineNumber = ((IXmlLineInfo)nodeAsXElement).LineNumber,
                                                UserAssemblyName = userAssemblyName,
                                                MethodAssemblyName = attributeAssemblyName,
                                                NeedToBeCheckedBecauseOfInheritance = true,
                                            });
                                            // event
                                            whatToDoWhenNotSupportedMethodFound(new UnsupportedMethodInfo()
                                            {
                                                MethodName = "add_" + attributeName,
                                                TypeName = attributeTypeName,
                                                CallingMethodFullName = callingMethodFullName,
                                                CallingMethodFileNameWithPath = fileName,
                                                CallingMethodLineNumber = ((IXmlLineInfo)nodeAsXElement).LineNumber,
                                                UserAssemblyName = userAssemblyName,
                                                MethodAssemblyName = attributeAssemblyName,
                                                NeedToBeCheckedBecauseOfInheritance = true,
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else //it is a property of an element (such as <Border.Background> for example).
                {
                    bool isAttached = !IsSameType(nodeAsXElement.Name, nodeAsXElement.Parent.Name);
                    int dotIndex = eltName.IndexOf('.');
                    string typeName = eltName.Substring(0, dotIndex);
                    string propertyName = eltName.Substring(dotIndex + 1);
                    string methodName;
                    if (isAttached)
                    {
                        methodName = "Set" + propertyName;
                    }
                    else
                    {
                        methodName = "set_" + propertyName;
                    }
                    string fullMethodName = typeName + "." + methodName;
                    if (!attributesToIgnoreInXamlBecauseTheyAreFromBaseClasses.Contains(propertyName))
                    {
                        if (!analyzeHelper.CheckMethodValidity(userAssembliesNamesLowercase, fileName, userAssemblyName, errorsAlreadyRaised, ((IXmlLineInfo)nodeAsXElement).LineNumber, elementAssemblyName, elementNamespaceName, methodName, typeName, callingMethodFullName, false, whatToDoWhenNotSupportedMethodFound))
                        {
                            if (isAttached)
                            {
                                //Property Setter
                                whatToDoWhenNotSupportedMethodFound(new UnsupportedMethodInfo()
                                {
                                    MethodName = "Set" + propertyName,
                                    TypeName = typeName,
                                    CallingMethodFullName = callingMethodFullName,
                                    CallingMethodFileNameWithPath = fileName,
                                    CallingMethodLineNumber = ((IXmlLineInfo)nodeAsXElement).LineNumber,
                                    UserAssemblyName = userAssemblyName,
                                    MethodAssemblyName = elementAssemblyName,
                                    NeedToBeCheckedBecauseOfInheritance = true,
                                });
                                //Property Getter
                                whatToDoWhenNotSupportedMethodFound(new UnsupportedMethodInfo()
                                {
                                    MethodName = "Get" + propertyName,
                                    TypeName = typeName,
                                    CallingMethodFullName = callingMethodFullName,
                                    CallingMethodFileNameWithPath = fileName,
                                    CallingMethodLineNumber = ((IXmlLineInfo)nodeAsXElement).LineNumber,
                                    UserAssemblyName = userAssemblyName,
                                    MethodAssemblyName = elementAssemblyName,
                                    NeedToBeCheckedBecauseOfInheritance = true,
                                });
                            }
                            else
                            {
                                //Property Setter
                                whatToDoWhenNotSupportedMethodFound(new UnsupportedMethodInfo()
                                {
                                    MethodName = "set_" + propertyName,
                                    TypeName = typeName,
                                    CallingMethodFullName = callingMethodFullName,
                                    CallingMethodFileNameWithPath = fileName,
                                    CallingMethodLineNumber = -1,
                                    UserAssemblyName = userAssemblyName,
                                    MethodAssemblyName = elementAssemblyName,
                                    NeedToBeCheckedBecauseOfInheritance = true,
                                });
                                //Property Getter
                                whatToDoWhenNotSupportedMethodFound(new UnsupportedMethodInfo()
                                {
                                    MethodName = "get_" + propertyName,
                                    TypeName = typeName,
                                    CallingMethodFullName = callingMethodFullName,
                                    CallingMethodFileNameWithPath = fileName,
                                    CallingMethodLineNumber = ((IXmlLineInfo)nodeAsXElement).LineNumber,
                                    UserAssemblyName = userAssemblyName,
                                    MethodAssemblyName = elementAssemblyName,
                                    NeedToBeCheckedBecauseOfInheritance = true,
                                });
                            }
                        }
                    }
                }
            }

            //recursion
            if (node is XContainer)
            {
                foreach (XNode child in ((XContainer)node).Nodes())
                {
                    //todo: check if the node defines a type that has a default content property and if not, make sure the children take that into consideration.
                    CheckCurrentXNode(child, userAssembliesNamesLowercase, analyzeHelper, fileName, userAssemblyName, attributesToIgnoreInXamlBecauseTheyAreFromBaseClasses, addBothPropertyAndEventWhenNotFound, whatToDoWhenNotSupportedMethodFound);
                }
            }
        }
        #endregion

        private static void GetXmlNamespaceInfo(string xmlns, out string assemblyName, out string namespaceName, string userAssemblyName, string typeName)
        {
            string[] splittedNamespaceName = xmlns.Split(';');
            string namespaceNameValue = null;
            string assemblyNameValue = "";
            foreach (string str in splittedNamespaceName)
            {
                if (str.StartsWith("assembly"))
                {
                    assemblyNameValue = str.Substring(9); //9 because is is the length of "assembly="
                }
                else if (str.StartsWith("clr-namespace"))
                {
                    namespaceNameValue = str.Substring(14); //14 because it is the length of "clr-namespace:"
                }
                else if (str.StartsWith("http://"))
                {
                    assemblyNameValue = str;
                    namespaceNameValue = _coreSupportedMethods.ResolveNamespaceName(str, typeName);
                }
            }
            assemblyName = assemblyNameValue;
            namespaceName = namespaceNameValue;
            if (string.IsNullOrWhiteSpace(assemblyName) && namespaceName != null)
            {
                //it means we found a namespace defined like "clr-namespace:nsName" without anything about the assembly. It means we should use the current user assembly name.
                assemblyName = userAssemblyName;
            }

        }

        /// <summary>
        /// Get the XName of the type or property in the value of an XAttribute.
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        private static XName GetTargetTypeValueAsXName(XAttribute attribute)
        {
            string attributeValue = attribute.Value;
            string attributeTypeAndPropertyName;
            string AttributeNamespaceName;
            if (attributeValue.Contains(":"))
            {
                string[] splittedTargetTypeValue = attributeValue.Split(':');
                attributeTypeAndPropertyName = splittedTargetTypeValue[1];
                AttributeNamespaceName = attribute.Parent.GetNamespaceOfPrefix(splittedTargetTypeValue[0]).NamespaceName;
            }
            else
            {
                attributeTypeAndPropertyName = attributeValue;
                AttributeNamespaceName = attribute.Parent.GetDefaultNamespace().NamespaceName;
            }
            return "{" + AttributeNamespaceName + "}" + attributeTypeAndPropertyName;
        }

        private static XName GetSetterPropertyValueAsXName(XAttribute attribute, out bool? isAttachedProperty)
        {
            string attributeValue = attribute.Value;
            string attributePropertyName;
            string attributeTypeName;
            string attributeNamespaceName;
            XName propertyDeclaringType = GetTargetTypeValueAsXNameFromSetter(attribute.Parent);
            if (attributeValue.Contains("."))
            {
                string[] splittedPropertyPath = attributeValue.Split('.');
                attributePropertyName = splittedPropertyPath[1];
                if (splittedPropertyPath[0].Contains(":"))
                {
                    string[] splittedTypeName = splittedPropertyPath[0].Split(':');
                    attributeTypeName = splittedTypeName[1];
                    attributeNamespaceName = attribute.Parent.GetNamespaceOfPrefix(splittedTypeName[0]).NamespaceName;
                }
                else
                {
                    attributeTypeName = splittedPropertyPath[0];
                    attributeNamespaceName = attribute.Parent.GetDefaultNamespace().NamespaceName;
                }
            }
            else
            {
                attributePropertyName = attributeValue;
                attributeTypeName = propertyDeclaringType.LocalName;
                attributeNamespaceName = propertyDeclaringType.NamespaceName;
            }
            if (propertyDeclaringType.NamespaceName != attributeNamespaceName || propertyDeclaringType.LocalName != attributeTypeName)
            {
                isAttachedProperty = null;
            }
            else
            {
                isAttachedProperty = false;
            }
            return "{" + attributeNamespaceName + "}" + attributeTypeName + "." + attributePropertyName;
        }

        private static XName GetTargetTypeValueAsXNameFromSetter(XElement parent)
        {
            XName targetTypeValueAsXName = null;
            XElement currentElement = parent;
            do
            {
                XAttribute targetType = currentElement.Attribute("TargetType");
                if(targetType != null)
                {
                    targetTypeValueAsXName = GetTargetTypeValueAsXName(targetType);
                }
                currentElement = currentElement.Parent;

            }
            while (currentElement != null && targetTypeValueAsXName == null);
            return targetTypeValueAsXName;
        }

        /// <summary>
        /// Check if the type in the XNames is the same (it does not check if XName are the same, 
        /// for example {{namespace}Type} and {{namespace}Type.Property} have the same type since Property is declared in {{namespace}Type}).
        /// </summary>
        /// <param name="element"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private static bool IsSameType(XName element, XName parent)
        {
            return ((element.NamespaceName == parent.NamespaceName) && (element.LocalName.Split('.')[0] == parent.LocalName.Split('.')[0]));
        }

        #region checking C#
        public static void Check(AssemblyDefinition[] assembliesDefinitions, HashSet<string> userAssembliesNamesLowercase, AnalyzeHelper analyzeHelper, HashSet<string> ignoredFiles, string nameOfAssembliesThatDoNotContainUserCode, CoreSupportedMethodsContainer coreSupportedMethods, bool skipTypesWhereNoMethodIsActuallyCalled, Action<UnsupportedMethodInfo> whatToDoWhenNotSupportedMethodFound)
        {
            //try to see if there is a way to ignore .cs file.

            //todo-performance: optimize this method (only check the right assemblies, only check some types, etc.)

            HashSet<string> errorsAlreadyRaised = new HashSet<string>(); // This prevents raising multiple times the same error.
            foreach (AssemblyDefinition userAssembly in AnalyzeHelper.GetAllUserAssemblies(assembliesDefinitions, nameOfAssembliesThatDoNotContainUserCode))
            {
                HashSet<TypeReference> typesDefinedInUserCodeOrUnsupported = new HashSet<TypeReference>();
                string userAssemblyName = userAssembly.Name.Name;
                foreach (TypeDefinition type in AnalyzeHelper.GetAllTypesDefinedInAssembly(userAssembly))
                {
                    bool baseTypeIsDefinedInUserCodeOrUnsupported = false;
                    TypeReference baseType = type.BaseType;
                    if (baseType != null)
                    {
                        if (!analyzeHelper._coreSupportedMethods.ContainsType(baseType.Name, baseType.Namespace))
                        {
                            if (!analyzeHelper.IsTypeSupported(baseType))
                            {
                                baseTypeIsDefinedInUserCodeOrUnsupported = true;
                                if (!skipTypesWhereNoMethodIsActuallyCalled)
                                {
                                    typesDefinedInUserCodeOrUnsupported.Add(baseType);
                                }
                            }
                        }
                    }
                    foreach (MethodDefinition userMethod in AnalyzeHelper.GetAllMethodsDefinedInType(type))
                    {
                        if (!skipTypesWhereNoMethodIsActuallyCalled)
                        {
                            HashSet<TypeReference> unsupportedTypesUsedInTheSignatureOfTheMethod = AnalyzeSignature(userMethod, analyzeHelper);
                            typesDefinedInUserCodeOrUnsupported.UnionWith(unsupportedTypesUsedInTheSignatureOfTheMethod);
                        }
                        if (baseTypeIsDefinedInUserCodeOrUnsupported)
                        {
                            if (AnalysisUtils.IsMethodOverride(userMethod))
                            {
                                MethodDefinition methodAsInitiallyDeclaredInParentType = AnalysisUtils.LookForMethodInParents(userMethod, type);
                                if (methodAsInitiallyDeclaredInParentType != null)
                                {
                                    TypeDefinition typeWhereMethodIsOriginallyDefined = methodAsInitiallyDeclaredInParentType.DeclaringType;
                                    string assemblyWhereMethodIsOriginallyDefined = RemoveDllExtension(typeWhereMethodIsOriginallyDefined.Scope.Name);

                                    // Skip if the method was defined in one of the user's assemblies:
                                    if (!userAssembliesNamesLowercase.Contains(assemblyWhereMethodIsOriginallyDefined.ToLowerInvariant())
                                        && !userAssembliesNamesLowercase.Contains(assemblyWhereMethodIsOriginallyDefined.ToLowerInvariant() + ".dll"))
                                    {
                                        string fullMethodName = methodAsInitiallyDeclaredInParentType.DeclaringType.Namespace + "." + methodAsInitiallyDeclaredInParentType.DeclaringType.Name + "." + methodAsInitiallyDeclaredInParentType.Name;
                                        if (!analyzeHelper._coreSupportedMethods.Contains(methodAsInitiallyDeclaredInParentType.DeclaringType.Namespace, methodAsInitiallyDeclaredInParentType.DeclaringType.Name, methodAsInitiallyDeclaredInParentType.Name))
                                        {
                                            if (!analyzeHelper.IsMethodSupported(methodAsInitiallyDeclaredInParentType))
                                            {
                                                whatToDoWhenNotSupportedMethodFound(
                                                new UnsupportedMethodInfo()
                                                {
                                                    MethodName = methodAsInitiallyDeclaredInParentType.Name,
                                                    TypeName = methodAsInitiallyDeclaredInParentType.DeclaringType.Name,
                                                    CallingMethodFullName = "",
                                                    CallingMethodFileNameWithPath = "",
                                                    CallingMethodLineNumber = -1,
                                                    UserAssemblyName = userAssemblyName,
                                                    MethodAssemblyName = methodAsInitiallyDeclaredInParentType.DeclaringType.Scope.Name.Replace(".dll", ""),
                                                    NeedToBeCheckedBecauseOfInheritance = false,
                                                });
                                                errorsAlreadyRaised.Add(fullMethodName);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        foreach (MemberReferenceAndCallerInformation referencedMethodAndCorrespondingInstruction in AnalyzeHelper.GetAllMethodsReferencedInMethod(userMethod))
                        {
                            TypeReference declaringType = MemberReferenceHelper.GetDeclaringTypeResolvingOverrides(referencedMethodAndCorrespondingInstruction.MemberReference); //referencedMethodAndCorrespondingInstruction.MemberReference.DeclaringType;
                            string methodName = referencedMethodAndCorrespondingInstruction.MemberReference.Name;
                            string declaringTypeName = declaringType.Name;
                            if (declaringTypeName.Contains('['))
                                declaringTypeName = declaringTypeName.Substring(0, declaringTypeName.IndexOf('['));
                            string methodAssemblyName = RemoveDllExtension(declaringType.Scope.Name);
                            string fullMethodName = declaringType.Namespace + "." + declaringTypeName + "." + methodName;
                            string callingMethodFullName = userMethod.DeclaringType.Name + "." + userMethod.Name;

                            // Skip if the method was defined in one of the user's assemblies:
                            if (!userAssembliesNamesLowercase.Contains(methodAssemblyName.ToLowerInvariant())
                                && !userAssembliesNamesLowercase.Contains(methodAssemblyName.ToLowerInvariant() + ".dll"))
                            {
                                //if (!errorsAlreadyRaised.Contains(fullMethodName)) //if the error was raised already, do nothing
                                //{
                                if (!analyzeHelper._coreSupportedMethods.Contains(declaringType.Namespace, declaringTypeName, methodName))
                                {
                                    //we couldn't find the method, we try to look somewhere else (in supportedElements basically)
                                    if (!analyzeHelper.IsMethodSupported(referencedMethodAndCorrespondingInstruction.MemberReference))
                                    {
                                        whatToDoWhenNotSupportedMethodFound(
                                            new UnsupportedMethodInfo()
                                            {
                                                MethodName = referencedMethodAndCorrespondingInstruction.MemberReference.Name,
                                                TypeName = declaringTypeName,
                                                CallingMethodFullName = callingMethodFullName,
                                                CallingMethodFileNameWithPath = referencedMethodAndCorrespondingInstruction.CallerFileNameOrEmpty,
                                                CallingMethodLineNumber = referencedMethodAndCorrespondingInstruction.CallerLineNumberOrZero,
                                                UserAssemblyName = userAssemblyName,
                                                MethodAssemblyName = methodAssemblyName,
                                                NeedToBeCheckedBecauseOfInheritance = false,
                                            });
                                        errorsAlreadyRaised.Add(fullMethodName);
#if LOG
                                        System.Diagnostics.Debug.WriteLine(fullMethodName);
#endif
                                    }
                                }
                            }
                        }
                    }
                }
                foreach (TypeReference typeReference in typesDefinedInUserCodeOrUnsupported)
                {
                    // Skip if the type was defined in one of the user's assemblies:
                    string assemblyName = RemoveDllExtension(typeReference.Scope.Name);
                    if (!userAssembliesNamesLowercase.Contains(assemblyName.ToLowerInvariant())
                            && !userAssembliesNamesLowercase.Contains(assemblyName.ToLowerInvariant() + ".dll"))
                    {
                        whatToDoWhenNotSupportedMethodFound(
                                new UnsupportedMethodInfo()
                                {
                                    MethodName = "",
                                    TypeName = typeReference.Name,
                                    CallingMethodFullName = "",
                                    CallingMethodFileNameWithPath = "",
                                    CallingMethodLineNumber = -1,
                                    UserAssemblyName = userAssemblyName,
                                    MethodAssemblyName = typeReference.Scope.Name.Replace(".dll", ""),
                                    NeedToBeCheckedBecauseOfInheritance = false,
                                });
                    }
                }
            }
        }

        static string RemoveDllExtension(string fileName)
        {
            if (fileName.ToLower().EndsWith(".dll"))
                return fileName.Substring(0, fileName.Length - 4);
            else
                return fileName;
        }

        public static HashSet<TypeReference> AnalyzeSignature(MethodDefinition method, AnalyzeHelper analyzeHelper)
        {
            HashSet<TypeReference> unsupportedTypes = new HashSet<TypeReference>();
            if (!analyzeHelper._coreSupportedMethods.ContainsType(method.ReturnType.Name, method.ReturnType.Namespace))
            {
                if (!analyzeHelper.IsTypeSupported(method.ReturnType))
                {
                    unsupportedTypes.Add(method.ReturnType);
                }
            }
            if (method.HasParameters)
            {
                foreach (ParameterDefinition param in method.Parameters)
                {
                    if (!analyzeHelper._coreSupportedMethods.ContainsType(param.ParameterType.Name, param.ParameterType.Namespace))
                    {
                        if (!analyzeHelper.IsTypeSupported(param.ParameterType))
                        {
                            unsupportedTypes.Add(param.ParameterType);
                        }
                    }
                }
            }
            return unsupportedTypes;
        }
        #endregion
        public static AssemblyDefinition LoadAssembly(string path, string mscorlibFolderPath = null, string additionalFolderWhereToResolveAssemblies = null)
        {
            if (String.IsNullOrWhiteSpace(path))
                throw new InvalidDataException("Assembly path was empty.");

            var resolver = new DefaultAssemblyResolver();

            // Tell the resolver to look for referenced assemblies in the same folder where the loaded assembly is located:
            string containingFolderPath = Path.GetDirectoryName(path);
            resolver.AddSearchDirectory(containingFolderPath);

            // Tell the resolver to look for referenced Mscorlib and other framework assemblies in the "mscorlibFolderPath" directory:
            if (!string.IsNullOrEmpty(mscorlibFolderPath))
                resolver.AddSearchDirectory(mscorlibFolderPath);

            // Tell the resolver to look for referenced assemblies in the specified additional location:
            if (!string.IsNullOrEmpty(additionalFolderWhereToResolveAssemblies))
                resolver.AddSearchDirectory(additionalFolderWhereToResolveAssemblies);

            var readerParameters = new ReaderParameters
            {
                ReadingMode = ReadingMode.Immediate,
                ReadSymbols = false,
                AssemblyResolver = resolver
            };

            try
            {
                AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path, readerParameters);

                if (assembly == null)
                    throw new FileNotFoundException("Could not load the assembly '" + path + "'");

                return assembly;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not load the assembly '" + path + "'", ex);
            }
        }
    }
}