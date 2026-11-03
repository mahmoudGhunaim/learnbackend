import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// Dev: Vite serves React on :5173 and forwards /api calls to the ASP.NET Core API on :5064
// (so the browser sees one origin and no CORS setup is needed).
// Build: the output goes straight into the API's wwwroot, served by app.UseStaticFiles().
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5064',
    },
  },
  build: {
    outDir: '../Backend/LearnBackend.Api/wwwroot',
    emptyOutDir: true,
    chunkSizeWarningLimit: 4000,
  },
});
