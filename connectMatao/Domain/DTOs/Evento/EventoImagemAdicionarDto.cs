namespace connectMatao.Domain.DTOs.Evento
{
    public class EventoImagemAdicionarDto
    {
        public Guid EventoId { get; set; }
        public List<string> Imagens { get; set; } = new();
    }

}
