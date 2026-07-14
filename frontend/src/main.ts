import { createPinia } from 'pinia'
import { createApp } from 'vue'
import { markLockedFromServer } from '~/composables'
import { setupIconifyOffline } from '~/iconify'
import { setupI18n } from '~/locales'
import { bindLockHook, bindLogoutHook, bindRouter } from '~/request'
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
  // 任何请求被服务端以 423 拒绝 → 拉起锁屏遮罩（而不是登出：用户身份仍有效）。
  // 这条钩子让"新开标签页 / 刷新页面"也会立刻进入锁屏——它们的第一个接口调用就会撞上 423。
  bindLockHook(() => {
    markLockedFromServer()
  })
  registerApplicationContext(router)
  setupRouterGuard(router)

  app.use(router)
  app.mount('#app')
})()
