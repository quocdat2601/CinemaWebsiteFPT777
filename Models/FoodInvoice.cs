using System.ComponentModel.DataAnnotations.Schema;

namespace MovieTheater.Models
{
    public class FoodInvoice
    {
        public int FoodInvoiceId { get; set; }
        public string InvoiceId { get; set; }
        public int FoodId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        public virtual Invoice Invoice { get; set; }
        public virtual Food Food { get; set; }
    }
} 