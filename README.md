# Handling Temporal Coupling — C# Solution and Defense Guide

## 1. Assignment overview

An online shop processes an order in four steps: **Start → Validate → ApplyDiscount → Charge**. The steps only work in one correct order, but the original class `Checkout` does not enforce that order. The assignment asks me to:

1. Show how the original mutable class breaks (three wrong sequences that compile, plus Charge-before-Validate in `try/catch`).
2. Redesign it with immutable records `Draft`, `Validated`, `Priced`, `Receipt` and functions `Validate`, `ApplyDiscount`, `Charge`.
3. Make `Charge` the only step with a side effect by giving it a `Func<decimal, string>` gateway parameter (one demo gateway, one fake gateway).
4. Put the wrong sequences into the new code as comments and say whether the compiler rejects them, and whether any are still possible.
5. Answer four conceptual questions (section 10).

**Why temporal coupling is a problem:** the correctness of the program depends on *when* and *in what order* methods are called, but nothing checks this at compile time. In a payment process, an order mistake can mean a wrong amount or a double charge, and the program keeps running as if nothing happened.

## 2. Student introduction

I am a second-year Computer Science student in Kazakhstan. I am developing my programming skills and learning to design, implement, test, and explain software solutions. My broader career goal is to become a Fullstack Developer. This assignment is part of my programming education, and I wrote this README to understand the code well enough to explain and defend it.

## 3. Requirements and technology stack

- **Language / platform:** C# on .NET 8 (SDK 8.0.132, installed with `apt` in the environment where I built and verified it; target framework `net8.0`).
- **Immutable records** (`sealed record`) for the four stages.
- **Delegates:** `Func<decimal, string>` for the payment gateway.
- **Testing:** no framework and no NuGet packages. A small built-in `SelfChecks` class runs 16 checks and returns a non-zero exit code if any fails.
- No external APIs, banks, or databases.

## 4. Project structure

```
HandlingTemporalCoupling/
├── HandlingTemporalCoupling.sln
├── README.md
├── src/HandlingTemporalCoupling/
│   ├── HandlingTemporalCoupling.csproj
│   ├── Program.cs           entry point: runs Task 1, Tasks 2-4, then the self-checks
│   ├── LegacyCheckout.cs    the ORIGINAL mutable Checkout class (unchanged)
│   ├── LegacyDemo.cs        Task 1: the four wrong call sequences
│   ├── Stages.cs            Task 2: Draft, Validated, Priced, Receipt records
│   ├── CheckoutFlow.cs      Start, Validate, ApplyDiscount, Charge (+ Money formatting)
│   ├── Gateways.cs          Task 3: DemoGateway and FakeGateway
│   ├── NewDesignDemo.cs     Tasks 2-4 demo; the Task 4 comments are at the bottom
│   └── SelfChecks.cs        built-in assertions
└── compile-failures/        NOT in the solution; building it is EXPECTED to fail
    ├── CompileFailures.csproj
    └── Scenario1..4_*.cs    the wrong sequences written for real, to capture compiler errors
```

The legacy code lives in namespace `HandlingTemporalCoupling.Legacy` and the new design in `HandlingTemporalCoupling.Design`, so the two never interfere. The invalid statements are only in `compile-failures/`, which is outside the solution and outside `src/`, so the normal build stays green.

## 5. How to build and run

Prerequisite: .NET 8 SDK (`dotnet --version` should print 8.x). Working directory: the folder that contains `HandlingTemporalCoupling.sln`.

```bash
dotnet build HandlingTemporalCoupling.sln
dotnet run --project src/HandlingTemporalCoupling
```

To see the real compiler errors for Task 4 (this build is *supposed* to fail with 4 errors):

```bash
dotnet build compile-failures/CompileFailures.csproj
```

