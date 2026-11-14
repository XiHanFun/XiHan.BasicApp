import process from 'node:process'
import { fileURLToPath, URL } from 'node:url'
import { defineConfig, devices } from '@playwright/test'
import { BACKEND_GRPC_PORT, BACKEND_URL, DATABASE, FRONTEND_PORT, FRONTEND_URL, SUPER_ADMIN } from './e2e/env.ts'

const hostDir = fileURLToPath(new URL('../backend/src/XiHan.BasicApp.Host', import.meta.url))
const ci = !!process.env.CI

/**
 * 端到端冒烟：自起后端（Development 环境、专用库、关掉图形验证码）与前端生产构建的预览，按真实用户的操作走主干流程
 */
export default defineConfig({
  testDir: './e2e',
  // 用例之间共享开通出来的租户与账号，串行跑
  fullyParallel: false,
  workers: 1,
  forbidOnly: ci,
  retries: 0,
  timeout: 60_000,
  expect: { timeout: 15_000 },
  // 失败时的轨迹与截图收进 playwright-report，CI 上作为产物上传
  reporter: [
    ['list'],
    [
      'html',
      { open: 'never' },
    ],
  ],
  use: {
    baseURL: FRONTEND_URL,
    locale: 'zh-CN',
    timezoneId: 'Asia/Shanghai',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        // 通话用例要麦克风：给假的媒体设备、授权提示自动同意，别的用例不受影响
        launchOptions: {
          args: [
            '--use-fake-ui-for-media-stream',
            '--use-fake-device-for-media-stream',
          ],
        },
      },
    },
  ],
  webServer: [
    {
      command: 'dotnet run -c Release --no-launch-profile',
      cwd: hostDir,
      url: `${BACKEND_URL}/health/live`,
      // 端点先于种子开始服务，等到种子跑完才算就绪
      wait: { stdout: /Startup seeding completed\./ },
      reuseExistingServer: !ci,
      timeout: 300_000,
      gracefulShutdown: { signal: 'SIGINT', timeout: 10_000 },
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        XiHan__Bindings__db__value: DATABASE,
        Kestrel__Endpoints__Http__Url: BACKEND_URL,
        Kestrel__Endpoints__Grpc__Url: `http://127.0.0.1:${BACKEND_GRPC_PORT}`,
        BasicApp__Auth__CaptchaEnabled: 'false',
        BasicApp__Seed__SuperAdminPassword: SUPER_ADMIN.password,
      },
    },
    {
      command: `pnpm run build && pnpm exec vite preview --port ${FRONTEND_PORT} --strictPort`,
      url: FRONTEND_URL,
      reuseExistingServer: !ci,
      timeout: 300_000,
      env: {
        VITE_DEV_PROXY_TARGET: BACKEND_URL,
      },
    },
  ],
})
