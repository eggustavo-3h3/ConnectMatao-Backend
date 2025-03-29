using connectMatao.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace connectMatao.Configurations
{
    public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
    {
        public void Configure(EntityTypeBuilder<Categoria> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(u => u.Descricao).IsRequired().HasMaxLength(150);

            builder.ToTable("TB_Categoria");
        }
    }
}
