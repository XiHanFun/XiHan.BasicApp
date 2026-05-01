import { createPinia } from 'pinia'
import { createApp } from 'vue'
import { setupVxeTable } from '~/hooks'
import { setupIconifyOffline } from '~/iconify'
import { setupI18n } from '~/locales'
import { bindRouter } from '~/request'
import { setupRouterGuard } from '~/router/guard'
import { invalidateCacheIfBuildTimeChanged } from '~/stores'
import { resetSetupStorePlugin } from '~/stores/plugins'
import App from './App.vue'
import { registerApplicationContext } from './app/context'
import { router } from './router'
import './styles/index.css'

setupIconifyOffline()
invalidateCacheIfBuildTimeChanged()

const app = createApp(App)
const pinia = createPinia()
pinia.use(resetSetupStorePlugin())

app.use(pinia)
setupI18n(app)
setupVxeTable(app)

bindRouter(router)
registerApplicationContext(router)
setupRouterGuard(router)

app.use(router)
app.mount('#app')
