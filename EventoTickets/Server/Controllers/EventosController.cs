using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventoTickets.Server.Data;
using EventoTickets.Shared;
using Z.BulkOperations;

namespace EventoTickets.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventosController : ControllerBase
    {
        private readonly EventoDbContext _context;

        public EventosController(EventoDbContext context)
        {
            _context = context;
        }

        // GET: api/Eventos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Evento>>> GetEvento()
        {
            return await _context.Eventos.OrderByDescending(x => x.DataRealizacao).ToListAsync();
        }

        // GET: api/Eventos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Evento>> GetEvento(string id)
        {
            var evento = await _context.Eventos.FindAsync(id);

            if (evento == null)
            {
                return NotFound();
            }

            return evento;
        }

        [HttpGet()]
        [Route("[action]")]
        public async Task<ActionResult<Evento>> GetEventoPadrao()
        {
            var evento = await _context.Eventos.FirstOrDefaultAsync(x => x.EventoPadrao && x.DataRealizacao >= DateTime.Now);

            if (evento == null)
                evento = new Evento { NomeEvento = "Manutenção de eventos" };

            return evento;
        }

        // PUT: api/Eventos/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEvento(string id, Evento evento)
        {
            if (id != evento.EventoId)
            {
                return BadRequest();
            }

            _context.Entry(evento).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventoExists(id))
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

        // POST: api/Eventos
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Evento>> PostEvento(Evento evento)
        {
            evento.EventoId = Guid.NewGuid().ToString();
            _context.Eventos.Add(evento);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (EventoExists(evento.EventoId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return Ok();
            //_context.Evento.Add(evento);
            //await _context.SaveChangesAsync();

            //return CreatedAtAction("GetEvento", new { id = evento.EventoId }, evento);
        }

        // DELETE: api/Eventos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvento(string id)
        {
            var evento = await _context.Eventos.FindAsync(id);
            if (evento == null)
            {
                return NotFound();
            }

            _context.Eventos.Remove(evento);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{evento}")]
        [Route("[action]")]
        public async Task<IActionResult> DefinirEventoPadrao(Evento evento)
        {
            var eventos = await _context.Eventos.ToListAsync();
            eventos.ForEach(x => x.EventoPadrao = x.EventoId.Equals(evento.EventoId));

            if (evento != null)
            {
                await _context.BulkUpdateAsync(eventos);
            }

            return Ok();
        }

        private bool EventoExists(string id)
        {
            return _context.Eventos.Any(e => e.EventoId == id);
        }
    }
}
