using Tandem.Service.Settlements;

namespace Tandem.Tests.Settlements;

// The examples from USER_FLOW.md, section 10: four members, the event at Sara's home (0),
// Rachel bought the gift (400), Leah ordered the catering (800). Total 1,200
public class SettlementCalculatorTests
{
    private const int Sara = 1;
    private const int Rachel = 2;
    private const int Leah = 3;
    private const int Rivka = 4;

    private static readonly int[] AllMembers = [Sara, Rachel, Leah, Rivka];

    private static readonly Payment[] Payments =
    [
        new(Sara, 0),       // the venue: her home
        new(Rachel, 400),   // the gift
        new(Leah, 800)      // the catering
    ];

    [Fact]
    public void Equal_SplitsBetweenAllMembers()
    {
        // 300 each: Sara -300, Rachel +100, Leah +500, Rivka -300
        var transfers = SettlementCalculator.Calculate(AllMembers, Payments);

        Assert.Equal(
        [
            new Transfer(Sara, Leah, 300),
            new Transfer(Rivka, Leah, 200),
            new Transfer(Rivka, Rachel, 100)
        ], transfers);
    }

    [Fact]
    public void OrganizerPays_ReturnsEveryoneWhatTheyPaid()
    {
        // Sara is the organizer and the only one sharing the cost
        var transfers = SettlementCalculator.Calculate([Sara], Payments);

        Assert.Equal(
        [
            new Transfer(Sara, Leah, 800),
            new Transfer(Sara, Rachel, 400)
        ], transfers);
    }

    [Fact]
    public void SelectedMembers_PayerWhoDoesNotShare_GetsEverythingBack()
    {
        // Rachel bought the gift but is not a participant: 1,200 / 3 = 400 each
        var transfers = SettlementCalculator.Calculate([Sara, Leah, Rivka], Payments);

        Assert.Equal(
        [
            new Transfer(Sara, Rachel, 400),
            new Transfer(Rivka, Leah, 400)
        ], transfers);
    }

    [Fact]
    public void AmountThatDoesNotDivide_RemainderGoesToFirstMembers_TotalBalances()
    {
        // 100 between 3 = 33.33...: parts of 33.34, 33.33, 33.33
        var transfers = SettlementCalculator.Calculate([Sara, Rachel, Leah], [new Payment(Leah, 100)]);

        Assert.Equal(
        [
            new Transfer(Sara, Leah, 33.34m),
            new Transfer(Rachel, Leah, 33.33m)
        ], transfers);
        // Leah's own part (33.33) + what she receives = exactly what she paid
        Assert.Equal(100m, transfers.Sum(t => t.Amount) + 33.33m);
    }

    [Fact]
    public void SeveralPayers_SamePerson_AreAddedUp()
    {
        // Sara paid for the venue and the meal: 200 + 100 = 300; split between 2 = 150 each
        var transfers = SettlementCalculator.Calculate([Sara, Rachel], [new Payment(Sara, 200), new Payment(Sara, 100)]);

        Assert.Equal([new Transfer(Rachel, Sara, 150)], transfers);
    }

    [Fact]
    public void AllPricesZero_NoTransfers()
    {
        var transfers = SettlementCalculator.Calculate(AllMembers, [new Payment(Sara, 0), new Payment(Leah, 0)]);

        Assert.Empty(transfers);
    }

    [Fact]
    public void EveryonePaidTheirPart_NoTransfers()
    {
        var transfers = SettlementCalculator.Calculate([Sara, Rachel], [new Payment(Sara, 50), new Payment(Rachel, 50)]);

        Assert.Empty(transfers);
    }

    [Fact]
    public void NoSharers_Throws()
    {
        Assert.Throws<ArgumentException>(() => SettlementCalculator.Calculate([], Payments));
    }
}
