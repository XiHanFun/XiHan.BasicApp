import { createPinia } from 'pinia'

import piniaPluginPersistedstate from 'pinia-plugin-persistedstate'
import { createApp } from 'vue'
import { setupIconifyOffline } from '~/iconify'
import App from '~/app.vue'
import { setupVxeTable } from '~/hooks'
import { setupI18n } from '~/locales'
import { router } from './router'
import '~/design/global.css'

// Iconify 离线图标：预加载图标集，避免运行时请求 API
setupIconifyOffline()

async function bootstrap() {
  const app = createApp(App)

  const pinia = createPinia()
  pinia.use(piniaPluginPersistedstate)
  app.use(pinia)

  setupI18n(app)
  setupVxeTable(app)

  app.use(router)

  app.mount('#app')

  // 隐藏初始加载动画
  const loading = document.getElementById('app-loading')
  if (loading) {
    loading.classList.add('hidden')
    setTimeout(() => loading.remove(), 300)
  }
}

bootstrap()
