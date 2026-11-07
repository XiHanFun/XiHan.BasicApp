import type { PluginOption } from 'vite'
import { existsSync, readFileSync } from 'node:fs'
import { join } from 'node:path'
import process from 'node:process'
import { fileURLToPath, URL } from 'node:url'
import tailwindcss from '@tailwindcss/vite'
import vue from '@vitejs/plugin-vue'
import { XiHanUIResolver } from '@xihan-ui/vue/resolver'
import Components from 'unplugin-vue-components/vite'
import { defineConfig, loadEnv } from 'vite'

const { version, dependencies, devDependencies } = JSON.parse(readFileSync(new URL('./package.json', import.meta.url), 'utf8')) as {
  version: string
  dependencies?: Record<string, unknown>
  devDependencies?: Record<string, unknown>
}
const rootDir = fileURLToPath(new URL('.', import.meta.url))

/** pnpm 内部依赖协议前缀，这类声明不是版本号 */
const PNPM_PROTOCOL_RE = /^(?:catalog|workspace|link|file|npm):/

/**
 * 解析单个依赖的真实版本：读 node_modules 里已安装那一份的 version
 * 安装态一次覆盖 catalog / workspace / link / overrides 四种协议，
 * 而 pnpm-workspace.yaml 的 catalog 段只写范围符、且可能与实装漂移
 * 读不到时具体版本号原样保留，pnpm 协议串显示为 -
 */
function resolveDependencyVersion(name: string, spec: string): string {
  const file = join(rootDir, 'node_modules', name, 'package.json')
  if (existsSync(file)) {
    try {
      const installed = (JSON.parse(readFileSync(file, 'utf8')) as { version?: string }).version
      if (typeof installed === 'string' && installed)
        return installed
    }
    catch {
      // 装坏的包按读不到处理
    }
  }
  return PNPM_PROTOCOL_RE.test(spec)
    ? '-'
    : spec
}

/** 把一组依赖声明整体解析成 包名 → 真实版本 */
function resolveDependencyVersions(declarations: Record<string, unknown>): Record<string, string> {
  const result: Record<string, string> = {}
  for (const [name, spec] of Object.entries(declarations)) {
    result[name] = resolveDependencyVersion(name, typeof spec === 'string'
      ? spec
      : '')
  }
  return result
}

const appDependencies = resolveDependencyVersions(dependencies ?? {})
const appDevDependencies = resolveDependencyVersions(devDependencies ?? {})
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
    ...Object.fromEntries([
      '/api',
      '/openapi.json',
      '/mcp',
      '/health',
      '/blobs',
    ].map(path => [
      path,
      { target, changeOrigin: true },
    ])),
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
      // 关于页列出的前端依赖
      __APP_DEPENDENCIES__: JSON.stringify(appDependencies),
      __APP_DEV_DEPENDENCIES__: JSON.stringify(appDevDependencies),
    },
    plugins: [
      releaseManifest(),
      tailwindcss(),
      // 设计器是 Web Components（<xh-print-designer> 等），模板里的 xh- 标签交给浏览器，不当组件解析；
      // 应用里的组件库组件一律写 PascalCase 的 Xh*，不受影响
      vue({
        template: {
          compilerOptions: {
            isCustomElement: tag => tag.startsWith('xh-'),
          },
        },
      }),
      Components({
        // XiHan.UI 是解剖式组件库：模板里的 Xh* 标签由库自带的解析器按需映射到 @xihan-ui/vue 的具名导出，摇树不受影响
        resolvers: [
          XiHanUIResolver(),
        ],
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
      dedupe: [
        'vue',
        'vue-router',
        'pinia',
        'vue-i18n',
        '@vue/runtime-core',
      ],
    },
    optimizeDeps: {
      // 打印设计器与应用共用 @xihan-ui/web-components 的全局配置（语言、滚动源）：预构建会把 web-components 打进 @xihan-print 自己的块，
      // 链到同级 XiHan.UI 源码联调时应用写的是另一份，设计器只看得见英文缺省；不预构建即按源码引用同一份（生产构建按真实路径去重，不受影响）
      exclude: [
        '@xihan-flow/core',
        '@xihan-flow/designer',
        '@xihan-flow/runtime',
        '@xihan-print/core',
        '@xihan-print/designer',
        '@xihan-print/runtime',
      ],
    },
    server: {
      port: Number(env.VITE_PORT) || 9100,
      // 链到同级 XiHan.UI 源码时仓外路径要在允许范围内；装正式版时这一条没有副作用
      fs: {
        allow: ['..', '../../XiHan.UI'],
      },
      proxy,
    },
    build: {
      target: 'es2022',
      sourcemap: mode !== 'production',
    },
  }
})
