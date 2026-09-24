<script setup lang="ts">
import type {
  ApiId,
  DateTimeString,
  PageResult,
  TenantAdminInitializeDto,
  TenantCreateDto,
  TenantDetailDto,
  TenantEditionListItemDto,
  TenantListItemDto,
  TenantMemberListItemDto,
  TenantOverQuotaDto,
  TenantUpdateDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhAlertContent, XhAlertDescription, XhAlertIndicator, XhAlertRoot, XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhEmptyStateAction, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldDescription, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFlex, XhFormFieldGroup, XhFormRoot, XhSpinner, XhTabsContent, XhTabsIndicator, XhTabsList, XhTabsRoot, XhTabsTrigger, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  querySortsFromSchema,
  TenantConfigStatus,
  tenantEditionApi,
  TenantIsolationMode,
  tenantManagementApi,
  TenantMemberInviteStatus,
  TenantMemberType,
  TenantStatus,
  userApi,
  ValidityStatus,
} from '@/api'
import XLogoUpload from '@/components/LogoUpload.vue'
import { MEMBER_INVITE_STATUS_OPTIONS, MEMBER_TYPE_OPTIONS, TENANT_CONFIG_STATUS_OPTIONS, TENANT_DATABASE_TYPE_OPTIONS, TENANT_ISOLATION_MODE_OPTIONS, TENANT_STATUS_OPTIONS, VALIDITY_STATUS_OPTIONS } from '@/constants'
import { Icon, resolveStatusTagTone, SchemaPage, SchemaPagination, XDatePicker, XEditModal, XInput, XNumberInput, XSelect, XUserAvatar } from '~/components'
import { dialog, toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { useAccessStore } from '~/stores'
import { formatDate, formatFileSize, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformTenantPage' })

interface TenantFormModel extends Omit<TenantCreateDto, 'adminUserName' | 'adminEmail' | 'adminPassword'> {
  // 表单里三个管理员字段恒为字符串（空串代表未填），编辑态不展示但保留占位，避免到处判空
  adminUserName: string
  adminEmail: string
  adminPassword: string
  basicId?: ApiId
  tenantStatus?: TenantStatus
}

/** 支持人员入驻表单 */
interface SupportMemberFormModel {
  effectiveTime: DateTimeString | null
  expirationTime: DateTimeString | null
  remark: string
  userId: ApiId | null
}

/** 租户管理员用户名长度区间，与后端 TenantAppService 的校验保持一致 */
const ADMIN_USER_NAME_MIN_LENGTH = 3
const ADMIN_USER_NAME_MAX_LENGTH = 50
/** 前端最低密码长度；真正的密码策略以后端校验为准 */
const ADMIN_PASSWORD_MIN_LENGTH = 8
const EMAIL_PATTERN = /^[^\s@]+@[^\s@][^\s.@]*\.[^\s@]+$/
/** 平台账号选择器每次拉取的用户数 */
const MEMBER_USER_PAGE_SIZE = 20

const { t } = useI18n()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()
const supportMemberFormId = useId()
const initAdminFormId = useId()

const tenantStatusOptions = useEnumOptions('TenantStatus', TENANT_STATUS_OPTIONS)
const configStatusOptions = useEnumOptions('TenantConfigStatus', TENANT_CONFIG_STATUS_OPTIONS)
const isolationModeOptions = useEnumOptions('TenantIsolationMode', TENANT_ISOLATION_MODE_OPTIONS)
const databaseTypeOptions = useEnumOptions('TenantDatabaseType', TENANT_DATABASE_TYPE_OPTIONS)
const memberTypeOptions = useEnumOptions('TenantMemberType', MEMBER_TYPE_OPTIONS)
const inviteStatusOptions = useEnumOptions('TenantMemberInviteStatus', MEMBER_INVITE_STATUS_OPTIONS)
const validityStatusOptions = useEnumOptions('ValidityStatus', VALIDITY_STATUS_OPTIONS)

const expiredOptions = computed(() => [
  { label: t('tenant.list.yes'), value: 1 },
  { label: t('tenant.list.no'), value: 0 },
])

// ── 版本套餐：下拉选项 + 名称回显 + 上限默认值联动 ───────────────
const editions = ref<TenantEditionListItemDto[]>([])

async function loadEditions() {
  try {
    editions.value = await tenantEditionApi.enabledList()
  }
  catch {
    // 加载失败时表单退化为无选项下拉、列表回显原始 ID，不阻塞页面
    editions.value = []
  }
}

void loadEditions()

const editionOptions = computed(() =>
  editions.value.map(e => ({
    label: e.isDefault ? `${e.editionName}${t('tenant.list.edition_default_suffix')}` : e.editionName,
    value: e.basicId,
  })),
)

/** 版本名称回显：未加载到/找不到时回退原始 ID */
function editionLabel(id?: ApiId | null) {
  if (id === null || id === undefined || id === '') {
    return null
  }
  return editions.value.find(e => e.basicId === id)?.editionName ?? String(id)
}

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadTenant() {
  void schemaPageRef.value?.reload()
}

// ── 弹窗/表单状态(保留全部增删改) ───────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<TenantStatus | null>(null)
const tenantForm = ref<TenantFormModel>(createDefaultForm())
const modalTitle = computed(() => (tenantForm.value.basicId ? t('tenant.list.edit_title') : t('tenant.list.add_title')))
/** 库隔离租户先初始化数据库、再初始化管理员：创建时不填管理员 */
const isDatabaseIsolation = computed(() => tenantForm.value.isolationMode === TenantIsolationMode.Database)
const showAdminFields = computed(() => !tenantForm.value.basicId && !isDatabaseIsolation.value)

// ── 库隔离租户初始化管理员 ───────────────────────────────
const initAdminVisible = ref(false)
const initAdminLoading = ref(false)
const initAdminForm = ref<TenantAdminInitializeDto>(createDefaultInitAdminForm(''))

/** 表单当前生效版本：选中的版本；留空时为默认版本 */
const selectedEdition = computed(() => {
  const id = tenantForm.value.editionId
  if (id) {
    return editions.value.find(e => e.basicId === id) ?? null
  }
  return editions.value.find(e => e.isDefault) ?? null
})

function limitPlaceholder(value?: number | null) {
  if (!selectedEdition.value) {
    return undefined
  }
  return t('tenant.list.limit_from_edition', { value: value ?? t('tenant.list.unlimited') })
}

const userLimitPlaceholder = computed(() => limitPlaceholder(selectedEdition.value?.userLimit))
const storageLimitPlaceholder = computed(() => limitPlaceholder(selectedEdition.value?.storageLimit))

const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<TenantDetailDto | null>(null)

const memberLoading = ref(false)
const memberError = ref(false)
const members = ref<TenantMemberListItemDto[]>([])
const MEMBER_PAGE_SIZE = 10
const memberPage = ref(1)
const memberTotal = ref(0)

/**
 * 日期选择收发的是毫秒时间戳，而这几个表单字段存的是后端的时间串：在此两向换算。
 * 换算放在这一层，表单模型与提交载荷都不必跟着改类型。
 */
function timestampModel(
  read: () => DateTimeString | null | undefined,
  write: (value: DateTimeString | null) => void,
) {
  return computed<number | null>({
    get: () => {
      const raw = read()
      return raw ? new Date(raw).getTime() : null
    },
    set: (next) => {
      write(next == null ? null : (new Date(next).toISOString() as DateTimeString))
    },
  })
}
const accessStore = useAccessStore()
const canManageSupportMember = computed(() => accessStore.hasCode('tenant.list.support-member'))
const supportMemberVisible = ref(false)
const supportMemberLoading = ref(false)
const supportMemberForm = ref<SupportMemberFormModel>(createDefaultSupportMemberForm())

const tenantExpirationTs = timestampModel(
  () => tenantForm.value.expirationTime,
  (value) => { tenantForm.value.expirationTime = value },
)
const supportMemberEffectiveTs = timestampModel(
  () => supportMemberForm.value.effectiveTime,
  (value) => { supportMemberForm.value.effectiveTime = value },
)
const supportMemberExpirationTs = timestampModel(
  () => supportMemberForm.value.expirationTime,
  (value) => { supportMemberForm.value.expirationTime = value },
)
const memberUserOptions = ref<{ label: string, value: string | number }[]>([])
const memberUserLoading = ref(false)

function getTenantStatusTagType(status: TenantStatus) {
  if (status === TenantStatus.Normal) {
    return 'success'
  }
  if (status === TenantStatus.Disabled) {
    return 'danger'
  }
  return 'warning'
}

/** 套餐存储上限以 MB 表达，已用量以字节统计，比较与展示前统一换算到字节 */
const BYTES_PER_MB = 1024 * 1024

/** 配额吃紧的判定水位：到达即提示，避免用户在毫无预兆的情况下被拒 */
const QUOTA_WARNING_RATIO = 0.8

/**
 * 配额用量文本：已用 / 生效上限，上限为空时显示"不限"
 */
function quotaUsageText(used: number, limit: number | null | undefined, format: (value: number) => string) {
  const limitText = limit == null ? t('tenant.list.unlimited') : format(limit)
  return `${format(used)} / ${limitText}`
}

/**
 * 配额用量单元格：超限标红、逼近上限标黄，其余为普通文本
 */
function renderQuotaUsage(used: number, limit: number | null | undefined, format: (value: number) => string) {
  const text = quotaUsageText(used, limit, format)
  if (limit == null) {
    return h('span', {}, text)
  }

  const ratio = limit <= 0 ? 1 : used / limit
  if (ratio >= 1) {
    return h(XhTagRoot, { variant: 'subtle', tone: 'danger' }, () => h(XhTagLabel, () => text))
  }
  if (ratio >= QUOTA_WARNING_RATIO) {
    return h(XhTagRoot, { variant: 'subtle', tone: 'warning' }, () => h(XhTagLabel, () => text))
  }
  return h('span', {}, text)
}

/** 席位用量文本（详情抽屉用） */
function seatUsageText(used: number, limit?: number | null) {
  return quotaUsageText(used, limit, value => String(value))
}

/** 存储用量文本（详情抽屉用；上限来自套餐、单位 MB） */
function storageUsageText(usedBytes: number, limitMb?: number | null) {
  return quotaUsageText(usedBytes, limitMb == null ? null : limitMb * BYTES_PER_MB, formatFileSize)
}

// ── 字段单一事实源:列 + 常用搜索 + 高级搜索 ─────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索(不作为列)
  { key: 'keyword', title: t('tenant.list.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('tenant.list.keyword_placeholder'), width: 250, order: 0 },
  // 常用搜索 + 列
  {
    key: 'tenantName',
    title: t('tenant.list.tenant_name'),
    dataType: 'string',
    sortable: true,
    minWidth: 160,
    order: 1,
  },
  { key: 'tenantCode', title: t('tenant.list.tenant_code'), dataType: 'string', sortable: true, minWidth: 150, order: 2 },
  { key: 'tenantShortName', title: t('tenant.list.tenant_short_name'), dataType: 'string', sortable: true, minWidth: 130, order: 3 },
  { key: 'domain', title: t('tenant.list.domain'), dataType: 'string', sortable: true, minWidth: 180, order: 4 },
  {
    key: 'isolationMode',
    title: t('tenant.list.isolation_mode'),
    dataType: 'enum',
    dictionaryCode: 'TenantIsolationMode',
    options: isolationModeOptions.value,
    minWidth: 120,
    order: 5,
  },
  {
    key: 'editionId',
    title: t('tenant.list.edition'),
    dataType: 'string',
    minWidth: 100,
    order: 6,
    render: (row) => {
      const r = row as unknown as TenantListItemDto
      return editionLabel(r.editionId) ?? '-'
    },
  },
  {
    key: 'tenantStatus',
    title: t('tenant.list.tenant_status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'TenantStatus',
    options: tenantStatusOptions.value,
    searchPlaceholder: t('tenant.list.tenant_status_placeholder'),
    width: 100,
    order: 7,
    render: (row) => {
      const r = row as unknown as TenantListItemDto
      return h(XhTagRoot, { variant: 'subtle', tone: getTenantStatusTagType(r.tenantStatus) }, () => h(XhTagLabel, () => getOptionLabel(tenantStatusOptions.value, r.tenantStatus)))
    },
  },
  {
    key: 'configStatus',
    title: t('tenant.list.config_status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'TenantConfigStatus',
    options: configStatusOptions.value,
    searchPlaceholder: t('tenant.list.config_status_placeholder'),
    minWidth: 110,
    order: 8,
  },
  {
    key: 'isExpired',
    title: t('tenant.list.is_expired'),
    dataType: 'boolean',
    options: expiredOptions.value,
    width: 82,
    order: 9,
    render: (row) => {
      const r = row as unknown as TenantListItemDto
      return h(XhTagRoot, { variant: 'subtle', tone: r.isExpired ? 'danger' : 'success' }, () => h(XhTagLabel, () => (r.isExpired ? t('tenant.list.yes') : t('tenant.list.no'))))
    },
  },
  {
    key: 'userLimit',
    title: t('tenant.list.seat_usage'),
    dataType: 'number',
    sortable: true,
    minWidth: 130,
    order: 10,
    render: (row) => {
      const r = row as unknown as TenantListItemDto
      return renderQuotaUsage(r.usedUserCount, r.effectiveUserLimit, value => String(value))
    },
  },
  {
    key: 'storageLimit',
    title: t('tenant.list.storage_usage'),
    dataType: 'number',
    sortable: true,
    minWidth: 160,
    order: 11,
    render: (row) => {
      const r = row as unknown as TenantListItemDto
      return renderQuotaUsage(
        r.usedStorageBytes,
        r.effectiveStorageLimit == null ? null : r.effectiveStorageLimit * BYTES_PER_MB,
        formatFileSize,
      )
    },
  },
  { key: 'sort', title: t('tenant.list.sort'), dataType: 'number', sortable: true, minWidth: 80, order: 12 },
  { key: 'expirationTime', title: t('tenant.list.expiration_time'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, advancedSearch: true, minWidth: 170, order: 13 },
  { key: 'createdTime', title: t('tenant.list.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 14 },
  // 仅高级搜索(不作为列)：版本下拉（值仍为版本 ID）
  { key: 'editionIdFilter', title: t('tenant.list.edition'), dataType: 'enum', visible: false, advancedSearch: true, options: editionOptions.value, searchPlaceholder: t('tenant.list.edition_filter_placeholder'), order: 20 },
])

/** 过滤值辅助:trim 字符串 */
function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'platform.tenant',
  exportPermission: 'tenant.list.export',
  pageName: t('tenant.list.page_name'),
  rowKey: 'basicId',
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return tenantManagementApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 区间(expirationTime)/多选(tenantStatus/configStatus) 统一走 conditions
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: toStr(f.keyword) ?? null,
        editionId: toStr(f.editionIdFilter) ?? null,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    { key: 'create', title: t('tenant.list.add'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'quota-audit', title: t('tenant.list.quota_audit'), scope: 'page', icon: 'lucide:gauge' },
    { key: 'view', title: t('tenant.list.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('tenant.list.edit'), scope: 'row' },
    {
      key: 'initdb',
      title: t('tenant.list.init_db'),
      scope: 'row',
      icon: 'lucide:database',
      permission: 'tenant.list.initdb',
      confirm: true,
      confirmText: t('tenant.list.init_db_confirm'),
      // 仅库隔离租户可初始化独立数据库
      visible: row => (row as unknown as TenantListItemDto).isolationMode === TenantIsolationMode.Database,
    },
    {
      key: 'init-admin',
      title: t('tenant.list.init_admin'),
      scope: 'row',
      icon: 'lucide:user-plus',
      permission: 'tenant.list.init-admin',
      // 库隔离租户建好库、还没有成员时开通管理员（后端按「没有所有者」精确校验）
      visible: (row) => {
        const tenant = row as unknown as TenantListItemDto
        return tenant.isolationMode === TenantIsolationMode.Database
          && tenant.configStatus === TenantConfigStatus.Configured
          && tenant.usedUserCount === 0
      },
    },
    {
      key: 'status-enable',
      title: t('tenant.list.status_enable'),
      scope: 'row',
      type: 'success',
      icon: 'lucide:play',
      permission: 'tenant.list.status',
      confirm: true,
      confirmText: t('tenant.list.status_enable_confirm'),
      visible: row => (row as unknown as TenantListItemDto).tenantStatus !== TenantStatus.Normal,
    },
    {
      key: 'status-suspend',
      title: t('tenant.list.status_suspend'),
      scope: 'row',
      type: 'warning',
      icon: 'lucide:pause',
      permission: 'tenant.list.status',
      confirm: true,
      confirmText: t('tenant.list.status_suspend_confirm'),
      visible: row => (row as unknown as TenantListItemDto).tenantStatus !== TenantStatus.Suspended,
    },
    {
      key: 'status-disable',
      title: t('tenant.list.status_disable'),
      scope: 'row',
      type: 'error',
      icon: 'lucide:power',
      permission: 'tenant.list.status',
      confirm: true,
      confirmText: t('tenant.list.status_disable_confirm'),
      visible: row => (row as unknown as TenantListItemDto).tenantStatus !== TenantStatus.Disabled,
    },
    {
      key: 'delete',
      title: t('tenant.list.delete'),
      scope: 'row',
      type: 'error',
      icon: 'lucide:trash-2',
      permission: 'tenant.list.delete',
      confirm: true,
      confirmText: t('tenant.list.delete_confirm'),
      // 后端要求先停用或暂停。入口常显但置灰：整条消失会让人以为没有删除功能，
      // 而上面几条启停就摆在同一个菜单里，置灰即指明了先做哪一步
      disabled: row => !isTenantDeletable((row as unknown as TenantListItemDto).tenantStatus),
    },
  ],
}))

/** 租户是否可删除：后端要求先停用或暂停 */
function isTenantDeletable(status: TenantStatus) {
  return status === TenantStatus.Disabled || status === TenantStatus.Suspended
}

// ── 行/页面操作分发 ─────────────────────────────────────────────
/** 超配额租户清单（存量核对） */
const quotaAlerts = ref<TenantOverQuotaDto[]>([])
const quotaAuditVisible = ref(false)
const quotaAuditLoading = ref(false)

/**
 * 存量配额核对
 *
 * 配额拦截只作用于新增、不追溯存量，启用配额之前就已超限的租户不会被动暴露出来。
 * 列表里虽然会标红，但租户一多就得翻页找，这里一次列全。
 */
async function handleQuotaAudit() {
  quotaAuditVisible.value = true
  quotaAuditLoading.value = true
  try {
    quotaAlerts.value = await tenantManagementApi.overQuotaTenants()
  }
  catch (error) {
    quotaAlerts.value = []
    toast.danger((error as Error)?.message || t('tenant.list.quota_audit_failed'))
  }
  finally {
    quotaAuditLoading.value = false
  }
}

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as TenantListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'quota-audit':
      void handleQuotaAudit()
      break
    case 'view':
      if (row) {
        void handleView(row)
      }
      break
    case 'edit':
      if (row) {
        void handleEdit(row)
      }
      break
    case 'initdb':
      if (row) {
        void handleInitDb(row)
      }
      break
    case 'init-admin':
      if (row) {
        handleInitAdmin(row)
      }
      break
    case 'status-enable':
      if (row) {
        void handleStatusChange(row, TenantStatus.Normal)
      }
      break
    case 'status-suspend':
      if (row) {
        void handleStatusChange(row, TenantStatus.Suspended)
      }
      break
    case 'status-disable':
      if (row) {
        void handleStatusChange(row, TenantStatus.Disabled)
      }
      break
    case 'delete':
      if (row) {
        void handleDelete(row)
      }
      break
  }
}

/**
 * 行级启停
 *
 * 状态本来只能在编辑弹窗的下拉里改，而删除又要求先停用或暂停，
 * 于是「停用后删除」这条最常走的路在列表上是断的。这里把三个目标状态直接摆到行操作里。
 */
async function handleStatusChange(row: TenantListItemDto, tenantStatus: TenantStatus) {
  try {
    await tenantManagementApi.updateStatus({
      basicId: row.basicId,
      reason: t('tenant.list.status_change_reason'),
      tenantStatus,
    })
    toast.success(t('tenant.list.status_update_success'))
    reloadTenant()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('tenant.list.status_update_failed'))
  }
}

async function handleDelete(row: TenantListItemDto) {
  try {
    await tenantManagementApi.remove(row.basicId)
    toast.success(t('tenant.list.delete_success'))
    reloadTenant()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('tenant.list.delete_failed'))
  }
}

async function handleInitDb(row: TenantListItemDto) {
  try {
    await tenantManagementApi.initializeDatabase(row.basicId)
    toast.success(t('tenant.list.init_db_success'))
    reloadTenant()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('tenant.list.init_db_failed'))
  }
}

function createDefaultInitAdminForm(tenantId: ApiId): TenantAdminInitializeDto {
  return {
    adminEmail: '',
    adminPassword: '',
    adminUserName: '',
    tenantId,
  }
}

function handleInitAdmin(row: TenantListItemDto) {
  initAdminForm.value = createDefaultInitAdminForm(row.basicId)
  initAdminVisible.value = true
}

async function handleSaveInitAdmin() {
  if (!validateAdmin(initAdminForm.value)) {
    return
  }

  initAdminLoading.value = true
  try {
    await tenantManagementApi.initializeTenantAdmin({
      adminEmail: initAdminForm.value.adminEmail.trim(),
      adminPassword: initAdminForm.value.adminPassword.trim(),
      adminUserName: initAdminForm.value.adminUserName.trim(),
      tenantId: initAdminForm.value.tenantId,
    })
    toast.success(t('tenant.list.init_admin_success'))
    initAdminVisible.value = false
    reloadTenant()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('tenant.list.init_admin_failed'))
  }
  finally {
    initAdminLoading.value = false
  }
}

