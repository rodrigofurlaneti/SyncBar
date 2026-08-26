import { useThemeStore } from "../../../stores/themeStore";
import { firstNameFrom, initialsFrom } from "../utils";

interface WaiterHeaderProps {
    userName: string | null;
    readyItemsCount: number;
    onBellClick: () => void;
}

export function WaiterHeader({ userName, readyItemsCount, onBellClick }: WaiterHeaderProps) {
    const { theme, toggleTheme } = useThemeStore();

    return (
        <header className="waiter-header">
            <div className="waiter-header-top">
                <div className="waiter-avatar" aria-hidden="true">{initialsFrom(userName)}</div>
                <div className="waiter-greeting">
                    <span className="waiter-greeting-hello">Olá, {firstNameFrom(userName)} 👋</span>
                    <span className="waiter-online-chip">
                        <span className="waiter-online-dot" /> Garçom Online
                    </span>
                </div>
                <span className="waiter-spacer" />
                <button type="button" className="waiter-icon-btn" onClick={toggleTheme}>
                    {theme === "dark" ? "☀" : "🌙"}
                </button>
                <button type="button" className="waiter-icon-btn waiter-bell" onClick={onBellClick}>
                    🔔
                    {readyItemsCount > 0 && <span className="waiter-bell-badge">{readyItemsCount}</span>}
                </button>
            </div>
        </header>
    );
}