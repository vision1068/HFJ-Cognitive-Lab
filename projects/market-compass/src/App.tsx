import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AppLayout } from "@/components/layout/AppLayout";
import { LandingPage } from "@/pages/LandingPage";
import { AuthPage } from "@/pages/AuthPage";
import { DashboardPage } from "@/pages/DashboardPage";
import { LiveAnalysisPage } from "@/pages/LiveAnalysisPage";
import { SearchPage } from "@/pages/SearchPage";
import { CompanyDetailPage } from "@/pages/CompanyDetailPage";
import { MarketsPage } from "@/pages/MarketsPage";
import { ScreenerPage } from "@/pages/ScreenerPage";
import { WatchlistsPage } from "@/pages/WatchlistsPage";
import { PortfolioPage } from "@/pages/PortfolioPage";
import { NewsPage } from "@/pages/NewsPage";
import { CalendarPage } from "@/pages/CalendarPage";
import { AlertsPage } from "@/pages/AlertsPage";
import { SettingsPage } from "@/pages/SettingsPage";

const queryClient = new QueryClient();

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<LandingPage />} />
          <Route path="/login" element={<AuthPage mode="login" />} />
          <Route path="/signup" element={<AuthPage mode="signup" />} />

          <Route element={<AppLayout />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/live-analysis" element={<LiveAnalysisPage />} />
            <Route path="/search" element={<SearchPage />} />
            <Route path="/company/:ticker" element={<CompanyDetailPage />} />
            <Route path="/markets" element={<MarketsPage />} />
            <Route path="/screener" element={<ScreenerPage />} />
            <Route path="/watchlists" element={<WatchlistsPage />} />
            <Route path="/portfolio" element={<PortfolioPage />} />
            <Route path="/news" element={<NewsPage />} />
            <Route path="/calendar" element={<CalendarPage />} />
            <Route path="/alerts" element={<AlertsPage />} />
            <Route path="/settings" element={<SettingsPage />} />
          </Route>

          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  );
}

export default App;
