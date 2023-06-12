using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventoTickets.Server.Data;
using EventoTickets.Shared;
using EventoTickets.Shared.Requests;

namespace EventoTickets.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly EventoDbContext _context;

        public TicketsController(EventoDbContext context)
        {
            _context = context;
        }

        // GET: api/Tickets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ticket>>> GetTickets()
        {
            return await _context.Tickets.AsNoTracking().ToListAsync();
        }

        [HttpGet("CarregarTickets/{idEvento}")]
        public async Task<ActionResult<RetornoAcao<IEnumerable<Ticket>>>> CarregarTickets(string idEvento)
        {
            var retorno = new RetornoAcao<IEnumerable<Ticket>>();

            try
            {
                var consulta = await _context.Tickets.Where(t => t.EventoId.Equals(idEvento))
                    .AsNoTracking()
                    .Select(t => new
                    {
                        t.Talao.NumeroTalao,
                        Responsavel = t.Talao.ResponsavelTalao,
                        Ticket = t
                    }).ToListAsync();

                retorno.Result = consulta?.Select(t => new Ticket
                {
                    TicketId = t.Ticket.TicketId,
                    NumeroTicket = t.Ticket.NumeroTicket,
                    DataConfirmacao = t.Ticket.DataConfirmacao,
                    EventoId = t.Ticket.EventoId,
                    TalaoId = t.Ticket.TalaoId,
                    Status = t.Ticket.Status,
                    Talao = new Talao
                    {
                        NumeroTalao = t.NumeroTalao,
                        ResponsavelTalao = t.Responsavel
                    }
                });

                retorno.Sucesso = true;
            }
            catch (Exception ex)
            {
                retorno.Sucesso = false;
                retorno.MensagemErro = ex.Message;
            }

            return Ok(retorno);
        }

        // GET: api/Tickets/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RetornoAcao<Ticket>>> GetTicket(int id)
        {
            var retorno = new RetornoAcao<Ticket>();

            try
            {
                var ticket = await _context.Tickets.FindAsync(id);

                if (ticket == null)
                {
                    retorno.Sucesso = false;
                    retorno.MensagemErro = "Ficha não encontrada";
                }
                else
                {
                    retorno.Sucesso = true;
                }
            }
            catch (Exception ex)
            {
                retorno.Sucesso = false;
                retorno.MensagemErro = ex.Message;
            }

            return Ok(retorno);
        }

        // PUT: api/Tickets/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTicket(int id, Ticket ticket)
        {
            if (id != ticket.TicketId)
            {
                return BadRequest();
            }

            _context.Entry(ticket).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TicketExists(id))
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

        // POST: api/Tickets
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Ticket>> PostTicket(Ticket ticket)
        {
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTicket", new { id = ticket.TicketId }, ticket);
        }

        // DELETE: api/Tickets/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{ticketRequest}")]
        [Route("[action]")]
        public async Task<ActionResult<RetornoAcao<List<RetornoAcao<Ticket>>>>> AtualizarTicket([FromBody] TicketRequest ticketRequest)
        {
            var retorno = new RetornoAcao<List<RetornoAcao<Ticket>>>
            {
                Result = new()
            };

            try
            {
                await foreach (var ticket in _context.Tickets.Where(t => ticketRequest.TicketsIds.Contains(t.TicketId)).AsAsyncEnumerable())
                {
                    var retornoAcao = new RetornoAcao<Ticket>
                    {
                        Result = ticket
                    };

                    if (ticket.Status != StatusTicket.EmAberto && ticketRequest.Status != StatusTicket.EmAberto)
                    {
                        retornoAcao.MensagemErro = "Ficha já foi " + (ticket.Status == StatusTicket.Devolvido ? "devolvida" : "entregue") +
                            $" em {ticket.DataConfirmacao:dd/MM/yyyy HH:mm}";
                    }
                    else
                    {
                        ticket.Status = ticketRequest.Status;
                        ticket.DataConfirmacao = (ticketRequest.Status != StatusTicket.EmAberto ? DateTime.Now : null);

                        if (ticketRequest.Status == StatusTicket.Entregue)
                        {
                            var talao = await _context.Taloes.Where(t => t.TalaoId.Equals(ticket.TalaoId))
                                .AsNoTracking()
                                .Select(t => new
                                {
                                    t.ResponsavelTalao
                                }).FirstAsync();

                            if (talao != null && string.IsNullOrWhiteSpace(talao.ResponsavelTalao))
                                ticket.Status = StatusTicket.Avulso;
                        }

                        retornoAcao.Sucesso = true;
                    }

                    retorno.Result.Add(retornoAcao);
                }

                await _context.SaveChangesAsync();
                retorno.Sucesso = true;
            }
            catch (Exception ex)
            {
                retorno.Sucesso = false;
                retorno.MensagemErro = ex.StackTrace ?? ex.Message;
            }

            return Ok(retorno);
        }

        private bool TicketExists(int id)
        {
            return _context.Tickets.Any(e => e.TicketId == id);
        }
    }
}
