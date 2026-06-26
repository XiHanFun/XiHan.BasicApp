import type { Ref } from 'vue'
import type { ListFieldSchema } from './types'
import { computed, onScopeDispose, ref, watch } from 'vue'
import { storage } from '~/utils'
import { useUserSettingSync } from './useUserSettingSync'

/**
 * 单个搜索字段设置项
 */
export interface SearchFieldSetting {
  /** 字段键 */
  key: string
  /** 字段标题（展示用） */
  title: string
  /** 是否固定到常用搜索区（true=常驻常用区，false=收入高级条件） */
  pinned: boolean
  /** 是否在搜索面板中显示（false=完全隐藏，不出现在常用/高级区） */
  visible: boolean
}

/**
 * 持久化结构（localStorage）
 */
interface PersistedSearchSettings {
  /** 按顺序的字段设置 */
  fields: Array<{ key: string, pinned: boolean, visible?: boolean }>
}

const STORAGE_PREFIX = 'xh:search-settings:'

/**
 * 搜索字段设置状态机：固定（常用/高级）与拖拽排序，按 pageCode 持久化。
 * 字段池来自 schema 中所有 searchable 或 advancedSearch 的字段；
 * 默认：searchable=true 的字段固定到常用区，仅 advancedSearch 的收入高级区。
 *
 * @param pageCode 页面唯一码（存储维度）
 * @param fields 搜索字段池（已按权限过滤）
 */
export function useSearchSettings(
  pageCode: string,
  fields: Ref<ListFieldSchema[]>,
) {
  const storageKey = `${STORAGE_PREFIX}${pageCode}`
  const sync = useUserSettingSync(pageCode)

  /** 由 schema 字段生成的默认设置（保持 schema order） */
  function buildDefault(): SearchFieldSetting[] {
    return fields.value.map(f => ({
      key: f.key,
      title: f.title,
      // searchable 显式为 true 才默认常驻；仅 advancedSearch 的默认收入高级区
      pinned: f.searchable === true,
      // 默认全部显示
      visible: true,
    }))
  }

  const settings = ref<SearchFieldSetting[]>(buildDefault())

  /** 字段对象索引（供派生取回完整 schema） */
  const fieldMap = computed(() => {
    const map = new Map<string, ListFieldSchema>()
    for (const f of fields.value) {
      map.set(f.key, f)
    }
    return map
  })

  /** 应用一份持久化设置（按 key 合并，丢弃已不存在字段、追加新字段） */
  function applyPersisted(persisted: PersistedSearchSettings) {
    const defaults = buildDefault()
    const defaultMap = new Map(defaults.map(s => [s.key, s]))
    const ordered: SearchFieldSetting[] = []
    for (const p of persisted.fields) {
      const def = defaultMap.get(p.key)
      if (def) {
        // visible 旧数据缺省视为 true（向后兼容）
        ordered.push({ key: p.key, title: def.title, pinned: p.pinned, visible: p.visible ?? true })
        defaultMap.delete(p.key)
      }
    }
    for (const def of defaults) {
      if (defaultMap.has(def.key)) {
        ordered.push(def)
      }
    }
    settings.value = ordered
  }

  /** 从 localStorage 恢复 */
  function restore() {
    const persisted = storage.get<PersistedSearchSettings>(storageKey)
    if (!persisted) {
      settings.value = buildDefault()
      return
    }
    applyPersisted(persisted)
  }

  // 后端跨端同步：远端搜索设置就绪则覆盖本地（尽力而为，端点未就绪静默回退 localStorage）
  void sync.hydrate<PersistedSearchSettings>('search').then((remote) => {
    if (remote && Array.isArray(remote.fields) && remote.fields.length > 0) {
      applyPersisted(remote)
    }
  })

  // 其它在线设备保存搜索设置时实时应用（SignalR 推送），并落地本地保持刷新后一致
  const unsubscribeRemote = sync.subscribeRemote('search', (value) => {
    const remote = value as PersistedSearchSettings
    if (remote && Array.isArray(remote.fields) && remote.fields.length > 0) {
      applyPersisted(remote)
      storage.set(storageKey, remote)
    }
  })
  onScopeDispose(unsubscribeRemote)

  /** 持久化当前设置（写本地 + 后端；仅由「保存」显式触发，避免每次调整都落库） */
  function save() {
    const data: PersistedSearchSettings = {
      fields: settings.value.map(s => ({ key: s.key, pinned: s.pinned, visible: s.visible })),
    }
    storage.set(storageKey, data)
    sync.save('search', data)
  }

  /** 常用搜索字段（可见 + pinned，按当前顺序，取回完整 schema） */
  const commonFields = computed<ListFieldSchema[]>(() =>
    settings.value
      .filter(s => s.visible && s.pinned)
      .map(s => fieldMap.value.get(s.key))
      .filter((f): f is ListFieldSchema => !!f),
  )

  /** 高级搜索字段（可见 + 未 pinned，按当前顺序） */
  const advancedFields = computed<ListFieldSchema[]>(() =>
    settings.value
      .filter(s => s.visible && !s.pinned)
      .map(s => fieldMap.value.get(s.key))
      .filter((f): f is ListFieldSchema => !!f),
  )

  function togglePin(key: string, value: boolean) {
    const target = settings.value.find(s => s.key === key)
    if (target) {
      target.pinned = value
    }
  }

  function toggleVisible(key: string, value: boolean) {
    const target = settings.value.find(s => s.key === key)
    if (target) {
      target.visible = value
    }
  }

  function move(fromIndex: number, toIndex: number) {
    if (fromIndex < 0 || toIndex < 0 || fromIndex >= settings.value.length || toIndex >= settings.value.length) {
      return
    }
    const next = [...settings.value]
    const [moved] = next.splice(fromIndex, 1)
    if (moved) {
      next.splice(toIndex, 0, moved)
      settings.value = next
    }
  }

  function resetDefault() {
    settings.value = buildDefault()
  }

  // 字段池变化（权限变更等）时重建并尝试恢复
  watch(fields, () => restore(), { immediate: false })

  restore()

  return {
    settings,
    commonFields,
    advancedFields,
    togglePin,
    toggleVisible,
    move,
    resetDefault,
    restore,
    save,
  }
}
