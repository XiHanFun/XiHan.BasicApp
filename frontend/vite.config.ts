import { readFileSync } from 'node:fs'
import process from 'node:process'
import { fileURLToPath, URL } from 'node:url'

import vue from '@vitejs/plugin-vue'
import vueJsx from '@vitejs/plugin-vue-jsx'
import AutoImport from 'unplugin-auto-import/vite'
import { NaiveUiResolver } from 'unplugin-vue-components/resolvers'
import Components from 'unplugin-vue-components/vite'
import { defineConfig, loadEnv } from 'vite'

const pkg = JSON.parse(readFileSync(new URL('./package.json', import.meta.url), 'utf-8'))

function createManualChunks(id: string) {
  const normalizedId = id.replace(/\\/g, '/')

  if (!normalizedId.includes('/node_modules/')) {
    return undefined
  }

  if (
    normalizedId.includes('/vue/')
    || normalizedId.includes('/vue-router/')
    || normalizedId.includes('/pinia/')
    || normalizedId.includes('/@vue/')
    || normalizedId.includes('/vue-i18n/')
    || normalizedId.includes('/@intlify/')
    || normalizedId.includes('/naive-ui/')
    || normalizedId.includes('/@juggle/resize-observer/')
    || normalizedId.includes('/async-validator/')
    || normalizedId.includes('/css-render/')
    || normalizedId.includes('/@css-render/')
    || normalizedId.includes('/evtd/')
    || normalizedId.includes('/seemly/')
    || normalizedId.includes('/treemate/')
    || normalizedId.includes('/vdirs/')
    || normalizedId.includes('/vooks/')
    || normalizedId.includes('/vueuc/')
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

  if (
    normalizedId.includes('/date-fns/')
    || normalizedId.includes('/date-fns-tz/')
  ) {
    return 'vendor-date'
  }

  if (
    normalizedId.includes('/vxe-table/')
    || normalizedId.includes('/vxe-pc-ui/')
    || normalizedId.includes('/xe-utils/')
    || normalizedId.includes('/@vxe-ui/')
    || normalizedId.includes('/dom-zindex/')
    || normalizedId.includes('/sortablejs/')
  ) {
    return 'vendor-table'
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
    },
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
      warmup: {
        clientFiles: ['./src/main.ts', './src/App.vue', './packages/layouts/basic/index.vue'],
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
