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
    public const string Preparo = "Preparo";
    public const string Promocoes = "Promocoes";
    public const string Impressao = "Impressao";

    public static readonly string[] All = [Salao, Cardapio, Estoque, Equipe, Usuarios, Caixa, Faturamento, Preparo, Promocoes, Impressao];

    // Perfis com acesso total e direito de conceder acessos.
    public static readonly string[] ManagerRoles = ["Administrador", "Gerente"];
}
