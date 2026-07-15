import { Moon, Sun, User, Telescope } from "lucide-react";
import { GlobalSearch } from "./GlobalSearch";
import { useAppStore } from "@/store/useAppStore";
import { Link } from "react-router-dom";

export function TopNav() {
  const theme = useAppStore((s) => s.theme);
  const toggleTheme = useAppStore((s) => s.toggleTheme);

  return (
    <header className="flex items-center gap-4 h-16 px-4 md:px-6 border-b border-border-subtle bg-bg-surface/80 backdrop-blur-xl sticky top-0 z-30">
      <div className="flex-1 max-w-xl">
        <GlobalSearch />
      </div>
      <div className="flex items-center gap-1.5 ml-auto">
        <Link
          to="/predict"
          className="flex items-center justify-center h-9 w-9 rounded-lg text-text-secondary hover:bg-bg-hover hover:text-text-primary transition-colors"
          aria-label="Predict"
          title="Predict"
        >
          <Telescope className="h-[18px] w-[18px]" />
        </Link>
        <button
          onClick={toggleTheme}
          className="flex items-center justify-center h-9 w-9 rounded-lg text-text-secondary hover:bg-bg-hover hover:text-text-primary transition-colors"
          aria-label="Toggle theme"
        >
          {theme === "dark" ? <Sun className="h-[18px] w-[18px]" /> : <Moon className="h-[18px] w-[18px]" />}
        </button>
        <Link
          to="/settings"
          className="flex items-center justify-center h-9 w-9 rounded-full bg-gradient-to-br from-brand-500 to-accent-violet text-white"
        >
          <User className="h-[18px] w-[18px]" />
        </Link>
      </div>
    </header>
  );
}