function createDefaultForm(): TenantFormModel {
  return {
    adminEmail: '',
    adminPassword: '',
    adminUserName: '',
    connectionString: null,
    databaseType: null,
    domain: null,
    editionId: null,
    expirationTime: null,
    isolationMode: TenantIsolationMode.Field,
    logo: null,
    remark: null,
    sort: 100,
    storageLimit: null,
    tenantCode: '',
    tenantName: '',
    tenantShortName: null,
    tenantStatus: TenantStatus.Normal,
    userLimit: null,
  }
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function formatNullable(value: unknown) {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

function formatBoolean(value?: boolean | null) {
  if (value === undefined || value === null) {
    return '-'
  }
  return value ? t('tenant.list.yes') : t('tenant.list.no')
}

function handleAdd() {
  editingStatus.value = null
  tenantForm.value = createDefaultForm()
  modalVisible.value = true
}

async function handleEdit(row: TenantListItemDto) {
  editingStatus.value = row.tenantStatus
  // 列表行不含备注，取详情回填；否则保存时会把备注覆盖为空
  let detail: TenantDetailDto | null = null
  try {
    detail = await tenantManagementApi.detail(row.basicId)
  }
  catch {
    detail = null
  }
  tenantForm.value = {
    basicId: row.basicId,
    // 管理员只在创建时开通，编辑态不展示这三项
    adminEmail: '',
    adminPassword: '',
    adminUserName: '',
    // 连接串敏感、绝不回显：编辑时留空表示保持不变
    connectionString: null,
    databaseType: detail?.databaseType ?? row.databaseType ?? null,
    domain: detail?.domain ?? row.domain ?? null,
    editionId: detail?.editionId ?? row.editionId ?? null,
    expirationTime: detail?.expirationTime ?? row.expirationTime ?? null,
    isolationMode: detail?.isolationMode ?? row.isolationMode,
    logo: detail?.logo ?? row.logo ?? null,
    remark: detail?.remark ?? null,
    sort: detail?.sort ?? row.sort,
    storageLimit: detail?.storageLimit ?? row.storageLimit ?? null,
    tenantCode: detail?.tenantCode ?? row.tenantCode,
    tenantName: detail?.tenantName ?? row.tenantName,
    tenantShortName: detail?.tenantShortName ?? row.tenantShortName ?? null,
    tenantStatus: detail?.tenantStatus ?? row.tenantStatus,
    userLimit: detail?.userLimit ?? row.userLimit ?? null,
  }
  modalVisible.value = true
}

async function handleView(row: TenantListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null
  members.value = []
  memberPage.value = 1
  memberTotal.value = 0
  try {
    currentDetail.value = await tenantManagementApi.detail(row.basicId)
    if (!currentDetail.value) {
      toast.warning(t('tenant.list.detail_not_found'))
    }
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('tenant.list.detail_load_failed'))
  }
  finally {
    detailLoading.value = false
  }
  loadMembers()
}

/**
 * 解析成员展示名：租户内覆盖名 → 真实姓名 → 昵称 → 账号。
 * displayName 是「租户内覆盖名」，绝大多数成员没设（为空），直接展示它会让整列都是 "-"、只剩一串雪花 ID。
 * 注意别把回退结果写回 displayName——「编辑资料」编辑的就是它，那会把回退名当成覆盖名存回库。
 */
function resolveMemberName(item: TenantMemberListItemDto): string | null {
  return item.displayName || item.realName || item.nickName || item.userName || null
}

async function loadMembers() {
  if (!currentDetail.value) {
    return
  }
  memberLoading.value = true
  memberError.value = false
  try {
    const result = await tenantManagementApi.members.page({
      ...createPageRequest({
        page: { pageIndex: memberPage.value, pageSize: MEMBER_PAGE_SIZE },
      }),
      // 平台查看某个租户的成员：后端切入该租户只读
      tenantId: currentDetail.value.basicId,
    })
    members.value = result.items
    memberTotal.value = result.page.totalCount
  }
  catch (error) {
    memberError.value = true
    members.value = []
    memberTotal.value = 0
    toast.danger((error as Error)?.message || t('tenant.list.member_list_load_failed'))
  }
  finally {
    memberLoading.value = false
  }
}

function handleMemberPageChange(page: number) {
  memberPage.value = page
  void loadMembers()
}

function createDefaultSupportMemberForm(): SupportMemberFormModel {
  return {
    effectiveTime: null,
    expirationTime: null,
    remark: '',
    userId: null,
  }
}

/** 支持人员入驻：只能选平台账号，后端同样校验；支持人员不占席位 */
function handleAddSupportMember() {
  supportMemberForm.value = createDefaultSupportMemberForm()
  memberUserOptions.value = []
  supportMemberVisible.value = true
  void searchMemberUsers('')
}

async function searchMemberUsers(keyword: string) {
  memberUserLoading.value = true
  try {
    const items = await userApi.select({ keyword: keyword.trim() || null, limit: MEMBER_USER_PAGE_SIZE })
    memberUserOptions.value = items.map(item => ({
      label: item.realName ? `${item.userName}（${item.realName}）` : item.userName,
      value: item.basicId as string | number,
    }))
  }
  catch {
    memberUserOptions.value = []
  }
  finally {
    memberUserLoading.value = false
  }
}

async function handleSaveSupportMember() {
  const tenant = currentDetail.value
  if (!tenant) {
    return
  }
  if (!supportMemberForm.value.userId) {
    toast.warning(t('tenant.list.validate_member_user'))
    return
  }

  supportMemberLoading.value = true
  try {
    await tenantManagementApi.members.addSupport({
      effectiveTime: supportMemberForm.value.effectiveTime,
      expirationTime: supportMemberForm.value.expirationTime,
      remark: normalizeNullable(supportMemberForm.value.remark),
      tenantId: tenant.basicId,
      userId: supportMemberForm.value.userId,
    })
    toast.success(t('tenant.list.support_member_add_success'))
    supportMemberVisible.value = false
    memberPage.value = 1
    await loadMembers()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('tenant.list.support_member_add_failed'))
  }
  finally {
    supportMemberLoading.value = false
  }
}

