namespace SyncBar.Domain.Constants;

// Codigos das telas seedadas em BarRestaurante_FeatureAccess.sql.
public static class FeatureCodes
{
    public const string Salao = "Salao";
    public const string Cardapio = "Cardapio";
    public const string Estoque = "Estoque";
    public const string Equipe = "Equipe";
    public const string Usuarios = "Usuarios";
    public const string Caixa = "Caixa";
    public const string Faturamento = "Faturamento";

    public static readonly string[] All = [Salao, Cardapio, Estoque, Equipe, Usuarios, Caixa, Faturamento];

    // Perfis com acesso total e direito de conceder acessos.
    public static readonly string[] ManagerRoles = ["Administrador", "Gerente"];
}
