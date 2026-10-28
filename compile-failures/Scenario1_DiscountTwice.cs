using HandlingTemporalCoupling.Design;
using static HandlingTemporalCoupling.Design.CheckoutFlow;

static class Scenario1
{
    static void Run()
    {
        var validated = Validate(Start(1000m));
        var priced    = ApplyDiscount(validated, 0.10m);
        var twice     = ApplyDiscount(priced, 0.10m);   // wrong: Priced where Validated is required
    }
}
