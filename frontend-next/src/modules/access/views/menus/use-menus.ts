import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { menus as menusApi } from '~/contracts'
import { ApiError } from '~/request'

export type MenuRow = ResponseOf<'menus.list'>['items'][number]
export type MenuKind = MenuRow['kind']
/** 运营能新建的种类：页面菜单来自页面登记，不在这里新建 */
export type OperatorKind = Exclude<MenuKind, 'Page'>

export const MENU_KINDS: readonly MenuKind[] = ['Directory', 'Page', 'Link']
export const OPERATOR_KINDS: readonly OperatorKind[] = ['Directory', 'Link']

/** 运营节点的编码存为 custom.<名>（后端补前缀） */
export const CUSTOM_CODE_PREFIX = 'custom.'

function blankToNull(value: string): null | string {
  return value.trim() === '' ? null : value.trim()
}

/** 批量删除的结果里只有失败项的错误码：页面节点（ACCESS_CONFLICT_004）、还有子节点的目录（ACCESS_CONFLICT_005）由后端逐项拒绝 */
function failedCodes(result: { failed: { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(f => f.code).join('; '))
}

/** 深度优先摊平整棵树（上级菜单的候选、子页面判定都在摊平后的序列里找） */
export function flattenMenus(nodes: readonly MenuRow[], out: MenuRow[] = []): MenuRow[] {
  for (const node of nodes) {
    out.push(node)
    flattenMenus(node.children, out)
  }
  return out
}

/** 本地筛选登记的字段：menus.list 一次返回整棵树、不收筛选参数，搜索在取回的整树上做 */
const LOCAL_FILTERS = new Set(['keyword', 'kind', 'isVisible', 'isCustomized'])

function isBlank(value: unknown): boolean {
  return value === undefined || value === null || (typeof value === 'string' && value.trim() === '')
}

function matchesBoolean(value: unknown, actual: boolean): boolean {
  return String(value) === String(actual)
}

/**
 * 按搜索条件筛整棵菜单树：节点自身命中、或有命中的后代就留下（保留祖先链看得出层级），留下节点的子级只留命中的。
 * 关键字匹配显示标题、编码、路由与外链地址；种类、显示、已定制精确匹配。没登记的字段、区间 / 多选出现即报错，不静默丢条件。
 */
export function filterMenuTree(nodes: readonly MenuRow[], params: Pick<SchemaQueryParams, 'conditionFilters' | 'filters'>, titleOf: (node: MenuRow) => string): MenuRow[] {
  if (params.conditionFilters?.length)
    throw new Error('菜单树不支持区间 / 多选搜索：页面字段不要声明 searchRange / searchMultiple')
  const active = Object.entries(params.filters).filter(([, value]) => !isBlank(value))
  for (const [field] of active) {
    if (!LOCAL_FILTERS.has(field))
      throw new Error(`搜索字段 ${field} 没有登记菜单树的本地筛选`)
  }
  if (active.length === 0)
    return [...nodes]
  const filters = Object.fromEntries(active)
  const keyword = isBlank(filters.keyword) ? null : String(filters.keyword).trim().toLowerCase()
  const matches = (node: MenuRow): boolean => {
    if (keyword !== null && ![titleOf(node), node.code, node.route, node.externalUrl ?? ''].some(text => text.toLowerCase().includes(keyword)))
      return false
    if (!isBlank(filters.kind) && node.kind !== filters.kind)
      return false
    if (!isBlank(filters.isVisible) && !matchesBoolean(filters.isVisible, node.isVisible))
      return false
    return isBlank(filters.isCustomized) || matchesBoolean(filters.isCustomized, node.isCustomized)
  }
  const walk = (list: readonly MenuRow[]): MenuRow[] => list.flatMap((node) => {
    const children = walk(node.children)
    return matches(node) || children.length > 0 ? [{ ...node, children }] : []
  })
  return walk(nodes)
}

/**
 * SchemaPage 的资源（树形模式）：整棵树（含隐藏与运营节点）在本地筛选后交给表格；删除只对运营节点生效。
 * 取回的整树另交给 onLoaded（上级目录的候选与子页面判定按整树算，不受搜索影响）。
 */
export function menusResource(titleOf: (node: MenuRow) => string, onLoaded: (nodes: MenuRow[]) => void = () => {}): SchemaResource<MenuRow> {
  return {
    tree: async (params: SchemaQueryParams) => {
      const { items } = await menusApi.list({})
      onLoaded(items)
      return filterMenuTree(items, params, titleOf)
    },
    remove: async id => failedCodes(await menusApi.delete({ ids: [String(id)] })),
  }
}

/** 恢复默认：页面节点回到登记的展示并清掉定制标记 */
export function resetMenu(id: string): Promise<MenuRow> {
  return menusApi.reset({ id })
}

/** 弹窗表单：新建目录 / 外链与编辑共用一份（种类与编码只在新建时可填） */
export interface MenuForm {
  kind: MenuKind
  code: string
  title: string
  icon: string
  order: number
  /** 所属目录；null 即顶层（目录只能在顶层） */
  parentId: null | string
  externalUrl: string
  permission: string
  isVisible: boolean
  keepAlive: boolean
  isAffix: boolean
}

export type MenuEditorMode = 'create' | 'edit'

function emptyForm(): MenuForm {
  return { kind: 'Directory', code: '', title: '', icon: '', order: 0, parentId: null, externalUrl: '', permission: '', isVisible: true, keepAlive: false, isAffix: false }
}

/**
 * 菜单的编辑弹窗（平台侧）：新建目录（只在顶层）或外链（可挂目录）、编辑节点。
 * 页面节点只改展示（标题覆盖、图标、排序、所属目录、显示、缓存、固定标签），改过即定制；路由、组件与权限来自页面登记，不送。
 * 子页面钉在登记的父页面下，所属目录不能改。字段级错误挂回对应字段。
 */
export function useMenuEditor(isChildPage: (node: MenuRow) => boolean) {
  const open = ref(false)
  const mode = ref<MenuEditorMode>('create')
  const editing = ref<MenuRow | null>(null)
  const form = reactive<MenuForm>(emptyForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)

  const isEdit = computed(() => mode.value === 'edit')
  const isPage = computed(() => form.kind === 'Page')
  /** 目录只在顶层；子页面不能离开登记的父页面 */
  const parentLocked = computed(() => form.kind === 'Directory' || (editing.value !== null && isChildPage(editing.value)))

  function reset(next: Partial<MenuForm> = {}) {
    Object.assign(form, emptyForm(), next)
    fieldErrors.value = {}
    submitError.value = null
  }

  /** 新建：缺省目录；在某个目录下新建即外链挂在该目录 */
  function startCreate(kind: OperatorKind = 'Directory', parentId: null | string = null) {
    mode.value = 'create'
    editing.value = null
    reset({ kind, parentId: kind === 'Link' ? parentId : null })
    open.value = true
  }

  function startEdit(row: MenuRow) {
    mode.value = 'edit'
    editing.value = row
    reset({
      kind: row.kind,
      code: row.code,
      title: row.title ?? '',
      icon: row.icon ?? '',
      order: row.order,
      parentId: row.parentId ?? null,
      externalUrl: row.externalUrl ?? '',
      permission: row.permission ?? '',
      isVisible: row.isVisible,
      keepAlive: row.keepAlive,
      isAffix: row.isAffix,
    })
    open.value = true
  }

  /** 新建时切换种类：目录不带上级 */
  function changeKind(kind: OperatorKind) {
    form.kind = kind
    if (kind === 'Directory')
      form.parentId = null
  }

  function fieldError(field: keyof MenuForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const link = form.kind === 'Link'
      const current = editing.value
      if (mode.value === 'edit' && current) {
        await menusApi.update({
          id: current.id,
          title: blankToNull(form.title),
          icon: blankToNull(form.icon),
          order: form.order,
          parentId: current.kind === 'Directory' ? null : form.parentId,
          isVisible: form.isVisible,
          keepAlive: form.keepAlive,
          isAffix: form.isAffix,
          externalUrl: link ? blankToNull(form.externalUrl) : null,
          permission: current.kind === 'Page' ? null : blankToNull(form.permission),
        })
      }
      else {
        await menusApi.create({
          kind: form.kind,
          code: form.code.trim(),
          title: form.title.trim(),
          icon: blankToNull(form.icon),
          order: form.order,
          parentId: link ? form.parentId : null,
          externalUrl: link ? blankToNull(form.externalUrl) : null,
          permission: blankToNull(form.permission),
          isVisible: form.isVisible,
        })
      }
      open.value = false
      return true
    }
    catch (error) {
      if (error instanceof ApiError) {
        fieldErrors.value = error.errors ?? {}
        if (!error.hasFieldErrors)
          submitError.value = error.detail
      }
      else {
        submitError.value = String(error)
      }
      return false
    }
    finally {
      submitting.value = false
    }
  }

  return { open, mode, isEdit, isPage, parentLocked, editing, form, fieldError, submitError, submitting, startCreate, startEdit, changeKind, submit }
}

/**
 * 整棵菜单树（不受搜索影响）：上级目录的候选、子页面判定。
 * 页面节点的上级若也是页面，就是登记里的子页面。
 */
export function useMenuTree() {
  const nodes = ref<MenuRow[]>([])
  const flat = computed(() => flattenMenus(nodes.value))
  const directories = computed(() => flat.value.filter(node => node.kind === 'Directory'))

  function find(id: null | string | undefined): MenuRow | undefined {
    return id ? flat.value.find(node => node.id === id) : undefined
  }

  function isChildPage(node: MenuRow): boolean {
    return node.kind === 'Page' && find(node.parentId)?.kind === 'Page'
  }

  function setNodes(next: MenuRow[]) {
    nodes.value = next
  }

  return { nodes, flat, directories, find, isChildPage, setNodes }
}
