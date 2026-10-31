<script lang="ts" setup>
import type { EditionOption, TenantDatabaseStatus, TenantIsolationMode, TenantMember, TenantMemberType, TenantQuota, TenantRow, TenantStatus, UsageCell } from './use-tenants'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance, XDataTableColumn } from '~/components'
import type { SelectOption } from '~/types'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerBody, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerHeader, XhDrawerRoot, XhDrawerTitle, XhEmptyStateAction, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldDescription, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSpinner, XhTabsContent, XhTabsIndicator, XhTabsList, XhTabsRoot, XhTabsTrigger, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, onMounted, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, Icon, renderTag, SchemaPage, XCombobox, XDataTable, XDatePicker, XEditModal, XInput, XNumberInput, XSelect, XTooltip } from '~/components'
import { dialog, toast } from '~/composables'
import { useAuthStore } from '~/stores'
import { formatDate } from '~/utils'
import { canInitializeAdmin, canInitializeDatabase, DATABASE_PROVIDER_NAMES, inheritedEdition, initializeTenantDatabase, isMemberExpired, isOwnerCandidate, seatUsage, storageUsage, TENANT_DATABASE_PROVIDERS, TENANT_DATABASE_STATUSES, TENANT_ISOLATION_MODES, TENANT_MEMBER_TYPES, TENANT_STATUSES, tenantsResource, tenantStatusTone, useAdminInitializer, useEditionOptions, useMemberEditor, useOwnerTransfer, useQuotaAudit, useTenantDetail, useTenantEditor, useTenantQuotas } from './use-tenants'

defineOptions({ name: 'TenancyTenantsView' })

const { t } = useI18n()
const auth = useAuthStore()
/** 用量列的数据：随每次取页一起重取；取不到只提示，列表照常显示 */
const quotas = useTenantQuotas(reason => toast.danger(t('tenancy.tenants.quota.load_failed', { reason })))
const resource = tenantsResource(quotas)
const audit = useQuotaAudit()
const editions = useEditionOptions()
/** 隔离方式与独立库的连接只在有 configure-database 时出现在弹窗里（平台侧权限） */
const canConfigureDatabase = computed(() => auth.can('tenancy.tenants.configure-database'))
const editor = useTenantEditor(() => canConfigureDatabase.value)
const admin = useAdminInitializer()
const detail = useTenantDetail(() => auth.can('tenancy.tenant-members.read'))
const memberEditor = useMemberEditor()
const ownerTransfer = useOwnerTransfer()

/** 套餐下拉要读套餐；没有这项权限时不出套餐字段（开通即缺省套餐、编辑不动套餐） */
const canReadEditions = computed(() => auth.can('tenancy.editions.read'))
const canReadMembers = computed(() => auth.can('tenancy.tenant-members.read'))
/** 添加成员要从用户里挑人：同时要能读用户 */
const canAddMember = computed(() => auth.can('tenancy.tenant-members.create') && auth.can('identity.users.read'))
const canRemoveMember = computed(() => auth.can('tenancy.tenant-members.delete'))
/** 转移所有权要从用户里挑新所有者：同时要能读用户（平台侧权限，只在平台一侧出现） */
const canTransferOwner = computed(() => auth.can('tenancy.tenants.transfer-owner') && auth.can('identity.users.read'))
/** 成员表的操作列：能移除或能「设为所有者」才出 */
const showMemberActions = computed(() => canRemoveMember.value || canTransferOwner.value)
/** 现任所有者（租户的 adminUserId）：成员表打「所有者」、不给移除 */
const ownerId = computed(() => detail.detail.value?.tenant.adminUserId ?? null)

/** 弹窗的保存钮靠这些 id 关联到各自的表单，点它才会走整表提交 */
const editFormId = useId()
const adminFormId = useId()
const memberFormId = useId()
const ownerFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

onMounted(async () => {
  if (!canReadEditions.value)
    return
  await editions.load()
  if (editions.error.value)
    toast.danger(editions.error.value)
})

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对
function statusLabel(status: TenantStatus): string {
  switch (status) {
    case 'Suspended':
      return t('tenancy.tenants.status.suspended')
    case 'Expired':
      return t('tenancy.tenants.status.expired')
    default:
      return t('tenancy.tenants.status.active')
  }
}

function memberTypeLabel(type: TenantMemberType): string {
  return type === 'Support' ? t('tenancy.tenants.members.types.support') : t('tenancy.tenants.members.types.member')
}

/** 运维支持（平台账号）与普通成员分开着色；所有者另打一个标签 */
function memberTypeTone(type: TenantMemberType) {
  return type === 'Support' ? 'info' : 'neutral'
}

/** 列表与详情里的套餐：没绑定即不受套餐限制 */
function editionLabel(id: null | string | undefined): string {
  return editions.editionName(id) ?? t('tenancy.tenants.edition_unrestricted')
}

function adminTag(row: TenantRow) {
  return row.adminUserId
    ? renderTag(t('tenancy.tenants.admin_ready'), 'success')
    : renderTag(t('tenancy.tenants.admin_none'), 'neutral')
}

// ── 独立库：隔离方式、库类型与状态的文案（枚举值是契约字面量，文案键静态写出） ──
function isolationLabel(mode: TenantIsolationMode): string {
  return mode === 'Database' ? t('tenancy.tenants.database.isolation.database') : t('tenancy.tenants.database.isolation.field')
}

/** 共库即「共库」；独立库按状态：待初始化、已就绪、初始化失败 */
function databaseStatusLabel(status: TenantDatabaseStatus): string {
  switch (status) {
    case 'Pending':
      return t('tenancy.tenants.database.status.pending')
    case 'Ready':
      return t('tenancy.tenants.database.status.ready')
    case 'Failed':
      return t('tenancy.tenants.database.status.failed')
    default:
      return t('tenancy.tenants.database.status.not_required')
  }
}

