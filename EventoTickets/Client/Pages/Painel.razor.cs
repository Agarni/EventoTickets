using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using EventoTickets.Client;
using EventoTickets.Client.Shared;
using EventoTickets.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using static System.Net.WebRequestMethods;

namespace EventoTickets.Client.Pages
{
    public partial class Painel
    {
        [Parameter]
        public string Id { get; set; }

        #region Variáveis globais
        private List<Ticket> tickets;
        private List<Ticket> ticketsRestantes;
        private Evento evento;
        private HubConnection hubConnection;
        private System.Threading.Timer timer;
        #endregion Variáveis globais

        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrWhiteSpace(Id))
            {
                evento = await Http.GetFromJsonAsync<Evento>("api/eventos/" + Id);
            }
            else
            {
                evento = await Http.GetFromJsonAsync<Evento>("api/eventos/GetEventoPadrao");
                Id = evento?.EventoId;
            }

            if (evento == null)
                evento = new Evento();

            hubConnection = new HubConnectionBuilder().WithUrl(NavigationManager.ToAbsoluteUri("/eventohub")).Build();
            hubConnection.KeepAliveInterval = TimeSpan.FromDays(1);

            hubConnection.On<Ticket>("AtualizarTicketMessage", (ticket) =>
            {
                if (ticket != null)
                {
                    var ticketLancado = tickets?.FirstOrDefault(t => t.TicketId.Equals(ticket.TicketId));

                    if (ticketLancado != null)
                    {
                        ticketLancado.Status = ticket.Status;
                        ticketLancado.DataConfirmacao = ticket.DataConfirmacao;
                    }

                    AtualizarRestantes();
                }
            });

            // Quando houver atualização do talonário
            hubConnection.On<string>("AtualizarTaloesMessage", (_) =>
            {
                CallCarregarTickets();
            });

            // Verifica a cada 5 minutos que a conexão com SignalR não foi fechada
            timer = new System.Threading.Timer(async (object stateInfo) =>
            {
                if (!IsConnected)
                {
                    try
                    {
                        Snackbar.Add($"Erro ao reconectar SignalR: {hubConnection.State}", Severity.Warning);
                        await hubConnection.StartAsync();
                        await CarregarTickets();
                    }
                    catch (Exception ex)
                    {
                        Snackbar.Add($"Erro ao reconectar SignalR: {ex.Message}", Severity.Error);
                    }
                }
            }, new System.Threading.AutoResetEvent(false), 300000, 300000);

            await hubConnection.StartAsync();
            await CarregarTickets();
        }

        private void CallCarregarTickets()
        {
            Task.Run(async () =>
            {
                await CarregarTickets();
            });
        }

        public bool IsConnected => hubConnection.State == HubConnectionState.Connected;

        private async Task CarregarTickets()
        {
            try
            {
                var retornoTickets = await Http.GetFromJsonAsync<RetornoAcao<IEnumerable<Ticket>>>("api/tickets/CarregarTickets/" + Id);
                if (retornoTickets.Sucesso)
                {
                    tickets = retornoTickets.Result.ToList();
                }
                else
                {
                    tickets = new();
                    Snackbar.Add($"Erro ao carregar: {retornoTickets.MensagemErro}");
                }

                AtualizarRestantes();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Erro ao carregar: {ex.Message}");
            }
        }

        private void AtualizarRestantes()
        {
            ticketsRestantes = tickets?.Where(x => x.Status == StatusTicket.EmAberto && 
                !string.IsNullOrWhiteSpace(x.Talao?.ResponsavelTalao)).ToList();
            StateHasChanged();
        }
    }
}