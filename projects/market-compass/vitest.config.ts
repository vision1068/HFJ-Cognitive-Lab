import { defineConfig } from "vitest/config";
import path from "path";

// Test runner config kept separate from vite.config.ts so the dev-server proxy
// middleware never loads during unit tests. The `@` -> src alias mirrors
// vite.config.ts so imports resolve identically in tests and in the app.
// `esbuild.jsx: "automatic"` gives component tests the React 19 automatic JSX
// runtime (no per-file `import React`) — the app's tsconfig marks test files
// excluded, so esbuild would otherwise fall back to the classic runtime.
export default defineConfig({
  esbuild: {
    jsx: "automatic",
    jsxImportSource: "react",
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test/setup.ts"],
    // Live-network smoke tests are quarantined and excluded from the CI path.
    exclude: ["**/node_modules/**", "**/dist/**", "**/*.live.test.ts"],
  },
});
