namespace MovieTheater.Models;

public partial class Employee
{
    public string EmployeeId { get; set; } = null!;

    public string? AccountId { get; set; }

    public virtual Account? Account { get; set; }
}
