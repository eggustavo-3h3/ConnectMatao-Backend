namespace connectMatao.Domain.DTOs.Evento
{
    public class EventoAdicionarDto
    {
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Cep { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Whatsapp { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime Data { get; set; }
        public Guid Categoriaid { get; set; }
        public bool FlagAprovado { get; set; }
        public Guid UsuarioParceiroid { get; set; }
        public string Horario { get; set; } = string.Empty;
        public int FaixaEtaria { get; set; }
        public string Imagem { get; set; } = string.Empty;
    }
}
