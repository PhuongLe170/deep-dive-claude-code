using ExpenseTracker.Api.Features.Expenses;
using ExpenseTracker.Api.Features.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Data;

public class ExpenseDbContext(DbContextOptions<ExpenseDbContext> options) : DbContext(options)
{
    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

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

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd();
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
        });
    }
}
