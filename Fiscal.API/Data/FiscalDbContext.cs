using Fiscal.API.Models;
using Fiscal.API.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Data;

public class FiscalDbContext : DbContext
{
    public FiscalDbContext(DbContextOptions<FiscalDbContext> options) : base(options){ }

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<CertificadoDigital> CertificadosDigitais => Set<CertificadoDigital>();
    public DbSet<CertificadoDigitalArquivo> CertificadosDigitaisArquivos => Set<CertificadoDigitalArquivo>();
    public DbSet<ConfiguracaoFiscal> ConfiguracoesFiscais => Set<ConfiguracaoFiscal>();
    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<VendaItem> VendasItens => Set<VendaItem>();
    public DbSet<ConfiguracaoTributaria> ConfiguracoesTributarias => Set<ConfiguracaoTributaria>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<ProdutoImagem> ProdutosImagens => Set<ProdutoImagem>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioEmpresa> UsuariosEmpresas => Set<UsuarioEmpresa>();
    public DbSet<ConviteUsuario> ConvitesUsuarios => Set<ConviteUsuario>();
    public DbSet<Ncm> Ncms => Set<Ncm>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurarEmpresa(modelBuilder);
        ConfigurarCertificadoDigital(modelBuilder);
        ConfigurarCertificadoDigitalArquivo(modelBuilder);
        ConfigurarConfiguracaoFiscal(modelBuilder);
        ConfigurarNotaFiscal(modelBuilder);
        ConfigurarVenda(modelBuilder);
        ConfigurarVendaItem(modelBuilder);
        ConfigurarConfiguracaoTributaria(modelBuilder);
        ConfigurarProduto(modelBuilder);
        ConfigurarProdutoImagem(modelBuilder);   
        ConfigurarUsuario(modelBuilder);
        ConfigurarUsuarioEmpresa(modelBuilder);
        ConfigurarConviteUsuario(modelBuilder);
        ConfigurarNcm(modelBuilder);
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

