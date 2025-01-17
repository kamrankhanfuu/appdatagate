﻿// -*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-
// Utility class for managing sql
// queries, connections, commands

// Created: 04/2020
// Author:  Philip Shishov

// -*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-
namespace DataGate.Services.SqlClient
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Threading.Tasks;

    using DataGate.Common;
    using DataGate.Common.Exceptions;
    using DataGate.Services.SqlClient.Contracts;
    using DataGate.Web.Infrastructure.Extensions;
    using Microsoft.Extensions.Configuration;

    // _____________________________________________________________
    public class SqlQueryManager : ISqlQueryManager
    {
        private readonly IConfiguration configuration;

        // ________________________________________________________
        //
        // Constructor: initialize with DI IConfiguration
        // to retrieve appsettings.json connection string
        public SqlQueryManager(IConfiguration config)
        {
            this.configuration = config;
        }

        // ________________________________________________________
        //
        // Execute parameterized stored procedure
        public async Task ExecuteProcedure(SqlCommand command)
        {
            using (SqlConnection connection = new SqlConnection())
            {
                connection.ConnectionString = this.configuration.GetConnectionString(GlobalConstants.DataGatevFinaleConnection);
                try
                {
                    if (connection.State == ConnectionState.Closed)
                    {
                        await connection.OpenAsync();
                    }

                    command.Connection = connection;
                    using (command)
                    {
                        this.SetParametersForDB(command);
                        await command.ExecuteScalarAsync();
                    }
                }
                catch (SqlException exception)
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        await connection.CloseAsync();
                    }

                    throw new CustomSqlException(exception.Message, exception);
                }
            }
        }

        public async IAsyncEnumerable<string[]> ExecuteQueryAsync(string function, DateTime? date, int? id, IEnumerable<string> columns)
        {
            using (SqlConnection connection = new SqlConnection())
            {
                connection.ConnectionString = this.configuration.GetConnectionString(GlobalConstants.DataGatevFinaleConnection);
                await connection.OpenAsync();
                SqlCommand command = connection.CreateCommand();

                var sqlDate = DateTimeParser.ToSqlFormat(date);

                if (!date.HasValue)
                {
                    command.CommandText = $"select * from {function}({id})";
                }
                else if (id.HasValue && id != 0)
                {
                    if (columns != null)
                    {
                        command.CommandText = $"select {string.Join(", ", columns)} from {function}('{sqlDate}', {id})";
                    }
                    else
                    {
                        command.CommandText = $"select * from {function}('{sqlDate}', {id})";
                    }
                }
                else
                {
                    if (columns != null)
                    {
                        command.CommandText = $"select {string.Join(", ", columns)} from {function}('{sqlDate}')";
                    }
                    else
                    {
                        command.CommandText = $"select * from {function}('{sqlDate}')";
                    }
                }

                await foreach (var item in DataSqlHelper.GetStringDataAsync(command))
                {
                    yield return item;
                }
            }
        }

        public async IAsyncEnumerable<string[]> ExecuteQueryTimeSeriesAsync(string function)
        {
            using (SqlConnection connection = new SqlConnection())
            {
                connection.ConnectionString = this.configuration.GetConnectionString(GlobalConstants.DataGatevFinaleConnection);
                await connection.OpenAsync();
                SqlCommand command = connection.CreateCommand();

                command.CommandText = function;

                await foreach (var item in DataSqlHelper.GetStringDataAsync(command))
                {
                    yield return item;
                }
            }
        }

        public async IAsyncEnumerable<string[]> ExecuteQueryReportsAsync(string function, DateTime date)
        {
            using (SqlConnection connection = new SqlConnection())
            {
                SqlCommand command = this.SetUpSqlConnectionCommand(connection);

                command.CommandText = function;

                if (function.Contains("fn"))
                {
                    var sqlDate = DateTimeParser.ToSqlFormat(date);
                    command.CommandText = $"select * from {function}('{sqlDate}')";
                }
                else
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@datereport", SqlDbType.Date) { Value = date });
                }

                await foreach (var item in DataSqlHelper.GetStringDataAsync(command))
                {
                    yield return item;
                }
            }
        }

        // ________________________________________________________
        //
        // Convert rows values from a data reader into typed results
        // using IDataReaderParser interface
        public IEnumerable<T> ExecuteQueryMapping<T>(string function, int? id, DateTime? date)
            where T : IDataReaderParser, new()
        {
            using (SqlConnection connection = new SqlConnection())
            {
                SqlCommand command = this.SetUpSqlConnectionCommand(connection);
                var sqlDate = DateTimeParser.ToSqlFormat(date);

                if (id.HasValue)
                {
                    if (date.HasValue)
                    {
                        command.CommandText = $"select * from {function}('{sqlDate}', {id})";
                    }
                    else
                    {
                        command.CommandText = $"select * from {function}({id})";
                    }
                }
                else
                {
                    command.CommandText = $"select * from {function}('{sqlDate}')";
                }

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var entity = new T();
                        entity.Parse(reader);
                        yield return entity;
                    }
                }
            }
        }

        private SqlCommand SetUpSqlConnectionCommand(SqlConnection connection)
        {
            connection.ConnectionString = this.configuration.GetConnectionString(GlobalConstants.DataGatevFinaleConnection);
            connection.Open();
            SqlCommand command = connection.CreateCommand();
            return command;
        }

        private void SetParametersForDB(SqlCommand command)
        {
            var typeInt = DbType.Int32;
            var typeString = DbType.String;
            var typeStringFixed = DbType.StringFixedLength;

            foreach (SqlParameter parameter in command.Parameters)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = DBNull.Value;
                    continue;
                }

                try
                {
                    if (parameter.DbType == typeInt)
                    {
                        if ((int?)parameter.Value <= 0)
                        {
                            parameter.Value = DBNull.Value;
                            continue;
                        }
                    }

                    if (parameter.DbType == typeString || parameter.DbType == typeStringFixed)
                    {
                        if (string.IsNullOrEmpty((string)parameter.Value))
                        {
                            parameter.Value = DBNull.Value;
                            continue;
                        }
                    }
                }
                catch (SqlException exception)
                {
                    throw new CustomSqlException(exception.Message, exception);
                }
            }
        }
    }
}
