# BudgetTracker API

A small budget tracking API built in .NET 10 / C# for the Zeal API Design Bootcamp. You create a budget with a spending limit and record expenses against it. The API keeps track of how much you've spent and how much is left, and it refuses anything that breaks the rules, like a negative expense or going over the limit.

This README walks through how the code works, one layer at a time. My bootcamp progress is in the [Progress](#progress) section at the bottom.

---

## What the API does

### The problem

People often set a monthly spending limit (for example, $400 for groceries) but lose track of how much they've spent until it's too late. BudgetTracker lets you create a budget, record each expense as it happens, and always see what's spent and what remains.

### Who uses it

- People tracking their own spending
- A future web or mobile app that calls the API

### The rules

1. **Amounts must be greater than $0.** Negative or zero expenses (and limits) are rejected.
2. **You can't spend past the limit.** An expense that would push the total over the limit is rejected.

### What you can do today

| Request | What it does | Success response |
|---|---|---|
| `POST /api/budgets` | Create a budget, optionally with some starting expenses | `201 Created` |
| `GET /api/budgets/{id}` | Get one budget with its totals and expenses | `200 OK` (or `404` if it doesn't exist) |
| `POST /api/budgets/{id}/expenses` | Add an expense to a saved budget | `200 OK` |

Removing an expense is planned but not built yet.

---

## The big picture: four layers

Instead of putting everything in one project, the code is split into four projects. Each one has a single job.

| Project | Its job | Think of it as |
|---|---|---|
| `BudgetTracker.API` | Receives HTTP requests and sends back responses | The front desk |
| `BudgetTracker.Application` | Holds the use cases: commands (changes), queries (reads) and event handlers. Says *what* it needs from the database without saying *how* | The coordinator |
| `BudgetTracker.Domain` | Holds the business rules | The rulebook |
| `BudgetTracker.Data` | Stores and loads things from the database | The filing cabinet |

The arrows below mean "knows about" (a project reference):

```
   BudgetTracker.API
          │
          ▼
   BudgetTracker.Application  ◄──── BudgetTracker.Data
          │                               │
          ▼                               │
   BudgetTracker.Domain  ◄────────────────┘
```

**Domain is at the bottom and knows about nothing else.** The rules don't depend on the web or the database, so either of those can change without touching the rules. (API also references Data, but only so `Program.cs` can switch the database on at startup.)

### Following one request

Here's what happens when you add a $25 expense to a saved budget:

```
You (the .http file in VS Code)
  │   POST /api/budgets/{id}/expenses   { "amount": 25.00, "description": "Trader Joe's" }
  ▼
API          BudgetsController.AddExpense turns the request into an AddExpenseCommand
  │          and sends it through MediatR
  ▼
Application  LoggingBehavior logs "Handling AddExpenseCommand"
  │          UnitOfWorkBehavior sees a command and waits for the handler to finish
  ▼
Application  AddExpenseCommandHandler:
  │            Data:   BudgetRepository.Get rebuilds the Budget from SQLite
  │            Domain: budget.AddExpense(...) checks the rules
  │                      ✗ rule broken → DomainException → 400, nothing saved, no event
  │                      ✓ rules pass  → records an ExpenseAdded domain event
  │            Data:   BudgetRepository.Save stages the change and hands over the event
  ▼
Application  UnitOfWork.Commit:
  │            1. publishes ExpenseAdded → LogExpenseAddedHandler logs it
  │            2. IDataStore.SaveChanges writes everything to SQLite in one go
  ▼
API          returns 200 OK with the updated budget as JSON
```

Reads take a different, simpler path: `GET` sends a `GetBudgetByIdQuery`, which reads straight from the database into a plain read model without touching the Domain (see [Application layer](#application-layer-use-cases)).

The rest of this README goes through each layer, starting with the Domain because everything else depends on it.

---

## Domain layer: the rules

`BudgetTracker.Domain` is plain C#: no web code, no database code. If you read only one layer, read this one.

### Three kinds of objects

| Kind | What makes it that kind | In this project |
|---|---|---|
| **Entity** | Has an Id. Two entities are the same if their Ids match. | `Expense` |
| **Aggregate root** | An entity that owns other objects. All changes go through it. | `Budget` (owns its expenses) |
| **Value object** | Has no Id. Two are the same if their values match ($20 equals $20). | `Money` |

The base classes for these live in `Core/`: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject<T>`, plus `DomainException`.

### Budget: the front door

If outside code could add an `Expense` straight into a list, the budget's `Spent` total could get out of sync with its expenses. So `Budget` guards the door:

- The expense list (`_expenses`) is private. Outside code only sees a read-only `Expenses` list.
- `Expense`'s constructor is `internal`, so the API can't write `new Expense(...)`. The only way to make one is `budget.AddExpense(...)`.
- `AddExpense` updates the list and the total together.

```csharp
public Expense AddExpense(decimal amount, string description, DateOnly date)
{
    Money expenseAmount = Money.From(amount);          // throws if negative
    if (expenseAmount == Money.Zero)
        throw new InvalidAmountException("An expense must be greater than zero.");

    Money newSpent = Spent.Add(expenseAmount);
    if (newSpent.IsGreaterThan(Limit))
        throw new BudgetExceededException(...);         // over the limit

    var expense = new Expense(Guid.NewGuid(), expenseAmount, description, date);
    _expenses.Add(expense);                             // list and total
    Spent = newSpent;                                   // change together
    return expense;
}
```

It **checks everything first and changes things after**, so a broken rule leaves the budget exactly as it was.

New budgets are made with `Budget.Create(name, limit)`, which rejects a limit of $0.

### Money: a number with rules

A plain `decimal` can be -5. A `Money` can't:

- Its constructor is private, so the only way to make one is `Money.From(amount)`, which throws on a negative amount and rounds to cents.
- It never changes. `Add` returns a *new* `Money` instead of editing the old one.

### Keeping state safe

- **Public get, private set.** Anyone can read `Name`, `Limit` or `Spent`, but only the class itself can change them.
- **Internal constructors.** Outside code has to use `Budget.Create` and `AddExpense`, where the rules are checked.
- **No rules inside constructors.** Constructors only set values. That's what lets the Data layer rebuild a saved budget without re-running the rules (see [Loading a budget](#loading-a-budget-rehydrating)).
- **`sealed` classes.** Nothing can inherit from them and sneak around the rules.

### When a rule is broken

Every rule throws a named exception that inherits from the abstract `DomainException`:

- `InvalidAmountException`: a negative or zero amount
- `BudgetExceededException`: the expense would go over the limit

The Domain doesn't know anything about HTTP. It just throws, and the API layer turns the exception into a `400 Bad Request`.

### Domain events

When something meaningful happens, the aggregate records it as a **domain event**: a past-tense fact like `ExpenseAddedDomainEvent` (budget, expense, amount, what's left).

- `Budget.AddExpense` raises it on its **last line**, after every rule has passed. A rejected expense never raises one.
- `AggregateRoot` keeps pending events in a private list. `AddDomainEvent` is `protected` (only the aggregate can add), and `PullDomainEvents` is `internal` (only Data can collect them, through `InternalsVisibleTo`).
- The event is plain C#, with no MediatR, HTTP or EF Core. The budget **announces** what happened and doesn't know who is listening. Reactions live in the Application layer.

### Files

```
BudgetTracker.Domain/
├── Core/
│   ├── Entity.cs              Has an Id; equal by Id
│   ├── AggregateRoot.cs       Entity that owns others; holds pending domain events
│   ├── ValueObject.cs         No Id; equal by value
│   ├── DomainException.cs     Abstract base for every rule violation
│   └── IDomainEvent.cs        Label for "something that happened"
└── Budgets/
    ├── Budget.cs              Aggregate root: limit, spent, remaining, expenses
    ├── Expense.cs             Entity: amount, description, date
    ├── Money.cs               Value object: never negative, rounded to cents
    ├── Events/
    │   └── ExpenseAddedDomainEvent.cs
    └── Exceptions/
        ├── InvalidAmountException.cs
        └── BudgetExceededException.cs
```

---

## Presentation layer: talking HTTP

`BudgetTracker.API` turns HTTP requests into commands and queries, sends them through MediatR, and turns the results back into HTTP responses. It holds no business rules and no business workflow of its own.

### Startup: `Program.cs`

When the app starts, `Program.cs`:

1. Turns on controllers (`AddControllers`).
2. Reads the database connection string from `appsettings.json` (`Data Source=budgettracker.db`).
3. Registers the Data layer (`AddDataServices`).
4. Registers the Application layer (`AddApplicationServices`): MediatR, the handlers and the pipeline behaviors.
5. Registers `DomainExceptionHandler` (with `AddProblemDetails`), so broken rules become `400`s in one place.
6. Creates or updates the database (`ApplyMigrations`).
7. Turns on the exception handler (`UseExceptionHandler`) and connects the controller routes to real URLs (`MapControllers`).

### Request and response models

The API never sends or receives Domain objects directly. It has its own simple classes, called *contracts*, in `Contracts/`:

- **`CreateBudgetRequest`** is what the client sends. It holds only what the client should provide, as plain numbers (`decimal`, not `Money`).
- **`BudgetResponse`** is what the client gets back. Its `From(budget)` method copies values out of the Domain object.

Keeping them separate means the Domain can change without breaking clients, and clients never see private details.

**Request** (`POST /api/budgets`):

```json
{
  "name": "Groceries",
  "limit": 400.00,
  "expenses": [
    { "amount": 52.75, "description": "Costco run", "date": "2026-10-20" },
    { "amount": 18.40, "description": "Farmers market" }
  ]
}
```

| Field | Required? | Meaning |
|---|---|---|
| `name` | Yes | Budget name |
| `limit` | Yes | Spending cap |
| `expenses` | No | Expenses to add right away (empty if left out) |
| `expenses[].amount` | Yes | How much was spent |
| `expenses[].description` | Yes | What it was for |
| `expenses[].date` | No | When it happened (today if left out) |

There's no `id`, `spent` or `remaining` in the request. The server works those out, so a client can't send fake totals.

**Response:**

```json
{
  "id": "3f2b8c1e-...",
  "name": "Groceries",
  "limit": 400.00,
  "spent": 71.15,
  "remaining": 328.85,
  "expenses": [
    { "id": "a91d...", "amount": 52.75, "description": "Costco run", "date": "2026-10-20" },
    { "id": "c47e...", "amount": 18.40, "description": "Farmers market", "date": "2026-10-27" }
  ]
}
```

The request is what the client *wants*. The response is what the Domain *accepted and worked out*.

### The controller

`BudgetsController` has one method per endpoint, and its only dependency is MediatR's `ISender`. No repository, no `DbContext`. Each method does the same three things:

1. **Translate** the request into a command (`CreateBudgetCommand`, `AddExpenseCommand`) or a query (`GetBudgetByIdQuery`).
2. **Send** it: `await sender.Send(command, cancellationToken)`.
3. **Translate** the result into a response with the right status code.

The HTTP request classes never leave the API. The controller copies them into the command's own types (like `NewExpense`), so the same command could be sent from a CLI or a background job.

| Status code | Meaning | When you'll see it |
|---|---|---|
| `200 OK` | It worked; here's the data | GET a budget, add an expense |
| `201 Created` | A new thing was made | Create a budget. The `Location` header gives the URL to GET it. |
| `400 Bad Request` | The request broke a rule or was missing something | Negative amount, over the limit, missing `name` |
| `404 Not Found` | Nothing has that Id | GET or add an expense with an unknown Id |

**How errors work:**

- **Missing required fields** (like `name`) are caught by ASP.NET's `[ApiController]` before our code even runs.
- **Broken business rules** throw a `DomainException`. `Errors/DomainExceptionHandler.cs` catches it for every endpoint in one place and returns a standard error body called `ProblemDetails` (shortened here). The controller has no try/catch.

```json
{
  "title": "BudgetExceededException",
  "status": 400,
  "detail": "An expense of $25.00 would exceed the 'Dining out' budget. Only $20.00 remains."
}
```

The save happens *after* the rules are checked, so a broken rule never reaches the database.

### Files

```
BudgetTracker.API/
├── Program.cs                          Startup
├── appsettings.json                    Database connection string
├── BudgetTracker.API.http              Test requests (see "Running it")
├── Contracts/
│   ├── Requests/CreateBudgetRequest.cs
│   └── Responses/BudgetResponse.cs     From(Budget) for commands, From(BudgetDetails) for queries
├── Controllers/BudgetsController.cs    Request → command/query → Send → response
└── Errors/DomainExceptionHandler.cs    DomainException → 400 ProblemDetails
```

---

## Application layer: use cases

`BudgetTracker.Application` holds the app's use cases. It uses **MediatR**, a small library that delivers a message (a command or query) to the one class that handles it. The controller sends; it never knows which class does the work.

### Commands: changing things (Week 7)

| Command | Handler does |
|---|---|
| `CreateBudgetCommand(Name, Limit, Expenses)` | `Budget.Create`, `AddExpense` for each expense, `repository.Save` |
| `AddExpenseCommand(BudgetId, Expense)` | `repository.Get` (null → 404), `AddExpense`, `repository.Save` |

Commands are plain records with no HTTP types. Handlers work with the full `Budget` aggregate, so every rule runs. They save through `IBudgetRepository`, an interface the Data layer fulfils:

```csharp
public interface IBudgetRepository
{
    Task<Budget?> Get(Guid id, CancellationToken cancellationToken = default);
    Task Save(Budget budget, CancellationToken cancellationToken = default);
}
```

### Queries: reading things (Week 8, CQRS)

Reading doesn't change anything, so it doesn't need the rules. `GetBudgetByIdQuery` goes down a separate, lighter path:

```
Command → Budget aggregate → rules → IBudgetRepository.Save   (change state)
Query   → IBudgetQueries   → SQL projection → BudgetDetails   (just return data)
```

- **`BudgetDetails` / `ExpenseDetails`** are the *read model*: plain records with decimals, no methods.
- **`IBudgetQueries`** is the read-side interface. It has no `Save`, so a query *can't* change anything.
- **`GetBudgetByIdQueryHandler`** only calls `IBudgetQueries`. It never touches a `Budget`.

Both sides share one SQLite database. CQRS here means separate *paths*, not separate databases.

### Pipeline behaviors: code that wraps every request

```
sender.Send(...) → LoggingBehavior → UnitOfWorkBehavior → handler
```

- **`LoggingBehavior`** logs `Handling …` and `Handled … in N ms`, and logs a broken rule as a warning before passing the exception on.
- **`UnitOfWorkBehavior`** runs only for commands (anything that inherits `CommandBase`). After the handler finishes, it calls `IUnitOfWork.Commit` once. Queries pass straight through.

### Unit of Work: one save per command

The repository's `Save` only **stages** changes on EF Core's change tracker. Nothing is written until the command is done:

```
UnitOfWorkBehavior ─► handler runs (repository.Save stages rows, collects events)
                   ─► UnitOfWork.Commit:
                        1. publish every collected domain event (repeat until none are left)
                        2. IDataStore.SaveChanges  →  one SaveChangesAsync for the whole command
```

- **A broken rule** throws before `Commit`, so nothing is saved and no events are sent.
- **`UnitOfWorkBehaviorState.CallLevel`** tracks how many commands deep we are. If a handler ever sends another command, only the outermost one commits, so everything is saved together.
- **`IDataStore`** is defined in Application and implemented by `BudgetTrackerContext` in Data, so Application never references EF Core.

### Domain events: reacting without coupling (Week 9)

```
Budget.AddExpense ─► records ExpenseAddedDomainEvent (on its last line, after every rule passed)
BudgetRepository.Save ─► stages the rows, moves the events into IDomainEventCollector
UnitOfWork.Commit ─► publishes each event through MediatR, THEN calls SaveChanges
LogExpenseAddedHandler ─► logs "ExpenseAdded: $25.00 added to 'Groceries' (...). $303.85 left."
```

- **Events are handled inside the unit of work, before the save.** The command's changes and its reactions go to the database together: if a reaction fails, nothing is saved. This follows Chris's design from the Week 9 session (he calls these pre-transaction notifications).
- **A failed command sends no events.** For example, in request #2 the $30 expense is accepted inside the budget before the $25 one breaks the limit, but the handler throws before `Save`, so no event is collected and nothing is saved.
- **The Domain doesn't reference MediatR.** `DomainEventNotification<TEvent>` wraps the plain event so MediatR can deliver it.
- **Adding a reaction** (an alert at 90% of the limit, updating a report) means adding another handler. `Budget` doesn't change.
- **Before or after the save?** Reactions that must succeed with the change (like updating another table) belong before the save, as here. Reactions that talk to the outside world (email, a message queue) should run **after** the save or through a queue, so a slow or failed email can't undo the user's change. Chris calls those deferred notifications. BudgetTracker only needs the first kind for now.

### Files

```
BudgetTracker.Application/
├── Budgets/
│   ├── Commands/        CreateBudgetCommand(+Handler), AddExpenseCommand(+Handler), NewExpense
│   ├── Queries/         GetBudgetByIdQuery(+Handler), BudgetDetails, ExpenseDetails
│   └── EventHandlers/   LogExpenseAddedHandler
├── Commands/CommandBase.cs   Base for commands (so the unit of work knows to commit)
├── Behaviors/           LoggingBehavior, UnitOfWorkBehavior (+ UnitOfWorkBehaviorState)
├── UnitOfWorks/         IUnitOfWork, UnitOfWork (dispatch events, then save)
├── Events/              IDomainEventCollector, DomainEventNotification
├── Data/
│   ├── IDataStore.cs                        "save the staged changes"
│   ├── Repositories/IBudgetRepository.cs   write side
│   └── Queries/IBudgetQueries.cs           read side
└── Extensions/ServiceCollectionExtensions.cs   AddApplicationServices
```

---

## Data layer: saving to a database

`BudgetTracker.Data` uses **Entity Framework Core** (EF Core), a library that turns C# objects into SQL, with **SQLite**, a database that's just a single file (`budgettracker.db`).

### Table rows vs. Domain objects

The Data layer has its own simple classes shaped like table rows: `BudgetEntity` and `ExpenseEntity`. They have public getters and setters and no rules. EF Core works with these, never with the Domain classes.

Why not save `Budget` directly? Because the database shape and the business model can then change separately, and nothing about the database leaks into the Domain. The repository translates between the two.

```
Budgets                      Expenses
──────────────               ──────────────
BudgetId  (key)  ◄────┐      ExpenseId  (key)
Name                  └───── BudgetId   (which budget it belongs to)
Limit                        Amount
                             Description
                             Date
```

There's **no `Spent` column**. It's recalculated by adding up the expenses each time a budget is loaded, so it can never disagree with them.

### `BudgetTrackerContext`

This is EF Core's map of the database: which tables exist, their keys, maximum text lengths, and that one budget has many expenses. Deleting a budget also deletes its expenses ("cascade delete").

### The repository

`BudgetRepository` keeps the `IBudgetRepository` promise.

**Saving** (Domain → rows): if the budget isn't in the database yet, it adds a new row; otherwise it updates the existing one. Expenses can only be added (never edited or removed), so it only inserts expenses that aren't saved yet. It does **not** call `SaveChangesAsync`: it only stages the rows, then pulls the budget's pending domain events and hands them to `IDomainEventCollector`. The unit of work saves once at the end of the command, through `IDataStore`, which `BudgetTrackerContext` implements.

#### Loading a budget (rehydrating)

**Loading** (rows → Domain) is called *rehydrating*: turning stored data back into a living Domain object. The repository reads the budget row with its expense rows, then rebuilds a `Budget` using its `internal` constructors.

How can Data call an `internal` constructor that lives in another project? The Domain project lists Data in `InternalsVisibleTo`, so Data gets access while the API doesn't. Because constructors don't run the rules, the saved data loads as-is.

### Read queries: `BudgetQueries`

`BudgetQueries` keeps the `IBudgetQueries` promise for the read side:

- **`AsNoTracking()`** tells EF Core "I'm only reading." It skips change tracking, which is faster and means nothing loaded here can be saved by accident.
- **A `Select` projection** asks SQLite for only the columns the read model needs, and fills `ExpenseDetails` directly. No `Budget` object is built.
- **`Spent` is added up in C#** after the query, because SQLite stores decimals as text and can't sum them reliably.

### Migrations

A **migration** is a generated C# file that describes how to create or change the tables. `InitialCreate` creates the `Budgets` and `Expenses` tables.

`ApplyMigrations()` runs at startup and applies any migrations that haven't run yet. On the first run, that creates `budgettracker.db` automatically, with no extra command needed. Database files are listed in `.gitignore`, so they never get committed.

### Connecting it to the API

`AddDataServices(connectionString)` is the one public entry point into the Data project. It:

- registers `BudgetTrackerContext` and tells it to use SQLite, and
- says "whenever something asks for `IBudgetRepository`, give it a `BudgetRepository`" (and the same for `IBudgetQueries` → `BudgetQueries`). This is **dependency injection**: ASP.NET builds these classes and hands them to the handlers' constructors.

Everything else in Data is `internal`.

### Files

```
BudgetTracker.Data/
├── BudgetTrackerContext.cs
├── Entities/
│   ├── BudgetEntity.cs
│   └── ExpenseEntity.cs
├── Repositories/BudgetRepository.cs            Write side: Get/Save aggregates
├── Queries/BudgetQueries.cs                    Read side: AsNoTracking projections
├── Extensions/ServiceCollectionExtensions.cs   AddDataServices, ApplyMigrations
└── Migrations/                                 InitialCreate
```

---

## Running it

**You need:** the [.NET 10 SDK](https://dotnet.microsoft.com/download). To use the test file, you also need VS Code with the **REST Client** extension.

```
git clone https://github.com/zeal-harshi-vetrivel/BudgetTracker.git
cd BudgetTracker
dotnet build
dotnet run --project BudgetTracker.API
```

The API runs at `http://localhost:5166`. The database is created on the first run.

### Trying the requests

Open `BudgetTracker.API/BudgetTracker.API.http` and click **Send Request** above each one. **Send #1 first**, because requests 5, 7 and 8 reuse the Id it creates.

| # | Request | Expected result |
|---|---|---|
| 1 | Create "Groceries" ($400) with two expenses | `201`, spent $71.15, remaining $328.85 |
| 2 | Create "Dining out" ($50) with $30 + $25 | `400 BudgetExceededException` |
| 3 | Create a budget with a -$10 expense | `400 InvalidAmountException` |
| 4 | Create a budget with no name | `400` from ASP.NET, before our code runs |
| 5 | GET the budget from #1 | `200`, same data as #1 |
| 6 | GET an Id that doesn't exist | `404` |
| 7 | Add $25 to the budget from #1 | `200`, spent goes up by $25 |
| 8 | Add $400 to the budget from #1 | `400 BudgetExceededException` |

To confirm data really is saved, stop the app (`Ctrl+C`), start it again, and re-send #5.

### What to watch in the terminal

| # | Log lines |
|---|---|
| 1 | `Handling CreateBudgetCommand`, two `ExpenseAdded: …` lines, then the `INSERT` SQL, then `Handled CreateBudgetCommand` |
| 2, 3, 8 | `… rejected after N ms: <ExceptionName>`, with **no** `ExpenseAdded` line |
| 5, 6 | `Handling GetBudgetByIdQuery` / `Handled GetBudgetByIdQuery` (the read path) |
| 7 | `Handling AddExpenseCommand`, `ExpenseAdded: $25.00 added to 'Groceries' … $303.85 left.`, then the `INSERT`, then `Handled …` |

---

## Design choices

Where my approach differs from Chris's example repo, I've said so.

- **Overspending is blocked.** An over-limit expense returns 400, and one bad expense fails the whole create request. Kept that way so there's a clear rejected rule to show.
- **Named exceptions** for each rule, following Chris's lecture. His repo throws `DomainException` directly.
- **Create returns `201` via `CreatedAtAction`.** Chris uses `Created(url)`.
- **Responses return the whole budget,** so totals are visible when testing. Chris returns only the Id.
- **`Spent` isn't stored;** it's recalculated on load.
- **Separate row classes, manual mapping, and rehydration through internal constructors,** like Chris.
- **Migrations run at startup,** like Chris.
- **Expenses are exposed as `IReadOnlyList<Expense>`.** Chris uses snapshot records instead.
- **Unit of Work like Chris's** (`UnitOfWorkBehavior`, `UnitOfWorkBehaviorState`, `IUnitOfWork`, `IDataStore`, `CommandBase`), without his batched and deferred notifications. I added a `LoggingBehavior` as well.
- **One global exception handler** (`DomainExceptionHandler`) turns any `DomainException` into a 400. I didn't find one in Chris's repo, where a domain exception would come back as a 500.
- **Commands return the `Budget`,** so the create/add responses can show totals. Chris's command returns only the Id.
- **MediatR 12.5.0,** the last free version (Chris uses 12.3.0). Version 13+ needs a license key.
- **One read interface per aggregate** (`IBudgetQueries`), where Chris has one per query (`IListCharactersDataQuery`). My read returns a finished object, where Chris returns an `IQueryable` for OData.
- **Domain events, simplified.** A small collector and the same "dispatch, then save" order as Chris, without his batching attributes.
- **Budget names aren't validated yet.**

## Open questions

- Should going over budget be **blocked** (current) or **allowed and flagged** (`IsOverBudget` plus a `BudgetExceeded` domain event)?
- Should one bad expense fail the whole create request, or only that expense?

---

## Progress

| Week | Topic | Status |
|---|---|---|
| 1 | Choose your API | ✅ |
| 2 | Layered architecture | ✅ (optional `AssemblyMarker` skipped) |
| 3 | Domain layer | ✅ |
| 4 | Presentation layer | ✅ |
| 5 | Data layer | ✅ |
| 6 | Stabilize | ✅ |
| 7 | Commands (MediatR) | ✅ |
| 8 | Queries (CQRS) | ✅ |
| 9 | Domain events | ✅ |
| 10 | Finish and demo | 🟡 Demo Nov 25 |

### Week 6 checklist

- ✅ Migrations create the database on first run
- ✅ Create, save and load work, and data survives a restart
- ✅ Valid and invalid requests in the `.http` file
- ✅ README explains the code and how to run it
- ⬜ Fresh clone builds and runs in a new folder
- ✅ Remove the duplicated `catch (DomainException)` in `BudgetsController` (replaced by `DomainExceptionHandler` in Week 7)

### Ideas for later

- Remove an expense, and list all budgets (a second query).
- Allow overspending with a flag and a `BudgetExceeded` event.
- Deferred (after-save) notifications for reactions that reach outside systems.
- Validate budget names (`InvalidBudgetNameException`).
