import type { AppUserInboxDisplayItem } from '~/types'
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useAppContext } from '~/stores'
import { NotificationPriority, NotificationType } from '~/types/enums'

/**
 * 顶部横幅公告数据与行为（获取 / 过滤 / 关闭记忆 / 轮播）
 *
 * - 数据源：后端 banner 端点（服务端已按 IsBanner、有效期、角色/部门定向过滤）
 * - 关闭记忆：localStorage 按公告 id 记录，永久不再展示；后台重发即新 id 自然重现（30 天自动清理旧记录）
 * - 展示上限：按严重度+优先级排序取前 3 条，轮播 5s 自动切换，悬停暂停
 */
const DISMISSED_KEY = 'xh:banner:dismissed'
const DISMISSED_TTL_MS = 30 * 24 * 3600_000
const MAX_BANNERS = 3
const ROTATE_INTERVAL_MS = 5000

/** 横幅视觉类型（按通知类型/优先级映射，见 resolveBannerTone） */
export type BannerTone = 'info' | 'success' | 'warning' | 'error' | 'primary'

/** 通知 → 横幅视觉类型：优先级压过类型（Urgent→error、High→warning） */
export function resolveBannerTone(item: AppUserInboxDisplayItem): BannerTone {
  if (item.priority === NotificationPriority.Urgent || item.notificationType === NotificationType.Emergency) {
    return 'error'
  }
  if (item.priority === NotificationPriority.High || item.notificationType === NotificationType.Security) {
    return 'warning'
  }
  if (item.notificationType === NotificationType.Business) {
    return 'success'
  }
  if (item.notificationType === NotificationType.Todo) {
    return 'primary'
  }
  return 'info'
}

/** 严重度排序权重（高在前） */
function severityRank(item: AppUserInboxDisplayItem): number {
  const tone = resolveBannerTone(item)
  return { error: 4, warning: 3, primary: 2, success: 1, info: 0 }[tone]
}

function readDismissed(): Record<string, number> {
  try {
    const parsed = JSON.parse(localStorage.getItem(DISMISSED_KEY) ?? '{}') as Record<string, number>
    // 清理过期记录，避免无限增长
    const now = Date.now()
    const fresh = Object.fromEntries(Object.entries(parsed).filter(([, at]) => now - at < DISMISSED_TTL_MS))
    if (Object.keys(fresh).length !== Object.keys(parsed).length) {
      localStorage.setItem(DISMISSED_KEY, JSON.stringify(fresh))
    }
    return fresh
  }
  catch {
    return {}
  }
}

export function useBannerNotices() {
  const appContext = useAppContext()

  const all = ref<AppUserInboxDisplayItem[]>([])
  const dismissed = ref<Record<string, number>>(readDismissed())
  const activeIndex = ref(0)
  const paused = ref(false)

  /** 过滤已关闭 → 按严重度排序 → 截取上限 */
  const banners = computed(() =>
    all.value
      .filter(item => !dismissed.value[String(item.basicId)])
      .sort((a, b) => severityRank(b) - severityRank(a))
      .slice(0, MAX_BANNERS),
  )

  const active = computed<AppUserInboxDisplayItem | null>(
    () => banners.value[Math.min(activeIndex.value, banners.value.length - 1)] ?? null,
  )

  function next() {
    if (banners.value.length > 1) {
      activeIndex.value = (activeIndex.value + 1) % banners.value.length
    }
  }
  function prev() {
    if (banners.value.length > 1) {
      activeIndex.value = (activeIndex.value - 1 + banners.value.length) % banners.value.length
    }
  }

  function dismiss(item: AppUserInboxDisplayItem) {
    dismissed.value = { ...dismissed.value, [String(item.basicId)]: Date.now() }
    try {
      localStorage.setItem(DISMISSED_KEY, JSON.stringify(dismissed.value))
    }
    catch {
      // 本地存储异常（隐私模式等）：本次会话内仍生效
    }
    activeIndex.value = 0
  }

  let timer: ReturnType<typeof setInterval> | null = null

  onMounted(async () => {
    try {
      all.value = await appContext.apis.userInboxApi.banner()
    }
    catch {
      // 横幅拉取失败静默：不渲染即可
    }
    timer = setInterval(() => {
      if (!paused.value) {
        next()
      }
    }, ROTATE_INTERVAL_MS)
  })
  onUnmounted(() => {
    if (timer) {
      clearInterval(timer)
    }
  })

  return { banners, active, activeIndex, paused, next, prev, dismiss }
}
