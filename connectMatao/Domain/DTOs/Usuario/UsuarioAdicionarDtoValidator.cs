using connectMatao.Enumerator;
using FluentValidation;

namespace connectMatao.Domain.DTOs.Usuario
{
    public class UsuarioAdicionarDtoValidator : AbstractValidator<UsuarioAdicionarDto>
    {
        public UsuarioAdicionarDtoValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MaximumLength(150).WithMessage("Nome deve ter no máximo 150 caracteres.")
                .Matches(@"^[A-ZÁÉÍÓÚÂÊÎÔÛÃÕÇ]").WithMessage("O nome deve começar com uma letra maiúscula.");

            RuleFor(x => x.Login)
                .NotEmpty().WithMessage("Login é obrigatório.")
                .MaximumLength(100).WithMessage("Login deve ter no máximo 100 caracteres.")
                .EmailAddress().WithMessage("Login deve ser um endereço de e-mail válido."); 

            RuleFor(x => x.Senha)
                .NotEmpty().WithMessage("Senha é obrigatória.")
                .MinimumLength(8).WithMessage("A senha deve ter no mínimo 8 caracteres.")
                .Matches("[A-Z]").WithMessage("A senha deve conter pelo menos uma letra maiúscula.")
                .Matches("[a-z]").WithMessage("A senha deve conter pelo menos uma letra minúscula.")
                .Matches("[0-9]").WithMessage("A senha deve conter pelo menos um número.") 
                .Matches("[^a-zA-Z0-9]").WithMessage("A senha deve conter pelo menos um caractere especial."); 

            RuleFor(x => x.ConfirmacaoSenha)
                .Equal(x => x.Senha).WithMessage("As senhas não coincidem.");

            RuleFor(x => x.Perfil)
                .NotEqual(EnumPerfil.Administrador) 
                .WithMessage("O perfil não deve ser 'Administrador'.");

            RuleFor(x => x.Perfil)
                .Must(perfil => perfil == EnumPerfil.Usuario || perfil == EnumPerfil.Parceiro) 
                .WithMessage("Perfil inválido. Somente 'Usuário' ou 'Parceiro' são permitidos para cadastro.");
        }
    }
}