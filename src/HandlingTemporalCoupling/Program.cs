using System.Globalization;
using HandlingTemporalCoupling;
using HandlingTemporalCoupling.Legacy;

// Use a fixed culture so "F2" always prints 900.00 (dot), whatever the PC's regional settings.
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

Console.WriteLine("=== TASK 1: temporal coupling in the original mutable Checkout ===\n");
LegacyDemo.RunAll();

Console.WriteLine("=== TASKS 2-4: redesigned checkout with immutable stages ===\n");
NewDesignDemo.Run();

Console.WriteLine("\n=== SELF-CHECKS ===");
int failures = SelfChecks.RunAll();
return failures == 0 ? 0 : 1;
