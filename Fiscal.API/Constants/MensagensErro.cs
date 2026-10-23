
namespace Fiscal.API.Constants;

public static class MensagensErro
{
    // Autenticação
    public const string CredenciaisInvalidas =
        "E-mail ou senha inválidos.";

    public const string UsuarioInativo =
        "Seu usuário está inativo. Entre em contato com o administrador da empresa.";

    public const string AcessoNegado =
        "Você não possui permissão para realizar esta operação.";

    public const string EmailSenhaObrigatorios =
        "Informe o e-mail e a senha.";

    public const string MultiplasEmpresas =
        "É necessário selecionar a empresa para continuar.";

    // Usuários
    public const string UsuarioNaoEncontrado =
        "Usuário não encontrado.";

    public const string EmailJaCadastrado =
        "Este e-mail já está cadastrado.";

    public const string PerfilInvalido =
        "Perfil de usuário inválido.";

    public const string AdministradorExistente =
        "Esta empresa já possui um Administrador.";

    public const string AdministradorNaoPodeSerDesativado =
        "Não é permitido desativar o Administrador da empresa.";

    // Empresas
    public const string EmpresaNaoEncontrada =
        "Empresa não encontrada.";

    public const string EmpresaNaoPermitida =
        "Não é permitido informar EmpresaId.";

    public const string EmpresaObrigatoriaMaster =
        "O Master deve informar a empresa.";

    // Gerais
    public const string ErroInterno =
        "Ocorreu um erro interno. Tente novamente mais tarde.";
}
