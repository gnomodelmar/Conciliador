using System;
using System.Collections.Generic;
using System.Data.SQLite;
using ConciliadorBancario.Models;

namespace ConciliadorBancario.Data
{
    public class MovimientoRepository
    {
        public List<Movimiento> GetMovimientosActivos(string tipoFuente)
        {
            var movimientos = new List<Movimiento>();
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Movimientos WHERE TipoFuente = @TipoFuente AND Activo = 1";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TipoFuente", tipoFuente);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            movimientos.Add(MapMovimiento(reader));
                        }
                    }
                }
            }
            return movimientos;
        }

        public List<Movimiento> GetPendientes(string tipoFuente)
        {
            var movimientos = new List<Movimiento>();
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Movimientos WHERE TipoFuente = @TipoFuente AND Activo = 1 AND Estado != @Conciliado";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TipoFuente", tipoFuente);
                    command.Parameters.AddWithValue("@Conciliado", EstadosMovimiento.Conciliado);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            movimientos.Add(MapMovimiento(reader));
                        }
                    }
                }
            }
            return movimientos;
        }

        public void InsertMovimiento(Movimiento mov, SQLiteConnection connection, SQLiteTransaction transaction)
        {
            string query = @"
                INSERT INTO Movimientos (LoteId, TipoFuente, Fecha, Monto, Concepto, Referencia_CodOperacion, CodOperacionSistema, Estado, Observaciones, Banco, Activo)
                VALUES (@LoteId, @TipoFuente, @Fecha, @Monto, @Concepto, @Referencia_CodOperacion, @CodOperacionSistema, @Estado, @Observaciones, @Banco, 1)";

            using (var command = new SQLiteCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@LoteId", mov.LoteId);
                command.Parameters.AddWithValue("@TipoFuente", mov.TipoFuente);
                command.Parameters.AddWithValue("@Fecha", mov.Fecha);
                command.Parameters.AddWithValue("@Monto", mov.Monto);
                command.Parameters.AddWithValue("@Concepto", mov.Concepto ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Referencia_CodOperacion", mov.Referencia_CodOperacion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CodOperacionSistema", mov.CodOperacionSistema ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Estado", mov.Estado);
                command.Parameters.AddWithValue("@Observaciones", mov.Observaciones ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Banco", mov.Banco ?? (object)DBNull.Value);
                command.ExecuteNonQuery();
            }
        }

        public void UpdateEstado(int id, string nuevoEstado, string observaciones, int? matchId = null)
        {
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                string query = "UPDATE Movimientos SET Estado = @Estado, Observaciones = @Obs, MatchId = @MatchId WHERE Id = @Id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Estado", nuevoEstado);
                    command.Parameters.AddWithValue("@Obs", observaciones ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@MatchId", matchId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteMovimiento(int id)
        {
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                string query = "UPDATE Movimientos SET Activo = 0 WHERE Id = @Id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DesactivarMovimientosSistemaPorFecha(DateTime fechaInicio, DateTime? fechaFin, string banco, SQLiteConnection connection, SQLiteTransaction transaction)
        {
            string query = "UPDATE Movimientos SET Activo = 0 WHERE TipoFuente = 'Sistema' AND Banco = @Banco AND Fecha >= @Inicio AND (@Fin IS NULL OR Fecha <= @Fin)";
            using (var command = new SQLiteCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@Inicio", fechaInicio);
                command.Parameters.AddWithValue("@Fin", fechaFin ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Banco", banco);
                command.ExecuteNonQuery();
            }
        }

        public List<Movimiento> BuscarSimilares(string tipoFuente, DateTime fecha, double monto, string concepto)
        {
            var movimientos = new List<Movimiento>();
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                string query = @"
                    SELECT * FROM Movimientos
                    WHERE TipoFuente = @TipoFuente
                    AND Activo = 1
                    AND Monto = @Monto
                    AND (julianday(Fecha) - julianday(@Fecha)) BETWEEN -1 AND 1";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TipoFuente", tipoFuente);
                    command.Parameters.AddWithValue("@Monto", monto);
                    command.Parameters.AddWithValue("@Fecha", fecha.ToString("yyyy-MM-dd HH:mm:ss"));
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            movimientos.Add(MapMovimiento(reader));
                        }
                    }
                }
            }
            return movimientos;
        }

        private Movimiento MapMovimiento(SQLiteDataReader reader)
        {
            return new Movimiento
            {
                Id = Convert.ToInt32(reader["Id"]),
                LoteId = Convert.ToInt32(reader["LoteId"]),
                TipoFuente = reader["TipoFuente"].ToString(),
                Fecha = Convert.ToDateTime(reader["Fecha"]),
                Monto = Convert.ToDouble(reader["Monto"]),
                Concepto = reader["Concepto"] == DBNull.Value ? null : reader["Concepto"].ToString(),
                Referencia_CodOperacion = reader["Referencia_CodOperacion"] == DBNull.Value ? null : reader["Referencia_CodOperacion"].ToString(),
                CodOperacionSistema = reader["CodOperacionSistema"] == DBNull.Value ? null : reader["CodOperacionSistema"].ToString(),
                Estado = reader["Estado"].ToString(),
                Observaciones = reader["Observaciones"] == DBNull.Value ? null : reader["Observaciones"].ToString(),
                MatchId = reader["MatchId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["MatchId"]),
                Banco = reader["Banco"] == DBNull.Value ? null : reader["Banco"].ToString(),
                Activo = Convert.ToInt32(reader["Activo"]) == 1
            };
        }
    }
}
