using connectMatao.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace connectMatao.Configurations
{
    public class EventoEstatisticasConfiguration : IEntityTypeConfiguration<EventoEstatisticas>
    {
        public void Configure(EntityTypeBuilder<EventoEstatisticas> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(u => u.TipoEstatistica).IsRequired();

            builder.ToTable("TB_EventoEstatistica");
        }
    }
}
