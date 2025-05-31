using FluentValidation;
using System.Text.RegularExpressions; 

namespace connectMatao.Domain.DTOs.Usuario
{
    public class FormParceiroCompletarCadastroDtoValidator : AbstractValidator<FormParceiroCompletarCadastroDto>
    {
        public FormParceiroCompletarCadastroDtoValidator()
        {
            RuleFor(x => x.NomeCompleto)
                .NotEmpty().WithMessage("O nome completo é obrigatório.")
                .MaximumLength(100).WithMessage("O nome completo não pode exceder 100 caracteres.");

            RuleFor(x => x.CPF)
                .NotEmpty().WithMessage("O CPF é obrigatório.")
                .Length(11).WithMessage("O CPF deve conter 11 dígitos numéricos.") // Garante que tem 11 dígitos
                .Must(BeAValidCpf).WithMessage("CPF inválido."); // Chama a função de validação de CPF

            RuleFor(x => x.Telefone)
                .NotEmpty().WithMessage("O telefone é obrigatório.")
                .Matches(@"^\d{10,11}$").WithMessage("O telefone deve conter 10 ou 11 dígitos numéricos."); // Ex: 11987654321 ou 1123456789
        }

        private bool BeAValidCpf(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return false;

            cpf = Regex.Replace(cpf, @"[^\d]", "");

            if (cpf.Length != 11)
                return false;

            if (new string(cpf[0], 11) == cpf)
                return false;

            int[] multiplicador1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCpf;
            string digito;
            int soma;
            int resto;

            tempCpf = cpf.Substring(0, 9);
            soma = 0;
            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];
            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = resto.ToString();
            tempCpf += digito;
            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];
            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito += resto.ToString();

            return cpf.EndsWith(digito);
        }
    }
}