import { fileURLToPath, URL } from 'node:url'

import vue from '@vitejs/plugin-vue'
import vueJsx from '@vitejs/plugin-vue-jsx'
import AutoImport from 'unplugin-auto-import/vite'
import { NaiveUiResolver } from 'unplugin-vue-components/resolvers'
import Components from 'unplugin-vue-components/vite'
import { defineConfig, loadEnv } from 'vite'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd())

  return {
    plugins: [
      vue(),
      vueJsx(),
      AutoImport({
        imports: ['vue', 'vue-router', 'pinia', '@vueuse/core'],
        dts: 'src/types/auto-imports.d.ts',
      }),
      Components({
        resolvers: [NaiveUiResolver()],
        dts: 'src/types/components.d.ts',
      }),
    ],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
        '~': fileURLToPath(new URL('./packages', import.meta.url)),
      },
    },
    css: {
      preprocessorOptions: {},
    },
    server: {
      host: '0.0.0.0',
      port: Number(env.VITE_PORT) || 9000,
      proxy: {
        // 将 /api/* 请求转发给后端，绕过浏览器 CORS 限制
        // VITE_API_BASE_URL 开发环境留空，生产环境填写实际地址
        [env.VITE_API_PREFIX || '/api']: {
          target: 'http://localhost:9708',
          changeOrigin: true,
        },
      },
    },
    build: {
      target: 'es2022',
      chunkSizeWarningLimit: 2000,
      rollupOptions: {
        output: {
          chunkFileNames: 'assets/js/[name]-[hash].js',
          entryFileNames: 'assets/js/[name]-[hash].js',
          assetFileNames: 'assets/[ext]/[name]-[hash].[ext]',
          manualChunks: {
            'vendor-vue': ['vue', 'vue-router', 'pinia'],
            'vendor-naive': ['naive-ui'],
            'vendor-utils': ['axios', 'dayjs', '@vueuse/core'],
          },
        },
      },
    },
  }
})