function databaseStatusTone(status: TenantDatabaseStatus) {
  switch (status) {
    case 'Pending':
      return 'warning'
    case 'Ready':
      return 'success'
    case 'Failed':
      return 'danger'
    default:
      return 'neutral'
  }
}

/** 库类型：没选即与平台库相同；共库不涉及库类型 */
function providerLabel(row: Pick<TenantRow, 'databaseProvider' | 'isolationMode'>): string {
  if (row.isolationMode !== 'Database')
    return '-'
  return row.databaseProvider ? (DATABASE_PROVIDER_NAMES[row.databaseProvider] ?? row.databaseProvider) : t('tenancy.tenants.database.provider_platform')
}

/** 列表的数据库列：状态标签，初始化失败时悬停看原因（详情抽屉里也写着） */
function databaseTag(row: TenantRow) {
  const tag = renderTag(databaseStatusLabel(row.databaseStatus), databaseStatusTone(row.databaseStatus))
  return row.databaseStatus === 'Failed' && row.databaseError
    ? h(XTooltip, { content: row.databaseError }, { default: () => tag })
    : tag
}

// ── 配额：用量单元格（4.x 同：超限标红、到 80% 标黄，其余普通文本），弹窗里上限留空时沿用套餐的值 ──
function seatCell(quota: TenantQuota | undefined): UsageCell | null {
  return quota ? seatUsage(quota, t('tenancy.tenants.quota.unlimited')) : null
}

function storageCell(quota: TenantQuota | undefined): UsageCell | null {
  return quota ? storageUsage(quota, t('tenancy.tenants.quota.unlimited')) : null
}

function renderUsage(cell: UsageCell | null) {
  if (!cell)
    return '-'
  return cell.tone === 'muted' ? cell.text : renderTag(cell.text, cell.tone)
}

const inherited = computed(() => (canReadEditions.value ? inheritedEdition(editions.editions.value, editor.form.editionId, !editor.isEdit.value) : undefined))

/** 上限的占位：沿用的套餐知道就写出它的值，没有套餐即不限，说不出来就只说沿用套餐 */
function limitPlaceholder(edition: EditionOption | null | undefined, value: (edition: EditionOption) => null | string): string {
  if (edition === undefined)
    return t('tenancy.tenants.quota.follow_edition')
  if (edition === null)
    return t('tenancy.tenants.quota.no_edition')
  return t('tenancy.tenants.quota.from_edition', { value: value(edition) ?? t('tenancy.tenants.quota.unlimited') })
}

const userLimitPlaceholder = computed(() => limitPlaceholder(inherited.value, e => (e.userLimit === null || e.userLimit === undefined ? null : String(e.userLimit))))
const storageLimitPlaceholder = computed(() => limitPlaceholder(inherited.value, e => (e.storageLimitMb === null || e.storageLimitMb === undefined ? null : t('tenancy.tenants.quota.storage_mb', { size: e.storageLimitMb }))))

const auditColumns = computed<XDataTableColumn<TenantQuota>[]>(() => [
  { key: 'tenantId', title: t('tenancy.tenants.fields.tenant_id'), minWidth: 120, ellipsis: true },
  { key: 'name', title: t('tenancy.tenants.fields.name'), minWidth: 140, ellipsis: true },
  { key: 'edition', title: t('tenancy.tenants.fields.edition'), minWidth: 110, ellipsis: true, render: quota => quota.editionName ?? t('tenancy.tenants.edition_unrestricted') },
  { key: 'seats', title: t('tenancy.tenants.fields.seat_usage'), minWidth: 120, render: quota => renderUsage(seatCell(quota)) },
  { key: 'storage', title: t('tenancy.tenants.fields.storage_usage'), minWidth: 170, render: quota => renderUsage(storageCell(quota)) },
])

const statusOptions = computed<SelectOption[]>(() => TENANT_STATUSES.map(status => ({ label: statusLabel(status), value: status })))
const isolationOptions = computed<SelectOption[]>(() => TENANT_ISOLATION_MODES.map(mode => ({ label: isolationLabel(mode), value: mode })))
/** 列与导出用的数据库状态文案 */
const databaseStatusOptions = computed<SelectOption[]>(() => TENANT_DATABASE_STATUSES.map(status => ({ label: databaseStatusLabel(status), value: status })))

/** 下拉的选项值不能为空：「与平台库相同」以占位值出现，收上来换回 null */
const SAME_AS_PLATFORM = 'platform'
const providerOptions = computed<SelectOption[]>(() => [
  { label: t('tenancy.tenants.database.provider_platform'), value: SAME_AS_PLATFORM },
  ...TENANT_DATABASE_PROVIDERS.map(provider => ({ label: DATABASE_PROVIDER_NAMES[provider] ?? provider, value: provider })),
])

function onProviderChange(value: unknown) {
  editor.form.databaseProvider = typeof value === 'string' && value !== SAME_AS_PLATFORM ? value : null
}

