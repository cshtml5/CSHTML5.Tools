using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer
{
    public class UnsupportedMethodInfo
    {

        /// <summary>
        /// The name of the unsupported method.
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// The name of the unsupported type.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// The location (full method name) where the unsupported method is used/called.
        /// </summary>
        public string CallingMethodFullName { get; set; }

        /// <summary>
        /// The location (name of the source code file with its path) where the unsupported method is used/called.
        /// </summary>
        public string CallingMethodFileNameWithPath { get; set; }

        /// <summary>
        /// The location (the line numner in the source code file) where the unsupported method is used/called.
        /// </summary>
        public int CallingMethodLineNumber { get; set; }

        /// <summary>
        /// The name of the assembly where the unsupported missing method is used/called.
        /// </summary>
        public string UserAssemblyName { get; set; }

        /// <summary>
        /// The name of the assembly where the unsupported method is located (eg. "mscorlib").
        /// </summary>
        public string MethodAssemblyName { get; set; }

        /// <summary>
        /// Set to true when we are not sure the method is defined in the type but we know it is in one of his parents.
        /// </summary>
        public bool NeedToBeCheckedBecauseOfInheritance { get; set; }

    }
}


