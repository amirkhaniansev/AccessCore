using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace AccessCore.SpExecuters
{
    /// <summary>
    /// Class for executing stored procedures in MS SQL Server
    /// </summary>
    public class MsSqlSpExecuter : SpExecuter
    {
        /// <summary>
        /// Creates new instance of <see cref="MsSqlSpExecuter"/>
        /// </summary>
        /// <param name="connString">Connection string</param>
        public MsSqlSpExecuter(string connString) : base(connString)
        {
        }

        /// <summary>
        /// Executes MS SQL Server stored procedure.
        /// </summary>
        /// <typeparam name="TResult">Type of result.</typeparam>
        /// <param name="storedProcedure">Stored procedure.</param>
        /// <returns>result</returns>
        internal override object Execute<TResult>(StoredProcedure storedProcedure)
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
                                list.Add(this.RetrieveEnumerableFromReader<TResult, SqlDataReader>(reader));
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
                            return this.RetrieveEnumerableFromReader<TResult, SqlDataReader>(reader);
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
    }
}