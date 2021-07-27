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
using System.Net.Sockets;
using EventoTickets.Shared.Requests;

namespace EventoTickets.Client.Pages
{
    public partial class Lancamentos
    {
        [Parameter]
        public string Id { get; set; }

        #region Vari�veis globais
        private bool valido;
        private MudForm form;
        private List<Ticket> tickets;
        private Evento evento;
        private HubConnection hubConnection;
        private int numeroTicket;
        #endregion Vari�veis globais

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

                    StateHasChanged();
                }
            });

            // Quando houver atualiza��o do talon�rio
            hubConnection.On<string>("AtualizarTaloesMessage", (_) =>
            {
                CallCarregarTickets();
            });

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
        Task SendMessage(Ticket ticket) => hubConnection.SendAsync("AtualizarTicketMessage", ticket);

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

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Erro ao carregar: {ex.Message}");
            }
        }

        private Color CorTicket(Ticket ticket) => ticket.Status switch
        {
            StatusTicket.Entregue => Color.Default,
            StatusTicket.Devolvido => Color.Secondary,
            _ => Color.Info
        };

        public void TicketKeyDown(KeyboardEventArgs e)
        {
            if (e.Code == "Enter" || e.Code == "NumpadEnter")
            {
                Task.Run(async () =>
                {
                    await LancarTicket();
                });
            }
        }

        private async Task AtualizarTicket(Ticket ticket)
        {
            if (ticket.Status == StatusTicket.Entregue || ticket.Status == StatusTicket.Devolvido)
                return;

            await AtualizarTicket(ticket.TicketId, StatusTicket.Entregue);
        }

        private async Task LancarTicket()
        {
            await AtualizarTicket(numeroTicket, StatusTicket.Entregue);
        }

        private async Task AtualizarTicket(int idTicket, StatusTicket statusTicket)
        {
            if (idTicket <= 0)
            {
                Snackbar.Add("Informe o n� da ficha", Severity.Warning);
                return;
            }

            var ticketRequest = new TicketRequest
            {
                TicketId = idTicket,
                Status = statusTicket
            };
            var response = await Http.PostAsJsonAsync("api/tickets/AtualizarTicket/", ticketRequest);

            if (response.IsSuccessStatusCode)
            {
                var retorno = await response.Content.ReadFromJsonAsync<RetornoAcao<Ticket>>();

                if (!retorno.Sucesso)
                {
                    if (retorno.Result == null)
                    {
                        Snackbar.Add($"N�o foi poss�vel atualizar ficha: {retorno.MensagemErro}", Severity.Error);
                    }
                    else
                    {
                        Snackbar.Add(retorno.MensagemErro, Severity.Warning);
                    }
                }
                else
                {
                    if (IsConnected)
                    {
                        await SendMessage(retorno.Result);
                    }
                    else
                    {
                        var ticketConsulta = tickets?.FirstOrDefault(x => x.TicketId == idTicket);

                        if (ticketConsulta != null)
                        {
                            ticketConsulta.Status = retorno.Result.Status;
                            ticketConsulta.DataConfirmacao = retorno.Result.DataConfirmacao;
                        }
                    }

                    numeroTicket = 0;
                }
            }
        }

        private async void DevolverTicket()
        {
            await AtualizarTicket(numeroTicket, StatusTicket.Devolvido);
        }

        private async void ReabrirTicket()
        {
            await AtualizarTicket(numeroTicket, StatusTicket.EmAberto);
        }
    }
}