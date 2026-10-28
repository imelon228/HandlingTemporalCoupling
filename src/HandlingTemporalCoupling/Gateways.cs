namespace HandlingTemporalCoupling.Design;

/// <summary>
/// "Real" (demo) gateway for Task 3. It only prints a message and invents a
/// transaction id; no bank or network is involved.
/// Usage: CheckoutFlow.Charge(priced, DemoGateway.Pay)
/// </summary>
public static class DemoGateway
{
    public static string Pay(decimal amount)
    {
        Console.WriteLine($"[DemoGateway] Processing payment of {CheckoutFlow.Money(amount)} ...");
        return "DEMO-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    }
}

/// <summary>
/// Fake gateway for tests: counts calls, remembers the amounts it received
/// and returns a predictable transaction id (FAKE-TX-1, FAKE-TX-2, ...).
/// Usage: var fake = new FakeGateway(); CheckoutFlow.Charge(priced, fake.Pay)
/// </summary>
public sealed class FakeGateway
{
    private readonly List<decimal> _amounts = new();

    public int CallCount => _amounts.Count;
    public IReadOnlyList<decimal> ReceivedAmounts => _amounts;

    public string Pay(decimal amount)
    {
        _amounts.Add(amount);
        return $"FAKE-TX-{_amounts.Count}";
    }
}
