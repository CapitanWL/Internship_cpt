using Internship_Backend_cpt.Models.DbModels;
using Npgsql;

namespace Internship_Backend_cpt.Services.Postgres
{
    public class PostgreSqlService
    {
        public PostgreSqlService()
        {
        }

        public string? GetDbName(string connectionString)
        {
            string? databaseName = connectionString.Split(';')
                .Select(part => part.Split('='))
                .Where(pair => pair.Length == 2 && pair[0].Trim().Equals("Database", StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair[1])
                .FirstOrDefault();

            return databaseName;
        }

        public SchemaModel GetSchemaPostgreSql(string connectionString)
        {
            var schema = new SchemaModel();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT schema_name FROM information_schema.schemata WHERE catalog_name = @DatabaseName";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DatabaseName", GetDbName(connectionString));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            schema.SchemaName = reader["schema_name"].ToString();
                        }
                    }
                }

                var tables = GetTables(connection);
                schema.TableModels.AddRange(tables);

                foreach (var table in schema.TableModels)
                {
                    table.ColumnModels.AddRange(GetColumnsForTable(connection, table.TableName));
                    table.IndexModels.AddRange(GetIndexesForTable(connection, table.TableName));

                    table.ConstraintModels.AddRange(GetConstraintsForTable(connection, table.TableName, table.ColumnModels));
                    table.RelationModels.AddRange(GetRelationshipsForTable(connection, table.TableName));
                }
            }

            return schema;
        }

        #region get tables in db schema
        public IEnumerable<TableModel> GetTables(NpgsqlConnection connection)
        {
            List<TableModel> tables = new List<TableModel>();

            string query = "SELECT table_name, table_schema FROM information_schema.tables WHERE table_type = 'BASE TABLE' AND table_schema NOT IN ('pg_catalog', 'information_schema')";

            using (var command = new NpgsqlCommand(query, connection))
            {
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tableName = reader["table_name"].ToString();
                            string tableSchema = reader["table_schema"].ToString();

                            tables.Add(new TableModel
                            {
                                TableName = tableName,
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            return tables;
        }

        #endregion

        #region get columns for table
        private static List<ColumnModel> GetColumnsForTable(NpgsqlConnection connection, string tableName)
        {
            List<ColumnModel> columns = new List<ColumnModel>();

            string query = "SELECT column_name, data_type, character_maximum_length " +
                           "FROM information_schema.columns " +
                           "WHERE table_name = @tablename";

            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@tablename", tableName);

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ColumnModel column = new ColumnModel
                            {
                                ColumnName = reader["column_name"].ToString(),
                                ColumnType = reader["data_type"].ToString(),
                                ColumnSize = reader["character_maximum_length"] == DBNull.Value ? 0 : Convert.ToInt32(reader["character_maximum_length"])
                            };

                            columns.Add(column);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            return columns;
        }

        #endregion

        #region get indexes for table 

        private List<IndexModel> GetIndexesForTable(NpgsqlConnection connection, string tableName)
        {
            List<IndexModel> indexes = new List<IndexModel>();

            string query = "SELECT i.relname AS index_name, pg_get_indexdef(idx.indexrelid) AS index_definition, " +
                "idx.indisunique AS is_unique, idx.indisclustered AS is_clustered " +
                "FROM pg_class t JOIN pg_index idx ON t.oid = idx.indrelid JOIN pg_class i ON i.oid = idx.indexrelid " +
                "WHERE t.relname = @tablename";

            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@tablename", tableName);

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bool isUnique = Convert.ToBoolean(reader["is_unique"]);
                            bool isClustered = Convert.ToBoolean(reader["is_clustered"]);

                            IndexModel index = new IndexModel
                            {
                                IndexName = reader["index_name"].ToString(),
                                IsUnique = isUnique,
                                IsClustered = isClustered
                            };

                            index.ColumnModels.AddRange(GetColumnsForIndex(connection, index.IndexName));

                            indexes.Add(index);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            return indexes;
        }


        #endregion

        #region get columns for index

        private List<ColumnModel> GetColumnsForIndex(NpgsqlConnection connection, string indexName)
        {
            List<ColumnModel> columns = new List<ColumnModel>();

            string query = "SELECT a.attname AS column_name" +
                " FROM pg_catalog.pg_index i " +
                "JOIN pg_catalog.pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey)" +
                " WHERE pg_get_indexdef(i.indexrelid) ~*@IndexName; ";

            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@IndexName", indexName);

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            columns.Add(new ColumnModel
                            {
                                ColumnName = reader["column_name"].ToString()
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            return columns;
        }

        #endregion

        #region get constraints for table 

        public List<ConstraintModel> GetConstraintsForTable(NpgsqlConnection connection, string tablename, List<ColumnModel> columnModels)
        {
            List<ConstraintModel> constraints = new List<ConstraintModel>();

            string query = @"SELECT
    tc.constraint_name AS ConstraintName,
    tc.constraint_type AS ConstraintType,
    ccu.column_name AS ColumnName,
    CASE WHEN EXISTS (SELECT 1 FROM information_schema.constraint_column_usage WHERE constraint_name = tc.constraint_name AND table_name = @TableName) THEN 1 ELSE 0 END AS IsUnique,
    CASE WHEN col.is_nullable = 'YES' THEN 1 ELSE 0 END AS IsNullable,
    CASE WHEN tc.constraint_type = 'PRIMARY KEY' THEN 1 ELSE 0 END AS IsPrimaryKey,
    CASE WHEN tc.constraint_type = 'FOREIGN KEY' THEN 1 ELSE 0 END AS IsForeignKey,
    rc.delete_rule AS OnDeleteInfo,
    rc.update_rule AS OnUpdateInfo,
    cc.check_clause AS CheckClause
FROM information_schema.table_constraints AS tc
INNER JOIN information_schema.constraint_column_usage AS ccu ON tc.constraint_name = ccu.constraint_name
LEFT JOIN information_schema.referential_constraints AS rc ON tc.constraint_name = rc.constraint_name
LEFT JOIN information_schema.check_constraints AS cc ON tc.constraint_name = cc.constraint_name
LEFT JOIN information_schema.columns col ON col.table_name = ccu.table_name AND col.column_name = ccu.column_name
WHERE ccu.table_name = @TableName;
";

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@TableName", tablename);

                try
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string columnName = reader["ColumnName"].ToString();
                            ColumnModel columnModel = columnModels.FirstOrDefault(cm => cm.ColumnName == columnName);
                            if (columnModel != null)
                            {
                                ConstraintModel constraint = new ConstraintModel
                                {
                                    ConstraintName = reader["ConstraintName"].ToString(),
                                    ConstraintType = reader["ConstraintType"].ToString(),
                                    IsUnique = Convert.ToBoolean(reader["IsUnique"]),
                                    IsNullable = Convert.ToBoolean(reader["IsNullable"]),
                                    IsPrimaryKey = Convert.ToBoolean(reader["IsPrimaryKey"]),
                                    IsForeignKey = Convert.ToBoolean(reader["IsForeignKey"]),
                                    OnDeleteInfo = reader["OnDeleteInfo"].ToString(),
                                    OnUpdateInfo = reader["OnUpdateInfo"].ToString(),
                                    CheckedInfo = reader["CheckClause"].ToString(),
                                    ColumnModels = new List<ColumnModel> { columnModel }
                                };

                                constraints.Add(constraint);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            return constraints;
        }

        #endregion

        #region get relations for table

        private List<RelationModel> GetRelationshipsForTable(NpgsqlConnection connection, string tablename)
        {
            List<RelationModel> relations = new();

            string sql = @"
SELECT 
    tc.constraint_name AS RelationModelName,
    tc.constraint_type AS RelationModelType,
    tc.table_name AS TableModelFromName,
    kcu.column_name AS ColumnModelFromName,
    ccu.table_name AS TableModelToName,
    kcu2.column_name AS ColumnModelToName
FROM information_schema.table_constraints tc
JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage ccu ON tc.constraint_name = ccu.constraint_name
JOIN information_schema.key_column_usage kcu2 ON tc.constraint_name = kcu2.constraint_name
WHERE tc.table_name = @tableModel";

            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@tableModel", tablename);

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var relation = new RelationModel
                            {
                                RelationModelName = reader.GetString(0),
                                RelationModelType = reader.GetString(1),
                                TableModelFromName = reader.GetString(2),
                                ColumnModelFromName = reader.GetString(3),
                                TableModelToName = reader.GetString(4),
                                ColumnModelToName = reader.GetString(5)
                            };

                            relations.Add(relation);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            return relations;
        }

        #endregion
    }
}
