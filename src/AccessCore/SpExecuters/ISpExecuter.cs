using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccessCore.SpExecuters
{
    /// <summary>
    /// Interface for executing stored procedures.
    /// </summary>
    public interface ISpExecuter
    {
        /// <summary>
        /// Executes stored procedure asynchronously which return type is enumerable.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <param name="procedureName">stored procedure name</param>
        /// <param name="parameters">parameters</param>
        /// <returns>enumerable</returns>
        Task<IEnumerable<TEntity>> ExecuteSpAsync<TEntity>(
            string procedureName, 
            IEnumerable<KeyValuePair<string, object>> parameters = null)
            where TEntity : class;

        /// <summary>
        /// Executes stored procedure asynchrnously which return data is one row.
        /// </summary>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="procedureName">Stored procedure name.</param>
        /// <param name="parameters">Stored proceduer parameters</param>
        /// <returns>Result which is one row in SQL table.</returns>
        Task<TResult> ExecuteEntitySpAsync<TResult>(
            string procedureName, 
            IEnumerable<KeyValuePair<string, object>> parameters = null)
            where TResult : class;
        
        /// <summary>
        /// Executes store procedure asynchronously which return data is scalar.
        /// </summary>
        /// <typeparam name="TResult">Type of Result</typeparam>
        /// <param name="procedureName">Procedure name</param>
        /// <param name="parameters">Procedure Parameters</param>
        /// <returns>Scalar result</returns>
        Task<TResult> ExecuteScalarSpAsync<TResult>(
            string procedureName, 
            IEnumerable<KeyValuePair<string, object>> parameters = null)
            where TResult : class;

        /// <summary>
        /// Executes store procedure asynchroosuly which doesn't have return data.
        /// </summary>
        /// <param name="procedureName">Procedure name</param>
        /// <param name="parameters">Procedure parameters</param>
        /// <returns>Amount of affected rows</returns>
        Task<int> ExecuteSpNonQueryAsync(
            string procedureName, 
            IEnumerable<KeyValuePair<string, object>> parameters = null);
    }
}