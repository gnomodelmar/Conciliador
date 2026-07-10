using System;
using System.Collections.Generic;
using System.Data.SQLite;
using ConciliadorBancario.Models;

namespace ConciliadorBancario.Data
{
    public class ConfiguracionRepository
    {
        public ConfiguracionBanco GetConfiguracionBanco(string nombreBanco)
        {
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                string query = "SELECT * FROM ConfiguracionBancos WHERE NombreBanco = @Nombre";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Nombre", nombreBanco);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new ConfiguracionBanco
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                NombreBanco = reader["NombreBanco"].ToString(),
                                ColumnaFecha = reader["ColumnaFecha"].ToString(),
                                ColumnaMonto = reader["ColumnaMonto"].ToString(),
                                ColumnaMontoEntrada = reader["ColumnaMontoEntrada"].ToString(),
                                ColumnaMontoSalida = reader["ColumnaMontoSalida"].ToString(),
                                ColumnaConcepto = reader["ColumnaConcepto"].ToString(),
                                ColumnaReferencia = reader["ColumnaReferencia"].ToString(),
                                ColumnaTipo = reader["ColumnaTipo"] == DBNull.Value ? "" : reader["ColumnaTipo"].ToString(),
                                ValorTipoSalida = reader["ValorTipoSalida"] == DBNull.Value ? "" : reader["ValorTipoSalida"].ToString()
                            };
                        }
                    }
                }
            }
            return null;
        }

        public List<ConfiguracionBanco> GetAllBancos()
        {
            var bancos = new List<ConfiguracionBanco>();
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                string query = "SELECT * FROM ConfiguracionBancos";
                using (var command = new SQLiteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bancos.Add(new ConfiguracionBanco
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            NombreBanco = reader["NombreBanco"].ToString(),
                            ColumnaFecha = reader["ColumnaFecha"].ToString(),
                            ColumnaMonto = reader["ColumnaMonto"].ToString(),
                            ColumnaMontoEntrada = reader["ColumnaMontoEntrada"].ToString(),
                            ColumnaMontoSalida = reader["ColumnaMontoSalida"].ToString(),
                            ColumnaConcepto = reader["ColumnaConcepto"].ToString(),
                            ColumnaReferencia = reader["ColumnaReferencia"].ToString(),
                            ColumnaTipo = reader["ColumnaTipo"] == DBNull.Value ? "" : reader["ColumnaTipo"].ToString(),
                            ValorTipoSalida = reader["ValorTipoSalida"] == DBNull.Value ? "" : reader["ValorTipoSalida"].ToString()
                        });
                    }
                }
            }
            return bancos;
        }

        public void SaveConfiguracionBanco(ConfiguracionBanco config)
        {
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                var existente = GetConfiguracionBanco(config.NombreBanco);

                string query;
                if (existente == null)
                {
                    query = @"INSERT INTO ConfiguracionBancos
                              (NombreBanco, ColumnaFecha, ColumnaMonto, ColumnaMontoEntrada, ColumnaMontoSalida, ColumnaConcepto, ColumnaReferencia, ColumnaTipo, ValorTipoSalida)
                              VALUES (@Nombre, @F, @M, @ME, @MS, @C, @R, @CT, @VT)";
                }
                else
                {
                    query = @"UPDATE ConfiguracionBancos
                              SET ColumnaFecha=@F, ColumnaMonto=@M, ColumnaMontoEntrada=@ME, ColumnaMontoSalida=@MS,
                                  ColumnaConcepto=@C, ColumnaReferencia=@R, ColumnaTipo=@CT, ValorTipoSalida=@VT
                              WHERE NombreBanco=@Nombre";
                }

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Nombre", config.NombreBanco);
                    command.Parameters.AddWithValue("@F", config.ColumnaFecha ?? "");
                    command.Parameters.AddWithValue("@M", config.ColumnaMonto ?? "");
                    command.Parameters.AddWithValue("@ME", config.ColumnaMontoEntrada ?? "");
                    command.Parameters.AddWithValue("@MS", config.ColumnaMontoSalida ?? "");
                    command.Parameters.AddWithValue("@C", config.ColumnaConcepto ?? "");
                    command.Parameters.AddWithValue("@R", config.ColumnaReferencia ?? "");
                    command.Parameters.AddWithValue("@CT", config.ColumnaTipo ?? "");
                    command.Parameters.AddWithValue("@VT", config.ValorTipoSalida ?? "");
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteConfiguracionBanco(string nombreBanco)
        {
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                string query = "DELETE FROM ConfiguracionBancos WHERE NombreBanco = @Nombre";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Nombre", nombreBanco);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
