namespace connectMatao.Domain.Entities
{
    public class EventoImagens
    {
        public Guid Id { get; set; }
        public string Imagem { get; set; } = string.Empty;
        public Guid EventoId { get; set; } 

        public virtual Evento Evento { get; set; }

    }
}