/** 选了独立库：显出库类型与连接串，开通后的提示改为先初始化数据库 */
const choosesDatabase = computed(() => canConfigureDatabase.value && editor.form.isolationMode === 'Database')
/** 连接串必填：选了独立库而还没配过连接（开通，或从共库改过来）；配过的留空即沿用 */
const connectionRequired = computed(() => choosesDatabase.value && !editor.hasConnection.value)
const memberTypeOptions = computed<SelectOption[]>(() => TENANT_MEMBER_TYPES.map(type => ({ label: memberTypeLabel(type), value: type })))
/** 列与导出用的套餐名 */
const editionColumnOptions = computed<SelectOption[]>(() => editions.editions.value.map(e => ({ label: e.name, value: e.id })))
/** 弹窗下拉：缺省套餐带标注（开通时留空即用它） */
const editionFormOptions = computed<SelectOption[]>(() => editions.editions.value.map(e => ({
  label: e.isDefault ? `${e.name}${t('tenancy.tenants.edition_default_suffix')}` : e.name,
  value: e.id,
})))

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；order 控顺序；可排序的只放后端白名单里的） ──
// SchemaPage 的行类型在框架边界放宽（Record）：回调里按本页的行类型取用
const asRow = (row: object) => row as TenantRow

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('tenancy.tenants.search'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('tenancy.tenants.keyword_placeholder'), order: 0 },
  { key: 'tenantId', title: t('tenancy.tenants.fields.tenant_id'), dataType: 'string', sortable: true, exportable: true, minWidth: 140, order: 1 },
  { key: 'name', title: t('tenancy.tenants.fields.name'), dataType: 'string', sortable: true, exportable: true, minWidth: 160, order: 2 },
  { key: 'status', title: t('tenancy.tenants.fields.status'), dataType: 'enum', searchable: true, exportable: true, options: statusOptions.value, render: row => renderTag(statusLabel(asRow(row).status), tenantStatusTone(asRow(row).status)), width: 90, order: 3 },
  { key: 'editionId', title: t('tenancy.tenants.fields.edition'), dataType: 'enum', exportable: true, options: editionColumnOptions.value, render: row => editionLabel(asRow(row).editionId), ellipsis: true, minWidth: 130, order: 4 },
  { key: 'adminUserId', title: t('tenancy.tenants.fields.admin'), dataType: 'string', render: row => adminTag(asRow(row)), width: 100, order: 5 },
  // 共库即「共库」，独立库按状态（4.x 的配置状态）；失败原因悬停可看
  { key: 'databaseStatus', title: t('tenancy.tenants.fields.database'), dataType: 'enum', exportable: true, options: databaseStatusOptions.value, render: row => databaseTag(asRow(row)), width: 110, order: 5.5 },
  { key: 'expiresAt', title: t('tenancy.tenants.fields.expires_at'), dataType: 'datetime', sortable: true, exportable: true, minWidth: 170, order: 6 },
  // 用量来自 tenants.quotas（有效上限 = 租户自己的 → 套餐的 → 不限），不进后端排序与导出
  { key: 'seatUsage', title: t('tenancy.tenants.fields.seat_usage'), dataType: 'string', render: row => renderUsage(seatCell(quotas.of(asRow(row).tenantId))), minWidth: 120, order: 6.2 },
  { key: 'storageUsage', title: t('tenancy.tenants.fields.storage_usage'), dataType: 'string', render: row => renderUsage(storageCell(quotas.of(asRow(row).tenantId))), minWidth: 170, order: 6.4 },
  { key: 'order', title: t('tenancy.tenants.fields.order'), dataType: 'number', sortable: true, exportable: true, width: 80, order: 7 },
  { key: 'description', title: t('tenancy.tenants.fields.description'), dataType: 'text', visible: false, exportable: true, order: 7.5 },
  { key: 'createdAt', title: t('tenancy.tenants.fields.created_at'), dataType: 'datetime', sortable: true, exportable: true, minWidth: 170, order: 8 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'tenancy.tenants',
  pageName: t('tenancy.tenants.title'),
  exportPermission: 'tenancy.tenants.read',
  batchRemovable: true,
  removePermission: 'tenancy.tenants.delete',
  // 批量启用 / 停用即恢复 / 停用租户
  statusPermission: 'tenancy.tenants.suspend',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('tenancy.tenants.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'tenancy.tenants.create' },
    { key: 'quota-audit', title: t('tenancy.tenants.actions.quota_audit'), scope: 'page', icon: 'lucide:gauge', permission: 'tenancy.tenants.read' },
    { key: 'view', title: t('common.actions.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'tenancy.tenants.update' },
    // 开通三步：独立库先初始化数据库（待初始化或失败后再试；确认框在页面里弹，初始化期间确认钮转圈）
    { key: 'initialize-database', title: t('tenancy.tenants.actions.initialize_database'), scope: 'row', icon: 'lucide:database', visible: row => canInitializeDatabase(asRow(row)), permission: 'tenancy.tenants.initialize-database' },
    // 每个租户只初始化一次：已有管理员就不再出这一项；独立库没就绪时也不出（后端拒绝 TENANCY_CONFLICT_014）
    { key: 'initialize-admin', title: t('tenancy.tenants.actions.initialize_admin'), scope: 'row', icon: 'lucide:user-plus', visible: row => canInitializeAdmin(asRow(row)), permission: 'tenancy.tenants.initialize-admin' },
    { key: 'resume', title: t('tenancy.tenants.actions.resume'), scope: 'row', tone: 'success', icon: 'lucide:play', confirm: true, confirmText: row => t('tenancy.tenants.resume_confirm', { name: asRow(row).name }), visible: row => asRow(row).status !== 'Active', permission: 'tenancy.tenants.suspend' },
    { key: 'suspend', title: t('tenancy.tenants.actions.suspend'), scope: 'row', tone: 'warning', icon: 'lucide:pause', confirm: true, confirmText: row => t('tenancy.tenants.suspend_confirm', { name: asRow(row).name }), visible: row => asRow(row).status === 'Active', permission: 'tenancy.tenants.suspend' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => deleteConfirmText(t, asRow(row).name), permission: 'tenancy.tenants.delete' },
  ],
}))

// ── 行 / 页面操作分发 ──
function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      editor.startCreate()
      break
    case 'quota-audit':
      void audit.start()
      break
    case 'view':
      if (row)
        void detail.show(row)
      break
    case 'edit':
      if (row)
        editor.startEdit(row)
      break
    case 'initialize-database':
      if (row)
        void confirmInitializeDatabase(row, t('tenancy.tenants.database.init_confirm', { name: row.name }))
      break
    case 'initialize-admin':
      if (row)
        admin.start(row)
      break
    case 'resume':
      if (row)
        void setStatus(row, true)
      break
    case 'suspend':
      if (row)
        void setStatus(row, false)
      break
    case 'delete':
      if (row)
        void removeRow(row)
      break
  }
}

