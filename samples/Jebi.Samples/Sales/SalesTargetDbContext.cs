// 3) SalesTargetDbContext — un dominio “vendite”
using Microsoft.EntityFrameworkCore;

namespace Jebi.Samples.Sales;

public class SalesTargetDbContext(DbContextOptions<SalesTargetDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderLine> OrderLines { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.FullName)
             .IsRequired()
             .HasMaxLength(200);
        });

        // Order (FK esplicita CustomerId, no shadow)
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.CustomerId)
             .IsRequired();

            e.Property(x => x.PlacedAt)
             .IsRequired();
        });

        // OrderLine (FK esplicita OrderId, no shadow)
        modelBuilder.Entity<OrderLine>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.OrderId)
             .IsRequired();

            e.Property(x => x.ProductName)
             .IsRequired()
             .HasMaxLength(200);

            e.Property(x => x.Quantity)
             .IsRequired();
        });

        // Relazioni
        modelBuilder.Entity<Customer>()
            .HasMany(c => c.Orders)
            .WithOne(o => o.Customer!)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Lines)
            .WithOne(ol => ol.Order!)
            .HasForeignKey(ol => ol.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}