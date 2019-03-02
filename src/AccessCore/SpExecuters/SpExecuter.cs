using System;
using System.Reflection;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.Concurrent;
using AccessCore.Helpers;

namespace AccessCore.SpExecuters
{
    /// <summary>
    /// Base class for executing stored procedure.
    /// Override this class for the given database.
    /// AccessCore providers MySql and MsSql overrides.
    /// However AccessCore can be used for any relational database.
    /// </summary>
    public abstract class SpExecuter : ISpExecuter
    {
        #region fields

        /// <summary>
        /// Connection string
        /// </summary>
        protected readonly string _connString;

        /// <summary>
        /// Cached properties for entities.
        /// </summary>
        protected readonly ConcurrentDictionary<Type, PropertyInfo[]> _cachedProperties;

        /// <summary>
        /// Cached mappers.
        /// </summary>
        protected readonly ConcurrentDictionary<Type, Delegate> _cachedMappers;

        #endregion

        #region constructors

        /// <summary>
        /// Creates new instance of <see cref="SpExecuter"/>
        /// </summary>
        /// <param name="connString">database connection string.</param>
        public SpExecuter(string connString)
        {
            // intializing
            this._connString = connString;
            this._cachedProperties = new ConcurrentDictionary<Type, PropertyInfo[]>();            
            this._cachedMappers = new ConcurrentDictionary<Type, Delegate>();
        }

        #endregion

        #region public API

        /// <summary>
        /// Executes stored procedure asynchronously which return type is enumerable.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <param name="procedureName">stored procedure name</param>
        /// <param name="parameters">parameters</param>
        /// <returns>enumerable</returns>
        public async Task<IEnumerable<TEntity>> ExecuteSpAsync<TEntity>(
            string procedureName, 
            IEnumerable<KeyValuePair<string, object>> parameters = null) 
            where TEntity : class
        {
            return await this.GetSpResult<TEntity, IEnumerable<TEntity>>(
                procedureName,
                parameters,
                StoredProcedureReturnData.Enumerable);
        }

        /// <summary>
        /// Executes stored procedure asynchrnously which return data is one row.
        /// </summary>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="procedureName">Stored procedure name.</param>
        /// <param name="parameters">Stored proceduer parameters</param>
        /// <returns>Result which is one row in SQL table.</returns>
        public async Task<TResult> ExecuteEntitySpAsync<TResult>(
            string procedureName, 
            IEnumerable<KeyValuePair<string, object>> parameters = null) 
            where TResult : class
        {
            return await this.GetSpResult<TResult, TResult>(
                procedureName,
                parameters,
                StoredProcedureReturnData.OneRow);
        }

        /// <summary>
        /// Executes store procedure asynchronously which return data is scalar.
        /// </summary>
        /// <typeparam name="TResult">Type of Result</typeparam>
        /// <param name="procedureName">Procedure name</param>
        /// <param name="parameters">Procedure Parameters</param>
        /// <returns>Scalar result</returns>
        public async Task<TResult> ExecuteScalarSpAsync<TResult>(
            string procedureName, 
            IEnumerable<KeyValuePair<string, object>> parameters = null) 
            where TResult : class
        {
            return await this.GetSpResult<TResult, TResult>(
                procedureName,
                parameters,
                StoredProcedureReturnData.Scalar);
        }

