using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer
{
    internal class CoreSupportedMethodTypeItem
    {
        string _typeName;
        internal string _namespace;
        internal string _baseTypeName = null;
        internal string _baseTypeNamespace = null;
        HashSet<string> _methodNames = new HashSet<string>();

        internal CoreSupportedMethodTypeItem(string typeName, string baseTypeName, string @namespace, string baseTypeNamespace)
        {
            _typeName = typeName;
            _baseTypeName = baseTypeName;
            _namespace = @namespace;
            _baseTypeNamespace = baseTypeNamespace;
        }

        internal void Add(string methodName)
        {
            if(!_methodNames.Contains(methodName))
            {
                _methodNames.Add(methodName);
            }
        }

        internal bool Contains(string methodName)
        {
            return _methodNames.Contains(methodName);
        }
    }
}
