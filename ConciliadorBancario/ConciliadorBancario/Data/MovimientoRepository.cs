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
                INSERT INTO Movimientos (LoteId, TipoFuente, Fecha, Monto, Concepto, Referencia_CodOperacion, CodOperacionSistema, MatchGrupoId, Estado, Observaciones, Banco, Activo)
                VALUES (@LoteId, @TipoFuente, @Fecha, @Monto, @Concepto, @Referencia_CodOperacion, @CodOperacionSistema, @MatchGrupoId, @Estado, @Observaciones, @Banco, 1)";

            using (var command = new SQLiteCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@LoteId", mov.LoteId);
                command.Parameters.AddWithValue("@TipoFuente", mov.TipoFuente);
                command.Parameters.AddWithValue("@Fecha", mov.Fecha);
                command.Parameters.AddWithValue("@Monto", mov.Monto);
                command.Parameters.AddWithValue("@Concepto", mov.Concepto ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Referencia_CodOperacion", mov.Referencia_CodOperacion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CodOperacionSistema", mov.CodOperacionSistema ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MatchGrupoId", mov.MatchGrupoId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Estado", mov.Estado);
                command.Parameters.AddWithValue("@Observaciones", mov.Observaciones ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Banco", mov.Banco ?? (object)DBNull.Value);
                command.ExecuteNonQuery();
            }
        }

        public void UpdateEstado(int id, string nuevoEstado, string observaciones, int? matchId)
        {
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // To properly clear groups when unmatched via UI
                        string fetchGroupQuery = "SELECT MatchGrupoId FROM Movimientos WHERE Id = @Id";
                        string matchGrupoId = null;
                        using(var fetchCmd = new SQLiteCommand(fetchGroupQuery, connection, transaction))
                        {
                            fetchCmd.Parameters.AddWithValue("@Id", id);
                            var res = fetchCmd.ExecuteScalar();
                            if (res != DBNull.Value && res != null) matchGrupoId = res.ToString();
                        }

                        string query = "UPDATE Movimientos SET Estado = @Estado, Observaciones = @Obs, MatchId = @MatchId WHERE Id = @Id";
                        using (var command = new SQLiteCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Estado", nuevoEstado);
                            command.Parameters.AddWithValue("@Obs", observaciones ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@MatchId", matchId ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@Id", id);
                            command.ExecuteNonQuery();
                        }

                        if (nuevoEstado != EstadosMovimiento.Conciliado)
                        {
                            if (matchId.HasValue)
                            {
                                string revQuery = "UPDATE Movimientos SET Estado = @Estado, MatchId = NULL, Observaciones = 'Desvinculado manualmente' WHERE Id = @MId";
                                using (var revCommand = new SQLiteCommand(revQuery, connection, transaction))
                                {
                                    revCommand.Parameters.AddWithValue("@Estado", EstadosMovimiento.NoEncontrado);
                                    revCommand.Parameters.AddWithValue("@MId", matchId.Value);
                                    revCommand.ExecuteNonQuery();
                                }
                            }
                            else if (!string.IsNullOrEmpty(matchGrupoId))
                            {
                                // If it was a group match, release ALL items in that group on both sides
                                string revQueryGroup = "UPDATE Movimientos SET Estado = @Estado, MatchId = NULL, MatchGrupoId = NULL, Observaciones = 'Grupo desvinculado manualmente' WHERE MatchGrupoId = @MGId";
                                using (var revCommand = new SQLiteCommand(revQueryGroup, connection, transaction))
                                {
                                    revCommand.Parameters.AddWithValue("@Estado", EstadosMovimiento.NoEncontrado);
                                    revCommand.Parameters.AddWithValue("@MGId", matchGrupoId);
                                    revCommand.ExecuteNonQuery();
                                }
                            }

                            // Also clear own match fields just in case
                            using (var clearCmd = new SQLiteCommand("UPDATE Movimientos SET MatchId = NULL, MatchGrupoId = NULL WHERE Id = @Id", connection, transaction))
                            {
                                clearCmd.Parameters.AddWithValue("@Id", id);
                                clearCmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void UpdateEstadoMulti(List<int> ids, string nuevoEstado, string observaciones, string matchGrupoId)
        {
            if (ids.Count == 0) return;
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach(var id in ids)
                        {
                            string query = "UPDATE Movimientos SET Estado = @Estado, Observaciones = @Obs, MatchGrupoId = @MatchGrupoId WHERE Id = @Id";
                            using (var command = new SQLiteCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Estado", nuevoEstado);
                                command.Parameters.AddWithValue("@Obs", observaciones ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@MatchGrupoId", matchGrupoId);
                                command.Parameters.AddWithValue("@Id", id);
                                command.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void DeleteMovimientoAndUnmatch(int id)
        {
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int? matchId = null;
                        string matchGrupoId = null;
                        using (var cmd = new SQLiteCommand("SELECT MatchId, MatchGrupoId FROM Movimientos WHERE Id = @Id", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Id", id);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    if (reader["MatchId"] != DBNull.Value) matchId = Convert.ToInt32(reader["MatchId"]);
                                    if (reader["MatchGrupoId"] != DBNull.Value) matchGrupoId = reader["MatchGrupoId"].ToString();
                                }
                            }
                        }

                        using (var cmd = new SQLiteCommand("UPDATE Movimientos SET Activo = 0 WHERE Id = @Id", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Id", id);
                            cmd.ExecuteNonQuery();
                        }

                        if (matchId.HasValue)
                        {
                            using (var cmd = new SQLiteCommand("UPDATE Movimientos SET Estado = @NoEnc, MatchId = NULL, Observaciones = 'Desvinculado por eliminación' WHERE Id = @MId", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@NoEnc", EstadosMovimiento.NoEncontrado);
                                cmd.Parameters.AddWithValue("@MId", matchId.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else if (!string.IsNullOrEmpty(matchGrupoId))
                        {
                             using (var cmd = new SQLiteCommand("UPDATE Movimientos SET Estado = @NoEnc, MatchId = NULL, MatchGrupoId = NULL, Observaciones = 'Desvinculado por eliminación' WHERE MatchGrupoId = @MGId", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@NoEnc", EstadosMovimiento.NoEncontrado);
                                cmd.Parameters.AddWithValue("@MGId", matchGrupoId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void DesactivarMovimientosSistemaPorFecha(DateTime fechaInicio, DateTime? fechaFin, string banco, SQLiteConnection connection, SQLiteTransaction transaction)
        {
            var idsToUnmatch = new List<int>();
            var groupIdsToUnmatch = new List<string>();
            string selectQuery = "SELECT MatchId, MatchGrupoId FROM Movimientos WHERE TipoFuente = 'Sistema' AND Banco = @Banco AND Fecha >= @Inicio AND (@Fin IS NULL OR Fecha <= @Fin) AND (MatchId IS NOT NULL OR MatchGrupoId IS NOT NULL)";
            using (var cmd = new SQLiteCommand(selectQuery, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@Inicio", fechaInicio);
                cmd.Parameters.AddWithValue("@Fin", fechaFin ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Banco", banco);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["MatchId"] != DBNull.Value) idsToUnmatch.Add(Convert.ToInt32(reader["MatchId"]));
                        if (reader["MatchGrupoId"] != DBNull.Value) groupIdsToUnmatch.Add(reader["MatchGrupoId"].ToString());
                    }
                }
            }

            foreach (var matchId in idsToUnmatch)
            {
                string revertQuery = "UPDATE Movimientos SET Estado = @Estado, MatchId = NULL, Observaciones = 'Desvinculado por recarga de sistema' WHERE Id = @Id";
                using (var revCmd = new SQLiteCommand(revertQuery, connection, transaction))
                {
                    revCmd.Parameters.AddWithValue("@Estado", EstadosMovimiento.NoEncontrado);
                    revCmd.Parameters.AddWithValue("@Id", matchId);
                    revCmd.ExecuteNonQuery();
                }
            }

            foreach (var mgId in groupIdsToUnmatch)
            {
                string revertQuery = "UPDATE Movimientos SET Estado = @Estado, MatchId = NULL, MatchGrupoId = NULL, Observaciones = 'Desvinculado por recarga de sistema' WHERE MatchGrupoId = @Id";
                using (var revCmd = new SQLiteCommand(revertQuery, connection, transaction))
                {
                    revCmd.Parameters.AddWithValue("@Estado", EstadosMovimiento.NoEncontrado);
                    revCmd.Parameters.AddWithValue("@Id", mgId);
                    revCmd.ExecuteNonQuery();
                }
            }

            string updateQuery = "UPDATE Movimientos SET Activo = 0 WHERE TipoFuente = 'Sistema' AND Banco = @Banco AND Fecha >= @Inicio AND (@Fin IS NULL OR Fecha <= @Fin)";
            using (var command = new SQLiteCommand(updateQuery, connection, transaction))
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
                MatchGrupoId = reader["MatchGrupoId"] == DBNull.Value ? null : reader["MatchGrupoId"].ToString(),
                Banco = reader["Banco"] == DBNull.Value ? null : reader["Banco"].ToString(),
                Activo = Convert.ToInt32(reader["Activo"]) == 1
            };
        }
    }
}
