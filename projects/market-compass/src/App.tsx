import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AppLayout } from "@/components/layout/AppLayout";
import { LandingPage } from "@/pages/LandingPage";
import { AuthPage } from "@/pages/AuthPage";
import { DashboardPage } from "@/pages/DashboardPage";
import { LiveAnalysisPage } from "@/pages/LiveAnalysisPage";
import { SearchPage } from "@/pages/SearchPage";
import { CompanyDetailPage } from "@/pages/CompanyDetailPage";
import { PredictPage } from "@/pages/PredictPage";
import { MarketsPage } from "@/pages/MarketsPage";
import { SettingsPage } from "@/pages/SettingsPage";

const queryClient = new QueryClient();

// GitHub Pages serves this app from a subpath in production (see vite.config.ts `base`).
// import.meta.env.BASE_URL already reflects that, so the router and the app agree.
const routerBasename = import.meta.env.BASE_URL.replace(/\/$/, "");

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter basename={routerBasename}>
        <Routes>
          <Route path="/" element={<LandingPage />} />
          <Route path="/login" element={<AuthPage mode="login" />} />
          <Route path="/signup" element={<AuthPage mode="signup" />} />

          <Route element={<AppLayout />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/live-analysis" element={<LiveAnalysisPage />} />
            <Route path="/search" element={<SearchPage />} />
            <Route path="/company/:ticker" element={<CompanyDetailPage />} />
            <Route path="/predict" element={<PredictPage />} />
            <Route path="/markets" element={<MarketsPage />} />
            <Route path="/settings" element={<SettingsPage />} />
          </Route>

          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  );
}

export default App;
