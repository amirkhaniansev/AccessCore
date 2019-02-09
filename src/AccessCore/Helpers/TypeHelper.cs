using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using AccessCore.Repository.MapInfos;

namespace AccessCore.Helpers
{
    /// <summary>
    /// Class to help doing type operations
    /// </summary>
    internal static class TypeHelper
    {
        /// <summary>
        /// Checks if type is .NET primitive type or 
        /// type is one of these : 
        /// <see cref="decimal"/>, 
        /// <see cref="string"/>, 
        /// <see cref="DateTime"/>, 
        /// <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="type">type</param>
        /// <returns>true if type is .NET primitive type or type is one of these:
        /// decimal, string, DateTime, DateTimeOffset
        /// </returns>
        public static bool IsPrimitive(Type type)
        {
            return TypeHelper.IsPrimitive(
                type,
                typeof(decimal),
                typeof(string),
                typeof(DateTime),
                typeof(DateTimeOffset));
        }

        /// <summary>
        /// Checks if type is primitive
        /// </summary>
        /// <param name="type">type</param>
        /// <param name="primitives">types that must be treated as primitives.</param>
        /// <returns>true if type is primitive, false otherwise.</returns>
        public static bool IsPrimitive(Type type, params Type[] primitives)
        {
            return type.IsPrimitive || primitives.Contains(type);
        }

        /// <summary>
        /// Gets properties which have the given attribute.
        /// </summary>
        /// <param name="inputType">input type</param>
        /// <param name="attributeType">custom attribute type</param>
        /// <returns>properties which have the given attribute.</returns>
        public static IEnumerable<PropertyInfo> GetProperties(Type inputType, Type attributeType)
        {
            return inputType.GetProperties().Where(property => TypeHelper.HasAttribute(property));
        }

        /// <summary>
        /// Determines whether the property has custom attribute.
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns>true if porperty has the given attribute, false otherwise.</returns>
        private static bool HasAttribute(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttributes(typeof(AcPropertyAttribute)).Count() != 0;
        }
    }
}