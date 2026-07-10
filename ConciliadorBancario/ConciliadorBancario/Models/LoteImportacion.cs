using System;

namespace ConciliadorBancario.Models
{
    public class LoteImportacion
    {
        public int Id { get; set; }
        public DateTime FechaImportacion { get; set; }
        public string TipoFuente { get; set; }
        public string NombreArchivo { get; set; }
        public int RegistrosImportados { get; set; }
    }
}
