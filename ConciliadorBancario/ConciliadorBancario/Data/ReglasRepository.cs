using System;
using System.Collections.Generic;
using System.Data.SQLite;
using ConciliadorBancario.Models;

namespace ConciliadorBancario.Data
{
    public class ReglasRepository
    {
        public List<ReglaAsiento> GetAllReglas()
        {
            var reglas = new List<ReglaAsiento>();
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                string query = "SELECT * FROM ReglasAsientos";
                using (var command = new SQLiteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var regla = new ReglaAsiento
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            NombreRegla = reader["NombreRegla"].ToString(),
                            PalabraClave = reader["PalabraClave"].ToString(),
                            Banco = reader["Banco"].ToString()
                        };
                        regla.Detalles = GetDetalles(regla.Id, connection);
                        reglas.Add(regla);
                    }
                }
            }
            return reglas;
        }

        private List<ReglaAsientoDetalle> GetDetalles(int reglaId, SQLiteConnection connection)
        {
            var detalles = new List<ReglaAsientoDetalle>();
            string query = "SELECT * FROM ReglasAsientosDetalle WHERE ReglaId = @Id";
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", reglaId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        detalles.Add(new ReglaAsientoDetalle
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            ReglaId = reglaId,
                            Tipo = reader["Tipo"].ToString(),
                            IdCuenta = reader["IdCuenta"].ToString(),
                            ConceptoTemplate = reader["ConceptoTemplate"].ToString()
                        });
                    }
                }
            }
            return detalles;
        }

        public void SaveRegla(ReglaAsiento regla)
        {
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string qInsertRegla = "INSERT INTO ReglasAsientos (NombreRegla, PalabraClave, Banco) VALUES (@N, @P, @B); SELECT last_insert_rowid();";
                        using (var cmd = new SQLiteCommand(qInsertRegla, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@N", regla.NombreRegla);
                            cmd.Parameters.AddWithValue("@P", regla.PalabraClave);
                            cmd.Parameters.AddWithValue("@B", regla.Banco);
                            regla.Id = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        foreach (var det in regla.Detalles)
                        {
                            string qInsertDet = "INSERT INTO ReglasAsientosDetalle (ReglaId, Tipo, IdCuenta, ConceptoTemplate) VALUES (@R, @T, @I, @C)";
                            using (var cmd = new SQLiteCommand(qInsertDet, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@R", regla.Id);
                                cmd.Parameters.AddWithValue("@T", det.Tipo);
                                cmd.Parameters.AddWithValue("@I", det.IdCuenta);
                                cmd.Parameters.AddWithValue("@C", det.ConceptoTemplate);
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

        public void DeleteRegla(int reglaId)
        {
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string q1 = "DELETE FROM ReglasAsientosDetalle WHERE ReglaId = @Id";
                        using (var c1 = new SQLiteCommand(q1, connection, transaction))
                        {
                            c1.Parameters.AddWithValue("@Id", reglaId);
                            c1.ExecuteNonQuery();
                        }

                        string q2 = "DELETE FROM ReglasAsientos WHERE Id = @Id";
                        using (var c2 = new SQLiteCommand(q2, connection, transaction))
                        {
                            c2.Parameters.AddWithValue("@Id", reglaId);
                            c2.ExecuteNonQuery();
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
    }
}
