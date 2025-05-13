using FluentValidation;

namespace connectMatao.Domain.DTOs.Categoria
{
    public class CategoriaAdicionarDtoValidator : AbstractValidator<CategoriaAdicionarDto>
    {
        public CategoriaAdicionarDtoValidator()
        {
            RuleFor(p => p.Descricao)
                .NotEmpty().WithMessage("Descrição deve ser de prenchimento obrigatório.")
                .MaximumLength(150).WithMessage("Descrição deve ter no máximo 150 caracteres");
        }
    }
}
