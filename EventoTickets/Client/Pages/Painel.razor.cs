using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using EventoTickets.Client.Components;
using EventoTickets.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;

namespace EventoTickets.Client.Pages
{
    public partial class Painel
    {
        [Parameter]
        public string? Id { get; set; }

        #region Variáveis globais
        private List<Ticket> _tickets = [];
        private List<Ticket> _ticketsRestantes = [];
        private Evento? _evento = null;
        private HubConnection _hubConnection = null!;
        private System.Threading.Timer _timer;
        private ResumoFichas _resumoFichas = null!;
        private bool _statusInicializado;

        #endregion Variáveis globais

        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrWhiteSpace(Id))
            {
                _evento = await Http.GetFromJsonAsync<Evento>("api/eventos/" + Id);
            }
            else
            {
                _evento = await Http.GetFromJsonAsync<Evento>("api/eventos/GetEventoPadrao");
                Id = _evento?.EventoId;
            }

            _evento ??= new Evento();

            _hubConnection = new HubConnectionBuilder().WithUrl(NavigationManager.ToAbsoluteUri("/eventohub"))
                .WithAutomaticReconnect()
                .Build();
            
            _hubConnection.Reconnecting += (_) =>
            {
                _resumoFichas.StatusConexao = StatusConexao.Reconectando;
                Snackbar.Add("Reconectando ao SignalR...", Severity.Warning);
                return Task.CompletedTask;
            };
            
            _hubConnection.Closed += (_) =>
            {
                _resumoFichas.StatusConexao = StatusConexao.Desconectado;
                Snackbar.Add("Conexão com SignalR fechada.", Severity.Error);
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += async (_) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                _resumoFichas.StatusConexao = StatusConexao.Conectado;
                await CarregarTickets();
            };

            _hubConnection.On<Ticket?>("AtualizarTicketMessage", (ticket) =>
            {
                if (ticket is null) return;
                
                var ticketLancado = _tickets?.FirstOrDefault(t => t.TicketId.Equals(ticket.TicketId));

                if (ticketLancado != null)
                {
                    ticketLancado.Status = ticket.Status;
                    ticketLancado.DataConfirmacao = ticket.DataConfirmacao;
                }

                AtualizarRestantes();
            });

            // Quando houver atualização do talonário
            _hubConnection.On<string>("AtualizarTaloesMessage", (_) =>
            {
                CallCarregarTickets();
            });

            // Verifica a cada 5 minutos que a conexão com SignalR não foi fechada
            _timer = new System.Threading.Timer(async stateInfo =>
            {
                if (!IsConnected)
                {
                    try
                    {
                        Snackbar.Add($"Erro ao reconectar SignalR: {_hubConnection.State}", Severity.Warning);
                        await _hubConnection.StartAsync();
                        await CarregarTickets();
                    }
                    catch (Exception ex)
                    {
                        Snackbar.Add($"Erro ao reconectar SignalR: {ex.Message}", Severity.Error);
                        _resumoFichas.StatusConexao = StatusConexao.Desconectado;
                    }

                    return;
                }

                _resumoFichas.StatusConexao = StatusConexao.Conectado;
            }, new System.Threading.AutoResetEvent(false), 300000, 300000);

            await _hubConnection.StartAsync()
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Snackbar.Add($"Erro ao iniciar conexão com SignalR: {t.Exception?.GetBaseException().Message}", Severity.Error);
                    }
                });
            await CarregarTickets();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender || _statusInicializado) 
                return;
            
            _statusInicializado = true;
            _resumoFichas.StatusConexao = IsConnected ? StatusConexao.Conectado : StatusConexao.Desconectado;
        }

        private void CallCarregarTickets()
        {
            Task.Run(async () =>
            {
                await CarregarTickets();
            });
        }

        public bool IsConnected => _hubConnection.State == HubConnectionState.Connected;

        private async Task CarregarTickets()
        {
            try
            {
                if (Id is null)
                    return;

                var retornoTickets = await Http.GetFromJsonAsync<RetornoAcao<IEnumerable<Ticket>>>("api/tickets/CarregarTickets/" + Id);
                if (retornoTickets.Sucesso)
                {
                    _tickets = [.. retornoTickets.Result];
                }
                else
                {
                    _tickets = new();
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
            _ticketsRestantes = _tickets?.Where(x => x.Status == StatusTicket.EmAberto && 
                !string.IsNullOrWhiteSpace(x.Talao?.ResponsavelTalao)).ToList() ?? [];
            StateHasChanged();
        }
    }
}