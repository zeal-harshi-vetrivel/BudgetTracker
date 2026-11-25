namespace BudgetTracker.Application.Data;

/// <summary>
///   Writes everything that was staged during this request to the database, in one go.
/// </summary>

// Application only knows "save the staged changes". The Data project decides how (EF Core's SaveChangesAsync).
// same name as Chris's IDataStore.
public interface IDataStore
{
    Task SaveChanges(CancellationToken cancellationToken = default);
}
