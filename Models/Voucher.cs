using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class Voucher
{
    public string VoucherId { get; set; } = null!;

    public string AccountId { get; set; } = null!;

    public string Code { get; set; } = null!;

    public decimal Value { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    public bool? IsUsed { get; set; }

    public string? Image { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