/** 已生效或待生效的支持人员可以移除；已撤销的不再出操作 */
function isRemovableSupportMember(item: TenantMemberListItemDto) {
  return item.memberType === TenantMemberType.PlatformAdmin && item.status === ValidityStatus.Valid
}

function handleRemoveSupportMember(item: TenantMemberListItemDto) {
  const tenant = currentDetail.value
  if (!tenant) {
    return
  }
  void dialog.confirm({
    badge: 'warning',
    title: t('tenant.list.support_member_remove_title'),
    content: t('tenant.list.support_member_remove_content', { name: resolveMemberName(item) ?? item.userId }),
    okText: t('tenant.list.support_member_remove'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await tenantManagementApi.members.removeSupport(tenant.basicId, item.basicId)
        toast.success(t('tenant.list.support_member_remove_success'))
        await loadMembers()
      }
      catch (error) {
        toast.danger((error as Error)?.message || t('tenant.list.support_member_remove_failed'))
      }
    },
  })
}

function getInviteStatusTagType(status: TenantMemberInviteStatus) {
  if (status === TenantMemberInviteStatus.Accepted) {
    return 'success'
  }
  if (status === TenantMemberInviteStatus.Pending) {
    return 'info'
  }
  if (status === TenantMemberInviteStatus.Rejected) {
    return 'danger'
  }
  if (status === TenantMemberInviteStatus.Revoked) {
    return 'warning'
  }
  return 'neutral'
}

