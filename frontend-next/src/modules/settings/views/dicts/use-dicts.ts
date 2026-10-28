import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { dictItems as dictItemsApi, dicts as dictsApi } from '~/contracts'
import { ApiError } from '~/request'

export type DictRow = ResponseOf<'dicts.list'>['items'][number]
export type DictItemRow = ResponseOf<'dict-items.list'>['items'][number]
export type SettingStatus = DictRow['status']

export interface CreateDictForm {
  code: string
  name: string
  category: string
  description: string
  order: string
}

export interface EditDictForm {
  name: string
  category: string
  description: string
  order: string
  status: SettingStatus
}

export interface ItemForm {
  code: string
  name: string
  parentId: string
  value: string
  description: string
  metadata: string
  isDefault: boolean
  order: string
  status: SettingStatus
}

function describe(error: unknown): string {
  return error instanceof ApiError ? error.detail : String(error)
}

function toOrder(value: string): number {
  return value.trim() === '' ? 0 : Number(value)
}

function blankToNull(value: string): null | string {
  return value.trim() === '' ? null : value.trim()
}

function emptyItemForm(): ItemForm {
  return { code: '', name: '', parentId: '', value: '', description: '', metadata: '', isDefault: false, order: '', status: 'Active' }
}

/**
 * 字典页的状态与动作（两侧都有）：左边是字典列表（平台字典 + 本侧字典）与新建，右边是选中字典的编辑与它的项（深度优先、带层级）。
 * 另一侧的字典与项只读；项的上级只能选同一字典里不是自己、也不是自己下级的项。
 */
