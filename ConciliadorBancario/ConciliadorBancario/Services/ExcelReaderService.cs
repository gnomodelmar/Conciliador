using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ExcelDataReader;

namespace ConciliadorBancario.Services
{
    public class ExcelReaderService
    {
        public ExcelReaderService()
        {
            // Required for reading Excel 1252 encodings with ExcelDataReader on modern .NET
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public DataTable ReadExcelToDataTable(string filePath)
        {
            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var conf = new ExcelDataSetConfiguration
                        {
                            ConfigureDataTable = _ => new ExcelDataTableConfiguration
                            {
                                UseHeaderRow = true
                            }
                        };

                        var dataSet = reader.AsDataSet(conf);
                        if (dataSet.Tables.Count > 0)
                        {
                            return dataSet.Tables[0];
                        }
                        return new DataTable();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error leyendo Excel: {ex.Message}");
            }
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
