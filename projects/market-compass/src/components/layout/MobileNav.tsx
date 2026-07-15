import { NavLink } from "react-router-dom";
import { LayoutDashboard, Search, Telescope, Menu } from "lucide-react";
import { clsx } from "clsx";

const ITEMS = [
  { to: "/dashboard", label: "Home", icon: LayoutDashboard },
  { to: "/search", label: "Search", icon: Search },
  { to: "/predict", label: "Predict", icon: Telescope },
  { to: "/settings", label: "More", icon: Menu },
];

export function MobileNav() {
  return (
    <nav className="md:hidden fixed bottom-0 inset-x-0 z-30 flex items-center justify-around h-16 border-t border-border-subtle bg-bg-surface/95 backdrop-blur-xl">
      {ITEMS.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          className={({ isActive }) =>
            clsx(
              "flex flex-col items-center justify-center gap-1 flex-1 h-full text-[11px] font-medium",
              isActive ? "text-brand-400" : "text-text-secondary"
            )
          }
        >
          <item.icon className="h-5 w-5" />
          {item.label}
        </NavLink>
      ))}
    </nav>
  );
}
