using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class Employee
{
    public string EmployeeId { get; set; } = null!;

    public string? AccountId { get; set; }

    public bool Status { get; set; }

    public virtual Account? Account { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
