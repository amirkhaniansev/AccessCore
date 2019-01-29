using System;
using System.Collections.Generic;
using System.Text;

namespace AccessCore.Repository
{
    /// <summary>
    /// Base class for descriping SQL operation
    /// </summary>
    internal class OperationBase
    {
        /// <summary>
        /// Gets or sets Operation name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets Stored procedur name
        /// </summary>
        public string SpName { get; set; }
    }
}
