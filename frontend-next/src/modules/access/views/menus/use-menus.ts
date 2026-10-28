import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { menus as menusApi } from '~/contracts'
import { ApiError } from '~/request'

export type MenuRow = ResponseOf<'menus.list'>['items'][number]
export type MenuKind = MenuRow['kind']

export interface FlatMenu {
  node: MenuRow
  depth: number
}

export interface CreateMenuForm {
  kind: 'Directory' | 'Link'
  code: string
  title: string
  icon: string
  order: string
  /** 所属目录；空串即顶层（目录只能在顶层） */
  parentId: string
  externalUrl: string
  permission: string
}

export interface EditMenuForm {
  title: string
  icon: string
  order: string
  parentId: string
  isVisible: boolean
  keepAlive: boolean
  isAffix: boolean
  externalUrl: string
  permission: string
}

function describe(error: unknown): string {
  return error instanceof ApiError ? error.detail : String(error)
}

function toOrder(value: string): number {
  return value.trim() === '' ? 0 : Number(value)
}

function blank(value: string): string | null {
  return value.trim() === '' ? null : value.trim()
}

/**
 * 菜单页的状态与动作（平台侧）：整棵树（含隐藏与运营节点）、新建目录 / 外链、选中节点的编辑（页面节点只改展示、改过即定制）、
 * 页面节点恢复默认、删除运营节点。规则以后端为准：目录只在顶层、外链与顶层页面可挂目录、子页面钉在登记的父页面下。
 */
export function useMenus() {
  const items = ref<MenuRow[]>([])
  const loading = ref(false)
  const listError = ref<string | null>(null)

  const form = reactive<CreateMenuForm>({ kind: 'Directory', code: '', title: '', icon: '', order: '', parentId: '', externalUrl: '', permission: '' })
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)

  const selected = ref<MenuRow | null>(null)
  const edit = reactive<EditMenuForm>({ title: '', icon: '', order: '', parentId: '', isVisible: true, keepAlive: false, isAffix: false, externalUrl: '', permission: '' })
  const editErrors = ref<Record<string, string[]>>({})
  const detailError = ref<string | null>(null)
  const notice = ref<'saved' | 'reset' | null>(null)
  const working = ref(false)

  /** 深度优先摊平，供树表逐行缩进 */
  const flat = computed<FlatMenu[]>(() => {
    const rows: FlatMenu[] = []
    const walk = (nodes: readonly MenuRow[], depth: number) => {
      for (const node of nodes) {
        rows.push({ node, depth })
        walk(node.children, depth + 1)
      }
    }
    walk(items.value, 0)
    return rows
  })

  const directories = computed(() => flat.value.map(row => row.node).filter(node => node.kind === 'Directory'))

  /** 子页面（上级是页面节点）不能离开登记的父页面 */
  function isChildPage(node: MenuRow | null): boolean {
    if (!node || node.kind !== 'Page' || !node.parentId)
      return false
    return flat.value.find(row => row.node.id === node.parentId)?.node.kind === 'Page'
  }

  async function load() {
    loading.value = true
    listError.value = null
    try {
      items.value = (await menusApi.list({})).items
    }
    catch (error) {
      items.value = []
      listError.value = describe(error)
    }
    finally {
      loading.value = false
    }
  }

  function fieldError(field: keyof CreateMenuForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  function editError(field: keyof EditMenuForm): string | undefined {
    return editErrors.value[field]?.[0]
  }

  function apply(node: MenuRow) {
    selected.value = node
    edit.title = node.title ?? ''
    edit.icon = node.icon ?? ''
    edit.order = String(node.order)
    edit.parentId = node.parentId ?? ''
    edit.isVisible = node.isVisible
    edit.keepAlive = node.keepAlive
    edit.isAffix = node.isAffix
    edit.externalUrl = node.externalUrl ?? ''
    edit.permission = node.permission ?? ''
  }

  function handleFormError(error: unknown, fields: typeof fieldErrors, message: typeof submitError) {
    if (error instanceof ApiError) {
      fields.value = error.errors ?? {}
      if (!error.hasFieldErrors)
        message.value = error.detail
    }
    else {
      message.value = String(error)
    }
  }

  /** 新建目录或外链：目录不带上级与地址，外链地址必填 */
  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const link = form.kind === 'Link'
      const created = await menusApi.create({
        kind: form.kind,
        code: form.code.trim(),
        title: form.title.trim(),
        icon: blank(form.icon),
        order: toOrder(form.order),
        parentId: link ? form.parentId || null : null,
        externalUrl: link ? blank(form.externalUrl) : null,
        permission: blank(form.permission),
        isVisible: true,
      })
      Object.assign(form, { code: '', title: '', icon: '', order: '', parentId: '', externalUrl: '', permission: '' })
      await load()
      apply(created)
      notice.value = null
      return true
    }
    catch (error) {
      handleFormError(error, fieldErrors, submitError)
      return false
    }
    finally {
      submitting.value = false
    }
  }

  async function select(id: string) {
    detailError.value = null
    notice.value = null
    editErrors.value = {}
    try {
      apply(await menusApi.get({ id }))
    }
    catch (error) {
      selected.value = null
      detailError.value = describe(error)
    }
  }

  /** 保存：页面节点不送外链与权限（来自页面登记），子页面的上级原样送回 */
  async function save(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    editErrors.value = {}
    try {
      const page = current.kind === 'Page'
      apply(await menusApi.update({
        id: current.id,
        title: blank(edit.title),
        icon: blank(edit.icon),
        order: toOrder(edit.order),
        parentId: current.kind === 'Directory' ? null : edit.parentId || null,
        isVisible: edit.isVisible,
        keepAlive: edit.keepAlive,
        isAffix: edit.isAffix,
        externalUrl: current.kind === 'Link' ? blank(edit.externalUrl) : null,
        permission: page ? null : blank(edit.permission),
      }))
      await load()
      notice.value = 'saved'
      return true
    }
    catch (error) {
      handleFormError(error, editErrors, detailError)
      return false
    }
    finally {
      working.value = false
    }
  }

  /** 恢复默认：页面节点回到登记的展示并清掉定制标记 */
  async function reset(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    try {
      apply(await menusApi.reset({ id: current.id }))
      await load()
      notice.value = 'reset'
      return true
    }
    catch (error) {
      detailError.value = describe(error)
      return false
    }
    finally {
      working.value = false
    }
  }

  /** 删除：页面节点与还有子节点的目录由后端逐项拒绝 */
  async function remove(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    try {
      const result = await menusApi.delete({ ids: [current.id] })
      if (result.failed.length > 0) {
        detailError.value = result.failed.map(f => f.code).join('; ')
        return false
      }
      selected.value = null
      await load()
      return true
    }
    catch (error) {
      detailError.value = describe(error)
      return false
    }
    finally {
      working.value = false
    }
  }

  return {
    items,
    flat,
    directories,
    loading,
    listError,
    form,
    fieldErrors,
    fieldError,
    submitError,
    submitting,
    selected,
    edit,
    editError,
    detailError,
    notice,
    working,
    isChildPage,
    load,
    submit,
    select,
    save,
    reset,
    remove,
  }
}
