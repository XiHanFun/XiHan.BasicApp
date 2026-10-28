import type { SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { editions as editionsApi, tenantMembers as tenantMembersApi, tenants as tenantsApi, toPageRequest, toSchemaPageResult, users as usersApi } from '~/contracts'
import { ApiError } from '~/request'

export type TenantRow = ResponseOf<'tenants.list'>['items'][number]
export type TenantDetail = ResponseOf<'tenants.get'>
export type EditionOption = ResponseOf<'editions.list'>['items'][number]
export type TenantMember = ResponseOf<'tenant-members.list'>['items'][number]
export type TenantStatus = TenantRow['status']
export type TenantMemberType = TenantMember['memberType']

export const TENANT_STATUSES: readonly TenantStatus[] = ['Active', 'Suspended']
export const TENANT_MEMBER_TYPES: readonly TenantMemberType[] = ['Member', 'Owner']

/** 成员弹窗里用户候选每次取回的条数 */
export const MEMBER_USER_PAGE_SIZE = 20

/** 弹窗表单：开通与编辑共用一份（租户 ID 只在开通时可填） */
export interface TenantForm {
  tenantId: string
  name: string
  description: string
  /** 到期时间（毫秒时间戳，日期选择的取值）；空即长期有效 */
  expiresAt: number | null
  order: number
  /** 套餐主键：开通时空即缺省套餐，编辑时空即不受套餐限制 */
  editionId: null | string
}

export type TenantEditorMode = 'create' | 'edit'

export interface AdminForm {
  userName: string
  password: string
  displayName: string
  email: string
}

export interface MemberForm {
  userId: null | string
  memberType: TenantMemberType
}

function blankToNull(value: string): null | string {
  const trimmed = value.trim()
  return trimmed === '' ? null : trimmed
}

function toTimestamp(value: null | string | undefined): number | null {
  return value ? new Date(value).getTime() : null
}

function toIso(value: number | null): null | string {
  return value === null ? null : new Date(value).toISOString()
}

function describe(error: unknown): string {
  return error instanceof ApiError ? error.detail : String(error)
}

/** 批量结果里只有失败项的错误码：逐项拒绝的原因拼成一条报错 */
function failedCodes(result: { failed: { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(f => f.code).join('; '))
}

/** 提交失败的落点：字段级错误挂回字段，其余给整条提示 */
function captureErrors(error: unknown, fieldErrors: { value: Record<string, string[]> }, submitError: { value: null | string }) {
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
 * SchemaPage 的资源：分页、删除与启停（启用即恢复、停用即停用；都是集合操作，逐项拒绝以错误码抛出）。
 * 搜索字段按后端白名单下发：关键字匹配租户 ID 与名称，状态精确匹配；排序只放后端接受的租户 ID / 名称 / 排序 / 到期 / 创建时间。
 */
export function tenantsResource(): SchemaResource<TenantRow> {
  return {
    page: async params => toSchemaPageResult(await tenantsApi.list(toPageRequest(params, { filters: { status: 'eq' } })), params),
    // 还有用户的租户由后端逐项拒绝（TENANCY_CONFLICT_003）
    remove: async id => failedCodes(await tenantsApi.delete({ ids: [String(id)] })),
    updateStatus: async (id, enabled) => failedCodes(await (enabled ? tenantsApi.resume : tenantsApi.suspend)({ ids: [String(id)] })),
  }
}

/**
 * 套餐选项（需要 tenancy.editions.read；页面按权限决定是否加载）：开通 / 编辑弹窗的下拉与列表里的套餐名。
 */
export function useEditionOptions() {
  const editions = ref<EditionOption[]>([])
  const error = ref<null | string>(null)

  async function load() {
    error.value = null
    try {
      editions.value = (await editionsApi.list({})).items
    }
    catch (e) {
      editions.value = []
      error.value = describe(e)
    }
  }

  /** 套餐名：没绑定即不受限（返回 null），绑定的不在选项里（没有读套餐的权限）就显示主键 */
  function editionName(id: null | string | undefined): null | string {
    if (!id)
      return null
    return editions.value.find(e => e.id === id)?.name ?? id
  }

  return { editions, error, load, editionName }
}

function emptyForm(): TenantForm {
  return { tenantId: '', name: '', description: '', expiresAt: null, order: 0, editionId: null }
}

/**
 * 开通 / 编辑弹窗。编辑时套餐与资料分属两个操作（`tenants.update` 不含套餐，换套餐走 `tenants.change-edition`）：
 * 先存资料，套餐变了再换，与 4.x 编辑弹窗里改状态的做法相同。字段级错误挂回对应字段。
 */
export function useTenantEditor() {
  const open = ref(false)
  const mode = ref<TenantEditorMode>('create')
  const editing = ref<TenantRow | null>(null)
  const form = reactive<TenantForm>(emptyForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)

  const isEdit = computed(() => mode.value === 'edit')

  function reset(next: Partial<TenantForm> = {}) {
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

  function startEdit(row: TenantRow) {
    mode.value = 'edit'
    editing.value = row
    reset({
      tenantId: row.tenantId,
      name: row.name,
      description: row.description ?? '',
      expiresAt: toTimestamp(row.expiresAt),
      order: row.order,
      editionId: row.editionId ?? null,
    })
    open.value = true
  }

  function fieldError(field: keyof TenantForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  /** 保存成功返回保存后的租户（开通时页面据此接着初始化管理员），失败返回 null、弹窗保持打开 */
  async function submit(): Promise<null | TenantRow> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const current = editing.value
      let saved: TenantRow
      if (mode.value === 'edit' && current) {
        saved = await tenantsApi.update({
          id: current.id,
          name: form.name.trim(),
          description: blankToNull(form.description),
          expiresAt: toIso(form.expiresAt),
          order: form.order,
        })
        // 空选即不受套餐限制（显式选择）；后端写入后立即失效该租户的门控缓存
        if ((form.editionId ?? null) !== (current.editionId ?? null))
          saved = await tenantsApi.changeEdition({ id: current.id, editionId: form.editionId })
      }
      else {
        saved = await tenantsApi.create({
          tenantId: form.tenantId.trim(),
          name: form.name.trim(),
          description: blankToNull(form.description),
          expiresAt: toIso(form.expiresAt),
          order: form.order,
          editionId: form.editionId,
        })
      }
      open.value = false
      return saved
    }
    catch (error) {
      captureErrors(error, fieldErrors, submitError)
      return null
    }
    finally {
      submitting.value = false
    }
  }

  return { open, mode, isEdit, editing, form, fieldError, submitError, submitting, startCreate, startEdit, submit }
}

function emptyAdminForm(): AdminForm {
  return { userName: '', password: '', displayName: '', email: '' }
}

/**
 * 初始化管理员弹窗：每个租户一次（后端拒绝重复初始化）；建的用户持有租户侧全部权限、首次登录须改密。
 */
export function useAdminInitializer() {
  const open = ref(false)
  const tenant = ref<TenantRow | null>(null)
  const form = reactive<AdminForm>(emptyAdminForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)

  function start(row: TenantRow) {
    tenant.value = row
    Object.assign(form, emptyAdminForm())
    fieldErrors.value = {}
    submitError.value = null
    open.value = true
  }

  function fieldError(field: keyof AdminForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    const current = tenant.value
    if (!current)
      return false
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      await tenantsApi.initializeAdmin({
        id: current.id,
        userName: form.userName.trim(),
        password: form.password,
        displayName: blankToNull(form.displayName),
        email: blankToNull(form.email),
      })
      open.value = false
      return true
    }
    catch (error) {
      captureErrors(error, fieldErrors, submitError)
      return false
    }
    finally {
      submitting.value = false
    }
  }

  return { open, tenant, form, fieldError, submitError, submitting, start, submit }
}

/**
 * 详情抽屉：概览（`tenants.get` 带管理员登录名与用户数）与成员（`tenant-members.list`，一次全列，按加入时间排）。
 * 成员按 tenancy.tenant-members.read 门控，没有权限就不取。
 */
export function useTenantDetail(canReadMembers: () => boolean) {
  const open = ref(false)
  const loading = ref(false)
  const detail = ref<null | TenantDetail>(null)
  const error = ref<null | string>(null)
  const members = ref<TenantMember[]>([])
  const membersLoading = ref(false)
  const membersError = ref<null | string>(null)

  async function loadMembers() {
    const current = detail.value
    if (!current)
      return
    membersLoading.value = true
    membersError.value = null
    try {
      members.value = (await tenantMembersApi.list({ tenantId: current.tenant.tenantId })).items
    }
    catch (e) {
      members.value = []
      membersError.value = describe(e)
    }
    finally {
      membersLoading.value = false
    }
  }

  async function show(row: TenantRow) {
    open.value = true
    loading.value = true
    error.value = null
    detail.value = null
    members.value = []
    membersError.value = null
    try {
      detail.value = await tenantsApi.get({ id: row.id })
    }
    catch (e) {
      error.value = describe(e)
    }
    finally {
      loading.value = false
    }
    if (detail.value && canReadMembers())
      await loadMembers()
  }

  /** 移除成员：成员行不存在由后端逐项拒绝（TENANCY_NOTFOUND_002），以错误码抛出；成功后重取成员 */
  async function removeMember(member: TenantMember) {
    failedCodes(await tenantMembersApi.delete({ ids: [member.id] }))
    await loadMembers()
  }

  return { open, loading, detail, error, members, membersLoading, membersError, show, loadMembers, removeMember }
}

/**
 * 添加成员弹窗：用户从平台可见的用户里远程检索（`users.list` 关键字匹配登录名与显示名），成员类型缺省为成员。
 * 注册地就是该租户的用户与已有成员由后端拒绝（TENANCY_CONFLICT_005）。
 */
export function useMemberEditor() {
  const open = ref(false)
  const form = reactive<MemberForm>({ userId: null, memberType: 'Member' })
  const userOptions = ref<{ label: string, value: string, description: string }[]>([])
  const userLoading = ref(false)
  const userError = ref<null | string>(null)
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)

  async function searchUsers(keyword: string) {
    userLoading.value = true
    userError.value = null
    try {
      const result = await usersApi.list({ page: { index: 1, size: MEMBER_USER_PAGE_SIZE }, filters: [], sorts: [], keyword: blankToNull(keyword) })
      userOptions.value = result.items.map(user => ({ label: user.userName, value: user.id, description: `${user.displayName} · ${user.homeTenantId}` }))
    }
    catch (e) {
      userOptions.value = []
      userError.value = describe(e)
    }
    finally {
      userLoading.value = false
    }
  }

  function start() {
    form.userId = null
    form.memberType = 'Member'
    fieldErrors.value = {}
    submitError.value = null
    userOptions.value = []
    open.value = true
    void searchUsers('')
  }

  function fieldError(field: keyof MemberForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  /** 加进目录键为 tenantId 的租户（成员按目录键挂，不是租户主键） */
  async function submit(tenantId: string): Promise<boolean> {
    if (!form.userId)
      return false
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      await tenantMembersApi.create({ tenantId, userId: form.userId, memberType: form.memberType })
      open.value = false
      return true
    }
    catch (error) {
      captureErrors(error, fieldErrors, submitError)
      return false
    }
    finally {
      submitting.value = false
    }
  }

  return { open, form, userOptions, userLoading, userError, fieldError, submitError, submitting, searchUsers, start, submit }
}
