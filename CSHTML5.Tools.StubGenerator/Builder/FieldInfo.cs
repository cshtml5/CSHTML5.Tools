using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace StubGenerator.Common.Builder
{
    public class FieldInfo
    {
        public FieldDefinition Field { get; set; }

        public bool IsDependencyProperty { get; set; }

        public bool IsAttachedProperty { get; set; }

        public TypeReference DependencyPropertyTypeIfAny { get; set; }

        public string PropertyNameIfDependencyProperty { get; set; }

        public FieldInfo(FieldDefinition field)
        {
            Field = field;
            IsDependencyProperty = false;
            IsAttachedProperty = false;
            DependencyPropertyTypeIfAny = null;
            PropertyNameIfDependencyProperty = null;
        }

        public FieldInfo(FieldDefinition field, bool isDependencyProperty = false, bool isAttachedProperty = false, TypeReference dependencyPropertyTypeIfAny = null, string propertyName = null)
        {
            Field = field;
            IsDependencyProperty = isDependencyProperty;
            IsAttachedProperty = isAttachedProperty;
            DependencyPropertyTypeIfAny = dependencyPropertyTypeIfAny;
            PropertyNameIfDependencyProperty = propertyName;
        }

        public FieldInfo(string fieldName, TypeReference fieldType, bool isStatic = false, bool isDependencyProperty = false, bool isAttachedProperty = false, TypeReference dependencyPropertyTypeIfAny = null, string propertyName = null)
        {
            FieldAttributes attributes;
            if (isStatic)
            {
                if (isDependencyProperty)
                {
                    attributes = FieldAttributes.Static | FieldAttributes.Public;
                }
                else
                {
                    attributes = FieldAttributes.Static | FieldAttributes.Private;
                }
            }
            else
            {
                if (isDependencyProperty)
                {
                    attributes = FieldAttributes.Public;
                }
                else
                {
                    attributes = FieldAttributes.Private;
                }
            }
            Field = new FieldDefinition(fieldName, attributes, fieldType);
            IsDependencyProperty = isDependencyProperty;
            IsAttachedProperty = isAttachedProperty;
            DependencyPropertyTypeIfAny = dependencyPropertyTypeIfAny;
            PropertyNameIfDependencyProperty = propertyName;
        }

        public override bool Equals(object obj)
        {
            if (obj is FieldInfo)
            {
                return Equals((FieldInfo)obj);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public bool Equals(FieldInfo field)
        {
            return field.Field.Name == this.Field.Name;
        }

        public static bool operator ==(FieldInfo f1, FieldInfo f2)
        {
            return f1.Equals(f2);
        }

        public static bool operator !=(FieldInfo f1, FieldInfo f2)
        {
            return !f1.Equals(f2);
        }

        public override int GetHashCode()
        {
            return this.Field.Name.GetHashCode();
        }
    }
}
