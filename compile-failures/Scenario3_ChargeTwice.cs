using HandlingTemporalCoupling.Design;
using static HandlingTemporalCoupling.Design.CheckoutFlow;

static class Scenario3
{
    static void Run()
    {
        var priced = ApplyDiscount(Validate(Start(1000m)), 0.10m);

        // 3a) Charging the SAME Priced twice: this COMPILES (no error expected here).
        var r1 = Charge(priced, DemoGateway.Pay);
        var r2 = Charge(priced, DemoGateway.Pay);

        // 3b) Feeding the Receipt back into Charge: rejected by the compiler.
        var r3 = Charge(r1, DemoGateway.Pay);
    }
}
