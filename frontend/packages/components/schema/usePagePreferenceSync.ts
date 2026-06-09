import { islandStart } from '~/composables/useDynamicIsland'
import { useAppContext } from '~/stores'

/** 分区 → 中文名（用于灵动岛同步提示） */
const SECTION_NAMES: Record<string, string> = {
  table: '表格设置',
  search: '搜索设置',
  views: '视图',
}

function sectionName(section: string): string {
  return SECTION_NAMES[section] ?? '页面设置'
}

/**
 * 页面偏好后端同步（跨端）。
 *
 * 后端按 (用户 × pageCode) 存一条 JSON 载荷，前端在其中按「分区」(section) 读写：
 * - 列设置 → section `table`
 * - 个人视图 → section `views`
 *
 * 设计为「尽力而为」：localStorage 仍是同步事实源；后端加载成功则覆盖本地，
 * 保存失败（如端点未就绪/离线）静默忽略，不阻塞交互。
 */
interface PagePreferenceApiShape {
  get: (pageCode: string) => Promise<{ pageCode: string, payload?: null | string }>
  save: (input: { pageCode: string, payload?: null | string }) => Promise<{ pageCode: string, payload?: null | string }>
}

let sharedApi: PagePreferenceApiShape | null = null

/** pageCode → 已合并的载荷对象（内存缓存，分区读写共享同一条后端记录） */
const cache = new Map<string, Record<string, unknown>>()
const loading = new Map<string, Promise<Record<string, unknown>>>()
const saveTimers = new Map<string, ReturnType<typeof setTimeout>>()

function resolveApi(): PagePreferenceApiShape {
  sharedApi ??= useAppContext().apis.pagePreferenceApi as PagePreferenceApiShape
  return sharedApi
}

async function loadPayload(pageCode: string): Promise<Record<string, unknown>> {
  let pending = loading.get(pageCode)
  if (!pending) {
    pending = resolveApi()
      .get(pageCode)
      .then((dto) => {
        let parsed: Record<string, unknown> = {}
        if (dto?.payload) {
          try {
            parsed = JSON.parse(dto.payload) as Record<string, unknown>
          }
          catch {
            parsed = {}
          }
        }
        cache.set(pageCode, parsed)
        return parsed
      })
      .catch(() => {
        cache.set(pageCode, {})
        return {}
      })
    loading.set(pageCode, pending)
  }
  return pending
}

function saveSection(pageCode: string, section: string, value: unknown): void {
  const payload = cache.get(pageCode) ?? {}
  payload[section] = value
  cache.set(pageCode, payload)

  const existing = saveTimers.get(pageCode)
  if (existing) {
    clearTimeout(existing)
  }
  saveTimers.set(
    pageCode,
    setTimeout(() => {
      const name = sectionName(section)
      const task = islandStart(`page:${pageCode}`, `正在同步${name}…`)
      void resolveApi()
        .save({ pageCode, payload: JSON.stringify(payload) })
        .then(() => task.success(`${name}已同步`))
        .catch(() => task.error(`${name}同步失败`))
    }, 600),
  )
}

export interface PagePreferenceSync {
  /** 读取指定分区的远端值（首次触发后端加载，后续走缓存） */
  hydrate: <T>(section: string) => Promise<T | undefined>
  /** 写入指定分区（合并入整条载荷，防抖落库；失败静默） */
  save: (section: string, value: unknown) => void
}

export function usePagePreferenceSync(pageCode: string): PagePreferenceSync {
  // 在 setup 阶段提前捕获 api 引用，避免防抖回调里脱离 pinia 上下文
  resolveApi()
  return {
    hydrate: async <T>(section: string): Promise<T | undefined> => {
      const payload = await loadPayload(pageCode)
      return payload[section] as T | undefined
    },
    save: (section: string, value: unknown) => saveSection(pageCode, section, value),
  }
}
