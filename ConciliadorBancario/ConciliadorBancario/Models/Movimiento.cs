using System;

namespace ConciliadorBancario.Models
{
    public class Movimiento
    {
        public int Id { get; set; }
        public int LoteId { get; set; }
        public string TipoFuente { get; set; } // "Banco" o "Sistema"
        public DateTime Fecha { get; set; }
        public double Monto { get; set; }
        public string Concepto { get; set; }
        public string Referencia_CodOperacion { get; set; } // Cod Banco
        public string CodOperacionSistema { get; set; } // Cod Sistema (Only applies to System rows)
        public string Estado { get; set; }
        public string Observaciones { get; set; }
        public int? MatchId { get; set; }
        public string Banco { get; set; }
        public bool Activo { get; set; }

        public string MontoFormateado => Monto.ToString("C2");
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
    }

    public static class EstadosMovimiento
    {
        public const string NoEncontrado = "No encontrado";
        public const string PendienteCorregir = "Pendiente de corregir en sistema";
        public const string PendientePasar = "Pendiente de pasar";
        public const string PendienteAsientoMasivo = "Pend. Asiento Masivo";
        public const string Conciliado = "Conciliado";
        public const string PosibleMatch = "Posible match manual";
    }
}
