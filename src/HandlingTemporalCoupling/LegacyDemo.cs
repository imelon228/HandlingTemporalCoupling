namespace HandlingTemporalCoupling.Legacy;

/// <summary>
/// Task 1: four wrong call sequences against the original mutable Checkout.
/// All four COMPILE, because every method is public and callable in any order.
/// Each scenario method contains only the exact call sequence, so the
/// README / self-checks can show the real observed output.
/// </summary>
public static class LegacyDemo
{
    // Scenario 1: discount applied twice -> compiles, silently charges 810.00.
    public static void DiscountTwice()
    {
        var c = new Checkout();
        c.Start(1000m); c.Validate(); c.ApplyDiscount(0.10m); c.ApplyDiscount(0.10m); c.Charge();
    }

    // Scenario 2: discount forgotten -> compiles, silently charges 1000.00.
    public static void DiscountForgotten()
    {
        var c = new Checkout();
        c.Start(1000m); c.Validate(); c.Charge();
    }

    // Scenario 3: Charge called twice -> compiles, silently charges twice.
    public static void ChargeTwice()
    {
        var c = new Checkout();
        c.Start(1000m); c.Validate(); c.ApplyDiscount(0.10m); c.Charge(); c.Charge();
    }

    // Scenario 4: Charge before Validate -> compiles, detected at RUNTIME only.
    public static void ChargeBeforeValidate()
    {
        var c = new Checkout();
        try
        {
            c.Start(1000m); c.Charge();   // Validate() skipped
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Exception caught: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public static void RunAll()
    {
        Console.WriteLine("Scenario 1 - discount applied twice");
        Console.WriteLine("  c.Start(1000m); c.Validate(); c.ApplyDiscount(0.10m); c.ApplyDiscount(0.10m); c.Charge();");
        Console.Write("  Output: "); DiscountTwice();
        Console.WriteLine("  -> SILENT wrong result (810.00 instead of 900.00); nothing is detected.\n");

        Console.WriteLine("Scenario 2 - discount forgotten");
        Console.WriteLine("  c.Start(1000m); c.Validate(); c.Charge();");
        Console.Write("  Output: "); DiscountForgotten();
        Console.WriteLine("  -> SILENT wrong result (1000.00, customer loses the discount); nothing is detected.\n");

        Console.WriteLine("Scenario 3 - Charge called twice");
        Console.WriteLine("  c.Start(1000m); c.Validate(); c.ApplyDiscount(0.10m); c.Charge(); c.Charge();");
        Console.Write("  Output: "); ChargeTwice();
        Console.WriteLine("  -> SILENT double charge (900.00 leaves the account twice); nothing is detected.\n");

        Console.WriteLine("Scenario 4 - Charge before Validate (try/catch)");
        Console.WriteLine("  c.Start(1000m); c.Charge();   // Validate() skipped");
        Console.Write("  Output: "); ChargeBeforeValidate();
        Console.WriteLine("  -> DETECTED, but only at runtime (InvalidOperationException); the compiler accepted it.\n");
    }
}