export function useDicts(isPlatformCaller: () => boolean, pageSize = 20) {
  const items = ref<DictRow[]>([])
  const total = ref(0)
  const pageIndex = ref(1)
  const keyword = ref('')
  const loading = ref(false)
  const listError = ref<string | null>(null)
  const pageCount = computed(() => Math.max(1, Math.ceil(total.value / pageSize)))

  const form = reactive<CreateDictForm>({ code: '', name: '', category: '', description: '', order: '' })
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)

  const selected = ref<DictRow | null>(null)
  const edit = reactive<EditDictForm>({ name: '', category: '', description: '', order: '', status: 'Active' })
  const editErrors = ref<Record<string, string[]>>({})
  const detailError = ref<string | null>(null)
  const notice = ref<'item_saved' | 'saved' | null>(null)
  const working = ref(false)

  const dictItems = ref<DictItemRow[]>([])
  const itemsLoading = ref(false)
  const itemForm = reactive<ItemForm>(emptyItemForm())
  const itemErrors = ref<Record<string, string[]>>({})
  const selectedItem = ref<DictItemRow | null>(null)
  const itemEdit = reactive<ItemForm>(emptyItemForm())
  const itemEditErrors = ref<Record<string, string[]>>({})

  const editable = computed(() => selected.value !== null && selected.value.isPlatform === isPlatformCaller())

  /** 选中项可选的上级：同一字典里除了它自己和它的下级 */
  const parentChoices = computed(() => {
    const current = selectedItem.value
    if (!current)
      return dictItems.value
    const excluded = new Set<string>([current.id])
    let grew = true
    while (grew) {
      grew = false
      for (const item of dictItems.value) {
        if (item.parentId && excluded.has(item.parentId) && !excluded.has(item.id)) {
          excluded.add(item.id)
          grew = true
        }
      }
    }
    return dictItems.value.filter(item => !excluded.has(item.id))
  })

  async function load(index = pageIndex.value) {
    loading.value = true
    listError.value = null
    try {
      const result = await dictsApi.list({ page: { index, size: pageSize }, filters: [], sorts: [], keyword: keyword.value.trim() || null })
      items.value = result.items
      total.value = Number(result.total)
      pageIndex.value = result.page.index ?? index
    }
    catch (error) {
      items.value = []
      total.value = 0
      listError.value = describe(error)
    }
    finally {
      loading.value = false
    }
  }

  function fieldError(field: keyof CreateDictForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  function editError(field: keyof EditDictForm): string | undefined {
    return editErrors.value[field]?.[0]
  }

  function itemError(field: keyof ItemForm): string | undefined {
    return itemErrors.value[field]?.[0]
  }

  function itemEditError(field: keyof ItemForm): string | undefined {
    return itemEditErrors.value[field]?.[0]
  }

  function capture(error: unknown, errors: { value: Record<string, string[]> }, message: { value: null | string }) {
    if (error instanceof ApiError) {
      errors.value = error.errors ?? {}
      if (!error.hasFieldErrors)
        message.value = error.detail
    }
    else {
      message.value = String(error)
    }
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const created = await dictsApi.create({ code: form.code.trim(), name: form.name.trim(), category: blankToNull(form.category), description: blankToNull(form.description), order: toOrder(form.order) })
      Object.assign(form, { code: '', name: '', category: '', description: '', order: '' })
      await load(1)
      await select(created.id)
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

  async function loadItems() {
    const dict = selected.value
    if (!dict) {
      dictItems.value = []
      return
    }
    itemsLoading.value = true
    try {
      dictItems.value = (await dictItemsApi.list({ dictId: dict.id })).items
    }
    catch (error) {
      dictItems.value = []
      detailError.value = describe(error)
    }
    finally {
      itemsLoading.value = false
    }
  }

  async function select(id: string) {
    const row = items.value.find(d => d.id === id) ?? null
    selected.value = row
    selectedItem.value = null
    detailError.value = null
    notice.value = null
    editErrors.value = {}
    itemErrors.value = {}
    Object.assign(itemForm, emptyItemForm())
    if (row) {
      edit.name = row.name
      edit.category = row.category ?? ''
      edit.description = row.description ?? ''
      edit.order = String(row.order)
      edit.status = row.status
    }
    await loadItems()
  }

  async function save(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    editErrors.value = {}
    try {
      await dictsApi.update({ id: current.id, name: edit.name.trim(), category: blankToNull(edit.category), description: blankToNull(edit.description), order: toOrder(edit.order), status: edit.status })
      await load()
      const refreshed = items.value.find(d => d.id === current.id) ?? null
      selected.value = refreshed
      notice.value = 'saved'
      return true
    }
    catch (error) {
      capture(error, editErrors, detailError)
      return false
    }
    finally {
      working.value = false
    }
  }

  /** 删除字典：内建、还有项、另一侧的由后端逐项拒绝 */
  async function remove(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    try {
      const result = await dictsApi.delete({ ids: [current.id] })
      if (result.failed.length > 0) {
        detailError.value = result.failed.map(f => f.code).join('; ')
        return false
      }
      selected.value = null
      dictItems.value = []
      await load(1)
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

  async function submitItem(): Promise<boolean> {
    const dict = selected.value
    if (!dict)
      return false
    working.value = true
    detailError.value = null
    itemErrors.value = {}
    try {
      await dictItemsApi.create({
        dictId: dict.id,
        code: itemForm.code.trim(),
        name: itemForm.name.trim(),
        parentId: itemForm.parentId || null,
        value: blankToNull(itemForm.value),
        description: blankToNull(itemForm.description),
        metadata: blankToNull(itemForm.metadata),
        isDefault: itemForm.isDefault,
        order: toOrder(itemForm.order),
      })
      Object.assign(itemForm, emptyItemForm())
      await loadItems()
      return true
    }
    catch (error) {
      capture(error, itemErrors, detailError)
      return false
    }
    finally {
      working.value = false
    }
  }

  function selectItem(id: string) {
    const row = dictItems.value.find(i => i.id === id) ?? null
    selectedItem.value = row
    itemEditErrors.value = {}
    notice.value = null
    if (row) {
      Object.assign(itemEdit, {
        code: row.code,
        name: row.name,
        parentId: row.parentId ?? '',
        value: row.value ?? '',
        description: row.description ?? '',
        metadata: row.metadata ?? '',
        isDefault: row.isDefault,
        order: String(row.order),
        status: row.status,
      })
    }
  }

  async function saveItem(): Promise<boolean> {
    const current = selectedItem.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    itemEditErrors.value = {}
    try {
      await dictItemsApi.update({
        id: current.id,
        name: itemEdit.name.trim(),
        parentId: itemEdit.parentId || null,
        value: blankToNull(itemEdit.value),
        description: blankToNull(itemEdit.description),
        metadata: blankToNull(itemEdit.metadata),
        isDefault: itemEdit.isDefault,
        order: toOrder(itemEdit.order),
        status: itemEdit.status,
      })
      await loadItems()
      selectItem(current.id)
      notice.value = 'item_saved'
      return true
    }
    catch (error) {
      capture(error, itemEditErrors, detailError)
      return false
    }
    finally {
      working.value = false
    }
  }

  /** 删除项：还有下级的由后端拒绝 */
  async function removeItem(): Promise<boolean> {
    const current = selectedItem.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    try {
      const result = await dictItemsApi.delete({ ids: [current.id] })
      if (result.failed.length > 0) {
        detailError.value = result.failed.map(f => f.code).join('; ')
        return false
      }
      selectedItem.value = null
      await loadItems()
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
    total,
    pageIndex,
    pageCount,
    pageSize,
    keyword,
    loading,
    listError,
    form,
    fieldError,
    submitError,
    submitting,
    selected,
    edit,
    editable,
    editError,
    detailError,
    notice,
    working,
    dictItems,
    itemsLoading,
    itemForm,
    itemError,
    selectedItem,
    itemEdit,
    itemEditError,
    parentChoices,
    load,
    submit,
    select,
    save,
    remove,
    submitItem,
    selectItem,
    saveItem,
    removeItem,
  }
}
