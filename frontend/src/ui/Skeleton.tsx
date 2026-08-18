interface RowProps {
  /** altura da linha, em px — combine com a altura real do conteúdo que ela substitui */
  height?: number;
}

/** Uma linha de skeleton isolada. Usa a classe `.skeleton` que já existe em
 * global.css (shimmer + `prefers-reduced-motion`) mas que, antes desse
 * componente, não era consumida em nenhuma tela. */
export function SkeletonRow({ height = 20 }: RowProps) {
  return <div className="skeleton" style={{ height, width: "100%" }} />;
}

interface ListProps {
  /** quantas linhas mostrar enquanto a consulta carrega */
  rows?: number;
  rowHeight?: number;
}

/**
 * Lista de linhas de skeleton — substitui o texto solto "Carregando…"
 * (presente em só 4 das ~30 telas do app) nas demais telas que hoje não
 * mostram nada durante o `isLoading` do useQuery, evitando o flash de
 * layout vazio.
 */
export function SkeletonList({ rows = 4, rowHeight = 56 }: ListProps) {
  return (
    <div style={{ display: "grid", gap: 10 }} aria-hidden="true">
      {Array.from({ length: rows }).map((_, i) => (
        <SkeletonRow key={i} height={rowHeight} />
      ))}
    </div>
  );
}
