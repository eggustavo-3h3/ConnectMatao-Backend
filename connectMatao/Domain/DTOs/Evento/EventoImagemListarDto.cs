using System;

namespace connectMatao.Domain.DTOs.EventoImagem
{
    public class EventoImagemListarDto
    {
        public Guid Id { get; set; }
        public string Imagem { get; set; } = string.Empty;
        public Guid EventoId { get; set; }
    }
}