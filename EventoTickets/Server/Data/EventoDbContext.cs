using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EventoTickets.Shared;

namespace EventoTickets.Server.Data
{
    public class EventoDbContext : DbContext
    {
        public DbSet<Evento> Eventos { get; set; }
        public DbSet<Talao> Taloes { get; set; }
        public DbSet<Ticket> Tickets { get; set; }

        public EventoDbContext (DbContextOptions<EventoDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Talões
            modelBuilder.Entity<Talao>()
                .HasOne(e => e.Evento)
                .WithMany(t => t.Taloes)
                .HasForeignKey(t => t.EventoId)
                .IsRequired();

            modelBuilder.Entity<Talao>()
                .HasIndex(t => new { t.EventoId, t.NumeroTalao })
                .IsUnique();
            #endregion Talões

            #region Tickets
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Evento)
                .WithMany(t => t.Tickets)
                .HasForeignKey(t => t.EventoId)
                .IsRequired();

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Talao)
                .WithMany(t => t.Tickets)
                .HasForeignKey(t => t.TalaoId)
                .IsRequired();

            modelBuilder.Entity<Ticket>()
                .HasIndex(t => new { t.EventoId, t.NumeroTicket })
                .IsUnique();

            modelBuilder.Entity<Ticket>()
                .HasIndex(t => t.EventoId);

            modelBuilder.Entity<Ticket>()
                .HasIndex(t => t.Status);
            #endregion Tickets
        }

    }
}
