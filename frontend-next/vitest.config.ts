import { fileURLToPath, URL } from 'node:url'
import { defineConfig, mergeConfig } from 'vitest/config'
import viteConfig from './vite.config.ts'

/**
 * 单元测试复用 vite.config.ts：别名、去重与 Xh* 解析在测试里同样生效。
 */
export default defineConfig(async (env) => {
  const base = typeof viteConfig === 'function' ? await viteConfig(env) : viteConfig
  return mergeConfig(base, {
    test: {
      environment: 'jsdom',
      globals: false,
      setupFiles: [fileURLToPath(new URL('./scripts/vitest-setup.ts', import.meta.url))],
      include: ['src/**/*.{test,spec}.ts', 'packages/**/*.{test,spec}.ts', 'scripts/**/*.{test,spec}.ts'],
      exclude: ['**/node_modules/**', '**/dist/**'],
      server: {
        deps: {
          inline: [/@xihan-ui\//],
        },
      },
      restoreMocks: true,
      unstubEnvs: true,
      unstubGlobals: true,
    },
  })
})
