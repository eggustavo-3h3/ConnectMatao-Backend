namespace connectMatao.Domain.DTOs.Signup;

public class SignupDto
{
    public string Login { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string ConfirmacaoSenha { get; set; } = string.Empty;
}