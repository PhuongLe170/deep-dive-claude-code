using ExpenseTracker.Api.Features.Companies;
using ExpenseTracker.Api.Features.Expenses;
using ExpenseTracker.Api.Features.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Data;

public class ExpenseDbContext(DbContextOptions<ExpenseDbContext> options) : DbContext(options)
{
    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<MonthlyBudget> MonthlyBudgets => Set<MonthlyBudget>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<Company> Companies => Set<Company>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount)
                .HasPrecision(18, 2);

            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Note)
                .HasMaxLength(1000);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<MonthlyBudget>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.Property(b => b.Currency)
                .IsRequired()
                .HasMaxLength(3);

            entity.HasIndex(b => b.Currency)
                .IsUnique();

            entity.Property(b => b.Amount)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Vendor)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(s => s.Amount)
                .HasPrecision(18, 2);

            entity.Property(s => s.Currency)
                .IsRequired()
                .HasMaxLength(3);

            entity.Property(s => s.BillingCycle)
                .HasConversion<string>()
                .HasMaxLength(16);

            // The default is set in the database, not just in code, so the column the
            // migration adds backfills rows that existed before Status did.
            entity.Property(s => s.Status)
                .HasConversion<string>()
                .HasMaxLength(16)
                .HasDefaultValue(SubscriptionStatus.Active);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(c => c.Website)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(c => c.CreatedAt)
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd();
        });
    }
}
