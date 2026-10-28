namespace HandlingTemporalCoupling.Legacy;

/// <summary>
/// The ORIGINAL mutable design from the assignment (copied unchanged).
/// It is kept only to demonstrate temporal coupling in Task 1.
/// </summary>
public class Checkout
{
    private decimal _total;
    private bool _validated;

    public void Start(decimal amount)          { _total = amount; }
    public void Validate()                     { _validated = _total > 0; }
    public void ApplyDiscount(decimal percent) { _total -= _total * percent; }

    public void Charge()
    {
        if (!_validated) throw new InvalidOperationException("Not validated.");
        Console.WriteLine($"Charged {_total:F2}");   // imagine: the money leaves the account here
    }
}