    private static void ConfigurarCertificadoDigital( ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CertificadoDigital>();

        entity.ToTable("certificados_digitais");

        entity.HasKey(x => x.Id);

        entity.HasIndex(x => x.EmpresaId)
            .IsUnique();

        entity.Property(x => x.NomeArquivo)
            .HasMaxLength(255)
            .IsRequired(false);

        entity.Property(x => x.Titular)
            .HasMaxLength(250);

        entity.Property(x => x.Cnpj)
            .HasMaxLength(14);

        entity.Property(x => x.NumeroSerie)
            .HasMaxLength(100)
            .IsRequired(false);

        entity.Property(x => x.ReferenciaArmazenamento)
            .HasMaxLength(500)
            .IsRequired(false);

        entity.Property(x => x.Emissor)
            .HasMaxLength(200);

        entity.Property(x => x.Thumbprint)
            .HasMaxLength(100);

        entity.Property(x => x.TipoArmazenamento)
            .IsRequired();

        entity.Property(x => x.ValidoDe)
            .HasColumnType("timestamp with time zone");

        entity.Property(x => x.ValidoAte)
            .HasColumnType("timestamp with time zone");

        entity.HasOne(x => x.Empresa)
            .WithOne(x => x.CertificadoDigital)
            .HasForeignKey<CertificadoDigital>(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurarCertificadoDigitalArquivo(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CertificadoDigitalArquivo>();

        entity.ToTable("certificados_digitais_arquivos");

        entity.HasKey(x => x.Id);

        entity.HasIndex(x => x.CertificadoDigitalId)
            .IsUnique();

        entity.HasOne(x => x.CertificadoDigital)
            .WithOne(x => x.Arquivo)
            .HasForeignKey<CertificadoDigitalArquivo>(
                x => x.CertificadoDigitalId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Property(x => x.ConteudoCriptografado)
            .HasColumnType("bytea")
            .IsRequired();

        entity.Property(x => x.Nonce)
            .HasColumnType("bytea")
            .IsRequired();

        entity.Property(x => x.TagAutenticacao)
            .HasColumnType("bytea")
            .IsRequired();

        entity.Property(x => x.SenhaCriptografada)
            .HasColumnType("bytea")
            .IsRequired();

        entity.Property(x => x.SenhaNonce)
            .HasColumnType("bytea")
            .IsRequired();

        entity.Property(x => x.SenhaTagAutenticacao)
            .HasColumnType("bytea")
            .IsRequired();

        entity.Property(x => x.CriadoEm)
            .HasColumnType("timestamp with time zone");
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

    private static void ConfigurarVenda(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Venda>();

        entity.ToTable("vendas");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
        entity.Property(x => x.FormaPagamento).HasMaxLength(2).IsRequired();
        entity.Property(x => x.CpfConsumidor).HasMaxLength(11);
        entity.Property(x => x.ValorTotal).HasPrecision(18, 2);
        entity.Property(x => x.CriadoEm).HasColumnType("timestamp with time zone");
        entity.Property(x => x.AtualizadoEm).HasColumnType("timestamp with time zone");

        entity.HasIndex(x => new { x.EmpresaId, x.Status });
        entity.HasIndex(x => x.NotaFiscalId).IsUnique();

        entity.HasOne(x => x.Empresa)
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.NotaFiscal)
            .WithMany()
            .HasForeignKey(x => x.NotaFiscalId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurarVendaItem(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<VendaItem>();

        entity.ToTable("vendas_itens");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.CodigoProduto).HasMaxLength(50).IsRequired();
        entity.Property(x => x.DescricaoProduto).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Quantidade).HasPrecision(18, 4);
        entity.Property(x => x.ValorUnitario).HasPrecision(18, 2);
        entity.Property(x => x.ValorTotal).HasPrecision(18, 2);

        entity.HasIndex(x => x.VendaId);
        entity.HasIndex(x => x.ProdutoId);

        entity.HasOne(x => x.Venda)
            .WithMany(x => x.Itens)
            .HasForeignKey(x => x.VendaId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.Produto)
            .WithMany()
            .HasForeignKey(x => x.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);
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

    private static void ConfigurarProdutoImagem(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ProdutoImagem>();

        entity.ToTable("produtos_imagens");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Imagem)
            .HasColumnType("bytea")
            .IsRequired();

        entity.Property(x => x.ContentType)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.TamanhoBytes)
            .IsRequired();

        entity.Property(x => x.CriadoEm)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Cada produto poderá ter somente uma imagem.
        entity.HasIndex(x => x.ProdutoId)
            .IsUnique();

        entity.HasOne(x => x.Produto)
            .WithOne(x => x.ImagemProduto)
            .HasForeignKey<ProdutoImagem>(x => x.ProdutoId)
            .OnDelete(DeleteBehavior.Cascade);
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

    private static void ConfigurarUsuarioEmpresa(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UsuarioEmpresa>();

        entity.ToTable("usuarios_empresas");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Perfil)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.Ativo)
            .IsRequired();

        entity.Property(x => x.CriadoEm)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Impede vincular o mesmo usuário duas vezes à mesma empresa
        entity.HasIndex(x => new
        {
            x.UsuarioId,
            x.EmpresaId
        }).IsUnique();

        // Relacionamento com o usuário
        entity.HasOne(x => x.Usuario)
            .WithMany(x => x.UsuariosEmpresas)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relacionamento com a empresa
        entity.HasOne(x => x.Empresa)
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurarConviteUsuario(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConviteUsuario>(entity =>
        {
            entity.ToTable("convites_usuarios");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Nome)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Email)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(x => x.Perfil)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.TokenHash)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.CriadoEm)
                .HasColumnType("timestamp with time zone");

            entity.Property(x => x.ExpiraEm)
                .HasColumnType("timestamp with time zone");

            entity.Property(x => x.UtilizadoEm)
                .HasColumnType("timestamp with time zone");

            entity.HasIndex(x => x.TokenHash)
                .IsUnique();

            entity.HasIndex(x => new
            {
                x.EmpresaId,
                x.Email
            });

            entity.HasOne<Empresa>()
                .WithMany()
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurarNcm(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Ncm>();

        entity.ToTable("ncms");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Codigo)
            .HasMaxLength(8)
            .IsRequired();

        entity.Property(x => x.Descricao)
            .HasColumnType("text")
            .IsRequired();

        entity.HasIndex(x => x.Codigo)
            .IsUnique();

        entity.HasIndex(x => x.Descricao);

        entity.Property(x => x.Ativo)
            .IsRequired();
    }
}