using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StubGenerator.Common.Options
{
    /// <summary>
    /// Enum used to set methods (including constructors) output options to the Stub Generator.
    /// </summary>
    public enum OutputMethodOptions
    {
        /// <summary> 
        /// The method body will be written as a set of 'out' parameters and a 'return' value.
        /// </summary>
        OUTPUT_RETURN_TYPE = 2,
        /// <summary> 
        /// The method body will be written as a set of 'out' parameters and a 'return' value which is not null. (not working yet)
        /// </summary>
        OUTPUT_RETURN_TYPE_NOT_NULL = 1,
        /// <summary>
        /// The method body will be replaced by a 'throw new NotImplementedException()'.
        /// </summary>
        OUTPUT_NOT_IMPLEMENTED = 3,
    }

    /// <summary>
    /// Enum used to set properties output options to the Stub Generator.
    /// </summary>
    public enum OutputPropertyOptions
    {
        /// <summary>
        /// The property getter will be written as a 'return default(getter value)'.
        /// The property setter will be empty.
        /// </summary>
        OUTPUT_RETURN_TYPE = 2,
        /// <summary>
        /// The property getter will be written as a 'return new Type()' if Type has a constructor. Else it returns the default value of the type.
        /// The property setter will be empty.
        /// </summary>
        OUTPUT_RETURN_TYPE_NOT_NULL = 1,
        /// <summary>
        /// The property getter and setter bodies will be replaced by a 'throw new NotImplementedException()'.
        /// </summary>
        OUTPUT_NOT_IMPLEMENTED = 3,
        /// <summary>
        /// The property getter will return the value of a generated private field and the property setter
        /// will set the value of this same field.
        /// For instance the property Property will be generated as follow : Property { get { return _property; } set { _property = value; } }
        /// </summary>
        OUTPUT_PRIVATE_FIELD = 0,
    }

    /// <summary>
    /// Enum used to set events output options to the Stub Generator.
    /// </summary>
    public enum OutputEventOptions
    {
        /// <summary>
        /// The event will be auto-implemented.
        /// </summary>
        AUTO_IMPLEMENT,
        /// <summary>
        /// The event body will be filled with empty Add and Remove methods.
        /// </summary>
        OUTPUT_EMPTY_IMPLEMENTATION,
        /// <summary>
        /// The event body will be replaced by a 'throw new NotImplementedException()'.
        /// </summary>
        OUTPUT_NOT_IMPLEMENTED
    }
}
