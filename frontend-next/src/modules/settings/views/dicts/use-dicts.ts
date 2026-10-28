import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import type { TreeSelectOption } from '~/types'
import { computed, reactive, ref } from 'vue'
import { dictItems as dictItemsApi, dicts as dictsApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { ApiError } from '~/request'

export type DictRow = ResponseOf<'dicts.list'>['items'][number]
export type DictItemRow = ResponseOf<'dict-items.list'>['items'][number]
export type SettingStatus = DictRow['status']

/** 字典项树节点：后端按深度优先平铺（带 parentId），树表按 parentId 挂成嵌套 */
export type DictItemNode = DictItemRow & { children?: DictItemNode[] }

/** 弹窗表单：新建与编辑字典共用一份（编码只在新建时可改） */
export interface DictForm {
  code: string
  name: string
  category: string
  description: string
  /** 数字框清空时为 null，提交按 0 */
  order: null | number
  status: SettingStatus
}

/** 弹窗表单：新建与编辑字典项共用一份（项编码只在新建时可改） */
export interface DictItemForm {
  code: string
  name: string
  parentId: null | string
  value: string
  description: string
  metadata: string
  isDefault: boolean
  /** 数字框清空时为 null，提交按 0 */
  order: null | number
  status: SettingStatus
}

export type EditorMode = 'create' | 'edit'

function blankToNull(value: string): null | string {
  return value.trim() === '' ? null : value.trim()
}

function emptyDictForm(): DictForm {
  return { code: '', name: '', category: '', description: '', order: 0, status: 'Active' }
}

function emptyItemForm(): DictItemForm {
  return { code: '', name: '', parentId: null, value: '', description: '', metadata: '', isDefault: false, order: 0, status: 'Active' }
}

/** 批量删除的结果里只有失败项的错误码：逐项拒绝的原因拼成一条报错 */
function failedCodes(result: { failed: { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(f => f.code).join('; '))
}

/** 把后端的深度优先平铺列表按 parentId 挂成树；上级不在列表里的项按顶层处理 */
export function buildDictItemTree(items: readonly DictItemRow[]): DictItemNode[] {
  const nodes = new Map<string, DictItemNode>(items.map(item => [item.id, { ...item }]))
  const roots: DictItemNode[] = []
  for (const item of items) {
    const node = nodes.get(item.id)!
    const parent = item.parentId ? nodes.get(item.parentId) : undefined
    if (parent)
      (parent.children ??= []).push(node)
    else
      roots.push(node)
  }
  return roots
}

/** 在树里按主键找节点 */
export function findDictItem(nodes: readonly DictItemNode[], id: null | string | undefined): DictItemNode | undefined {
  if (!id)
    return undefined
  for (const node of nodes) {
    if (node.id === id)
      return node
    const found = findDictItem(node.children ?? [], id)
    if (found)
      return found
  }
  return undefined
}

/**
 * 字典项可选的上级：同一字典的树，去掉 `excludeId` 这一项连同它的整棵子树（项不能挂到自己或自己的下级下面）。
 * 不选即顶层。
 */
export function dictItemParentOptions(nodes: readonly DictItemNode[], excludeId?: string): TreeSelectOption[] {
  return nodes
    .filter(node => node.id !== excludeId)
    .map((node) => {
      const children = dictItemParentOptions(node.children ?? [], excludeId)
      return { value: node.id, label: `${node.code} · ${node.name}`, ...(children.length ? { children } : {}) }
    })
}

/**
 * 左栏字典的 SchemaPage 资源：分页（读共享：平台字典 + 本侧字典）、删除、启停与导入新建。
 * 搜索字段按后端白名单下发：分类 / 所属 / 状态 / 是否内置精确匹配，关键字匹配编码、名称与分类。
 */
export function dictsResource(): SchemaResource<DictRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await dictsApi.list(toPageRequest(params, { filters: { category: 'eq', owner: 'eq', status: 'eq', isBuiltIn: 'eq' } })),
      params,
    ),
    // 内建、还有项、另一侧的字典由后端逐项拒绝
    remove: async id => failedCodes(await dictsApi.delete({ ids: [String(id)] })),
    // 更新是整体覆盖：先取当前值再只改状态
    updateStatus: async (id, enabled) => {
      const row = await dictsApi.get({ id: String(id) })
      return dictsApi.update({ id: row.id, name: row.name, category: row.category, description: row.description, order: row.order, status: enabled ? 'Active' : 'Disabled' })
    },
    // 导入：importable 字段组装的记录 → 新建请求（缺省值在此兜底）
    create: record => dictsApi.create({
      code: String(record.code ?? '').trim(),
      name: String(record.name ?? '').trim(),
      category: blankToNull(String(record.category ?? '')),
      description: blankToNull(String(record.description ?? '')),
      order: typeof record.order === 'number' ? record.order : 0,
    }),
  }
}

/**
 * 右栏字典项的 SchemaPage 资源（树形，不分页）：取当前字典的全部项挂成树。
 * `editable`（字典在调用方这一侧）时才带删除、启停与导入新建（导入的项都在顶层）；另一侧的字典只读，这些入口一概不出。
 */
