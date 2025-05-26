using connectMatao.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace connectMatao.Infra.Data.Configurations
{
    public class EventoConfiguration : IEntityTypeConfiguration<Evento>
    {
        public void Configure(EntityTypeBuilder<Evento> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Titulo).HasMaxLength(150).IsRequired();
            builder.Property(e => e.Cep).HasMaxLength(9).IsRequired();
            builder.Property(e => e.Logradouro).HasMaxLength(150).IsRequired();
            builder.Property(e => e.Numero).HasMaxLength(5).IsRequired();
            builder.Property(e => e.Bairro).HasMaxLength(100).IsRequired();
            builder.Property(e => e.Descricao).HasMaxLength(800).IsRequired();
            builder.Property(e => e.Telefone).HasMaxLength(20).IsRequired();
            builder.Property(e => e.Whatsapp).HasMaxLength(20).IsRequired();
            builder.Property(e => e.Email).HasMaxLength(200).IsRequired();
            builder.Property(e => e.Horario).HasMaxLength(5).IsRequired();
            builder.Property(e => e.FaixaEtaria).IsRequired();

            builder.ToTable("TB_Evento");
        }
    }
}
