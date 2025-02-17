public class Company
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public required string Name { get; set; }
    public List<Employee> Employees { get; set; } = null!;
}