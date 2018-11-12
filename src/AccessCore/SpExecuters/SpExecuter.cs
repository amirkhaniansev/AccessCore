using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace AccessCore.SpExecuters
{
    /// <summary>
    /// Class for accessing data from database executing procedures.
    /// Works only with MS SQL server. 
    /// </summary>
    public class SpExecuter : ISpExecuter
    {
        /// <summary>
        /// SQL server connection string
        /// </summary>
        private readonly string _connString;

        /// <summary>
        /// Dictionary of  cached properties
        /// </summary>
        private readonly ConcurrentDictionary<Type, PropertyInfo[]> _cachedProperties;

        /// <summary>
        /// Dictionary of cached mappers
        /// </summary>
        private readonly ConcurrentDictionary<Type, Delegate> _cachedMappers;

        /// <summary>
        /// Gets connection string
        /// </summary>
        public string ConnectionString => this._connString;

        /// <summary>
        /// Creates new instance of <see cref="SpExecuter"/>
        /// </summary>
        public SpExecuter() { }

        /// <summary>
        /// Creates new instance of <see cref="SpExecuter"/> with the given connection string.
        /// </summary>
        /// <param name="connString"></param>
        public SpExecuter(string connString)
        {
            // sets connection string
            this._connString = connString;

            // initializes cached properties
            this._cachedProperties = new ConcurrentDictionary<Type, PropertyInfo[]>();

            // initializes cached mappers
            this._cachedMappers = new ConcurrentDictionary<Type, Delegate>();
        }

        /// <summary>
        /// Executes store procedure which return data is enumerable.
        /// </summary>
        /// <typeparam name="TResult">Type of Result.</typeparam>
        /// <param name="procedureName">Procedure name</param>
        /// <param name="parameters">Procedure parameters</param>
        /// <returns>Enumerable of rows</returns>
        public IEnumerable<TResult> ExecuteSp<TResult>(string procedureName, IEnumerable<KeyValuePair<string, object>> parameters = null)
            where TResult : class
        {
            // returning result
            return (IEnumerable<TResult>)this.Execute<TResult>(new StoredProcedure
            {
                Name = procedureName,
                StoredProcedureReturnData = StoredProcedureReturnData.Enumerable,
                Parameters = parameters
            });
        }

        /// <summary>
        /// Executes stored procedure which return data is one row.
        /// </summary>
        /// <typeparam name="TResult">Type of resutlt</typeparam>
        /// <param name="procedureName">Stored procedure name.</param>
        /// <param name="parameters">Stored proceduer parameters</param>
        /// <returns>Result which is one row in SQL table.</returns>
        public TResult ExecuteEntitySp<TResult>(string procedureName, IEnumerable<KeyValuePair<string, object>> parameters = null)
            where TResult : class
        {
            // returning result
            return (TResult)this.Execute<TResult>(new StoredProcedure
            {
                Name = procedureName,
                StoredProcedureReturnData = StoredProcedureReturnData.OneRow,
                Parameters = parameters
            });
        }

        /// <summary>
        /// Executes store procedure asynchronously which return data is enumerable.
        /// </summary>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="procedureName">Procedure name</param>
        /// <param name="parameters">Parameters</param>
        /// <returns>Enumerable of rows</returns>
        public Task<IEnumerable<TResult>> ExecuteSpAsync<TResult>(string procedureName,
                    IEnumerable<KeyValuePair<string, object>> parameters = null) where TResult : class
        {
            var task = new Task<IEnumerable<TResult>>(() =>
                   this.ExecuteSp<TResult>(procedureName, parameters));

            task.Start();

            return task;
        }

        /// <summary>
        /// Executes store procedure which return data is scalar.
        /// </summary>
        /// <typeparam name="TResult">Type of Result</typeparam>
        /// <param name="procedureName">Procedure name</param>
        /// <param name="parameters">Procedure Parameters</param>
        /// <returns>Scalar result</returns>
        public TResult ExecuteScalarSp<TResult>(string procedureName, IEnumerable<KeyValuePair<string, object>> parameters = null)
            where TResult : class
        {
            // returning result
            return (TResult)this.Execute<TResult>(new StoredProcedure
            {
                Name = procedureName,
                StoredProcedureReturnData = StoredProcedureReturnData.Scalar,
                Parameters = parameters
            });
        }

        /// <summary>
        /// Executes store procedure which doesn't have return data.
        /// </summary>
        /// <param name="procedureName">Procedure name</param>
        /// <param name="parameters">Procedure parameters</param>
        /// <returns>Amount of affected rows</returns>
        public int ExecuteSpNonQuery(string procedureName, IEnumerable<KeyValuePair<string, object>> parameters = null)
        {
            // returning amount of affected rows
            return (int)this.Execute<object>(new StoredProcedure
            {
                Name = procedureName,
                StoredProcedureReturnData = StoredProcedureReturnData.Nothing,
                Parameters = parameters
            });
        }

        /// <summary>
        /// Executes the given stored procedure.
        /// </summary>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="storedProcedure">Stored procedure</param>
        /// <returns>Result of stored procedure execution</returns>
        private object Execute<TResult>(StoredProcedure storedProcedure) where TResult : class
        {
            // checking argument
            if (string.IsNullOrEmpty(storedProcedure.Name))
            {
                throw new ArgumentException("Procedure name");
            }

            // establishing sql database connection
            using (var sqlConnection = new SqlConnection(this._connString))
            {
                // constructing command
                var sqlCommand = this.ConstructCommand(sqlConnection, storedProcedure);

                // opening connection
                sqlConnection.Open();

                // executing stored procedures depending on their type
                if (storedProcedure.StoredProcedureReturnData == StoredProcedureReturnData.Enumerable)
                {
                    // list of results
                    var list = new List<TResult>();

                    // executing reader and retrieving data
                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(this.RetrieveEnumerableFromReader<TResult>(reader));
                        }
                    }

                    // returning list of results
                    return list;
                }
                else if (storedProcedure.StoredProcedureReturnData == StoredProcedureReturnData.OneRow)
                {
                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        reader.Read();
                        return this.RetrieveEnumerableFromReader<TResult>(reader);
                    }
                }
                else if (storedProcedure.StoredProcedureReturnData == StoredProcedureReturnData.Scalar)
                {
                    // returning scalar result
                    return sqlCommand.ExecuteScalar();
                }
                else
                {
                    // returning amount of affected rows after non-query stored procedure execution
                    sqlCommand.ExecuteNonQuery();

                    return sqlCommand.Parameters["ReturnValue"].Value;
                }
            }
        }

        /// <summary>
        /// Constructs Sql Command
        /// </summary>
        /// <param name="sqlConnection">Sql Connection</param>
        /// <param name="storedProcedure">Stored procedure</param>
        /// <returns>Constructed command</returns>
        private SqlCommand ConstructCommand(SqlConnection sqlConnection, StoredProcedure storedProcedure)
        {
            // constructing command
            var sqlCommand = new SqlCommand
            {
                CommandText = storedProcedure.Name,
                Connection = sqlConnection,
                CommandType = CommandType.StoredProcedure
            };

            // if there are parameters then we need to add them to the command
            if (storedProcedure.Parameters != null)
            {
                foreach (var parameter in storedProcedure.Parameters)
                {                   
                    sqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);
                }
            }

            if (storedProcedure.StoredProcedureReturnData == StoredProcedureReturnData.Nothing)
            {
                var returnParameter = new SqlParameter
                {
                    ParameterName = "ReturnValue",
                    Direction = ParameterDirection.ReturnValue,
                    SqlDbType = SqlDbType.Int
                };

                sqlCommand.Parameters.Add(returnParameter);
            }

            // returning constructed command
            return sqlCommand;
        }

        /// <summary>
        /// Retrieves data from reader
        /// </summary>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="reader">Reader</param>
        /// <returns>Result</returns>
        private TResult RetrieveEnumerableFromReader<TResult>(SqlDataReader reader) where TResult : class
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
            var mapper = this.GetMapper<TResult>(properties);

            // executing mapper
            return mapper(reader);
        }

        /// <summary>
        /// Gets mapper from cached mappers if it exists, otherwise creates the new one.
        /// </summary>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="properties">Properties</param>
        /// <returns>Sql Data reader to object mapper</returns>
        private Func<SqlDataReader, TResult> GetMapper<TResult>(PropertyInfo[] properties)
        {
            // getting result type
            var resultType = typeof(TResult);

            //  checking if the mapper exists in cached mappers
            if (this._cachedMappers.ContainsKey(resultType))
                return (Func<SqlDataReader, TResult>)this._cachedMappers[resultType];

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
            var lambda = Expression.Lambda<Func<SqlDataReader, TResult>>(body, sourceExpr);

            // compiling lambda expression
            var mapper = lambda.Compile();

            // adding compiled mapper to cached mappers
            if(!this._cachedMappers.TryAdd(resultType, mapper))
                throw new Exception("CachedMapper");

            // returning mapper
            return mapper;
        }

        /// <summary>
        /// Gets properties from the cached properties if they exist,otherwise gets with reflection and adds them to cached properties
        /// </summary>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <returns>Properties</returns>
        private PropertyInfo[] GetProperties<TResult>()
        {
            // getting type of properties
            var type = typeof(TResult);

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
    }
}