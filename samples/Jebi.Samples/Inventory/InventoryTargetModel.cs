using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Jebi.Samples.Inventory
{
    // -----------------------------
    //  ProductDef  ←  products[]
    // -----------------------------
    [Table("Products")]
    public class Product
    {
        [Key]
        [Column("Id")]                          // PK (SQLite: TEXT, Guid)
        [JsonPropertyName("id_product")]
        public Guid Id { get; set; }

        [Required]
        [Column("Name")]                        // NOT NULL
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        // FK esplicita (no shadow) → Categories(Id)
        [Required]
        [Column("CategoryId")]                  // NOT NULL
        [JsonPropertyName("category_id")]
        public Guid CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; } // Navigazione

        // FK esplicita (no shadow) → Warehouses(Id)
        [Required]
        [Column("WarehouseId")]                 // NOT NULL
        [JsonPropertyName("warehouse_id")]
        public Guid WarehouseId { get; set; }

        [ForeignKey(nameof(WarehouseId))]
        public Warehouse? Warehouse { get; set; } // Navigazione
    }

    // -----------------------------
    //  CategoryDef  ←  categories[]
    // -----------------------------
    [Table("Categories")]
    public class Category
    {
        [Key]
        [Column("Id")]                          // PK (SQLite: TEXT, Guid)
        [JsonPropertyName("id_category")]
        public Guid Id { get; set; }

        [Required]
        [Column("Name")]                        // NOT NULL
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        // Navigazione (non presente nel payload come nested)
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    // -----------------------------
    //  WarehouseDef  ←  warehouses[]
    // -----------------------------
    [Table("Warehouses")]
    public class Warehouse
    {
        [Key]
        [Column("Id")]                          // PK (SQLite: TEXT, Guid)
        [JsonPropertyName("id_warehouse")]
        public Guid Id { get; set; }

        [Required]
        [Column("Location")]                    // NOT NULL
        [JsonPropertyName("location")]
        public string Location { get; set; } = string.Empty;

        // Navigazione (non presente nel payload come nested)
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}