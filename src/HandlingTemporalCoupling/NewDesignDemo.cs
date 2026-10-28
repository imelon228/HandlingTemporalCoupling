using HandlingTemporalCoupling.Design;
using static HandlingTemporalCoupling.Design.CheckoutFlow;

namespace HandlingTemporalCoupling;

/// <summary>Tasks 2, 3 and 4: the redesigned, stage-typed checkout.</summary>
public static class NewDesignDemo
{
    public static void Run()
    {
        Console.WriteLine("--- Task 2: immutable stages ---");
        Draft draft = Start(1000m);
        Validated validated = Validate(draft);
        Priced priced = ApplyDiscount(validated, 0.10m);
        Console.WriteLine($"Draft     : {Money(draft.Amount)}");
        Console.WriteLine($"Validated : {Money(validated.Amount)}");
        Console.WriteLine($"Priced    : {Money(priced.Amount)}   (10 % discount)");
        Console.WriteLine($"Earlier stages unchanged: draft={Money(draft.Amount)}, validated={Money(validated.Amount)}");

        try { Validate(Start(0m)); }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine($"Validate(Start(0m)) threw {ex.GetType().Name}: amount must be greater than zero.");
        }

        Console.WriteLine("\n--- Task 3: the same Charge with two gateways ---");
        Receipt demoReceipt = Charge(priced, DemoGateway.Pay);
        Console.WriteLine($"Receipt (demo gateway): id={demoReceipt.TransactionId}, amount={Money(demoReceipt.Amount)}");

        var fake = new FakeGateway();
        Receipt fakeReceipt = Charge(priced, fake.Pay);
        Console.WriteLine($"Receipt (fake gateway): id={fakeReceipt.TransactionId}, amount={Money(fakeReceipt.Amount)}");
        Console.WriteLine($"Fake gateway: calls={fake.CallCount}, amount received={Money(fake.ReceivedAmounts[0])}");

        Console.WriteLine("\n--- Task 4: what the types still allow (runtime demonstration) ---");
        var fake2 = new FakeGateway();
        Charge(priced, fake2.Pay);
        Charge(priced, fake2.Pay);                        // same Priced charged twice: compiles
        Console.WriteLine($"Charge(priced) twice -> fake gateway calls = {fake2.CallCount} (double charge NOT prevented)");

        var forged = new Priced(1m);                      // public constructor: anyone can create a Priced
        Console.WriteLine($"new Priced(1m) compiles -> would charge {Money(forged.Amount)} without validation or discount");
    }

    // ==================================================================
    // TASK 4 - the wrong sequences from Task 1 in the NEW design.
    // They are comments so that this project still builds. The real
    // compiler output was produced by compile-failures/CompileFailures.csproj
    // (dotnet SDK 8.0.132). Line numbers refer to those files.
    //
    // 1) Discount applied twice
    //      var priced = ApplyDiscount(validated, 0.10m);
    //      var twice  = ApplyDiscount(priced, 0.10m);
    //    REJECTED by the compiler:
    //      error CS1503: Argument 1: cannot convert from
    //      'HandlingTemporalCoupling.Design.Priced' to 'HandlingTemporalCoupling.Design.Validated'
    //    Why: ApplyDiscount(Validated, decimal) cannot take a Priced, and a Priced
    //    is not a Validated. Still possible in another form: ApplyDiscount(new Validated(priced.Amount), 0.10m)
    //    compiles (public constructor), so a deliberate double discount can be written.
    //
    // 2) Discount forgotten
    //      var receipt = Charge(validated, DemoGateway.Pay);
    //    REJECTED by the compiler:
    //      error CS1503: Argument 1: cannot convert from
    //      'HandlingTemporalCoupling.Design.Validated' to 'HandlingTemporalCoupling.Design.Priced'
    //    Why: Charge needs a Priced, and the only normal way to get one is ApplyDiscount.
    //    Still possible in another form: new Priced(validated.Amount) compiles, and
    //    ApplyDiscount(validated, 0m) is a "discount" of zero (the types cannot judge the percent).
    //
    // 3) Charge called twice
    //      var r1 = Charge(priced, DemoGateway.Pay);
    //      var r2 = Charge(priced, DemoGateway.Pay);
    //    NOT rejected: it compiles and runs (see the runtime demonstration above: 2 gateway calls).
    //    Why: a Priced is an ordinary value; using it does not "use it up". Only compile-time
    //    rejection exists for the chained form:
    //      var r3 = Charge(r1, DemoGateway.Pay);   // r1 is a Receipt
    //      error CS1503: Argument 1: cannot convert from
    //      'HandlingTemporalCoupling.Design.Receipt' to 'HandlingTemporalCoupling.Design.Priced'
    //    So this mistake REMAINS POSSIBLE; preventing it needs extra design (idempotency key,
    //    tracking in the gateway/database, or a linear/one-shot token), not just stage types.
    //
    // 4) Charge before Validate
    //      var draft   = Start(1000m);
    //      var receipt = Charge(draft, DemoGateway.Pay);
    //    REJECTED by the compiler:
    //      error CS1503: Argument 1: cannot convert from
    //      'HandlingTemporalCoupling.Design.Draft' to 'HandlingTemporalCoupling.Design.Priced'
    //    Why: Charge needs a Priced; a Draft has not been through Validate or ApplyDiscount.
    //    The old runtime check (_validated) is no longer needed. Still possible in another
    //    form: new Priced(-50m) compiles, so an invalid amount can reach Charge if someone
    //    bypasses Validate by calling the constructor directly.
    // ==================================================================
}
