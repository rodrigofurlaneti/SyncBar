import type { ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { useAuthStore } from "./stores/authStore";
import { AppShell } from "./components/AppShell";
import { LoginPage } from "./features/auth/LoginPage";
import { SignupPage } from "./features/auth/SignupPage";
import { OrdersPage } from "./features/orders/OrdersPage";
import { ProductsPage } from "./features/catalog/ProductsPage";
import { ComplementsPage } from "./features/catalog/ComplementsPage";
import { StockPage } from "./features/stock/StockPage";
import { EmployeesPage } from "./features/employees/EmployeesPage";
import { UsersPage } from "./features/users/UsersPage";
import { AccessPage } from "./features/access/AccessPage";
import { SettingsPage } from "./features/settings/SettingsPage";
import { FinancePage } from "./features/finance/FinancePage";
import { ScenariosPage } from "./features/finance/ScenariosPage";
import { ReportsPage } from "./features/finance/ReportsPage";
import { PreparationPage } from "./features/preparation/PreparationPage";
import { CashHistoryPage } from "./features/cash/CashHistoryPage";
import { PromotionsPage } from "./features/promotions/PromotionsPage";
import { PrintingPage } from "./features/printing/PrintingPage";
import { PurchasingPage } from "./features/purchasing/PurchasingPage";
import { ReservationsPage } from "./features/reservations/ReservationsPage";
import { CustomersPage } from "./features/customers/CustomersPage";
import { PublicOrderPage } from "./features/publicOrdering/PublicOrderPage";
import { IFoodIntegrationPage } from "./features/integrations/IFoodIntegrationPage";
import { IFoodOrdersPage } from "./features/integrations/IFoodOrdersPage";
import { IFoodShippingPage } from "./features/integrations/IFoodShippingPage";
import { IFoodLogisticsPage } from "./features/integrations/IFoodLogisticsPage";
import { IFoodCatalogPage } from "./features/integrations/IFoodCatalogPage";
import { IFoodReviewsPage } from "./features/integrations/IFoodReviewsPage";
import { IFoodAnalyticsPage } from "./features/integrations/IFoodAnalyticsPage";
import { IFoodFinancialReportsPage } from "./features/integrations/IFoodFinancialReportsPage";
import { IFoodDashboardPage } from "./features/integrations/IFoodDashboardPage";
import { IFoodStatusDetailedPage } from "./features/integrations/IFoodStatusDetailedPage";
import { IFoodReviewsDetailedPage } from "./features/integrations/IFoodReviewsDetailedPage";
import { IFoodAnalyticsEnhancedPage } from "./features/integrations/IFoodAnalyticsEnhancedPage";
import { FeatureGate, NoAccessPage } from "./features/access/FeatureGate";
import { useMyFeatures } from "./features/access/hooks";

function ManagerGate({ children }: { children: ReactNode }) {
  const featuresQuery = useMyFeatures();
  if (featuresQuery.isLoading) return null;
  if (featuresQuery.data?.canManageAccess) return <>{children}</>;
  return <Navigate to="/" replace />;
}

function RequireAuth({ children }: { children: ReactNode }) {
  const accessToken = useAuthStore((s) => s.accessToken);
  if (!accessToken) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/cadastro" element={<SignupPage />} />
      <Route path="/pedido/:token" element={<PublicOrderPage />} />
      <Route
        element={
          <RequireAuth>
            <AppShell />
          </RequireAuth>
        }
      >
        <Route path="/" element={<FeatureGate code="Salao"><OrdersPage /></FeatureGate>} />
        <Route path="/produtos" element={<FeatureGate code="Cardapio"><ProductsPage /></FeatureGate>} />
        <Route path="/complementos" element={<FeatureGate code="Cardapio"><ComplementsPage /></FeatureGate>} />
        <Route path="/estoque" element={<FeatureGate code="Estoque"><StockPage /></FeatureGate>} />
        <Route path="/equipe" element={<ManagerGate><EmployeesPage /></ManagerGate>} />
        <Route path="/usuarios" element={<ManagerGate><UsersPage /></ManagerGate>} />
        <Route path="/faturamento" element={<ManagerGate><FinancePage /></ManagerGate>} />
        <Route path="/cenarios" element={<ManagerGate><ScenariosPage /></ManagerGate>} />
        <Route path="/relatorios" element={<ManagerGate><ReportsPage /></ManagerGate>} />
        <Route path="/preparo" element={<FeatureGate code="Preparo"><PreparationPage /></FeatureGate>} />
        <Route path="/fechamentos" element={<ManagerGate><CashHistoryPage /></ManagerGate>} />
        <Route path="/promocoes" element={<ManagerGate><PromotionsPage /></ManagerGate>} />
        <Route path="/impressao" element={<ManagerGate><PrintingPage /></ManagerGate>} />
        <Route path="/compras" element={<FeatureGate code="Estoque"><PurchasingPage /></FeatureGate>} />
        <Route path="/reservas" element={<FeatureGate code="Salao"><ReservationsPage /></FeatureGate>} />
        <Route path="/clientes" element={<FeatureGate code="Salao"><CustomersPage /></FeatureGate>} />
        <Route path="/acessos" element={<ManagerGate><AccessPage /></ManagerGate>} />
        <Route path="/configuracoes" element={<ManagerGate><SettingsPage /></ManagerGate>} />
        <Route path="/integracoes/ifood" element={<ManagerGate><IFoodIntegrationPage /></ManagerGate>} />
        <Route path="/integracoes/ifood/dashboard" element={<ManagerGate><IFoodDashboardPage /></ManagerGate>} />
        <Route path="/integracoes/ifood/status" element={<ManagerGate><IFoodStatusDetailedPage /></ManagerGate>} />
        <Route path="/integracoes/ifood/pedidos" element={<ManagerGate><IFoodOrdersPage /></ManagerGate>} />
        <Route path="/integracoes/ifood/shipping" element={<ManagerGate><IFoodShippingPage /></ManagerGate>} />
        <Route path="/integracoes/ifood/logistica" element={<ManagerGate><IFoodLogisticsPage /></ManagerGate>} />
        <Route path="/integracoes/ifood/catalogo" element={<ManagerGate><IFoodCatalogPage /></ManagerGate>} />
        <Route path="/integracoes/ifood/avaliacoes" element={<ManagerGate><IFoodReviewsDetailedPage /></ManagerGate>} />
        <Route path="/integracoes/ifood/indicadores" element={<ManagerGate><IFoodAnalyticsEnhancedPage /></ManagerGate>} />
        <Route path="/integracoes/ifood/financeiro/relatorios" element={<ManagerGate><IFoodFinancialReportsPage /></ManagerGate>} />
        <Route path="/sem-acesso" element={<NoAccessPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
