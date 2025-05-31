namespace connectMatao.Domain.DTOs.Usuario
{
    public class FormUsuarioParceiroListarDto
    {
        public Guid Id { get; set; }
        public Guid UsuarioId { get; set; }
        public string NomeUsuario { get; set; } 
        public string LoginUsuario { get; set; } 
        public string NomeCompleto { get; set; }
        public string Cpf { get; set; }
        public string Telefone { get; set; }
        public bool FlagAprovado { get; set; }
        public DateTime DataEnvio { get; set; } 
    }
}
