//using Microsoft.AspNetCore.Mvc;
//using connectMatao.Domain;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using connectMatao.Enumerator;
//using System;
//using connectMatao.Domain.Entities;
//using connectMatao.Data.Context;

//[ApiController]
//[Route("api/Eventos")]
//public class EventosController : ControllerBase
//{
//    private readonly ConnectMataoContext _context;

//    public EventosController(ConnectMataoContext context)
//    {
//        _context = context;
//    }

//    [HttpPost ("CriarEvento")]
//    public async Task<IActionResult> CreateEvento([FromBody] Evento evento)
//    {
//        _context.Eventos.Add(evento);
//        await _context.SaveChangesAsync();
//        return Ok("Evento criado com sucesso.");
//    }

//    [HttpPost("{eventoId}/like")]
//    public async Task<IActionResult> LikeEvento(Guid eventoId, [FromBody] Guid usuarioId)
//    {
//        var estatistica = new EventoEstatisticas
//        {
//            TipoEstatistica = EnumTipoEstatistica.Like,
//            Eventoid = eventoId,
//            Usuarioid = usuarioId
//        };

//        _context.EventoEstatisticas.Add(estatistica);
//        await _context.SaveChangesAsync();

//        return Ok("Like adicionado com sucesso.");
//    }

//    [HttpPost("{eventoId}/deslike")]
//    public async Task<IActionResult> DeslikeEvento(Guid eventoId, [FromBody] Guid usuarioId)
//    {
//        var estatistica = new EventoEstatisticas
//        {
//            TipoEstatistica = EnumTipoEstatistica.Deslike,
//            Eventoid = eventoId,
//            Usuarioid = usuarioId
//        };

//        _context.EventoEstatisticas.Add(estatistica);
//        await _context.SaveChangesAsync();
//        return Ok("Deslike adicionado com sucesso.");
//    }



//    [HttpDelete("{eventoId}/Remover")]
//    public async Task<IActionResult> DeleteEvento(int eventoId)
//    {
//        var evento = await _context.Eventos.FindAsync(eventoId);
//        if (evento == null)
//        {
//            return NotFound("Evento não encontrado.");
//        }

//        _context.Eventos.Remove(evento);
//        await _context.SaveChangesAsync();
//        return Ok("Evento removido com sucesso.");
//    }

//    [HttpGet("ListarEventos")] 
//    public async Task<IActionResult> GetEventos()
//    {
//        var eventos = await _context.Eventos.ToListAsync();

        
//        if (eventos == null || eventos.Count == 0)
//        {
//            return NotFound("Nenhum evento encontrado.");
//        }

//        return Ok(eventos);
//    }

//}