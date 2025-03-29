using connectMatao.Enumerator;

namespace connectMatao.Domain.Entities
{
    public class EventoEstatisticas
    {
        public Guid Id { get; set; }
        public EnumTipoEstatistica TipoEstatistica { get; set; }
        public Guid Usuarioid { get; set; } 
        public Guid Eventoid { get; set; }

        public virtual Usuario Usuario { get; set; }
        public virtual Evento Evento { get; set; }

    }
}
