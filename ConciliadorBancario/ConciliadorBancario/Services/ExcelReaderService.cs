using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using System.Text.RegularExpressions;

namespace ConciliadorBancario.Services
{
    public class ExcelReaderService
    {
        public DataTable ReadExcelToDataTable(string filePath)
        {
            var dt = new DataTable();
            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1);
                    bool firstRow = true;

                    foreach (var row in worksheet.RowsUsed())
                    {
                        if (firstRow)
                        {
                            foreach (var cell in row.Cells())
                            {
                                dt.Columns.Add(cell.Value.ToString().Trim());
                            }
                            firstRow = false;
                        }
                        else
                        {
                            dt.Rows.Add();
                            int i = 0;
                            foreach (var cell in row.Cells(1, dt.Columns.Count))
                            {
                                dt.Rows[dt.Rows.Count - 1][i] = cell.Value.ToString();
                                i++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error leyendo Excel: {ex.Message}");
            }
            return dt;
        }

        public DataTable ReadCsvToDataTable(string filePath)
        {
            var dt = new DataTable();
            try
            {
                var lines = File.ReadAllLines(filePath);
                if (lines.Length == 0) return dt;

                string firstLine = lines[0];
                char delimiter = firstLine.Contains(";") ? ';' : ',';

                // Simple regex to split by delimiter but ignore delimiters inside quotes
                string pattern = string.Format("{0}(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)", delimiter);

                var headers = Regex.Split(firstLine, pattern);
                foreach (var header in headers)
                {
                    dt.Columns.Add(header.Trim('\"', ' '));
                }

                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    var row = Regex.Split(lines[i], pattern);
                    var cleanRow = row.Select(r => r.Trim('\"', ' ')).ToArray();
                    dt.Rows.Add(cleanRow);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error leyendo CSV: {ex.Message}");
            }
            return dt;
        }
    }
}
