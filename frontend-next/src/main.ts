import { createPinia } from 'pinia'
import { createApp } from 'vue'
import { markLockedFromServer, toast } from '~/composables'
import { LOGIN_PATH } from '~/constants'
import { setupIconifyOffline } from '~/iconify'
import { $t, DEFAULT_LOCALE, isLocale, setLocale, setupI18n } from '~/locales'
import { configureRequest } from '~/request'
import { setupRouterGuard } from '~/router/guard'
import { useAppStore, useAuthStore } from '~/stores'
import { resetSetupStorePlugin } from '~/stores/plugins'
import App from './App.vue'
import { registerApplicationContext } from './app/context'
import { router } from './router'
import './styles/index.css'

/** 维护提示的最短间隔：一批请求同时撞上维护只提示一次 */
const MAINTENANCE_NOTICE_INTERVAL_MS = 10_000

async function bootstrap() {
  // 图标集离线预载：Icon 组件对挂载后才到的图标集不重渲，先于挂载装好
  await setupIconifyOffline()
  const app = createApp(App)
  const pinia = createPinia()
  pinia.use(resetSetupStorePlugin())
  app.use(pinia)
  setupI18n(app)

  // 能力包的启动钩子（src/modules/<包>/setup.ts 的默认导出）：登记文案、壳层扩展等；删除模块目录即随之消失
  for (const mod of Object.values(import.meta.glob<{ default?: () => void }>('/src/modules/*/setup.ts', { eager: true })))
    mod.default?.()

  const auth = useAuthStore()
  const appStore = useAppStore()
  let lastMaintenanceNotice = 0
  const toLogin = () => {
    auth.clear()
    if (router.currentRoute.value.path !== LOGIN_PATH)
      void router.replace({ path: LOGIN_PATH, query: { redirect: router.currentRoute.value.fullPath } })
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
      // 会话被锁屏（IDENTITY_DENIED_002，带锁定原因）：拉回锁定态，锁屏遮罩接管
      onLocked: error => markLockedFromServer(typeof error.details?.reason === 'string' ? error.details.reason : null),
      // 别的节点在升级共享库或内核还没就绪：提示稍后重试（后端这条 503 不经管道、没有本地化正文，文案由前端出）
      onMaintenance: () => {
        const now = Date.now()
        if (now - lastMaintenanceNotice < MAINTENANCE_NOTICE_INTERVAL_MS)
          return
        lastMaintenanceNotice = now
        toast.warning($t('error.maintenance'))
      },
    },
  })
  // 首屏文案先装好再挂载，否则会先渲一帧裸 key；之后切语言由偏好 store 的 locale 监听驱动
  await setLocale(isLocale(appStore.locale) ? appStore.locale : DEFAULT_LOCALE)

  // 壳层要的接口实现、路由实例与应用路由（packages 不直接依赖 src）
  registerApplicationContext(router)
  setupRouterGuard(router)
  app.use(router)
  await router.isReady()
  app.mount('#app')
}

void bootstrap()
