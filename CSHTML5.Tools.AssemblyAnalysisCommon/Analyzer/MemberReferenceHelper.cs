using DotNetForHtml5.PrivateTools.AssemblyAnalysisCommon;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer
{
    static class MemberReferenceHelper
    {
        static Dictionary<MemberReference, TypeReference> Cache = new Dictionary<MemberReference, TypeReference>();

        /// <summary>
        /// If the member is "override", this method returns the type where the
        /// corresponding "virtual" member is defined, otherwise it returns the
        /// member declaring type.
        /// </summary>
        public static TypeReference GetDeclaringTypeResolvingOverrides(MemberReference memberReference)
        {
            if (Cache.ContainsKey(memberReference))
            {
                return Cache[memberReference];
            }
            else
            {
                TypeReference declaringType = memberReference.DeclaringType;
                string memberName = memberReference.Name;

                if (memberReference is MethodReference)
                {
                    MethodReference methodReference = (MethodReference)memberReference;
                    MethodDefinition methodDefinition = null;
                    try
                    {
                        methodDefinition = methodReference.Resolve();
                    }
                    catch { }
                    if (methodDefinition != null)
                    {
                        if (AnalysisUtils.IsMethodOverride(methodDefinition))
                        {
                            TypeDefinition typeDefinition = AnalysisUtils.GetTypeDefinitionFromTypeReference(declaringType, null);
                            MethodDefinition methodIsFirstDefinitionInParentType = AnalysisUtils.LookForMethodInParents(methodDefinition, typeDefinition);
                            if (methodIsFirstDefinitionInParentType != null)
                            {
                                return methodIsFirstDefinitionInParentType.DeclaringType;
                            }
                        }
                    }
                }

                Cache.Add(memberReference, declaringType);

                return declaringType;
            }
        }
    }
}
