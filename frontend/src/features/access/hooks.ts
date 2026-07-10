import { useQuery } from "@tanstack/react-query";
import { getMyFeatures } from "./api";
import { useAuthStore } from "../../stores/authStore";

export function useMyFeatures() {
  const accessToken = useAuthStore((s) => s.accessToken);
  const userName = useAuthStore((s) => s.userName);
  return useQuery({
    // userName na chave: trocar de usuario NUNCA reaproveita o cache do anterior.
    queryKey: ["access", "my", userName],
    queryFn: getMyFeatures,
    enabled: accessToken !== null,
    staleTime: 60_000,
  });
}

export const featurePath: Record<string, string> = {
  Salao: "/",
  Cardapio: "/produtos",
  Estoque: "/estoque",
  Equipe: "/equipe",
  Usuarios: "/usuarios",
  Faturamento: "/faturamento",
  Preparo: "/preparo",
};
