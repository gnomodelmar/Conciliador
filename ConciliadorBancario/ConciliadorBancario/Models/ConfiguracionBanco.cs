namespace ConciliadorBancario.Models
{
    public class ConfiguracionBanco
    {
        public int Id { get; set; }
        public string NombreBanco { get; set; }
        public string ColumnaFecha { get; set; }
        public string ColumnaMonto { get; set; }
        public string ColumnaMontoEntrada { get; set; }
        public string ColumnaMontoSalida { get; set; }
        public string ColumnaConcepto { get; set; }
        public string ColumnaReferencia { get; set; }
    }
}
