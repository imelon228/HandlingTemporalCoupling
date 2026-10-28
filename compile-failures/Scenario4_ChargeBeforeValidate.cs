using HandlingTemporalCoupling.Design;
using static HandlingTemporalCoupling.Design.CheckoutFlow;

static class Scenario4
{
    static void Run()
    {
        var draft   = Start(1000m);
        var receipt = Charge(draft, DemoGateway.Pay);   // wrong: Draft where Priced is required
    }
}
