import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    // O site importa o código do paciente de ../supabase/functions/_shared (fonte única da regra)
    fs: { allow: [".."] },
  },
});
