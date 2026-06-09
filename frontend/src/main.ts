import { createPinia } from 'pinia'
import { createApp } from 'vue'
import { setupIconifyOffline } from '~/iconify'
import { setupI18n } from '~/locales'
import { bindLogoutHook, bindRouter } from '~/request'
import { setupRouterGuard } from '~/router/guard'
import { invalidateCacheIfBuildTimeChanged, useAccessStore, useUserStore } from '~/stores'
import { resetSetupStorePlugin } from '~/stores/plugins'
import App from './App.vue'
import { registerApplicationContext } from './app/context'
import { setupGlobalErrorHandler } from './app/error-handler'
import { router } from './router'
import './styles/index.css'

(async () => {
  await setupIconifyOffline()
  invalidateCacheIfBuildTimeChanged()

  const app = createApp(App)
  setupGlobalErrorHandler(app)
  const pinia = createPinia()
  pinia.use(resetSetupStorePlugin())

  app.use(pinia)
  setupI18n(app)

  bindRouter(router)
  bindLogoutHook(() => {
    useAccessStore().$reset()
    useUserStore().$reset()
  })
  registerApplicationContext(router)
  setupRouterGuard(router)

  app.use(router)
  app.mount('#app')
})()
