using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using ConciliadorBancario.Data;
using ConciliadorBancario.Models;

namespace ConciliadorBancario.Services
{
    public class AsientoMasivoExportService
    {
        private readonly MovimientoRepository _movRepo;
        private readonly ReglasRepository _reglasRepo;

        public AsientoMasivoExportService()
        {
            _movRepo = new MovimientoRepository();
            _reglasRepo = new ReglasRepository();
        }

        public void ExportarAsientosMasivos(string filePath)
        {
            var pendientesAM = _movRepo.GetPendientes("Banco")
                .Where(m => m.Estado == EstadosMovimiento.PendienteAsientoMasivo)
                .ToList();

            if (pendientesAM.Count == 0) return;

            var reglas = _reglasRepo.GetAllReglas();
            var dt = new DataTable();
            dt.Columns.Add("Fecha");
            dt.Columns.Add("Tipo");
            dt.Columns.Add("ID Cuenta");
            dt.Columns.Add("Concepto");
            dt.Columns.Add("Importe");
            dt.Columns.Add("CodOperacionPago");

            string currentMonth = DateTime.Now.ToString("MM/yyyy");
            string uuidBatch = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();

            foreach (var mov in pendientesAM)
            {
                // Find matching rule
                var rule = reglas.FirstOrDefault(r =>
                    r.Banco == mov.Banco &&
                    mov.Concepto.IndexOf(r.PalabraClave, StringComparison.OrdinalIgnoreCase) >= 0);

                if (rule != null)
                {
                    foreach (var det in rule.Detalles)
                    {
                        var row = dt.NewRow();
                        row["Fecha"] = mov.Fecha.ToString("yyyy-MM-dd");
                        row["Tipo"] = det.Tipo;
                        row["ID Cuenta"] = det.IdCuenta;

                        string conceptoFinal = det.ConceptoTemplate
                            .Replace("{MM/YYYY}", currentMonth)
                            .Replace("{BANCO}", mov.Banco);

                        row["Concepto"] = $"{conceptoFinal} [AM-{uuidBatch}]";
                        row["Importe"] = Math.Abs(mov.Monto).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                        row["CodOperacionPago"] = mov.Referencia_CodOperacion ?? "";

                        dt.Rows.Add(row);
                    }
                }
            }

            if (filePath.EndsWith(".xlsx"))
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Asientos Masivos");
                    worksheet.Cell(1, 1).InsertTable(dt);
                    workbook.SaveAs(filePath);
                }
            }
            else if (filePath.EndsWith(".csv"))
            {
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine(string.Join(";", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
                    foreach (DataRow row in dt.Rows)
                    {
                        writer.WriteLine(string.Join(";", row.ItemArray));
                    }
                }
            }
        }
    }
}
