using FluentValidation;

namespace connectMatao.Domain.DTOs.Usuario
{
    public class UsuarioAtualizarDtoValidator : AbstractValidator<UsuarioAtualizarDto>
    {
        public UsuarioAtualizarDtoValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MaximumLength(150).WithMessage("Nome deve ter no máximo 150 caracteres.");

            RuleFor(x => x.Login)
                .NotEmpty().WithMessage("Login é obrigatório.")
                .MaximumLength(100).WithMessage("Login deve ter no máximo 100 caracteres.");
        }
    }
}
