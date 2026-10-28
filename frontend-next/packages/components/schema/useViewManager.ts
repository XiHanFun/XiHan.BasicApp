import type { SchemaSortRule } from './types'
import { onScopeDispose, ref } from 'vue'
import { storage } from '~/utils'
import { useUserSettingSync } from './useUserSettingSync'

/**
 * 视图快照 —— 捕获一套可复用的列表状态。
 */
export interface ViewSnapshot {
  /** 筛选条件 */
  filters: Record<string, unknown>
  /** 多字段排序（数组顺序即优先级） */
  sorts?: SchemaSortRule[]
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

  // 跨端同步：开着同步以后端那份为准（等它回来再渲染），其它设备推来时实时应用，二者都写回本地。
  // 空数组是用户把视图删光了，同样采用
  const remote = sync.bindSection<PersonalView[]>('views', {
    accept: (value): value is PersonalView[] => Array.isArray(value),
    apply: (list) => {
      views.value = list
      activeCode.value = list.find(v => v.isDefault)?.code
    },
    writeLocal: list => storage.set(storageKey, list),
  })
  onScopeDispose(remote.dispose)

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
    /** 首屏能否渲染：开着同步时等后端那份回来 */
    ready: remote.ready,
    views,
    activeCode,
    addView,
    updateView,
    removeView,
    setDefault,
    applyView,
  }
}
