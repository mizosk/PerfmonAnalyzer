import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  // 開発サーバーの設定
  server: {
    proxy: {
      // 開発中は localhost:5173 から localhost:5272 へプロキシ
      '/api': {
        target: 'http://localhost:5272',
        changeOrigin: true,
      },
    },
  },
  // ビルド設定
  build: {
    outDir: 'dist', // ビルド出力先
    emptyOutDir: true, // ビルド前に出力先をクリア
  },
})
