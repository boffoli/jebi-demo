using Microsoft.EntityFrameworkCore;

namespace Jebi.Samples.Crm
{
    /// <summary>
    /// DbContext EF Core per mappare le entità: Azienda, Contatto, Preferenza, Utente, Nota, Ruolo.
    /// </summary>
    public class CrmTargetDbContext(DbContextOptions<CrmTargetDbContext> options) : DbContext(options)
    {
        public DbSet<Azienda> Aziende { get; set; } = null!;
        public DbSet<Contatto> Contatti { get; set; } = null!;
        public DbSet<Preferenza> Preferenze { get; set; } = null!;
        public DbSet<Utente> Utenti { get; set; } = null!;
        public DbSet<Nota> Note { get; set; } = null!;
        public DbSet<Ruolo> Ruoli { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Azienda → Contatto (1-N)
            modelBuilder.Entity<Azienda>()
                .HasMany(a => a.Contatti)
                .WithOne(c => c.Azienda!)
                .HasForeignKey(c => c.AziendaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Contatto → Preferenza (1-1)
            modelBuilder.Entity<Contatto>()
                .HasOne(c => c.Preferenza)
                .WithMany()
                .HasForeignKey(c => c.PreferenzaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Contatto → Nota (1-N)
            modelBuilder.Entity<Contatto>()
                .HasMany(c => c.Note)
                .WithOne(n => n.Contatto)
                .HasForeignKey(n => n.ContattoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Contatto → Utente (N-1, optional)
            // Explicitly declared so metadata marks Contatto.UtenteId as FK.
            modelBuilder.Entity<Contatto>()
                .HasOne(c => c.Utente)
                .WithMany()
                .HasForeignKey(c => c.UtenteId)
                .OnDelete(DeleteBehavior.SetNull);

            // Utente → Contatto (1-1)
            modelBuilder.Entity<Utente>()
                .HasOne(u => u.Contatto)
                .WithOne()
                .HasForeignKey<Utente>(u => u.ContattoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Utente → Ruolo (1-N)
            modelBuilder.Entity<Utente>()
                .HasMany(u => u.Ruoli)
                .WithOne(r => r.Utente!)
                .HasForeignKey(r => r.UtenteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