> Note: in my sandbox, nuget.org was blocked, so I restored with an empty local source
> (`dotnet restore <project> --source <empty-folder>`, then `dotnet build --no-restore`). The project uses no packages, so on a normal machine the plain commands above should work; I could not test that on a machine with internet access.

## 6. Task 1 results — the original mutable design

The original class (unchanged):

```csharp
public class Checkout
{
    private decimal _total;
    private bool _validated;
    public void Start(decimal amount)          { _total = amount; }
    public void Validate()                     { _validated = _total > 0; }
    public void ApplyDiscount(decimal percent) { _total -= _total * percent; }
    public void Charge() { if (!_validated) throw new InvalidOperationException("Not validated.");
                           Console.WriteLine($"Charged {_total:F2}"); }
}
```

**Why all four sequences compile:** every method is `public`, takes only simple arguments, and returns `void`. From the compiler's point of view `c.Charge()` is valid at any moment, because the object is always of the same type `Checkout`. The "state" (`_total`, `_validated`) lives in hidden fields that the type system does not look at.

Observed output of `dotnet run` (actually executed):

| # | Call sequence | Observed output | Detected? |
|---|---|---|---|
| 1 Discount twice | `Start(1000m); Validate(); ApplyDiscount(0.10m); ApplyDiscount(0.10m); Charge();` | `Charged 810.00` | **Silent** wrong result (should be 900.00). Nothing notices. |
| 2 Discount forgotten | `Start(1000m); Validate(); Charge();` | `Charged 1000.00` | **Silent** wrong result (customer loses the discount). |
| 3 Charge twice | `Start(1000m); Validate(); ApplyDiscount(0.10m); Charge(); Charge();` | `Charged 900.00` printed **twice** | **Silent** double charge. In real life the money leaves twice. |
| 4 Charge before Validate | `Start(1000m); Charge();` inside `try/catch` | `Exception caught: InvalidOperationException: Not validated.` | **Detected, but only at runtime** (an exception), after the program already started running. |

**Silent vs. detected:** only scenario 4 is noticed, and only because of the single `if (!_validated)` check. The other three produce a believable-looking result with no error at all, which is the most dangerous kind of bug.

## 7. Task 2 implementation — immutable stages

```csharp
public sealed record Draft(decimal Amount);
public sealed record Validated(decimal Amount);
public sealed record Priced(decimal Amount);
public sealed record Receipt(string TransactionId, decimal Amount);
```

Positional records generate **init-only** properties, so a stage cannot be changed after creation. Transitions (in `CheckoutFlow.cs`):

| Function | Signature | Behaviour |
|---|---|---|
| `Start` | `decimal → Draft` | factory function, creates the first stage |
| `Validate` | `Draft → Validated` | throws `ArgumentOutOfRangeException` if amount ≤ 0 (the assignment allows throwing) |
| `ApplyDiscount` | `(Validated, decimal percent) → Priced` | `amount − amount × percent`, rounded to cents |
| `Charge` | `(Priced, Func<decimal,string>) → Receipt` | calls the gateway, builds the `Receipt` |

Normal use reads in the intended order, and each result is the *argument* of the next step:

```csharp
Priced priced = ApplyDiscount(Validate(Start(1000m)), 0.10m);   // Amount = 900.00
```

Design decisions (kept conservative): the discount is a fraction (`0.10m` = 10 %); I round to 2 decimals (`MidpointRounding.AwayFromZero`) so the result is exactly `900.00m`; money is printed with `ToString("F2", CultureInfo.InvariantCulture)` so the output always uses a dot. I did **not** add validation of the discount percent because the assignment does not ask for it.

## 8. Task 3 implementation — injectable gateway

`Charge` never calls a bank. It receives the *action to perform* as a parameter:

```csharp
public static Receipt Charge(Priced priced, Func<decimal, string> gateway)
{
    string transactionId = gateway(priced.Amount);
    return new Receipt(transactionId, priced.Amount);
}
```

