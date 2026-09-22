import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { dialog } from '~/composables'
import { useAppStore } from '~/stores'

function isLocalHost(): boolean {
  const { hostname } = window.location
  return ['localhost', '127.0.0.1', '[::1]', '0.0.0.0'].includes(hostname)
}

/**
 * 只在正式部署的站点上比对：开发服务器每改一次就换一份产物，本机跑的产物也不会被别人重新部署，
 * 两种情况下轮询只会白发请求甚至误报。挂载与偏好开关走同一把尺，免得两条路一个查一个不查。
 */
function canCheckUpdates(): boolean {
  return !import.meta.env.DEV && !isLocalHost()
}

/**
 * 线上那一版的发布标记。构建时随产物落一份 version.json，同一份源码重复打包也会换新值。
 * 没有这份清单（还没发过新版的老产物、或别处托管）就返回 null，本轮不判，等下一轮。
 * 查询串绕开按 URL 记缓存的那类 CDN 边缘副本；把查询串排除在缓存键外的（如 Workers 静态资产）
 * 对它无感，那边靠发版本身换掉整套资产来保证新鲜。
 */
async function fetchDeployedStamp(): Promise<string | null> {
  try {
    const response = await fetch(`${import.meta.env.BASE_URL}version.json?_=${Date.now()}`, { cache: 'no-store' })
    if (!response.ok) {
      return null
    }
    const manifest = await response.json() as { buildStamp?: unknown }
    return typeof manifest.buildStamp === 'string' ? manifest.buildStamp : null
  }
  catch {
    return null
  }
}

/**
 * 定时检查前端是否发过新版：比线上 version.json 里的构建标记与当前这份产物里打进来的那个。
 * 是绝对比对而不是攒基线，所以页面在发版之后打开的也判得出来。
 * 检测到变化时弹出通知提示用户刷新页面。
 * 页面不可见时暂停轮询，重新可见时立即检查一次再恢复定时器。
 */
export function useCheckUpdates() {
  const appStore = useAppStore()
  const { t } = useI18n()

  let timer: ReturnType<typeof setInterval> | null = null
  /** 有一次比对还在路上：上一次没回来就又轮到下一次时，两次都会判出同一版，提示要弹两遍 */
  let checking = false
  const hasUpdate = ref(false)
  /** 被按掉的那一版：同一版不再每轮追着问，换了新版才重新提醒 */
  const dismissedVersion = ref<string | null>(null)

  async function check() {
    if (checking) {
      return
    }
    checking = true
    try {
      const deployedStamp = await fetchDeployedStamp()
      if (!deployedStamp || deployedStamp === __APP_BUILD_STAMP__) {
        return
      }
      if (deployedStamp !== dismissedVersion.value && !hasUpdate.value) {
        hasUpdate.value = true
        showUpdateNotification(deployedStamp)
      }
    }
    finally {
      checking = false
    }
  }

  function showUpdateNotification(version: string) {
    // 命令式 toast 挂不了操作钮，改用带确认的对话框：确认即刷新，取消即本版不再提醒
    void dialog
      .confirm({
        title: t('check_updates.title'),
        content: t('check_updates.description'),
        badge: 'info',
        okText: t('check_updates.refresh'),
        onOk: () => {
          window.location.reload()
        },
      })
      .then((confirmed) => {
        if (!confirmed) {
          hasUpdate.value = false
          dismissedVersion.value = version
        }
      })
  }

  function startTimer() {
    stopTimer()
    if (!appStore.enableCheckUpdates || !canCheckUpdates())
      return
    const seconds = Math.max(10, Math.min(300, appStore.checkUpdatesInterval))
    timer = setInterval(check, seconds * 1000)
  }

  function stopTimer() {
    if (timer) {
      clearInterval(timer)
      timer = null
    }
  }

  /** 开始盯：先比一次（页面可能就是在发版之后打开的），再起定时器并接上可见性开关 */
  function startWatching() {
    if (!canCheckUpdates()) {
      return
    }
    void check()
    startTimer()
    document.addEventListener('visibilitychange', handleVisibilityChange)
  }

  function stopWatching() {
    stopTimer()
    document.removeEventListener('visibilitychange', handleVisibilityChange)
  }

  /** 页面可见性变化：隐藏时暂停轮询，恢复时立即检查一次再重启定时器 */
  function handleVisibilityChange() {
    if (document.hidden) {
      stopTimer()
    }
    else if (appStore.enableCheckUpdates) {
      void check()
      startTimer()
    }
  }

  watch(
    () => appStore.enableCheckUpdates,
    (enabled) => {
      if (enabled) {
        startWatching()
      }
      else {
        stopWatching()
      }
    },
  )

  watch(
    () => appStore.checkUpdatesInterval,
    () => {
      if (appStore.enableCheckUpdates) {
        startTimer()
      }
    },
  )

  onMounted(() => {
    if (!appStore.enableCheckUpdates) {
      return
    }
    startWatching()
  })

  onBeforeUnmount(stopWatching)
}
