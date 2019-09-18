using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StubGenerator.Common
{
    internal struct MethodSignature
    {
        internal string Name { get; set; }
        internal string ReturnType { get; set; }
        internal bool HasParameters { get; set; }
        internal List<string> Parameters { get; set; }

        internal MethodSignature(MethodDefinition method) : this()
        {
            Name = method.Name;
            ReturnType = method.ReturnType.FullName;
            HasParameters = method.HasParameters;
            List<string> parameters = new List<string>();
            foreach (ParameterDefinition p in method.Parameters)
            {
                parameters.Add(p.ParameterType.FullName);
            }
            Parameters = parameters;
        }

        internal MethodSignature(string name, string returnType, bool hasParameters, List<string> parameters) : this()
        {
            Name = name;
            ReturnType = returnType;
            HasParameters = hasParameters;
            Parameters = parameters;
        }

        public override bool Equals(object obj)
        {
            if (obj is MethodSignature)
            {
                MethodSignature o = (MethodSignature)obj;
                bool hasSameParameters = o.HasParameters == HasParameters;
                if (hasSameParameters)
                {
                    if (HasParameters)
                    {
                        hasSameParameters = hasSameParameters && o.Parameters.Count == Parameters.Count;
                    }
                }
                else
                {
                    return false;
                }
                if (HasParameters)
                {
                    int i = 0;
                    while (hasSameParameters && i < Parameters.Count)
                    {
                        hasSameParameters = o.Parameters[0] == Parameters[0];
                        i++;
                    }
                }
                return (o.Name == Name && o.ReturnType == ReturnType && hasSameParameters);
            }
            else
            {
                return base.Equals(obj);
            }
        }
        public static bool operator== (MethodSignature m1, MethodSignature m2)
        {
            return m1.Equals(m2);
        }

        public static bool operator!= (MethodSignature m1, MethodSignature m2)
        {
            return !m1.Equals(m2);
        }

        public static bool IsNull(MethodSignature sig)
        {
            return sig.Name == null && sig.Parameters == null && sig.ReturnType == null && sig.HasParameters == false;
        }

        public override int GetHashCode()
        {
            int hashcode = Name.GetHashCode() + ReturnType.GetHashCode() + HasParameters.GetHashCode();
            int i = 1;
            if(HasParameters)
            {
                foreach (string p in Parameters)
                {
                    hashcode += p.GetHashCode() * i++;
                }
            }
            return hashcode;
        }
    }
}
