import { onScopeDispose, ref } from 'vue'
import { storage } from '~/utils'
import { useUserSettingSync } from './useUserSettingSync'

/**
 * 视图快照 —— 捕获一套可复用的列表状态。
 */
export interface ViewSnapshot {
  /** 筛选条件 */
  filters: Record<string, unknown>
  /** 排序字段 */
  sortField?: string
  /** 排序方向 */
  sortOrder?: 'asc' | 'desc'
  /** 每页数量 */
  pageSize?: number
}

/**
 * 个人视图（运行时数据，存 localStorage）
 */
export interface PersonalView {
  /** 视图码（唯一） */
  code: string
  /** 视图名 */
  name: string
  /** 快照 */
  snapshot: ViewSnapshot
  /** 是否默认 */
  isDefault?: boolean
}

const STORAGE_PREFIX = 'xh:views:'

/**
 * 视图管理状态机：个人视图的增删改查与默认设置，按 pageCode 持久化到 localStorage。
 * S3 后端 SysPageView 端点就绪后可扩展系统/租户/团队视图（scope）。
 *
 * @param pageCode 页面唯一码（存储维度）
 */
export function useViewManager(pageCode: string) {
  const storageKey = `${STORAGE_PREFIX}${pageCode}`
  const sync = useUserSettingSync(pageCode)

  const views = ref<PersonalView[]>(storage.get<PersonalView[]>(storageKey) ?? [])
  const activeCode = ref<string | undefined>(views.value.find(v => v.isDefault)?.code)

  // 后端跨端同步：远端视图就绪则覆盖本地（尽力而为，端点未就绪时静默回退 localStorage）
  void sync.hydrate<PersonalView[]>('views').then((remote) => {
    if (Array.isArray(remote) && remote.length > 0) {
      views.value = remote
      activeCode.value = remote.find(v => v.isDefault)?.code
    }
  })

  // 其它在线设备保存视图时实时应用（SignalR 推送），并落地本地保持刷新后一致
  const unsubscribeRemote = sync.subscribeRemote('views', (value) => {
    if (Array.isArray(value)) {
      const remote = value as PersonalView[]
      views.value = remote
      activeCode.value = remote.find(v => v.isDefault)?.code
      storage.set(storageKey, remote)
    }
  })
  onScopeDispose(unsubscribeRemote)

  function persist() {
    storage.set(storageKey, views.value)
    sync.save('views', views.value)
  }

  /** 生成视图码（基于已有数量，避免依赖随机数） */
  function nextCode(): string {
    let index = views.value.length + 1
    let code = `view-${index}`
    const existing = new Set(views.value.map(v => v.code))
    while (existing.has(code)) {
      index += 1
      code = `view-${index}`
    }
    return code
  }

  /** 新增视图 */
  function addView(name: string, snapshot: ViewSnapshot): PersonalView {
    const view: PersonalView = { code: nextCode(), name, snapshot }
    views.value = [...views.value, view]
    persist()
    return view
  }

  /** 更新视图快照 */
  function updateView(code: string, snapshot: ViewSnapshot) {
    const target = views.value.find(v => v.code === code)
    if (target) {
      target.snapshot = snapshot
      persist()
    }
  }

  /** 删除视图 */
  function removeView(code: string) {
    views.value = views.value.filter(v => v.code !== code)
    if (activeCode.value === code) {
      activeCode.value = undefined
    }
    persist()
  }

  /** 设为默认（唯一） */
  function setDefault(code: string) {
    for (const v of views.value) {
      v.isDefault = v.code === code
    }
    persist()
  }

  /** 应用视图，返回其快照（由调用方落地到表格状态） */
  function applyView(code: string): ViewSnapshot | undefined {
    const target = views.value.find(v => v.code === code)
    if (!target) {
      return undefined
    }
    activeCode.value = code
    return target.snapshot
  }

  return {
    views,
    activeCode,
    addView,
    updateView,
    removeView,
    setDefault,
    applyView,
  }
}
