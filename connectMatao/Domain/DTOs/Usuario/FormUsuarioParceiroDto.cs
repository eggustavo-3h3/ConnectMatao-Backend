namespace connectMatao.Domain.DTOs.Usuario
{
    public class FormUsuarioParceiroDto
    {   
        public string NomeCompleto { get; set; } = string.Empty;
        public string CPF { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public bool? FlagAprovadoParceiro { get; set; } 
        public bool FormParceiroExiste { get; set; }
    }
}
