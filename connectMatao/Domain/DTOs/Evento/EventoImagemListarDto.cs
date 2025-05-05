namespace connectMatao.Domain.DTOs.Evento
{
    public class EventoImagemListarDto
    {
        public Guid Id { get; set; }
        public string Imagem { get; set; }
        public Guid EventoId { get; set; }
    }
}
