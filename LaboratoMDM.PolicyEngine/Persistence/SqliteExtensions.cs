using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Persistence
{
    public static class SqliteExtensions
    {
        public static SqliteCommand CreateCommand(
            this SqliteConnection conn,
            string sql,
            SqliteTransaction? tx = null)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            if (tx != null)
                cmd.Transaction = tx;
            return cmd;
        }

        public static void AddParam(
            this SqliteCommand cmd,
            string name,
            object? value)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
    }
}
