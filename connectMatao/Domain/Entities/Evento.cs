namespace connectMatao.Domain.Entities
{
    public class Evento
    {
        public Guid Id { get; set; }
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public string Cep { get; set; }
        public string Logradouro { get; set; }
        public string Numero { get; set; }
        public string Bairro { get; set; }
        public string Telefone { get; set; }
        public string Whatsapp { get; set; }
        public string Email { get; set; }
        public DateTime Data { get; set; }
        public string Horario { get; set; }
        public string FaixaEtaria { get; set; }
        public bool FlagAprovado { get; set; }
        public Guid UsuarioParceiroid { get; set; }
        public Guid Categoriaid { get; set; }
        public List<EventoEstatisticas> Estatisticas { get; set; } = [];
   
        #region Propriedade de Navegabilidade

        public virtual Usuario UsuarioParceiro { get; set; }
        public virtual Categoria Categoria { get; set; }

        public virtual List<EventoImagens> EventoImagens { get; set; }
        #endregion

        public Evento(
            string titulo,
            string descricao,
            string cep,
            string logradouro,
            string numero,
            string bairro,
            string telefone,
            string email,
            DateTime data,
            Guid categoriaid,
            bool flagAprovado,
            Guid usuarioParceiroid,
            string horario,
           string faixaEtaria,
            string whatsapp)
        {
            Id = Guid.NewGuid();
            Titulo = titulo;
            Descricao = descricao;
            Cep = cep;
            Logradouro = logradouro;
            Numero = numero;
            Bairro = bairro;
            Telefone = telefone;
            Email = email;
            Data = data;
            Categoriaid = categoriaid;
            FlagAprovado = flagAprovado;
            UsuarioParceiroid = usuarioParceiroid;
            Horario = horario;
            FaixaEtaria = faixaEtaria;
            Whatsapp = whatsapp;
        }
    }
}
