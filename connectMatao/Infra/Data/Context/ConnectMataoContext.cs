using Microsoft.EntityFrameworkCore;
using connectMatao.Domain.Entities;
using connectMatao.Infra.Data.Configurations;

namespace connectMatao.Infra.Data.Context
    {
        public class ConnectMataoContext : DbContext
        {        
            public DbSet<Usuario> UsuarioSet { get; set; }
            public DbSet<Evento> EventoSet { get; set; }
            public DbSet<Categoria> CategoriaSet { get; set; }
            public DbSet<EventoEstatisticas> EventoEstatisticaSet { get; set; }
            public DbSet<EventoImagens> EventoImagemSet { get; set; }
            public DbSet<FormUsuarioParceiro> FormsUsuarioParceiro { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                var conexao = "Server=mysql.tccnapratica.com.br;Port=3306;Database=tccnapratica13;User=tccnapratica13;Password=Etec3h3;";
                optionsBuilder.UseMySql(conexao, ServerVersion.AutoDetect(conexao));
                base.OnConfiguring(optionsBuilder);
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.ApplyConfiguration(new CategoriaConfiguration());
                modelBuilder.ApplyConfiguration(new EventoConfiguration());
                modelBuilder.ApplyConfiguration(new EventoEstatisticasConfiguration());
                modelBuilder.ApplyConfiguration(new EventoImagensConfiguration());
                modelBuilder.ApplyConfiguration(new UsuarioConfiguration());

                base.OnModelCreating(modelBuilder);
            }
        }
    }
