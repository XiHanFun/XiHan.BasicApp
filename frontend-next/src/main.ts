import { createPinia } from 'pinia'
import { createApp } from 'vue'
import { setupI18n } from '~/locales'
import { configureRequest } from '~/request'
import { router } from '~/router'
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
  configureRequest({
    baseUrl: import.meta.env.VITE_API_BASE_URL ?? '',
    getToken: () => auth.token,
    getLocale: () => appStore.locale,
    hooks: {
      // N1 的 auth.refresh 落地前没有刷新令牌：未认证即清掉令牌
      onUnauthenticated: async () => {
        auth.setToken(null)
        return null
      },
    },
  })
  await appStore.setLocale(appStore.locale)

  app.use(router)
  await router.isReady()
  app.mount('#app')
}

void bootstrap()
