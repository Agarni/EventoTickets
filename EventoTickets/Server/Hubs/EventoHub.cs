using EventoTickets.Shared;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventoTickets.Server.Hubs
{
    public class EventoHub : Hub
    {
        public async Task SendMessage()
        {
            await Clients.All.SendAsync("ReceiveMessage");
        }

        public async Task AtualizarTaloesMessage(string idEvento)
        {
            await Clients.All.SendAsync("AtualizarTaloesMessage", idEvento);
        }

        public async Task AtualizarTicketMessage(Ticket ticket)
        {
            await Clients.All.SendAsync("AtualizarTicketMessage", ticket);
        }
    }
}
