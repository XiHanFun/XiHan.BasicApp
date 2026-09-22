import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { dialog } from '~/composables'
import { useAppStore } from '~/stores'

function isLocalHost(): boolean {
  const { hostname } = window.location
  return ['localhost', '127.0.0.1', '[::1]', '0.0.0.0'].includes(hostname)
}

/**
 * 只在正式部署的站点上比对：开发服务器每次改动都会换掉首页指纹，本机跑的产物也不会被别人重新部署，
 * 两种情况下轮询只会白发请求甚至误报。挂载与偏好开关走同一把尺，免得两条路一个查一个不查。
 */
function canCheckUpdates(): boolean {
  return !import.meta.env.DEV && !isLocalHost()
}

/**
 * 首页引用的那批资源地址：打包每次都会换掉其中的哈希，是比校验头更硬的版本证据。
 * 用 DOMParser 而不是正则去捞：只是解析成文档树，不执行脚本、不加载资源，也不必跟正则的回溯较劲。
 */
function collectAssetRefs(html: string): string {
  const doc = new DOMParser().parseFromString(html, 'text/html')
  const refs = [...doc.querySelectorAll('script[src], link[href]')]
    .map(element => element.getAttribute('src') ?? element.getAttribute('href') ?? '')
    .filter(Boolean)
  return refs.join('|')
}

/** FNV-1a：把首页正文压成一个短串，省得把整页 HTML 留在内存里逐字比 */
function hashText(text: string): string {
  let hash = 0x811C9DC5
  for (let i = 0; i < text.length; i++) {
    hash ^= text.charCodeAt(i)
    hash = Math.imul(hash, 0x01000193)
  }
  return (hash >>> 0).toString(36)
}

/**
 * 定时检查前端资源是否有更新（对比首页指纹）。
 * 检测到变化时弹出通知提示用户刷新页面。
 * 页面不可见时暂停轮询，重新可见时立即检查一次再恢复定时器。
 */
export function useCheckUpdates() {
  const appStore = useAppStore()
  const { t } = useI18n()

  let timer: ReturnType<typeof setInterval> | null = null
  /** 有一次比对还在路上：上一次取指纹没回来就又轮到下一次时，两次都会拿老基线比，提示要弹两遍 */
  let checking = false
  const versionTag = ref<string | null>(null)
  const hasUpdate = ref(false)

  /**
   * 取首页指纹。校验头优先，但不能只靠它：CDN 压缩（zstd / br）后常把 etag 一并剥掉，
   * 静态站又未必给 last-modified——两头都没有时，这个功能会静悄悄地一直不报更新。
   * 所以拿不到校验头就退到正文：首页里那串带哈希的资源引用每次打包都会换。
   * 查询串是为了绕开 CDN 边缘的缓存副本，否则部署完还可能读到旧的那一份。
   */
  async function getVersionTag(): Promise<string | null> {
    try {
      const response = await fetch(`${import.meta.env.BASE_URL}?_=${Date.now()}`, { cache: 'no-store' })
      if (!response.ok) {
        return null
      }
      const validator = response.headers.get('etag') ?? response.headers.get('last-modified')
      if (validator) {
        return validator
      }
      const html = await response.text()
      // 连资源引用都捞不到（首页形态异常）就退而求其次，整页正文照样能当指纹
      return hashText(collectAssetRefs(html) || html)
    }
    catch {
      return null
    }
  }

  async function check() {
    if (checking) {
      return
    }
    checking = true
    try {
      const tag = await getVersionTag()
      if (!tag) {
        return
      }

      if (versionTag.value && tag !== versionTag.value && !hasUpdate.value) {
        hasUpdate.value = true
        showUpdateNotification()
      }

      versionTag.value = tag
    }
    finally {
      checking = false
    }
  }

  function showUpdateNotification() {
    // 命令式 toast 挂不了操作钮，改用带确认的对话框：确认即刷新，取消即本轮不再提醒
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

  /** 开始盯：先记下当前指纹当基线（没有基线就无从比对），再起定时器并接上可见性开关 */
  async function startWatching() {
    if (!canCheckUpdates()) {
      return
    }
    versionTag.value ??= await getVersionTag()
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
      check()
      startTimer()
    }
  }

  watch(
    () => appStore.enableCheckUpdates,
    (enabled) => {
      if (enabled) {
        void startWatching()
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
    void startWatching()
  })

  onBeforeUnmount(stopWatching)
}
