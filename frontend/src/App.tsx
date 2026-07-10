import type { ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { useAuthStore } from "./stores/authStore";
import { AppShell } from "./components/AppShell";
import { LoginPage } from "./features/auth/LoginPage";
import { OrdersPage } from "./features/orders/OrdersPage";
import { ProductsPage } from "./features/catalog/ProductsPage";
import { StockPage } from "./features/stock/StockPage";
import { EmployeesPage } from "./features/employees/EmployeesPage";
import { UsersPage } from "./features/users/UsersPage";
import { AccessPage } from "./features/access/AccessPage";
import { FinancePage } from "./features/finance/FinancePage";
import { ScenariosPage } from "./features/finance/ScenariosPage";
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
        <Route path="/equipe" element={<FeatureGate code="Equipe"><EmployeesPage /></FeatureGate>} />
        <Route path="/usuarios" element={<FeatureGate code="Usuarios"><UsersPage /></FeatureGate>} />
        <Route path="/faturamento" element={<FeatureGate code="Faturamento"><FinancePage /></FeatureGate>} />
        <Route path="/cenarios" element={<FeatureGate code="Faturamento"><ScenariosPage /></FeatureGate>} />
        <Route path="/acessos" element={<ManagerGate><AccessPage /></ManagerGate>} />
        <Route path="/sem-acesso" element={<NoAccessPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
