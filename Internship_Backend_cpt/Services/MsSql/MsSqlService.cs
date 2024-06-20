
using Internship_Backend_cpt.Models.DbModels;
using Microsoft.Data.SqlClient;

namespace Internship_Backend_cpt.Services.MsSql
{
    public class MsSqlService
    {
        public MsSqlService()
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
        public SchemaModel GetSchemaMsSql(string msSqlConnection)
        {
            var schema = new SchemaModel();

            using (var connection = new SqlConnection(msSqlConnection))
            {
                connection.Open();

                string query = "SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE CATALOG_NAME = @DatabaseName";

                SqlCommand command = new(query, connection);
                command.Parameters.AddWithValue("@DatabaseName", GetDbName(msSqlConnection));

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        schema.SchemaName = reader["SCHEMA_NAME"].ToString();
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
        public IEnumerable<TableModel> GetTables(SqlConnection connection)
        {
            List<TableModel> tables = [];

            string query = "SELECT TABLE_NAME, OBJECT_ID(TABLE_NAME) AS TABLE_ID FROM INFORMATION_SCHEMA.TABLES" +
                " WHERE TABLE_TYPE = 'BASE TABLE' AND\r\nTABLE_NAME != 'sysdiagrams'";

            using (var command = new SqlCommand(query, connection))
            {
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tableName = reader["TABLE_NAME"].ToString();
                            int tableId = Convert.ToInt32(reader["TABLE_ID"]);

                            tables.Add(
                                new
                                TableModel
                                { TableName = tableName,
                                    TableId = tableId });
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
        private static List<ColumnModel> GetColumnsForTable(SqlConnection connection, string tableName)
        {
            List<ColumnModel> columns = [];

            string query = "SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH " +
                           "FROM INFORMATION_SCHEMA.COLUMNS " +
                           "WHERE TABLE_NAME = @tablename";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@tablename", tableName);

                try
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ColumnModel column = new()
                            {
                                ColumnName = reader["COLUMN_NAME"].ToString(),
                                ColumnType = reader["DATA_TYPE"].ToString(),
                                ColumnSize = reader["CHARACTER_MAXIMUM_LENGTH"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CHARACTER_MAXIMUM_LENGTH"])
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

        private List<IndexModel> GetIndexesForTable(SqlConnection connection, string tableName)
        {
            List<IndexModel> indexes = new List<IndexModel>();

            string query = "SELECT index_id, name AS index_name, is_unique, type_desc FROM sys.indexes WHERE object_id = OBJECT_ID(@tableId)";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@tableId", tableName);

                try
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            IndexModel index = new IndexModel
                            {
                                IndexName = reader["index_name"].ToString(),
                                IsClustered = String.Equals(reader["type_desc"].ToString(), "CLUSTERED", StringComparison.OrdinalIgnoreCase),
                                IsUnique = Convert.ToBoolean(reader["is_unique"]),
                            };

                            index.ColumnModels.AddRange(GetColumnsForIndex(connection, index.IndexModelId, tableName));

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

        private List<ColumnModel> GetColumnsForIndex(SqlConnection connection, int indexId, string tablename)
        {
            List<ColumnModel> columns = new();

            string query = "SELECT c.name AS COLUMN_NAME " +
                           "FROM sys.indexes i " +
                           "INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id " +
                           "INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id " +
                           "WHERE i.index_id = @IndexId AND i.object_id = OBJECT_ID(@Tablename)";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@IndexId", indexId);
                command.Parameters.AddWithValue("@Tablename", tablename);

                try
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            columns.Add(new ColumnModel
                            {
                                ColumnName = reader["COLUMN_NAME"].ToString()
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

        public List<ConstraintModel> GetConstraintsForTable(SqlConnection connection, string tablename, List<ColumnModel> columnModels)
        {
            List<ConstraintModel> constraints = new List<ConstraintModel>();

                string query = @"SELECT
    tc.CONSTRAINT_NAME AS ConstraintName,
    tc.CONSTRAINT_TYPE AS ConstraintType,
    ccu.COLUMN_NAME AS ColumnName,
    CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE WHERE CONSTRAINT_NAME = tc.CONSTRAINT_NAME
AND TABLE_NAME = @TableName) THEN 1 ELSE 0 END AS IsUnique,
    CASE WHEN COLUMNPROPERTY(OBJECT_ID(ccu.TABLE_NAME), ccu.COLUMN_NAME, 'AllowsNull') = 0 THEN 0 ELSE 1 END AS IsNullable,
    CASE WHEN tc.CONSTRAINT_TYPE = 'PRIMARY KEY' THEN 1 ELSE 0 END AS IsPrimaryKey,
    CASE WHEN tc.CONSTRAINT_TYPE = 'FOREIGN KEY' THEN 1 ELSE 0 END AS IsForeignKey,
    rc.DELETE_RULE AS OnDeleteInfo,
    rc.UPDATE_RULE AS OnUpdateInfo,
    cc.CHECK_CLAUSE AS CheckClause
FROM
    INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc
INNER JOIN
    INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE AS ccu ON tc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
LEFT JOIN
    INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS rc ON tc.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
LEFT JOIN
    INFORMATION_SCHEMA.CHECK_CONSTRAINTS AS cc ON tc.CONSTRAINT_NAME = cc.CONSTRAINT_NAME
WHERE
    ccu.TABLE_NAME = @TableName";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TableName", tablename);

                    try
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                            while (reader.Read())
                            {
                                string constraintName = reader["ConstraintName"].ToString();
                                string constraintType = reader["ConstraintType"].ToString();
                                bool isUnique = Convert.ToBoolean(reader["IsUnique"]);
                                bool isNullable = Convert.ToBoolean(reader["IsNullable"]);
                                bool isPrimaryKey = Convert.ToBoolean(reader["IsPrimaryKey"]);
                                bool isForeignKey = Convert.ToBoolean(reader["IsForeignKey"]);
                                string onDeleteInfo = reader["OnDeleteInfo"].ToString();
                                string onUpdateInfo = reader["OnUpdateInfo"].ToString();
                                string checkClause = reader["CheckClause"].ToString();

                                ConstraintModel constraint = new ConstraintModel
                                {
                                    ConstraintName = constraintName,
                                    ConstraintType = constraintType,
                                    IsUnique = isUnique,
                                    IsNullable = isNullable,
                                    IsPrimaryKey = isPrimaryKey,
                                    IsForeignKey = isForeignKey,
                                    OnDeleteInfo = onDeleteInfo,
                                    OnUpdateInfo = onUpdateInfo,
                                    CheckedInfo = checkClause,
                                    ColumnModels = new List<ColumnModel>()
                                };

                                foreach (var columnModel in columnModels)
                                {
                                    if (columnModel.ColumnName == reader["ColumnName"].ToString())
                                    {
                                        constraint.ColumnModels.Add(columnModel);
                                    }
                                }

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

        private List<RelationModel> GetRelationshipsForTable(SqlConnection connection, string tablename)
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
WHERE tc.table_name = (SELECT table_name FROM information_schema.tables WHERE table_name = @tableModel)";

            using (var command = new SqlCommand(sql, connection))
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
                                ColumnModelFromName = reader.GetString(2),
                                ColumnModelToName = reader.GetString(3),
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
