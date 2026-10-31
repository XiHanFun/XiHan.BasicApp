/** 表格偏好：按页面维护数据列及序号列的独立对齐，复用既有保存和跨端同步。 */
import type { Size } from '@xihan-ui/core'
import type { Ref } from 'vue'
import type { ListFieldSchema } from './types'
import { computed, onScopeDispose, ref, watch } from 'vue'
import { storage } from '~/utils'
import { useUserSettingSync } from './useUserSettingSync'

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
  /** 该列是否可排序（来自 schema field.sortable，派生不持久化） */
  sortable?: boolean
  /** 该列的默认排序方向（参与默认多字段排序；优先级 = 列在列表中的顺序） */
  sort?: 'asc' | 'desc'
}

/** 表格密度：直接用组件库的尺寸档 */
export type TableDensity = Size

/** 表格风格 */
export interface TableStyle {
  /** 斑马纹（条纹） */
  striped: boolean
  /** 边框 */
  bordered: boolean
  /** 列之间的竖线 */
  ruled: boolean
}

/** 表格对齐：数据列都按这一档排，缺省靠起始边，与组件库一致 */
export type TableAlign = 'left' | 'center' | 'right'

/** 序号列缺省居左，与新增设置前的显示行为一致。 */
const DEFAULT_INDEX_ALIGN: TableAlign = 'left'

/** 旧配置和非法值保留原序号列的起始边对齐。 */
function readIndexAlign(value: unknown): TableAlign {
  return value === 'center' || value === 'right' ? value : DEFAULT_INDEX_ALIGN
}

/** 表格风格默认值：不打斑马纹、显示竖线 */
const DEFAULT_STYLE: TableStyle = { striped: false, bordered: true, ruled: true }

/** 旧版存的密度词（small / medium / large），读到时换成尺寸档 */
const LEGACY_DENSITY: Record<string, TableDensity> = { small: 'sm', medium: 'md', large: 'lg' }

/** 读出持久化的密度：现行尺寸档原样，旧版密度词换算 */
function readDensity(value: string | undefined): TableDensity {
  if (value === 'sm' || value === 'md' || value === 'lg')
    return value
  return (value && LEGACY_DENSITY[value]) || 'md'
}

/** 读出持久化的风格：旧版按 singleLine 存竖线（true 表示无竖线），换成 ruled */
function readStyle(value: PersistedTableSettings['style']): TableStyle {
  const { singleLine, ...rest } = value ?? {}
  return { ...DEFAULT_STYLE, ...(singleLine === undefined ? {} : { ruled: !singleLine }), ...rest }
}

/** 默认排序单条规则（列表打开时的初始排序；列头点击的临时排序会覆盖本次会话） */
export interface TableDefaultSort {
  /** 排序列字段键 */
  field: string
  /** 排序方向 */
  order: 'asc' | 'desc'
}

/**
 * 持久化结构（localStorage）
 */
interface PersistedTableSettings {
  /** 按顺序的列设置（含每列默认排序方向 sort） */
  columns: Array<{ key: string, visible: boolean, fixed?: 'left' | 'right', width?: number, sort?: 'asc' | 'desc' }>
  /** 密度；旧版存的是 small / medium / large，由 readDensity 换算 */
  density?: string
  /** 表格风格；旧版存的是 singleLine，由 readStyle 换算 */
  style?: Partial<TableStyle> & { singleLine?: boolean }
  /** 表格对齐 */
  align?: TableAlign
  /** 序号列独立对齐；缺省居左，兼容未保存该字段的旧配置。 */
  indexAlign?: TableAlign
  /** 是否允许多选 */
  selectable?: boolean
  /** 是否显示序号列 */
  showIndex?: boolean
  /** @deprecated 旧版默认多字段排序，仅用于向后兼容迁移到列内 sort */
  defaultSorts?: TableDefaultSort[]
  /** @deprecated 旧版单字段默认排序，仅用于向后兼容迁移 */
  defaultSort?: TableDefaultSort | null
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
  const sync = useUserSettingSync(pageCode)
  const defaultSelectable = options?.defaultSelectable ?? true

  /** 由 schema 字段生成的默认列设置 */
  function buildDefault(): ColumnSetting[] {
    return fields.value.map(f => ({
      key: f.key,
      title: f.title,
      visible: f.visible !== false,
      fixed: f.fixed,
      width: f.width,
      sortable: f.sortable === true,
    }))
  }

  const columns = ref<ColumnSetting[]>(buildDefault())
  /** 缺省密度：中间档 */
  const density = ref<TableDensity>('md')
  const style = ref<TableStyle>({ ...DEFAULT_STYLE })
  const align = ref<TableAlign>('left')
  const indexAlign = ref<TableAlign>(DEFAULT_INDEX_ALIGN)
  const selectable = ref<boolean>(defaultSelectable)
  const showIndex = ref<boolean>(true)

