import type { HintTone } from '../subscription/subscription'
import type { SchemaResource } from '~/components'
import type { RequestOf, ResponseOf } from '~/contracts'
import { computed, reactive, ref, shallowRef } from 'vue'
import { PLATFORM_TENANT_ID } from '~/constants'
import { editions as editionsApi, tenantMembers as tenantMembersApi, tenants as tenantsApi, toPageRequest, toSchemaPageResult, users as usersApi } from '~/contracts'
import { ApiError } from '~/request'
import { formatFileSize } from '~/utils'
import { formatSeats, quotaLevel, storageLimitBytes, usageText } from '../subscription/subscription'

export type TenantRow = ResponseOf<'tenants.list'>['items'][number]
export type TenantDetail = ResponseOf<'tenants.get'>
export type TenantQuota = ResponseOf<'tenants.quotas'>['items'][number]
export type EditionOption = ResponseOf<'editions.list'>['items'][number]
export type TenantMember = ResponseOf<'tenant-members.list'>['items'][number]
export type TenantStatus = TenantRow['status']
export type TenantMemberType = TenantMember['memberType']
export type UserRow = ResponseOf<'users.list'>['items'][number]
export type TenantIsolationMode = TenantRow['isolationMode']
export type TenantDatabaseStatus = TenantRow['databaseStatus']
/** tenants.configure-database 除租户主键以外的部分：隔离方式与独立库的连接 */
export type DatabaseConfiguration = Omit<RequestOf<'tenants.configure-database'>, 'id'>

export const TENANT_STATUSES: readonly TenantStatus[] = ['Active', 'Suspended']
/** 隔离方式：共库（与平台同一个库、按租户字段隔离）与独立库（租户的业务数据在自己的库里） */
export const TENANT_ISOLATION_MODES: readonly TenantIsolationMode[] = ['Field', 'Database']
/** 独立库的状态：共库即无需独立库；独立库从待初始化起，初始化成功即就绪、失败可改正连接后再试 */
export const TENANT_DATABASE_STATUSES: readonly TenantDatabaseStatus[] = ['NotRequired', 'Pending', 'Ready', 'Failed']
/** 独立库的库类型（与 tenants.configure-database 接受的取值一致）；不选即与平台库相同 */
export const TENANT_DATABASE_PROVIDERS = ['postgresql', 'sqlite', 'mysql', 'sqlserver'] as const
/** 库类型的显示名是产品名，不随区域翻译 */
export const DATABASE_PROVIDER_NAMES: Readonly<Record<string, string>> = { postgresql: 'PostgreSQL', sqlite: 'SQLite', mysql: 'MySQL', sqlserver: 'SQL Server' }
/** 成员类型：普通成员（注册地在别的租户的用户，占席位）与运维支持（平台账号，不占席位、不能当所有者）；所有者是租户上的 adminUserId，不是成员类型 */
export const TENANT_MEMBER_TYPES: readonly TenantMemberType[] = ['Member', 'Support']

/** 成员弹窗里用户候选每次取回的条数 */
export const MEMBER_USER_PAGE_SIZE = 20

/** 用户候选（组合框选项）：登录名为主、显示名与注册地作副文本，成员与所有者两个弹窗同一写法 */
export interface UserOption {
  label: string
  value: string
  description: string
}

function userOption(user: { userName: string, displayName: string, homeTenantId: string }, id: string): UserOption {
  return { label: user.userName, value: id, description: `${user.displayName} · ${user.homeTenantId}` }
}

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
  /** 租户自己的席位上限，覆盖套餐的；空即沿用套餐 */
  userLimit: number | null
  /** 租户自己的存储上限（MB），覆盖套餐的；空即沿用套餐 */
  storageLimitMb: number | null
  /** 隔离方式：开通与更新都不带，存好后另走 tenants.configure-database */
  isolationMode: TenantIsolationMode
  /** 独立库的库类型；空即与平台库相同 */
  databaseProvider: null | string
  /** 独立库的连接串：后端从不返回，编辑时留空即沿用已配置的 */
  connectionString: string
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
  /** 生效时间（毫秒时间戳，日期选择的取值）；空即加入即生效 */
  effectiveAt: number | null
  /** 到期时间（毫秒时间戳）；空即不到期，须晚于生效时间 */
  expiresAt: number | null
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

