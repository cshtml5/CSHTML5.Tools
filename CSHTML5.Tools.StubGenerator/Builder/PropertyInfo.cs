using Mono.Cecil;
using System;

namespace StubGenerator.Common.Builder
{
    public class PropertyInfo
    {
        public PropertyDefinition Property { get; set; }

        public bool IsDependencyProperty { get; set; }

        public string FieldNameIfAny { get; set; }

        public PropertyInfo(PropertyDefinition property, bool isDependencyProperty = false, string fieldNameIfAny = null)
        {
            Property = property;
            IsDependencyProperty = isDependencyProperty;
            FieldNameIfAny = fieldNameIfAny;
        }

        public override bool Equals(object obj)
        {
            if (obj is PropertyInfo)
            {
                return Equals((PropertyInfo)obj);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public bool Equals(PropertyInfo property)
        {
            return property.Property.Name == this.Property.Name;
        }

        public static bool operator ==(PropertyInfo p1, PropertyInfo p2)
        {
            return p1.Equals(p2);
        }

        public static bool operator !=(PropertyInfo p1, PropertyInfo p2)
        {
            return !p1.Equals(p2);
        }

        public override int GetHashCode()
        {
            return this.Property.Name.GetHashCode();
        }
    }
}
