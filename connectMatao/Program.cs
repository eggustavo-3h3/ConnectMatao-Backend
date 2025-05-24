using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using connectMatao.Data.Context;
using connectMatao.Domain.DTOs.Base;
using connectMatao.Domain.DTOs.Categoria;
using connectMatao.Domain.DTOs.Evento;
using connectMatao.Domain.DTOs.Login;
using connectMatao.Domain.DTOs.Usuario;
using connectMatao.Domain.Entities;
using connectMatao.Enumerator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace connectMatao;

internal class Program
{
    private static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<ConnectMataoContext>();
        builder.Services.AddCors();
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
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes("aacd9108-22d7-4ef5-9296-a2c5923fdf5d"))
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("Usuario", policy => policy.RequireRole("Usuario", "Admin"));
            options.AddPolicy("Parceiro", policy => policy.RequireRole("Parceiro", "Admin"));
            options.AddPolicy("User",
                policy => policy.RequireRole("User", "Admin")); // Admin também pode acessar rotas de usuário

            options.AddPolicy("UsuarioAutenticado", policy =>
       policy.RequireRole("Usuario", "Parceiro", "User", "Admin"));
        });

        WebApplication app = builder.Build();

        // Adicionando suporte ao CORS
        builder.Services.AddCors();

        app.UseSwagger();
        app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Connect Matão API v1"); });

        // Configuração de CORS
        app.UseCors(x => x
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
        );

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

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

        app.MapPost("categoria/adicionar", (ConnectMataoContext context, CategoriaAdicionarDto categoriaDto) =>
        {
            var resultado = new CategoriaAdicionarDtoValidator().Validate(categoriaDto);

            if (!resultado.IsValid)
                return Results.BadRequest(resultado.Errors.Select(error => error.ErrorMessage));

            context.CategoriaSet.Add(new Categoria
            {
                Id = Guid.NewGuid(),
                Descricao = categoriaDto.Descricao
            });

            context.SaveChanges();

            return Results.Created("Created", new BaseResponse("Categoria Registrada com Sucesso!"));
        }).RequireAuthorization("Parceiro").WithTags("Categoria");

        #endregion

        #region Usuario

        app.MapPost("/usuario/cadastrar", async (ConnectMataoContext context, UsuarioAdicionarDto usuarioDto) =>
        {
            var resultado = await new UsuarioAdicionarDtoValidator().ValidateAsync(usuarioDto);

            if (!resultado.IsValid)
                return Results.BadRequest(resultado.Errors.Select(error => error.ErrorMessage));

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

        app.MapGet("/usuario/{id:guid}", async (ConnectMataoContext context, Guid id) =>
        {
            var usuario = await context.Set<Usuario>().FindAsync(id);

            if (usuario == null)
                return Results.NotFound();

            return Results.Ok(new
            {
                usuario.Id,
                usuario.Nome,
                usuario.Login,
                usuario.Imagem,
                usuario.Perfil
            });
        }).WithTags("Usuário");

        app.MapDelete("/usuario/{id}", async (ConnectMataoContext context, Guid id) =>
        {
            var usuario = await context.Set<Usuario>().FindAsync(id);

            if (usuario == null)
                return Results.NotFound("Usuário não encontrado.");

            context.Set<Usuario>().Remove(usuario);
            await context.SaveChangesAsync();

            return Results.Ok(new BaseResponse("Usuário removido com sucesso!"));
        }).RequireAuthorization().WithTags("Usuário");

        app.MapPut("/usuario/alterar-senha", async (
    ConnectMataoContext context,
    AlterarSenhaDto dto,
    ClaimsPrincipal claims) =>
        {
            var userIdClaim = claims.FindFirst("Id")?.Value;

            if (!Guid.TryParse(userIdClaim, out var usuarioId))
                return Results.Unauthorized();

            var usuario = await context.UsuarioSet.FindAsync(usuarioId);
            if (usuario == null)
                return Results.NotFound("Usuário não encontrado.");

            // Verifica se a senha atual está correta
            if (!BCrypt.Net.BCrypt.Verify(dto.SenhaAtual, usuario.Senha))
                return Results.BadRequest("Senha atual incorreta.");

            // Atualiza com a nova senha criptografada
            usuario.Senha = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);
            await context.SaveChangesAsync();

            return Results.Ok(new BaseResponse("Senha alterada com sucesso!"));
        })
.RequireAuthorization()
.WithTags("Usuário");


        app.MapPut("/usuario/atualizar", async (ConnectMataoContext context, UsuarioAtualizarDto usuarioAtualizarDto, ClaimsPrincipal claims) =>
        {
            var resultado = await new UsuarioAtualizarDtoValidator().ValidateAsync(usuarioAtualizarDto);

            if (!resultado.IsValid)
                return Results.BadRequest(resultado.Errors.Select(error => error.ErrorMessage));

            var userId = claims.FindFirst("Id")?.Value;
            if (!Guid.TryParse(userId, out var loggedInUserId))
                return Results.Unauthorized();

            var usuario = await context.UsuarioSet.FindAsync(loggedInUserId);

            if (usuario == null)
                return Results.NotFound("Usuário não encontrado.");

            // Atualiza os campos do usuário
            usuario.Nome = usuarioAtualizarDto.Nome;
            usuario.Login = usuarioAtualizarDto.Login;
            usuario.Imagem = usuarioAtualizarDto.Imagem;

            await context.SaveChangesAsync(); 
            return Results.Ok(new BaseResponse("Informações do usuário atualizadas com sucesso!"));
        }).RequireAuthorization().WithTags("Usuário");

        app.MapGet("/usuario/{usuarioId:guid}/imagem", async (Guid usuarioId, ConnectMataoContext context) =>
        {
            var usuario = await context.UsuarioSet.FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario == null)
                return Results.NotFound(new BaseResponse("Usuário não encontrado."));

            return Results.Ok(new { usuario.Imagem });
        }).WithTags("Usuário");

        // EndPoint para Recuperar senha  e enviar Email

        app.MapPost("/usuario/recuperar-senha", (
            RecuperarSenhaDTO dto,
            ConnectMataoContext context) =>
        {
            var usuario = context.UsuarioSet
                .FirstOrDefault(u => u.Login == dto.Login);

            if (usuario == null)
                return Results.NotFound("Usuário não encontrado.");

            usuario.ChaveReset = Guid.NewGuid();

            // Atualiza no banco
            context.UsuarioSet.Update(usuario);
            context.SaveChanges();

            return Results.Ok("Chave de recuperação gerada com sucesso.");
        });

        app.MapPut("/usuario/ResetSenha/{chave}", (
            Guid chave,
            RedefinirSenhaDto dto,
            ConnectMataoContext context) =>
        {
            var usuario = context.UsuarioSet
                .FirstOrDefault(u => u.ChaveReset == chave);

            if (usuario == null)
                return Results.NotFound("Chave de redefinição inválida ou expirada.");

            if (dto.NovaSenha != dto.ConfirmacaoSenha)
                return Results.BadRequest("A confirmação da senha não confere.");

            usuario.Senha = dto.NovaSenha;
            usuario.ChaveReset = Guid.Empty;

            context.UsuarioSet.Update(usuario);
            context.SaveChanges();

            return Results.Ok("Senha redefinida com sucesso.");
        });


        #endregion

        #region Evento

        // Endpoint para adicionar evento
        app.MapPost("/evento/adicionar", (ConnectMataoContext context, EventoAdicionarDto eventoAdicionarDto) =>
        {
            var resultado = new EventoAdicionarDtoValidator().Validate(eventoAdicionarDto);

            if (!resultado.IsValid)
                return Results.BadRequest(resultado.Errors.Select(error => error.ErrorMessage));

            var evento = new Evento(
                eventoAdicionarDto.Titulo,
                eventoAdicionarDto.Descricao,
                eventoAdicionarDto.Cep,
                eventoAdicionarDto.Logradouro,
                eventoAdicionarDto.Numero,
                eventoAdicionarDto.Bairro,
                eventoAdicionarDto.Telefone,
                eventoAdicionarDto.Email,
                eventoAdicionarDto.Data,
                eventoAdicionarDto.Categoriaid,
                eventoAdicionarDto.FlagAprovado,
                eventoAdicionarDto.UsuarioParceiroid,
                eventoAdicionarDto.Horario,
                eventoAdicionarDto.FaixaEtaria,
                eventoAdicionarDto.Whatsapp
            );

            context.EventoSet.Add(evento);
            
            if (eventoAdicionarDto.Imagens.Any())
            {
                foreach (var base64 in eventoAdicionarDto.Imagens)
                {
                    if (!string.IsNullOrWhiteSpace(base64))
                    {
                        context.EventoImagemSet.Add(new EventoImagens
                        {
                            Id = Guid.NewGuid(),
                            EventoId = evento.Id,
                            Imagem = base64
                        });
                    }
                }
            }

            context.SaveChanges();
            return Results.Created("Created", new BaseResponse("Evento adicionado com sucesso!"));
        })
        .Accepts<EventoAdicionarDto>("application/json")
        .Produces<BaseResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
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

            if (perfil == EnumPerfil.Administrador.ToString() || (perfil == EnumPerfil.Parceiro.ToString() &&
                                                                  evento.UsuarioParceiroid ==
                                                                  new Guid(user.FindFirst(ClaimTypes.NameIdentifier)
                                                                      ?.Value)))
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
                    EventoImagem = e.EventoImagens.Select(img => img.Imagem).ToArray()
                }).ToListAsync();

            return Results.Ok(eventos);
        }).WithTags("Evento");

        // Endpoint para listar eventos de um usuário específico
        app.MapGet("/evento/listar/usuario/{usuarioId:guid}", async (ConnectMataoContext context, Guid usuarioId) =>
        {
            var eventosDoUsuario = await context.Set<Evento>()
                .Where(e => e.UsuarioParceiroid == usuarioId)
                .Include(e => e.UsuarioParceiro)
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
                    UsuarioNome = e.UsuarioParceiro.Nome,
                    UsuarioImagem = e.UsuarioParceiro.Imagem
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

        app.MapGet("/evento/{id:guid}/detalhe",
                async (Guid id, ConnectMataoContext context, ClaimsPrincipal user) =>
                {
                    var evento = await context.Set<Evento>()
                        .Include(e => e.UsuarioParceiro)
                        .Include(e => e.EventoImagens)
                        .FirstOrDefaultAsync(e => e.Id == id);

                    if (evento == null)
                        return Results.NotFound(new BaseResponse("Evento não encontrado."));

                    // Estatísticas agregadas
                    var stats = await context.EventoEstatisticaSet
                        .Where(es => es.Eventoid == id)
                        .GroupBy(es => es.TipoEstatistica)
                        .Select(g => new { Tipo = g.Key, Qt = g.Count() })
                        .ToListAsync();

                    var likes = stats.FirstOrDefault(s => s.Tipo == EnumTipoEstatistica.Like)?.Qt ?? 0;
                    var deslikes = stats.FirstOrDefault(s => s.Tipo == EnumTipoEstatistica.Deslike)?.Qt ?? 0;

                    // Interação do usuário atual
                    var usuarioInteragiu = 0;
                    var uidClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (Guid.TryParse(uidClaim, out var uid))
                    {
                        usuarioInteragiu = await context.EventoEstatisticaSet
                            .Where(es => es.Eventoid == id && es.Usuarioid == uid)
                            .Select(es => (int)es.TipoEstatistica)
                            .FirstOrDefaultAsync();
                    }

                    var dto = new EventoDetalheDto
                    {
                        Id = evento.Id,
                        Titulo = evento.Titulo,
                        Descricao = evento.Descricao,
                        Cep = evento.Cep,
                        Logradouro = evento.Logradouro,
                        Numero = evento.Numero,
                        Bairro = evento.Bairro,
                        Telefone = evento.Telefone,
                        Email = evento.Email,
                        Data = evento.Data,
                        Horario = evento.Horario,
                        FaixaEtaria = evento.FaixaEtaria,
                        FlagAprovado = evento.FlagAprovado,
                        UsuarioParceiroid = evento.UsuarioParceiroid,
                        Categoriaid = evento.Categoriaid,
                        UsuarioNome = evento.UsuarioParceiro.Nome,
                        Whatsapp = evento.Whatsapp,
                        Imagens = evento.EventoImagens.Select(img => img.Imagem).ToArray(), // Alteração aqui
                        Likes = likes,
                        Deslikes = deslikes,
                        UsuarioInteragiu = usuarioInteragiu
                    };

                    return Results.Ok(dto);
                })
            .WithTags("Evento");


        // POST LIKE
        app.MapPost("/eventos/{eventoId:guid}/likes",
                async (Guid eventoId, ConnectMataoContext context, ClaimsPrincipal user) =>
                {
                    var uClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!Guid.TryParse(uClaim, out var uid)) return Results.Unauthorized();

                    var evt = await context.Set<Evento>().FindAsync(eventoId);
                    if (evt == null) return Results.NotFound(new BaseResponse("Evento não encontrado."));

                    context.EventoEstatisticaSet.Add(new EventoEstatisticas
                    {
                        Id = Guid.NewGuid(),
                        Eventoid = eventoId,
                        Usuarioid = uid,
                        TipoEstatistica = EnumTipoEstatistica.Like
                    });
                    await context.SaveChangesAsync();
                    return Results.Ok(new BaseResponse("Like adicionado ao evento."));
                })
               .RequireAuthorization("UsuarioAutenticado")
            .WithTags("Evento");

        // DELETE LIKE
        app.MapDelete("/eventos/{eventoId:guid}/likes",
                async (Guid eventoId, ConnectMataoContext context, ClaimsPrincipal user) =>
                {
                    var uClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!Guid.TryParse(uClaim, out var uid)) return Results.Unauthorized();

                    var stat = await context.EventoEstatisticaSet
                        .FirstOrDefaultAsync(es => es.Eventoid == eventoId
                                                   && es.Usuarioid == uid
                                                   && es.TipoEstatistica == EnumTipoEstatistica.Like);
                    if (stat == null) return Results.NotFound();

                    context.EventoEstatisticaSet.Remove(stat);
                    await context.SaveChangesAsync();
                    return Results.NoContent();
                })
         .RequireAuthorization("UsuarioAutenticado")
            .WithTags("Evento");

        // POST DESLIKE
        app.MapPost("/eventos/{eventoId:guid}/deslikes",
                async (Guid eventoId, ConnectMataoContext context, ClaimsPrincipal user) =>
                {
                    var uClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!Guid.TryParse(uClaim, out var uid)) return Results.Unauthorized();

                    var evt = await context.Set<Evento>().FindAsync(eventoId);
                    if (evt == null) return Results.NotFound(new BaseResponse("Evento não encontrado."));

                    context.EventoEstatisticaSet.Add(new EventoEstatisticas
                    {
                        Id = Guid.NewGuid(),
                        Eventoid = eventoId,
                        Usuarioid = uid,
                        TipoEstatistica = EnumTipoEstatistica.Deslike
                    });
                    await context.SaveChangesAsync();
                    return Results.Ok(new BaseResponse("Deslike adicionado ao evento."));
                })
                .RequireAuthorization("UsuarioAutenticado")
            .WithTags("Evento");

        // DELETE DESLIKE
        app.MapDelete("/eventos/{eventoId:guid}/deslikes",
                async (Guid eventoId, ConnectMataoContext context, ClaimsPrincipal user) =>
                {
                    var uClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!Guid.TryParse(uClaim, out var uid)) return Results.Unauthorized();

                    var stat = await context.EventoEstatisticaSet
                        .FirstOrDefaultAsync(es => es.Eventoid == eventoId
                                                   && es.Usuarioid == uid
                                                   && es.TipoEstatistica == EnumTipoEstatistica.Deslike);
                    if (stat == null) return Results.NotFound();

                    context.EventoEstatisticaSet.Remove(stat);
                    await context.SaveChangesAsync();
                    return Results.NoContent();
                })
          .RequireAuthorization("UsuarioAutenticado")
            .WithTags("Evento");

        // GET ESTATÍSTICAS
        app.MapGet("/eventos/{eventoId:guid}/estatisticas", async (Guid eventoId, ConnectMataoContext context) =>
            {
                var stats = await context.EventoEstatisticaSet
                    .Where(es => es.Eventoid == eventoId)
                    .GroupBy(es => es.TipoEstatistica)
                    .Select(g => new { Tipo = g.Key, Qt = g.Count() })
                    .ToListAsync();

                var res = new
                {
                    likes = stats.FirstOrDefault(s => s.Tipo == EnumTipoEstatistica.Like)?.Qt ?? 0,
                    deslikes = stats.FirstOrDefault(s => s.Tipo == EnumTipoEstatistica.Deslike)?.Qt ?? 0
                };
                return Results.Ok(res);
            })
            .WithTags("Evento");

        #endregion

        #region controller autenticação

        app.MapPost("autenticar", async (ConnectMataoContext context, LoginDto loginDto) =>
        {
            var resultado = await new LoginDtoValidator().ValidateAsync(loginDto);

            if (!resultado.IsValid)
                return Results.BadRequest(resultado.Errors.Select(error => error.ErrorMessage));

            var usuario = await context.UsuarioSet.FirstOrDefaultAsync(u => u.Login == loginDto.Login);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(loginDto.Senha, usuario.Senha))
                return Results.BadRequest(new BaseResponse("Usuário ou senha inválidos."));

            var claims = new[]
            {
                new Claim("Id", usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Role, usuario.Perfil.ToString())
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
                Role = usuario.Perfil.ToString(),
                Imagem = usuario.Imagem,
                UsuarioId = usuario.Id
            });
        }).WithTags("Autenticação");

        #endregion

        app.Run();
    }
}