using Mono.Cecil;
using System;

namespace StubGenerator.Common.Builder
{
    public class MethodInfo
    {
        private MethodSignature _signature;
        private MethodSignature Signature
        {
            get
            {
                if (MethodSignature.IsNull(_signature))
                {
                    _signature = new MethodSignature(Method);
                }
                return _signature;
            }
        }

        public MethodDefinition Method { get; set; }

        public bool IsDependencyPropertyGetter { get; set; }

        public bool IsDependencyPropertySetter { get; set; }

        public string FieldNameIfAny { get; set; }

        public MethodInfo(MethodDefinition method, bool isDependencyPropertyGetter = false, bool isDependencyPropertySetter = false, string fieldNameIfAny = null)
        {
            Method = method;
            IsDependencyPropertyGetter = isDependencyPropertyGetter;
            IsDependencyPropertySetter = isDependencyPropertySetter;
            FieldNameIfAny = fieldNameIfAny;
        }

        public override bool Equals(object obj)
        {
            if(obj is MethodInfo)
            {
                return Equals((MethodInfo)obj);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public bool Equals(MethodInfo method)
        {
            return method.Signature == this.Signature;
        }

        public static bool operator ==(MethodInfo m1, MethodInfo m2)
        {
            return m1.Equals(m2);
        }

        public static bool operator !=(MethodInfo m1, MethodInfo m2)
        {
            return !m1.Equals(m2);
        }

        public override int GetHashCode()
        {
            return this.Signature.GetHashCode();
        }
    }
}
