using Fiscal.API.Data;
using Fiscal.API.Models.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Controllers.Cadastros;

public sealed class ClienteSalvarRequest
{
    public string TipoPessoa { get; set; } = "PF";
    public string NomeRazaoSocial { get; set; } = "";
    public string CpfCnpj { get; set; } = "";
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
}

public sealed class ClienteStatusRequest
{
    public bool Ativo { get; set; }
}

[ApiController]
[Authorize]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private readonly FiscalDbContext _context;
    public ClientesController(FiscalDbContext context) => _context = context;

    private bool TentarEmpresa(out Guid empresaId) =>
        Guid.TryParse(User.FindFirst("empresaId")?.Value, out empresaId) && empresaId != Guid.Empty;

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busca = null, [FromQuery] bool incluirInativos = false)
    {
        if (!TentarEmpresa(out var empresaId)) return Unauthorized(new { mensagem = "Empresa não identificada no token." });
        var consulta = _context.Clientes.AsNoTracking().Where(x => x.EmpresaId == empresaId);
        if (!incluirInativos) consulta = consulta.Where(x => x.Ativo);
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            var digitos = Digitos(termo);
            consulta = consulta.Where(x => EF.Functions.ILike(x.NomeRazaoSocial, $"%{termo}%") ||
                (digitos.Length > 0 && x.CpfCnpj.Contains(digitos)));
        }
        return Ok(await consulta.OrderBy(x => x.NomeRazaoSocial).Take(100).ToListAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id)
    {
        if (!TentarEmpresa(out var empresaId)) return Unauthorized(new { mensagem = "Empresa não identificada no token." });
        var cliente = await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == empresaId);
        return cliente is null ? NotFound(new { mensagem = "Cliente não encontrado." }) : Ok(cliente);
    }

    [HttpPost]
    [Authorize(Roles = "Master,Administrador,Operador")]
    public async Task<IActionResult> Criar([FromBody] ClienteSalvarRequest request)
    {
        if (!TentarEmpresa(out var empresaId)) return Unauthorized(new { mensagem = "Empresa não identificada no token." });
        var erro = Validar(request);
        if (erro != null) return BadRequest(new { mensagem = erro });
        var documento = Digitos(request.CpfCnpj);
        if (await _context.Clientes.AnyAsync(x => x.EmpresaId == empresaId && x.CpfCnpj == documento))
            return Conflict(new { mensagem = "CPF/CNPJ já cadastrado nesta empresa." });
        var cliente = new Cliente { EmpresaId = empresaId };
        Preencher(cliente, request);
        _context.Clientes.Add(cliente);
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(new { mensagem = "Não foi possível salvar. Verifique se o CPF/CNPJ já está cadastrado." }); }
        return CreatedAtAction(nameof(Obter), new { id = cliente.Id }, cliente);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Master,Administrador,Operador")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] ClienteSalvarRequest request)
    {
        if (!TentarEmpresa(out var empresaId)) return Unauthorized(new { mensagem = "Empresa não identificada no token." });
        var erro = Validar(request);
        if (erro != null) return BadRequest(new { mensagem = erro });
        var cliente = await _context.Clientes.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == empresaId);
        if (cliente == null) return NotFound(new { mensagem = "Cliente não encontrado." });
        var documento = Digitos(request.CpfCnpj);
        if (await _context.Clientes.AnyAsync(x => x.EmpresaId == empresaId && x.Id != id && x.CpfCnpj == documento))
            return Conflict(new { mensagem = "CPF/CNPJ já cadastrado nesta empresa." });
        Preencher(cliente, request);
        cliente.AtualizadoEm = DateTime.UtcNow;
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(new { mensagem = "Não foi possível atualizar o cadastro." }); }
        return Ok(cliente);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Master,Administrador")]
    public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] ClienteStatusRequest request)
    {
        if (!TentarEmpresa(out var empresaId)) return Unauthorized(new { mensagem = "Empresa não identificada no token." });
        var cliente = await _context.Clientes.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == empresaId);
        if (cliente == null) return NotFound(new { mensagem = "Cliente não encontrado." });
        cliente.Ativo = request.Ativo;
        cliente.AtualizadoEm = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { cliente.Id, cliente.Ativo });
    }

    private static void Preencher(Cliente cliente, ClienteSalvarRequest r)
    {
        cliente.TipoPessoa = r.TipoPessoa.Trim().ToUpperInvariant();
        cliente.NomeRazaoSocial = r.NomeRazaoSocial.Trim();
        cliente.CpfCnpj = Digitos(r.CpfCnpj);
        cliente.InscricaoEstadual = Limpar(r.InscricaoEstadual);
        cliente.Email = Limpar(r.Email);
        cliente.Telefone = Limpar(r.Telefone);
        cliente.Cep = Limpar(r.Cep) is string cep ? Digitos(cep) : null;
        cliente.Logradouro = Limpar(r.Logradouro);
        cliente.Numero = Limpar(r.Numero);
        cliente.Complemento = Limpar(r.Complemento);
        cliente.Bairro = Limpar(r.Bairro);
        cliente.Cidade = Limpar(r.Cidade);
        cliente.CodigoMunicipioIbge = Limpar(r.CodigoMunicipioIbge);
        cliente.Uf = Limpar(r.Uf)?.ToUpperInvariant();
    }

    private static string? Limpar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    private static string Digitos(string? valor) => new((valor ?? "").Where(c => c >= '0' && c <= '9').ToArray());

    private static string? Validar(ClienteSalvarRequest? r)
    {
        if (r == null) return "Informe os dados do cliente.";
        var tipo = r.TipoPessoa?.Trim().ToUpperInvariant();
        if (tipo is not ("PF" or "PJ")) return "TipoPessoa deve ser PF ou PJ.";
        if (string.IsNullOrWhiteSpace(r.NomeRazaoSocial) || r.NomeRazaoSocial.Trim().Length > 150)
            return "Nome/Razão Social obrigatório (máximo 150 caracteres).";
        var doc = Digitos(r.CpfCnpj);
        if (tipo == "PF" && !CpfValido(doc)) return "CPF inválido.";
        if (tipo == "PJ" && !CnpjValido(doc)) return "CNPJ inválido.";
        if (r.Uf != null && r.Uf.Trim().Length is not (0 or 2)) return "UF deve ter 2 letras.";
        if (Limpar(r.Cep) is string cep && Digitos(cep).Length != 8) return "CEP deve ter 8 dígitos.";
        if (Limpar(r.CodigoMunicipioIbge) is string ibge && (ibge.Length != 7 || ibge.Any(c => c < '0' || c > '9')))
            return "Código IBGE deve ter 7 dígitos.";
        if (r.Email?.Length > 200 || r.Telefone?.Length > 30 || r.Logradouro?.Length > 200 ||
            r.Numero?.Length > 30 || r.Complemento?.Length > 100 || r.Bairro?.Length > 100 ||
            r.Cidade?.Length > 100 || r.InscricaoEstadual?.Length > 20)
            return "Um ou mais campos excedem o tamanho permitido.";
        return null;
    }

    private static bool CpfValido(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1) return false;
        for (int digito = 9; digito < 11; digito++)
        {
            var soma = 0;
            for (int i = 0; i < digito; i++) soma += (cpf[i] - '0') * (digito + 1 - i);
            var resto = (soma * 10) % 11;
            if (resto == 10) resto = 0;
            if (resto != cpf[digito] - '0') return false;
        }
        return true;
    }

    private static bool CnpjValido(string cnpj)
    {
        if (cnpj.Length != 14 || cnpj.Distinct().Count() == 1) return false;
        int[] p1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] p2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var soma = 0;
        for (int i = 0; i < 12; i++) soma += (cnpj[i] - '0') * p1[i];
        var resto = soma % 11;
        if ((resto < 2 ? 0 : 11 - resto) != cnpj[12] - '0') return false;
        soma = 0;
        for (int i = 0; i < 13; i++) soma += (cnpj[i] - '0') * p2[i];
        resto = soma % 11;
        return (resto < 2 ? 0 : 11 - resto) == cnpj[13] - '0';
    }
}
