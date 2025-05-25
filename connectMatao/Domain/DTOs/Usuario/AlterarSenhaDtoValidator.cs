using FluentValidation;

namespace connectMatao.Domain.DTOs.Usuario
{
    public class AlterarSenhaDtoValidator : AbstractValidator<AlterarSenhaDto>
    {
        public AlterarSenhaDtoValidator()
        {
            RuleFor(x => x.SenhaAtual)
                .NotEmpty().WithMessage("Senha atual é obrigatória.");

            RuleFor(x => x.NovaSenha)
                .NotEmpty().WithMessage("Nova senha é obrigatória.")
                .MinimumLength(8).WithMessage("Nova senha deve ter no mínimo 8 caracteres.")
                .Matches("[A-Z]").WithMessage("A senha deve conter pelo menos uma letra maiúscula.")
                .Matches(@"^[A-ZÁÉÍÓÚÂÊÎÔÛÃÕÇ]").WithMessage("O nome deve começar com uma letra maiúscula.");
            RuleFor(x => x)
                .Must(dto => dto.NovaSenha != dto.SenhaAtual)
                .WithMessage("A nova senha não pode ser igual à senha atual.");
        }
    }
}
