using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventoTickets.Shared
{
    public class Ticket
    {
        [Key]
        public int TicketId { get; set; }
        [StringLength(50)]
        [Column(TypeName = "VARCHAR(50)")]
        public string EventoId { get; set; }
        [StringLength(50)]
        [Column(TypeName = "VARCHAR(50)")]
        public string TalaoId { get; set; }
        public int NumeroTicket { get; set; }
        public StatusTicket Status { get; set; }
        [Column(TypeName = "DATETIME")]
        public DateTime? DataConfirmacao { get; set; }

        public Evento Evento { get; set; }
        public Talao Talao { get; set; }
    }
}
