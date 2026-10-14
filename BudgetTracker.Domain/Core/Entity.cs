namespace BudgetTracker.Domain.Core;

/// <summary>
///   Each object has its own identity. Two entities are equal if their Ids match,
///   even when every other property is identical. EX: Two coffee orders are not considered equal unless their Ids match, even if they are the same $ amount.
/// </summary>
public abstract class Entity<TId>(TId id)
    where TId : notnull // id is never null 
{
    public TId Id { get; } = id; // anyone can read (get) id, but only set once through the constructor
    
    // determines if they are equal based on: 
    // 1. same id 
    // 2. an entity 
    // 3. same kind (Budget vs. Expense)
    public override bool Equals(object? obj)
        => obj is Entity<TId> other && GetType() == other.GetType() && Id.Equals(other.Id);

    // determines the hash code based on the id, ensuring that equal entities have the same hash code.
    public override int GetHashCode() => Id.GetHashCode();

    // determines if two entities are equal using the == operator, which internally uses the Equals method.
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        => left is null ? right is null : left.Equals(right);

    // determines if two entities are not equal using the != operator, which internally uses the == operator.
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}