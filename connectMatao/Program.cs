using connectMatao.Data.Context;
using connectMatao.Domain.DTOs.Base;
using connectMatao.Domain.DTOs.Categoria;
using connectMatao.Domain.DTOs.Evento;
using connectMatao.Domain.DTOs.Usuario;
using connectMatao.Domain.Entities;
using connectMatao.Enumerator;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Net.Mail;
using System.Net;
using System.Runtime.Intrinsics.X86;
using connectMatao.Domain.DTOs.Login;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

internal class Program
{
    private static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<ConnectMataoContext>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Connect Matão API", 
                Version = "v1",
                Description = "API para gerenciamento de eventos no Connect Matão"
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

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Connect Matão API v1");
            });
        }

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
        });


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
        }).RequireAuthorization();



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
        });

    

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
        });

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

            return Results.Ok( new BaseResponse("Usuário removido com sucesso!"));
        });

        #endregion

        #region Evento
        // Endpoint para adicionar evento
        app.MapPost("/evento/adicionar", async (ConnectMataoContext context, EventoAdicionarDto eventoDto, ClaimsPrincipal user) =>
        {
            var perfil = user.FindFirst(ClaimTypes.Role)?.Value;

            if (perfil != EnumPerfil.Parceiro.ToString() && perfil != EnumPerfil.Administrador.ToString())
            {
                return Results.Forbid(); 
            }

            var usuarioParceiro = await context.Set<Usuario>().FindAsync(eventoDto.UsuarioParceiroid);

            if (usuarioParceiro == null || usuarioParceiro.Perfil != EnumPerfil.Parceiro)
            {
                return Results.BadRequest(new BaseResponse("Usuário Parceiro Inválido. Apenas usuários parceiros podem criar eventos."));
            }

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

            return Results.Created("Created", new BaseResponse("Evento adicionado com sucesso!"));
        }).RequireAuthorization();

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
        }).RequireAuthorization();

        // Endpoint para listar eventos
        app.MapGet("/evento/listar", async (ConnectMataoContext context) =>
        {
            var eventos = await context.Set<Evento>().Select(e => new
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
                e.Categoriaid
            }).ToListAsync();

            return Results.Ok(eventos);
        });
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
                Expiration = DateTime.Now.AddDays(1)
            });
        });

        #endregion

        app.Run();
    }
}



















/*
using System;
using System.Net;
using System.Net.Mail;

public class EnviarEmail
{
    public static void Main(string[] args)
    {
        MailMessage mensagem = new MailMessage();
        mensagem.From = new MailAddress("seuemail@dominio.com");
        mensagem.To.Add("destinatario@dominio.com");
        mensagem.Subject = "Assunto do Email";
        mensagem.Body = "Corpo do email aqui.";

        SmtpClient clienteSmtp = new SmtpClient("smtp.dominio.com");
        clienteSmtp.Port = 587;
        clienteSmtp.Credentials = new NetworkCredential("seuemail@dominio.com", "suasenha");
        clienteSmtp.EnableSsl = true;

        try
        {
            clienteSmtp.Send(mensagem);
            Console.WriteLine("Email enviado com sucesso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao enviar email: " + ex.Message);
        }
    }
}*/