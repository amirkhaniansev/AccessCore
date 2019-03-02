using System.Collections.Generic;
using AccessCore.SpExecuters;

namespace AccessCore.Helpers
{
    /// <summary>
    /// Helper class for some operations with stored procedures
    /// </summary>
    internal static class SpHelper
    {
        /// <summary>
        /// Creates new instance of <see cref="StoredProcedure"/>
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="parameters">parameters</param>
        /// <param name="storedProcedureReturnData">return data type</param>
        /// <returns>stored procedure</returns>
        internal static StoredProcedure CreateSp(
            string name,
            IEnumerable<KeyValuePair<string, object>> parameters,
            StoredProcedureReturnData storedProcedureReturnData)
        {
            return new StoredProcedure
            {
                Name = name,
                Parameters = parameters,
                StoredProcedureReturnData = storedProcedureReturnData
            };
        }

        /// <summary>
        /// Gets result from the abstract sp result.
        /// </summary>
        /// <typeparam name="TResult">Type of result.</typeparam>
        /// <param name="result">result</param>
        /// <returns>result</returns>
        internal static TResult GetResult<TResult>(object result)
        {
            if (result is TResult)
                return (TResult)result;

            return default(TResult);
        }
    }
}