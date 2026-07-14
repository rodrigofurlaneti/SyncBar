import type { ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { useAuthStore } from "./stores/authStore";
import { AppShell } from "./components/AppShell";
import { LoginPage } from "./features/auth/LoginPage";
import { SignupPage } from "./features/auth/SignupPage";
import { OrdersPage } from "./features/orders/OrdersPage";
import { ProductsPage } from "./features/catalog/ProductsPage";
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
      <Route
        element={
          <RequireAuth>
            <AppShell />
          </RequireAuth>
        }
      >
        <Route path="/" element={<FeatureGate code="Salao"><OrdersPage /></FeatureGate>} />
        <Route path="/produtos" element={<FeatureGate code="Cardapio"><ProductsPage /></FeatureGate>} />
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
        <Route path="/acessos" element={<ManagerGate><AccessPage /></ManagerGate>} />
        <Route path="/configuracoes" element={<ManagerGate><SettingsPage /></ManagerGate>} />
        <Route path="/sem-acesso" element={<NoAccessPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
