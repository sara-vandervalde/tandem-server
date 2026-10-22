namespace Tandem.Core.Entities;

// A fixed list for the bank account form, filled by Seed. A table and not an enum:
// it is long and may change (banks merge), without a code change
public class Bank
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
