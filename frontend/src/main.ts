import { createPinia } from 'pinia'

import piniaPluginPersistedstate from 'pinia-plugin-persistedstate'
import { createApp } from 'vue'
import App from '~/app.vue'
import { setupI18n } from '~/locales'
import { router } from './router'
import '~/@core/design/global.css'

async function bootstrap() {
  const app = createApp(App)

  const pinia = createPinia()
  pinia.use(piniaPluginPersistedstate)
  app.use(pinia)

  setupI18n(app)

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
