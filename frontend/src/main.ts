import { createPinia } from 'pinia'
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate'
import { createApp } from 'vue'

import { setupVxeTable } from '~/hooks'
import { setupIconifyOffline } from '~/iconify'
import { setupI18n } from '~/locales'
import { bindRouter } from '~/request'
import { invalidateCacheIfBuildTimeChanged } from '~/stores/helpers'
import { resetSetupStorePlugin } from '~/stores/plugins'

import App from './App.vue'
import { router } from './router'

import '~/design/global.css'

// Iconify 离线图标：预加载图标集，避免运行时请求 API
setupIconifyOffline()

// 发版时自动清除旧偏好缓存
invalidateCacheIfBuildTimeChanged()

async function bootstrap() {
  const app = createApp(App)

  const pinia = createPinia()
  pinia.use(piniaPluginPersistedstate)
  pinia.use(resetSetupStorePlugin())
  app.use(pinia)

  setupI18n(app)
  setupVxeTable(app)

  app.use(router)
  bindRouter(router)

  app.mount('#app')

  // 隐藏初始加载动画
  const loading = document.getElementById('app-loading')
  if (loading) {
    loading.classList.add('hidden')
    setTimeout(() => loading.remove(), 300)
  }
}

bootstrap()
