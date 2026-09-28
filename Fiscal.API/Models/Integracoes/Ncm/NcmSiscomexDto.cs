using System.Text.Json.Serialization;

namespace Fiscal.API.Models.Integracoes.Ncm
{
    public class NcmSiscomexResponseDto
    {
        [JsonPropertyName("Data_Ultima_Atualizacao_NCM")]
        public string DataUltimaAtualizacaoNcm { get; set; } = string.Empty;

        [JsonPropertyName("Ato")]
        public string Ato { get; set; } = string.Empty;

        [JsonPropertyName("Nomenclaturas")]
        public List<NcmSiscomexDto> Nomenclaturas { get; set; } = new();
    }

    public class NcmSiscomexDto
    {
        [JsonPropertyName("Codigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("Descricao")]
        public string Descricao { get; set; } = string.Empty;

        [JsonPropertyName("Data_Inicio")]
        public string DataInicio { get; set; } = string.Empty;

        [JsonPropertyName("Data_Fim")]
        public string DataFim { get; set; } = string.Empty;

        [JsonPropertyName("Tipo_Ato_Ini")]
        public string? TipoAtoIni { get; set; }

        [JsonPropertyName("Numero_Ato_Ini")]
        public string? NumeroAtoIni { get; set; }

        [JsonPropertyName("Ano_Ato_Ini")]
        public string? AnoAtoIni { get; set; }
    }
}