using HandlingTemporalCoupling.Design;
using HandlingTemporalCoupling.Legacy;
using static HandlingTemporalCoupling.Design.CheckoutFlow;

namespace HandlingTemporalCoupling;

/// <summary>
/// Minimal built-in verification (no test framework needed).
/// Returns the number of failed checks.
/// </summary>
public static class SelfChecks
{
    private static int _passed;
    private static int _failed;

    private static void Check(string name, bool condition)
    {
        if (condition) _passed++; else _failed++;
        Console.WriteLine($"  [{(condition ? "PASS" : "FAIL")}] {name}");
    }

    /// <summary>Runs an action and returns everything it wrote to the console.</summary>
    private static string Capture(Action action)
    {
        var original = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        try { action(); }
        finally { Console.SetOut(original); }
        return writer.ToString().Replace("\r\n", "\n");
    }

    public static int RunAll()
    {
        // Task 1: legacy behaviour
        Check("Legacy 1: discount twice prints 'Charged 810.00'", Capture(LegacyDemo.DiscountTwice) == "Charged 810.00\n");
        Check("Legacy 2: forgotten discount prints 'Charged 1000.00'", Capture(LegacyDemo.DiscountForgotten) == "Charged 1000.00\n");
        Check("Legacy 3: Charge twice prints 'Charged 900.00' two times", Capture(LegacyDemo.ChargeTwice) == "Charged 900.00\nCharged 900.00\n");
        Check("Legacy 4: Charge before Validate throws 'Not validated.'",
            Capture(LegacyDemo.ChargeBeforeValidate) == "Exception caught: InvalidOperationException: Not validated.\n");

        // Task 2: stages
        var draft = Start(1000m);
        var validated = Validate(draft);
        var priced = ApplyDiscount(validated, 0.10m);
        Check("Start -> Draft(1000)", draft == new Draft(1000m));
        Check("Validate -> Validated(1000)", validated == new Validated(1000m));
        Check("ApplyDiscount(0.10m) -> Priced(900.00)", priced.Amount == 900.00m && Money(priced.Amount) == "900.00");
        Check("Earlier stages are not mutated", draft.Amount == 1000m && validated.Amount == 1000m);
        Check("Validate rejects 0", Throws(() => Validate(Start(0m))));
        Check("Validate rejects -5", Throws(() => Validate(Start(-5m))));

        // Task 3: gateways
        var fake = new FakeGateway();
        var receipt = Charge(priced, fake.Pay);
        Check("Fake gateway called exactly once", fake.CallCount == 1);
        Check("Fake gateway received 900.00", fake.ReceivedAmounts.Count == 1 && fake.ReceivedAmounts[0] == 900.00m);
        Check("Receipt = (FAKE-TX-1, 900.00)", receipt == new Receipt("FAKE-TX-1", 900.00m));

        string demoOutput = "";
        Receipt demoReceipt = null!;
        demoOutput = Capture(() => demoReceipt = Charge(priced, DemoGateway.Pay));
        Check("Demo gateway prints a processing message", demoOutput.Contains("Processing payment of 900.00"));
        Check("Same Charge works with demo gateway (id DEMO-..., 900.00)",
            demoReceipt.TransactionId.StartsWith("DEMO-") && demoReceipt.Amount == 900.00m);

        // Task 4: documented limitations
        var fake2 = new FakeGateway();
        Charge(priced, fake2.Pay);
        Charge(priced, fake2.Pay);
        Check("Limitation: same Priced can be charged twice (2 calls)", fake2.CallCount == 2);

        Console.WriteLine($"\nResult: {_passed} passed, {_failed} failed.");
        return _failed;
    }

    private static bool Throws(Action action)
    {
        try { action(); return false; }
        catch (ArgumentOutOfRangeException) { return true; }
    }
}
