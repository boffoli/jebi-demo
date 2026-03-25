// 2) InventoryTargetDbContext — un dominio “magazzino”
using Microsoft.EntityFrameworkCore;

namespace Jebi.Samples.Inventory;

public class InventoryTargetDbContext : DbContext
{
    public InventoryTargetDbContext(DbContextOptions<InventoryTargetDbContext> options)
        : base(options) { }

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Warehouse> Warehouses { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category (1-N) → Product
        modelBuilder.Entity<Category>()
            .HasMany(c => c.Products)
            .WithOne(p => p.Category!)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Warehouse (1-N) → Product
        modelBuilder.Entity<Warehouse>()
            .HasMany(w => w.Products)
            .WithOne(p => p.Warehouse!)
            .HasForeignKey(p => p.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}