/** 成员是否在有效期里：生效时间为空或已到、到期时间为空或还没到（与后端 IsInEffect 同口径） */
export function isMemberInEffect(member: Pick<TenantMember, 'effectiveAt' | 'expiresAt'>, now = Date.now()): boolean {
  const from = toTimestamp(member.effectiveAt)
  const until = toTimestamp(member.expiresAt)
  return (from === null || from <= now) && (until === null || until > now)
}

/** 成员是否已到期（到了到期时间，不能再进入该租户） */
export function isMemberExpired(member: Pick<TenantMember, 'expiresAt'>, now = Date.now()): boolean {
  const until = toTimestamp(member.expiresAt)
  return until !== null && until <= now
}

/** 成员候选的用户范围（users.list 的 homeTenantId 筛选）：运维支持只能是平台账号，普通成员只能是租户用户（注册地就是本租户的另由调用方去掉） */
export function memberUserFilters(memberType: TenantMemberType): { field: string, operator: 'eq' | 'ne', value: string }[] {
  return [{ field: 'homeTenantId', operator: memberType === 'Support' ? 'eq' : 'ne', value: PLATFORM_TENANT_ID }]
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
 * 给了用量就随每次取页（含刷新）并行取一次全部租户的配额与用量，列表的用量列按租户 ID 对上。
 */
export function tenantsResource(quotas?: { load: () => Promise<void> }): SchemaResource<TenantRow> {
  return {
    page: async (params) => {
      const [page] = await Promise.all([tenantsApi.list(toPageRequest(params, { filters: { status: 'eq' } })), quotas?.load()])
      return toSchemaPageResult(page, params)
    },
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

/**
 * 弹窗里上限留空时沿用的套餐：选了即所选；开通时没选即缺省套餐，编辑时没选即没有套餐（null，不限）。
 * 所选不在选项里、或开通时没有缺省套餐为 undefined（说不出沿用多少）。
 */
export function inheritedEdition(editions: readonly EditionOption[], editionId: null | string, creating: boolean): EditionOption | null | undefined {
  if (editionId)
    return editions.find(e => e.id === editionId)
  return creating ? editions.find(e => e.isDefault) : null
}

/** 一项用量的单元格：「已用 / 上限」与水位（到 80% 吃紧、到上限超限，与订阅页同一口径） */
export interface UsageCell {
  text: string
  tone: HintTone
}

/** 席位用量：席位 = 注册地是该租户的用户 + 租户成员 */
export function seatUsage(quota: Pick<TenantQuota, 'usedSeats' | 'userLimit'>, unlimited: string): UsageCell {
  const limit = quota.userLimit ?? null
  return { text: usageText(quota.usedSeats, limit, formatSeats, unlimited), tone: quotaLevel(quota.usedSeats, limit).tone }
}

/** 存储用量：上限以 MB 表达、已用以字节统计，换算到字节再比 */
export function storageUsage(quota: Pick<TenantQuota, 'storageLimitMb' | 'usedStorageBytes'>, unlimited: string): UsageCell {
  const limit = storageLimitBytes(quota.storageLimitMb)
  return { text: usageText(quota.usedStorageBytes, limit, formatFileSize, unlimited), tone: quotaLevel(quota.usedStorageBytes, limit).tone }
}

/**
 * 列表的用量列（`tenants.quotas`，有效上限 = 租户自己的 → 套餐的 → 不限）：按租户 ID 存一份，随每次取页重取。
 * 取不到时清空（用量列显示 -）并把原因交给 onError，不拖垮列表本身。
 */
export function useTenantQuotas(onError?: (message: string) => void) {
  const byTenant = shallowRef<ReadonlyMap<string, TenantQuota>>(new Map())
  const error = ref<null | string>(null)

  async function load() {
    error.value = null
    try {
      const { items } = await tenantsApi.quotas({})
      byTenant.value = new Map(items.map(quota => [quota.tenantId, quota]))
    }
    catch (e) {
      byTenant.value = new Map()
      error.value = describe(e)
      onError?.(error.value)
    }
  }

  function of(tenantId: string): TenantQuota | undefined {
    return byTenant.value.get(tenantId)
  }

  return { byTenant, error, load, of }
}

/**
 * 配额核对抽屉（4.x 同）：只列超出席位或存储上限的租户（`tenants.quotas?overOnly=true`）。
 * 配额只拦截租户自己新增用户与上传文件，已经超额的不回收，在这里报告。
 */
export function useQuotaAudit() {
  const open = ref(false)
  const loading = ref(false)
  const items = ref<TenantQuota[]>([])
  const error = ref<null | string>(null)

  async function load() {
    loading.value = true
    error.value = null
    try {
      items.value = (await tenantsApi.quotas({ overOnly: true })).items
    }
    catch (e) {
      items.value = []
      error.value = describe(e)
    }
    finally {
      loading.value = false
    }
  }

  async function start() {
    open.value = true
    await load()
  }

  return { open, loading, items, error, load, start }
}

// ── 独立库（开通三步：开通 →（独立库）初始化数据库 → 初始化管理员） ──

/**
 * 弹窗里的隔离设置相对租户现状要不要另走 `tenants.configure-database`（开通与更新都不带这几项）：
 * 独立库已就绪即不能再改（TENANCY_CONFLICT_013），不发；改回共库只发隔离方式（后端清掉连接）；
 * 独立库在换了隔离方式、填了新连接串或换了库类型时才发，连接串留空发 null（已配过的沿用，没配过的由后端拒绝 TENANCY_VALIDATION_008）。
 */
export function databaseConfiguration(
  form: Pick<TenantForm, 'connectionString' | 'databaseProvider' | 'isolationMode'>,
  tenant: Pick<TenantRow, 'databaseProvider' | 'databaseStatus' | 'isolationMode'>,
): DatabaseConfiguration | null {
  if (tenant.databaseStatus === 'Ready')
    return null
  if (form.isolationMode === 'Field')
    return tenant.isolationMode === 'Field' ? null : { isolationMode: 'Field' }
  const connectionString = blankToNull(form.connectionString)
  const databaseProvider = form.databaseProvider ?? null
  if (tenant.isolationMode === 'Database' && connectionString === null && databaseProvider === (tenant.databaseProvider ?? null))
    return null
  return { isolationMode: 'Database', connectionString, databaseProvider }
}

/** 能初始化独立库：独立库且还没就绪（待初始化或上次失败；已就绪的后端拒绝 TENANCY_CONFLICT_013） */
export function canInitializeDatabase(row: Pick<TenantRow, 'databaseStatus' | 'isolationMode'>): boolean {
  return row.isolationMode === 'Database' && row.databaseStatus !== 'Ready'
}

/** 能初始化管理员：还没有管理员，且是共库或独立库已就绪（独立库没就绪时后端拒绝 TENANCY_CONFLICT_014） */
export function canInitializeAdmin(row: Pick<TenantRow, 'adminUserId' | 'databaseStatus' | 'isolationMode'>): boolean {
  return !row.adminUserId && (row.isolationMode === 'Field' || row.databaseStatus === 'Ready')
}

/** 初始化独立库的结果：ready 即已就绪；没就绪时 error 是原因 */
export interface DatabaseInitOutcome {
  ready: boolean
  /** 初始化后的租户；请求本身被拒（不是独立库、已就绪等）时为 null */
  tenant: null | TenantRow
  /** 初始化失败（照常 200、状态为 Failed）取 databaseError，请求被拒取错误详情；成功为 null */
  error: null | string
}

/**
 * 初始化独立库（`tenants.initialize-database`，平台侧）：建库、建表并登记结构版本。
 * 初始化失败不是请求失败——响应照常返回、databaseStatus 为 Failed 并带 databaseError，改正连接后可以再试。
 */
export async function initializeTenantDatabase(row: Pick<TenantRow, 'id'>): Promise<DatabaseInitOutcome> {
  try {
    const tenant = await tenantsApi.initializeDatabase({ id: row.id })
    const ready = tenant.databaseStatus === 'Ready'
    return { ready, tenant, error: ready ? null : (tenant.databaseError ?? null) }
  }
  catch (e) {
    return { ready: false, tenant: null, error: describe(e) }
  }
}

function emptyForm(): TenantForm {
  return { tenantId: '', name: '', description: '', expiresAt: null, order: 0, editionId: null, userLimit: null, storageLimitMb: null, isolationMode: 'Field', databaseProvider: null, connectionString: '' }
}

/**
 * 开通 / 编辑弹窗。编辑时套餐与资料分属两个操作（`tenants.update` 不含套餐，换套餐走 `tenants.change-edition`）：
 * 先存资料，套餐变了再换，与 4.x 编辑弹窗里改状态的做法相同。字段级错误挂回对应字段。
 * 隔离方式与独立库的连接同样另走 `tenants.configure-database`（canConfigureDatabase 为真、且相对租户现状有变化时，在开通或更新之后）。
 * 开通成功而配库失败时弹窗转为编辑这个已开通的租户：再保存走更新与配库，不会重复开通。
 */
export function useTenantEditor(canConfigureDatabase: () => boolean) {
  const open = ref(false)
  const mode = ref<TenantEditorMode>('create')
  const editing = ref<TenantRow | null>(null)
  const form = reactive<TenantForm>(emptyForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)

  const isEdit = computed(() => mode.value === 'edit')
  /** 独立库已就绪：隔离方式与连接不能再改 */
  const databaseLocked = computed(() => isEdit.value && editing.value?.databaseStatus === 'Ready')
  /** 已配过独立库的连接：编辑时连接串留空即沿用（连接串从不回显） */
  const hasConnection = computed(() => isEdit.value && editing.value?.isolationMode === 'Database')

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
      userLimit: row.userLimit ?? null,
      storageLimitMb: row.storageLimitMb ?? null,
      isolationMode: row.isolationMode,
      databaseProvider: row.databaseProvider ?? null,
    })
    open.value = true
  }

  function fieldError(field: keyof TenantForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  /** 开通成功、后面一步失败：转为编辑这个已开通的租户，套餐按建好的回填（开通时留空即缺省套餐，回填后再存不会误换成不受限） */
  function adopt(created: TenantRow) {
    mode.value = 'edit'
    editing.value = created
    form.tenantId = created.tenantId
    form.editionId = created.editionId ?? null
  }

  /**
   * 保存成功返回保存后的租户（开通时页面据此接着初始化数据库或管理员），失败返回 null、弹窗保持打开。
   * 开通成功而配库失败时也返回 null，此时 isEdit 已转为真（页面据此刷新列表）。
   */
  async function submit(): Promise<null | TenantRow> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    let created: null | TenantRow = null
    try {
      const current = editing.value
      let saved: TenantRow
      if (mode.value === 'edit' && current) {
        // 更新是整体覆盖：上限清空即改回沿用套餐
        saved = await tenantsApi.update({
          id: current.id,
          name: form.name.trim(),
          description: blankToNull(form.description),
          expiresAt: toIso(form.expiresAt),
          order: form.order,
          userLimit: form.userLimit,
          storageLimitMb: form.storageLimitMb,
        })
        // 空选即不受套餐限制（显式选择）；后端写入后立即失效该租户的门控缓存
        if ((form.editionId ?? null) !== (current.editionId ?? null))
          saved = await tenantsApi.changeEdition({ id: current.id, editionId: form.editionId })
      }
      else {
        saved = created = await tenantsApi.create({
          tenantId: form.tenantId.trim(),
          name: form.name.trim(),
          description: blankToNull(form.description),
          expiresAt: toIso(form.expiresAt),
          order: form.order,
          editionId: form.editionId,
          userLimit: form.userLimit,
          storageLimitMb: form.storageLimitMb,
        })
      }
      // 更新与换套餐不动隔离设置：保存后的租户就是配库前的现状（开通的即共库）
      const database = canConfigureDatabase() ? databaseConfiguration(form, saved) : null
      if (database)
        saved = await tenantsApi.configureDatabase({ id: saved.id, ...database })
      open.value = false
      return saved
    }
    catch (error) {
      if (created)
        adopt(created)
      captureErrors(error, fieldErrors, submitError)
      return null
    }
    finally {
      submitting.value = false
    }
  }

  return { open, mode, isEdit, editing, form, databaseLocked, hasConnection, fieldError, submitError, submitting, startCreate, startEdit, submit }
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
 * 详情抽屉：概览（`tenants.get` 带管理员登录名、用户数与配额用量）与成员（`tenant-members.list`，一次全列，按加入时间排）。
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

  /** 原地重取详情与成员（转移所有权之后）：不清空已显示的内容，读失败原样抛出、由调用方提示 */
  async function refresh() {
    const current = detail.value
    if (!current)
      return
    detail.value = await tenantsApi.get({ id: current.tenant.id })
    if (canReadMembers())
      await loadMembers()
  }

  /** 移除成员：成员行不存在（TENANCY_NOTFOUND_002）、是租户所有者（TENANCY_CONFLICT_012）由后端逐项拒绝，以错误码抛出；成功后重取成员 */
  async function removeMember(member: TenantMember) {
    failedCodes(await tenantMembersApi.delete({ ids: [member.id] }))
    await loadMembers()
  }

  return { open, loading, detail, error, members, membersLoading, membersError, show, refresh, loadMembers, removeMember }
}

