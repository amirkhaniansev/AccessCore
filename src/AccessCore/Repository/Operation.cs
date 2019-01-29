using System.Collections.Generic;

namespace AccessCore.Repository
{
    /// <summary>
    /// Class for describing SQL operation
    /// </summary>
    internal class Operation : OperationBase
    {
        /// <summary>
        /// Gets or sets parameters
        /// </summary>
        public List<Parameter> Parameters { get; set; }

        /// <summary>
        /// Gets or sets return data type
        /// </summary>
        public string ReturnDataType { get; set; }
    }
}