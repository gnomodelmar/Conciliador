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
                                ColumnaReferencia = reader["ColumnaReferencia"].ToString()
                            };
                        }
                    }
                }
            }
            return null;
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
                              (NombreBanco, ColumnaFecha, ColumnaMonto, ColumnaMontoEntrada, ColumnaMontoSalida, ColumnaConcepto, ColumnaReferencia)
                              VALUES (@Nombre, @F, @M, @ME, @MS, @C, @R)";
                }
                else
                {
                    query = @"UPDATE ConfiguracionBancos
                              SET ColumnaFecha=@F, ColumnaMonto=@M, ColumnaMontoEntrada=@ME, ColumnaMontoSalida=@MS,
                                  ColumnaConcepto=@C, ColumnaReferencia=@R
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
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
