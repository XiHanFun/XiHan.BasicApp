import { fileURLToPath, URL } from 'node:url'

import { defineConfig, mergeConfig } from 'vitest/config'

import viteConfig from './vite.config.ts'

/**
 * 单元测试配置。
 *
 * 复用 vite.config.ts 而不是另起一份：别名（@ / ~）、dedupe、`__APP_*__` 常量注入、
 * 以及 AutoImport / Components 两个插件都必须在测试里生效，
 * 否则依赖自动导入（ref、computed…）和 `Xh*` 组件解析的源码在测试里会直接解析失败。
 */
export default defineConfig(async (env) => {
  const base = typeof viteConfig === 'function' ? await viteConfig(env) : viteConfig

  // vite.config.ts 的 warmup 是开发服务器首屏预热用的，会在每次测试启动时强行预转换
  // main.ts / App.vue / 布局入口。测试根本不导入它们，只会刷出一屏与用例无关的解析告警。
  // 必须在这里删掉：mergeConfig 对数组是拼接语义，写成空数组覆盖不掉原有条目。
  delete base.server?.warmup

  return mergeConfig(base, {
    test: {
      // 被测代码大量依赖 window/document/localStorage/matchMedia，统一跑在 jsdom 下，
      // 纯逻辑用例在 jsdom 里同样成立，不再按目录切分环境。
      environment: 'jsdom',
      // 不开 globals：与既有 packages/printing/printing.test.ts 一致，测试 API 一律显式 import，
      // 保证测试文件本身也受 TypeScript 与 lint 约束。
      globals: false,
      setupFiles: [fileURLToPath(new URL('./scripts/vitest-setup.ts', import.meta.url))],
      include: ['src/**/*.{test,spec}.ts', 'packages/**/*.{test,spec}.ts'],
      exclude: ['**/node_modules/**', '**/dist/**', '**/.turbo/**'],
      // @xihan-ui/* 不内联的话会走 Node 原生 ESM 解析，拿不到 vite 的别名与 dedupe，
      // 出现两份 Vue 运行时。临时链到同级 XiHan.UI 源码调试时同样要靠这条。
      server: {
        deps: {
          inline: [/@xihan-ui\//],
        },
      },
      restoreMocks: true,
      unstubEnvs: true,
      unstubGlobals: true,
      coverage: {
        provider: 'v8',
        reporter: ['text-summary', 'html', 'json-summary'],
        reportsDirectory: 'coverage',
        include: ['src/**/*.{ts,vue}', 'packages/**/*.{ts,vue}'],
        exclude: [
          '**/*.d.ts',
          '**/index.ts',
          '**/*.{test,spec}.ts',
          'src/locales/**',
          'packages/locales/**',
          'src/types/**',
          'packages/types/**',
          'packages/design/**',
        ],
      },
    },
  })
})
