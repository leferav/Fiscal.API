using Fiscal.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Data;

public static class UsuarioSeed
{
    public static async Task CriarAdminInicialAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<FiscalDbContext>();

        var email = configuration["AdminInicial:Email"];
        var senha = configuration["AdminInicial:Senha"];
        var empresaIdTexto = configuration["AdminInicial:EmpresaId"];

        // Se não estiver configurado, simplesmente não executa o seed.
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(senha) ||
            string.IsNullOrWhiteSpace(empresaIdTexto))
        {
            Console.WriteLine("SEED: configuração do administrador inicial não encontrada.");

            Console.WriteLine(
                $"Email: {(string.IsNullOrWhiteSpace(email) ? "NÃO" : "OK")}"
            );

            Console.WriteLine(
                $"Senha: {(string.IsNullOrWhiteSpace(senha) ? "NÃO" : "OK")}"
            );

            Console.WriteLine(
                $"EmpresaId: {(string.IsNullOrWhiteSpace(empresaIdTexto) ? "NÃO" : "OK")}"
            );

            return;
        }

        if (!Guid.TryParse(empresaIdTexto, out var empresaId))
        {
            throw new InvalidOperationException(
                "AdminInicial:EmpresaId inválido."
            );
        }

        email = email.Trim().ToLowerInvariant();

        // Não cria novamente caso já exista.
        var usuarioExiste = await context.Usuarios
            .AnyAsync(x => x.Email == email);

        if (usuarioExiste)
            return;

        var empresaExiste = await context.Empresas
            .AnyAsync(x => x.Id == empresaId);

        if (!empresaExiste)
        {
            throw new InvalidOperationException(
                $"Empresa {empresaId} não encontrada para criação do administrador inicial."
            );
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            Nome = "Administrador",
            Email = email,
            Perfil = "Administrador",
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };

        var passwordHasher = new PasswordHasher<Usuario>();

        usuario.SenhaHash = passwordHasher.HashPassword(
            usuario,
            senha
        );

        context.Usuarios.Add(usuario);

        await context.SaveChangesAsync();

        Console.WriteLine(
            $"Usuário administrador inicial criado: {email}"
        );
    }
}