- **Demo ("real") gateway** — `DemoGateway.Pay`: prints `[DemoGateway] Processing payment of 900.00 ...` and returns `DEMO-` plus 8 random hex characters. No network or bank.
- **Fake gateway** — `FakeGateway.Pay`: stores each received amount, so `CallCount` and `ReceivedAmounts` can be checked, and returns the predictable id `FAKE-TX-<call number>`.

The same `Charge` is used with both (`Charge(priced, DemoGateway.Pay)` and `Charge(priced, fake.Pay)`). Observed output:

```
[DemoGateway] Processing payment of 900.00 ...
Receipt (demo gateway): id=DEMO-27276260, amount=900.00     (the id is random on each run)
Receipt (fake gateway): id=FAKE-TX-1, amount=900.00
Fake gateway: calls=1, amount received=900.00
```

Self-checks confirm: one normal `Charge` → exactly one gateway call, the gateway received `900.00`, and the receipt equals `Receipt("FAKE-TX-1", 900.00m)`.

**Pure vs. effect:** `Start`, `Validate`, `ApplyDiscount` and the records are *pure*: the result depends only on the inputs and nothing outside is changed (`Validate` may throw, but it throws the same way for the same input). `Charge` itself only calls the delegate and builds a record, so the single effect — "the money moves" — is contained in the gateway that the caller chooses.

## 9. Task 4 compiler analysis

The invalid statements are in `compile-failures/` and as comments at the bottom of `NewDesignDemo.cs`. The errors below are the **real output** of `dotnet build compile-failures/CompileFailures.csproj` (SDK 8.0.132): exactly 4 errors, all `CS1503`. Namespace prefixes shortened here to `Design.`.

| # | Wrong sequence | Compiler result | Why |
|---|---|---|---|
| 1 Discount twice | `ApplyDiscount(priced, 0.10m)` where `priced` is a `Priced` | **Rejected.** `error CS1503: Argument 1: cannot convert from 'Design.Priced' to 'Design.Validated'` | `ApplyDiscount` accepts only a `Validated`. After the first discount you hold a `Priced`, which is a different type. |
| 2 Discount forgotten | `Charge(validated, DemoGateway.Pay)` | **Rejected.** `error CS1503: Argument 1: cannot convert from 'Design.Validated' to 'Design.Priced'` | `Charge` needs a `Priced`, and the normal way to obtain one is `ApplyDiscount`. |
| 3 Charge twice | `Charge(priced, gw); Charge(priced, gw);` | **NOT rejected** (compiled without error; runtime demo shows 2 gateway calls). Only the *chained* form `Charge(receipt, gw)` is rejected: `error CS1503: Argument 1: cannot convert from 'Design.Receipt' to 'Design.Priced'` | A `Priced` is an ordinary value; using it does not consume it. |
| 4 Charge before Validate | `Charge(draft, DemoGateway.Pay)` | **Rejected.** `error CS1503: Argument 1: cannot convert from 'Design.Draft' to 'Design.Priced'` | A `Draft` never passed `Validate` or `ApplyDiscount`. The old runtime flag `_validated` is not needed any more. |

**Is any mistake still possible?** Yes — summary of each original mistake:

| Original mistake | Status in the new design |
|---|---|
| Discount twice | Prevented at compile time for the normal path. Still possible if someone writes `ApplyDiscount(new Validated(priced.Amount), 0.10m)` on purpose. |
| Discount forgotten | Prevented at compile time for the normal path. Still possible via `new Priced(validated.Amount)` or a discount of `0m` (the types cannot judge the percent). |
| Charge twice | **Not prevented** — compile time or runtime. Possible in the same form. |
| Charge before Validate | Prevented at compile time for the normal path. Still possible via `new Priced(-50m)`, which skips `Validate` entirely. |

**Honest limitations:**

