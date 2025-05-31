using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using connectMatao.Domain.Entities;

namespace connectMatao.Infra.Data.Configurations

{
    public class ParceiroConfiguration : IEntityTypeConfiguration<Parceiro>
    {
        public void Configure(EntityTypeBuilder<Parceiro> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.UsuarioId).IsRequired();
            builder.Property(u => u.NomeCompleto).IsRequired().HasMaxLength(150);
            builder.Property(u => u.Cpf).IsRequired().HasMaxLength(20);
            builder.Property(u => u.Telefone).IsRequired().HasMaxLength(20);
            builder.Property(u => u.FlagAprovado).IsRequired();
            builder.Property(u => u.DataEnvio).IsRequired();

            builder.ToTable("TB_Parceiro");
        }
    }
}