
using Microsoft.EntityFrameworkCore;
using ControlDelDinero.Domain;

namespace ControlDeDinero.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>(); // DbSet for the Client entity

    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating( // Override the OnModelCreating method to configure the model
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("clients");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(c => c.Phone)
                .HasMaxLength(30);
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.ToTable("loans");

            entity.HasKey(l => l.Id);

            entity.HasOne(l => l.Client)
                .WithMany(c => c.Loans)
                .HasForeignKey(l => l.ClientId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete to avoid deleting clients when a loan is deleted

            entity.Property(l => l.PrincipalAmount)
                .HasPrecision(18, 2);

            entity.Property(l => l.InterestRate)
                .HasPrecision(9, 4);
        });


        foreach (var foreignKey in modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict; // Set the delete behavior for all foreign keys to restrict to prevent cascading deletes
        }


    }

}

//AppDbContext va a ser la clase principal que Entity Framework va a usar para trabajar con la base de datos
