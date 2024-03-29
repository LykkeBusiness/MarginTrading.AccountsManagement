// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Dal.Common
{
    public static class SqlExtensions
    {
        private const char WildcardCharacterAnyString = '%';
        public static void InitializeSqlObject(this string connectionString, string scriptFileName, ILogger log = null)
        {
            var creationScript = FileExtensions.ReadFromFile(scriptFileName);

            using var conn = new SqlConnection(connectionString);
            try
            {
                conn.Execute(creationScript);
            }
            catch (Exception ex)
            {
                log?.LogError(ex, "Failed to initialize SQL object, script file name [{ScriptFileName}]",
                    scriptFileName);
                throw;
            }
        }

        /// <summary>
        /// Adds '%' wildcard to the beginning and end of the source string if it is not there yet.
        /// Supposed to be used for supporting Transact-SQL LIKE matching.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string AddLikeWildcards(this string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return source;
            
            return source
                .AddFirstSymbolIfNotExists(WildcardCharacterAnyString)
                .AddLastSymbolIfNotExists(WildcardCharacterAnyString);
        }

        /// <summary>
        /// Checks if string is empty or null and return corresponding NULL value for SQL provider
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static object AsSqlParameterValue(this string source) =>
            string.IsNullOrWhiteSpace(source) ? (object)DBNull.Value : source;
    }
}