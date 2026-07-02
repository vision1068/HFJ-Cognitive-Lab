import { NavLink } from "react-router-dom";
import { clsx } from "clsx";
import {
  LayoutDashboard,
  Activity,
  Globe2,
  Search,
  SlidersHorizontal,
  Eye,
  Briefcase,
  Newspaper,
  Calendar,
  Bell,
  Settings,
  Compass,
  ChevronsLeft,
} from "lucide-react";
import { useAppStore } from "@/store/useAppStore";

const NAV_ITEMS = [
  { to: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  { to: "/live-analysis", label: "Live Analysis", icon: Activity },
  { to: "/markets", label: "Markets", icon: Globe2 },
  { to: "/search", label: "Company Search", icon: Search },
  { to: "/screener", label: "Screener", icon: SlidersHorizontal },
  { to: "/watchlists", label: "Watchlists", icon: Eye },
  { to: "/portfolio", label: "Portfolio", icon: Briefcase },
  { to: "/news", label: "News & Insights", icon: Newspaper },
  { to: "/calendar", label: "Economic Calendar", icon: Calendar },
  { to: "/alerts", label: "Alerts", icon: Bell },
  { to: "/settings", label: "Settings", icon: Settings },
];

export function Sidebar() {
  const collapsed = useAppStore((s) => s.sidebarCollapsed);
  const toggleSidebar = useAppStore((s) => s.toggleSidebar);

  return (
    <aside
      className={clsx(
        "hidden md:flex flex-col shrink-0 border-r border-border-subtle bg-bg-surface transition-all duration-200",
        collapsed ? "w-[72px]" : "w-64"
      )}
    >
      <div className="flex items-center gap-2 px-4 h-16 border-b border-border-subtle">
        <div className="flex items-center justify-center h-9 w-9 rounded-xl bg-gradient-to-br from-brand-500 to-accent-violet shrink-0">
          <Compass className="h-5 w-5 text-white" />
        </div>
        {!collapsed && (
          <span className="font-bold text-text-primary tracking-tight whitespace-nowrap">Market Compass</span>
        )}
      </div>

      <nav className="flex-1 overflow-y-auto py-4 px-3 space-y-1">
        {NAV_ITEMS.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) =>
              clsx(
                "flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors",
                isActive
                  ? "bg-brand-500/15 text-brand-400"
                  : "text-text-secondary hover:bg-bg-hover hover:text-text-primary"
              )
            }
            title={collapsed ? item.label : undefined}
          >
            <item.icon className="h-[18px] w-[18px] shrink-0" />
            {!collapsed && <span className="whitespace-nowrap">{item.label}</span>}
          </NavLink>
        ))}
      </nav>

      <button
        onClick={toggleSidebar}
        className="flex items-center gap-2 px-4 py-4 border-t border-border-subtle text-text-secondary hover:text-text-primary text-sm"
      >
        <ChevronsLeft className={clsx("h-4 w-4 transition-transform", collapsed && "rotate-180")} />
        {!collapsed && "Collapse"}
      </button>
    </aside>
  );
}
