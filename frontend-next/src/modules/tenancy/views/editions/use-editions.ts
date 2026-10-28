import type { PermissionGrantItem, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { editions as editionsApi, permissions as permissionsApi, toSchemaPageResult } from '~/contracts'
import { ApiError } from '~/request'
import { randomString } from '~/utils'

export type EditionRow = ResponseOf<'editions.list'>['items'][number]
export type EditionDetail = ResponseOf<'editions.get'>
export type CatalogItem = ResponseOf<'permissions.list'>['items'][number]

/** 白名单穿梭框的条目：权限码即主键；白名单里有、租户侧目录里没有的码标 outside */
export interface WhitelistItem extends PermissionGrantItem {
  basicId: string
  outside: boolean
}

/** 弹窗表单：新建与编辑共用一份（编码只在新建时可填） */
export interface EditionForm {
  code: string
  name: string
  description: string
  isDefault: boolean
  order: number
}

export type EditionEditorMode = 'create' | 'edit'

const CODE_LETTERS = 'abcdefghijklmnopqrstuvwxyz'

function isBlank(value: unknown): boolean {
  return value === undefined || value === null || (typeof value === 'string' && value.trim() === '')
}

function blankToNull(value: string): null | string {
  const trimmed = value.trim()
  return trimmed === '' ? null : trimmed
}

function describe(error: unknown): string {
  return error instanceof ApiError ? error.detail : String(error)
}

/** 批量结果里只有失败项的错误码：逐项拒绝的原因拼成一条报错 */
function failedCodes(result: { failed: { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(f => f.code).join('; '))
}

function sameSet(left: readonly string[], right: readonly string[]): boolean {
  const set = new Set(right)
  return left.length === set.size && left.every(code => set.has(code))
}

/**
 * SchemaPage 的资源：分页与删除。
 * `editions.list` 不分页、不收筛选与排序（套餐数量有限，一次全列，后端按排序号、编码排好），资源在前端按页切片；
 * 页面因此不登记搜索与排序字段，查询里一旦出现筛选或排序就报错，而不是静默丢掉条件（与 toPageRequest 同一口径）。
 */
export function editionsResource(): SchemaResource<EditionRow> {
  return {
    page: async (params) => {
      const conditions = [
        ...Object.entries(params.filters).filter(([, value]) => !isBlank(value)).map(([field]) => field),
        ...(params.sorts ?? []).map(sort => sort.field),
        ...(params.conditionFilters ?? []).map(filter => filter.field),
      ]
      if (conditions.length > 0)
        throw new Error(`editions.list 不接受筛选与排序：${conditions.join(', ')}`)
      const { items } = await editionsApi.list({})
      const start = (params.page - 1) * params.pageSize
      return toSchemaPageResult({ items: items.slice(start, start + params.pageSize), total: items.length, page: { index: params.page, size: params.pageSize } }, params)
    },
    // 还有租户绑定的套餐由后端逐项拒绝（TENANCY_CONFLICT_007）
    remove: async id => failedCodes(await editionsApi.delete({ ids: [String(id)] })),
  }
}

/** 设为缺省：更新是整体覆盖，先取当前值再只改缺省标记；后端随之清掉其余套餐的缺省 */
export async function setDefaultEdition(id: string): Promise<EditionRow> {
  const { edition } = await editionsApi.get({ id })
  return (await editionsApi.update({ id: edition.id, name: edition.name, description: edition.description ?? null, isDefault: true, order: edition.order })).edition
}

/**
 * 租户侧权限目录 + 白名单 → 穿梭框条目。按「包.资源」分段；白名单里有、目录里没有的码（权限已下线或改了作用侧）
 * 也列出来并标 outside，否则一动穿梭框它们就被悄悄丢掉。
 */
export function toWhitelistItems(catalog: readonly CatalogItem[], whitelist: readonly string[], outsideGroup: string): WhitelistItem[] {
  const items: WhitelistItem[] = catalog.map(item => ({
    basicId: item.code,
    permissionCode: item.code,
    permissionName: item.displayName,
    groupCode: `${item.package}.${item.resource}`,
    groupName: `${item.package}.${item.resource}`,
    outside: false,
  }))
  const known = new Set(catalog.map(item => item.code))
  for (const code of whitelist) {
    if (!known.has(code))
      items.push({ basicId: code, permissionCode: code, permissionName: code, groupCode: outsideGroup, groupName: outsideGroup, outside: true })
  }
  return items
}

function emptyForm(): EditionForm {
  return { code: '', name: '', description: '', isDefault: false, order: 0 }
}

/**
 * 新建 / 编辑弹窗。新建时白名单为空（什么都不放行），页面接着打开白名单抽屉；编码创建后不可改。字段级错误挂回对应字段。
 */
export function useEditionEditor() {
  const open = ref(false)
  const mode = ref<EditionEditorMode>('create')
  const editing = ref<EditionRow | null>(null)
  const form = reactive<EditionForm>(emptyForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)

  const isEdit = computed(() => mode.value === 'edit')

  function reset(next: Partial<EditionForm> = {}) {
    Object.assign(form, emptyForm(), next)
    fieldErrors.value = {}
    submitError.value = null
  }

  function startCreate() {
    mode.value = 'create'
    editing.value = null
    reset()
    open.value = true
  }

  function startEdit(row: EditionRow) {
    mode.value = 'edit'
    editing.value = row
    reset({ code: row.code, name: row.name, description: row.description ?? '', isDefault: row.isDefault, order: row.order })
    open.value = true
  }

  /** 随机编码（4.x 同款）：小写字母开头、只含小写字母与数字，合后端的编码格式；重复由后端唯一校验拒绝 */
  function generateCode() {
    form.code = `${CODE_LETTERS.charAt(Math.floor(Math.random() * CODE_LETTERS.length))}${randomString(7).toLowerCase()}`
  }

  function fieldError(field: keyof EditionForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  /** 保存成功返回保存后的套餐，失败返回 null、弹窗保持打开 */
  async function submit(): Promise<EditionRow | null> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const current = editing.value
      const saved = mode.value === 'edit' && current
        ? await editionsApi.update({ id: current.id, name: form.name.trim(), description: blankToNull(form.description), isDefault: form.isDefault, order: form.order })
        : await editionsApi.create({ code: form.code.trim(), name: form.name.trim(), description: blankToNull(form.description), isDefault: form.isDefault, order: form.order, permissions: [] })
      open.value = false
      return saved.edition
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
      return null
    }
    finally {
      submitting.value = false
    }
  }

  return { open, mode, isEdit, editing, form, fieldError, submitError, submitting, startCreate, startEdit, generateCode, submit }
}

/**
 * 白名单抽屉：目录取租户侧（`permissions.list?side=tenant`，一次取全后缓存），白名单取 `editions.get`；
 * 本地勾选、整体替换保存（`editions.set-permissions`），后端只接受租户能生效的权限并立即作用到绑定它的租户。
 * 加载失败时只读：拿不到现有白名单就算不出该存什么。
 */
export function useEditionPermissions() {
  const open = ref(false)
  const edition = ref<EditionRow | null>(null)
  const catalog = ref<CatalogItem[]>([])
  /** 已保存的白名单 */
  const whitelist = ref<string[]>([])
  /** 本地勾选（穿梭框右栏） */
  const checked = ref<string[]>([])
  const loading = ref(false)
  const loadError = ref<null | string>(null)
  const saving = ref(false)
  const saveError = ref<null | string>(null)

  const dirty = computed(() => !sameSet(checked.value, whitelist.value))

  async function loadCatalog() {
    if (catalog.value.length === 0)
      catalog.value = (await permissionsApi.list({ side: 'tenant' })).items
  }

  async function start(row: EditionRow) {
    edition.value = row
    whitelist.value = []
    checked.value = []
    loadError.value = null
    saveError.value = null
    open.value = true
    loading.value = true
    try {
      const [, detail] = await Promise.all([loadCatalog(), editionsApi.get({ id: row.id })])
      edition.value = detail.edition
      whitelist.value = [...detail.permissions]
      checked.value = [...detail.permissions]
    }
    catch (error) {
      loadError.value = describe(error)
    }
    finally {
      loading.value = false
    }
  }

  function setChecked(next: ReadonlyArray<number | string>) {
    checked.value = next.map(String)
  }

  async function save(): Promise<boolean> {
    const current = edition.value
    if (!current || loadError.value)
      return false
    saving.value = true
    saveError.value = null
    try {
      const detail = await editionsApi.setPermissions({ id: current.id, permissions: [...checked.value].sort() })
      edition.value = detail.edition
      whitelist.value = [...detail.permissions]
      checked.value = [...detail.permissions]
      return true
    }
    catch (error) {
      saveError.value = describe(error)
      return false
    }
    finally {
      saving.value = false
    }
  }

  return { open, edition, catalog, whitelist, checked, dirty, loading, loadError, saving, saveError, start, setChecked, save }
}
