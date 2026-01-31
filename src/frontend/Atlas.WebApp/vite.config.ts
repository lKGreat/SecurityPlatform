import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import path from "node:path";

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "src"),
      "@popperjs/core/lib/modifiers/arrow": path.resolve(__dirname, "src/compat/popper/modifiers/arrow.ts"),
      "@popperjs/core/lib/modifiers/computeStyles": path.resolve(
        __dirname,
        "src/compat/popper/modifiers/computeStyles.ts"
      ),
      "@popperjs/core/lib/modifiers/eventListeners": path.resolve(
        __dirname,
        "src/compat/popper/modifiers/eventListeners.ts"
      ),
      "@popperjs/core/lib/modifiers/flip": path.resolve(__dirname, "src/compat/popper/modifiers/flip.ts"),
      "@popperjs/core/lib/modifiers/hide": path.resolve(__dirname, "src/compat/popper/modifiers/hide.ts"),
      "@popperjs/core/lib/modifiers/offset": path.resolve(__dirname, "src/compat/popper/modifiers/offset.ts"),
      "@popperjs/core/lib/modifiers/popperOffsets": path.resolve(
        __dirname,
        "src/compat/popper/modifiers/popperOffsets.ts"
      ),
      "@popperjs/core/lib/modifiers/preventOverflow": path.resolve(
        __dirname,
        "src/compat/popper/modifiers/preventOverflow.ts"
      ),
      "@popperjs/core/lib/enums": path.resolve(__dirname, "src/compat/popper/enums.ts"),
      "@popperjs/core/lib/popper-base": path.resolve(__dirname, "src/compat/popper/popper-base.ts")
    }
  },
  server: {
    host: "0.0.0.0",
    port: 5173,
    open: true,
    proxy: {
      "/api/v1": {
        target: "http://localhost:5000",
        changeOrigin: true,
        secure: false
      }
    }
  },
  build: {
    chunkSizeWarningLimit: 2000,
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (!id.includes("node_modules")) {
            return undefined;
          }
          if (id.includes("vform3-builds")) {
            return "vendor-vform";
          }
          if (id.includes("@antv/x6")) {
            return "vendor-x6";
          }
          if (id.includes("ant-design-vue") || id.includes("@ant-design")) {
            return "vendor-antd";
          }
          if (id.includes("element-plus")) {
            return "vendor-element";
          }
          if (id.includes("vue-router")) {
            return "vendor-router";
          }
          return "vendor";
        }
      }
    }
  }
});
