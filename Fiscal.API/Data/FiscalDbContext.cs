using Fiscal.API.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Data;

public class FiscalDbContext : DbContext
{
    public FiscalDbContext(DbContextOptions<FiscalDbContext> options) : base(options){ }

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<ConfiguracaoFiscal> ConfiguracoesFiscais => Set<ConfiguracaoFiscal>();
    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurarEmpresa(modelBuilder);
        ConfigurarConfiguracaoFiscal(modelBuilder);
        ConfigurarNotaFiscal(modelBuilder);
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
}