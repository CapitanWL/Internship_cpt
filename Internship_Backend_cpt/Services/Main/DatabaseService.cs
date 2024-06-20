using Internship_Backend_cpt.Enums;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Internship_Backend_cpt.Services.Main
{
    public class DatabaseService
    {
        public DatabaseService() { }

        public bool TestConnection(string connectionString, ProvidersEnum provider)
        {
            return provider switch
            {
                ProvidersEnum.MsSql => TestSqlConnection(connectionString),
                ProvidersEnum.Postgres => TestPostgreSqlConnection(connectionString),
                _ => false,
            };
        }

        private static bool TestSqlConnection(string connectionString)
        {
            using var connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
            finally { connection.Close(); }
        }

        private static bool TestPostgreSqlConnection(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);
            try
            {
                connection.Open();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
            finally { connection.Close(); }
        }
    }
}
