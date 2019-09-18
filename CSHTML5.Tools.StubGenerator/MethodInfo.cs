using DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer;

namespace StubGenerator.Common
{
    internal struct MethodInfo
    {
        internal string AssemblyName { get; set; }

        internal string TypeName { get; set; }

        internal string MethodName { get; set; }

        internal bool NeedToBeCheckedBecauseOfInheritance { get; set; }

        internal MethodInfo(string assemblyName, string typeName, string methodName, bool needToBeCheckedBecauseOfInheritance) : this()
        {
            AssemblyName = assemblyName;
            TypeName = typeName;
            MethodName = methodName;
            NeedToBeCheckedBecauseOfInheritance = needToBeCheckedBecauseOfInheritance;
        }

        internal MethodInfo(UnsupportedMethodInfo methodInfo) : this()
        {
            AssemblyName = methodInfo.MethodAssemblyName;
            TypeName = methodInfo.TypeName;
            MethodName = methodInfo.MethodName;
            NeedToBeCheckedBecauseOfInheritance = methodInfo.NeedToBeCheckedBecauseOfInheritance;
        }

        public override bool Equals(object obj)
        {
            if(obj is MethodInfo)
            {
                MethodInfo o = (MethodInfo)obj;
                return (o.AssemblyName == this.AssemblyName && o.TypeName == this.TypeName && o.MethodName == this.MethodName);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return AssemblyName.GetHashCode() * 100 + TypeName.GetHashCode() * 10 + MethodName.GetHashCode();
        }
    }
}
