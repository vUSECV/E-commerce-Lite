namespace InventoryService.DTOs
{
    public class ReserveStockDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class AddStockDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class ReleaseStockDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class ConfirmStockDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
