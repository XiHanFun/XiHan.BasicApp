import type { PluginOption } from 'vite'
import { readFileSync } from 'node:fs'
import process from 'node:process'
import { fileURLToPath, URL } from 'node:url'
import tailwindcss from '@tailwindcss/vite'
import vue from '@vitejs/plugin-vue'
import { XiHanUIResolver } from '@xihan-ui/vue/resolver'
import Components from 'unplugin-vue-components/vite'
import { defineConfig, loadEnv } from 'vite'

const { version } = JSON.parse(readFileSync(new URL('./package.json', import.meta.url), 'utf8')) as { version: string }
const buildTime = new Date().toISOString().slice(0, 16).replace('T', ' ')
/** 本次构建的标记：同一份源码重复打包也会换新值；发版检查据它判断线上那一版是不是正在跑的这一版 */
const buildStamp = new Date().toISOString()

/** 把版本与构建标记落成 version.json，随产物一起发出去 */
function releaseManifest(): PluginOption {
  return {
    name: 'xihan:release-manifest',
    apply: 'build',
    generateBundle() {
      this.emitFile({ type: 'asset', fileName: 'version.json', source: `${JSON.stringify({ version, buildTime, buildStamp }, null, 2)}
` })
    },
  }
}

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  // 开发态把 /api、/openapi.json、/mcp、/health 与签名地址 /blobs 代理到后端宿主（Kestrel 的 Http 端点）；实时 Hub /hubs 要转 WebSocket
  const target = env.VITE_DEV_PROXY_TARGET || 'http://localhost:9710'
  const proxy = {
    ...Object.fromEntries(['/api', '/openapi.json', '/mcp', '/health', '/blobs'].map(path => [path, { target, changeOrigin: true }])),
    '/hubs': { target, changeOrigin: true, ws: true },
  }

  return {
    // 版本号进壳层页脚；来源只有 package.json 一处
    define: {
      __APP_VERSION__: JSON.stringify(version),
      // 页脚与标题里的应用名（package.json 的 name 是工作区包名，不上屏）
      __APP_NAME__: JSON.stringify('XiHan.BasicApp'),
      __APP_BUILD_TIME__: JSON.stringify(buildTime),
      __APP_BUILD_STAMP__: JSON.stringify(buildStamp),
      __APP_REPOSITORY__: JSON.stringify('https://github.com/XiHanFun/XiHan.BasicApp'),
    },
    plugins: [
      releaseManifest(),
      tailwindcss(),
      vue(),
      Components({
        // XiHan.UI 是解剖式组件库：模板里的 Xh* 标签由库自带的解析器按需映射到 @xihan-ui/vue 的具名导出，摇树不受影响
        resolvers: [XiHanUIResolver()],
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
