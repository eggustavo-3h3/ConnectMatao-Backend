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
    using Microsoft.AspNetCore.Mvc;

    internal class Program
    {
        private static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


            builder.Services.AddDbContext<ConnectMataoContext>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddCors();
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

        app.MapGet("/usuario/{usuarioId:guid}/imagem", async (Guid usuarioId, ConnectMataoContext context) =>
        {
            // Buscando o usuário diretamente na tabela de Usuario, pois a classe Usuario é que tem o campo Imagem
            var usuario = await context.Set<Usuario>()
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
            {
                return Results.NotFound(new BaseResponse("Usuário não encontrado."));
            }

            // Retorna a imagem do usuário
            return Results.Ok(new { usuario.Imagem });
        }).WithTags("Usuário");

        #endregion

        #region Evento
        // Endpoint para adicionar evento
        app.MapPost("/evento/adicionar",
          (ConnectMataoContext context, EventoAdicionarDto eventoDto, ClaimsPrincipal user) =>
          {
              // 1) Cria a entidade Evento
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

              // 2) Itera sobre a lista de base64 e persiste cada imagem
              if (eventoDto.Imagens != null && eventoDto.Imagens.Any())
              {
                  foreach (var base64 in eventoDto.Imagens)
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

              // 3) Salva no banco
              context.SaveChanges();

              // 4) Retorna 201 com a localização do recurso
              return Results.Created($"/evento/{evento.Id}", new BaseResponse("Evento adicionado com sucesso!"));
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

        app.MapGet("/evento/{id:guid}/detalhe", async (Guid id, ConnectMataoContext context, ClaimsPrincipal user) =>
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
        app.MapPost("/eventos/{eventoId:guid}/likes", async (Guid eventoId, ConnectMataoContext context, ClaimsPrincipal user) =>
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
            .RequireAuthorization()
            .WithTags("Evento");

            // DELETE LIKE
            app.MapDelete("/eventos/{eventoId:guid}/likes", async (Guid eventoId, ConnectMataoContext context, ClaimsPrincipal user) =>
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
            .RequireAuthorization()
            .WithTags("Evento");

            // POST DESLIKE
            app.MapPost("/eventos/{eventoId:guid}/deslikes", async (Guid eventoId, ConnectMataoContext context, ClaimsPrincipal user) =>
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
            .RequireAuthorization()
            .WithTags("Evento");

            // DELETE DESLIKE
            app.MapDelete("/eventos/{eventoId:guid}/deslikes", async (Guid eventoId, ConnectMataoContext context, ClaimsPrincipal user) =>
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
            .RequireAuthorization()
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
                    Role = usuario.Perfil.ToString(),
                    Imagem = usuario.Imagem,
                    UsuarioId = usuario.Id
                });
            }).WithTags("Autenticação");

            #endregion

            app.Run();
        }
    }