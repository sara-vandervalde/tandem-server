namespace Tandem.Service.Settlements;

// One transfer the calculation produces: FromUserId pays Amount to ToUserId
internal record Transfer(int FromUserId, int ToUserId, decimal Amount);

// What one member paid for an area (venue, gift or meal)
internal record Payment(int PayerId, decimal Amount);

// Who pays whom. A pure calculation: no database, no interface - the service gathers the input.
// The split method only decides who shares the cost:
//   Equal           - every group member
//   SelectedMembers - the chosen participants
//   OrganizerPays   - the organizer alone
internal static class SettlementCalculator
{
    public static List<Transfer> Calculate(IReadOnlyList<int> sharerIds, IReadOnlyList<Payment> payments)
    {
        if (sharerIds.Count == 0)
        {
            throw new ArgumentException("At least one member must share the cost.", nameof(sharerIds));
        }

        // Work in agorot (whole numbers), so dividing never loses or invents money
        var balances = new Dictionary<int, long>();
        foreach (var payment in payments.Where(p => p.Amount > 0))
        {
            balances[payment.PayerId] = balances.GetValueOrDefault(payment.PayerId) + ToAgorot(payment.Amount);
        }

        // 1. Each sharer's part. The remainder of the division goes one agora at a time
        //    to the first sharers, so the parts always add up to the exact total
        var total = balances.Values.Sum();
        var sharers = sharerIds.Distinct().OrderBy(id => id).ToList();
        var share = total / sharers.Count;
        var remainder = total % sharers.Count;
        for (var i = 0; i < sharers.Count; i++)
        {
            var part = share + (i < remainder ? 1 : 0);
            balances[sharers[i]] = balances.GetValueOrDefault(sharers[i]) - part;
        }

        // 2. Balance = paid - part. Negative = owes money, positive = gets money back
        //    (a payer who does not share the cost simply gets back everything they paid)
        var debtors = balances.Where(b => b.Value < 0)
            .Select(b => new Balance(b.Key, -b.Value))
            .OrderBy(b => b.UserId)
            .ToList();
        var creditors = balances.Where(b => b.Value > 0)
            .Select(b => new Balance(b.Key, b.Value))
            .OrderByDescending(b => b.Amount).ThenBy(b => b.UserId)
            .ToList();

        // 3. Match them: each debtor pays the creditor who is owed the most, until both sides are settled
        var transfers = new List<Transfer>();
        var c = 0;
        foreach (var debtor in debtors)
        {
            while (debtor.Amount > 0)
            {
                var creditor = creditors[c];
                var amount = Math.Min(debtor.Amount, creditor.Amount);
                transfers.Add(new Transfer(debtor.UserId, creditor.UserId, amount / 100m));

                debtor.Amount -= amount;
                creditor.Amount -= amount;
                if (creditor.Amount == 0)
                {
                    c++;
                }
            }
        }

        return transfers;
    }

    private static long ToAgorot(decimal amount) => (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);

    private sealed class Balance(int userId, long amount)
    {
        public int UserId { get; } = userId;

        public long Amount { get; set; } = amount;
    }
}
