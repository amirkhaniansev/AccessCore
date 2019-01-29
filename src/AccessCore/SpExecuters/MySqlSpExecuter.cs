using System;
using System.Data;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace AccessCore.SpExecuters
{
    /// <summary>
    /// Class for executing MySql stored procedures
    /// </summary>
    public class MySqlSpExecuter : SpExecuter
    {
        /// <summary>
        /// Creates new instance of <see cref="MySqlSpExecuter"/>
        /// </summary>
        /// <param name="connString">Connection String</param>
        public MySqlSpExecuter(string connString) : base(connString)
        {
        }

        /// <summary>
        /// Executes MySql stored procedure.
        /// </summary>
        /// <typeparam name="TResult">Type of result.</typeparam>
        /// <param name="storedProcedure">Stored procedure.</param>
        /// <returns>result of stored procedure execution.</returns>
        internal override object Execute<TResult>(StoredProcedure storedProcedure)
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
                using (var sqlCommand = this.ConstructCommand(sqlConnection, storedProcedure))
                {
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
                                list.Add(this.RetrieveEnumerableFromReader<TResult, MySqlDataReader>(reader));
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
                            return this.RetrieveEnumerableFromReader<TResult, MySqlDataReader>(reader);
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
        }

        /// <summary>
        /// Constructs Sql Command
        /// </summary>
        /// <param name="sqlConnection">Sql Connection</param>
        /// <param name="storedProcedure">Stored procedure</param>
        /// <returns>Constructed command</returns>
        private MySqlCommand ConstructCommand(MySqlConnection sqlConnection, StoredProcedure storedProcedure)
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
    }
}