using FluentValidation;

namespace connectMatao.Domain.DTOs.Evento
{
    public class EventoAdicionarDtoValidator : AbstractValidator<EventoAdicionarDto>
    {
        public EventoAdicionarDtoValidator()
        {
            RuleFor(p => p.Titulo)
                .NotEmpty().WithMessage("Titulo deve ser de prenchimento obrigatório.")
                .MaximumLength(150).WithMessage("Titulo deve ter no máximo 150 caracteres");

            RuleFor(p => p.Cep)
                .NotEmpty().WithMessage("Cep deve ser de prenchimento obrigatório.")
                .MaximumLength(9).WithMessage("Cep deve ter no máximo 9 caracteres");

            RuleFor(p => p.Logradouro)
                .NotEmpty().WithMessage("Logradouro deve ser de prenchimento obrigatório.")
                .MaximumLength(150).WithMessage("Logradouro deve ter no máximo 150 caracteres");

            RuleFor(p => p.Numero)
                .NotEmpty().WithMessage("Número deve ser de prenchimento obrigatório.")
                .MaximumLength(5).WithMessage("Número deve ter no máximo 5 caracteres");

            RuleFor(p => p.Bairro)
                .NotEmpty().WithMessage("Bairro deve ser de prenchimento obrigatório.")
                .MaximumLength(100).WithMessage("Bairro deve ter no máximo 100 caracteres");

            RuleFor(p => p.Descricao)
                .NotEmpty().WithMessage("Descrição deve ser de prenchimento obrigatório.")
                .MaximumLength(800).WithMessage("Descrição deve ter no máximo 800 caracteres");

            RuleFor(p => p.Telefone)
                .NotEmpty().WithMessage("Telefone deve ser de prenchimento obrigatório.")
                .MaximumLength(20).WithMessage("Telefone deve ter no máximo 20 caracteres");

            RuleFor(p => p.Whatsapp)
                .NotEmpty().WithMessage("WhatsApp deve ser de prenchimento obrigatório.")
                .MaximumLength(20).WithMessage("WhatsApp deve ter no máximo 20 caracteres");

            RuleFor(p => p.Email)
                .NotEmpty().WithMessage("Email deve ser de prenchimento obrigatório.")
                .MaximumLength(200).WithMessage("Titulo deve ter no máximo 200 caracteres");

            RuleFor(p => p.Horario)
                .NotEmpty().WithMessage("Horário deve ser de prenchimento obrigatório.")
                .MaximumLength(5).WithMessage("Horário deve ter no máximo 5 caracteres");

            RuleFor(p => p.FaixaEtaria)
                .NotEmpty().WithMessage("Faixa Etária deve ser de prenchimento obrigatório.");
        }
    }
}
