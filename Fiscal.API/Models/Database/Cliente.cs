using System;

namespace Fiscal.API.Models.Database;

public class Cliente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public string TipoPessoa { get; set; } = "PF"; // PF ou PJ
    public string NomeRazaoSocial { get; set; } = string.Empty;
    public string CpfCnpj { get; set; } = string.Empty; // Somente dígitos
    public string? InscricaoEstadual { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? CodigoMunicipioIbge { get; set; }
    public string? Uf { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
}
