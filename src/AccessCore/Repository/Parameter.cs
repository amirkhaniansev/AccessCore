namespace AccessCore.Repository
{
    /// <summary>
    /// Class for describing SQL operation parameter
    /// </summary>
    internal class Parameter
    {
        /// <summary>
        /// Gets or sets parameter name
        /// </summary>
        public string ParameterName { get; set; }

        /// <summary>
        /// Gets or sets stored procedure parameter name
        /// </summary>
        public string SpParameterName { get; set; }
    }
}
