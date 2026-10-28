using HandlingTemporalCoupling.Design;
using static HandlingTemporalCoupling.Design.CheckoutFlow;

static class Scenario2
{
    static void Run()
    {
        var validated = Validate(Start(1000m));
        var receipt   = Charge(validated, DemoGateway.Pay);   // wrong: Validated where Priced is required
    }
}
