using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ConciliadorBancario.Services
{
    public static class DataSanitizer
    {
        public static double ParseMonto(string montoStr)
        {
            if (string.IsNullOrWhiteSpace(montoStr)) return 0;

            // Remove currency symbols, keep negative sign and numbers/decimals
            montoStr = Regex.Replace(montoStr, @"[^\d.,-]", "");

            // Handle different decimal separators (assume comma or dot)
            // A simple heuristic: if it contains both, the last one is the decimal separator
            int lastDot = montoStr.LastIndexOf('.');
            int lastComma = montoStr.LastIndexOf(',');

            if (lastDot > lastComma)
            {
                // Dot is decimal
                montoStr = montoStr.Replace(",", "");
            }
            else if (lastComma > lastDot)
            {
                // Comma is decimal
                montoStr = montoStr.Replace(".", "").Replace(",", ".");
            }

            if (double.TryParse(montoStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }
            return 0;
        }

        public static DateTime? ParseFecha(string fechaStr)
        {
            if (string.IsNullOrWhiteSpace(fechaStr)) return null;

            string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "dd-MM-yyyy" };
            if (DateTime.TryParseExact(fechaStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                return result;
            }

            if (DateTime.TryParse(fechaStr, out result))
            {
                return result;
            }

            return null;
        }
    }
}
