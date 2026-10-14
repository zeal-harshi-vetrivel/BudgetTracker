using BudgetTracker.Domain.Budgets.Exceptions;   // so we can throw our named exceptions
using BudgetTracker.Domain.Core;                 // so we can inherit from AggregateRoot

namespace BudgetTracker.Domain.Budgets;

/// <summary>
///   A spending budget with a limit and a list of expenses.
///   This is the "front door": all changes to expenses go through here.
/// </summary>

// budget is an aggregate root (an entity with a Guid Id that owns other objects).
public sealed class Budget : AggregateRoot<Guid>
{
    // list of expenses. PRIVATE so no one outside can add or remove directly.
    private readonly List<Expense> _expenses = [];

    // internal constructor
    internal Budget(Guid id, string name, Money limit)
        : base(id)
    {
        Name = name;
        Limit = limit;
    }

    // public get, private set: anyone can read, only this class can change.
    public string Name { get; private set; }

    // spending limit, e.g. $400 for groceries.
    public Money Limit { get; private set; }

    // total spent so far. Starts at $0 and only changes inside AddExpense.
    public Money Spent { get; private set; } = Money.Zero;

    // how much is left. Calculated every time it's read, so it's up to date.
    // spending can never pass the limit (AddExpense blocks it), so this is never negative.
    public Money Remaining => Limit.Subtract(Spent);

    // read-only view of the expenses. Callers can look but can't call .Add() or .Remove().
    public IReadOnlyList<Expense> Expenses => _expenses.AsReadOnly();

    // only public way to create a NEW budget.
    // all the rules for a valid new budget are checked here.
    public static Budget Create(string name, decimal limit)
    {
        // Clean up the name: treat null as empty, remove extra spaces.
        name = name?.Trim() ?? string.Empty;

        // rule: limit can't be negative (Money.From checks this)...
        Money limitAmount = Money.From(limit);

        // rule: and can't be zero.
        if (limitAmount == Money.Zero)
            throw new InvalidAmountException("A budget limit must be greater than zero.");

        // All rules passed: create it with a brand-new Id.
        return new Budget(Guid.NewGuid(), name, limitAmount);
    }

    // ACTION: record an expense against this budget.
    public Expense AddExpense(decimal amount, string description, DateOnly date)
    {
        // rule: amount can't be negative (Money.From checks this)...
        Money expenseAmount = Money.From(amount);

        // rule: and can't be zero.
        if (expenseAmount == Money.Zero)
            throw new InvalidAmountException("An expense must be greater than zero.");

        // rule: spending can't go over the limit.
        // work out the new total first, and only save it if it's allowed.
        Money newSpent = Spent.Add(expenseAmount);
        if (newSpent.IsGreaterThan(Limit))
            throw new BudgetExceededException(
                $"An expense of {expenseAmount} would exceed the '{Name}' budget. Only {Remaining} remains.");

        // all rules passed: create the expense (only Budget can call Expense's constructor).
        var expense = new Expense(Guid.NewGuid(), expenseAmount, description?.Trim() ?? string.Empty, date);

        // update the list AND the total together, so they never get out of sync.
        _expenses.Add(expense);
        Spent = newSpent;

        return expense;
    }
}