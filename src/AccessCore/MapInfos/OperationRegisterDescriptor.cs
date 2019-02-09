using System;

namespace AccessCore.Repository.MapInfos
{
    /// <summary>
    /// Class for describing operation registration.
    /// </summary>
    internal class OperationRegisterDescriptor
    {
        /// <summary>
        /// Gets or sets operation name.
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        /// Gets or sets stored procedure name.
        /// </summary>
        public string SpName { get; set; }

        /// <summary>
        /// Gets or sets input type.
        /// </summary>
        public Type InputType { get; set; }

        /// <summary>
        /// Gets or sets return data type.
        /// </summary>
        public ReturnDataType ReturnDataType { get; set; }

        /// <summary>
        /// Gets or sets stored procedure primitive parameter name.
        /// </summary>
        public string SpPrimitiveParameterName { get; set; }

        /// <summary>
        /// Gets hash code.
        /// </summary>
        /// <returns>hash code.</returns>
        public override int GetHashCode()
        {
            return OperationName.GetHashCode();
        }

        /// <summary>
        /// Determines if the objects the object is equal to the given object.s
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>true if the object is equal to the given object, false otherwise. </returns>
        public override bool Equals(object obj)
        {
            var descriptor = obj as OperationRegisterDescriptor;
            
            if(descriptor == null)
            {
                return false;
            }

            return object.ReferenceEquals(this, descriptor) ||
                   this.GetHashCode() == descriptor.GetHashCode() ||
                   this.OperationName == descriptor.OperationName;
        }
    }
}