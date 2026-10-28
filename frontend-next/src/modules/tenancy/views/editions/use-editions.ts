import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { editions as editionsApi, permissions as permissionsApi } from '~/contracts'
import { ApiError } from '~/request'

export type EditionRow = ResponseOf<'editions.list'>['items'][number]
export type EditionDetail = ResponseOf<'editions.get'>
export type CatalogItem = ResponseOf<'permissions.list'>['items'][number]

export interface CreateEditionForm {
  code: string
  name: string
  description: string
  order: string
  isDefault: boolean
}

export interface EditEditionForm {
  name: string
  description: string
  order: string
  isDefault: boolean
}

function describe(error: unknown): string {
  return error instanceof ApiError ? error.detail : String(error)
}

function toOrder(value: string): number {
  return value.trim() === '' ? 0 : Number(value)
}

/**
 * 套餐页的状态与动作（平台侧）：列表、新建、选中套餐的编辑、白名单勾选（目录取租户侧 `permissions.list?side=tenant`）、删除。
 * 白名单整体替换：保存时把勾选集合原样交给 `editions.set-permissions`，后端只接受租户能生效的权限并立即作用到绑定它的租户。
 */
export function useEditions() {
  const items = ref<EditionRow[]>([])
  const catalog = ref<CatalogItem[]>([])
  const loading = ref(false)
  const listError = ref<string | null>(null)

  const form = reactive<CreateEditionForm>({ code: '', name: '', description: '', order: '', isDefault: false })
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)

  const selected = ref<EditionDetail | null>(null)
  const edit = reactive<EditEditionForm>({ name: '', description: '', order: '', isDefault: false })
  const editErrors = ref<Record<string, string[]>>({})
  const draft = ref<Set<string>>(new Set())
  const detailError = ref<string | null>(null)
  const notice = ref<'saved' | 'permissions_saved' | null>(null)
  const working = ref(false)

  /** 目录按包分组，组内按权限码排序（与角色页一致） */
  const groups = computed(() => {
    const byPackage = new Map<string, CatalogItem[]>()
    for (const item of catalog.value)
      byPackage.set(item.package, [...(byPackage.get(item.package) ?? []), item])
    return [...byPackage.entries()].map(([pkg, list]) => ({ package: pkg, items: list }))
  })

  const dirty = computed(() => {
    const current = new Set(selected.value?.permissions ?? [])
    return current.size !== draft.value.size || [...draft.value].some(code => !current.has(code))
  })

  async function load() {
    loading.value = true
    listError.value = null
    try {
      const [list, tenantSide] = await Promise.all([editionsApi.list({}), permissionsApi.list({ side: 'tenant' })])
      items.value = list.items
      catalog.value = tenantSide.items
    }
    catch (error) {
      items.value = []
      listError.value = describe(error)
    }
    finally {
      loading.value = false
    }
  }

  function fieldError(field: keyof CreateEditionForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  function editError(field: keyof EditEditionForm): string | undefined {
    return editErrors.value[field]?.[0]
  }

  function apply(detail: EditionDetail) {
    selected.value = detail
    edit.name = detail.edition.name
    edit.description = detail.edition.description ?? ''
    edit.order = String(detail.edition.order)
    edit.isDefault = detail.edition.isDefault
    draft.value = new Set(detail.permissions)
  }

  /** 新建：白名单先空着（什么都不放行），建好后在详情里勾选 */
  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const created = await editionsApi.create({
        code: form.code.trim(),
        name: form.name.trim(),
        description: form.description.trim() || null,
        isDefault: form.isDefault,
        order: toOrder(form.order),
        permissions: [],
      })
      form.code = ''
      form.name = ''
      form.description = ''
      form.order = ''
      form.isDefault = false
      await load()
      apply(created)
      notice.value = null
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

  async function select(id: string) {
    detailError.value = null
    notice.value = null
    editErrors.value = {}
    try {
      apply(await editionsApi.get({ id }))
    }
    catch (error) {
      selected.value = null
      detailError.value = describe(error)
    }
  }

  /** 编辑：名称、说明、排序与缺省标记（设为缺省时后端清掉其余套餐的缺省） */
  async function save(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    editErrors.value = {}
    try {
      apply(await editionsApi.update({ id: current.edition.id, name: edit.name.trim(), description: edit.description.trim() || null, isDefault: edit.isDefault, order: toOrder(edit.order) }))
      await load()
      notice.value = 'saved'
      return true
    }
    catch (error) {
      if (error instanceof ApiError) {
        editErrors.value = error.errors ?? {}
        if (!error.hasFieldErrors)
          detailError.value = error.detail
      }
      else {
        detailError.value = String(error)
      }
      return false
    }
    finally {
      working.value = false
    }
  }

  function togglePermission(code: string) {
    const next = new Set(draft.value)
    if (next.has(code))
      next.delete(code)
    else next.add(code)
    draft.value = next
  }

  /** 整组勾上或清空 */
  function setGroup(pkg: string, on: boolean) {
    const next = new Set(draft.value)
    for (const item of catalog.value.filter(i => i.package === pkg)) {
      if (on)
        next.add(item.code)
      else next.delete(item.code)
    }
    draft.value = next
  }

  /** 保存白名单：整体替换；绑定该套餐的租户随即按新白名单计算有效权限 */
  async function savePermissions(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    try {
      apply(await editionsApi.setPermissions({ id: current.edition.id, permissions: [...draft.value].sort() }))
      await load()
      notice.value = 'permissions_saved'
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

  /** 删除：还有租户绑定的套餐由后端逐项拒绝（TENANCY_CONFLICT_007） */
  async function remove(): Promise<boolean> {
    const current = selected.value
    if (!current)
      return false
    working.value = true
    detailError.value = null
    notice.value = null
    try {
      const result = await editionsApi.delete({ ids: [current.edition.id] })
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
    catalog,
    groups,
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
    draft,
    dirty,
    detailError,
    notice,
    working,
    load,
    submit,
    select,
    save,
    togglePermission,
    setGroup,
    savePermissions,
    remove,
  }
}
