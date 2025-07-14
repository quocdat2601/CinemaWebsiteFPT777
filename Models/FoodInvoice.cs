using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class FoodInvoice
{
    public class FoodInvoice
    {
        public int FoodInvoiceId { get; set; }

    public string InvoiceId { get; set; } = null!;

        public int FoodId { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

    public virtual Food Food { get; set; } = null!;

    public virtual Invoice Invoice { get; set; } = null!;
} 