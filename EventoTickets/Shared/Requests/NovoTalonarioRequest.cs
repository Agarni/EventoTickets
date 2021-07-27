using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventoTickets.Shared.Requests
{
    public class NovoTalonarioRequest
    {
        public string EventoId { get; set; }
        public int QuantidadeTaloes { get; set; }
        public int TicketsPorTalao { get; set; }
    }
}
