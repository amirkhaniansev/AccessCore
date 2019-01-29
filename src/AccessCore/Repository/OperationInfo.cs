using System.Collections.Generic;

namespace AccessCore.Repository
{
    /// <summary>
    /// Class for operation information
    /// </summary>
    internal class OperationInfo : OperationBase
    {
        /// <summary>
        /// Gets or sets Parameters names map info
        /// </summary>
        public Dictionary<string, string> ParametersMappInfo { get; set; }

        /// <summary>
        /// Gets or sets return data type
        /// </summary>
        public ReturnDataType ReturnDataType { get; set; }
    }
}
