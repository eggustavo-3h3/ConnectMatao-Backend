using connectMatao.Enumerator;

namespace connectMatao.Domain.Entities
{
    public class Usuario
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string Imagem { get; set; } = string.Empty;
        public Guid? ChaveReset { get; set; }

        public EnumPerfil Perfil { get; set; }
    }
}
