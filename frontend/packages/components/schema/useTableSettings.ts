import type { Ref } from 'vue'
import type { ListFieldSchema } from './types'
import { ref, watch } from 'vue'
import { computed } from 'vue'
import { storage } from '~/utils'

/**
 * 单列设置项（显隐 / 固定）
 */
export interface ColumnSetting {
  /** 列字段键 */
  key: string
  /** 列标题（展示用） */
  title: string
  /** 是否可见 */
  visible: boolean
  /** 固定方向 */
  fixed?: 'left' | 'right'
}

/** 表格密度 */
export type TableDensity = 'small' | 'medium' | 'large'

/**
 * 持久化结构（localStorage）
 */
interface PersistedTableSettings {
  /** 按顺序的列设置 */
  columns: Array<{ key: string, visible: boolean, fixed?: 'left' | 'right' }>
  /** 密度 */
  density: TableDensity
}

const STORAGE_PREFIX = 'xh:table-settings:'

/**
 * 表格列设置状态机：显隐 / 排序 / 固定 / 密度，按 pageCode 持久化到 localStorage。
 * 作为用户偏好的子集；后端偏好端点就绪后（S3）可替换存储后端。
 *
 * @param pageCode 页面唯一码（存储维度）
 * @param fields 列字段（schema 的可见字段，提供默认顺序与标题）
 */
export function useTableSettings(
  pageCode: string,
  fields: Ref<ListFieldSchema[]>,
) {
  const storageKey = `${STORAGE_PREFIX}${pageCode}`

  /** 由 schema 字段生成的默认列设置 */
  function buildDefault(): ColumnSetting[] {
    return fields.value.map(f => ({
      key: f.key,
      title: f.title,
      visible: f.visible !== false,
      fixed: f.fixed,
    }))
  }

  const columns = ref<ColumnSetting[]>(buildDefault())
  const density = ref<TableDensity>('small')

  /** 从 localStorage 恢复（按 key 合并，丢弃已不存在的列、追加新列） */
  function restore() {
    const persisted = storage.get<PersistedTableSettings>(storageKey)
    if (!persisted) {
      columns.value = buildDefault()
      return
    }
    const defaults = buildDefault()
    const defaultMap = new Map(defaults.map(c => [c.key, c]))
    const ordered: ColumnSetting[] = []
    // 先按持久化顺序恢复仍存在的列
    for (const p of persisted.columns) {
      const def = defaultMap.get(p.key)
      if (def) {
        ordered.push({ key: p.key, title: def.title, visible: p.visible, fixed: p.fixed })
        defaultMap.delete(p.key)
      }
    }
    // 追加新增字段（持久化里没有的）
    for (const def of defaults) {
      if (defaultMap.has(def.key)) {
        ordered.push(def)
      }
    }
    columns.value = ordered
    density.value = persisted.density ?? 'small'
  }

  /** 持久化当前设置 */
  function persist() {
    const data: PersistedTableSettings = {
      columns: columns.value.map(c => ({ key: c.key, visible: c.visible, fixed: c.fixed })),
      density: density.value,
    }
    storage.set(storageKey, data)
  }

  /** 可见列键（按当前顺序） */
  const visibleKeys = computed(() => columns.value.filter(c => c.visible).map(c => c.key))
  /** 列顺序（全部，含隐藏） */
  const columnOrder = computed(() => columns.value.map(c => c.key))
  /** 固定映射 */
  const fixedMap = computed(() => {
    const map: Record<string, 'left' | 'right' | undefined> = {}
    for (const c of columns.value) {
      map[c.key] = c.fixed
    }
    return map
  })

  function toggleVisible(key: string, value: boolean) {
    const target = columns.value.find(c => c.key === key)
    if (target) {
      target.visible = value
    }
  }

  function setFixed(key: string, fixed?: 'left' | 'right') {
    const target = columns.value.find(c => c.key === key)
    if (target) {
      target.fixed = fixed
    }
  }

  function move(fromIndex: number, toIndex: number) {
    if (fromIndex < 0 || toIndex < 0 || fromIndex >= columns.value.length || toIndex >= columns.value.length) {
      return
    }
    const next = [...columns.value]
    const [moved] = next.splice(fromIndex, 1)
    if (moved) {
      next.splice(toIndex, 0, moved)
      columns.value = next
    }
  }

  function setDensity(value: TableDensity) {
    density.value = value
  }

  function resetDefault() {
    columns.value = buildDefault()
    density.value = 'small'
  }

  // 字段变化（如权限变更导致列增减）时重建并尝试恢复
  watch(fields, () => restore(), { immediate: false })

  // 设置变化即持久化
  watch([columns, density], () => persist(), { deep: true })

  restore()

  return {
    columns,
    density,
    visibleKeys,
    columnOrder,
    fixedMap,
    toggleVisible,
    setFixed,
    move,
    setDensity,
    resetDefault,
    restore,
  }
}
