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
using EventoTickets.Shared.Requests;
using System.Diagnostics;
using System.Text;
using Toolbelt.Blazor.HotKeys2;

namespace EventoTickets.Client.Pages
{
    public partial class Lancamentos
    {
        [Parameter]
        public string Id { get; set; }

        #region Variáveis globais

        private MudNumericField<int> _fichaFinal = null!;
        private List<Ticket> tickets;
        private Evento evento;
        private HubConnection hubConnection;
        private HotKeysContext? _hotKeysContext;
        
        private int numeroTicket
        {
            get => lancamento.FichaInicial;
            set
            {
                lancamento.FichaInicial = value;
                lancamento.FichaFinal = lancamento.FichaFinal == 0 || value > lancamento.FichaFinal ? value : lancamento.FichaFinal;
            }
        }
        private MudNumericField<int> _fichaInicial;
        private LancamentoFicha lancamento = new();
        private bool _fichaFinalDesativada = true;
        private StringBuilder _ultimoLancamento = new();

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
            
            // Criando hotkey para habilitar/desabilitar ficha final
            _hotKeysContext = HotKeys.CreateContext()
                .Add(ModCode.Ctrl | ModCode.Shift, Code.E, AtivarDesativarFichaFinal, 
                    "Ativar/Desativar ficha final",
                    Exclude.None);

            if (_fichaInicial is not null)
                await _fichaInicial.FocusAsync();
        }

        private async ValueTask AtivarDesativarFichaFinal()
        {
            _fichaFinalDesativada = !_fichaFinalDesativada;

            switch (_fichaFinalDesativada)
            {
                case true when _fichaInicial is not null:
                    await _fichaInicial.FocusAsync();
                    break;
                
                case false when _fichaFinal is not null:
                    await _fichaFinal.FocusAsync();
                    break;
            }
        }

        private void CallCarregarTickets()
        {
            Task.Run(async () =>
            {
                await CarregarTickets();
            });
        }

        private bool IsConnected => hubConnection.State == HubConnectionState.Connected;
        private Task SendMessage(Ticket ticket) => hubConnection.SendAsync("AtualizarTicketMessage", ticket);

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
                StatusTicket.Entregue => Colors.Gray.Lighten2, //Color.Default,
                StatusTicket.Devolvido => Colors.Purple.Accent3 + ";color:#fff", //Color.Secondary,
                StatusTicket.Avulso => Colors.Green.Default + ";color:#fff", //Color.Success,
                _ => Colors.Blue.Default + ";color:#fff" //Color.Info
            };

            if (!string.IsNullOrWhiteSpace(ticket.Talao.ResponsavelTalao) && ticket.Status == StatusTicket.EmAberto)
                cor = Colors.Indigo.Default + ";color:#fff"; // Color.Primary;

            return cor;
        }

        private async Task LancarTicket()
        {
            var ticket = tickets.FirstOrDefault(t => t.NumeroTicket.Equals(lancamento.FichaInicial) && t.EventoId.Equals(evento.EventoId));
            
            if (ticket == null)
            {
                Snackbar.Add($"Informe um nº de ficha inicial entre {tickets.Min(x => x.NumeroTicket)} e {tickets.Max(x => x.NumeroTicket)}",
                    Severity.Warning);
                await _fichaInicial.FocusAsync();
                return;
            }

            var listaTickets = ListaIdsTickets();
            
            await AtualizarTicket(listaTickets.ToArray(), StatusTicket.Entregue);
        }

        private async Task AtualizarTicket(int[] idsTickets, StatusTicket statusTicket)
        {
            if (idsTickets.Length == 0)
            {
                Snackbar.Add("Informe o nº da ficha", Severity.Warning);
                await _fichaInicial.FocusAsync();
                return;
            }

            var qtdFichas = (lancamento.FichaFinal - lancamento.FichaInicial) + 1;
            if (!_fichaFinalDesativada && qtdFichas > 5)
            {
                var result = await DialogService.ShowMessageBox("Confirmação de lançamento de fichas", 
                    $"Tem certeza que deseja realizar a {descricaoStatus(statusTicket).ToLower()} de {qtdFichas} fichas?",
                    yesText: "Sim", cancelText: "Não");
                
                if (result is not true)
                    return;
            }
            
            _ultimoLancamento.Clear();
            _ultimoLancamento.Append("Último lançamento: <strong>Ficha");
            
            if (!_fichaFinal.Disabled && (lancamento.FichaInicial < lancamento.FichaFinal))
                _ultimoLancamento.Append("s de");
            
            _ultimoLancamento.Append(' ');
            _ultimoLancamento.Append(lancamento.FichaInicial);

            if (!_fichaFinal.Disabled && (lancamento.FichaInicial < lancamento.FichaFinal))
            {
                _ultimoLancamento.Append(" a ");
                _ultimoLancamento.Append(lancamento.FichaFinal);
            }

            _ultimoLancamento.Append($" ({descricaoStatus(statusTicket)})");
            _ultimoLancamento.Append("</strong>");

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

                            if (ticketConsulta == null) 
                                continue;
                            
                            ticketConsulta.Status = retornoTicket.Result.Status;
                            ticketConsulta.DataConfirmacao = retornoTicket.Result.DataConfirmacao;
                            StateHasChanged();
                        }
                    }

                    lancamento.FichaInicial = 0;
                    lancamento.FichaFinal = 0;
                }
            }

            await _fichaInicial.FocusAsync();
            return;
            
            string descricaoStatus(StatusTicket statusTicket1) =>
                statusTicket1 switch
                {
                    StatusTicket.Entregue => "Entrega",
                    StatusTicket.Devolvido => "Devolução",
                    StatusTicket.Avulso => "Venda avulsa",
                    _ => "Reabertura"
                };
        }

        private async void DevolverTicket()
        {
            await AtualizarTicket(ListaIdsTickets(StatusTicket.Devolvido)?.ToArray(), StatusTicket.Devolvido);
        }

        private async void ReabrirTicket()
        {
            await AtualizarTicket(ListaIdsTickets(StatusTicket.EmAberto)?.ToArray(), StatusTicket.EmAberto);
        }

        private List<int> ListaIdsTickets(StatusTicket statusTicket = StatusTicket.Entregue)
        {
            var lst = (_fichaFinal.Disabled
                    ? tickets.Where(x => x.NumeroTicket.Equals(lancamento.FichaInicial))
                    : tickets.Where(x => x.NumeroTicket >= lancamento.FichaInicial && x.NumeroTicket <= lancamento.FichaFinal))
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

        public async ValueTask DisposeAsync()
        {
            if (_hotKeysContext is not null)
                await _hotKeysContext.DisposeAsync();
        }
    }
}