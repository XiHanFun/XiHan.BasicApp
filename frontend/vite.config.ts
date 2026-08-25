import type { ComponentResolver } from 'unplugin-vue-components'
import { existsSync, readFileSync } from 'node:fs'
import { join } from 'node:path'
import process from 'node:process'
import { fileURLToPath, URL } from 'node:url'

import tailwindcss from '@tailwindcss/vite'
import vue from '@vitejs/plugin-vue'
import vueJsx from '@vitejs/plugin-vue-jsx'
import AutoImport from 'unplugin-auto-import/vite'
import Components from 'unplugin-vue-components/vite'
import { defineConfig, loadEnv } from 'vite'

const pkg = JSON.parse(readFileSync(new URL('./package.json', import.meta.url), 'utf-8'))

const rootDir = fileURLToPath(new URL('.', import.meta.url))

/** pnpm 内部依赖协议前缀，这类声明不是版本号 */
const PNPM_PROTOCOL_RE = /^(?:catalog|workspace|link|file|npm):/

/**
 * 解析单个依赖的真实版本：读 node_modules 里已安装那一份的 version。
 * 安装态一次覆盖 catalog / workspace / link / overrides 四种协议，
 * 而 pnpm-workspace.yaml 的 catalog 段只写范围符、且可能与实装漂移。
 * 读不到时具体版本号原样保留，pnpm 协议串显示为 '-'。
 */
function resolveDependencyVersion(name: string, spec: string): string {
  const file = join(rootDir, 'node_modules', name, 'package.json')
  if (existsSync(file)) {
    try {
      const version = (JSON.parse(readFileSync(file, 'utf-8')) as { version?: string }).version
      if (typeof version === 'string' && version)
        return version
    }
    catch {
      // 装坏的包按读不到处理
    }
  }
  return PNPM_PROTOCOL_RE.test(spec) ? '-' : spec
}

/** 把一组依赖声明整体解析成 包名 → 真实版本 */
function resolveDependencyVersions(declarations: Record<string, unknown>): Record<string, string> {
  const result: Record<string, string> = {}
  for (const [name, spec] of Object.entries(declarations))
    result[name] = resolveDependencyVersion(name, typeof spec === 'string' ? spec : '')
  return result
}

const appDependencies = resolveDependencyVersions(pkg.dependencies ?? {})
const appDevDependencies = resolveDependencyVersions(pkg.devDependencies ?? {})

/**
 * XiHan.UI 是解剖式组件库：一个组件由 Root / Trigger / Content 等多个部件组成，
 * 一个页面动辄要引十几个具名导出。此解析器把模板里的 `Xh*` 标签直接映射到 `@xihan-ui/vue`，
 * 免去逐个手写 import；仍是具名导入，摇树不受影响。
 */
function XiHanUiResolver(): ComponentResolver {
  return {
    type: 'component',
    resolve(name: string) {
      if (name.startsWith('Xh'))
        return { name, from: '@xihan-ui/vue' }
      return undefined
    },
  }
}

