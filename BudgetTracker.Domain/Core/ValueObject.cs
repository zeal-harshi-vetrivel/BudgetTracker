namespace BudgetTracker.Domain.Core;

/// <summary>
///   A small object with no Id, EX: an amount of money ($20 = $20).
///   They are equal if their values are the same.
/// </summary>
public abstract class ValueObject<T> // T is the class that is inheriting from this one
    where T : ValueObject<T> // ensures that the other object is of the same ValueObject type (Money = Money)
{
    protected abstract IEnumerable<object?> GetEqualityComponents(); // provides the components that determine equality for this value object (Money would just be the $ amount)

    // equal if: 
    // 1. same type (T)
    // 2. all equality components match in order
    public override bool Equals(object? obj)
        => obj is T other && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    // combines values into one number 
    // equal value objects should produce the same hash code
    public override int GetHashCode()
        => GetEqualityComponents().Aggregate(0, (hash, part) => HashCode.Combine(hash, part));
    
    // determines if two value objects are equal using the == operator, which internally uses the Equals method.
    public static bool operator ==(ValueObject<T>? left, ValueObject<T>? right)
        => left is null ? right is null : left.Equals(right);
    
    // determines if two value objects are not equal using the != operator, which internally uses the == operator.
    public static bool operator !=(ValueObject<T>? left, ValueObject<T>? right) => !(left == right);
}