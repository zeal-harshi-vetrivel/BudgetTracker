using BudgetTracker.Data.Entities;    // BudgetEntity, ExpenseEntity
using Microsoft.EntityFrameworkCore;  // DbContext, DbSet, ModelBuilder

namespace BudgetTracker.Data;

/// <summary>
///   EF Core's view of the database: which tables exist and how they connect.
/// </summary>

// internal: only the Data project uses this directly.
// (options) is filled in at startup with "use SQLite, and here's the file". We set that up in Step 7.
internal sealed class BudgetTrackerContext(DbContextOptions<BudgetTrackerContext> options)
    : DbContext(options)
{
    // each DbSet is one table we can query or add to.
    public DbSet<BudgetEntity> Budgets => Set<BudgetEntity>();

    public DbSet<ExpenseEntity> Expenses => Set<ExpenseEntity>();

    // runs once when EF builds its picture of the database. This is where table rules go.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BudgetEntity>(entity =>
        {
            entity.ToTable("Budgets");

            // our key is BudgetId (EF wouldn't guess that on its own, so we tell it).
            entity.HasKey(e => e.BudgetId);

            // name can't be null, and we cap its length.
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();

            // one budget has many expenses. Each expense points back with BudgetId.
            // Cascade = deleting a budget also deletes its expenses.
            entity.HasMany(e => e.Expenses)
                .WithOne()
                .HasForeignKey(e => e.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExpenseEntity>(entity =>
        {
            entity.ToTable("Expenses");
            entity.HasKey(e => e.ExpenseId);
            entity.Property(e => e.Description).HasMaxLength(200).IsRequired();
        });
    }
}