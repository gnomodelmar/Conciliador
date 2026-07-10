using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using ConciliadorBancario.Data;
using ConciliadorBancario.Models;

namespace ConciliadorBancario.Services
{
    public class ImportadorService
    {
        private readonly ExcelReaderService _excelReader;
        private readonly LoteRepository _loteRepo;
        private readonly MovimientoRepository _movRepo;
        private readonly ConfiguracionRepository _confRepo;
        private readonly ReglasRepository _reglasRepo;

        public ImportadorService()
        {
            _excelReader = new ExcelReaderService();
            _loteRepo = new LoteRepository();
            _movRepo = new MovimientoRepository();
            _confRepo = new ConfiguracionRepository();
            _reglasRepo = new ReglasRepository();
        }

        public List<Movimiento> PrepararImportacionBanco(string filePath, string nombreBanco)
        {
            var conf = _confRepo.GetConfiguracionBanco(nombreBanco, "Banco");
            if (conf == null) throw new Exception($"No hay configuración mapeada para el banco: {nombreBanco}");

            DataTable dt = filePath.EndsWith(".csv") ? _excelReader.ReadCsvToDataTable(filePath) : _excelReader.ReadExcelToDataTable(filePath);
            var movimientos = new List<Movimiento>();
            var reglas = _reglasRepo.GetAllReglas();

            foreach (DataRow row in dt.Rows)
            {
                var mov = new Movimiento
                {
                    TipoFuente = "Banco",
                    Banco = nombreBanco,
                    Estado = EstadosMovimiento.NoEncontrado,
                    Activo = true
                };

                string fechaStr = GetRowValue(row, conf.ColumnaFecha);
                var f = DataSanitizer.ParseFecha(fechaStr);
                if (f == null) continue; // Skip invalid rows
                mov.Fecha = f.Value;

                if (!string.IsNullOrEmpty(conf.ColumnaMonto))
                {
                    mov.Monto = DataSanitizer.ParseMonto(GetRowValue(row, conf.ColumnaMonto));

                    if (!string.IsNullOrEmpty(conf.ColumnaTipo) && !string.IsNullOrEmpty(conf.ValorTipoSalida))
                    {
                        string tipo = GetRowValue(row, conf.ColumnaTipo);
                        if (tipo.Equals(conf.ValorTipoSalida, StringComparison.OrdinalIgnoreCase))
                        {
                            mov.Monto = -Math.Abs(mov.Monto);
                        }
                    }
                }
                else
                {
                    double entrada = DataSanitizer.ParseMonto(GetRowValue(row, conf.ColumnaMontoEntrada));
                    double salida = DataSanitizer.ParseMonto(GetRowValue(row, conf.ColumnaMontoSalida));
                    mov.Monto = entrada > 0 ? entrada : -salida;
                }

                mov.Concepto = GetRowValue(row, conf.ColumnaConcepto);
                mov.Referencia_CodOperacion = GetRowValue(row, conf.ColumnaReferencia);

                foreach (var regla in reglas)
                {
                    if (regla.Banco == nombreBanco && mov.Concepto.IndexOf(regla.PalabraClave, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        mov.Estado = EstadosMovimiento.PendienteAsientoMasivo;
                        break;
                    }
                }

                var similares = _movRepo.BuscarSimilares("Banco", mov.Fecha, mov.Monto, mov.Concepto);
                if (similares.Count > 0)
                {
                    mov.Observaciones = "Posible Duplicado (Ya cargado en el sistema)";
                }

                movimientos.Add(mov);
            }
            return movimientos;
        }

        public List<Movimiento> PrepararImportacionSistema(string filePath, ConfiguracionBanco conf)
        {
            if (conf == null) throw new Exception("Configuración no encontrada para el sistema.");

            DataTable dt = filePath.EndsWith(".csv") ? _excelReader.ReadCsvToDataTable(filePath) : _excelReader.ReadExcelToDataTable(filePath);
            var movimientos = new List<Movimiento>();

            foreach (DataRow row in dt.Rows)
            {
                var mov = new Movimiento
                {
                    TipoFuente = "Sistema",
                    Estado = EstadosMovimiento.NoEncontrado,
                    Activo = true
                };

                string fechaStr = GetRowValue(row, conf.ColumnaFecha);
                var f = DataSanitizer.ParseFecha(fechaStr);
                if (f == null) continue;
                mov.Fecha = f.Value;

                if (!string.IsNullOrEmpty(conf.ColumnaMonto))
                {
                    mov.Monto = DataSanitizer.ParseMonto(GetRowValue(row, conf.ColumnaMonto));

                    if (!string.IsNullOrEmpty(conf.ColumnaTipo) && !string.IsNullOrEmpty(conf.ValorTipoSalida))
                    {
                        string tipo = GetRowValue(row, conf.ColumnaTipo);
                        if (tipo.Equals(conf.ValorTipoSalida, StringComparison.OrdinalIgnoreCase))
                        {
                            mov.Monto = -Math.Abs(mov.Monto);
                        }
                    }
                }
                else
                {
                    double entrada = DataSanitizer.ParseMonto(GetRowValue(row, conf.ColumnaMontoEntrada));
                    double salida = DataSanitizer.ParseMonto(GetRowValue(row, conf.ColumnaMontoSalida));
                    mov.Monto = entrada > 0 ? entrada : -salida;
                }

                mov.Concepto = GetRowValue(row, conf.ColumnaConcepto);
                mov.Referencia_CodOperacion = GetRowValue(row, conf.ColumnaReferencia);
                mov.CodOperacionSistema = GetRowValue(row, conf.ColumnaCodOperacionSistema);
                mov.Banco = conf.NombreBanco;

                movimientos.Add(mov);
            }
            return movimientos;
        }

        public void GuardarLote(List<Movimiento> movimientos, string fileName, string tipoFuente)
        {
            if (movimientos.Count == 0) return;

            DateTime minFecha = DateTime.MaxValue;
            DateTime maxFecha = DateTime.MinValue;

            using (var connection = new SQLiteConnection(DatabaseHelper.ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        if (tipoFuente == "Sistema")
                        {
                            string bancoRef = movimientos.First().Banco;
                            foreach (var mov in movimientos)
                            {
                                if (mov.Fecha < minFecha) minFecha = mov.Fecha;
                                if (mov.Fecha > maxFecha) maxFecha = mov.Fecha;
                            }
                            if (minFecha <= maxFecha)
                            {
                                // Pass the specific bank so we don't deactivate rows from other banks
                                _movRepo.DesactivarMovimientosSistemaPorFecha(minFecha, maxFecha, bancoRef, connection, transaction);
                            }
                        }

                        var lote = new LoteImportacion
                        {
                            FechaImportacion = DateTime.Now,
                            TipoFuente = tipoFuente,
                            NombreArchivo = Path.GetFileName(fileName),
                            RegistrosImportados = movimientos.Count
                        };

                        int loteId = _loteRepo.InsertLote(lote, connection, transaction);

                        foreach (var mov in movimientos)
                        {
                            mov.LoteId = loteId;
                            _movRepo.InsertMovimiento(mov, connection, transaction);
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

        private string GetRowValue(DataRow row, string colName)
        {
            if (string.IsNullOrEmpty(colName) || !row.Table.Columns.Contains(colName)) return "";
            return row[colName]?.ToString().Trim() ?? "";
        }
    }
}
