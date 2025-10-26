namespace Jango_Travel.Models;

public class Country
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!; // ISO-код, например "IT" или "KZ"
}