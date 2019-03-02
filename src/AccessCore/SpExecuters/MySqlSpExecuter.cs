using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Common;
using MySql.Data.MySqlClient;

namespace AccessCore.SpExecuters
{
    /// <summary>
    /// Class for executing stored procedures in MySQL db engine.
    /// </summary>
    public class MySqlSpExecuter : SpExecuter
    {
        #region constructors

        /// <summary>
        /// Creates new instance of <see cref="MySqlSpExecuter"/>
        /// </summary>
        /// <param name="userId">user ID</param>
        /// <param name="password">user password</param>
        /// <param name="server">server</param>
        /// <param name="database">database name</param>
        public MySqlSpExecuter(string userId, string password, string server, string database) :
            this(MySqlSpExecuter.ConstructConnectionString(userId, password, server, database))
        {
        }

        /// <summary>
        /// Creates new instance of <see cref="MySqlSpExecuter"/>
        /// </summary>
        /// <param name="connString">connection string</param>
        public MySqlSpExecuter(string connString) : base(connString)
        {
        }

        #endregion

        #region protected override methods

        /// <summary>
        /// Executes the stored procedure asynchonously in MySQL db engine.
        /// </summary>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="storedProcedure">stored procedure</param>
        /// <returns>result</returns>
        protected override async Task<object> ExecuteAsync<TResult>(StoredProcedure storedProcedure)
        {
            // checking argument
            if (string.IsNullOrEmpty(storedProcedure.Name))
            {
                throw new ArgumentException("Procedure name");
            }

            // establishing sql database connection
            using (var sqlConnection = new MySqlConnection(this._connString))
            {
                // constructing command
                using (var sqlCommand = ConstructCommand(sqlConnection, storedProcedure))
                {
                    // opening connection
                    await sqlConnection.OpenAsync();

                    // executing stored procedures depending on their type
                    if (storedProcedure.StoredProcedureReturnData == StoredProcedureReturnData.Enumerable)
                    {
                        // list of results
                        var list = new List<TResult>();

                        // executing reader and retrieving data
                        using (var reader = await sqlCommand.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(this.RetrieveEnumerableFromReader<TResult, DbDataReader>(reader));
                            }
                        }

                        // returning list of results
                        return list;
                    }
                    else if (storedProcedure.StoredProcedureReturnData == StoredProcedureReturnData.OneRow)
                    {
                        using (var reader = await sqlCommand.ExecuteReaderAsync())
                        {
                            reader.Read();
                            return this.RetrieveEnumerableFromReader<TResult, DbDataReader>(reader);
                        }
                    }
                    else if (storedProcedure.StoredProcedureReturnData == StoredProcedureReturnData.Scalar)
                    {
                        // returning scalar result
                        return await sqlCommand.ExecuteScalarAsync();
                    }
                    else
                    {
                        // returning amount of affected rows after non-query stored procedure execution
                        await sqlCommand.ExecuteNonQueryAsync();

                        return sqlCommand.Parameters["ReturnValue"].Value;
                    }
                }
            }
        }

        #endregion

        #region private helper methods

        /// <summary>
        /// Constructs My SQL command.
        /// </summary>
        /// <param name="sqlConnection">SQL connection</param>
        /// <param name="storedProcedure">stored procedure</param>
        /// <returns>MySQL Command</returns>
        private static MySqlCommand ConstructCommand(MySqlConnection sqlConnection, StoredProcedure storedProcedure)
        {
            // constructing command
            var sqlCommand = new MySqlCommand
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
                var returnParameter = new MySqlParameter
                {
                    ParameterName = "ReturnValue",
                    Direction = ParameterDirection.ReturnValue,
                    MySqlDbType = MySqlDbType.Int32
                };

                sqlCommand.Parameters.Add(returnParameter);
            }

            // returning constructed command
            return sqlCommand;
        }

        /// <summary>
        /// Constructs connection string.
        /// </summary>
        /// <param name="userId">user ID</param>
        /// <param name="password">user password</param>
        /// <param name="server">server</param>
        /// <param name="database">database name</param>
        /// <returns>connection string</returns>
        private static string ConstructConnectionString(
            string userId, string password, string server, string database)
        {
            var builder = new MySqlConnectionStringBuilder();
            builder.UserID = userId;
            builder.Password = password;
            builder.Server = server;
            builder.Database = database;
            return builder.ToString();
        }

        #endregion
    }
}