        /// <summary>
        /// Executes store procedure asynchroosuly which doesn't have return data.
        /// </summary>
        /// <param name="procedureName">Procedure name</param>
        /// <param name="parameters">Procedure parameters</param>
        /// <returns>Amount of affected rows</returns>
        public async Task<int> ExecuteSpNonQueryAsync(
            string procedureName, 
            IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            return await this.GetSpResult<object, int>(
                procedureName,
                parameters,
                StoredProcedureReturnData.Nothing);
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Executes the stored procedure asynchonously.
        /// Overriding this method you can have AccessCore functionality with any database engine.
        /// </summary>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="storedProcedure">stored procedure</param>
        /// <returns>result</returns>
        protected abstract Task<object> ExecuteAsync<TResult>(StoredProcedure storedProcedure)
            where TResult : class;

        /// <summary>
        /// Retrieves enumerable from reader.
        /// </summary>
        /// <typeparam name="TResult">Type of result.</typeparam>
        /// <typeparam name="TDataReader">Type of data reader.</typeparam>
        /// <param name="reader">reader</param>
        /// <returns>result</returns>
        protected TResult RetrieveEnumerableFromReader<TResult, TDataReader>(TDataReader reader) 
            where TResult : class
            where TDataReader : DbDataReader
        {
            // checking argument
            if (reader == null)
            {
                throw new ArgumentNullException("Reader");
            }

            if (!reader.HasRows)
                return null;

            // getting properties
            var properties = this.GetProperties<TResult>();

            // getting mapper
            var mapper = this.GetMapper<TResult, TDataReader>(properties);

            // executing mapper
            return mapper(reader);
        }

        /// <summary>
        /// Gets mapper from cached mappers if it exists, otherwise creates the new one.
        /// </summary>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <typeparam name="TDataReader">Type of data reader</typeparam>
        /// <param name="properties">Properties</param>
        /// <returns>Sql Data reader to object mapper</returns>
        protected Func<TDataReader, TResult> GetMapper<TResult, TDataReader>(PropertyInfo[] properties)
            where TDataReader : DbDataReader
        {
            // getting result type
            var resultType = typeof(TResult);

            //  checking if the mapper exists in cached mappers
            if (this._cachedMappers.ContainsKey(resultType))
                return (Func<TDataReader, TResult>)this._cachedMappers[resultType];

            // getting type of Sql Data Reader
            var sqlReaderType = typeof(SqlDataReader);

            // creating list of variable expressions
            var expressions = new List<Expression>();

            // creating list of expressions
            var variables = new List<ParameterExpression>();

            // constructing sql reader parameter expression
            var sourceExpr = Expression.Parameter(sqlReaderType);

            // constructing result expression
            var result = Expression.Parameter(resultType);

            // adding variables to collection of variables expressions
            variables.Add(result);

            // constructing initializing expression
            var init = Expression.New(resultType);

            // constructing assign expression which assigned the initialized value to result
            var resultInitAssign = Expression.Assign(result, init);

            // adding result assignment expression to list of expressions
            expressions.Add(resultInitAssign);

            // loop over the properties constructing the block of assignment
            foreach(var property in properties)
            {
                var propExpr = Expression.Property(result, property.Name);

                var columnGetExpr = Expression.Property(sourceExpr,"Item",Expression.Constant(property.Name,typeof(string)));

                var value = Expression.Convert(columnGetExpr, property.PropertyType);

                var assignExpr = Expression.Assign(propExpr, value);

                expressions.Add(assignExpr);
            }

            // constructing label target
            var target = Expression.Label(resultType);

            // constructing retrun expression
            var returnExpr = Expression.Return(target, result, resultType);

            // constructing label expression
            var label = Expression.Label(target, Expression.Default(resultType));

            // adding return expression and label to list of expressions
            expressions.Add(returnExpr);
            expressions.Add(label);

            // constructing body  
            var body = Expression.Block(variables, expressions);

            // constructing lambda
            var lambda = Expression.Lambda<Func<TDataReader, TResult>>(body, sourceExpr);

            // compiling lambda expression
            var mapper = lambda.Compile();

            // adding compiled mapper to cached mappers
            if(!this._cachedMappers.TryAdd(resultType, mapper))
                throw new Exception("CachedMapper");

            // returning mapper
            return mapper;
        }

        #endregion

        #region private helper methods

        /// <summary>
        /// Gets properties from entity.
        /// </summary>
        /// <typeparam name="TEntity">type of entity</typeparam>
        /// <returns>array of property infos</returns>
        private PropertyInfo[] GetProperties<TEntity>()
        {
            // getting type of properties
            var type = typeof(TEntity);

            // checking if dictionary of cached properties contains properties of the given type
            if (this._cachedProperties.ContainsKey(type))
                return this._cachedProperties[type];

            // getting properties
            var properties = type.GetProperties();

            // adding them to cached properties
            if(!this._cachedProperties.TryAdd(type, properties))
                throw new Exception("CachedMapper");

            // returning properties
            return properties;
        }
        
        /// <summary>
        /// Gets stored procedure result.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <typeparam name="TResult">Type of result.</typeparam>
        /// <param name="name">name of stored procedure</param>
        /// <param name="parameters">parameters of stored procedure</param>
        /// <param name="storedProcedureReturnData">stored procedure return data type</param>
        /// <returns>result</returns>
        private async Task<TResult> GetSpResult<TEntity, TResult>(
            string name,
            IEnumerable<KeyValuePair<string, object>> parameters,
            StoredProcedureReturnData storedProcedureReturnData)
            where TEntity : class
        {
            var sp = SpHelper.CreateSp(name, parameters, storedProcedureReturnData);
            var dbResult = await this.ExecuteAsync<TEntity>(sp);
            var result = SpHelper.GetResult<TResult>(dbResult);
            return result;
        }

        #endregion
    }
}