namespace Tandem.Core.Entities;

// What venue, gift and meal have in common: one responsible member and the price they paid.
// The service claims and releases any of the three through this interface - one piece of logic, not three
public interface IResponsibility
{
    int? ResponsibleId { get; set; }

    decimal? Price { get; set; }
}
