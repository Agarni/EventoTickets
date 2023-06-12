using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventoTickets.Shared.Requests
{
    public class TicketRequest
    {
        public int[] TicketsIds { get; set; }
        public StatusTicket Status { get; set; }
    }
}
