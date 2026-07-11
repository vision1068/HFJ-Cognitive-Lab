import { useEffect } from "react";
import { Outlet } from "react-router-dom";
import { Sidebar } from "./Sidebar";
import { TopNav } from "./TopNav";
import { MobileNav } from "./MobileNav";
import { RightPanel } from "./RightPanel";
import { DataAttribution } from "@/components/market/DataAttribution";
import { useAppStore } from "@/store/useAppStore";

export function AppLayout() {
  const theme = useAppStore((s) => s.theme);

  useEffect(() => {
    document.documentElement.classList.toggle("light", theme === "light");
    document.documentElement.classList.toggle("dark", theme === "dark");
  }, [theme]);

  return (
    <div className="flex h-screen overflow-hidden">
      <Sidebar />
      <div className="flex-1 flex flex-col min-w-0">
        <TopNav />
        <main className="flex-1 overflow-y-auto pb-20 md:pb-0">
          <Outlet />
          <footer className="border-t border-border-subtle px-4 md:px-6 py-4 mt-4">
            <DataAttribution />
          </footer>
        </main>
      </div>
      <RightPanel />
      <MobileNav />
    </div>
  );
}
