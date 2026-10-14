using System.Globalization;                      // for the "en-US" dollar format
using BudgetTracker.Domain.Budgets.Exceptions;   // so we can throw InvalidAmountException
using BudgetTracker.Domain.Core;                 // so we can inherit from ValueObject

namespace BudgetTracker.Domain.Budgets;

/// <summary>
///   An amount of money in dollars. It can never be negative, and it's always rounded to cents.
/// </summary>

// Money is a value object: no Id, and $20 always equals $20.
public sealed class Money : ValueObject<Money>
{
    // ready-made $0.00 so we don't keep creating new zeros.
    public static readonly Money Zero = new(0m);

    // private constructor: no one outside this class can write "new Money(-5)".
    private Money(decimal amount) => Amount = amount;

    // dollar value. only a "get", with no "set" at all, so a money can never change
    public decimal Amount { get; }

    // the path for creating Money. This is where the rules live.
    public static Money From(decimal amount)
    {
        // rule 1: money can't be negative.
        if (amount < 0)
            throw new InvalidAmountException($"{amount} is not a valid amount. Amounts cannot be negative.");

        // always round to 2 decimal places (cents). 10.005 becomes 10.01.
        return new Money(decimal.Round(amount, 2, MidpointRounding.AwayFromZero));
    }

    // adding returns a new money. original is never changed.
    public Money Add(Money other) => new(Amount + other.Amount);

    // goes through from(), so if the result would be negative, it throws instead of creating bad Money.
    public Money Subtract(Money other) => From(Amount - other.Amount);

    // simple comparison helper. we'll use it to check if spending gone over the limit.
    public bool IsGreaterThan(Money other) => Amount > other.Amount;

    // how Money gets turned into text, e.g. in an error message: "$55.00"
    public override string ToString() => Amount.ToString("C", CultureInfo.GetCultureInfo("en-US"));

    // required by ValueObject: which values decide whether two Money objects are equal? just the Amount.
    // "yield return" hands back one item from the list at a time.
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
    }
}