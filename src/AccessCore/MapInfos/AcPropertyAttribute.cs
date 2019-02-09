using System;

namespace AccessCore.Repository.MapInfos
{
    /// <summary>
    /// Attribute to allow AccessCore constructing map info without 
    /// any map description file
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AcPropertyAttribute : Attribute
    {       
        /// <summary>
        /// Name of paramter in stored procedure.
        /// </summary>
        public string SpParameterName { get; set; }
    }
}