function emptyMemberForm(): MemberForm {
  return { userId: null, memberType: 'Member', effectiveAt: null, expiresAt: null }
}

/**
 * 添加成员弹窗（4.x「支持人员入驻」与加成员合在这一处）：先选成员类型，用户按类型远程检索（`users.list` 关键字匹配登录名与显示名）——
 * 运维支持只在平台账号里挑（homeTenantId eq platform），普通成员只在租户用户里挑（homeTenantId ne platform，注册地就是本租户的也去掉）；
 * 换类型即清掉已选的用户、按空关键字重新检索。生效与到期时间可选，按 ISO 发出、留空发 null。
 * 已有成员（TENANCY_CONFLICT_005）、类型与账号不符（TENANCY_VALIDATION_006）、到期不晚于生效（TENANCY_VALIDATION_007）由后端拒绝。
 */
export function useMemberEditor() {
  const open = ref(false)
  /** 目标租户的目录键（成员按目录键挂，不是租户主键） */
  const tenantId = ref<null | string>(null)
  const form = reactive<MemberForm>(emptyMemberForm())
  const userOptions = ref<UserOption[]>([])
  const userLoading = ref(false)
  const userError = ref<null | string>(null)
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)
  let searchSeq = 0

  /** 只认最后一次检索的结果（连续打字、换类型时慢请求不盖掉新结果）；当前选中的用户始终留在候选里 */
  async function searchUsers(keyword: string) {
    const seq = ++searchSeq
    const home = tenantId.value
    userLoading.value = true
    userError.value = null
    try {
      const result = await usersApi.list({ page: { index: 1, size: MEMBER_USER_PAGE_SIZE }, filters: memberUserFilters(form.memberType), sorts: [], keyword: blankToNull(keyword) })
      if (seq !== searchSeq)
        return
      const options = result.items.filter(user => user.homeTenantId !== home).map(user => userOption(user, user.id))
      const selected = userOptions.value.find(option => option.value === form.userId)
      if (selected && !options.some(option => option.value === selected.value))
        options.unshift(selected)
      userOptions.value = options
    }
    catch (e) {
      if (seq !== searchSeq)
        return
      userOptions.value = []
      userError.value = describe(e)
    }
    finally {
      if (seq === searchSeq)
        userLoading.value = false
    }
  }

  /** 换成员类型：候选范围跟着换，已选的用户不再适用，清掉后按空关键字重新检索 */
  function changeMemberType(value: unknown) {
    const next = TENANT_MEMBER_TYPES.find(type => type === value)
    if (!next || next === form.memberType)
      return
    form.memberType = next
    form.userId = null
    userOptions.value = []
    void searchUsers('')
  }

  /** 给目录键为 target 的租户加成员：缺省普通成员、加入即生效、不到期 */
  function start(target: string) {
    tenantId.value = target
    Object.assign(form, emptyMemberForm())
    fieldErrors.value = {}
    submitError.value = null
    userOptions.value = []
    open.value = true
    void searchUsers('')
  }

  function fieldError(field: keyof MemberForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    const target = tenantId.value
    if (!target || !form.userId)
      return false
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      await tenantMembersApi.create({
        tenantId: target,
        userId: form.userId,
        memberType: form.memberType,
        effectiveAt: toIso(form.effectiveAt),
        expiresAt: toIso(form.expiresAt),
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

  return { open, tenantId, form, userOptions, userLoading, userError, fieldError, submitError, submitting, searchUsers, changeMemberType, start, submit }
}

/**
 * 所有权候选：注册地是该租户的用户（users.list 已按注册地与启用状态筛过）在前，生效中的普通成员在后（成员已全量取回，按关键字在本地匹配登录名与显示名）；
 * 去掉现任所有者，同一用户只出一次。运维支持与不在有效期里的成员不能接任，不进候选（与后端 tenants.transfer-owner 的校验同口径）。
 */
export function ownerCandidates(users: readonly UserRow[], members: readonly TenantMember[], keyword: string, ownerId: null | string | undefined, now = Date.now()): UserOption[] {
  const term = keyword.trim().toLowerCase()
  const seen = new Set<string>(ownerId ? [ownerId] : [])
  const options: UserOption[] = []
  for (const user of users) {
    if (seen.has(user.id))
      continue
    seen.add(user.id)
    options.push(userOption(user, user.id))
  }
  for (const member of members) {
    if (member.memberType !== 'Member' || !isMemberInEffect(member, now) || seen.has(member.userId))
      continue
    if (term && !member.userName.toLowerCase().includes(term) && !member.displayName.toLowerCase().includes(term))
      continue
    seen.add(member.userId)
    options.push(userOption(member, member.userId))
  }
  return options
}

/** 能接任所有者的成员（成员行「设为所有者」）：生效中的普通成员、不是现任所有者 */
export function isOwnerCandidate(member: TenantMember, ownerId: null | string | undefined, now = Date.now()): boolean {
  return !!ownerId && member.memberType === 'Member' && member.userId !== ownerId && isMemberInEffect(member, now)
}

/**
 * 转移所有权弹窗（`tenants.transfer-owner`，平台侧；4.x 是成员行上的「设为所有者」）：新所有者从候选里挑——
 * 注册地是该租户的启用用户（`users.list` 按 homeTenantId eq 与 status eq Active 远程检索）与生效中的普通成员（详情抽屉已取的成员）。
 * 新所有者获得管理员角色，原所有者保留已有的角色。还没有所有者（TENANCY_CONFLICT_010）、已是所有者（TENANCY_CONFLICT_011）、
 * 不合格（TENANCY_VALIDATION_005：停用、锁定、不在有效期或是运维支持）由后端拒绝，给整条提示。
 */
export function useOwnerTransfer() {
  const open = ref(false)
  const tenant = ref<null | TenantRow>(null)
  const userId = ref<null | string>(null)
  const options = ref<UserOption[]>([])
  const loading = ref(false)
  const loadError = ref<null | string>(null)
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<null | string>(null)
  const submitting = ref(false)
  let roster: readonly TenantMember[] = []
  let searchSeq = 0

  /** 只认最后一次检索；当前选中的始终留在候选里。用户检索失败时成员候选照给，原因另报 */
  async function search(keyword: string) {
    const current = tenant.value
    if (!current)
      return
    const seq = ++searchSeq
    loading.value = true
    loadError.value = null
    let users: UserRow[] = []
    try {
      users = (await usersApi.list({
        page: { index: 1, size: MEMBER_USER_PAGE_SIZE },
        filters: [{ field: 'homeTenantId', operator: 'eq', value: current.tenantId }, { field: 'status', operator: 'eq', value: 'Active' }],
        sorts: [],
        keyword: blankToNull(keyword),
      })).items
    }
    catch (e) {
      if (seq === searchSeq)
        loadError.value = describe(e)
    }
    finally {
      if (seq === searchSeq)
        loading.value = false
    }
    if (seq !== searchSeq)
      return
    const next = ownerCandidates(users, roster, keyword, current.adminUserId)
    const selected = options.value.find(option => option.value === userId.value)
    if (selected && !next.some(option => option.value === selected.value))
      next.unshift(selected)
    options.value = next
  }

  /**
   * members：详情抽屉已取的成员（读不了成员时传空，只在本租户的用户里挑）；
   * preset：从成员行「设为所有者」进来时预选的用户（须是能接任的成员）。
   */
  function start(target: TenantRow, members: readonly TenantMember[], preset: null | string = null) {
    tenant.value = target
    roster = members
    const seeded = preset ? ownerCandidates([], members, '', target.adminUserId).filter(option => option.value === preset) : []
    userId.value = seeded.length > 0 ? preset : null
    options.value = seeded
    loadError.value = null
    fieldErrors.value = {}
    submitError.value = null
    open.value = true
    void search('')
  }

  function fieldError(field: 'userId'): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  /** 成功返回转移后的租户（adminUserId 已是新所有者）并关掉弹窗；失败返回 null、弹窗保持打开 */
  async function submit(): Promise<null | TenantRow> {
    const current = tenant.value
    if (!current || !userId.value)
      return null
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const saved = await tenantsApi.transferOwner({ id: current.id, userId: userId.value })
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

  return { open, tenant, userId, options, loading, loadError, fieldError, submitError, submitting, search, start, submit }
}
