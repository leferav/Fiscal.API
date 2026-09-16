using Fiscal.API.Data;
using Fiscal.API.Models.Database;
using Fiscal.API.Models.Requests.Cadastros;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Controllers.Cadastros;

[ApiController]
[Route("api/cadastros/[controller]")]
public class EmpresasController : ControllerBase
{
    private readonly FiscalDbContext _context;

    public EmpresasController(FiscalDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Empresa>>> Get()
    {
        var empresas = await _context.Empresas
            .AsNoTracking()
            .OrderBy(x => x.RazaoSocial)
            .ToListAsync();

        return Ok(empresas);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Empresa>> GetById(Guid id)
    {
        var empresa = await _context.Empresas
            .AsNoTracking()
            .Include(x => x.ConfiguracaoFiscal)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (empresa is null)
            return NotFound();

        return Ok(empresa);
    }

    [HttpPost]
    public async Task<ActionResult<Empresa>> Post(CadastrarEmpresaRequest request)
    {
        var cnpjExistente = await _context.Empresas
            .AnyAsync(x => x.Cnpj == request.Cnpj);

        if (cnpjExistente)
            return Conflict("Já existe uma empresa cadastrada com este CNPJ.");

        var empresa = new Empresa
        {
            Id = Guid.NewGuid(),

            Cnpj = request.Cnpj,
            RazaoSocial = request.RazaoSocial,
            NomeFantasia = request.NomeFantasia,
            InscricaoEstadual = request.InscricaoEstadual,

            Logradouro = request.Logradouro,
            Numero = request.Numero,
            Bairro = request.Bairro,
            CodigoMunicipio = request.CodigoMunicipio,
            Municipio = request.Municipio,
            Uf = request.Uf,
            Cep = request.Cep,
            Crt = request.Crt,

            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };

        _context.Empresas.Add(empresa);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = empresa.Id },
            empresa);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] CadastrarEmpresaRequest request)
    {
        var empresa = await _context.Empresas
            .FirstOrDefaultAsync(x => x.Id == id);

        if (empresa is null)
            return NotFound(new
            {
                mensagem = "Empresa não encontrada."
            });

        var cnpjExistente = await _context.Empresas
            .AnyAsync(x => x.Cnpj == request.Cnpj && x.Id != id);

        if (cnpjExistente)
            return BadRequest(new
            {
                mensagem = "Já existe outra empresa cadastrada com este CNPJ."
            });

        empresa.Cnpj = request.Cnpj;
        empresa.RazaoSocial = request.RazaoSocial;
        empresa.NomeFantasia = request.NomeFantasia;
        empresa.InscricaoEstadual = request.InscricaoEstadual;

        empresa.Logradouro = request.Logradouro;
        empresa.Numero = request.Numero;
        empresa.Bairro = request.Bairro;
        empresa.CodigoMunicipio = request.CodigoMunicipio;
        empresa.Municipio = request.Municipio;
        empresa.Uf = request.Uf;
        empresa.Cep = request.Cep;
        empresa.Crt = request.Crt;

        await _context.SaveChangesAsync();

        return Ok(empresa);
    }
}