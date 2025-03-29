using connectMatao.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace connectMatao.Configurations
{
    public class EventoImagensConfiguration : IEntityTypeConfiguration<EventoImagens>
    {
        public void Configure(EntityTypeBuilder<EventoImagens> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(u => u.Imagem).IsRequired();

            builder.ToTable("TB_EventoImagens");
        }

    }
}
