using System.Globalization;

namespace HandlingTemporalCoupling.Design;

/// <summary>
/// Tasks 2 and 3: the transitions Draft -> Validated -> Priced -> Receipt.
/// Each function takes the previous stage and returns the next one, so the
/// compiler checks the order. Only Charge performs an effect, and it does so
/// through the injected gateway delegate.
/// </summary>
public static class CheckoutFlow
{
    /// <summary>Entry point: creates the first stage. Pure.</summary>
    public static Draft Start(decimal amount) => new(amount);

    /// <summary>Draft -> Validated. Throws for amount &lt;= 0 (allowed by the assignment). Pure.</summary>
    public static Validated Validate(Draft draft)
    {
        if (draft.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(draft), draft.Amount, "Amount must be greater than zero.");
        return new Validated(draft.Amount);
    }

    /// <summary>
    /// Validated -> Priced. Pure: returns a NEW record, the input is untouched.
    /// percent is a fraction (0.10m = 10 %). The result is rounded to cents.
    /// </summary>
    public static Priced ApplyDiscount(Validated validated, decimal percent)
    {
        decimal discounted = validated.Amount - validated.Amount * percent;
        return new Priced(decimal.Round(discounted, 2, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Priced -> Receipt. The ONLY step with a side effect, and the effect
    /// lives entirely inside the gateway that the caller passes in.
    /// </summary>
    public static Receipt Charge(Priced priced, Func<decimal, string> gateway)
    {
        string transactionId = gateway(priced.Amount);
        return new Receipt(transactionId, priced.Amount);
    }

    /// <summary>Formats money with two decimals and a dot, independent of the PC's culture.</summary>
    public static string Money(decimal amount) => amount.ToString("F2", CultureInfo.InvariantCulture);
}