export function dictItemsResource(dictId: string, editable: boolean): SchemaResource<DictItemNode> {
  const tree = async () => buildDictItemTree((await dictItemsApi.list({ dictId })).items)
  if (!editable)
    return { tree }
  return {
    tree,
    // 还有下级的项由后端拒绝
    remove: async id => failedCodes(await dictItemsApi.delete({ ids: [String(id)] })),
    // 更新是整体覆盖：先取当前值再只改状态
    updateStatus: async (id, enabled) => {
      const row = await dictItemsApi.get({ id: String(id) })
      return dictItemsApi.update({
        id: row.id,
        name: row.name,
        parentId: row.parentId,
        value: row.value,
        description: row.description,
        metadata: row.metadata,
        isDefault: row.isDefault,
        order: row.order,
        status: enabled ? 'Active' : 'Disabled',
      })
    },
    // 导入：importable 字段组装的记录 → 新建请求（导入到当前字典的顶层，缺省值在此兜底）
    create: record => dictItemsApi.create({
      dictId,
      code: String(record.code ?? '').trim(),
      name: String(record.name ?? '').trim(),
      value: blankToNull(String(record.value ?? '')),
      description: blankToNull(String(record.description ?? '')),
      isDefault: record.isDefault === true,
      order: typeof record.order === 'number' ? record.order : 0,
    }),
  }
}

/** 提交失败：字段级错误挂回对应字段，其余给整条提示 */
function capture(error: unknown, fieldErrors: { value: Record<string, string[]> }, submitError: { value: null | string }) {
  if (error instanceof ApiError) {
    fieldErrors.value = error.errors ?? {}
    if (!error.hasFieldErrors)
      submitError.value = error.detail
  }
  else {
    submitError.value = String(error)
  }
}

/**
 * 字典的编辑弹窗（两侧都有）：新建（写在调用方这一侧，编码平台与租户之间也不重）、编辑本侧字典。
 * 另一侧的字典只读；字段级错误挂回对应字段。
 */
export function useDictEditor(isPlatformCaller: () => boolean) {
  const open = ref(false)
  const mode = ref<EditorMode>('create')
  const editing = ref<DictRow | null>(null)
  const form = reactive<DictForm>(emptyDictForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)

  /** 这个字典是否属于调用方这一侧（只有本侧的字典与它的项能改、能删） */
  function isOwn(row: DictRow | null | undefined): boolean {
    return !!row && row.isPlatform === isPlatformCaller()
  }

  const isEdit = computed(() => mode.value === 'edit')

  function reset(next: Partial<DictForm> = {}) {
    Object.assign(form, emptyDictForm(), next)
    fieldErrors.value = {}
    submitError.value = null
  }

  function startCreate() {
    mode.value = 'create'
    editing.value = null
    reset()
    open.value = true
  }

  function startEdit(row: DictRow) {
    mode.value = 'edit'
    editing.value = row
    reset({ code: row.code, name: row.name, category: row.category ?? '', description: row.description ?? '', order: row.order, status: row.status })
    open.value = true
  }

  function fieldError(field: keyof DictForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const current = editing.value
      if (mode.value === 'edit' && current) {
        await dictsApi.update({ id: current.id, name: form.name.trim(), category: blankToNull(form.category), description: blankToNull(form.description), order: form.order ?? 0, status: form.status })
      }
      else {
        await dictsApi.create({ code: form.code.trim(), name: form.name.trim(), category: blankToNull(form.category), description: blankToNull(form.description), order: form.order ?? 0 })
      }
      open.value = false
      return true
    }
    catch (error) {
      capture(error, fieldErrors, submitError)
      return false
    }
    finally {
      submitting.value = false
    }
  }

  return { open, mode, isEdit, editing, form, fieldError, submitError, submitting, isOwn, startCreate, startEdit, submit }
}

/**
 * 字典项的编辑弹窗：新建（字典要在本侧且启用）、编辑本侧字典的项。
 * 上级只能选同一字典里不是自己、也不是自己下级的项；元数据是 JSON 文本，后端校验。字段级错误挂回对应字段。
 */
export function useDictItemEditor() {
  const open = ref(false)
  const mode = ref<EditorMode>('create')
  const dictId = ref<string | null>(null)
  const editing = ref<DictItemRow | null>(null)
  /** 打开弹窗时的项树：上级下拉从它派生 */
  const nodes = ref<DictItemNode[]>([])
  const form = reactive<DictItemForm>(emptyItemForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)

  const isEdit = computed(() => mode.value === 'edit')
  const parentOptions = computed(() => dictItemParentOptions(nodes.value, editing.value?.id))

  function reset(next: Partial<DictItemForm> = {}) {
    Object.assign(form, emptyItemForm(), next)
    fieldErrors.value = {}
    submitError.value = null
  }

  function startCreate(dict: DictRow, tree: DictItemNode[]) {
    mode.value = 'create'
    dictId.value = dict.id
    editing.value = null
    nodes.value = tree
    reset()
    open.value = true
  }

  function startEdit(row: DictItemRow, tree: DictItemNode[]) {
    mode.value = 'edit'
    dictId.value = row.dictId
    editing.value = row
    nodes.value = tree
    reset({
      code: row.code,
      name: row.name,
      parentId: row.parentId ?? null,
      value: row.value ?? '',
      description: row.description ?? '',
      metadata: row.metadata ?? '',
      isDefault: row.isDefault,
      order: row.order,
      status: row.status,
    })
    open.value = true
  }

  function fieldError(field: keyof DictItemForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const current = editing.value
      const common = {
        name: form.name.trim(),
        parentId: form.parentId || null,
        value: blankToNull(form.value),
        description: blankToNull(form.description),
        metadata: blankToNull(form.metadata),
        isDefault: form.isDefault,
        order: form.order ?? 0,
      }
      if (mode.value === 'edit' && current)
        await dictItemsApi.update({ id: current.id, ...common, status: form.status })
      else
        await dictItemsApi.create({ dictId: dictId.value ?? '', code: form.code.trim(), ...common })
      open.value = false
      return true
    }
    catch (error) {
      capture(error, fieldErrors, submitError)
      return false
    }
    finally {
      submitting.value = false
    }
  }

  return { open, mode, isEdit, editing, form, parentOptions, fieldError, submitError, submitting, startCreate, startEdit, submit }
}