function validateForm() {
  if (!tenantForm.value.tenantName.trim()) {
    toast.warning(t('tenant.list.validate_tenant_name'))
    return false
  }
  if (tenantForm.value.basicId) {
    return true
  }

  if (!tenantForm.value.tenantCode.trim()) {
    toast.warning(t('tenant.list.validate_tenant_code'))
    return false
  }

  // 库隔离租户在初始化数据库之后再初始化管理员
  if (isDatabaseIsolation.value) {
    return true
  }

  // 管理员是新建租户的必要组成：没有管理员的租户没有任何账号能登录
  return validateAdmin(tenantForm.value)
}

function validateAdmin(admin: Pick<TenantAdminInitializeDto, 'adminEmail' | 'adminPassword' | 'adminUserName'>) {
  const adminUserName = admin.adminUserName.trim()
  if (adminUserName.length < ADMIN_USER_NAME_MIN_LENGTH || adminUserName.length > ADMIN_USER_NAME_MAX_LENGTH) {
    toast.warning(t('tenant.list.validate_admin_user_name', { max: ADMIN_USER_NAME_MAX_LENGTH, min: ADMIN_USER_NAME_MIN_LENGTH }))
    return false
  }
  if (!EMAIL_PATTERN.test(admin.adminEmail.trim())) {
    toast.warning(t('tenant.list.validate_admin_email'))
    return false
  }
  if (admin.adminPassword.trim().length < ADMIN_PASSWORD_MIN_LENGTH) {
    toast.warning(t('tenant.list.validate_admin_password', { min: ADMIN_PASSWORD_MIN_LENGTH }))
    return false
  }

  return true
}

