import { TabKey, tabs } from "../utils";

interface WaiterTabBarProps {
    activeTab: TabKey;
    onTabChange: (key: TabKey) => void;
}

export function WaiterTabBar({ activeTab, onTabChange }: WaiterTabBarProps) {
    return (
        <nav className="waiter-tabbar" aria-label="Navegação do Modo Garçom">
            {tabs.map((tab) => (
                <button
                    key={tab.key}
                    type="button"
                    className={`waiter-tab${tab.key === activeTab ? " is-active" : ""}`}
                    onClick={() => onTabChange(tab.key)}
                >
                    <span aria-hidden="true">{tab.icon}</span>
                    <span>{tab.label}</span>
                </button>
            ))}
        </nav>
    );
}