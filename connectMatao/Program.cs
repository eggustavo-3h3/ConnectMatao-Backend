using connectMatao.Data.Context;
using connectMatao.Domain.DTOs.Base;
using connectMatao.Domain.DTOs.Categoria;
using connectMatao.Domain.DTOs.Evento;
using connectMatao.Domain.DTOs.Usuario;
using connectMatao.Domain.Entities;
using connectMatao.Enumerator;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using connectMatao.Domain.DTOs.Login;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using connectMatao.Domain.DTOs.EventoImagem;
using Microsoft.AspNetCore.Mvc;

internal class Program
{
    private static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


        builder.Services.AddDbContext<ConnectMataoContext>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(config =>
        {
            config.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Connect Matão API",
                Version = "v1",
                Description = "API para gerenciamento de eventos no Connect Matão"
            });

            config.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = @"<b>JWT Autorização</b> <br/>
                            Digite 'Bearer' [espaço] e em seguida seu token na caixa de texto abaixo.
                            <br/> <br/>
                            <b>Exemplo:</b> 'bearer 123456abcdefg...'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            config.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header
                    },
                    new List<string>()
                }
            });
        });

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "connect.m",
                    ValidAudience = "connect.m",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("aacd9108-22d7-4ef5-9296-a2c5923fdf5d"))
                };
            });

        WebApplication app = builder.Build();

        // Adicionando suporte ao CORS
        builder.Services.AddCors();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Connect Matão API v1");
        });

        // Configuração de CORS
        app.UseCors(x => x
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
           );

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        #region Categoria

        app.MapGet("categoria/listar", (ConnectMataoContext context) =>
        {
            var listaCategoriaDto = context.CategoriaSet.Select(p => new CategoriaListarDto
            {
                Id = p.Id,
                Descricao = p.Descricao
            }).AsEnumerable();

            return Results.Ok(listaCategoriaDto);
        }).WithTags("Categoria");


        app.MapPost("categoria/adicionar", (ConnectMataoContext context, CategoriaAdicionarDto categoriaDto, ClaimsPrincipal user) =>
        {
            var perfil = user.FindFirst(ClaimTypes.Role)?.Value;

            if (perfil != EnumPerfil.Parceiro.ToString() && perfil != EnumPerfil.Administrador.ToString())
            {
                return Results.Forbid();
            }

            context.CategoriaSet.Add(new Categoria
            {
                Id = Guid.NewGuid(),
                Descricao = categoriaDto.Descricao
            });

            context.SaveChanges();

            return Results.Created("Created", new BaseResponse("Categoria Registrada com Sucesso!"));
        }).RequireAuthorization().WithTags("Categoria");

        #endregion

        #region Usuario
        app.MapPost("/usuario/cadastrar", async (ConnectMataoContext context, UsuarioAdicionarDto usuarioDto) =>
        {
            if (usuarioDto.Senha != usuarioDto.ConfirmacaoSenha)
            {
                return Results.BadRequest(new BaseResponse("As senhas não coincidem."));
            }

            if (string.IsNullOrEmpty(usuarioDto.Senha) || usuarioDto.Senha.Length < 8)
            {
                return Results.BadRequest(new BaseResponse("A senha deve ter no mínimo 8 caracteres."));
            }

            if (!usuarioDto.Senha.Any(char.IsUpper))
            {
                return Results.BadRequest(new BaseResponse("A senha deve conter pelo menos uma letra maiúscula"));
            }

            if (!usuarioDto.Nome.Any(char.IsUpper))
            {
                return Results.BadRequest(new BaseResponse("O nome deve começar com pelo menos uma letra maiúscula"));
            }


            var usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                Nome = usuarioDto.Nome,
                Login = usuarioDto.Login,
                Senha = BCrypt.Net.BCrypt.HashPassword(usuarioDto.Senha), // Hash da senha
                Imagem = usuarioDto.Imagem,
                Perfil = usuarioDto.Perfil
            };

            context.Set<Usuario>().Add(usuario);
            await context.SaveChangesAsync();

            return Results.Created("Created", new BaseResponse("Usuário cadastrado com sucesso!"));
        }).WithTags("Usuário");

        // Endpoint para listar usuarios
        app.MapGet("/usuario/listar", async (ConnectMataoContext context) =>
        {
            var usuarios = await context.Set<Usuario>().Select(u => new
            {
                u.Id,
                u.Nome,
                u.Login,
                u.Imagem,
                u.Perfil
            }).ToListAsync();

            return Results.Ok(usuarios);
        }).WithTags("Usuário");

        // Endpoint para obter um usuário por ID
        app.MapGet("/usuario/{id:guid}", async (ConnectMataoContext context, Guid id) =>
        {
            var usuario = await context.Set<Usuario>().FindAsync(id);

            if (usuario == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                usuario.Id,
                usuario.Nome,
                usuario.Login,
                usuario.Imagem,
                usuario.Perfil
            });
        }).WithTags("Usuário");

        // Endpoint para remover usuário
        app.MapDelete("/usuario/{id}", async (ConnectMataoContext context, Guid id) =>
        {
            var usuario = await context.Set<Usuario>().FindAsync(id);

            if (usuario == null)
            {
                return Results.NotFound("Usuário não encontrado.");
            }

            context.Set<Usuario>().Remove(usuario);
            await context.SaveChangesAsync();

            return Results.Ok(new BaseResponse("Usuário removido com sucesso!"));
        }).RequireAuthorization().WithTags("Usuário");

        // Endpoint para atualizar informações do usuário
        app.MapPut("/usuario/atualizar", async (ConnectMataoContext context, UsuarioAtualizar usuarioDto, ClaimsPrincipal user) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var loggedInUserId))
            {
                return Results.Unauthorized();
            }

            var usuario = await context.Set<Usuario>().FindAsync(loggedInUserId);

            if (usuario == null)
            {
                return Results.NotFound("Usuário não encontrado.");
            }

            // Atualiza as propriedades permitidas
            if (!string.IsNullOrEmpty(usuarioDto.Nome))
            {
                if (!usuarioDto.Nome.Any(char.IsUpper))
                {
                    return Results.BadRequest(new BaseResponse("O nome deve começar com pelo menos uma letra maiúscula"));
                }
                usuario.Nome = usuarioDto.Nome;
            }
            if (!string.IsNullOrEmpty(usuarioDto.Login))
            {
                // Adicionar lógica para verificar se o novo login já existe, se necessário
                usuario.Login = usuarioDto.Login;
            }
            if (usuarioDto.Imagem != null) // Permite atualizar ou manter a imagem
            {
                usuario.Imagem = usuarioDto.Imagem;
            }

            try
            {
                await context.SaveChangesAsync();
                return Results.Ok(new BaseResponse("Informações do usuário atualizadas com sucesso!"));
            }
            catch (DbUpdateException ex)
            {
                // Logar o erro específico
                Console.WriteLine($"Erro ao atualizar usuário: {ex.Message}");
                return Results.BadRequest( new BaseResponse("Erro ao atualizar informações do usuário."));
            }
        }).RequireAuthorization().WithTags("Usuário");


        #endregion

        #region Evento
        // Endpoint para adicionar evento
        app.MapPost("/evento/adicionar", async (
     ConnectMataoContext context,
     [FromForm] EventoAdicionarDto eventoDto,
     ClaimsPrincipal user) =>
        {
            var evento = new Evento(
                eventoDto.Titulo,
                eventoDto.Descricao,
                eventoDto.Cep,
                eventoDto.Logradouro,
                eventoDto.Numero,
                eventoDto.Bairro,
                eventoDto.Telefone,
                eventoDto.Email,
                eventoDto.Data,
                eventoDto.Categoriaid,
                eventoDto.FlagAprovado,
                eventoDto.UsuarioParceiroid,
                eventoDto.Horario,
                eventoDto.FaixaEtaria,
                eventoDto.Whatsapp
            );

            context.Set<Evento>().Add(evento);
            await context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(eventoDto.Imagem))
            {
                context.EventoImagemSet.Add(new EventoImagens
                {
                    Id = Guid.NewGuid(),
                    EventoId = evento.Id,
                    Imagem = eventoDto.Imagem  
                });
                await context.SaveChangesAsync();
            }

            return Results.Created("Created", new BaseResponse("Evento adicionado com sucesso!"));
        })
 .RequireAuthorization()
 .WithTags("Evento");




        // Endpoint para remover evento
        app.MapDelete("/evento/remover/{id}", async (ConnectMataoContext context, Guid id, ClaimsPrincipal user) =>
        {
            var evento = await context.Set<Evento>().FindAsync(id);
            if (evento == null)
            {
                return Results.NotFound(new BaseResponse("Evento não encontrado"));
            }

            var perfil = user.FindFirst(ClaimTypes.Role)?.Value;

            if (perfil == EnumPerfil.Administrador.ToString() || (perfil == EnumPerfil.Parceiro.ToString() && evento.UsuarioParceiroid == new Guid(user.FindFirst(ClaimTypes.NameIdentifier)?.Value)))
            {
                context.Set<Evento>().Remove(evento);
                await context.SaveChangesAsync();
                return Results.Ok(new BaseResponse("Evento removido com Sucesso!"));
            }

            return Results.Forbid();
        }).RequireAuthorization().WithTags("Evento");

        // Endpoint para listar eventos
        app.MapGet("/evento/listar", async (ConnectMataoContext context) =>
        {
            var eventos = await context.Set<Evento>()
                .Include(e => e.UsuarioParceiro)
                 .Include(e => e.EventoImagens)
                .Select(e => new
                {
                    e.Id,
                    e.Titulo,
                    e.Descricao,
                    e.Cep,
                    e.Logradouro,
                    e.Numero,
                    e.Bairro,
                    e.Telefone,
                    e.Email,
                    e.Data,
                    e.Horario,
                    e.FaixaEtaria,
                    e.FlagAprovado,
                    e.UsuarioParceiroid,
                    e.Categoriaid,
                    UsuarioNome = e.UsuarioParceiro.Nome,
                    UsuarioImagem = e.UsuarioParceiro.Imagem,
                    EventoImagem = e.EventoImagens.Select(img => img.Imagem).ToList()
                }).ToListAsync();

            return Results.Ok(eventos);
        }).WithTags("Evento");

        // Endpoint para listar eventos de um usuário específico
        app.MapGet("/evento/listar/usuario/{usuarioId:guid}", async (ConnectMataoContext context, Guid usuarioId) =>
        {
            var eventosDoUsuario = await context.Set<Evento>()
                .Where(e => e.UsuarioParceiroid == usuarioId)
                .Include(e => e.UsuarioParceiro) // Inclui a entidade Usuario
                .Select(e => new
                {
                    e.Id,
                    e.Titulo,
                    e.Descricao,
                    Data = e.Data,
                    e.Horario,
                    Endereco = e.Logradouro,
                    e.Bairro,
                    e.Numero,
                    e.Logradouro,
                    e.Cep,
                    e.Email,
                    e.Telefone,
                    e.Whatsapp,
                    e.FaixaEtaria,
                    UsuarioParceiroid = e.UsuarioParceiroid.ToString(),
                    Categoriaid = e.Categoriaid.ToString(),
                    UsuarioNome = e.UsuarioParceiro.Nome, // Acessa o nome do usuário
                    UsuarioImagem = e.UsuarioParceiro.Imagem // Acessa a imagem do usuário
                })
                .ToListAsync();

            return Results.Ok(eventosDoUsuario);
        }).WithTags("Evento");


        app.MapGet("evento/{eventoId:guid}/imagens", async (Guid eventoId, ConnectMataoContext context) =>
        {
            var imagens = await context.EventoImagemSet
                .Where(ei => ei.EventoId == eventoId)
                .Select(ei => new EventoImagemListarDto
                {
                    Id = ei.Id,
                    Imagem = ei.Imagem,
                    EventoId = ei.EventoId
                })
                .ToListAsync();

            if (imagens == null || !imagens.Any())
            {
                return Results.NotFound(new BaseResponse("Nenhuma imagem encontrada para este evento."));
            }

            return Results.Ok(imagens);
        }).WithTags("Evento");

        app.MapGet("/evento/{id:guid}/detalhe", async (ConnectMataoContext context, Guid id) =>
        {
            var evento = await context.Set<Evento>()
                .Where(e => e.Id == id)
                .Include(e => e.UsuarioParceiro)
                .Include(e => e.EventoImagens) // Correctly include the collection of images
                .Select(e => new EventoDetalheDto
                {
                    Id = e.Id,
                    Titulo = e.Titulo,
                    Descricao = e.Descricao,
                    Cep = e.Cep,
                    Logradouro = e.Logradouro,
                    Numero = e.Numero,
                    Bairro = e.Bairro,
                    Telefone = e.Telefone,
                    Email = e.Email,
                    Data = e.Data,
                    Horario = e.Horario,
                    FaixaEtaria = e.FaixaEtaria,
                    FlagAprovado = e.FlagAprovado,
                    UsuarioParceiroid = e.UsuarioParceiroid,
                    Categoriaid = e.Categoriaid,
                    UsuarioNome = e.UsuarioParceiro.Nome,
                    UsuarioImagem = e.UsuarioParceiro.Imagem,
                    Imagens = e.EventoImagens.Select(img => new EventoImagemDto { Imagem = img.Imagem }).ToList(),
                    Whatsapp = e.Whatsapp
                })
                .FirstOrDefaultAsync();

            if (evento == null)
            {
                return Results.NotFound(new BaseResponse("Evento não encontrado."));
            }

            return Results.Ok(evento);
        }).WithTags("Evento");


        // Endpoint para dar Like em um evento
        app.MapPost("/eventos/{eventoId:guid}/likes", async (Guid eventoId, ConnectMataoContext context, ClaimsPrincipal user) =>
        {
            var usuarioIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return Results.Unauthorized();
            }

            var evento = await context.Set<Evento>().FindAsync(eventoId);
            if (evento == null)
            {
                return Results.NotFound(new BaseResponse("Evento não encontrado."));
            }

            var x = new EventoEstatisticas
            {
                Id = Guid.NewGuid(),
                Eventoid = eventoId,
                Usuarioid = usuarioId,
                TipoEstatistica = EnumTipoEstatistica.Like
            };

            context.EventoEstatisticaSet.Add(x);

            await context.SaveChangesAsync();
            return Results.Ok(new BaseResponse("Like adicionado ao evento."));
        }).RequireAuthorization().WithTags("Evento");

        // Endpoint para dar Deslike em um evento
        app.MapPost("/eventos/{eventoId:guid}/deslikes", async (Guid eventoId, ConnectMataoContext context, ClaimsPrincipal user) =>
        {
            var usuarioIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(usuarioIdClaim, out var usuarioId))
            {
                return Results.Unauthorized();
            }

            var evento = await context.Set<Evento>().FindAsync(eventoId);
            if (evento == null)
            {
                return Results.NotFound(new BaseResponse("Evento não encontrado."));
            }

            context.EventoEstatisticaSet.Add(new EventoEstatisticas
            {
                Id = Guid.NewGuid(),
                Eventoid = eventoId,
                Usuarioid = usuarioId,
                TipoEstatistica = EnumTipoEstatistica.Deslike
            });

            await context.SaveChangesAsync();
            return Results.Ok(new BaseResponse("Deslike adicionado ao evento."));
        }).RequireAuthorization().WithTags("Evento");

        // Endpoint para obter estatísticas de um evento (likes e deslikes)
        app.MapGet("/eventos/{eventoId:guid}/estatisticas", async (Guid eventoId, ConnectMataoContext context) =>
        {
            var evento = await context.Set<Evento>().FindAsync(eventoId);
            if (evento == null)
            {
                return Results.NotFound(new BaseResponse("Evento não encontrado."));
            }

            var likes = await context.EventoEstatisticaSet
                .CountAsync(es => es.Eventoid == eventoId && es.TipoEstatistica == EnumTipoEstatistica.Like);

            var deslikes = await context.EventoEstatisticaSet
                .CountAsync(es => es.Eventoid == eventoId && es.TipoEstatistica == EnumTipoEstatistica.Deslike);

            return Results.Ok(new
            {
                likes = likes,
                deslikes = deslikes
            });
        }).WithTags("Evento");
        #endregion

        #region controller autenticação

        app.MapPost("autenticar", async (ConnectMataoContext context, LoginDto loginDto) =>
        {
            if (string.IsNullOrEmpty(loginDto.Login) || string.IsNullOrEmpty(loginDto.Senha))
            {
                return Results.BadRequest(new BaseResponse("Login e senha são obrigatórios."));
            }

            var usuario = await context.Set<Usuario>().FirstOrDefaultAsync(u => u.Login == loginDto.Login);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(loginDto.Senha, usuario.Senha))
            {
                return Results.BadRequest(new BaseResponse("Usuário ou senha inválidos."));
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Role, usuario.Perfil.ToString()),
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("aacd9108-22d7-4ef5-9296-a2c5923fdf5d"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "connect.m",
                audience: "connect.m",
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            return Results.Ok(new
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = DateTime.Now.AddDays(1),
                Role = usuario.Perfil.ToString()
            });
        }).WithTags("Autenticação");

        #endregion

        app.Run();
    }
}