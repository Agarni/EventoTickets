using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Web;
using EventoTickets.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using System.Net.Sockets;
using EventoTickets.Shared.Requests;
using System.Diagnostics;

namespace EventoTickets.Client.Pages
{
    public partial class Lancamentos
    {
        [Parameter]
        public string Id { get; set; }

        #region Variáveis globais
        private List<Ticket> tickets;
        private Evento evento;
        private HubConnection hubConnection;
        private int numeroTicket
        {
            get => lancamento.FichaInicial;
            set
            {
                lancamento.FichaInicial = value;
                lancamento.FichaFinal = lancamento.FichaFinal == 0 || value > lancamento.FichaFinal ? value : lancamento.FichaFinal;
            }
        }
        private MudNumericField<int> fichaLancada;
        private LancamentoFicha lancamento = new();
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

            hubConnection = new HubConnectionBuilder()
                .WithUrl(NavigationManager.ToAbsoluteUri("/eventohub"))
                .WithAutomaticReconnect()
                .Build();

            hubConnection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await hubConnection.StartAsync();
                await CarregarTickets();
            };

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

            // Quando houver atualização do talonário
            hubConnection.On<string>("AtualizarTaloesMessage", (_) =>
            {
                CallCarregarTickets();
            });

            await hubConnection.StartAsync();
            await CarregarTickets();
            await fichaLancada.FocusAsync();
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
                if (Id == null)
                    return;

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

        private static string CorTicket(Ticket ticket)
        {
            var cor = ticket.Status switch
            {
                StatusTicket.Entregue => Colors.Grey.Lighten2, //Color.Default,
                StatusTicket.Devolvido => Colors.Purple.Accent3 + ";color:#fff", //Color.Secondary,
                StatusTicket.Avulso => Colors.Green.Default + ";color:#fff", //Color.Success,
                _ => Colors.Blue.Default + ";color:#fff" //Color.Info
            };

            if (!string.IsNullOrWhiteSpace(ticket.Talao.ResponsavelTalao) && ticket.Status == StatusTicket.EmAberto)
                cor = Colors.Indigo.Default + ";color:#fff"; // Color.Primary;

            return cor;
        }

        public async void TicketKeyDown(KeyboardEventArgs args)
        {
            if (args.Code == "Enter" || args.Code == "NumpadEnter")
            {
                await LancarTicket();
            }
        }

        private async Task LancarTicket()
        {
            var ticket = tickets.FirstOrDefault(t => t.NumeroTicket.Equals(lancamento.FichaInicial) && t.EventoId.Equals(evento.EventoId));
            
            if (ticket == null)
            {
                Snackbar.Add($"Informe um nº da ficha inicial entre {tickets.Min(x => x.NumeroTicket)} e {tickets.Max(x => x.NumeroTicket)}",
                    Severity.Warning);
                await fichaLancada.FocusAsync();
                return;
            }

            var listaTickets = ListaIdsTickets();
            //var idTicket = ticket.TicketId;
            await AtualizarTicket(listaTickets.ToArray(), StatusTicket.Entregue);
        }

        private async Task AtualizarTicket(int[] idsTickets, StatusTicket statusTicket)
        {
            if (idsTickets.Length == 0)
            {
                Snackbar.Add("Informe o nº da ficha", Severity.Warning);
                await fichaLancada.FocusAsync();
                return;
            }

            var ticketRequest = new TicketRequest
            {
                TicketsIds = idsTickets,
                Status = statusTicket
            };
            var response = await Http.PostAsJsonAsync("api/tickets/AtualizarTicket/", ticketRequest);

            if (response.IsSuccessStatusCode)
            {
                var retorno = await response.Content.ReadFromJsonAsync<RetornoAcao<List<RetornoAcao<Ticket>>>>();

                if (!retorno.Sucesso)
                {
                    if (retorno.Result == null)
                    {
                        Snackbar.Add($"Não foi possível atualizar ficha: {retorno.MensagemErro}", Severity.Error);
                    }
                    else
                    {
                        Snackbar.Add(retorno.MensagemErro, Severity.Warning);
                    }
                }
                else
                {
                    foreach(var retornoTicket in retorno.Result)
                    {
                        Debug.WriteLine(retornoTicket);
                        if (!retornoTicket.Sucesso)
                        {
                            Snackbar.Add($"Erro ao atualizar ficha {retornoTicket.Result.NumeroTicket}: {retornoTicket.MensagemErro}", 
                                Severity.Error);
                            continue;
                        }

                        if (IsConnected)
                        {
                            await SendMessage(retornoTicket.Result);
                        }
                        else
                        {
                            var ticketConsulta = tickets?.FirstOrDefault(x => x.TicketId == retornoTicket.Result.TicketId);

                            if (ticketConsulta != null)
                            {
                                ticketConsulta.Status = retornoTicket.Result.Status;
                                ticketConsulta.DataConfirmacao = retornoTicket.Result.DataConfirmacao;
                                StateHasChanged();
                            }
                        }
                    }

                    lancamento.FichaInicial = 0;
                    lancamento.FichaFinal = 0;
                }
            }

            await fichaLancada.FocusAsync();
        }

        private void NumeroFicha_Changed(string valor)
        {
            if (!int.TryParse(valor, out int numeroFicha)) 
                return;

            lancamento.FichaFinal = numeroFicha;
        }

        private async void DevolverTicket()
        {
            //var idTicket = tickets.FirstOrDefault(t => t.NumeroTicket.Equals(lancamento.FichaInicial) && t.EventoId.Equals(evento.EventoId))?.TicketId ?? 0;
            await AtualizarTicket(ListaIdsTickets(StatusTicket.Devolvido)?.ToArray(), StatusTicket.Devolvido);
        }

        private async void ReabrirTicket()
        {
            //var idTicket = tickets.FirstOrDefault(t => t.NumeroTicket.Equals(lancamento.FichaInicial) && t.EventoId.Equals(evento.EventoId))?.TicketId ?? 0;
            await AtualizarTicket(ListaIdsTickets(StatusTicket.EmAberto)?.ToArray(), StatusTicket.EmAberto);
        }

        private List<int> ListaIdsTickets(StatusTicket statusTicket = StatusTicket.Entregue)
        {
            //if (lancamento.FichaFinal <= lancamento.FichaInicial)
            //    return new() { lancamento.FichaInicial, lancamento.FichaFinal };

            var lst = tickets.Where(x => x.NumeroTicket >= lancamento.FichaInicial && x.NumeroTicket <= lancamento.FichaFinal)
                .ToList();

            // Considera somente os tickets em aberto quando entregar ou devolver
            if (statusTicket is StatusTicket.Entregue or StatusTicket.Devolvido)
            {
                lst = lst.Where(x => x.Status == StatusTicket.EmAberto).ToList();
            }

            return lst.Select(x => x.TicketId).ToList();
        }

        private class LancamentoFicha
        {
            public int FichaInicial { get; set; }
            public int FichaFinal { get; set; }
        }
    }
}