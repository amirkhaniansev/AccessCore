using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccessCore.SpExecuters
{
    /// <summary>
    /// Class for executing stored procedures in MsSQL DB engine.
    /// </summary>
    public class MsSqlSpExecuter : SpExecuter
    {
        #region constructors

        /// <summary>
        /// Creates new instance of <see cref="MsSqlSpExecuter"/>
        /// </summary>
        /// <param name="userId">user ID</param>
        /// <param name="password">user password</param>
        /// <param name="server">server</param>
        /// <param name="database">database name</param>
        public MsSqlSpExecuter(string userId, string password, string server, string database) 
            : this(MsSqlSpExecuter.ConstructConnectionString(userId, password, server, database))
        {
        }

        /// <summary>
        /// Creates new instance of <see cref="MsSqlSpExecuter"/>
        /// </summary>
        /// <param name="connString">connection string</param>
        public MsSqlSpExecuter(string connString) : base(connString)
        {
        }

        #endregion

        #region protected override methods

        /// <summary>
        /// Executes the stored procedure asynchonously in MsSQL db engine.
        /// </summary>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="storedProcedure">stored procedure</param>
        /// <returns>result</returns>
        protected async override Task<object> ExecuteAsync<TResult>(StoredProcedure storedProcedure)
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
                                list.Add(this.RetrieveEnumerableFromReader<TResult, SqlDataReader>(reader));
                            }
                        }

                        // returning list of results
                        return list;
                    }
                    else if (storedProcedure.StoredProcedureReturnData == StoredProcedureReturnData.OneRow)
                    {
                        using (var reader = await sqlCommand.ExecuteReaderAsync())
                        {
                            await reader.ReadAsync();
                            return this.RetrieveEnumerableFromReader<TResult, SqlDataReader>(reader);
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
        /// Constructs MS SQL command.
        /// </summary>
        /// <param name="sqlConnection">SQL connection</param>
        /// <param name="storedProcedure">stored procedure</param>
        /// <returns>SQL Command</returns>
        private static SqlCommand ConstructCommand(SqlConnection sqlConnection, StoredProcedure storedProcedure)
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
            var builder = new SqlConnectionStringBuilder();
            builder.UserID = userId;
            builder.Password = password;
            builder.DataSource = server;
            builder.InitialCatalog = database;
            return builder.ToString();
        }

        #endregion
    }
}