- The records have **public constructors**, so anyone can create a `Validated` or `Priced` directly (demonstrated at runtime with `new Priced(1m)`). The types make the *right* order easy and the *accidental* wrong order impossible, but they do not stop deliberate bypassing. Making constructors `internal`/`private` and exposing only the functions would reduce this; I kept the simple positional records the assignment describes.
- A valid `Priced` can be charged any number of times, because nothing tracks that it was used.
- Compile-time guarantees: wrong *types* at each step. Runtime behaviour: `Validate` rejecting amounts ≤ 0, and everything about values (percent, repeated calls).

## 10. Answers to the four assignment questions

**1. What is temporal coupling? Why is the runtime check inside `Charge` not enough?**
Temporal coupling means that operations must be called in a specific order (or at a specific time) for the program to be correct, but this requirement is invisible in the code's types. In `Checkout`, `Start → Validate → ApplyDiscount → Charge` is the only correct order, yet the compiler accepts any order. The check `if (!_validated)` in `Charge` is not enough because (a) it covers only *one* mistake: scenarios 1–3 (double discount, forgotten discount, double charge) all pass through it silently and print wrong amounts; (b) it works only at runtime, so the error appears when the code runs (maybe in production, with real money) and not while writing it; (c) it needs hidden mutable state (`_validated`) that has to be kept in sync by hand.

**2. How do the stage types enforce the order? Name a mistake the types still cannot prevent.**
Each function accepts only the type produced by the previous step: `Validate(Draft)`, `ApplyDiscount(Validated, …)`, `Charge(Priced, …)`. Because the only way to get a `Priced` is (normally) `ApplyDiscount(Validate(Start(…)))`, the compiler rejects skipped or repeated steps with `CS1503` (section 9). What the types cannot prevent: **(i) who can create a `Priced`** — with a public constructor, `new Priced(-50m)` skips validation and discount; **(ii) how often `Charge` can be called** — the same `Priced` can be charged twice, which my runtime demo confirms (2 gateway calls). The "charge twice" mistake from Task 1 is therefore still possible.

**3. Why does passing the gateway as a delegate make `Charge` easier to test? Which parts have no side effects?**
`Charge` depends on an *abstraction of the effect* (`Func<decimal,string>`), not on a concrete bank. A test passes `fake.Pay`, which records the call count and the amount and returns a known id, so we can assert "called once with 900.00 and the receipt is `FAKE-TX-1`" without printing, networking, or moving money. In production the caller passes the real gateway; `Charge` does not change. This is dependency injection using a plain delegate (no interface or framework needed). Side-effect-free parts: the four records, `Start`, `Validate`, `ApplyDiscount`, and `Money` formatting. `Charge` is pure apart from calling the injected gateway, so the only effect is in the gateway itself.

**4. Why is it natural that every stage is a new immutable object? What does it cost, and what does it allow?**
Each stage is a *different kind of fact* ("a draft", "a validated amount", "a priced amount", "a receipt"), so representing it as a new value of a new type matches the process: `ApplyDiscount` returns a new `Priced` and leaves the `Validated` it received unchanged. **Cost:** a new object is allocated for each step, and the code has to pass values along instead of mutating one object (a few more small allocations; negligible here, but it can matter in huge loops). **What it allows:** (a) the type tells which stage we are in, which is what lets the compiler enforce the order; (b) no hidden state, so no flags like `_validated` that can get out of sync; (c) earlier stages stay valid and can be inspected or reused (e.g. `validated` can be priced with two different discounts without one affecting the other); (d) easy testing and reasoning, and safe sharing between threads.

## 11. Testing and verification results

Commands actually executed (SDK 8.0.132, Ubuntu 24.04 sandbox):

