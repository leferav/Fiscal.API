namespace Fiscal.API.Models.Database
{
    public class Ncm
    {
        public Guid Id { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public DateTime? DataInicio { get; set; }

        public DateTime? DataFim { get; set; }

        public bool Ativo { get; set; } = true;
    }
}