  /** 应用一份持久化设置（按 key 合并，丢弃已不存在的列、追加新列） */
  function applyPersisted(persisted: PersistedTableSettings) {
    const defaults = buildDefault()
    const defaultMap = new Map(defaults.map(c => [c.key, c]))
    // 兼容旧版：默认排序曾独立存储（defaultSorts/defaultSort），迁移为各列内置 sort（按字段名匹配）
    const legacySorts = persisted.defaultSorts ?? (persisted.defaultSort ? [persisted.defaultSort] : [])
    const legacySortMap = new Map(legacySorts.map(s => [s.field, s.order]))
    const ordered: ColumnSetting[] = []
    // 先按持久化顺序恢复仍存在的列
    for (const p of persisted.columns) {
      const def = defaultMap.get(p.key)
      if (def) {
        ordered.push({
          key: p.key,
          title: def.title,
          visible: p.visible,
          fixed: p.fixed,
          width: p.width ?? def.width,
          sortable: def.sortable,
          // 列内 sort 优先；旧版独立默认排序按字段名迁移；仅可排序列保留
          sort: def.sortable ? (p.sort ?? legacySortMap.get(p.key)) : undefined,
        })
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
    density.value = readDensity(persisted.density)
    style.value = readStyle(persisted.style)
    align.value = persisted.align ?? 'left'
    indexAlign.value = readIndexAlign(persisted.indexAlign)
    selectable.value = persisted.selectable ?? defaultSelectable
    showIndex.value = persisted.showIndex ?? true
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

  /** 持久化当前设置（写本地 + 后端；仅由「保存」显式触发，避免每次调整都落库） */
  function save() {
    const data: PersistedTableSettings = {
      columns: columns.value.map(c => ({ key: c.key, visible: c.visible, fixed: c.fixed, width: c.width, sort: c.sort })),
      density: density.value,
      style: { ...style.value },
      align: align.value,
      indexAlign: indexAlign.value,
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
  /** 默认多字段排序：由各列内置 sort 派生，优先级 = 列在列表中的顺序（拖拽列即可调优先级） */
  const defaultSorts = computed<TableDefaultSort[]>(() =>
    columns.value.filter(c => c.sort).map(c => ({ field: c.key, order: c.sort! })))

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

  function setAlign(value: TableAlign) {
    align.value = value
  }

  /**
   * 只修改本页草稿，持久化和并发同步继续由显式保存及现有同步服务负责。
   * @param value 序号列的对齐方向。
   * @returns {void} 无返回值；不改变数据列、勾选列或序号显示开关。
   */
  function setIndexAlign(value: TableAlign): void {
    indexAlign.value = readIndexAlign(value)
  }

  function setSelectable(value: boolean) {
    selectable.value = value
  }

  function setShowIndex(value: boolean) {
    showIndex.value = value
  }

  /** 循环切换某列的默认排序方向：无 → 升 → 降 → 无（仅可排序列生效；优先级由列顺序决定） */
  function cycleSort(key: string) {
    const target = columns.value.find(c => c.key === key)
    if (!target?.sortable) {
      return
    }
    target.sort = target.sort === undefined ? 'asc' : target.sort === 'asc' ? 'desc' : undefined
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
    // buildDefault 不含 sort，列重建后 defaultSorts 计算属性自然清空
    columns.value = buildDefault()
    density.value = 'md'
    style.value = { ...DEFAULT_STYLE }
    align.value = 'left'
    indexAlign.value = DEFAULT_INDEX_ALIGN
    selectable.value = defaultSelectable
    showIndex.value = true
  }

  // 字段变化（如权限变更导致列增减）时重建并尝试恢复
  watch(fields, () => restore(), { immediate: false })

  restore()

  // 跨端同步：开着同步以后端那份为准（等它回来再渲染），其它设备推来时实时应用，二者都写回本地
  const remote = sync.bindSection<PersistedTableSettings>('table', {
    accept: (value): value is PersistedTableSettings => Array.isArray((value as PersistedTableSettings | undefined)?.columns),
    apply: applyPersisted,
    writeLocal: value => storage.set(storageKey, value),
  })
  onScopeDispose(remote.dispose)

  return {
    /** 首屏能否渲染：开着同步时等后端那份回来 */
    ready: remote.ready,
    columns,
    density,
    style,
    align,
    indexAlign,
    selectable,
    showIndex,
    defaultSorts,
    visibleKeys,
    columnOrder,
    fixedMap,
    widthMap,
    toggleVisible,
    setFixed,
    setWidth,
    setStyle,
    setAlign,
    setIndexAlign,
    setSelectable,
    setShowIndex,
    cycleSort,
    move,
    setDensity,
    resetDefault,
    restore,
    save,
  }
}
