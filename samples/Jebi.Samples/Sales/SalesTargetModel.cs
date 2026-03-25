using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Jebi.Samples.Sales
{
    // -----------------------------
    //  CustomerDef  ←  customers[]
    // -----------------------------
    public class Customer
    {
        [Key]
        [Column("id_customer")]
        [JsonPropertyName("id_customer")]
        public Guid Id { get; set; }

        [Required]
        [Column("fullName")]
        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;

        // Navigazione (non presente nel payload come nested)
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }

    // -----------------------------
    //  OrderDef  ←  orders[]
    // -----------------------------
    public class Order
    {
        [Key]
        [Column("id_order")]
        [JsonPropertyName("id_order")]
        public Guid Id { get; set; }

        // FK esplicita (no shadow)
        [Required]
        [Column("customer_id")]
        [JsonPropertyName("customer_id")]
        public Guid CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        [Required]
        [Column("placedAt")]
        [JsonPropertyName("placedAt")]
        public DateTime PlacedAt { get; set; }

        // Navigazione (nel payload è array root separato: orderLines[])
        public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
    }

    // -----------------------------
    //  OrderLineDef  ←  orderLines[]
    // -----------------------------
    public class OrderLine
    {
        [Key]
        [Column("id_line")]
        [JsonPropertyName("id_line")]
        public Guid Id { get; set; }

        // FK esplicita (no shadow)
        [Required]
        [Column("order_id")]
        [JsonPropertyName("order_id")]
        public Guid OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order? Order { get; set; }

        [Required]
        [Column("productName")]
        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Column("quantity")]
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }
}