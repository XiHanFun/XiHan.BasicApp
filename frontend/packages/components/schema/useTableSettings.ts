import type { Ref } from 'vue'
import type { ListFieldSchema } from './types'
import { computed, ref, watch } from 'vue'
import { storage } from '~/utils'
import { usePagePreferenceSync } from './usePagePreferenceSync'

/**
 * 单列设置项（显隐 / 固定 / 列宽）
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
  /** 列宽覆盖（px）；为空表示自动（沿用 schema 的 width/minWidth） */
  width?: number
}

/** 表格密度 */
export type TableDensity = 'small' | 'medium' | 'large'

/** 表格风格 */
export interface TableStyle {
  /** 斑马纹（条纹） */
  striped: boolean
  /** 边框 */
  bordered: boolean
  /** 单线（关闭时显示纵向分隔线） */
  singleLine: boolean
}

/** 表格风格默认值（与 NDataTable 默认渲染保持一致，避免视觉回归） */
const DEFAULT_STYLE: TableStyle = { striped: true, bordered: true, singleLine: true }

/**
 * 持久化结构（localStorage）
 */
interface PersistedTableSettings {
  /** 按顺序的列设置 */
  columns: Array<{ key: string, visible: boolean, fixed?: 'left' | 'right', width?: number }>
  /** 密度 */
  density: TableDensity
  /** 表格风格 */
  style?: TableStyle
  /** 是否允许多选 */
  selectable?: boolean
  /** 是否显示序号列 */
  showIndex?: boolean
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
  options?: {
    /** 多选默认值（通常取「存在批量操作」），用户未显式设置时生效 */
    defaultSelectable?: boolean
  },
) {
  const storageKey = `${STORAGE_PREFIX}${pageCode}`
  const sync = usePagePreferenceSync(pageCode)
  const defaultSelectable = options?.defaultSelectable ?? false

  /** 由 schema 字段生成的默认列设置 */
  function buildDefault(): ColumnSetting[] {
    return fields.value.map(f => ({
      key: f.key,
      title: f.title,
      visible: f.visible !== false,
      fixed: f.fixed,
      width: f.width,
    }))
  }

  const columns = ref<ColumnSetting[]>(buildDefault())
  const density = ref<TableDensity>('small')
  const style = ref<TableStyle>({ ...DEFAULT_STYLE })
  const selectable = ref<boolean>(defaultSelectable)
  const showIndex = ref<boolean>(false)

  /** 应用一份持久化设置（按 key 合并，丢弃已不存在的列、追加新列） */
  function applyPersisted(persisted: PersistedTableSettings) {
    const defaults = buildDefault()
    const defaultMap = new Map(defaults.map(c => [c.key, c]))
    const ordered: ColumnSetting[] = []
    // 先按持久化顺序恢复仍存在的列
    for (const p of persisted.columns) {
      const def = defaultMap.get(p.key)
      if (def) {
        ordered.push({ key: p.key, title: def.title, visible: p.visible, fixed: p.fixed, width: p.width ?? def.width })
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
    style.value = { ...DEFAULT_STYLE, ...persisted.style }
    selectable.value = persisted.selectable ?? defaultSelectable
    showIndex.value = persisted.showIndex ?? false
  }

  /** 从 localStorage 恢复 */
  function restore() {
    const persisted = storage.get<PersistedTableSettings>(storageKey)
    if (!persisted) {
      columns.value = buildDefault()
      return
    }
    applyPersisted(persisted)
  }

  // 后端跨端同步：远端列设置就绪则覆盖本地（尽力而为，端点未就绪时静默回退 localStorage）
  void sync.hydrate<PersistedTableSettings>('table').then((remote) => {
    if (remote && Array.isArray(remote.columns) && remote.columns.length > 0) {
      applyPersisted(remote)
    }
  })

  /** 持久化当前设置（写本地 + 后端；仅由「保存」显式触发，避免每次调整都落库） */
  function save() {
    const data: PersistedTableSettings = {
      columns: columns.value.map(c => ({ key: c.key, visible: c.visible, fixed: c.fixed, width: c.width })),
      density: density.value,
      style: { ...style.value },
      selectable: selectable.value,
      showIndex: showIndex.value,
    }
    storage.set(storageKey, data)
    sync.save('table', data)
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
  /** 列宽映射（key → 覆盖宽度，undefined 表示自动） */
  const widthMap = computed(() => {
    const map: Record<string, number | undefined> = {}
    for (const c of columns.value) {
      map[c.key] = c.width
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

  function setWidth(key: string, width?: number) {
    const target = columns.value.find(c => c.key === key)
    if (target) {
      target.width = width && width > 0 ? width : undefined
    }
  }

  function setStyle(key: keyof TableStyle, value: boolean) {
    style.value = { ...style.value, [key]: value }
  }

  function setSelectable(value: boolean) {
    selectable.value = value
  }

  function setShowIndex(value: boolean) {
    showIndex.value = value
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
    style.value = { ...DEFAULT_STYLE }
    selectable.value = defaultSelectable
    showIndex.value = false
  }

  // 字段变化（如权限变更导致列增减）时重建并尝试恢复
  watch(fields, () => restore(), { immediate: false })

  restore()

  return {
    columns,
    density,
    style,
    selectable,
    showIndex,
    visibleKeys,
    columnOrder,
    fixedMap,
    widthMap,
    toggleVisible,
    setFixed,
    setWidth,
    setStyle,
    setSelectable,
    setShowIndex,
    move,
    setDensity,
    resetDefault,
    restore,
    save,
  }
}
