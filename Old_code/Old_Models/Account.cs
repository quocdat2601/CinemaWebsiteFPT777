using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class Account
{
    public string AccountId { get; set; } = null!;

    public string? Address { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Email { get; set; }

    public string? FullName { get; set; }

    public string? Gender { get; set; }

    public string? IdentityCard { get; set; }

    public string? Image { get; set; }

    public string? Password { get; set; }

    public string? PhoneNumber { get; set; }

    public DateOnly? RegisterDate { get; set; }

    public int? RoleId { get; set; }

    public int? Status { get; set; }

    public string? Username { get; set; }

    public int? RankId { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Member> Members { get; set; } = new List<Member>();

    public virtual Rank? Rank { get; set; }

    public virtual Role? Role { get; set; }

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();

    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();
}
