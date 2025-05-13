using FluentValidation;

namespace connectMatao.Domain.DTOs.Categoria
{
    public class CategoriaAtualizarDtoValidator : AbstractValidator<CategoriaAdicionarDto>
    {
        public CategoriaAtualizarDtoValidator()
        {
            RuleFor(p => p.Descricao)
                .NotEmpty().WithMessage("Descrição deve ser de prenchimento obrigatório.")
                .MaximumLength(150).WithMessage("Descrição deve ter no máximo 150 caracteres");
        }
    }
}
