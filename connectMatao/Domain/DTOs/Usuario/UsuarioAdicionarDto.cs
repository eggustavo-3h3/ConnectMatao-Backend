using connectMatao.Enumerator;

namespace connectMatao.Domain.DTOs.Usuario
{
    public class UsuarioAdicionarDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string ConfirmacaoSenha { get; set; } = string.Empty;
        public string Imagem { get; set; } = string.Empty;
        public EnumPerfil Perfil { get; set; }
    }
}