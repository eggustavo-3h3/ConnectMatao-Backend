namespace connectMatao.Domain.DTOs.Evento
{
    public class EventoDetalheDto
    {
        public Guid Id { get; set; }
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public string Cep { get; set; }
        public string Logradouro { get; set; }
        public string Numero { get; set; }
        public string Bairro { get; set; }
        public string Telefone { get; set; }
        public string Email { get; set; }
        public DateTime Data { get; set; }
        public string Horario { get; set; }
        public int FaixaEtaria { get; set; }
        public bool FlagAprovado { get; set; }
        public Guid UsuarioParceiroid { get; set; }
        public Guid Categoriaid { get; set; }
        public string UsuarioNome { get; set; }
        public string UsuarioImagem { get; set; }
        public string Whatsapp { get; set; }
        public string[] Imagens { get; set; } = new string[0];
        public int Likes { get; set; }
        public int Deslikes { get; set; }
        public int UsuarioInteragiu { get; set; }
    }

    public class EventoImagemDto
    {
        public string Imagem { get; set; }
    }
}