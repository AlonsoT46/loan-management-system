using ControlDeDinero.Domain;
using Microsoft.EntityFrameworkCore;

namespace ControlDeDinero.Infrastructure.Persistence;

/// <summary>Main EF Core entry point for the database.</summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<Installment> Installments => Set<Installment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Every decimal is money unless a property says otherwise (e.g. InterestRatePerQuincena).
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);

        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("clients");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
            entity.Property(c => c.Phone).HasMaxLength(30);
            entity.Property(c => c.Notes).HasMaxLength(500);

            entity.HasIndex(c => c.Name); // search by name; NOT unique, two people can share a name
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.ToTable("loans");
            entity.HasKey(l => l.Id);

            entity.HasOne(l => l.Client)
                .WithMany(c => c.Loans)
                .HasForeignKey(l => l.ClientId);

            entity.Property(l => l.InterestRatePerQuincena).HasPrecision(9, 4);

            entity.Property(l => l.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(l => l.Notes).HasMaxLength(500);

            entity.HasIndex(l => l.ClientId);
            entity.HasIndex(l => l.LoanDate);
        });

        modelBuilder.Entity<Installment>(entity =>
        {
            entity.ToTable("installments");
            entity.HasKey(i => i.Id);

            entity.HasOne(i => i.Loan)
                .WithMany(l => l.Installments)
                .HasForeignKey(i => i.LoanId);

            entity.Property(i => i.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasIndex(i => new { i.LoanId, i.Number }).IsUnique();
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(p => p.Id);

            entity.HasOne(p => p.Loan)
                .WithMany(l => l.Payments)
                .HasForeignKey(p => p.LoanId);

            entity.Property(p => p.Notes).HasMaxLength(500);

            entity.HasIndex(p => new { p.LoanId, p.PaymentDate });
        });

        modelBuilder.Entity<PaymentAllocation>(entity =>
        {
            entity.ToTable("payment_allocations");
            entity.HasKey(a => a.Id);

            entity.HasOne(a => a.Payment)
                .WithMany(p => p.Allocations)
                .HasForeignKey(a => a.PaymentId);

            entity.HasOne(a => a.Loan)
                .WithMany()
                .HasForeignKey(a => a.LoanId);
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("audit_events");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Loan)
                .WithMany(l => l.AuditEvents)
                .HasForeignKey(e => e.LoanId);

            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
        });

        // Financial records must never disappear because a parent row was deleted.
        foreach (var foreignKey in modelBuilder.Model
                     .GetEntityTypes()
                     .SelectMany(entity => entity.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