async function onSubmit() {
  const creating = !editor.isEdit.value
  const saved = await editor.submit()
  if (!saved) {
    // 开通成功、配独立库失败：租户已建好（共库），弹窗转为编辑它，改正后再保存只补配库
    if (creating && editor.isEdit.value) {
      toast.warning(t('tenancy.tenants.database.configure_failed'))
      reload()
    }
    return
  }
  toast.success(t('common.messages.save_success'))
  reload()
  if (!creating)
    return
  // 开通三步（与 4.x 相同）：独立库先初始化数据库、就绪后再初始化管理员，共库直接接着初始化管理员；都可以稍后从行操作进
  if (saved.isolationMode === 'Database') {
    if (auth.can('tenancy.tenants.initialize-database'))
      void confirmInitializeDatabase(saved, t('tenancy.tenants.database.init_after_create', { name: saved.name }), true)
    return
  }
  if (auth.can('tenancy.tenants.initialize-admin'))
    admin.start(saved)
}

/**
 * 初始化独立库：确认后发起，建库与建表期间确认钮转圈；初始化失败也是正常响应，按返回的状态提示，改正连接后可再试。
 * 开通后接着来的（thenAdmin）在就绪后接着初始化管理员，等确认框收起再开。
 */
async function confirmInitializeDatabase(row: TenantRow, content: string, thenAdmin = false) {
  // 在 onOk 里写入：声明处不让类型收窄成 null
  let initialized = null as null | TenantRow
  const confirmed = await dialog.confirm({
    badge: 'warning',
    title: t('tenancy.tenants.actions.initialize_database'),
    content,
    okText: t('tenancy.tenants.actions.initialize_database'),
    cancelText: thenAdmin ? t('tenancy.tenants.database.init_later') : t('common.actions.cancel'),
    onOk: async () => {
      const outcome = await initializeTenantDatabase(row)
      reload()
      if (!outcome.ready) {
        toast.danger(t('tenancy.tenants.database.init_failed'), outcome.error ? { description: outcome.error } : undefined)
        return
      }
      toast.success(t('tenancy.tenants.database.init_success'))
      initialized = outcome.tenant
    },
  })
  if (confirmed && thenAdmin && initialized && canInitializeAdmin(initialized) && auth.can('tenancy.tenants.initialize-admin'))
    admin.start(initialized)
}

async function onAdminSubmit() {
  if (await admin.submit()) {
    toast.success(t('tenancy.tenants.admin_created'))
    reload()
  }
}

async function removeRow(row: TenantRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

async function setStatus(row: TenantRow, enabled: boolean) {
  try {
    await resource.updateStatus?.(row.id, enabled)
    toast.success(t('common.messages.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.status_failed'))
  }
}

// ── 成员 ──
function startAddMember() {
  const tenant = detail.detail.value?.tenant
  if (tenant)
    memberEditor.start(tenant.tenantId)
}

/** 用户一栏随成员类型换说法：运维支持只在平台账号里挑（4.x「支持人员入驻」的写法） */
const memberUserLabel = computed(() => (memberEditor.form.memberType === 'Support' ? t('tenancy.tenants.members.platform_user') : t('tenancy.tenants.members.user')))
const memberUserPlaceholder = computed(() => (memberEditor.form.memberType === 'Support' ? t('tenancy.tenants.members.platform_user_placeholder') : t('tenancy.tenants.members.user_placeholder')))
const memberTypeHint = computed(() => (memberEditor.form.memberType === 'Support' ? t('tenancy.tenants.members.type_hint_support') : t('tenancy.tenants.members.type_hint_member')))

async function onMemberSubmit() {
  if (await memberEditor.submit()) {
    toast.success(t('tenancy.tenants.members.added'))
    await detail.loadMembers()
  }
}

/** 到期时间：没有即长期有效 */
function memberExpiry(member: TenantMember): string {
  return member.expiresAt ? formatDate(member.expiresAt) : t('tenancy.tenants.members.never_expires')
}

// ── 所有权 ──
/** 概览里「转移所有权」不预选；成员行「设为所有者」预选该成员。候选里的成员取详情抽屉已取的（读不了成员即只在本租户用户里挑） */
function startOwnerTransfer(preset: null | string = null) {
  const current = detail.detail.value
  if (current)
    ownerTransfer.start(current.tenant, canReadMembers.value ? detail.members.value : [], preset)
}

async function onOwnerSubmit() {
  if (!await ownerTransfer.submit())
    return
  toast.success(t('tenancy.tenants.owner.success'))
  reload()
  try {
    await detail.refresh()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.load_failed'))
  }
}

function confirmRemoveMember(member: TenantMember) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('tenancy.tenants.members.remove'),
    content: t('tenancy.tenants.members.remove_confirm', { name: member.displayName || member.userName }),
    okText: t('tenancy.tenants.members.remove'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await detail.removeMember(member)
        toast.success(t('tenancy.tenants.members.removed'))
      }
      catch (error) {
        toast.danger((error as Error).message || t('common.messages.delete_failed'))
      }
    },
  })
}

