import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Compass, Mail, Lock, User, ArrowRight } from "lucide-react";
import { Disclaimer } from "@/components/ui/Disclaimer";

export function AuthPage({ mode }: { mode: "login" | "signup" }) {
  const navigate = useNavigate();
  const [submitting, setSubmitting] = useState(false);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setTimeout(() => navigate("/dashboard"), 400);
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-bg-base px-4 py-10">
      <div className="w-full max-w-md">
        <Link to="/" className="flex items-center justify-center gap-2 mb-8">
          <div className="flex items-center justify-center h-9 w-9 rounded-xl bg-gradient-to-br from-brand-500 to-accent-violet">
            <Compass className="h-5 w-5 text-white" />
          </div>
          <span className="font-bold text-lg text-text-primary tracking-tight">Market Compass</span>
        </Link>

        <div className="card p-8">
          <h1 className="text-xl font-bold text-text-primary mb-1">
            {mode === "login" ? "Welcome back" : "Create your account"}
          </h1>
          <p className="text-sm text-text-secondary mb-6">
            {mode === "login" ? "Log in to access your watchlists and portfolio." : "Start researching companies across PSX and global markets."}
          </p>

          <form onSubmit={handleSubmit} className="space-y-4">
            {mode === "signup" && (
              <div>
                <label className="text-xs font-medium text-text-secondary mb-1.5 block">Full name</label>
                <div className="relative">
                  <User className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-secondary" />
                  <input
                    required
                    type="text"
                    placeholder="Jane Investor"
                    className="w-full bg-bg-elevated border border-border-subtle rounded-lg pl-9 pr-3 py-2.5 text-sm outline-none focus:border-brand-500 transition-colors"
                  />
                </div>
              </div>
            )}
            <div>
              <label className="text-xs font-medium text-text-secondary mb-1.5 block">Email</label>
              <div className="relative">
                <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-secondary" />
                <input
                  required
                  type="email"
                  placeholder="you@example.com"
                  className="w-full bg-bg-elevated border border-border-subtle rounded-lg pl-9 pr-3 py-2.5 text-sm outline-none focus:border-brand-500 transition-colors"
                />
              </div>
            </div>
            <div>
              <label className="text-xs font-medium text-text-secondary mb-1.5 block">Password</label>
              <div className="relative">
                <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-secondary" />
                <input
                  required
                  type="password"
                  placeholder="••••••••"
                  className="w-full bg-bg-elevated border border-border-subtle rounded-lg pl-9 pr-3 py-2.5 text-sm outline-none focus:border-brand-500 transition-colors"
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={submitting}
              className="w-full flex items-center justify-center gap-2 bg-brand-500 hover:bg-brand-600 disabled:opacity-60 text-white font-medium px-4 py-2.5 rounded-lg transition-colors"
            >
              {submitting ? "Please wait..." : mode === "login" ? "Log in" : "Create account"}
              {!submitting && <ArrowRight className="h-4 w-4" />}
            </button>
          </form>

          <p className="text-sm text-text-secondary text-center mt-5">
            {mode === "login" ? (
              <>
                Don't have an account? <Link to="/signup" className="text-brand-400 font-medium">Sign up</Link>
              </>
            ) : (
              <>
                Already have an account? <Link to="/login" className="text-brand-400 font-medium">Log in</Link>
              </>
            )}
          </p>
        </div>

        <Disclaimer compact className="mt-6 justify-center text-center" />
      </div>
    </div>
  );
}
