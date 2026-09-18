using Fiscal.API.Models;
using Fiscal.API.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Data;

public class FiscalDbContext : DbContext
{
    public FiscalDbContext(DbContextOptions<FiscalDbContext> options) : base(options){ }

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<ConfiguracaoFiscal> ConfiguracoesFiscais => Set<ConfiguracaoFiscal>();
    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();
    public DbSet<ConfiguracaoTributaria> ConfiguracoesTributarias => Set<ConfiguracaoTributaria>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurarEmpresa(modelBuilder);
        ConfigurarConfiguracaoFiscal(modelBuilder);
        ConfigurarNotaFiscal(modelBuilder);
        ConfigurarConfiguracaoTributaria(modelBuilder);
        ConfigurarProduto(modelBuilder);
        ConfigurarUsuario(modelBuilder);
    }

    private static void ConfigurarEmpresa(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Empresa>();

        entity.ToTable("empresas");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Cnpj)
            .HasMaxLength(14)
            .IsRequired();

        entity.HasIndex(x => x.Cnpj)
            .IsUnique();

        entity.Property(x => x.RazaoSocial)
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(x => x.NomeFantasia)
            .HasMaxLength(150);

        entity.Property(x => x.InscricaoEstadual)
            .HasMaxLength(20);

        entity.Property(x => x.Uf)
            .HasMaxLength(2)
            .IsRequired();

        entity.HasOne(x => x.ConfiguracaoFiscal)
            .WithOne(x => x.Empresa)
            .HasForeignKey<ConfiguracaoFiscal>(x => x.EmpresaId);
    }

    private static void ConfigurarConfiguracaoFiscal(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ConfiguracaoFiscal>();

        entity.ToTable("configuracoes_fiscais");

        entity.HasKey(x => x.Id);

        entity.HasIndex(x => x.EmpresaId)
            .IsUnique();

        entity.Property(x => x.Csc)
            .HasMaxLength(100);

        entity.Property(x => x.CscId)
            .HasMaxLength(10);
    }

    private static void ConfigurarNotaFiscal(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<NotaFiscal>();

        entity.ToTable("notas_fiscais");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.ChaveAcesso)
            .HasMaxLength(44);

        entity.Property(x => x.Status)
            .HasMaxLength(30)
            .IsRequired();

        entity.Property(x => x.Protocolo)
            .HasMaxLength(30);

        entity.Property(x => x.Recibo)
            .HasMaxLength(30);

        entity.Property(x => x.ValorProdutos)
            .HasPrecision(15, 2);

        entity.Property(x => x.ValorTotal)
            .HasPrecision(15, 2);

        entity.HasIndex(x => x.ChaveAcesso);

        entity.HasIndex(x => x.Status);

        entity.HasIndex(x => new
        {
            x.EmpresaId,
            x.Ambiente,
            x.Modelo,
            x.Serie,
            x.Numero
        }).IsUnique();

        entity.HasOne(x => x.Empresa)
            .WithMany(x => x.NotasFiscais)
            .HasForeignKey(x => x.EmpresaId);
    }

    private static void ConfigurarConfiguracaoTributaria(ModelBuilder modelBuilder)
    {
        var entity =
            modelBuilder.Entity<ConfiguracaoTributaria>();

        entity.ToTable("configuracoes_tributarias");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Nome)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.CstIcms)
            .HasMaxLength(3);

        entity.Property(x => x.Csosn)
            .HasMaxLength(4);

        entity.Property(x => x.AliquotaIcms)
            .HasPrecision(7, 4);

        entity.Property(x => x.Cfop)
            .HasMaxLength(4)
            .IsRequired();

        entity.HasIndex(x => x.EmpresaId);

        entity.HasOne(x => x.Empresa)
            .WithMany(x => x.ConfiguracoesTributarias)
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurarProduto(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Produto>();

        entity.ToTable("produtos");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Codigo)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.Descricao)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.Ncm)
            .HasMaxLength(8)
            .IsRequired();

        entity.Property(x => x.Unidade)
            .HasMaxLength(10)
            .IsRequired();

        // Um código de produto não pode se repetir
        // dentro da mesma empresa.
        entity.HasIndex(x => new
        {
            x.EmpresaId,
            x.Codigo
        })
        .IsUnique();

        entity.HasIndex(x => x.ConfiguracaoTributariaId);

        entity.HasOne(x => x.Empresa)
            .WithMany(x => x.Produtos)
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Property(x => x.ValorVenda)
            .HasPrecision(18, 2)
            .IsRequired();

        entity.HasOne(x => x.ConfiguracaoTributaria)
            .WithMany(x => x.Produtos)
            .HasForeignKey(x => x.ConfiguracaoTributariaId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurarUsuario(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Usuario>();

        entity.ToTable("usuarios");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Nome)
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(x => x.Email)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.SenhaHash)
            .HasMaxLength(500)
            .IsRequired();

        entity.Property(x => x.Perfil)
            .HasMaxLength(50)
            .IsRequired();

        entity.HasIndex(x => x.Email)
            .IsUnique();

        entity.HasOne(x => x.Empresa)
            .WithMany(x => x.Usuarios)
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}