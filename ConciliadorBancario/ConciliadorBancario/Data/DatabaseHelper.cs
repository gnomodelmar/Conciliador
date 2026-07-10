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

                // LotesImportacion
                ExecuteQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS LotesImportacion (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FechaImportacion DATETIME NOT NULL,
                        TipoFuente TEXT NOT NULL, -- 'Banco' o 'Sistema'
                        NombreArchivo TEXT NOT NULL,
                        RegistrosImportados INTEGER NOT NULL
                    );
                ");

                // Movimientos
                ExecuteQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS Movimientos (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        LoteId INTEGER NOT NULL,
                        TipoFuente TEXT NOT NULL, -- 'Banco' o 'Sistema'
                        Fecha DATETIME NOT NULL,
                        Monto REAL NOT NULL,
                        Concepto TEXT,
                        Referencia_CodOperacion TEXT,
                        Estado TEXT NOT NULL, -- 'No encontrado', 'Pendiente de corregir en sistema', 'Pendiente de pasar', 'Pend. Asiento Masivo', 'Conciliado'
                        Observaciones TEXT,
                        MatchId INTEGER, -- ID del movimiento con el que se concilió (en la tabla opuesta)
                        Banco TEXT, -- Para identificar de qué banco vino, si es de banco
                        Activo INTEGER DEFAULT 1, -- Para marcado lógico de eliminados
                        FOREIGN KEY (LoteId) REFERENCES LotesImportacion(Id)
                    );
                ");

                // ConfiguracionBancos
                ExecuteQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS ConfiguracionBancos (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        NombreBanco TEXT NOT NULL UNIQUE,
                        ColumnaFecha TEXT,
                        ColumnaMonto TEXT,
                        ColumnaMontoEntrada TEXT, -- Si el banco usa columnas separadas
                        ColumnaMontoSalida TEXT,  -- Si el banco usa columnas separadas
                        ColumnaConcepto TEXT,
                        ColumnaReferencia TEXT
                    );
                ");

                // ReglasAsientosMasivos
                ExecuteQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS ReglasAsientos (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        NombreRegla TEXT NOT NULL,
                        PalabraClave TEXT NOT NULL,
                        Banco TEXT NOT NULL
                    );
                ");

                // ReglasAsientosDetalle
                ExecuteQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS ReglasAsientosDetalle (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ReglaId INTEGER NOT NULL,
                        Tipo TEXT NOT NULL, -- 'Entrada' o 'Salida'
                        IdCuenta TEXT NOT NULL,
                        ConceptoTemplate TEXT NOT NULL, -- Ej: 'Galicia Impuesto a los Debitos {MM/YYYY}'
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
    }
}
