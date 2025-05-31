public class UsuarioListarComStatusParceiroDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Imagem { get; set; } = string.Empty;
    public string Perfil { get; set; } 
    public bool? FlagAprovadoParceiro { get; set; } 
    public bool FormParceiroExiste { get; set; }
}