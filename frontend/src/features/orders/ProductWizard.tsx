import { memo, useEffect, useId, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { formatBRL } from "../../lib/types";
import { useProductWizard } from "./useProductWizard";
import type { WizardOption, WizardStep } from "./useProductWizard";
import "./ProductWizard.css";

interface Props {
    name: string;
    description?: string | null;
    imageUrl?: string | null;
    basePrice: number;
    steps: WizardStep[];
    loading?: boolean;
    loadError?: boolean;
    onRetry?: () => void;
    submitting?: boolean;
    error?: string | null;
    onCancel: () => void;
    onConfirm: (selected: Record<string, number[]>) => void;
}

const ProductIdentity = memo(function ProductIdentity({ name, description, imageUrl }: Pick<Props, "name" | "description" | "imageUrl">) {
    const [failedImage, setFailedImage] = useState(false);
    return <div className="pw-product">
        {imageUrl && !failedImage
            ? <img className="pw-photo" src={imageUrl} alt={name} onError={() => setFailedImage(true)} />
            : <div className="pw-photo pw-placeholder" aria-hidden="true"><span>✦</span><small>Feito do seu jeito</small></div>}
        <div><p className="pw-eyebrow">DO SEU JEITO</p><h2>{name}</h2>
            {description && <p className="pw-description">{description}</p>}</div>
    </div>;
});

const WizardSidebar = memo(function WizardSidebar({ steps, active, onStep, disabled }: {
    steps: WizardStep[]; active: number; onStep: (index: number) => void; disabled: boolean;
}) {
    return <nav className="pw-steps" aria-label="Etapas da personalização">
        {steps.map((step, index) => <button type="button" key={step.id} disabled={disabled}
            aria-current={index === active ? "step" : undefined} onClick={() => onStep(index)}>
            <span className="pw-step-number">{index + 1}</span><span>{step.title}</span>
            <span className="pw-step-arrow" aria-hidden="true">›</span>
        </button>)}
    </nav>;
});

function OptionItem({ option, checked, disabled, onToggle, groupId }: {
    option: WizardOption; checked: boolean; disabled: boolean; onToggle: () => void; groupId?: number;
}) {
    return <label className={`pw-option${checked ? " is-selected" : ""}${disabled ? " is-disabled" : ""}`}
        data-testid={groupId ? `complement-label-${option.id}` : undefined}>
        <input type="checkbox" checked={checked} disabled={disabled} onChange={onToggle}
            data-testid={groupId ? `input-complement-${option.id}` : undefined} />
        <span className="pw-check" aria-hidden="true">{checked && "✓"}</span>
        <span className="pw-option-name">{option.name}</span>
        <span className="pw-price">{option.price > 0 ? `+ ${formatBRL(option.price)}` : "Grátis"}</span>
    </label>;
}

export function ProductWizard(props: Props) {
    const { name, steps, onCancel, onConfirm, loading = false, loadError = false, submitting = false, error } = props;
    const wizard = useProductWizard(steps, props.basePrice);
    const dialogRef = useRef<HTMLDialogElement>(null);
    const headingRef = useRef<HTMLHeadingElement>(null);
    const titleId = useId();
    const stepTitleId = useId();
    const busy = submitting || loading || loadError;
    const choices = wizard.selected[wizard.step?.id] ?? [];
    useEffect(() => {
        const dialog = dialogRef.current;
        const previous = document.activeElement as HTMLElement | null;
        const overflow = document.body.style.overflow;
        document.body.style.overflow = "hidden";
        dialog?.showModal();
        return () => { dialog?.close(); document.body.style.overflow = overflow; previous?.focus(); };
    }, []);
    useEffect(() => { if (!loading && !loadError) headingRef.current?.focus(); }, [wizard.active, loading, loadError]);

    return createPortal(<dialog ref={dialogRef} className="product-wizard" aria-labelledby={titleId}
        onCancel={event => { event.preventDefault(); if (!submitting) onCancel(); }}
        onKeyDown={event => {
            if (event.key !== "Tab") return;
            const items = Array.from(dialogRef.current?.querySelectorAll<HTMLElement>(
                'button:not(:disabled), input:not(:disabled), [tabindex="0"]') ?? []).filter(item => item.getClientRects().length > 0);
            const first = items[0], last = items[items.length - 1];
            if (event.shiftKey && (document.activeElement === first || document.activeElement === headingRef.current)) {
                event.preventDefault(); last?.focus();
            } else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
        }}>
        <div className="pw-layout" data-testid="complement-selector-view">
            <h1 id={titleId} className="pw-sr-only">Personalizar — {name}</h1>
            <button className="pw-close" type="button" onClick={onCancel} disabled={submitting} aria-label="Fechar personalização">×</button>
            <aside className="pw-sidebar">
                <ProductIdentity name={name} description={props.description} imageUrl={props.imageUrl} />
                <WizardSidebar steps={steps} active={wizard.active} onStep={wizard.setActive} disabled={busy} />
                <p className="pw-sidebar-note">Escolha os detalhes.<br />A gente prepara para você.</p>
            </aside>
            <section className="pw-content" aria-busy={loading}>
                {loading ? <div className="pw-skeleton" role="status" aria-label="Carregando opções">
                    <div /><div /><div /><div /><span className="pw-sr-only">Carregando opções…</span>
                </div> : loadError ? <div className="pw-empty"><p role="alert">Não foi possível carregar as opções deste produto.</p>
                    <button className="pw-secondary" onClick={props.onRetry}>Tentar novamente</button></div>
                    : <div key={wizard.step?.id ?? "empty"} className="pw-step-content">
                        <p className="pw-eyebrow">{steps.length ? `ETAPA ${wizard.active + 1} DE ${steps.length}` : "PRONTO PARA ADICIONAR"}</p>
                        <div className="pw-heading"><h2 ref={headingRef} tabIndex={-1} id={stepTitleId}>{wizard.step?.title ?? "Tudo pronto"}</h2>
                            {wizard.step && <span>Até {wizard.step.max} {wizard.step.max === 1 ? "opção" : "opções"}</span>}</div>
                        <p className="pw-hint">{wizard.step?.min ? `Escolha pelo menos ${wizard.step.min} ${wizard.step.min === 1 ? "opção" : "opções"} para continuar.` : "Escolha o que combina com você. Esta etapa é opcional."}</p>
                        {wizard.step ? <fieldset aria-labelledby={stepTitleId} className="pw-options"
                            data-testid={wizard.step.groupId ? `group-container-${wizard.step.groupId}` : undefined}>
                            {wizard.step.options.map(option => <OptionItem key={option.id} option={option}
                                groupId={wizard.step.groupId} checked={choices.includes(option.id)}
                                disabled={submitting || (!choices.includes(option.id) && choices.length >= wizard.step.max)}
                                onToggle={() => wizard.toggle(wizard.step, option.id)} />)}
                            {!wizard.step.options.length && <p>Nenhuma opção disponível nesta etapa.</p>}
                        </fieldset> : <p className="pw-empty">Nenhum opcional ou adicional disponível. Você pode adicionar o produto sem essas opções.</p>}
                        {wizard.step && <p className="pw-selection-count" role="status">{wizard.count} de {wizard.step.max} selecionadas</p>}
                    </div>}
            </section>
            <div className="pw-subtotal" data-testid="customization-subtotal" aria-live="polite" aria-atomic="true">
                <span>Subtotal <small>1 unidade</small></span><strong>{formatBRL(wizard.subtotal)}</strong>
            </div>
            <footer className="pw-actions">
                {error && <p className="pw-error" role="alert">{error}</p>}
                {!busy && wizard.last && !wizard.allValid && <p className="pw-error">Complete as escolhas obrigatórias nas etapas anteriores.</p>}
                {!loading && !loadError && <div className="pw-action-row">
                    {wizard.active > 0 && <button type="button" className="pw-back" disabled={submitting}
                        onClick={() => wizard.setActive(wizard.active - 1)} aria-label="Voltar à etapa anterior">←</button>}
                    <button type="button" className="pw-primary" data-testid="btn-confirm-complements"
                        disabled={submitting || !wizard.valid || (wizard.last && !wizard.allValid)}
                        onClick={() => {
                            if (!steps.length || wizard.last) { if (wizard.allValid) onConfirm(wizard.selected); }
                            else wizard.setActive(wizard.active + 1);
                        }}>
                        <span>{submitting ? "Adicionando…" : !steps.length ? "Adicionar ao pedido" : wizard.label}</span>
                        <span aria-hidden="true">{wizard.last || !steps.length ? "✓" : "→"}</span>
                    </button>
                </div>}
            </footer>
        </div>
    </dialog>, document.body);
}
