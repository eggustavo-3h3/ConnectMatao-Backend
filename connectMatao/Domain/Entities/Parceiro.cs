namespace connectMatao.Domain.Entities
{
    public class Parceiro
    {
        public Guid Id { get; set; }
        public Guid UsuarioId { get; set; }
        public string NomeCompleto { get; set; } = string.Empty;
        public string Cpf { get; set; }
        public string Telefone { get; set; }
        public bool FlagAprovado { get; set; } = false;
        public DateTime DataEnvio { get; set; }

        #region Navegabilidade

        public Usuario Usuario { get; set; } = null!;

        #endregion
    }
}