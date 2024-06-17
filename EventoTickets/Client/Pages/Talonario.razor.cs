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
using EventoTickets.Shared.Requests;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using EventoTickets.Client.Extensions;

namespace EventoTickets.Client.Pages
{
    public partial class Talonario
    {
        [Parameter]
        public string Id { get; set; }

        #region Variáveis globais
        private bool valido;
        private MudForm form;
        private int qtdTaloes, qtdTickets;
        private List<Talao> taloes;
        private Evento evento;
        private HubConnection hubConnection;
        private Talao talaoSelecionado = null;
        private Talao talaoSemEditar = null;
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

            hubConnection.On<string>("AtualizarTaloesMessage", (id) =>
            {
                if (id == Id)
                {
                    CallCarregarTalonarios();
                }
            });

            await hubConnection.StartAsync();
            await CarregarTalonarios();
        }

        public bool IsConnected => hubConnection.State == HubConnectionState.Connected;
        Task SendMessage() => hubConnection.SendAsync("AtualizarTaloesMessage", Id);

        private void CallCarregarTalonarios()
        {
            Task.Run(async () =>
            {
                await CarregarTalonarios();
            });
        }

        async Task CarregarTalonarios()
        {
            try
            {
                var retornoTaloes = await Http.GetFromJsonAsync<IEnumerable<Talao>>("api/taloes/" + Id);
                if (retornoTaloes != null)
                {
                    taloes = retornoTaloes.ToList();
                }
                else
                {
                    taloes = new();
                }

                StateHasChanged();
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
        }

        async Task GerarTaloes()
        {
            if (qtdTaloes == 0 || qtdTickets == 0)
            {
                Snackbar.Add("Quantidade de talões e Nº de bilhetes por talão devem ser superiores a 0 (zero)", Severity.Error);
                StateHasChanged();
            }
            else
            {
                NovoTalonarioRequest talonarioRequest = new NovoTalonarioRequest{EventoId = evento.EventoId, QuantidadeTaloes = qtdTaloes, TicketsPorTalao = qtdTickets};
                var response = await Http.PostAsJsonAsync("api/taloes/GerarTaloes/", talonarioRequest);
                if (response.IsSuccessStatusCode)
                {
                    var retorno = await response.Content.ReadFromJsonAsync<RetornoAcao>();
                    if (!retorno.Sucesso)
                    {
                        Snackbar.Add($"Não foi possível gerar talões: {retorno.MensagemErro}", Severity.Error);
                    }
                    else
                    {
                        Snackbar.Add("Talões gerados com sucesso", Severity.Success);
                        
                        if (IsConnected)
                        {
                            await SendMessage();
                        }
                        else
                        {
                            await CarregarTalonarios();
                        }
                    }
                }
            }
        }

        void BackupItem(object item)
        {
            talaoSemEditar = new();

            if (item != null)
                item.CopyPropsTo(ref talaoSemEditar);
        }

        void CancelarEdicaoTalao(object item)
        {
            talaoSemEditar?.CopyPropsTo(ref item);
        }

        async void UpdateTalao(object item)
        {
            Talao talao = (Talao)item;

            await Http.PutAsJsonAsync("api/taloes/" + talao.TalaoId, talao);

            if (IsConnected)
            {
                await SendMessage();
            }
            else
            {
                await CarregarTalonarios();
            }
        }

        async void ExcluirTalao(Talao talao)
        {
            bool? result = await DialogService.ShowMessageBox("Excluir", "Confirma a exclusão do talão?",
               yesText: "Excluir", cancelText: "Cancelar");

            if (result.GetValueOrDefault())
            {
                var response = await Http.DeleteAsync("api/taloes/" + talao.TalaoId);

                if (!response.IsSuccessStatusCode)
                {
                    Snackbar.Add("N�o foi poss�vel excluir talão: " + await response.Content.ReadAsStringAsync(), Severity.Error);
                }
                else
                {
                    var retorno = await response.Content.ReadFromJsonAsync<RetornoAcao>();
                    if (!retorno.Sucesso)
                    {
                        Snackbar.Add($"N�o foi possível excluir talão: {retorno.MensagemErro}", Severity.Error);
                    }
                    else
                    {
                        Snackbar.Add("Talão excluído com sucesso!", Severity.Success);

                        if (IsConnected)
                        {
                            await SendMessage();
                        }
                        else
                        {
                            await CarregarTalonarios();
                        }
                    }
                }

                if (IsConnected)
                {
                    await SendMessage();
                }
                else
                {
                    await CarregarTalonarios();
                }
            }
        }
    }
}