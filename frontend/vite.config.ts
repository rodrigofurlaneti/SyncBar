import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Proxy para a SyncBar.API — evita CORS em desenvolvimento.
// Porta HTTP do launchSettings: 5250 (a HTTPS 7250 usa certificado de dev).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: "http://localhost:5250",
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
