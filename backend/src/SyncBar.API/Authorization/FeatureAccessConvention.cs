using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace SyncBar.API.Authorization;

// Uma única definição governa o vínculo dos endpoints às funcionalidades.
public sealed class FeatureAccessConvention : IControllerModelConvention
{
    public static readonly IReadOnlyDictionary<string, string> Controllers = new Dictionary<string, string>
    {
        ["Orders"] = "Salao,Preparo,Caixa", ["Tables"] = "Salao", ["Comandas"] = "Salao",
        ["DiningAreas"] = "Salao", ["Reservations"] = "Salao", ["Customers"] = "Salao,Caixa",
        ["CustomerAddresses"] = "Salao", ["Products"] = "Cardapio,Salao,Estoque",
        ["Categories"] = "Cardapio,Salao", ["Catalog"] = "Cardapio,Salao",
        ["Complements"] = "Cardapio,Salao", ["Pizza"] = "Cardapio,Salao",
        ["Stock"] = "Estoque", ["Suppliers"] = "Estoque", ["Purchases"] = "Estoque",
        ["Employees"] = "Equipe,Usuarios,Salao", ["Users"] = "Usuarios",
        ["Cash"] = "Caixa,Salao", ["ShiftClosing"] = "Caixa", ["Sales"] = "Caixa,Faturamento,Salao",
        ["Payments"] = "Caixa,Faturamento,Salao", ["Finance"] = "Faturamento",
        ["Preparation"] = "Preparo", ["Promotions"] = "Promocoes",
        ["Printing"] = "Impressao,Salao,Caixa", ["Printers"] = "Impressao"
    };

    public void Apply(ControllerModel controller)
    {
        if (Controllers.TryGetValue(controller.ControllerName, out var features))
            controller.Filters.Add(new AuthorizeFilter(new[] { new AuthorizeAttribute { Policy = $"Feature:{features}" } }));
    }
}
