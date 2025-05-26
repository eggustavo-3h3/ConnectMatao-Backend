using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using connectMatao.Domain.Entities;

namespace connectMatao.Infra.Data.Configurations

{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Nome).IsRequired().HasMaxLength(150);
            builder.Property(u => u.Login).IsRequired().HasMaxLength(100); // Login é o email
            builder.Property(u => u.Senha).IsRequired().HasMaxLength(250);
            builder.Property(u => u.Imagem).IsRequired();
            builder.Property(u => u.Perfil).IsRequired();
            builder.Property(u => u.ChaveResetSenha).IsRequired(false);

            builder.ToTable("TB_Usuario");
        }
    }
}
