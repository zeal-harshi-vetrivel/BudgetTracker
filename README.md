# BudgetTracker API

A budget tracking API built for the Zeal API Design Bootcamp. Users create spending budgets with a limit and record expenses against them. The domain enforces the spending rules.

**Status:** Weeks 1–5 complete · Week 6 (stabilize) in progress

---

## Week 1: Choose your API ✅

### Problem description

People often set a monthly spending limit (for example, $400 for groceries) but lose track of how much they've spent until it's too late. BudgetTracker lets a user create a budget with a limit, record each expense as it happens, and always see how much they've spent and how much remains. The API enforces the rules, so a budget's totals are always accurate and invalid expenses are rejected.

### Users

- Individuals tracking personal spending
- A future front end (web or mobile app) that calls the API

### Planned operations

| Operation | Type |
|---|---|
| Create a budget with a name and spending limit | Changes state |
| Add an expense to a budget | Changes state |
| Remove an expense from a budget | Changes state |
| View a budget's spent and remaining totals | Reads |
| List a budget's expenses | Reads |

### Business rules

1. An expense amount must be greater than $0. Negative and zero amounts are rejected.
2. Expenses cannot push total spending past the budget's limit. *(Open question: block it, or allow it and flag the budget as over budget. See [Open questions](#open-questions).)*

---

## Week 2: Layered architecture ✅

| Task | Status |
|---|---|
| Create the solution and four projects | ✅ |
| Add each project to the solution | ✅ |
| Configure project references so dependencies point inward | ✅ |
| Add an `AssemblyMarker` type | ⏭️ Skipped (optional, only needed for assembly scanning later) |
| Confirm the solution builds | ✅ |

### Projects

| Project | Responsibility |
|---|---|
| `BudgetTracker.API` | HTTP endpoints, request/response models, startup |
| `BudgetTracker.Application` | Interfaces the Data layer implements (`IBudgetRepository`). Commands and queries come in Weeks 7–8. |
| `BudgetTracker.Domain` | Business concepts, rules and behavior |
| `BudgetTracker.Data` | Database access with EF Core and SQLite |

### References

```
API → Application → Domain ← Data
              ↖_____________/
```

- **API** references Application.
- **Application** references Domain.
- **Data** references Application and Domain, so it can implement Application's interfaces and save Domain objects.
- **Domain** references nothing, so the business rules don't depend on the database, UI or framework.

---

## Week 3: Domain layer (DDD) ✅

| Task | Status | Where |
|---|---|---|
| Create at least one entity or aggregate root with an Id | ✅ | `Budget` (aggregate root), `Expense` (entity) |
| Keep state private / use private setters | ✅ | All `Budget` and `Expense` properties. `Money` is immutable. |
| Methods named after business actions | ✅ | `Budget.Create`, `Budget.AddExpense` |
| At least one business invariant in the Domain layer | ✅ | No negative or zero amounts; no spending past the limit |
| Create a `DomainException` and throw it when a rule is broken | ✅ | `DomainException` base, plus `InvalidAmountException` and `BudgetExceededException` |
| Add a basic controller and endpoint that exercises the domain | ✅ | `POST /api/budgets` in `BudgetsController` (Week 4) |

### Instructor checklist (from the Week 3 session)

| Requirement | Status | Where |
|---|---|---|
| Internal constructor | ✅ | `Budget` and `Expense` |
| Factory method (like `Character.Create`) | ✅ | `Budget.Create(name, limit)` |
| Public getters, private setters | ✅ | `Budget`, `Expense` |
| At least one method that changes the object, with validation | ✅ | `Budget.AddExpense` |
| Custom domain exception | ✅ | `InvalidAmountException`, `BudgetExceededException` |
| One endpoint that calls the domain model | ✅ | `POST /api/budgets` |

### Domain structure

```
BudgetTracker.Domain/
├── Core/
│   ├── Entity.cs              Has an Id; equal by Id
│   ├── AggregateRoot.cs       Entity that owns others; all changes go through it
│   ├── ValueObject.cs         No Id; equal by value
│   └── DomainException.cs     Abstract base for every rule violation
└── Budgets/
    ├── Budget.cs              Aggregate root: limit, spent, remaining, expenses
    ├── Expense.cs             Entity: amount, description, date
    ├── Money.cs               Value object: never negative, rounded to cents
    └── Exceptions/
        ├── InvalidAmountException.cs
        └── BudgetExceededException.cs
```

### Design decisions

- **Internal constructors with no validation.** Validation lives in `Budget.Create`, so budgets saved under older rules can still be loaded from the database later (Week 5).
- **`Expenses` is read-only.** Callers can view expenses but can't add or remove them directly, so `Spent` always matches the list.
- **`Money` value object instead of `decimal`.** A negative amount can't exist anywhere in the domain.
- **Named exceptions.** Each rule throws an exception named after it, rather than a generic `ArgumentException`.
- **Classes are `sealed`.** This follows the D&D example repo, so finished classes can't be subclassed to bypass the rules.

---

## Open questions

- **Should going over budget be blocked or allowed?** It's currently blocked with `BudgetExceededException`. The alternative is to allow it, since the money was already spent, and have the budget report `IsOverBudget` and the overspent amount. This could also become a domain event (`BudgetExceeded`) in Week 9.

---

## Running the project

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```
dotnet build
dotnet run --project BudgetTracker.API
```
