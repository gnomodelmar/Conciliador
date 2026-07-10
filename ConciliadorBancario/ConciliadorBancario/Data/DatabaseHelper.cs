using System;
using System.Data.SQLite;
using System.IO;

namespace ConciliadorBancario.Data
{
    public static class DatabaseHelper
    {
        private static readonly string DbFile = "ConciliadorData.sqlite";
        public static string ConnectionString => $"Data Source={DbFile};Version=3;";

        public static void InitializeDatabase()
        {
            if (!File.Exists(DbFile))
            {
                SQLiteConnection.CreateFile(DbFile);
            }

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                ExecuteQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS LotesImportacion (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FechaImportacion DATETIME NOT NULL,
                        TipoFuente TEXT NOT NULL,
                        NombreArchivo TEXT NOT NULL,
                        RegistrosImportados INTEGER NOT NULL
                    );
                ");

                ExecuteQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS Movimientos (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        LoteId INTEGER NOT NULL,
                        TipoFuente TEXT NOT NULL,
                        Fecha DATETIME NOT NULL,
                        Monto REAL NOT NULL,
                        Concepto TEXT,
                        Referencia_CodOperacion TEXT,
                        Estado TEXT NOT NULL,
                        Observaciones TEXT,
                        MatchId INTEGER,
                        Banco TEXT,
                        Activo INTEGER DEFAULT 1,
                        FOREIGN KEY (LoteId) REFERENCES LotesImportacion(Id)
                    );
                ");

                EnsureColumnExists(connection, "Movimientos", "CodOperacionSistema", "TEXT");

                ExecuteQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS ConfiguracionBancos (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        NombreBanco TEXT NOT NULL,
                        ColumnaFecha TEXT,
                        ColumnaMonto TEXT,
                        ColumnaMontoEntrada TEXT,
                        ColumnaMontoSalida TEXT,
                        ColumnaConcepto TEXT,
                        ColumnaReferencia TEXT
                    );
                ");

                EnsureColumnExists(connection, "ConfiguracionBancos", "ColumnaTipo", "TEXT");
                EnsureColumnExists(connection, "ConfiguracionBancos", "ValorTipoSalida", "TEXT");
                EnsureColumnExists(connection, "ConfiguracionBancos", "TipoConfiguracion", "TEXT");
                EnsureColumnExists(connection, "ConfiguracionBancos", "ColumnaCodOperacionSistema", "TEXT");

                ExecuteQuery(connection, "UPDATE ConfiguracionBancos SET TipoConfiguracion = 'Banco' WHERE TipoConfiguracion IS NULL;");

                // SEED the global system configuration based on the user's requirement
                SeedSystemConfiguration(connection);

                ExecuteQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS ReglasAsientos (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        NombreRegla TEXT NOT NULL,
                        PalabraClave TEXT NOT NULL,
                        Banco TEXT NOT NULL
                    );
                ");

                ExecuteQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS ReglasAsientosDetalle (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ReglaId INTEGER NOT NULL,
                        Tipo TEXT NOT NULL,
                        IdCuenta TEXT NOT NULL,
                        ConceptoTemplate TEXT NOT NULL,
                        FOREIGN KEY (ReglaId) REFERENCES ReglasAsientos(Id)
                    );
                ");
            }
        }

        private static void ExecuteQuery(SQLiteConnection connection, string query)
        {
            using (var command = new SQLiteCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private static void EnsureColumnExists(SQLiteConnection connection, string tableName, string columnName, string columnDef)
        {
            bool exists = false;
            using (var cmd = new SQLiteCommand($"PRAGMA table_info({tableName});", connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["name"].ToString().Equals(columnName, StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }
                }
            }

            if (!exists)
            {
                ExecuteQuery(connection, $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDef};");
            }
        }

        private static void SeedSystemConfiguration(SQLiteConnection connection)
        {
            // Check if global system config exists
            bool hasSystem = false;
            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM ConfiguracionBancos WHERE NombreBanco = 'SISTEMA_GENERAL' AND TipoConfiguracion = 'Sistema'", connection))
            {
                hasSystem = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }

            if (!hasSystem)
            {
                // Delete any old system configs to clean up
                ExecuteQuery(connection, "DELETE FROM ConfiguracionBancos WHERE TipoConfiguracion = 'Sistema'");

                // Insert the definitive System config based on the screenshot
                string insert = @"
                    INSERT INTO ConfiguracionBancos
                    (NombreBanco, TipoConfiguracion, ColumnaFecha, ColumnaMonto, ColumnaConcepto, ColumnaReferencia, ColumnaCodOperacionSistema, ColumnaTipo, ValorTipoSalida)
                    VALUES ('SISTEMA_GENERAL', 'Sistema', 'Fecha', 'Importe', 'Concepto', 'codOperacionPago', 'NroOper', 'Tipo', '-');";

                ExecuteQuery(connection, insert);
            }
        }
    }
}
