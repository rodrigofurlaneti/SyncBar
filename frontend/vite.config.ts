import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Proxy para a SyncBar.API — evita CORS em desenvolvimento.
// Porta HTTP do launchSettings: 64235 (a HTTPS 64234 usa certificado de dev).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: "http://localhost:64235",
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