| Check | Result |
|---|---|
| `dotnet build HandlingTemporalCoupling.sln` | Build succeeded, 0 warnings, 0 errors |
| `dotnet run --project src/HandlingTemporalCoupling` | Ran, exit code 0; all Task 1 outputs as in section 6 |
| Built-in `SelfChecks` | **16 passed, 0 failed** (4 legacy behaviours, stage transitions and 900.00, validation rejects 0 and −5, fake gateway: 1 call / 900.00 / `FAKE-TX-1`, demo gateway works with the same `Charge`, double charge possible) |
| `dotnet build compile-failures/CompileFailures.csproj` | Failed as expected with 4 × `CS1503` (section 9); the `Charge(priced); Charge(priced);` lines compiled without error |
| ZIP | Extracted into a clean folder; the solution rebuilt (0 warnings, 0 errors) and re-ran with exit code 0 and 16/16 checks passing; the archive contains no `bin/` or `obj/` folders |

Limitations: I only tested on .NET 8 / Linux. Because nuget.org was blocked in my sandbox I restored with an empty local package source (section 5). I did not test on Windows/macOS or with an IDE.

## 12. Defense preparation

**Short oral explanation (about 1 minute).**
"The original `Checkout` class has temporal coupling: its four methods must be called in a certain order, but the compiler doesn't know that. I showed four wrong sequences. Three of them — discount twice, no discount, charging twice — compile and silently give a wrong result (810.00, 1000.00, or a double charge). Only Charge-before-Validate is noticed, and only at runtime through an exception. In the redesign each step is a function from one immutable record to the next: `Draft → Validated → Priced → Receipt`. Because `Charge` needs a `Priced`, the compiler rejects skipped steps and double discounts with error CS1503. `Charge` receives the payment as a `Func<decimal,string>`, so I can test it with a fake gateway that counts calls. The design is not perfect: someone can call `new Priced(...)` directly, and the same `Priced` can be charged twice."

**Likely questions and suggested answers**

- *Why is `Validate` allowed to throw?* The assignment permits it; throwing is simple and honest for invalid input. A better way (e.g. returning a result type instead of an exception) is the topic of the next assignment.
- *Why records and not classes?* Records give immutability-friendly init-only properties, value equality (I compare `receipt == new Receipt(...)` in the checks) and short syntax. `sealed` prevents subclasses from breaking the guarantees.
- *Does the compiler really prevent every wrong order?* No. It prevents wrong *types* in the normal flow. It cannot stop `new Priced(...)`, a zero discount, or charging the same `Priced` twice.
- *How would you prevent double charging?* The types alone cannot. Options: an idempotency key / transaction id checked by the gateway or database, marking the order as charged in storage, or making `Charge` return a consumed token. Each needs extra design beyond this assignment.
- *How would you make `new Priced(...)` harder?* Make the constructors `internal` or `private` and expose only the factory/transition functions, so a `Priced` can only come from `ApplyDiscount`.
- *Is `Charge` a pure function?* Not quite: it is pure except for calling the gateway it was given. All the real side effect is in the gateway, which is what makes it testable.
- *Why `Func<decimal,string>` and not an interface?* For one operation a delegate is lighter; an interface becomes useful if the gateway grows several methods.
- *Why did you round the discount?* So that money has two decimals and `1000m` with `0.10m` gives exactly `900.00m`; it is a small decision I documented, not required by the assignment.
- *Why does the legacy demo compile?* All methods are public, and the compiler only checks method names and argument types, not the object's hidden state.

## 13. Key takeaways

- **Temporal coupling** hides rules about order inside mutable state; mistakes can be silent and costly.
- **Type safety:** encoding each stage as its own type moves the order rule from runtime to compile time (`CS1503`). It protects against accidents, not against deliberate bypassing, and it cannot count how often a value is used.
- **Immutability:** every step returns a new value, so there is no hidden state and earlier stages stay valid; the price is a few extra small objects.
- **Pure functions:** `Start`, `Validate`, `ApplyDiscount` and the records are easy to test and reason about; the side effect is pushed to the edge.
- **Dependency injection through delegates:** passing `Func<decimal,string>` lets the same `Charge` run with a demo or a fake gateway, so tests can check call count, amount, and receipt without any real payment.
