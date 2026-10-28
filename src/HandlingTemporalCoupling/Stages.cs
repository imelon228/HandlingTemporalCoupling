namespace HandlingTemporalCoupling.Design;

// Task 2: one immutable record per stage of the checkout process.
// Positional records give each parameter a public, init-only property,
// so a stage cannot be modified after it is created.

/// <summary>Initial stage: an amount that has not been checked yet.</summary>
public sealed record Draft(decimal Amount);

/// <summary>An amount that passed validation (greater than zero).</summary>
public sealed record Validated(decimal Amount);

/// <summary>A validated amount after the discount was applied.</summary>
public sealed record Priced(decimal Amount);

/// <summary>The result of a successful payment.</summary>
public sealed record Receipt(string TransactionId, decimal Amount);
