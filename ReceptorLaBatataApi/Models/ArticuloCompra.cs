namespace ReceptorLaBatataApi.Models
{
    public class ArticuloCompra
    {
        public string CodigoBarras { get; set; 
        }
        public string Descripcion { get; set; }
        public string Departamento { get; set; }
        public decimal StockActual { get; set; }
        public decimal PuntoReorden { get; set; }
        public decimal CantidadComprar { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal CostoTotalEstimado { get; set; }
    }
}
