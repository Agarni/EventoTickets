using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventoTickets.Shared
{
    public class Evento
    {
        [Key]
        [StringLength(50)]
        [Column(TypeName = "VARCHAR(50)")]
        public string EventoId { get; set; }
        [StringLength(100)]
        [Column(TypeName = "VARCHAR(50)")]
        public string NomeEvento { get; set; }
        [StringLength(200)]
        [Column(TypeName = "VARCHAR(50)")]
        public string Descricao { get; set; }
        [Column(TypeName = "DATETIME")]
        public DateTime DataRealizacao { get; set; }
        public bool EventoPadrao { get; set; }

        public virtual ICollection<Talao> Taloes { get; set; }
        public virtual ICollection<Ticket> Tickets { get; set; }
    }
}
