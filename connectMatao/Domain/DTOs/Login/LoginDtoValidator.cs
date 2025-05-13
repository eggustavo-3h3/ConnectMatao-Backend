using FluentValidation;

namespace connectMatao.Domain.DTOs.Login
{
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(p => p.Login)
                .NotEmpty().WithMessage("Descrição deve ser de prenchimento obrigatório.")
                .MaximumLength(100).WithMessage("Descrição deve ter no máximo 100 caracteres");

            RuleFor(p => p.Senha)
                .NotEmpty().WithMessage("Senha deve ser de prenchimento obrigatório.");
        }
    }
}
