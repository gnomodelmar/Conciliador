using System.Collections.Generic;

namespace ConciliadorBancario.Models
{
    public class ReglaAsiento
    {
        public int Id { get; set; }
        public string NombreRegla { get; set; }
        public string PalabraClave { get; set; }
        public string Banco { get; set; }

        public List<ReglaAsientoDetalle> Detalles { get; set; } = new List<ReglaAsientoDetalle>();
    }

    public class ReglaAsientoDetalle
    {
        public int Id { get; set; }
        public int ReglaId { get; set; }
        public string Tipo { get; set; } // 'Entrada' o 'Salida'
        public string IdCuenta { get; set; }
        public string ConceptoTemplate { get; set; }
    }
}
