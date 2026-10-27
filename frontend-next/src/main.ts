import { createPinia } from 'pinia'
import { createApp } from 'vue'
import { setupI18n } from '~/locales'
import { configureRequest } from '~/request'
import { LOGIN_ROUTE, router } from '~/router'
import { useAppStore, useAuthStore } from '~/stores'
import App from './App.vue'
import './styles/index.css'

async function bootstrap() {
  const app = createApp(App)
  const pinia = createPinia()
  app.use(pinia)
  setupI18n(app)

  // 能力包的启动钩子（src/modules/<包>/setup.ts 的默认导出）：登记文案、壳层扩展等；删除模块目录即随之消失
  for (const mod of Object.values(import.meta.glob<{ default?: () => void }>('/src/modules/*/setup.ts', { eager: true })))
    mod.default?.()

  const auth = useAuthStore()
  const appStore = useAppStore()
  const toLogin = () => {
    auth.clear()
    if (router.currentRoute.value.name !== LOGIN_ROUTE)
      void router.replace({ name: LOGIN_ROUTE, query: { redirect: router.currentRoute.value.fullPath } })
  }
  configureRequest({
    baseUrl: import.meta.env.VITE_API_BASE_URL ?? '',
    getToken: () => auth.accessToken,
    getLocale: () => appStore.locale,
    hooks: {
      // 访问令牌过期：用刷新令牌换一次并重放；刷新也失败即回登录页
      onUnauthenticated: async () => {
        const token = await auth.refresh()
        if (!token)
          toLogin()
        return token
      },
      // 会话被吊销（注销、改密、锁定、重置密码）：刷新令牌同样失效，直接回登录页
      onSessionInvalid: toLogin,
    },
  })
  await appStore.setLocale(appStore.locale)

  app.use(router)
  await router.isReady()
  app.mount('#app')
}

void bootstrap()
