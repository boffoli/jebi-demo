using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Jebi.Samples.Crm
{
    // -----------------------------
    //  AziendeDef  ←  aziende[]
    // -----------------------------
    public class Azienda
    {
        [Key]
        [Column("id_azienda")]
        [JsonPropertyName("id_azienda")]
        public string IdAzienda { get; set; } = string.Empty;

        [Required]
        [Column("ragione_sociale")]
        [JsonPropertyName("ragione_sociale")]
        public string RagioneSociale { get; set; } = string.Empty;

        [JsonPropertyName("contatti")]
        public List<Contatto> Contatti { get; set; } = new();
    }

    // -----------------------------
    //  ContattiDef  ←  contatti[]
    // -----------------------------
    public class Contatto
    {
        [Key]
        [Column("id_contatto")]
        [JsonPropertyName("id_contatto")]
        public int IdContatto { get; set; }

        [Required]
        [Column("telefono")]
        [JsonPropertyName("telefono")]
        public string Telefono { get; set; } = string.Empty;

        // Preferenze (1‑to‑1 obbligatoria)
        [Required]
        [Column("id_preferenza")]
        [JsonPropertyName("id_preferenza")]
        public int PreferenzaId { get; set; }

        [ForeignKey(nameof(PreferenzaId))]
        [JsonPropertyName("preferenza")]
        public Preferenza Preferenza { get; set; } = null!;

        // Note (1‑to‑many)
        [JsonPropertyName("note")]
        public List<Nota> Note { get; set; } = new();

        // ---------------------------------
        // Relationship verso contenitore
        // ---------------------------------
        public string? AziendaId { get; set; }
        [ForeignKey(nameof(AziendaId))]
        public Azienda? Azienda { get; set; }

        public int? UtenteId { get; set; }
        [ForeignKey(nameof(UtenteId))]
        public Utente? Utente { get; set; }
    }

    // -----------------------------
    //  PreferenzeDef
    // -----------------------------
    public class Preferenza
    {
        [Key]
        [Column("id_preferenza")]
        [JsonPropertyName("id_preferenza")]
        public int IdPreferenza { get; set; }

        [Required]
        [Column("tema")]
        [JsonPropertyName("tema")]
        public string Tema { get; set; } = string.Empty;
    }

    // -----------------------------
    //  UtentiDef  ←  utenti[]
    // -----------------------------
    public class Utente
    {
        [Key]
        [Column("id_utente")]
        [JsonPropertyName("id_utente")]
        public int IdUtente { get; set; }

        [Required]
        [Column("nome")]
        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        // Contatto (1‑to‑1 obbligatorio)
        [Column("id_contatto")]
        [JsonPropertyName("id_contatto")]
        public int? ContattoId { get; set; }

        [ForeignKey(nameof(ContattoId))]
        [JsonPropertyName("contatto")]
        public Contatto? Contatto { get; set; }

        // Ruoli (1‑to‑many)
        [JsonPropertyName("ruoli")]
        public List<Ruolo> Ruoli { get; set; } = [];
    }

    // -----------------------------
    //  NoteDef
    // -----------------------------
    public class Nota
    {
        [Key]
        [Column("id_nota")]
        [JsonPropertyName("id_nota")]
        public int IdNota { get; set; }

        [Required]
        [Column("testo")]
        [JsonPropertyName("testo")]
        public string Testo { get; set; } = string.Empty;

        // FK verso Contatto
        [Required]
        [Column("id_contatto")]
        public int ContattoId { get; set; }

        [ForeignKey(nameof(ContattoId))]
        public Contatto Contatto { get; set; } = null!;
    }

    // -----------------------------
    //  RuoliDef
    // -----------------------------
    public class Ruolo
    {
        [Key]
        [Column("id_ruolo")]
        [JsonPropertyName("id_ruolo")]
        public int IdRuolo { get; set; }

        [Required]
        [Column("nome")]
        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        // FK verso Utente
        [Required]
        [Column("id_utente")]
        public int UtenteId { get; set; }

        [ForeignKey(nameof(UtenteId))]
        public Utente Utente { get; set; } = null!;
    }
}
