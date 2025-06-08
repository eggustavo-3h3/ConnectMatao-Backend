using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using connectMatao.Domain.DTOs.Base;
using connectMatao.Domain.DTOs.Categoria;
using connectMatao.Domain.DTOs.Evento;
using connectMatao.Domain.DTOs.Login;
using connectMatao.Domain.DTOs.Signup;
using connectMatao.Domain.DTOs.Usuario;
using connectMatao.Domain.Entities;
using connectMatao.Enumerator;
using connectMatao.Infra.Data.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Startly.Domain.DTOs.ResetSenha;
using Startly.Infra.Email;

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

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
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
            options.AddPolicy("Usuario", policy => policy.RequireRole("Usuario", "Administrador"));
            options.AddPolicy("Parceiro", policy => policy.RequireRole("Parceiro", "Administrador"));
            options.AddPolicy("Administrador", policy => policy.RequireRole("Administrador"));
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

        app.MapDelete("categoria/excluir/{id}", (ConnectMataoContext context, Guid id) =>
        {
            var categoria = context.CategoriaSet.FirstOrDefault(c => c.Id == id);

            if (categoria == null)
            {
                return Results.NotFound(new BaseResponse("Categoria não encontrada."));
            }

            context.CategoriaSet.Remove(categoria);
            context.SaveChanges();

            return Results.Ok(new BaseResponse("Categoria excluída com sucesso!"));
        }).RequireAuthorization("Parceiro").WithTags("Categoria");
        #endregion

        #region Usuario

        app.MapPost("/usuario/cadastrar", async (ConnectMataoContext context, UsuarioAdicionarDto usuarioDto) =>
        {
            var resultado = await new UsuarioAdicionarDtoValidator().ValidateAsync(usuarioDto);

            if (!resultado.IsValid)
                return Results.BadRequest(resultado.Errors.Select(error => error.ErrorMessage));

            var usuariosExistentes = await context.Set<Usuario>()
                .CountAsync(u => u.Login == usuarioDto.Login);

            if (usuariosExistentes >= 1)
                return Results.BadRequest(new { mensagem = "Já existe uma conta cadastrada com este e-mail." });

            var usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                Nome = usuarioDto.Nome,
                Login = usuarioDto.Login,
                Senha = BCrypt.Net.BCrypt.HashPassword(usuarioDto.Senha),
                Imagem = usuarioDto.Imagem,
                Perfil = usuarioDto.Perfil
            };

            context.Set<Usuario>().Add(usuario);

            await context.SaveChangesAsync();

            return Results.Created("Created", new BaseResponse("Usuário cadastrado com sucesso!"));
        }).WithTags("Usuário");

        app.MapGet("/usuario/listar", async (ConnectMataoContext context) =>
        {
            var usuarios = await context.Set<Usuario>()
                .Select(u => new UsuarioListarComStatusParceiroDto
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Login = u.Login,
                    Imagem = u.Imagem,
                    Perfil = u.Perfil.ToString()
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

        app.MapDelete("/usuario/{id:guid}", async (ConnectMataoContext context, Guid id) =>
        {
            var usuario = await context.Set<Usuario>().FindAsync(id);

            if (usuario == null)
                return Results.NotFound("Usuário não encontrado.");

            context.Set<Usuario>().Remove(usuario);
            await context.SaveChangesAsync();

            return Results.Ok(new BaseResponse("Usuário removido com sucesso!"));
        }).RequireAuthorization().WithTags("Usuário");

        app.MapPut("/usuario/alterar-senha", (ConnectMataoContext context, AlterarSenhaDto dto, ClaimsPrincipal claims) =>
        {
            var validator = new AlterarSenhaDtoValidator();

            var validationResult = validator.Validate(dto);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                return Results.BadRequest(new { errors });
            }

            var userIdClaim = claims.FindFirst("Id")?.Value;

            if (!Guid.TryParse(userIdClaim, out var usuarioId))
                return Results.Unauthorized();

            var usuario = context.UsuarioSet.Find(usuarioId);
            if (usuario == null)
                return Results.NotFound("Usuário não encontrado.");

            if (!BCrypt.Net.BCrypt.Verify(dto.SenhaAtual, usuario.Senha))
                return Results.BadRequest("Senha atual incorreta.");

            usuario.Senha = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);
            context.SaveChanges();

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

        #endregion

        #region Parceiro

        app.MapGet("/parceiro/pendentes", async (ConnectMataoContext context) =>
        {
            var parceirosPendentes = await context.ParceiroSet
                .Include(p => p.Usuario)
                .Where(p => p.FlagAprovado == false && p.DataEnvio != DateTime.MinValue)
                .Select(p => new FormUsuarioParceiroListarDto
                {
                    Id = p.Id,
                    UsuarioId = p.UsuarioId,
                    NomeCompleto = p.NomeCompleto,
                    Cpf = p.Cpf,
                    Telefone = p.Telefone,
                    FlagAprovado = p.FlagAprovado,
                    DataEnvio = p.DataEnvio.Kind == DateTimeKind.Utc ? p.DataEnvio : p.DataEnvio.ToUniversalTime(),
                    NomeUsuario = p.Usuario != null ? p.Usuario.Nome : "N/A",
                    LoginUsuario = p.Usuario != null ? p.Usuario.Login : "N/A"
                })
                .ToListAsync();

            return Results.Ok(parceirosPendentes);

        }).WithTags("Parceiro").RequireAuthorization("Administrador");


        app.MapGet("/parceiro/status-cadastro", async (ConnectMataoContext context, ClaimsPrincipal claims) =>
        {
            var userIdClaim = claims.FindFirst("Id")?.Value;
            if (userIdClaim == null)
                return Results.Unauthorized();

            var userId = Guid.Parse(userIdClaim);

            var parceiro = await context.ParceiroSet.FirstOrDefaultAsync(p => p.UsuarioId == userId);

            bool formParceiroExiste = parceiro != null;

            if (parceiro == null)
            {
                return Results.Ok(new FormUsuarioParceiroDto
                {
                    NomeCompleto = string.Empty,
                    CPF = string.Empty,
                    Telefone = string.Empty,
                    FlagAprovadoParceiro = false,
                    FormParceiroExiste = formParceiroExiste
                });
            }
            else
            {
                return Results.Ok(new FormUsuarioParceiroDto
                {
                    NomeCompleto = parceiro.NomeCompleto,
                    CPF = parceiro.Cpf,
                    Telefone = parceiro.Telefone,
                    FlagAprovadoParceiro = parceiro.FlagAprovado,
                    FormParceiroExiste = formParceiroExiste
                });
            }
        }).WithTags("Parceiro").RequireAuthorization("Parceiro");

        app.MapPut("/parceiro/completar-cadastro", (ConnectMataoContext context, ParceiroDto parceiroDto, ClaimsPrincipal claims) =>
        {
            var userIdClaim = claims.FindFirst("Id")?.Value;
            if (userIdClaim == null)
                return Results.Unauthorized();

            var userId = Guid.Parse(userIdClaim);

            var usuario = context.UsuarioSet.Find(userId);

            if (usuario == null)
                return Results.NotFound(new BaseResponse("Usuário não encontrado."));

            if (usuario.Perfil != EnumPerfil.Parceiro)
                return Results.BadRequest(new BaseResponse("Apenas usuários com perfil de Parceiro podem completar este cadastro."));

            var existingParceiro = context.ParceiroSet.FirstOrDefault(p => p.UsuarioId == userId);
            if (existingParceiro != null)
            {
                return Results.Conflict(new BaseResponse("Você já enviou um formulário de parceiro. Aguarde a aprovação ou entre em contato com o suporte."));
            }

            var parceiro = new Parceiro
            {
                Id = Guid.NewGuid(),
                UsuarioId = userId,
                NomeCompleto = parceiroDto.NomeCompleto,
                Cpf = parceiroDto.Cpf,
                Telefone = parceiroDto.Telefone,
                FlagAprovado = false,
                DataEnvio = DateTime.UtcNow
            };

            context.ParceiroSet.Add(parceiro);
            context.SaveChanges();
            return Results.Ok(new BaseResponse("Cadastro de parceiro enviado para aprovação com sucesso!"));
        }).RequireAuthorization("Parceiro").WithTags("Parceiro");

        app.MapPut("/parceiro/aprovar/{id:guid}", (ConnectMataoContext context, Guid id) =>
        {
            var parceiro = context.ParceiroSet.Find(id);
            if (parceiro == null)
                return Results.NotFound(new BaseResponse("Parceiro não encontrado."));

            parceiro.FlagAprovado = true;
            context.SaveChanges();
            return Results.Ok(new BaseResponse("Cadastro de parceiro aprovado com sucesso!"));
        }).RequireAuthorization("Administrador").WithTags("Parceiro");

        app.MapPut("/parceiro/reprovar/{id:guid}", (ConnectMataoContext context, Guid id) =>
        {
            var parceiro = context.ParceiroSet.Find(id);
            if (parceiro == null)
                return Results.NotFound(new BaseResponse("Parceiro não encontrado."));

            parceiro.FlagAprovado = false;
            context.SaveChanges();
            return Results.Ok(new BaseResponse("Cadastro de parceiro reprovado com sucesso!"));
        }).RequireAuthorization("Administrador").WithTags("Parceiro");

        app.MapGet("/parceiro/status/{id:guid}", async (ConnectMataoContext context, Guid id) =>
        {
            var parceiro = await context.ParceiroSet
                .FirstOrDefaultAsync(p => p.UsuarioId == id);

            bool isApprovedPartner = parceiro != null && parceiro.FlagAprovado;

            return Results.Ok(new { userId = id, isPartner = isApprovedPartner });

        }).WithTags("Parceiro");

        #endregion

        #region Evento

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
        }).Accepts<EventoAdicionarDto>("application/json")
            .Produces<BaseResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .RequireAuthorization()
            .WithTags("Evento");

        app.MapDelete("/evento/remover/{id}", (ConnectMataoContext context, Guid id) =>
        {
            var evento = context.Set<Evento>().Find(id);
            if (evento == null)
                return Results.NotFound(new BaseResponse("Evento não encontrado"));

            context.Set<Evento>().Remove(evento);
            context.SaveChanges();

            return Results.Ok(new BaseResponse("Evento removido com Sucesso!"));
        }).RequireAuthorization().WithTags("Evento");

        app.MapGet("/evento/listar", async (ConnectMataoContext context )=>
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


        app.MapGet("/evento/listar/destaque", async (ConnectMataoContext context, [FromQuery] int limite = 8) =>
        {
            var eventosDestaque = await context.Set<Evento>()
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
                    EventoImagem = e.EventoImagens.Select(img => img.Imagem).ToArray(),
                    Curtidas = context.Set<EventoEstatisticas>()
                                        .Count(est => est.Eventoid == e.Id && est.TipoEstatistica == EnumTipoEstatistica.Like)
                })
                .OrderByDescending(e => e.Curtidas)
                .Take(limite)
                .ToListAsync();

            return Results.Ok(eventosDestaque);
        }).WithTags("Evento");

        app.MapGet("/evento/listar/titulo", async (ConnectMataoContext context, [FromQuery] string? titulo) =>
        {
            var query = context.Set<Evento>()
                .Include(e => e.UsuarioParceiro)
                .Include(e => e.EventoImagens)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(titulo))
            {
                query = query.Where(e => e.Titulo.Contains(titulo));
            }

            var eventos = await query
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
                })
                .ToListAsync();

            return Results.Ok(eventos);
        }).WithTags("Evento");

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

        app.MapGet("/evento/{id:guid}/detalhe", async (Guid id, ConnectMataoContext context, ClaimsPrincipal user) =>
        {
            var evento = await context.Set<Evento>()
                .Include(e => e.UsuarioParceiro)
                .Include(e => e.EventoImagens)
                  .Include(e => e.Categoria)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento == null)
                return Results.NotFound(new BaseResponse("Evento não encontrado."));

            var stats = await context.EventoEstatisticaSet
                .Where(es => es.Eventoid == id)
                .GroupBy(es => es.TipoEstatistica)
                .Select(g => new { Tipo = g.Key, Qt = g.Count() })
                .ToListAsync();

            var likes = stats.FirstOrDefault(s => s.Tipo == EnumTipoEstatistica.Like)?.Qt ?? 0;
            var deslikes = stats.FirstOrDefault(s => s.Tipo == EnumTipoEstatistica.Deslike)?.Qt ?? 0;

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
                Imagens = evento.EventoImagens.Select(img => img.Imagem).ToArray(), 
                Likes = likes,
                Deslikes = deslikes,
                UsuarioInteragiu = usuarioInteragiu,
                CategoriaNome = evento.Categoria?.Descricao
            };

            return Results.Ok(dto);
        }).WithTags("Evento");

        app.MapPost("/eventos/{eventoId:guid}/likes", async (Guid eventoId, ConnectMataoContext context, ClaimsPrincipal claims) =>
        {
            var userIdClaim = claims.FindFirst("Id")?.Value;
            if (userIdClaim == null)
                return Results.Unauthorized();

            var userId = Guid.Parse(userIdClaim);

            var evt = await context.EventoSet.FindAsync(eventoId);
            if (evt == null)
                return Results.NotFound(new BaseResponse("Evento não encontrado."));

            var existingInteraction = await context.EventoEstatisticaSet
                .FirstOrDefaultAsync(p => p.Eventoid == eventoId && p.Usuarioid == userId);

            if (existingInteraction != null)
            {
                if (existingInteraction.TipoEstatistica == EnumTipoEstatistica.Like)
                {
                    context.EventoEstatisticaSet.Remove(existingInteraction);
                    await context.SaveChangesAsync();
                    return Results.Ok(new BaseResponse("Like removido com sucesso."));
                }
                else
                {
                    context.EventoEstatisticaSet.Remove(existingInteraction);
                    context.EventoEstatisticaSet.Add(new EventoEstatisticas
                    {
                        Id = Guid.NewGuid(),
                        Eventoid = eventoId,
                        Usuarioid = userId,
                        TipoEstatistica = EnumTipoEstatistica.Like
                    });
                    await context.SaveChangesAsync();
                    return Results.Ok(new BaseResponse("Deslike removido e like adicionado com sucesso."));
                }
            }
            else
            {
                context.EventoEstatisticaSet.Add(new EventoEstatisticas
                {
                    Id = Guid.NewGuid(),
                    Eventoid = eventoId,
                    Usuarioid = userId,
                    TipoEstatistica = EnumTipoEstatistica.Like
                });
                await context.SaveChangesAsync();
                return Results.Ok(new BaseResponse("Like adicionado com sucesso."));
            }
        }).RequireAuthorization().WithTags("Evento");


        app.MapPost("/eventos/{eventoId:guid}/deslikes", async (Guid eventoId, ConnectMataoContext context, ClaimsPrincipal claims) =>
        {
            var userIdClaim = claims.FindFirst("Id")?.Value;
            if (userIdClaim == null)
                return Results.Unauthorized();

            var userId = Guid.Parse(userIdClaim);

            var evt = await context.EventoSet.FindAsync(eventoId);
            if (evt == null)
                return Results.NotFound(new BaseResponse("Evento não encontrado."));

            var existingInteraction = await context.EventoEstatisticaSet
                .FirstOrDefaultAsync(p => p.Eventoid == eventoId && p.Usuarioid == userId);

            if (existingInteraction != null)
            {
                if (existingInteraction.TipoEstatistica == EnumTipoEstatistica.Deslike)
                {
                    context.EventoEstatisticaSet.Remove(existingInteraction);
                    await context.SaveChangesAsync();
                    return Results.Ok(new BaseResponse("Deslike removido com sucesso."));
                }
                else
                {
                    context.EventoEstatisticaSet.Remove(existingInteraction);
                    context.EventoEstatisticaSet.Add(new EventoEstatisticas
                    {
                        Id = Guid.NewGuid(),
                        Eventoid = eventoId,
                        Usuarioid = userId,
                        TipoEstatistica = EnumTipoEstatistica.Deslike
                    });
                    await context.SaveChangesAsync();
                    return Results.Ok(new BaseResponse("Like removido e deslike adicionado com sucesso."));
                }
            }
            else
            {
                context.EventoEstatisticaSet.Add(new EventoEstatisticas
                {
                    Id = Guid.NewGuid(),
                    Eventoid = eventoId,
                    Usuarioid = userId,
                    TipoEstatistica = EnumTipoEstatistica.Deslike
                });
                await context.SaveChangesAsync();
                return Results.Ok(new BaseResponse("Deslike adicionado com sucesso."));
            }
        }).RequireAuthorization().WithTags("Evento");


        app.MapDelete("/eventos/{eventoId:guid}/likes", async (Guid eventoId, ConnectMataoContext context, ClaimsPrincipal claims) =>
        {
            var userIdClaim = claims.FindFirst("Id")?.Value;
            if (userIdClaim == null)
                return Results.Unauthorized();

            var userId = Guid.Parse(userIdClaim);

            var stat = await context.EventoEstatisticaSet
                .FirstOrDefaultAsync(es => es.Eventoid == eventoId
                                         && es.Usuarioid == userId
                                         && es.TipoEstatistica == EnumTipoEstatistica.Like);
            if (stat == null)
                return Results.NotFound();

            context.EventoEstatisticaSet.Remove(stat);
            await context.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization().WithTags("Evento");


        app.MapDelete("/eventos/{eventoId:guid}/deslikes", async (Guid eventoId, ConnectMataoContext context, ClaimsPrincipal claims) =>
        {
            var userIdClaim = claims.FindFirst("Id")?.Value;
            if (userIdClaim == null)
                return Results.Unauthorized();

            var userId = Guid.Parse(userIdClaim);

            var stat = await context.EventoEstatisticaSet
                .FirstOrDefaultAsync(es => es.Eventoid == eventoId
                                         && es.Usuarioid == userId
                                         && es.TipoEstatistica == EnumTipoEstatistica.Deslike);

            if (stat == null)
                return Results.NotFound();

            context.EventoEstatisticaSet.Remove(stat);
            await context.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization().WithTags("Evento");


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
        }).WithTags("Evento");

        #endregion

        #region Segurança

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
                UsuarioId = usuario.Id
            });
        }).WithTags("Segurança");

        app.MapPost("gerar-chave-reset-senha", (ConnectMataoContext context, GerarResetSenhaDto gerarResetSenhaDto) =>
        {
            var resultado = new GerarResetSenhaDtoValidator().Validate(gerarResetSenhaDto);
            if (!resultado.IsValid)
                return Results.BadRequest(resultado.Errors.Select(error => error.ErrorMessage));

            var usuario = context.UsuarioSet.FirstOrDefault(p => p.Login == gerarResetSenhaDto.Email);

            if (usuario is not null)
            {
                usuario.ChaveResetSenha = Guid.NewGuid();
                context.UsuarioSet.Update(usuario);
                context.SaveChanges();

                var emailService = new EmailService();
                var enviarEmailResponse = emailService.EnviarEmail(gerarResetSenhaDto.Email, "Reset de Senha", $"http://localhost:4200/reset-senha/{usuario.ChaveResetSenha}", true);
                if (!enviarEmailResponse.Sucesso)
                    return Results.BadRequest(new BaseResponse("Erro ao enviar o e-mail: " + enviarEmailResponse.Mensagem));
            }

            return Results.Ok(new BaseResponse("Se o e-mail informado estiver correto, você receberá as instruções por e-mail."));
        }).WithTags("Segurança");

        app.MapPut("resetar-senha", (ConnectMataoContext context, ResetSenhaDto resetSenhaDto) =>
        {
            var resultado = new ResetSenhaDtoValidator().Validate(resetSenhaDto);
            if (!resultado.IsValid)
                return Results.BadRequest(resultado.Errors.Select(error => error.ErrorMessage));

            var usuario = context.UsuarioSet.FirstOrDefault(p => p.ChaveResetSenha == resetSenhaDto.ChaveResetSenha);

            if (usuario is null)
                return Results.BadRequest(new BaseResponse("Chave de reset de senha inválida."));

            usuario.Senha = BCrypt.Net.BCrypt.HashPassword(resetSenhaDto.NovaSenha);
            usuario.ChaveResetSenha = null;
            context.UsuarioSet.Update(usuario);
            context.SaveChanges();

            return Results.Ok(new BaseResponse("Senha alterada com sucesso."));
        }).WithTags("Segurança");

        app.MapPut("alterar-senha", (ConnectMataoContext context, ClaimsPrincipal claims, AlterarSenhaDto alterarSenhaDto) =>
        {
            var resultado = new AlterarSenhaDtoValidator().Validate(alterarSenhaDto);
            if (!resultado.IsValid)
                return Results.BadRequest(resultado.Errors.Select(error => error.ErrorMessage));

            var userIdClaim = claims.FindFirst("Id")?.Value;
            if (userIdClaim == null)
                return Results.Unauthorized();

            var userId = Guid.Parse(userIdClaim);
            var usuario = context.UsuarioSet.FirstOrDefault(p => p.Id == userId);
            if (usuario == null)
                return Results.NotFound(new BaseResponse("Usuário não encontrado."));

            usuario.Senha = BCrypt.Net.BCrypt.HashPassword(alterarSenhaDto.NovaSenha);
            context.UsuarioSet.Update(usuario);
            context.SaveChanges();

            return Results.Ok(new BaseResponse("Senha alterada com sucesso."));
        }).WithTags("Segurança");

        app.MapPost("signup", (ConnectMataoContext context, SignupDto signupDto) =>
        {
            var resultado = new SignupDtoValidator().Validate(signupDto);
            if (!resultado.IsValid)
                return Results.BadRequest(resultado.Errors.Select(error => error.ErrorMessage));

            var usuario = context.UsuarioSet.FirstOrDefault(u => u.Perfil == EnumPerfil.Administrador);
            if (usuario is not null)
                return Results.BadRequest(new BaseResponse("Já existe um usuário administrador cadastrado."));

            usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                Nome = signupDto.Nome,
                Login = signupDto.Login,
                Senha = BCrypt.Net.BCrypt.HashPassword(signupDto.Senha),
                Imagem = string.Empty,
                Perfil = EnumPerfil.Administrador
            };

            context.UsuarioSet.Add(usuario);
            context.SaveChanges();

            return Results.Created("Created", new BaseResponse("Usuário Administrador cadastrado com sucesso!"));
        }).WithTags("Segurança");

        #endregion

        app.Run();
    }
}