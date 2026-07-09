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
        <Route path="/" element={<OrdersPage />} />
        <Route path="/produtos" element={<ProductsPage />} />
        <Route path="/estoque" element={<StockPage />} />
        <Route path="/equipe" element={<EmployeesPage />} />
        <Route path="/usuarios" element={<UsersPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
