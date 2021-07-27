using EventoTickets.Server.Data;
using EventoTickets.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using EventoTickets.Shared.Requests;

namespace EventoTickets.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaloesController : ControllerBase
    {
        private readonly EventoDbContext _context;

        public TaloesController(EventoDbContext context)
        {
            _context = context;
        }

        [HttpGet("{idEvento}")]
        public async Task<ActionResult<IEnumerable<Talao>>> Get(string idEvento)
        {
            var taloes = await _context.Taloes.Where(x => x.EventoId.Equals(idEvento))?
                .OrderBy(x => x.NumeroTalao).ToListAsync();
            return taloes;
        }

        // GET api/<TaloesController>/5
        [HttpGet("Talao/{id}")]
        public async Task<ActionResult<Talao>> CarregarTalao(string id)
        {
            return await _context.Taloes.FirstOrDefaultAsync(x => x.TalaoId.Equals(id));
        }

        // POST api/<TaloesController>
        [HttpPost("{talonarioRequest}")]
        [Route("[action]")]
        public async Task<ActionResult<RetornoAcao>> GerarTaloes(NovoTalonarioRequest talonarioRequest)
        {
            var retorno = new RetornoAcao();

            try
            {
                var evento = await _context.Eventos.FirstOrDefaultAsync(x => x.EventoId.Equals(talonarioRequest.EventoId));

                if (evento == null)
                    return NotFound(new RetornoAcao { MensagemErro = "Evento não encontrado" });

                var ultimoTalao = (await _context.Taloes.Where(t => t.EventoId.Equals(evento.EventoId))?
                    .MaxAsync(x => (int?)x.NumeroTalao)) ?? 0;
                var ultimoTicket = (await _context.Tickets.Where(t => t.EventoId.Equals(evento.EventoId))?
                    .MaxAsync(n => (int?)n.NumeroTicket)) ?? 0;
                var novosTaloes = new List<Talao>();
                var novosTickets = new List<Ticket>();

                await Task.Run(() =>
                {
                    for (int i = 1; i <= talonarioRequest.QuantidadeTaloes; i++)
                    {
                        var talao = new Talao
                        {
                            NumeroTalao = ++ultimoTalao,
                            EventoId = talonarioRequest.EventoId
                        };

                        novosTaloes.Add(talao);

                        for (int t = 1; t <= talonarioRequest.TicketsPorTalao; t++)
                        {
                            var ticket = new Ticket
                            {
                                EventoId = talonarioRequest.EventoId,
                                TalaoId = talao.TalaoId,
                                Status = StatusTicket.EmAberto,
                                NumeroTicket = ++ultimoTicket
                            };

                            if (t == 1)
                            {
                                talao.NumeroInicial = ultimoTicket;
                            }
                            else
                            {
                                talao.NumeroFinal = ultimoTicket;
                            }

                            novosTickets.Add(ticket);
                        }
                    }
                });

                _context.BulkInsert(novosTaloes);
                _context.BulkInsert(novosTickets);

                retorno.Sucesso = true;
            }
            catch (Exception ex)
            {
                retorno.Sucesso = false;
                retorno.MensagemErro = ex.Message;
            }

            return Ok(retorno);
        }

        // PUT api/<TaloesController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] Talao talao)
        {
            if (id != talao.TalaoId)
            {
                return BadRequest();
            }

            _context.Entry(talao).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TalaoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE api/<TaloesController>/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<RetornoAcao>> Delete(string id)
        {
            var retorno = new RetornoAcao();

            try
            {
                var talao = await _context.Taloes.FindAsync(id);
                if (talao == null)
                {
                    return NotFound();
                }

                // Verifica se existem fichas entregues/devolvidas
                var fichas = await _context.Tickets.CountAsync(t => t.EventoId.Equals(talao.EventoId) && t.TalaoId.Equals(talao.TalaoId) &&
                    t.DataConfirmacao != null);

                if (fichas == 0)
                {
                    _context.Taloes.Remove(talao);
                    await _context.SaveChangesAsync();

                    retorno.Sucesso = true;
                }
                else
                {
                    retorno.MensagemErro = "Já existem fichas confirmadas para esse talão";
                }

            }
            catch (Exception ex)
            {
                retorno.Sucesso = false;
                retorno.MensagemErro = ex.StackTrace ?? ex.Message;
            }

            return Ok(retorno);
        }

        private bool TalaoExists(string id)
        {
            return _context.Taloes.Any(e => e.TalaoId == id);
        }
    }
}