function formatNullable(value: unknown): string {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatNullableDate(value: null | string | undefined): string {
  return value ? formatDate(value) : '-'
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <XhDrawerRoot v-model:open="detail.open.value" side="right">
      <XhDrawerContent style="--xh-drawer-size: 800px">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ t('tenancy.tenants.detail_title') }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body">
          <div class="xh-loading-stage" :class="{ 'is-loading': detail.loading.value }">
            <div class="xh-loading-stage__veil">
              <XhSpinner />
            </div>
            <XhEmptyStateRoot v-if="!detail.loading.value && !detail.detail.value" class="tenant-detail-empty">
              <XhEmptyStateIndicator>
                <Icon icon="lucide:alert-circle" />
              </XhEmptyStateIndicator>
              <XhEmptyStateTitle>{{ t('common.messages.load_failed') }}</XhEmptyStateTitle>
              <XhEmptyStateDescription>{{ detail.error.value }}</XhEmptyStateDescription>
            </XhEmptyStateRoot>
            <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
            <XhTabsRoot v-else-if="detail.detail.value" default-value="overview" variant="line">
              <XhTabsList>
                <XhTabsTrigger value="overview">
                  {{ t('tenancy.tenants.tabs.overview') }}
                </XhTabsTrigger>
                <XhTabsTrigger v-if="canReadMembers" value="members">
                  {{ t('tenancy.tenants.tabs.members') }}
                </XhTabsTrigger>
                <XhTabsIndicator />
              </XhTabsList>
              <XhTabsContent value="overview">
                <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.tenant_id') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ detail.detail.value.tenant.tenantId }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.name') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ detail.detail.value.tenant.name }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.status') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      <XhTagRoot variant="subtle" size="sm" :tone="tenantStatusTone(detail.detail.value.tenant.status)">
                        <XhTagLabel>{{ statusLabel(detail.detail.value.tenant.status) }}</XhTagLabel>
                      </XhTagRoot>
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.edition') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ editionLabel(detail.detail.value.tenant.editionId) }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <!-- 独立库（4.x 的隔离模式、数据库类型与配置状态）：连接串从不返回；初始化失败的原因写在状态下面 -->
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.isolation_mode') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ isolationLabel(detail.detail.value.tenant.isolationMode) }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.database_provider') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ providerLabel(detail.detail.value.tenant) }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.database_status') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      <div class="tenant-database-status">
                        <XhTagRoot variant="subtle" size="sm" :tone="databaseStatusTone(detail.detail.value.tenant.databaseStatus)">
                          <XhTagLabel>{{ databaseStatusLabel(detail.detail.value.tenant.databaseStatus) }}</XhTagLabel>
                        </XhTagRoot>
                        <span v-if="detail.detail.value.tenant.databaseStatus === 'Failed' && detail.detail.value.tenant.databaseError" class="text-caption text-fg-danger" data-testid="tenant-database-error">
                          {{ detail.detail.value.tenant.databaseError }}
                        </span>
                      </div>
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <!-- 所有者即租户的 adminUserId（初始化管理员时定下）；转移改写它，新所有者获得管理员角色 -->
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.owner') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      <div class="flex flex-wrap items-center gap-2">
                        <span>{{ detail.detail.value.adminUserName ?? t('tenancy.tenants.admin_none') }}</span>
                        <XhButton
                          v-if="canTransferOwner && detail.detail.value.tenant.adminUserId"
                          variant="ghost"
                          tone="brand"
                          size="sm"
                          data-testid="transfer-owner"
                          @click="startOwnerTransfer()"
                        >
                          <XhButtonIndicator>
                            <Icon icon="lucide:arrow-right-left" width="14" />
                          </XhButtonIndicator>
                          <XhButtonLabel>{{ t('tenancy.tenants.owner.transfer') }}</XhButtonLabel>
                        </XhButton>
                      </div>
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.user_count') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ detail.detail.value.userCount }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.seat_usage') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ seatCell(detail.detail.value.quota)?.text ?? '-' }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.storage_usage') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ storageCell(detail.detail.value.quota)?.text ?? '-' }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.expires_at') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ formatNullableDate(detail.detail.value.tenant.expiresAt) }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.order') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ detail.detail.value.tenant.order }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.created_at') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ formatDate(detail.detail.value.tenant.createdAt) }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <!-- 前面十四个单格项两两成行；说明是长文本，独占一行 -->
                  <XhDescriptionsItem :span="2">
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.description') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ formatNullable(detail.detail.value.tenant.description) }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                </XhDescriptionsRoot>
              </XhTabsContent>
              <XhTabsContent v-if="canReadMembers" value="members">
                <div class="tenant-member-toolbar">
                  <span class="text-caption text-fg-muted">{{ t('tenancy.tenants.members.hint') }}</span>
                  <XhButton v-if="canAddMember" variant="subtle" size="sm" tone="brand" @click="startAddMember()">
                    {{ t('tenancy.tenants.members.add') }}
                  </XhButton>
                </div>
                <div class="xh-loading-stage" :class="{ 'is-loading': detail.membersLoading.value }">
                  <div class="xh-loading-stage__veil">
                    <XhSpinner />
                  </div>
                  <XhEmptyStateRoot v-if="detail.membersError.value" class="tenant-detail-empty">
                    <XhEmptyStateIndicator>
                      <Icon icon="lucide:alert-circle" />
                    </XhEmptyStateIndicator>
                    <XhEmptyStateTitle>{{ t('tenancy.tenants.members.load_failed') }}</XhEmptyStateTitle>
                    <XhEmptyStateDescription>{{ detail.membersError.value }}</XhEmptyStateDescription>
                    <XhEmptyStateAction>
                      <XhButton variant="subtle" size="sm" @click="detail.loadMembers()">
                        {{ t('common.actions.refresh') }}
                      </XhButton>
                    </XhEmptyStateAction>
                  </XhEmptyStateRoot>
                  <XhEmptyStateRoot v-else-if="!detail.membersLoading.value && detail.members.value.length === 0" class="tenant-detail-empty">
                    <XhEmptyStateIndicator>
                      <Icon icon="lucide:inbox" />
                    </XhEmptyStateIndicator>
                    <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                    <XhEmptyStateDescription>{{ t('tenancy.tenants.members.empty') }}</XhEmptyStateDescription>
                  </XhEmptyStateRoot>
                  <table v-else class="xh-detail-table" data-testid="tenant-members">
                    <thead>
                      <tr>
                        <th>{{ t('tenancy.tenants.members.user_name') }}</th>
                        <th>{{ t('tenancy.tenants.members.display_name') }}</th>
                        <th>{{ t('tenancy.tenants.members.home_tenant') }}</th>
                        <th>{{ t('tenancy.tenants.members.type') }}</th>
                        <th>{{ t('tenancy.tenants.members.joined_at') }}</th>
                        <th>{{ t('tenancy.tenants.members.expires_at') }}</th>
                        <th v-if="showMemberActions">
                          {{ t('tenancy.tenants.members.operation') }}
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="member in detail.members.value" :key="member.id">
                        <td>{{ member.userName }}</td>
                        <td>{{ formatNullable(member.displayName) }}</td>
                        <td>{{ member.homeTenantId }}</td>
                        <td>
                          <div class="flex flex-wrap items-center gap-1">
                            <XhTagRoot variant="subtle" size="sm" :tone="memberTypeTone(member.memberType)">
                              <XhTagLabel>{{ memberTypeLabel(member.memberType) }}</XhTagLabel>
                            </XhTagRoot>
                            <XhTagRoot v-if="member.userId === ownerId" variant="subtle" size="sm" tone="warning">
                              <XhTagLabel>{{ t('tenancy.tenants.members.owner_tag') }}</XhTagLabel>
                            </XhTagRoot>
                          </div>
                        </td>
                        <td>{{ formatDate(member.createdAt) }}</td>
                        <td>
                          <div class="flex flex-wrap items-center gap-1">
                            <span>{{ memberExpiry(member) }}</span>
                            <XhTagRoot v-if="isMemberExpired(member)" variant="subtle" size="sm" tone="danger">
                              <XhTagLabel>{{ t('tenancy.tenants.members.expired') }}</XhTagLabel>
                            </XhTagRoot>
                          </div>
                        </td>
                        <td v-if="showMemberActions">
                          <div class="flex flex-wrap items-center gap-1">
                            <!-- 4.x 同：能接任的成员行上「设为所有者」，打开转移弹窗并预选该成员 -->
                            <XhButton v-if="canTransferOwner && isOwnerCandidate(member, ownerId)" variant="subtle" size="sm" @click="startOwnerTransfer(member.userId)">
                              {{ t('tenancy.tenants.owner.set_owner') }}
                            </XhButton>
                            <!-- 所有者的成员行后端拒绝移除（TENANCY_CONFLICT_012），先转移所有权 -->
                            <XhButton v-if="canRemoveMember && member.userId !== ownerId" variant="subtle" size="sm" tone="danger" @click="confirmRemoveMember(member)">
                              {{ t('tenancy.tenants.members.remove') }}
                            </XhButton>
                          </div>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </XhTabsContent>
            </XhTabsRoot>
          </div>
        </XhDrawerBody>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 配额核对（4.x 同）：只列超出席位或存储上限的租户；配额只拦新增，已超额的不回收 -->
    <XhDrawerRoot v-model:open="audit.open.value" side="right">
      <XhDrawerContent style="--xh-drawer-size: 760px">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ t('tenancy.tenants.quota.audit_title') }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body" data-testid="tenant-quota-audit">
          <div class="tenant-quota-audit__head">
            <span class="text-caption text-fg-muted">
              {{ audit.items.value.length > 0 ? t('tenancy.tenants.quota.audit_hint', { count: audit.items.value.length }) : t('tenancy.tenants.quota.audit_scope') }}
            </span>
            <XhButton variant="subtle" size="sm" :loading="audit.loading.value" @click="audit.load()">
              <XhButtonIndicator>
                <Icon icon="lucide:refresh-cw" width="14" />
              </XhButtonIndicator>
              <XhButtonLabel>{{ t('common.actions.refresh') }}</XhButtonLabel>
            </XhButton>
          </div>
          <div class="xh-loading-stage" :class="{ 'is-loading': audit.loading.value }">
            <div class="xh-loading-stage__veil">
              <XhSpinner />
            </div>
            <XhEmptyStateRoot v-if="audit.error.value" class="tenant-detail-empty">
              <XhEmptyStateIndicator>
                <Icon icon="lucide:alert-circle" />
              </XhEmptyStateIndicator>
              <XhEmptyStateTitle>{{ t('tenancy.tenants.quota.audit_failed') }}</XhEmptyStateTitle>
              <XhEmptyStateDescription>{{ audit.error.value }}</XhEmptyStateDescription>
            </XhEmptyStateRoot>
            <XhEmptyStateRoot v-else-if="!audit.loading.value && audit.items.value.length === 0" class="tenant-detail-empty">
              <XhEmptyStateIndicator>
                <Icon icon="lucide:shield-check" />
              </XhEmptyStateIndicator>
              <XhEmptyStateTitle>{{ t('tenancy.tenants.quota.audit_clear') }}</XhEmptyStateTitle>
              <XhEmptyStateDescription>{{ t('tenancy.tenants.quota.audit_clear_desc') }}</XhEmptyStateDescription>
            </XhEmptyStateRoot>
            <XDataTable v-else :columns="auditColumns" :data="audit.items.value" row-key="tenantId" />
          </div>
        </XhDrawerBody>
      </XhDrawerContent>
    </XhDrawerRoot>

    <XEditModal
      v-model:show="editor.open.value"
      :title="editor.isEdit.value ? t('tenancy.tenants.edit_title') : t('tenancy.tenants.create_title')"
      :loading="editor.submitting.value"
      :form-id="editFormId"
    >
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="tenant-form" @submit="onSubmit">
        <XhFieldRoot :invalid="!!editor.fieldError('tenantId')" :required="!editor.isEdit.value">
          <XhFieldLabel>{{ t('tenancy.tenants.fields.tenant_id') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.tenantId" name="tenantId" clearable autocomplete="off" :disabled="editor.isEdit.value" :placeholder="t('tenancy.tenants.tenant_id_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('tenantId')">
            {{ editor.fieldError('tenantId') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('name')" required>
          <XhFieldLabel>{{ t('tenancy.tenants.fields.name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.name" name="name" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('name')">
            {{ editor.fieldError('name') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-if="canReadEditions" :invalid="!!editor.fieldError('editionId')">
          <XhFieldLabel>{{ t('tenancy.tenants.fields.edition') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect
              v-model:value="editor.form.editionId"
              name="editionId"
              clearable
              :options="editionFormOptions"
              :placeholder="editor.isEdit.value ? t('tenancy.tenants.edition_unrestricted') : t('tenancy.tenants.edition_default')"
            />
          </XhFieldControl>
          <XhFieldDescription v-if="editor.isEdit.value">
            {{ t('tenancy.tenants.edition_hint') }}
          </XhFieldDescription>
          <XhFieldErrorText v-if="editor.fieldError('editionId')">
            {{ editor.fieldError('editionId') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('expiresAt')">
          <XhFieldLabel>{{ t('tenancy.tenants.fields.expires_at') }}</XhFieldLabel>
          <XhFieldControl>
            <XDatePicker v-model:value="editor.form.expiresAt" name="expiresAt" clearable show-time :placeholder="t('tenancy.tenants.expires_never')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('expiresAt')">
            {{ editor.fieldError('expiresAt') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <!-- 租户自己的上限覆盖套餐的；留空即沿用套餐，占位写出沿用的值（4.x 同） -->
        <XhFieldRoot :invalid="!!editor.fieldError('userLimit')">
          <XhFieldLabel>{{ t('tenancy.tenants.fields.user_limit') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.userLimit" name="userLimit" :min="0" :max="10000000" :precision="0" :placeholder="userLimitPlaceholder" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('userLimit')">
            {{ editor.fieldError('userLimit') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('storageLimitMb')">
          <XhFieldLabel>{{ t('tenancy.tenants.fields.storage_limit') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.storageLimitMb" name="storageLimitMb" :min="0" :max="1000000000" :precision="0" :placeholder="storageLimitPlaceholder" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('storageLimitMb')">
            {{ editor.fieldError('storageLimitMb') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('order')">
          <XhFieldLabel>{{ t('tenancy.tenants.fields.order') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.order" name="order" :min="0" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('order')">
            {{ editor.fieldError('order') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <!-- 隔离方式与独立库的连接（4.x 同一组字段）：存好资料后另走 configure-database；独立库就绪后只读 -->
        <template v-if="canConfigureDatabase">
          <XhFieldRoot :invalid="!!editor.fieldError('isolationMode')" :disabled="editor.databaseLocked.value">
            <XhFieldLabel>{{ t('tenancy.tenants.fields.isolation_mode') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="editor.form.isolationMode" name="isolationMode" :options="isolationOptions" />
            </XhFieldControl>
            <XhFieldDescription>
              {{ editor.databaseLocked.value ? t('tenancy.tenants.database.locked_hint') : t('tenancy.tenants.database.isolation_hint') }}
            </XhFieldDescription>
            <XhFieldErrorText v-if="editor.fieldError('isolationMode')">
              {{ editor.fieldError('isolationMode') }}
            </XhFieldErrorText>
          </XhFieldRoot>
          <template v-if="choosesDatabase">
            <XhFieldRoot :invalid="!!editor.fieldError('databaseProvider')" :disabled="editor.databaseLocked.value">
              <XhFieldLabel>{{ t('tenancy.tenants.fields.database_provider') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect :value="editor.form.databaseProvider ?? SAME_AS_PLATFORM" name="databaseProvider" :options="providerOptions" @update:value="onProviderChange" />
              </XhFieldControl>
              <XhFieldErrorText v-if="editor.fieldError('databaseProvider')">
                {{ editor.fieldError('databaseProvider') }}
              </XhFieldErrorText>
            </XhFieldRoot>
            <!-- 连接串从不回显：配过的留空即沿用 -->
            <XhFieldRoot :invalid="!!editor.fieldError('connectionString')" :required="connectionRequired" :disabled="editor.databaseLocked.value">
              <XhFieldLabel>{{ t('tenancy.tenants.fields.connection_string') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput
                  v-model:value="editor.form.connectionString"
                  name="connectionString"
                  type="password"
                  autocomplete="new-password"
                  :placeholder="editor.hasConnection.value ? t('tenancy.tenants.database.connection_keep_placeholder') : t('tenancy.tenants.database.connection_placeholder')"
                />
              </XhFieldControl>
              <XhFieldErrorText v-if="editor.fieldError('connectionString')">
                {{ editor.fieldError('connectionString') }}
              </XhFieldErrorText>
            </XhFieldRoot>
          </template>
        </template>
        <XhFieldRoot class="xh-span-2" :invalid="!!editor.fieldError('description')">
          <XhFieldLabel>{{ t('tenancy.tenants.fields.description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.description" name="description" type="textarea" :rows="3" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('description')">
            {{ editor.fieldError('description') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <!-- 开通三步：独立库先初始化数据库，再初始化管理员（4.x 同） -->
        <p v-if="!editor.isEdit.value && choosesDatabase" class="xh-span-2 text-caption text-fg-muted">
          {{ t('tenancy.tenants.database.admin_hint') }}
        </p>
        <p v-else-if="!editor.isEdit.value && auth.can('tenancy.tenants.initialize-admin')" class="xh-span-2 text-caption text-fg-muted">
          {{ t('tenancy.tenants.create_admin_hint') }}
        </p>
        <p v-if="editor.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ editor.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>

    <XEditModal
      v-model:show="admin.open.value"
      :title="t('tenancy.tenants.admin_form.title', { name: admin.tenant.value?.name ?? '' })"
      :loading="admin.submitting.value"
      :form-id="adminFormId"
    >
      <XhFormRoot :id="adminFormId" class="xh-edit-form-grid" data-testid="admin-form" @submit="onAdminSubmit">
        <XhFieldRoot :invalid="!!admin.fieldError('userName')" required>
          <XhFieldLabel>{{ t('tenancy.tenants.admin_form.user_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="admin.form.userName" name="userName" clearable autocomplete="off" :placeholder="t('tenancy.tenants.admin_form.user_name_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="admin.fieldError('userName')">
            {{ admin.fieldError('userName') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!admin.fieldError('displayName')">
          <XhFieldLabel>{{ t('tenancy.tenants.admin_form.display_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="admin.form.displayName" name="displayName" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="admin.fieldError('displayName')">
            {{ admin.fieldError('displayName') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!admin.fieldError('email')">
          <XhFieldLabel>{{ t('tenancy.tenants.admin_form.email') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="admin.form.email" name="email" type="email" clearable autocomplete="off" />
          </XhFieldControl>
          <XhFieldErrorText v-if="admin.fieldError('email')">
            {{ admin.fieldError('email') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!admin.fieldError('password')" required>
          <XhFieldLabel>{{ t('tenancy.tenants.admin_form.password') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="admin.form.password" name="password" type="password" autocomplete="new-password" :placeholder="t('tenancy.tenants.admin_form.password_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="admin.fieldError('password')">
            {{ admin.fieldError('password') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <p v-if="admin.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ admin.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>

    <XEditModal
      v-model:show="memberEditor.open.value"
      :title="t('tenancy.tenants.members.add_title', { name: detail.detail.value?.tenant.name ?? '' })"
      :loading="memberEditor.submitting.value"
      :save-disabled="!memberEditor.form.userId"
      :form-id="memberFormId"
    >
      <XhFormRoot :id="memberFormId" class="xh-edit-form-grid" data-testid="member-form" @submit="onMemberSubmit">
        <!-- 先定类型：用户候选按类型分平台账号与租户用户，换类型即清掉已选的用户重新检索 -->
        <XhFieldRoot class="xh-span-2" :invalid="!!memberEditor.fieldError('memberType')" required>
          <XhFieldLabel>{{ t('tenancy.tenants.members.type') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect :value="memberEditor.form.memberType" name="memberType" :options="memberTypeOptions" @update:value="memberEditor.changeMemberType" />
          </XhFieldControl>
          <XhFieldDescription>
            {{ memberTypeHint }}
          </XhFieldDescription>
          <XhFieldErrorText v-if="memberEditor.fieldError('memberType')">
            {{ memberEditor.fieldError('memberType') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" :invalid="!!(memberEditor.fieldError('userId') || memberEditor.userError.value)" required>
          <XhFieldLabel>{{ memberUserLabel }}</XhFieldLabel>
          <XhFieldControl>
            <XCombobox
              v-model:value="memberEditor.form.userId"
              name="userId"
              remote
              clearable
              :loading="memberEditor.userLoading.value"
              :options="memberEditor.userOptions.value"
              :placeholder="memberUserPlaceholder"
              @search="memberEditor.searchUsers"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="memberEditor.fieldError('userId') || memberEditor.userError.value">
            {{ memberEditor.fieldError('userId') ?? memberEditor.userError.value }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!memberEditor.fieldError('effectiveAt')">
          <XhFieldLabel>{{ t('tenancy.tenants.members.effective_at') }}</XhFieldLabel>
          <XhFieldControl>
            <XDatePicker v-model:value="memberEditor.form.effectiveAt" name="effectiveAt" clearable show-time :placeholder="t('tenancy.tenants.members.effective_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="memberEditor.fieldError('effectiveAt')">
            {{ memberEditor.fieldError('effectiveAt') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <!-- 到期须晚于生效（TENANCY_VALIDATION_007）：选择器从生效时间起可选 -->
        <XhFieldRoot :invalid="!!memberEditor.fieldError('expiresAt')">
          <XhFieldLabel>{{ t('tenancy.tenants.members.expires_at') }}</XhFieldLabel>
          <XhFieldControl>
            <XDatePicker v-model:value="memberEditor.form.expiresAt" name="expiresAt" clearable show-time :min="memberEditor.form.effectiveAt" :placeholder="t('tenancy.tenants.members.expires_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="memberEditor.fieldError('expiresAt')">
            {{ memberEditor.fieldError('expiresAt') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <p v-if="memberEditor.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ memberEditor.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>

    <XEditModal
      v-model:show="ownerTransfer.open.value"
      :title="t('tenancy.tenants.owner.title', { name: ownerTransfer.tenant.value?.name ?? '' })"
      :loading="ownerTransfer.submitting.value"
      :save-disabled="!ownerTransfer.userId.value"
      :form-id="ownerFormId"
    >
      <XhFormRoot :id="ownerFormId" class="xh-edit-form-grid" data-testid="owner-form" @submit="onOwnerSubmit">
        <p class="xh-span-2 text-caption text-fg-muted">
          {{ t('tenancy.tenants.owner.current', { name: detail.detail.value?.adminUserName ?? '-' }) }}
        </p>
        <XhFieldRoot class="xh-span-2" :invalid="!!(ownerTransfer.fieldError('userId') || ownerTransfer.loadError.value)" required>
          <XhFieldLabel>{{ t('tenancy.tenants.owner.user') }}</XhFieldLabel>
          <XhFieldControl>
            <XCombobox
              v-model:value="ownerTransfer.userId.value"
              name="userId"
              remote
              clearable
              :loading="ownerTransfer.loading.value"
              :options="ownerTransfer.options.value"
              :placeholder="t('tenancy.tenants.owner.user_placeholder')"
              @search="ownerTransfer.search"
            />
          </XhFieldControl>
          <XhFieldDescription>
            {{ t('tenancy.tenants.owner.hint') }}
          </XhFieldDescription>
          <XhFieldErrorText v-if="ownerTransfer.fieldError('userId') || ownerTransfer.loadError.value">
            {{ ownerTransfer.fieldError('userId') ?? ownerTransfer.loadError.value }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <p v-if="ownerTransfer.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ ownerTransfer.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>

<style scoped>
.tenant-detail-empty {
  padding-block: var(--xh-space-8);
}

.tenant-quota-audit__head {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-2);
  align-items: center;
  justify-content: space-between;
  margin-block-end: var(--xh-space-3);
}

.tenant-database-status {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--xh-space-1);
}

.tenant-member-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--xh-space-2);
  margin-block-end: var(--xh-space-3);
}
</style>
