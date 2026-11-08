using ControlDeDinero.Domain;
using ControlDeDinero.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ControlDeDinero.Infrastructure.Persistence;

/// <summary>
/// Main EF Core entry point for the database. It also holds the Identity tables (users, roles...),
/// which is why it inherits from IdentityDbContext instead of DbContext.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
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

        // Identity's tables get the same lowercase snake_case naming as the rest.
        modelBuilder.Entity<ApplicationUser>().ToTable("users");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("clients");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
            entity.Property(c => c.Phone).HasMaxLength(30);
            entity.Property(c => c.Notes).HasMaxLength(500);

            entity.HasIndex(c => c.Name); // search by name; NOT unique, two people can share a name

            // The owner is a user account. Client has no navigation property to the user on purpose:
            // Domain does not know about Identity.
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(c => c.OwnerId);

            entity.HasIndex(c => c.OwnerId);
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
            entity.Property(p => p.VoidReason).HasMaxLength(500);

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
