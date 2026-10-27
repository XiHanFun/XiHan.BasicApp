import type { ComponentResolver } from 'unplugin-vue-components'
import process from 'node:process'
import { fileURLToPath, URL } from 'node:url'
import tailwindcss from '@tailwindcss/vite'
import vue from '@vitejs/plugin-vue'
import Components from 'unplugin-vue-components/vite'
import { defineConfig, loadEnv } from 'vite'

/**
 * XiHan.UI 是解剖式组件库：模板里的 `Xh*` 标签直接映射到 `@xihan-ui/vue` 的具名导出，免去逐个 import，摇树不受影响。
 */
function XiHanUiResolver(): ComponentResolver {
  return {
    type: 'component',
    resolve(name: string) {
      return name.startsWith('Xh') ? { name, from: '@xihan-ui/vue' } : undefined
    },
  }
}

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  // 开发态把 /api、/openapi.json、/mcp、/health 代理到后端宿主（Kestrel 的 Http 端点）
  const target = env.VITE_DEV_PROXY_TARGET || 'http://localhost:9710'
  const proxy = Object.fromEntries(['/api', '/openapi.json', '/mcp', '/health'].map(path => [path, { target, changeOrigin: true }]))

  return {
    plugins: [
      tailwindcss(),
      vue(),
      Components({
        resolvers: [XiHanUiResolver()],
        // 只解析 XiHan.UI 的部件；应用自有组件一律显式 import
        dirs: [],
        dts: 'src/types/components.d.ts',
      }),
    ],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
        '~': fileURLToPath(new URL('./packages', import.meta.url)),
      },
      // 临时用 overrides 链到同级 XiHan.UI 源码时，组件库的 peer 依赖可能解析到另一份 Vue；去重以免出现两份运行时
      dedupe: ['vue', 'vue-router', 'pinia', 'vue-i18n', '@vue/runtime-core'],
    },
    server: {
      port: Number(env.VITE_PORT) || 9100,
      // 链到同级 XiHan.UI 源码时仓外路径要在允许范围内；装正式版时这一条没有副作用
      fs: { allow: ['..', '../../XiHan.UI'] },
      proxy,
    },
    build: {
      target: 'es2022',
      sourcemap: mode !== 'production',
    },
  }
})
