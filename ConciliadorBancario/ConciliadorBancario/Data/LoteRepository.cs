using System;
using System.Data.SQLite;
using ConciliadorBancario.Models;

namespace ConciliadorBancario.Data
{
    public class LoteRepository
    {
        public int InsertLote(LoteImportacion lote, SQLiteConnection connection, SQLiteTransaction transaction)
        {
            string query = @"
                INSERT INTO LotesImportacion (FechaImportacion, TipoFuente, NombreArchivo, RegistrosImportados)
                VALUES (@Fecha, @TipoFuente, @NombreArchivo, @Registros);
                SELECT last_insert_rowid();";

            using (var command = new SQLiteCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@Fecha", lote.FechaImportacion);
                command.Parameters.AddWithValue("@TipoFuente", lote.TipoFuente);
                command.Parameters.AddWithValue("@NombreArchivo", lote.NombreArchivo);
                command.Parameters.AddWithValue("@Registros", lote.RegistrosImportados);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        public void RevertirLote(int loteId)
        {
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    // Eliminar movimientos de este lote
                    string q1 = "DELETE FROM Movimientos WHERE LoteId = @LoteId";
                    using (var c1 = new SQLiteCommand(q1, connection, transaction))
                    {
                        c1.Parameters.AddWithValue("@LoteId", loteId);
                        c1.ExecuteNonQuery();
                    }

                    // Eliminar el lote
                    string q2 = "DELETE FROM LotesImportacion WHERE Id = @LoteId";
                    using (var c2 = new SQLiteCommand(q2, connection, transaction))
                    {
                        c2.Parameters.AddWithValue("@LoteId", loteId);
                        c2.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }
        }
    }
}
