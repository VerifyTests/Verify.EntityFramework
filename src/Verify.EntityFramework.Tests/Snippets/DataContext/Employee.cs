public class Employee
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public required string Name { get; set; }
    public int Age { get; set; }
}