async function handleSubmit() {
  if (!validateForm()) {
    return
  }

  submitLoading.value = true
  try {
    if (tenantForm.value.basicId) {
      const updateInput: TenantUpdateDto = {
        basicId: tenantForm.value.basicId,
        connectionString: normalizeNullable(tenantForm.value.connectionString),
        databaseType: tenantForm.value.databaseType ?? null,
        domain: normalizeNullable(tenantForm.value.domain),
        editionId: tenantForm.value.editionId ?? null,
        expirationTime: tenantForm.value.expirationTime,
        logo: normalizeNullable(tenantForm.value.logo),
        remark: normalizeNullable(tenantForm.value.remark),
        sort: tenantForm.value.sort,
        storageLimit: tenantForm.value.storageLimit ?? null,
        tenantName: tenantForm.value.tenantName.trim(),
        tenantShortName: normalizeNullable(tenantForm.value.tenantShortName),
        userLimit: tenantForm.value.userLimit ?? null,
      }

      await tenantManagementApi.update(updateInput)

      if (editingStatus.value !== tenantForm.value.tenantStatus && tenantForm.value.tenantStatus !== undefined) {
        await tenantManagementApi.updateStatus({
          basicId: tenantForm.value.basicId,
          reason: t('tenant.list.status_change_reason'),
          tenantStatus: tenantForm.value.tenantStatus,
        })
      }
    }
    else {
      // 库隔离租户的管理员在初始化数据库之后开通，创建时不带
      const withAdmin = !isDatabaseIsolation.value
      const createInput: TenantCreateDto = {
        adminEmail: withAdmin ? tenantForm.value.adminEmail.trim() : '',
        adminPassword: withAdmin ? tenantForm.value.adminPassword.trim() : '',
        adminUserName: withAdmin ? tenantForm.value.adminUserName.trim() : '',
        connectionString: normalizeNullable(tenantForm.value.connectionString),
        databaseType: tenantForm.value.databaseType ?? null,
        domain: normalizeNullable(tenantForm.value.domain),
        editionId: tenantForm.value.editionId ?? null,
        expirationTime: tenantForm.value.expirationTime,
        isolationMode: tenantForm.value.isolationMode,
        logo: normalizeNullable(tenantForm.value.logo),
        remark: normalizeNullable(tenantForm.value.remark),
        sort: tenantForm.value.sort,
        storageLimit: tenantForm.value.storageLimit ?? null,
        tenantCode: tenantForm.value.tenantCode.trim(),
        tenantName: tenantForm.value.tenantName.trim(),
        tenantShortName: normalizeNullable(tenantForm.value.tenantShortName),
        userLimit: tenantForm.value.userLimit ?? null,
      }

      await tenantManagementApi.create(createInput)
    }

    toast.success(t('tenant.list.save_success'))
    modalVisible.value = false
    reloadTenant()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('tenant.list.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 800px">
        <XhDrawerTitle>{{ t('tenant.list.detail_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div class="xh-loading-stage" :class="{ 'is-loading': detailLoading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <XhEmptyStateRoot v-if="!detailLoading && !currentDetail" class="xh-detail-empty">
            <XhEmptyStateIndicator>
              <Icon icon="lucide:inbox" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('tenant.list.detail_empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
          <div v-else-if="currentDetail" class="xh-scroll-area" style="max-height: calc(100vh - 120px)">
            <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
            <XhTabsRoot default-value="overview" variant="line">
              <XhTabsList>
                <XhTabsTrigger value="overview">
                  {{ t('tenant.list.tab_overview') }}
                </XhTabsTrigger>
                <XhTabsTrigger value="members">
                  {{ t('tenant.list.tab_members') }}
                </XhTabsTrigger>
                <XhTabsTrigger value="config">
                  {{ t('tenant.list.tab_config') }}
                </XhTabsTrigger>
                <XhTabsIndicator />
              </XhTabsList>
              <XhTabsContent value="overview">
                <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.tenant_name') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ currentDetail.tenantName }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.tenant_code') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ currentDetail.tenantCode }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.tenant_short_name') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatNullable(currentDetail.tenantShortName) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.domain') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatNullable(currentDetail.domain) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.tenant_status') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      <XhTagRoot variant="subtle" :tone="getTenantStatusTagType(currentDetail.tenantStatus)" size="sm">
                        <XhTagLabel>
                          {{ getOptionLabel(tenantStatusOptions, currentDetail.tenantStatus) }}
                        </XhTagLabel>
                      </XhTagRoot>
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.config_status') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      <XhTagRoot variant="subtle" :tone="resolveStatusTagTone('TenantConfigStatus', currentDetail.configStatus)" size="sm">
                        <XhTagLabel>
                          {{ getOptionLabel(configStatusOptions, currentDetail.configStatus) }}
                        </XhTagLabel>
                      </XhTagRoot>
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.isolation_mode') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ getOptionLabel(isolationModeOptions, currentDetail.isolationMode) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem v-if="currentDetail.databaseType">
                    <XhDescriptionsLabel>{{ t('tenant.list.database_type') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ getOptionLabel(databaseTypeOptions, currentDetail.databaseType) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.edition') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ editionLabel(currentDetail.editionId) ?? '-' }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.seat_usage') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ seatUsageText(currentDetail.usedUserCount, currentDetail.effectiveUserLimit) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.storage_usage') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ storageUsageText(currentDetail.usedStorageBytes, currentDetail.effectiveStorageLimit) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.sort') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ currentDetail.sort }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.is_expired_value') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatBoolean(currentDetail.isExpired) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.expiration_time') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatNullableDate(currentDetail.expirationTime) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.created_time') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatNullableDate(currentDetail.createdTime) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.modified_time') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatNullableDate(currentDetail.modifiedTime) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                </XhDescriptionsRoot>
              </XhTabsContent>
              <XhTabsContent value="members">
                <XhFlex class="xh-member-toolbar" gap="sm" align="center" justify="between">
                  <span class="xh-member-hint">{{ t('tenant.list.member_readonly_hint') }}</span>
                  <XhButton v-if="canManageSupportMember" variant="subtle" size="sm" tone="brand" @click="handleAddSupportMember">
                    {{ t('tenant.list.support_member_add') }}
                  </XhButton>
                </XhFlex>
                <div class="xh-loading-stage" :class="{ 'is-loading': memberLoading }">
                  <div class="xh-loading-stage__veil">
                    <XhSpinner />
                  </div>
                  <div v-if="memberError" class="xh-detail-empty">
                    <XhEmptyStateRoot>
                      <XhEmptyStateIndicator>
                        <Icon icon="lucide:alert-circle" />
                      </XhEmptyStateIndicator>
                      <XhEmptyStateTitle>{{ t('common.messages.load_failed') }}</XhEmptyStateTitle>
                      <XhEmptyStateDescription>{{ t('tenant.list.member_load_failed') }}</XhEmptyStateDescription>
                      <XhEmptyStateAction>
                        <XhButton variant="subtle" size="sm" @click="loadMembers">
                          {{ t('tenant.list.member_retry') }}
                        </XhButton>
                      </XhEmptyStateAction>
                    </XhEmptyStateRoot>
                  </div>
                  <XhEmptyStateRoot v-else-if="!memberLoading && members.length === 0" class="xh-detail-empty">
                    <XhEmptyStateIndicator>
                      <Icon icon="lucide:inbox" />
                    </XhEmptyStateIndicator>
                    <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                    <XhEmptyStateDescription>{{ t('tenant.list.member_empty') }}</XhEmptyStateDescription>
                  </XhEmptyStateRoot>
                  <template v-else>
                    <table class="xh-detail-table">
                      <thead>
                        <tr>
                          <th>{{ t('tenant.list.member_user_id') }}</th>
                          <th>{{ t('tenant.list.member_display_name') }}</th>
                          <th>{{ t('tenant.list.member_type') }}</th>
                          <th>{{ t('tenant.list.member_invite_status') }}</th>
                          <th>{{ t('tenant.list.member_status') }}</th>
                          <th>{{ t('tenant.list.member_join_time') }}</th>
                          <th>{{ t('tenant.list.member_operation') }}</th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr v-for="item in members" :key="item.basicId">
                          <td>{{ item.userId }}</td>
                          <td>{{ formatNullable(resolveMemberName(item)) }}</td>
                          <td>
                            <XhTagRoot variant="subtle" :tone="item.memberType === TenantMemberType.Owner ? 'warning' : item.memberType === TenantMemberType.Admin ? 'brand' : 'neutral'">
                              <XhTagLabel>
                                {{ getOptionLabel(memberTypeOptions, item.memberType) }}
                              </XhTagLabel>
                            </XhTagRoot>
                          </td>
                          <td>
                            <XhTagRoot variant="subtle" :tone="getInviteStatusTagType(item.inviteStatus)">
                              <XhTagLabel>
                                {{ getOptionLabel(inviteStatusOptions, item.inviteStatus) }}
                              </XhTagLabel>
                            </XhTagRoot>
                          </td>
                          <td>
                            <XhTagRoot variant="subtle" :tone="item.status === ValidityStatus.Valid ? 'success' : 'danger'">
                              <XhTagLabel>
                                {{ getOptionLabel(validityStatusOptions, item.status) }}
                              </XhTagLabel>
                            </XhTagRoot>
                          </td>
                          <td>{{ formatNullableDate(item.createdTime) }}</td>
                          <td>
                            <XhButton
                              v-if="canManageSupportMember && isRemovableSupportMember(item)"
                              variant="subtle"
                              size="sm"
                              tone="danger"
                              @click="handleRemoveSupportMember(item)"
                            >
                              {{ t('tenant.list.support_member_remove') }}
                            </XhButton>
                          </td>
                        </tr>
                      </tbody>
                    </table>
                    <div class="xh-member-pager">
                      <SchemaPagination
                        :page="memberPage"
                        :total="memberTotal"
                        :page-size="MEMBER_PAGE_SIZE" compact
                        @update:page="handleMemberPageChange"
                      />
                    </div>
                  </template>
                </div>
              </XhTabsContent>
              <XhTabsContent value="config">
                <XhDescriptionsRoot :columns="1" variant="outline" size="sm">
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.logo') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      <XUserAvatar
                        :avatar="currentDetail.logo"
                        :name="currentDetail.tenantName"
                        :size="48"
                        :round="false"
                      />
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.domain') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatNullable(currentDetail.domain) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.created_id') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatNullable(currentDetail.createdId) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenant.list.modified_id') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatNullable(currentDetail.modifiedId) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                </XhDescriptionsRoot>
              </XhTabsContent>
            </XhTabsRoot>
          </div>
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>

    <XhDrawerRoot v-model:open="quotaAuditVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 620px">
        <XhDrawerTitle>{{ t('tenant.list.quota_audit_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div class="xh-loading-stage" :class="{ 'is-loading': quotaAuditLoading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <XhEmptyStateRoot v-if="!quotaAuditLoading && quotaAlerts.length === 0" class="xh-detail-empty">
            <XhEmptyStateIndicator>
              <Icon icon="lucide:shield-check" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('tenant.list.quota_audit_clear') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('tenant.list.quota_audit_clear_desc') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
          <div v-else class="xh-scroll-area" style="max-height: calc(100vh - 120px)">
            <p class="xh-quota-audit__hint">
              {{ t('tenant.list.quota_audit_hint', { count: quotaAlerts.length }) }}
            </p>
            <div v-for="item in quotaAlerts" :key="String(item.tenantId)" class="xh-quota-alert">
              <div class="xh-quota-alert__title">
                {{ item.tenantName }}
                <span class="xh-quota-alert__code">{{ item.tenantCode }}</span>
              </div>
              <XhFlex gap="sm">
                <XhTagRoot v-if="item.seatExceeded" variant="outline" tone="danger">
                  <XhTagLabel>
                    {{ t('tenant.list.seat_usage') }} {{ seatUsageText(item.usedUserCount, item.userLimit) }}
                  </XhTagLabel>
                </XhTagRoot>
                <XhTagRoot v-if="item.storageExceeded" variant="outline" tone="danger">
                  <XhTagLabel>
                    {{ t('tenant.list.storage_usage') }} {{ storageUsageText(item.usedStorageBytes, item.storageLimit) }}
                  </XhTagLabel>
                </XhTagRoot>
              </XhFlex>
            </div>
          </div>
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>

    <XEditModal
      v-model:show="modalVisible"
      :title="modalTitle"
      :loading="submitLoading"
      :form-id="editFormId"
    >
      <XhFormRoot
        :id="editFormId"
        v-model:values="tenantForm"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleSubmit"
      >
        <XhFormFieldGroup name="tenantName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.tenant_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="tenantForm.tenantName" clearable :placeholder="t('tenant.list.tenant_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="tenantCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.tenant_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="tenantForm.tenantCode"
                :disabled="Boolean(tenantForm.basicId)"
                clearable
                :placeholder="t('tenant.list.tenant_code_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="tenantShortName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.tenant_short_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="tenantForm.tenantShortName" clearable :placeholder="t('tenant.list.tenant_short_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="domain">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.domain') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="tenantForm.domain" clearable :placeholder="t('tenant.list.domain_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="isolationMode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.isolation_mode') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect
                v-model:value="tenantForm.isolationMode"
                :disabled="Boolean(tenantForm.basicId)"
                :options="isolationModeOptions"
              />
            </XhFieldControl>
            <XhFieldDescription v-if="tenantForm.basicId">
              {{ t('tenant.list.isolation_mode_locked') }}
            </XhFieldDescription>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <template v-if="isDatabaseIsolation">
          <XhAlertRoot v-if="!tenantForm.basicId" tone="info" class="xh-span-2">
            <XhAlertIndicator>
              <Icon icon="lucide:database" :size="16" />
            </XhAlertIndicator>
            <XhAlertContent>
              <XhAlertDescription>
                {{ t('tenant.list.database_admin_hint') }}
              </XhAlertDescription>
            </XhAlertContent>
          </XhAlertRoot>
          <XhFormFieldGroup name="databaseType">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('tenant.list.database_type') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect
                  v-model:value="tenantForm.databaseType"
                  clearable
                  :options="databaseTypeOptions"
                  :placeholder="t('tenant.list.database_type_placeholder')"
                />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="connectionString" class="xh-span-2">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('tenant.list.connection_string') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput
                  v-model:value="tenantForm.connectionString"
                  clearable
                  :placeholder="t('tenant.list.connection_string_placeholder')"
                  :rows="2"
                  type="textarea"
                />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
        </template>
        <XhFormFieldGroup name="editionId">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.edition') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect
                v-model:value="tenantForm.editionId"
                clearable
                :options="editionOptions"
                :placeholder="t('tenant.list.edition_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup v-if="showAdminFields" name="adminUserName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.admin_user_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="tenantForm.adminUserName" clearable :placeholder="t('tenant.list.admin_user_name_placeholder')" autocomplete="off" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup v-if="showAdminFields" name="adminEmail">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.admin_email') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="tenantForm.adminEmail"
                clearable
                :placeholder="t('tenant.list.admin_email_placeholder')"
                inputmode="email"
                autocomplete="off"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup v-if="showAdminFields" name="adminPassword">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.admin_password') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="tenantForm.adminPassword"
                clearable
                :placeholder="t('tenant.list.admin_password_placeholder')"
                type="password"
                autocomplete="new-password"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="userLimit">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.user_limit') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput
                v-model:value="tenantForm.userLimit"
                :min="0"
                clearable
                :placeholder="userLimitPlaceholder"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="storageLimit">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.storage_limit') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput
                v-model:value="tenantForm.storageLimit"
                :min="0"
                clearable
                :placeholder="storageLimitPlaceholder"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="tenantForm.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup v-if="tenantForm.basicId" name="tenantStatus">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.tenant_status') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="tenantForm.tenantStatus" :options="tenantStatusOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="expirationTime">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.expiration_time') }}</XhFieldLabel>
            <XhFieldControl>
              <XDatePicker
                v-model:value="tenantExpirationTs"
                clearable
                type="datetime"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="logo">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.logo') }}</XhFieldLabel>
            <XhFieldControl :as-child="false">
              <XLogoUpload v-model="tenantForm.logo" directory="tenant-logo" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="remark" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.remark') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="tenantForm.remark"
                clearable
                :placeholder="t('tenant.list.remark_placeholder')"
                :rows="3"
                type="textarea"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>

    <XEditModal
      v-model:show="supportMemberVisible"
      :title="t('tenant.list.support_member_add_title')"
      :loading="supportMemberLoading"
      :form-id="supportMemberFormId"
    >
      <XhFormRoot
        :id="supportMemberFormId"
        v-model:values="supportMemberForm"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleSaveSupportMember"
      >
        <XhFormFieldGroup name="userId" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.support_member_user') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect
                v-model:value="supportMemberForm.userId"
                clearable
                :options="memberUserOptions"
                :placeholder="t('tenant.list.support_member_user_placeholder')"
                @search="searchMemberUsers"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="effectiveTime">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.member_effective_time') }}</XhFieldLabel>
            <XhFieldControl>
              <XDatePicker
                v-model:value="supportMemberEffectiveTs"
                clearable
                type="datetime"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="expirationTime">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.member_expiration_time') }}</XhFieldLabel>
            <XhFieldControl>
              <XDatePicker
                v-model:value="supportMemberExpirationTs"
                clearable
                type="datetime"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="remark" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.support_member_reason') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="supportMemberForm.remark"
                clearable
                :placeholder="t('tenant.list.support_member_reason_placeholder')"
                :rows="2"
                type="textarea"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>

    <XEditModal
      v-model:show="initAdminVisible"
      :title="t('tenant.list.init_admin_title')"
      :loading="initAdminLoading"
      :form-id="initAdminFormId"
    >
      <XhFormRoot
        :id="initAdminFormId"
        v-model:values="initAdminForm"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleSaveInitAdmin"
      >
        <XhFormFieldGroup name="adminUserName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.admin_user_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="initAdminForm.adminUserName" clearable :placeholder="t('tenant.list.admin_user_name_placeholder')" autocomplete="off" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="adminEmail">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.admin_email') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="initAdminForm.adminEmail"
                clearable
                :placeholder="t('tenant.list.admin_email_placeholder')"
                inputmode="email"
                autocomplete="off"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="adminPassword" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.list.admin_password') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="initAdminForm.adminPassword"
                clearable
                :placeholder="t('tenant.list.admin_password_placeholder')"
                type="password"
                autocomplete="new-password"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}

.xh-member-pager {
  display: flex;
  justify-content: flex-end;
  margin-top: 12px;
}

.xh-member-toolbar {
  margin-bottom: 12px;
}

.xh-member-hint {
  font-size: var(--xh-text-caption-size);
  color: hsl(var(--muted-foreground));
}

.xh-quota-audit__hint {
  margin-bottom: 12px;
  font-size: 13px;
  color: hsl(var(--muted-foreground));
}

.xh-quota-alert {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 12px 0;
  border-bottom: 1px solid hsl(var(--border));
}

.xh-quota-alert:last-child {
  border-bottom: none;
}

.xh-quota-alert__title {
  font-size: 14px;
  font-weight: 500;
}

.xh-quota-alert__code {
  margin-left: 8px;
  font-size: 12px;
  font-weight: 400;
  color: hsl(var(--muted-foreground));
}
</style>
