using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventoTickets.Shared
{
    public class Talao
    {
        [Key]
        [StringLength(50)]
        [Column(TypeName = "VARCHAR(50)")]
        public string TalaoId { get; set; } = Guid.NewGuid().ToString();
        public string EventoId { get; set; }
        public int NumeroTalao { get; set; }
        [StringLength(50)]
        [Column(TypeName = "VARCHAR(50)")]
        public string ResponsavelTalao { get; set; }
        [Column(TypeName = "DATE")]
        public DateTime? DataEntrega { get; set; }
        public int NumeroInicial { get; set; }
        public int NumeroFinal { get; set; }

        public Evento Evento { get; set; }
        public virtual ICollection<Ticket> Tickets { get; set; }
    }
}
