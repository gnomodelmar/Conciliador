using System;
using System.Collections.Generic;
using System.Data.SQLite;
using ConciliadorBancario.Models;

namespace ConciliadorBancario.Data
{
    public class ConfiguracionRepository
    {
        public ConfiguracionBanco GetConfiguracionBanco(string nombreBanco, string tipoConfiguracion)
        {
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                string query = "SELECT * FROM ConfiguracionBancos WHERE NombreBanco = @Nombre AND TipoConfiguracion = @Tipo";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Nombre", nombreBanco);
                    command.Parameters.AddWithValue("@Tipo", tipoConfiguracion);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new ConfiguracionBanco
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                NombreBanco = reader["NombreBanco"].ToString(),
                                TipoConfiguracion = reader["TipoConfiguracion"].ToString(),
                                ColumnaFecha = reader["ColumnaFecha"].ToString(),
                                ColumnaMonto = reader["ColumnaMonto"].ToString(),
                                ColumnaMontoEntrada = reader["ColumnaMontoEntrada"].ToString(),
                                ColumnaMontoSalida = reader["ColumnaMontoSalida"].ToString(),
                                ColumnaConcepto = reader["ColumnaConcepto"].ToString(),
                                ColumnaReferencia = reader["ColumnaReferencia"].ToString(),
                                ColumnaCodOperacionSistema = reader["ColumnaCodOperacionSistema"] == DBNull.Value ? "" : reader["ColumnaCodOperacionSistema"].ToString(),
                                ColumnaTipo = reader["ColumnaTipo"] == DBNull.Value ? "" : reader["ColumnaTipo"].ToString(),
                                ValorTipoSalida = reader["ValorTipoSalida"] == DBNull.Value ? "" : reader["ValorTipoSalida"].ToString()
                            };
                        }
                    }
                }
            }
            return null;
        }

        public List<ConfiguracionBanco> GetAllBancos(string tipoConfiguracion = null)
        {
            var bancos = new List<ConfiguracionBanco>();
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                string query = "SELECT * FROM ConfiguracionBancos";
                if (!string.IsNullOrEmpty(tipoConfiguracion))
                {
                    query += " WHERE TipoConfiguracion = @Tipo";
                }

                using (var command = new SQLiteCommand(query, connection))
                {
                    if (!string.IsNullOrEmpty(tipoConfiguracion))
                    {
                        command.Parameters.AddWithValue("@Tipo", tipoConfiguracion);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bancos.Add(new ConfiguracionBanco
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                NombreBanco = reader["NombreBanco"].ToString(),
                                TipoConfiguracion = reader["TipoConfiguracion"].ToString(),
                                ColumnaFecha = reader["ColumnaFecha"].ToString(),
                                ColumnaMonto = reader["ColumnaMonto"].ToString(),
                                ColumnaMontoEntrada = reader["ColumnaMontoEntrada"].ToString(),
                                ColumnaMontoSalida = reader["ColumnaMontoSalida"].ToString(),
                                ColumnaConcepto = reader["ColumnaConcepto"].ToString(),
                                ColumnaReferencia = reader["ColumnaReferencia"].ToString(),
                                ColumnaCodOperacionSistema = reader["ColumnaCodOperacionSistema"] == DBNull.Value ? "" : reader["ColumnaCodOperacionSistema"].ToString(),
                                ColumnaTipo = reader["ColumnaTipo"] == DBNull.Value ? "" : reader["ColumnaTipo"].ToString(),
                                ValorTipoSalida = reader["ValorTipoSalida"] == DBNull.Value ? "" : reader["ValorTipoSalida"].ToString()
                            });
                        }
                    }
                }
            }
            return bancos;
        }

        public void SaveConfiguracionBanco(ConfiguracionBanco config)
        {
            if (string.IsNullOrEmpty(config.TipoConfiguracion)) config.TipoConfiguracion = "Banco";

            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                var existente = GetConfiguracionBanco(config.NombreBanco, config.TipoConfiguracion);

                string query;
                if (existente == null)
                {
                    query = @"INSERT INTO ConfiguracionBancos
                              (NombreBanco, TipoConfiguracion, ColumnaFecha, ColumnaMonto, ColumnaMontoEntrada, ColumnaMontoSalida, ColumnaConcepto, ColumnaReferencia, ColumnaCodOperacionSistema, ColumnaTipo, ValorTipoSalida)
                              VALUES (@Nombre, @Tipo, @F, @M, @ME, @MS, @C, @R, @CS, @CT, @VT)";
                }
                else
                {
                    query = @"UPDATE ConfiguracionBancos
                              SET ColumnaFecha=@F, ColumnaMonto=@M, ColumnaMontoEntrada=@ME, ColumnaMontoSalida=@MS,
                                  ColumnaConcepto=@C, ColumnaReferencia=@R, ColumnaCodOperacionSistema=@CS, ColumnaTipo=@CT, ValorTipoSalida=@VT
                              WHERE NombreBanco=@Nombre AND TipoConfiguracion=@Tipo";
                }

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Nombre", config.NombreBanco);
                    command.Parameters.AddWithValue("@Tipo", config.TipoConfiguracion);
                    command.Parameters.AddWithValue("@F", config.ColumnaFecha ?? "");
                    command.Parameters.AddWithValue("@M", config.ColumnaMonto ?? "");
                    command.Parameters.AddWithValue("@ME", config.ColumnaMontoEntrada ?? "");
                    command.Parameters.AddWithValue("@MS", config.ColumnaMontoSalida ?? "");
                    command.Parameters.AddWithValue("@C", config.ColumnaConcepto ?? "");
                    command.Parameters.AddWithValue("@R", config.ColumnaReferencia ?? "");
                    command.Parameters.AddWithValue("@CS", config.ColumnaCodOperacionSistema ?? "");
                    command.Parameters.AddWithValue("@CT", config.ColumnaTipo ?? "");
                    command.Parameters.AddWithValue("@VT", config.ValorTipoSalida ?? "");
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteConfiguracionBanco(string nombreBanco, string tipoConfiguracion)
        {
            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                string query = "DELETE FROM ConfiguracionBancos WHERE NombreBanco = @Nombre AND TipoConfiguracion = @Tipo";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Nombre", nombreBanco);
                    command.Parameters.AddWithValue("@Tipo", tipoConfiguracion);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