function createManualChunks(id: string) {
  const normalizedId = id.replace(/\\/g, '/')

  // @xihan-ui/* 经 pnpm overrides 链到同级 XiHan.UI 工作区，路径不含 /node_modules/，
  // 因此这条判断必须排在下面的 node_modules 早退之前。
  if (normalizedId.includes('/XiHan.UI/ui/packages/') || normalizedId.includes('/@xihan-ui/')) {
    if (normalizedId.includes('/features/backgrounds/') || normalizedId.includes('/@xihan-ui/backgrounds/'))
      return 'vendor-backgrounds'
    return 'vendor-ui'
  }

  if (!normalizedId.includes('/node_modules/')) {
    return undefined
  }

  // hiprint 与其 PDF/Canvas/条码依赖体积较大，单独分块并由 printing 包动态加载，避免进入管理后台首屏。
  if (
    normalizedId.includes('/vue-plugin-hiprint/')
    || normalizedId.includes('/@wtto00/html2canvas/')
    || normalizedId.includes('/jspdf/')
    || normalizedId.includes('/canvg/')
    || normalizedId.includes('/jsbarcode/')
    || normalizedId.includes('/bwip-js/')
    || normalizedId.includes('/socket.io-client/')
    || normalizedId.includes('/@claviska/jquery-minicolors/')
    || normalizedId.includes('/nzh/')
  ) {
    return 'vendor-printing'
  }

  if (
    normalizedId.includes('/vue/')
    || normalizedId.includes('/vue-router/')
    || normalizedId.includes('/pinia/')
    || normalizedId.includes('/@vue/')
    || normalizedId.includes('/vue-i18n/')
    || normalizedId.includes('/@intlify/')
  ) {
    return 'vendor-ui'
  }

  if (
    normalizedId.includes('/axios/')
    || normalizedId.includes('/dayjs/')
    || normalizedId.includes('/@vueuse/')
  ) {
    return 'vendor-utils'
  }

  if (normalizedId.includes('/lodash-es/')) {
    return 'vendor-lodash'
  }

  if (normalizedId.includes('/monaco-editor/')) {
    return 'vendor-monaco'
  }

  if (
    normalizedId.includes('/date-fns/')
    || normalizedId.includes('/date-fns-tz/')
  ) {
    return 'vendor-date'
  }

  if (
    normalizedId.includes('/@codemirror/')
    || normalizedId.includes('/codemirror/')
    || normalizedId.includes('/@marijn/')
    || normalizedId.includes('/crelt/')
    || normalizedId.includes('/rope-sequence/')
    || normalizedId.includes('/style-mod/')
    || normalizedId.includes('/w3c-keyname/')
  ) {
    return 'vendor-codemirror'
  }

  if (
    normalizedId.includes('/@lezer/')
    || normalizedId.includes('/highlight.js/')
    || normalizedId.includes('/katex/')
    || normalizedId.includes('/markdown-it/')
    || normalizedId.includes('/markdown-it-')
    || normalizedId.includes('/mermaid/')
    || normalizedId.includes('/@vavt/')
    || normalizedId.includes('/entities/')
    || normalizedId.includes('/linkify-it/')
    || normalizedId.includes('/linkifyjs/')
    || normalizedId.includes('/mdurl/')
    || normalizedId.includes('/medium-zoom/')
    || normalizedId.includes('/punycode.js/')
    || normalizedId.includes('/uc.micro/')
  ) {
    return 'vendor-markdown'
  }

  if (
    normalizedId.includes('/md-editor-v3/')
    || normalizedId.includes('/@tiptap/')
    || normalizedId.includes('/prosemirror-')
    || normalizedId.includes('/orderedmap/')
  ) {
    return 'vendor-editor'
  }

  if (
    normalizedId.includes('/vanilla-jsoneditor/')
    || normalizedId.includes('/vue3-ts-jsoneditor/')
    || normalizedId.includes('/immutable-json-patch/')
    || normalizedId.includes('/ajv/')
  ) {
    return 'vendor-jsoneditor'
  }

  if (
    normalizedId.includes('/@iconify-json/carbon/')
    || normalizedId.includes('/@iconify-json/ep/')
    || normalizedId.includes('/@iconify-json/heroicons/')
    || normalizedId.includes('/@iconify-json/logos/')
    || normalizedId.includes('/@iconify-json/lucide/')
    || normalizedId.includes('/@iconify-json/mdi/')
    || normalizedId.includes('/@iconify-json/tabler/')
  ) {
    const iconSet = normalizedId.match(/\/@iconify-json\/([^/]+)\//)?.[1]
    return iconSet ? `vendor-icon-${iconSet}` : 'vendor-icons'
  }

  if (
    normalizedId.includes('/@iconify/')
    || normalizedId.includes('/lucide-vue-next/')
  ) {
    return 'vendor-icons'
  }

  if (normalizedId.includes('/@microsoft/signalr/')) {
    return 'vendor-realtime'
  }

  if (normalizedId.includes('/papaparse/')) {
    return 'vendor-csv'
  }

  return undefined
}

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd())
  const apiPrefix = env.VITE_API_PREFIX || '/api'

  return {
    define: {
      __APP_VERSION__: JSON.stringify(pkg.version),
      __APP_BUILD_TIME__: JSON.stringify(pkg.lastBuildTime),
      __APP_HOMEPAGE__: JSON.stringify(pkg.homepage),
      __APP_NAME__: JSON.stringify(pkg.name),
      __APP_AUTHOR_NAME__: JSON.stringify(pkg.author?.name ?? ''),
      __APP_AUTHOR_URL__: JSON.stringify(pkg.author?.url ?? ''),
      __APP_DEPENDENCIES__: JSON.stringify(appDependencies),
      __APP_DEV_DEPENDENCIES__: JSON.stringify(appDevDependencies),
    },
    plugins: [
      tailwindcss(),
      vue(),
      vueJsx(),
      AutoImport({
        imports: ['vue', 'vue-router', 'pinia', '@vueuse/core'],
        dts: 'src/types/auto-imports.d.ts',
      }),
      Components({
        resolvers: [XiHanUiResolver()],
        // 只解析 XiHan.UI 的部件；应用自有组件一律显式 import，避免隐式全局注册
        dirs: [],
        dts: 'src/types/components.d.ts',
      }),
    ],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
        '~': fileURLToPath(new URL('./packages', import.meta.url)),
      },
      // @xihan-ui/* 是链到同级仓的符号链接，其 peer 依赖会解析到 XiHan.UI 自己的
      // node_modules。不去重就会出现两份 Vue 运行时，provide/inject 与响应式当场断掉。
      dedupe: ['vue', 'vue-router', 'pinia', 'vue-i18n', '@vue/runtime-core'],
    },
    css: {
      preprocessorOptions: {},
    },
    server: {
      host: '0.0.0.0',
      port: Number(env.VITE_PORT) || 9000,
      warmup: {
        clientFiles: ['./src/main.ts', './src/App.vue', './packages/layouts/basic/index.vue'],
      },
      // 链到仓外的源码不在 Vite 默认允许的文件系统范围内
      fs: {
        allow: ['..', '../../XiHan.UI'],
      },
      proxy: {
        [apiPrefix]: {
          target: env.VITE_DEV_PROXY_TARGET,
          changeOrigin: true,
        },
        '/hubs': {
          target: env.VITE_DEV_PROXY_TARGET,
          changeOrigin: true,
          ws: true,
        },
        // 本地存储静态文件（头像、公开文件等）：转发到后端 UseStaticFiles 暴露的 /uploads
        '/uploads': {
          target: env.VITE_DEV_PROXY_TARGET,
          changeOrigin: true,
        },
      },
    },
    build: {
      target: 'es2022',
      chunkSizeWarningLimit: 2000,
      reportCompressedSize: false,
      rollupOptions: {
        output: {
          chunkFileNames: 'assets/js/[name]-[hash].js',
          entryFileNames: 'assets/js/[name]-[hash].js',
          assetFileNames: 'assets/[ext]/[name]-[hash].[ext]',
          manualChunks: createManualChunks,
        },
      },
    },
  }
})
