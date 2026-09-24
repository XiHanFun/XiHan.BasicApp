<script setup lang="ts">
import type { DataScopeDraft } from '../components/data-scope'
import type {
  ApiId,
  PageResult,
  PermissionListItemDto,
  RoleSelectItemDto,
  UserCreateDto,
  UserListItemDto,
  UserManagementDetailDto,
  UserUpdateDto,
} from '@/api'
import type { UserPermissionListItemDto } from '@/api/modules/authorization/user-permission.types'
import type { UserRoleListItemDto } from '@/api/modules/authorization/user-role.types'
import type { DepartmentTreeNodeDto } from '@/api/modules/organization/department.types'
import type { UserDepartmentListItemDto } from '@/api/modules/organization/user-department.types'
import type { GrantTransferGroup, ListFieldSchema, PageSchema, PermissionGrantItem, SchemaActionPayload, SchemaQueryParams } from '~/components'
import { XhAlertContent, XhAlertDescription, XhAlertIndicator, XhAlertRoot, XhButton, XhClipboardControl, XhClipboardCopyTrigger, XhClipboardIndicator, XhClipboardInput, XhClipboardLabel, XhClipboardRoot, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFlex, XhFormRoot, XhSpinner, XhSwitch, XhTabsContent, XhTabsIndicator, XhTabsList, XhTabsRoot, XhTabsTrigger, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, onMounted, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  EnableStatus,
  PermissionAction,
  permissionApi,
  querySortsFromSchema,
  roleApi,
  RoleType,
  SessionStatus,
  StatisticsPeriod,
  TenantMemberInviteStatus,
  TenantMemberType,
  TwoFactorMethod,
  userDataScopeApi,
  UserGender,
  userManagementApi,
  ValidityStatus,
} from '@/api'
import { GENDER_OPTIONS, ROLE_TYPE_OPTIONS, STATUS_OPTIONS } from '@/constants'
import { Icon, SchemaPage, XDatePicker, XEditModal, XGrantTransfer, XInput, XNumberInput, XPermissionTransfer, XSelect } from '~/components'
import { dialog, toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { useAuthStore, useUserStore } from '~/stores'
import { formatDate, getOptionLabel } from '~/utils'
import { isDataScopeComplete, isDataScopeDirty, toDataScopePayload } from '../components/data-scope'
import DataScopeEditor from '../components/DataScopeEditor.vue'
import { applyPermissionTransfer, diffPermissionGrants, diffRoleGrants } from './direct-grant'
import UserAvatarCell from './UserAvatarCell.vue'

defineOptions({ name: 'SystemUserPage' })

const { t } = useI18n()
const authStore = useAuthStore()
const userStore = useUserStore()
/** 数据范围是租户侧设置：平台没有成员关系 */
const isPlatformContext = computed(() => userStore.userInfo?.isPlatform ?? false)

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()

const GENDER_TAG_TYPE: Record<UserGender, 'neutral' | 'info' | 'warning'> = {
  [UserGender.Unknown]: 'neutral',
  [UserGender.Male]: 'info',
  [UserGender.Female]: 'warning',
}

interface UserFormState {
  basicId?: ApiId
  userName: string
  realName: string
  nickName: string
  avatar: string | null
  email: string
  phone: string
  gender: UserGender
  birthday: number | null
  country: string
  status: EnableStatus
  remark: string
  initialPassword: string
  isLocked: boolean
  multiLogin: boolean
  maxDev: number
  /** 外部成员：身份类字段只读，保存只提交本租户的角色与部门 */
  isExternal: boolean
}

/** 头像色板：跟随语义色，明暗主题均可用 */
const AVATAR_TONES = ['primary', 'info', 'success', 'warning', 'error'] as const

// 搜索/表单选项
const statusOptions = STATUS_OPTIONS.map(o => ({ label: o.label, value: o.value }))
const genderOptions = GENDER_OPTIONS

// 响应式枚举选项（后端本地化单一事实源，切语言自动重取；静态常量作兜底）
const genderEnumOptions = useEnumOptions('UserGender', GENDER_OPTIONS)
const statusEnumOptions = useEnumOptions('EnableStatus', statusOptions)

const showFormModal = ref(false)
const showDetModal = ref(false)
const showDelModal = ref(false)
const formTab = ref('0')
const submitLoading = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<UserManagementDetailDto | null>(null)
const delTarget = ref<{ id: ApiId, name: string } | null>(null)

const roleOptions = ref<RoleSelectItemDto[]>([])
const deptFlatOptions = ref<{ label: string, value: ApiId }[]>([])
const selRoleIds = ref<ApiId[]>([])
const existingRoles = ref<UserRoleListItemDto[]>([])
const selDeptIds = ref<ApiId[]>([])
const existingDepts = ref<UserDepartmentListItemDto[]>([])

const userForm = ref<UserFormState>(createDefaultForm())

/** 外部成员的账号由其注册地维护：资料、状态与安全设置只读 */
const identityReadonly = computed(() => userForm.value.isExternal)

const formTitle = computed(() =>
  userForm.value.basicId ? t('identity.user.form_edit_title', { name: userForm.value.userName }) : t('identity.user.form_create_title'),
)

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadList() {
  void schemaPageRef.value?.reload()
}

const detUser = computed(() => {
  const d = currentDetail.value
  if (!d)
    return null
  const u = d.user
  const sec = d.security
  const todayStat = d.statistics.find(s => s.period === StatisticsPeriod.Today) ?? d.statistics[0]
  const onlineSession = d.sessions.find(s => s.status === SessionStatus.Active)
  const badges: { label: string, cls: string, icon: string }[] = []
  if (sec) {
    badges.push(
      sec.emailVerified
        ? { label: t('identity.user.badge.email_verified'), cls: 'bdg-ok', icon: 'tabler:mail' }
        : { label: t('identity.user.badge.email_unverified'), cls: 'bdg-gray', icon: 'tabler:mail' },
    )
    badges.push(
      sec.phoneVerified
        ? { label: t('identity.user.badge.phone_verified'), cls: 'bdg-ok', icon: 'tabler:phone' }
        : { label: t('identity.user.badge.phone_unverified'), cls: 'bdg-gray', icon: 'tabler:phone' },
    )
    if (sec.twoFactorEnabled) {
      badges.push({
        label: `2FA: ${formatTwoFa(sec.twoFactorMethod)}`,
        cls: 'bdg-info',
        icon: 'tabler:shield-check',
      })
    }
    if (sec.isLocked)
      badges.push({ label: t('identity.user.badge.account_locked'), cls: 'bdg-no', icon: 'tabler:lock' })
    if (sec.failedLoginAttempts > 0) {
      badges.push({
        label: t('identity.user.badge.failed_login', { count: sec.failedLoginAttempts }),
        cls: 'bdg-warn',
        icon: 'tabler:alert-triangle',
      })
    }
  }
  if (u.isExternalMember)
    badges.push({ label: t('identity.user.badge.external_member'), cls: 'bdg-info', icon: 'tabler:building-community' })
  const inviteAccepted = d.tenantMembership?.inviteStatus === TenantMemberInviteStatus.Accepted
  if (d.tenantMembership && !inviteAccepted) {
    badges.push({ label: t('identity.user.badge.inactive'), cls: 'bdg-warn', icon: 'tabler:clock-pause' })
  }
  return {
    userName: u.userName,
    displayName: u.realName || u.nickName || u.userName,
    initials: getInitials(u),
    avatar: getAvatarStyle(u.userName),
    country: u.country ?? '—',
    gender: getOptionLabel(genderEnumOptions.value, u.gender),
    roles: d.roles.map(r => r.roleName ?? '').filter(Boolean),
    depts: d.departments.map(dep => dep.departmentName ?? '').filter(Boolean),
    remark: u.remark,
    badges,
    metrics: [
      {
        label: t('identity.user.detail.metric.login_count'),
        value: todayStat?.loginCount ?? 0,
        icon: 'tabler:login-2',
        cls: 'det-stat-primary',
      },
      {
        label: t('identity.user.detail.metric.access_count'),
        value: todayStat?.accessCount ?? 0,
        icon: 'tabler:activity',
        cls: 'det-stat-info',
      },
      {
        label: t('identity.user.detail.metric.online_time'),
        value: `${Math.round((todayStat?.onlineTime ?? 0) / 3600)}h`,
        icon: 'tabler:clock',
        cls: 'det-stat-warning',
      },
      {
        label: t('identity.user.detail.metric.current_status'),
        value: onlineSession ? t('identity.user.detail.online') : t('identity.user.detail.offline'),
        icon: onlineSession ? 'tabler:wifi' : 'tabler:wifi-off',
        cls: onlineSession ? 'det-stat-info' : 'det-stat-muted',
      },
    ],
    online: !!onlineSession,
    lastLoginIp: onlineSession?.ipAddressMasked ?? '—',
    lastLoginTime: onlineSession
      ? formatDate(onlineSession.lastActivityTime)
      : formatNullableDate(u.lastLoginTime),
    sessionLabel: onlineSession
      ? `${onlineSession.deviceName || t('identity.user.device_default')} · ${onlineSession.browser || ''}`
      : '',
  }
})

function createDefaultForm(): UserFormState {
  return {
    userName: '',
    realName: '',
    nickName: '',
    avatar: null,
    email: '',
    phone: '',
    gender: UserGender.Unknown,
    birthday: null,
    country: '',
    status: EnableStatus.Enabled,
    remark: '',
    initialPassword: '',
    isLocked: false,
    multiLogin: true,
    maxDev: 0,
    isExternal: false,
  }
}

function getAvatarStyle(name: string) {
  const tone = AVATAR_TONES[name.charCodeAt(0) % AVATAR_TONES.length]!
  return {
    bg: `color-mix(in srgb, var(--n-${tone}-color) 16%, hsl(var(--card)))`,
    fg: `var(--n-${tone}-color)`,
  }
}

function getInitials(row: {
  realName?: string | null
  nickName?: string | null
  userName: string
}) {
  const name = row.realName || row.nickName || row.userName
  return name ? name.substring(0, 2) : '?'
}

function formatTwoFa(method: number) {
  const parts: string[] = []
  if (method & TwoFactorMethod.Totp)
    parts.push('TOTP')
  if (method & TwoFactorMethod.Email)
    parts.push(t('identity.user.twofa_email'))
  if (method & TwoFactorMethod.Phone)
    parts.push(t('identity.user.twofa_phone'))
  return parts.join('+') || '—'
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '—'
}

function normalizeStr(value?: string | null) {
  const v = value?.trim()
  return v || null
}

function flattenDeptOptions(
  nodes: DepartmentTreeNodeDto[],
  depth = 0,
): { label: string, value: ApiId }[] {
  const out: { label: string, value: ApiId }[] = []
  for (const n of nodes) {
    out.push({ label: `${'　'.repeat(depth)}${n.departmentName}`, value: n.basicId })
    if (n.children?.length)
      out.push(...flattenDeptOptions(n.children, depth + 1))
  }
  return out
}

// ── 过滤值辅助：trim 字符串（gender/status 均为后端字符串枚举） ──────
function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}

/** 查询构建（resource.page 与导出快照复用）。排序：前端选择下发 conditions.sorts，后端 FLS 门控 + 默认兜底 */
function buildUserQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({
      page: { pageIndex: params.page, pageSize: params.pageSize },
      // 排序 + 区间(createdTime)/多选(status) 等通用过滤统一走 conditions
      conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
    }),
    keyword: toStr(f.keyword),
    gender: toStr(f.gender) as UserGender | undefined,
    // status 改为多选，经 conditions.filters In 下发（不再走 DTO 顶层 status 单值字段）
  }
}

// ── 字段单一事实源：列 + 常用搜索 ──────────────────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('common.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('identity.user.keyword_placeholder'), width: 240, order: 0 },
  // 头像（仅列）
  {
    key: 'avatar',
    title: t('identity.user.col_avatar'),
    dataType: 'string',
    width: 80,
    order: 1,
    render: (row) => {
      const r = row as unknown as UserListItemDto
      const c = getAvatarStyle(r.userName)
      return h(UserAvatarCell, {
        avatar: r.avatar,
        name: r.realName || r.nickName || r.userName,
        bg: c.bg,
        fg: c.fg,
        size: 40,
      })
    },
  },
  // 用户信息（仅列）
  {
    key: 'userName',
    title: t('identity.user.col_user_info'),
    dataType: 'string',
    minWidth: 180,
    order: 2,
    render: (row) => {
      const r = row as unknown as UserListItemDto
      const display = r.realName || r.nickName || r.userName
      const nickLine = r.nickName && r.nickName !== display ? r.nickName : null
      const subLine = nickLine ? `${nickLine} · @${r.userName}` : `@${r.userName}`
      return h('div', { class: 'tbl-cell-2l' }, [
        h('div', { class: 'tbl-cell-2l__primary tbl-cell-2l__primary--strong' }, [
          display,
          r.isSystemAccount ? h('span', { class: 'sys-tag' }, t('identity.user.tag_system')) : null,
          r.isExternalMember
            ? h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: 'info', class: 'ml-1' }, () => h(XhTagLabel, () => t('identity.user.tag_external')))
            : null,
        ]),
        h('div', { class: 'tbl-cell-2l__secondary' }, subLine),
      ])
    },
  },
  // 性别（常用搜索 + 列）
  {
    key: 'gender',
    title: t('identity.user.col_gender'),
    dataType: 'enum',
    searchable: true,
    dictionaryCode: 'UserGender',
    options: genderOptions,
    searchPlaceholder: t('identity.user.gender_placeholder'),
    width: 80,
    order: 3,
    render: (row) => {
      const r = row as unknown as UserListItemDto
      const label = getOptionLabel(genderEnumOptions.value, r.gender)
      return h(XhTagRoot, { variant: 'subtle', tone: GENDER_TAG_TYPE[r.gender] ?? 'neutral' }, () => h(XhTagLabel, () => label))
    },
  },
  // 地区/语言（仅列）
  {
    key: 'locale',
    title: t('identity.user.col_locale'),
    dataType: 'string',
    width: 140,
    order: 4,
    render: (row) => {
      const r = row as unknown as UserListItemDto
      return h('div', { class: 'tbl-cell-2l' }, [
        h('div', { class: 'tbl-cell-2l__primary' }, r.country || '—'),
      ])
    },
  },
  // 账号状态（常用搜索 + 列）
  {
    key: 'status',
    title: t('identity.user.col_status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    dictionaryCode: 'EnableStatus',
    options: statusOptions,
    searchPlaceholder: t('identity.user.status_placeholder'),
    width: 100,
    order: 5,
    render: (row) => {
      const r = row as unknown as UserListItemDto
      return h(XhTagRoot, { variant: 'subtle', tone: r.status === EnableStatus.Enabled ? 'success' : 'danger' }, () => h(XhTagLabel, () => (r.status === EnableStatus.Enabled ? t('identity.user.status_enabled') : t('identity.user.status_disabled'))))
    },
  },
  // 角色（仅列，来自后端批量聚合 roleNames）
  {
    key: 'roleNames',
    title: t('identity.user.col_roles'),
    dataType: 'string',
    minWidth: 160,
    order: 5.1,
    render: (row) => {
      const r = row as unknown as UserListItemDto
      const names = r.roleNames ?? []
      if (names.length === 0) {
        return h('span', { class: 'text-foreground/40' }, '—')
      }
      return h('div', { class: 'flex flex-wrap gap-1' }, names.map(name =>
        h(XhTagRoot, { variant: 'subtle', tone: 'info' }, () => h(XhTagLabel, () => name))))
    },
  },
  // 部门（仅列，主部门名称）
  {
    key: 'departmentName',
    title: t('identity.user.col_department'),
    dataType: 'string',
    minWidth: 120,
    order: 5.2,
    // 与角色列同款标签，但取中性色以示区分（角色为 info）
    render: (row) => {
      const r = row as unknown as UserListItemDto
      return r.departmentName
        ? h(XhTagRoot, { variant: 'subtle', tone: 'neutral' }, () => h(XhTagLabel, () => r.departmentName))
        : h('span', { class: 'text-foreground/40' }, '—')
    },
  },
  // 安全标记（仅列，锁定 / 双因素）
  {
    key: 'security',
    title: t('identity.user.col_security'),
    dataType: 'string',
    width: 120,
    order: 5.3,
    render: (row) => {
      const r = row as unknown as UserListItemDto
      const tags = []
      if (r.isLocked) {
        tags.push(h(XhTagRoot, { variant: 'subtle', tone: 'danger' }, () => h(XhTagLabel, () => t('identity.user.security_locked'))))
      }
      if (r.twoFactorEnabled) {
        tags.push(h(XhTagRoot, { variant: 'subtle', tone: 'success' }, () => h(XhTagLabel, () => '2FA')))
      }
      if (tags.length === 0) {
        return h('span', { class: 'text-foreground/40' }, t('identity.user.security_normal'))
      }
      return h('div', { class: 'flex flex-wrap gap-1' }, tags)
    },
  },
  // 最后登录（仅列）
  {
    key: 'lastLoginTime',
    title: t('identity.user.col_last_login'),
    dataType: 'datetime',
    minWidth: 150,
    order: 6,
    render: (row) => {
      const r = row as unknown as UserListItemDto
      return formatNullableDate(r.lastLoginTime)
    },
  },
  // 最后登录 IP（仅列）
  {
    key: 'lastLoginIp',
    title: t('identity.user.col_last_login_ip'),
    dataType: 'string',
    minWidth: 130,
    order: 6.1,
    render: (row) => {
      const r = row as unknown as UserListItemDto
      return r.lastLoginIp || h('span', { class: 'text-foreground/40' }, '—')
    },
  },
  // 创建时间（仅列）
  { key: 'createdTime', title: t('common.fields.created_time'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, width: 170, order: 7 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'system.user',
  exportPermission: 'identity.user.export',
  pageName: t('identity.user.page_name'),
  batchRemovable: true,
  removePermission: 'identity.user.delete',
  statusPermission: 'identity.user.status',
  rowKey: 'basicId',
  fields: fields.value,
  resource: {
    page: params => userManagementApi.page(buildUserQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
    remove: id => userManagementApi.delete(id),
    updateStatus: (id, enabled) => userManagementApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled }),
    export: { businessType: 'system.user', buildQuery: buildUserQuery },
  },
  actions: [
    { key: 'create', title: t('identity.user.action_create'), scope: 'page', type: 'primary', icon: 'tabler:plus', permission: 'identity.user.create' },
    { key: 'view', title: t('identity.user.action_view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('identity.user.action_edit'), scope: 'row', icon: 'lucide:pencil', permission: 'identity.user.update' },
    { key: 'grantRole', title: t('identity.user.action_grant_role'), scope: 'row', icon: 'lucide:users-round', permission: 'identity.user.grant-role' },
    { key: 'grantPermission', title: t('identity.user.action_grant_perm'), scope: 'row', icon: 'lucide:key-round', permission: 'identity.user.grant-permission' },
    { key: 'dataScope', title: t('identity.user.action_data_scope'), scope: 'row', icon: 'lucide:building-2', visible: () => !isPlatformContext.value, permission: 'identity.user.data-scope' },
    { key: 'lock', title: t('identity.user.action_lock'), scope: 'row', icon: 'lucide:lock', visible: isHomeAccountRow, permission: 'identity.user.lock' },
    { key: 'resetPassword', title: t('identity.user.action_reset_password'), scope: 'row', icon: 'lucide:key-square', visible: isHomeAccountRow, permission: 'identity.user.reset-password' },
    {
      key: 'resetOtp',
      title: t('identity.user.action_reset_otp'),
      scope: 'row',
      icon: 'lucide:shield-off',
      visible: row => isHomeAccountRow(row) && (row as unknown as UserListItemDto).twoFactorEnabled,
      permission: 'identity.user.reset-two-factor',
    },
    {
      key: 'impersonate',
      title: t('identity.user.action_impersonate'),
      scope: 'row',
      icon: 'lucide:user-round-cog',
      visible: row => canImpersonate((row as unknown as UserListItemDto)),
      permission: 'identity.user.impersonate',
    },
    { key: 'logout', title: t('identity.user.action_logout'), scope: 'row', icon: 'lucide:log-out', visible: isHomeAccountRow, permission: 'identity.user.revoke-sessions' },
    {
      key: 'delete',
      title: t('identity.user.action_delete'),
      scope: 'row',
      icon: 'lucide:trash-2',
      visible: row => isHomeAccountRow(row) && !(row as unknown as UserListItemDto).isSystemAccount,
      permission: 'identity.user.delete',
    },
  ],
}))

/** 账号级操作（锁定、重置、下线、删除）只对本上下文注册的账号；外部成员由其注册地维护 */
function isHomeAccountRow(row: Record<string, unknown>) {
  return !(row as unknown as UserListItemDto).isExternalMember
}

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as UserListItemDto | undefined
  switch (payload.key) {
    case 'create':
      openCreate()
      break
    case 'view':
      if (row)
        void openDetail(row.basicId)
      break
    case 'edit':
      if (row)
        void openEdit(row.basicId)
      break
    case 'grantRole':
      if (row)
        void openRoleGrantDrawer(row)
      break
    case 'grantPermission':
      if (row)
        void openPermGrantDrawer(row)
      break
    case 'dataScope':
      if (row)
        void openScopeDrawer(row)
      break
    case 'lock':
      if (row)
        void toggleLock(row)
      break
    case 'resetPassword':
      if (row)
        resetPassword(row)
      break
    case 'resetOtp':
      if (row)
        resetOtp(row)
      break
    case 'impersonate':
      if (row)
        impersonate(row)
      break
    case 'logout':
      if (row)
        forceLogout(row)
      break
    case 'delete':
      if (row)
        openDelete(row)
      break
  }
}

function closeModals() {
  showFormModal.value = false
  showDetModal.value = false
  showDelModal.value = false
}

function openCreate() {
  userForm.value = createDefaultForm()
  selRoleIds.value = []
  selDeptIds.value = []
  existingRoles.value = []
  existingDepts.value = []
  formTab.value = '0'
  showFormModal.value = true
}

async function loadOptions() {
  try {
    const [roles, tree] = await Promise.all([
      roleApi.enabledList({ limit: 200 }),
      userManagementApi.departments.tree({ limit: 500, onlyEnabled: true }),
    ])
    roleOptions.value = roles
    deptFlatOptions.value = flattenDeptOptions(tree)
  }
  catch {
    toast.warning(t('identity.user.msg_load_options_failed'))
  }
}

onMounted(() => {
  void loadOptions()
})

async function fillFormFromDetail(detail: UserManagementDetailDto) {
  const u = detail.user
  const sec = detail.security
  userForm.value = {
    basicId: u.basicId,
    userName: u.userName,
    realName: u.realName ?? '',
    nickName: u.nickName ?? '',
    avatar: u.avatar ?? null,
    email: u.email ?? '',
    phone: u.phone ?? '',
    gender: u.gender,
    birthday: u.birthday ? new Date(u.birthday).getTime() : null,
    country: u.country ?? '',
    status: u.status,
    remark: u.remark ?? '',
    initialPassword: '',
    isLocked: sec?.isLocked ?? false,
    multiLogin: sec?.allowMultiLogin ?? true,
    maxDev: sec?.maxLoginDevices ?? 0,
    isExternal: u.isExternalMember,
  }
  // 详情里的 roles 连撤销过、已过期的历史行一并返回；比对基准要的是当前生效的那份
  existingRoles.value = await userManagementApi.roles.list(u.basicId, true)
  // 部门归属同理：撤销过的行仍在详情里，只拿有效的做勾选与比对
  existingDepts.value = detail.departments.filter(d => d.status === ValidityStatus.Valid)
  selRoleIds.value = existingRoles.value.map(r => r.roleId)
  selDeptIds.value = existingDepts.value.map(d => d.departmentId)
}

async function openEdit(id: ApiId) {
  try {
    const detail = await userManagementApi.detailView(id)
    if (!detail) {
      toast.warning(t('identity.user.msg_user_not_found'))
      return
    }
    await fillFormFromDetail(detail)
    formTab.value = '0'
    showFormModal.value = true
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('identity.user.msg_load_user_failed'))
  }
}

async function openDetail(id: ApiId) {
  showDetModal.value = true
  detailLoading.value = true
  currentDetail.value = null
  try {
    currentDetail.value = await userManagementApi.detailView(id)
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('identity.user.msg_load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

function openDelete(row: UserListItemDto) {
  if (row.isSystemAccount)
    return
  delTarget.value = { id: row.basicId, name: row.userName }
  showDelModal.value = true
}

function togglePick(arr: ApiId[], id: ApiId) {
  const i = arr.indexOf(id)
  if (i >= 0)
    arr.splice(i, 1)
  else arr.push(id)
}

/**
 * 表单里的角色勾选一次提交：与当前生效的角色比出差量，交后端单事务落地。
 * 只在可选角色范围内比对——不在列表里的（如已停用）勾选区看不到，也就不该被顺手撤掉
 */
async function syncRoles(userId: ApiId) {
  const optionIds = new Set(roleOptions.value.map(role => role.basicId))
  const { grantRoleIds, revokeUserRoleIds } = diffRoleGrants(
    selRoleIds.value,
    existingRoles.value.filter(item => optionIds.has(item.roleId)),
  )
  if (grantRoleIds.length === 0 && revokeUserRoleIds.length === 0) {
    return
  }
  await userManagementApi.roles.batchUpdate({ userId, grantRoleIds, revokeUserRoleIds })
}

/**
 * 表单里的部门勾选一次提交：与现有归属比出差量，交后端单事务落地。
 * 主部门由后端保持唯一——首个部门自动为主，撤掉主部门时由最早的归属接任。
 * 只在可选部门范围内比对，不在列表里的归属勾选区看不到，也就不该被顺手撤掉
 */
async function syncDepartments(userId: ApiId) {
  const optionIds = new Set(deptFlatOptions.value.map(d => d.value))
  const current = existingDepts.value.filter(d => optionIds.has(d.departmentId))
  const selected = new Set(selDeptIds.value)
  const boundIds = new Set(current.map(d => d.departmentId))
  const assigns = selDeptIds.value
    .filter(id => optionIds.has(id) && !boundIds.has(id))
    .map(departmentId => ({ departmentId, isMain: false }))
  const revokeUserDepartmentIds = current.filter(d => !selected.has(d.departmentId)).map(d => d.basicId)
  if (assigns.length === 0 && revokeUserDepartmentIds.length === 0) {
    return
  }
  await userManagementApi.userDepartments.batchUpdate({ userId, assigns, revokeUserDepartmentIds })
}

async function saveUser() {
  const form = userForm.value
  if (!form.userName.trim()) {
    toast.warning(t('identity.user.msg_username_required'))
    formTab.value = '0'
    return
  }
  if (!form.basicId && !form.initialPassword.trim()) {
    toast.warning(t('identity.user.msg_initial_password_required'))
    formTab.value = '0'
    return
  }
  submitLoading.value = true
  try {
    // 外部成员：账号资料、状态与安全设置由注册地维护，这里只提交本租户的角色与部门
    if (form.isExternal && form.basicId) {
      await syncRoles(form.basicId)
      await syncDepartments(form.basicId)
      toast.success(t('common.messages.save_success'))
      closeModals()
      reloadList()
      return
    }

    let userId = form.basicId
    if (userId) {
      const updateInput: UserUpdateDto = {
        basicId: userId,
        avatar: form.avatar,
        birthday: form.birthday ? new Date(form.birthday).toISOString() : null,
        country: normalizeStr(form.country),
        email: normalizeStr(form.email),
        gender: form.gender,
        nickName: normalizeStr(form.nickName),
        phone: normalizeStr(form.phone),
        realName: normalizeStr(form.realName),
        remark: normalizeStr(form.remark),
      }
      await userManagementApi.update(updateInput)
      if (form.status !== undefined) {
        await userManagementApi.updateStatus({ basicId: userId, status: form.status })
      }
    }
    else {
      const createInput: UserCreateDto = {
        userName: form.userName.trim(),
        initialPassword: form.initialPassword,
        realName: normalizeStr(form.realName),
        nickName: normalizeStr(form.nickName),
        email: normalizeStr(form.email),
        phone: normalizeStr(form.phone),
        gender: form.gender,
        birthday: form.birthday ? new Date(form.birthday).toISOString() : null,
        status: form.status,
        country: normalizeStr(form.country),
        memberType: TenantMemberType.Member,
        remark: normalizeStr(form.remark),
        avatar: null,
        displayName: null,
        effectiveTime: null,
        expirationTime: null,
        inviteRemark: null,
      }
      const created = await userManagementApi.create(createInput)
      userId = created.basicId
    }

    if (userId) {
      await userManagementApi.security.updateLock({
        userId,
        isLocked: form.isLocked,
        lockoutEndTime: null,
      })
      await userManagementApi.security.updateLoginPolicy({
        userId,
        allowMultiLogin: form.multiLogin,
        maxLoginDevices: form.maxDev || 0,
      })
      await syncRoles(userId)
      await syncDepartments(userId)
    }

    toast.success(t('common.messages.save_success'))
    closeModals()
    reloadList()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

async function toggleLock(row: UserListItemDto) {
  try {
    const detail = await userManagementApi.detailView(row.basicId)
    const locked = detail?.security?.isLocked ?? false
    await userManagementApi.security.updateLock({
      userId: row.basicId,
      isLocked: !locked,
      lockoutEndTime: null,
    })
    toast.success(locked ? t('identity.user.msg_account_unlocked') : t('identity.user.msg_account_locked'))
    reloadList()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('common.messages.operation_failed'))
  }
}

function displayName(row: UserListItemDto): string {
  return row.nickName || row.userName
}

/** 行操作是否出现：服务端下发的能力位为真、目标已启用、且不是自己 */
function canImpersonate(row: UserListItemDto) {
  return userStore.userInfo?.canImpersonate === true
    && row.status === EnableStatus.Enabled
    && String(row.basicId) !== userStore.userInfo?.basicId
}

function impersonate(row: UserListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'warning',
    title: t('identity.user.impersonate_title'),
    content: t('identity.user.impersonate_content', { name: displayName(row) }),
    okText: t('identity.user.impersonate_confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await authStore.startImpersonation({ targetUserId: String(row.basicId) })
      }
      catch (error) {
        toast.danger((error as Error)?.message || t('identity.user.impersonate_failed'))
      }
    },
  })
}
function forceLogout(row: UserListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('identity.user.logout_title'),
    content: t('identity.user.logout_content', { name: displayName(row) }),
    okText: t('identity.user.logout_confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await userManagementApi.sessions.revokeUserSessions({
          userId: row.basicId,
          reason: t('identity.user.logout_reason'),
        })
        toast.success(t('identity.user.logout_done'))
        reloadList()
      }
      catch (error) {
        toast.danger((error as Error)?.message || t('identity.user.logout_failed'))
      }
    },
  })
}

/** 生成临时密码：大小写 + 数字 + 符号，规避易混淆字符（0O1lI） */
function generateTempPassword(length = 12): string {
  const upper = 'ABCDEFGHJKMNPQRSTUVWXYZ'
  const lower = 'abcdefghjkmnpqrstuvwxyz'
  const digits = '23456789'
  const symbols = '!@#$%&*'
  const all = upper + lower + digits + symbols
  const buf = new Uint32Array(length)
  crypto.getRandomValues(buf)
  const pick = (set: string, seed: number) => set[seed % set.length]!
  const chars = [pick(upper, buf[0]!), pick(lower, buf[1]!), pick(digits, buf[2]!), pick(symbols, buf[3]!)]
  for (let i = chars.length; i < length; i++) {
    chars.push(pick(all, buf[i]!))
  }
  // Fisher-Yates 打乱（复用随机源）
  for (let i = chars.length - 1; i > 0; i--) {
    const j = buf[i]! % (i + 1)
    ;[chars[i], chars[j]] = [chars[j]!, chars[i]!]
  }
  return chars.join('')
}

function resetPassword(row: UserListItemDto) {
  const tempPassword = generateTempPassword()
  void dialog.confirm({
    badge: 'warning',
    title: t('identity.user.reset_password_title'),
    content: t('identity.user.reset_password_content', { name: displayName(row) }),
    okText: t('identity.user.reset_confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await userManagementApi.security.resetPassword({
          userId: row.basicId,
          newPassword: tempPassword,
          remark: t('identity.user.reset_password_reason'),
        })
        // 结果框：正文摆一个只读的新密码与复制钮，复制态由剪贴板部件自己反馈
        void dialog.success({
          title: t('identity.user.reset_password_done_title'),
          okText: t('common.actions.close'),
          content: () => [
            h(XhClipboardRoot, { value: tempPassword }, () => [
              h(XhClipboardLabel, () => t('identity.user.reset_password_done_content', { name: displayName(row) })),
              h(XhClipboardControl, null, () => [
                h(XhClipboardInput),
                h(XhClipboardCopyTrigger, { 'aria-label': t('identity.user.reset_password_copy') }, () => [
                  h(XhClipboardIndicator),
                  h(XhClipboardIndicator, { copied: true }),
                ]),
              ]),
            ]),
          ],
        })
      }
      catch (error) {
        toast.danger((error as Error)?.message || t('identity.user.reset_password_failed'))
      }
    },
  })
}

function resetOtp(row: UserListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    title: t('identity.user.reset_otp_title'),
    content: t('identity.user.reset_otp_content', { name: displayName(row) }),
    okText: t('identity.user.reset_confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await userManagementApi.security.resetTwoFactor({
          userId: row.basicId,
          remark: t('identity.user.reset_otp_reason'),
        })
        toast.success(t('identity.user.reset_otp_done'))
        reloadList()
      }
      catch (error) {
        toast.danger((error as Error)?.message || t('identity.user.reset_otp_failed'))
      }
    },
  })
}

// ── 成员数据范围抽屉（覆盖档位与自定义部门一次保存） ──────────────
const scopeVisible = ref(false)
const scopeUser = ref<UserListItemDto | null>(null)
const scopeTree = ref<DepartmentTreeNodeDto[]>([])
const scopeDraft = ref<DataScopeDraft>({ dataScope: null, departments: [] })
/** 打开时的现状，保存钮只在有改动时可用 */
const scopeOriginal = ref<DataScopeDraft>({ dataScope: null, departments: [] })
const scopeLoading = ref(false)
const scopeSubmitting = ref(false)
const scopeDirty = computed(() => isDataScopeDirty(scopeDraft.value, scopeOriginal.value))

function resetScopeDraft(draft: DataScopeDraft) {
  scopeDraft.value = draft
  scopeOriginal.value = draft
}

async function openScopeDrawer(row: UserListItemDto) {
  scopeUser.value = row
  scopeVisible.value = true
  scopeTree.value = []
  resetScopeDraft({ dataScope: null, departments: [] })
  scopeLoading.value = true
  try {
    const [tree, setting] = await Promise.all([
      userManagementApi.departments.tree({ limit: 1000, onlyEnabled: true }),
      userDataScopeApi.setting(row.basicId),
    ])
    scopeTree.value = tree
    resetScopeDraft({
      dataScope: setting.dataScope,
      departments: setting.departments.map(({ departmentId, includeChildren }) => ({ departmentId, includeChildren })),
    })
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('identity.data_scope.load_failed'))
  }
  finally {
    scopeLoading.value = false
  }
}

async function saveScopes() {
  const user = scopeUser.value
  if (!user || !scopeDirty.value || scopeSubmitting.value)
    return
  if (!isDataScopeComplete(scopeDraft.value)) {
    toast.warning(t('identity.data_scope.custom_required'))
    return
  }

  scopeSubmitting.value = true
  try {
    await userDataScopeApi.set({ userId: user.basicId, ...toDataScopePayload(scopeDraft.value) })
    toast.success(t('identity.data_scope.saved'))
    scopeVisible.value = false
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    scopeSubmitting.value = false
  }
}

// ── 角色直授抽屉（穿梭框，挪好后一次提交） ──────────────
const roleGrantVisible = ref(false)
const roleGrantUser = ref<UserListItemDto | null>(null)
const roleGrantLoading = ref(false)
/** 当前生效的角色直授（后端 onlyValid 口径，撤销过、已过期的不在其中） */
const roleGrants = ref<UserRoleListItemDto[]>([])
/** 右栏草稿：保存时与 roleGrants 比出授予与撤销的差量 */
const roleDraft = ref<ApiId[]>([])
const roleDirty = ref(false)

const roleTypeOptions = useEnumOptions('RoleType', ROLE_TYPE_OPTIONS)

/**
 * 条目为可选角色全集，再补上已授予却不在可选列表里的（例如授予后角色被停用）：
 * 否则右栏「已授角色」会漏掉它们，审阅时看不全
 */
const roleGrantItems = computed<RoleSelectItemDto[]>(() => {
  const known = new Set(roleOptions.value.map(role => role.basicId))
  const extra = roleGrants.value
    .filter(grant => !known.has(grant.roleId))
    .map(grant => ({
      basicId: grant.roleId,
      roleName: grant.roleName ?? String(grant.roleId),
      roleCode: grant.roleCode ?? '',
      roleType: grant.roleType ?? RoleType.Custom,
      isGlobal: grant.isGlobalRole ?? false,
    }))
  return [...roleOptions.value, ...extra]
})

/** 按角色类型分段，段序跟随角色列表里各类型首次出现的顺序 */
const roleGrantGroups = computed<GrantTransferGroup<RoleSelectItemDto>[]>(() => {
  const byType = new Map<string, RoleSelectItemDto[]>()
  for (const role of roleGrantItems.value) {
    const items = byType.get(role.roleType) ?? []
    items.push(role)
    byType.set(role.roleType, items)
  }
  return [...byType].map(([type, items]) => ({
    key: type,
    name: getOptionLabel(roleTypeOptions.value, type, type),
    items,
  }))
})

async function openRoleGrantDrawer(row: UserListItemDto) {
  roleGrantUser.value = row
  roleGrantVisible.value = true
  roleGrants.value = []
  roleDraft.value = []
  roleDirty.value = false
  roleGrantLoading.value = true
  try {
    roleGrants.value = await userManagementApi.roles.list(row.basicId, true)
    roleDraft.value = roleGrants.value.map(grant => grant.roleId)
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('identity.user.grant_load_failed'))
  }
  finally {
    roleGrantLoading.value = false
  }
}

function onRoleTransfer(next: ApiId[]) {
  if (roleGrantLoading.value) {
    return
  }
  roleDraft.value = next
  roleDirty.value = true
}

async function saveRoleGrants() {
  const user = roleGrantUser.value
  if (!user || roleGrantLoading.value) {
    return
  }
  const { grantRoleIds, revokeUserRoleIds } = diffRoleGrants(roleDraft.value, roleGrants.value)
  if (grantRoleIds.length === 0 && revokeUserRoleIds.length === 0) {
    toast.info(t('identity.user.grant_no_change'))
    roleDirty.value = false
    return
  }
  roleGrantLoading.value = true
  try {
    await userManagementApi.roles.batchUpdate({ userId: user.basicId, grantRoleIds, revokeUserRoleIds })
    roleGrants.value = await userManagementApi.roles.list(user.basicId, true)
    roleDraft.value = roleGrants.value.map(grant => grant.roleId)
    roleDirty.value = false
    toast.success(t('identity.user.grant_saved', { grant: grantRoleIds.length, revoke: revokeUserRoleIds.length }))
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    roleGrantLoading.value = false
  }
}

// ── 权限直授抽屉（授予 / 拒绝各一个穿梭框，两页签一次提交） ──────────────
const permGrantVisible = ref(false)
const permGrantUser = ref<UserListItemDto | null>(null)
const permGrantTab = ref<'grant' | 'deny'>('grant')
const permGrantLoading = ref(false)
/** 当前生效的权限直授（后端 onlyValid 口径） */
const permGrants = ref<UserPermissionListItemDto[]>([])
const permCatalog = ref<PermissionListItemDto[]>([])
/** 草稿：权限主键 → 直授动作；不在表里即未设置。授予与拒绝互斥，一个权限只落在一边 */
const permActions = ref<Map<ApiId, PermissionAction>>(new Map())
const permDirty = ref(false)

/**
 * 条目为权限目录，再补上已直授却不在目录里的（例如授予后权限被停用）：
 * 否则右栏会漏掉它们，保存时却仍原样保留，界面与实际对不上
 */
const permGrantItems = computed<Array<PermissionGrantItem & { basicId: ApiId }>>(() => {
  const known = new Set(permCatalog.value.map(permission => permission.basicId))
  const extra = permGrants.value
    .filter(grant => !known.has(grant.permissionId))
    .map(grant => ({
      basicId: grant.permissionId,
      permissionCode: grant.permissionCode ?? '',
      permissionName: grant.permissionName ?? String(grant.permissionId),
      moduleCode: grant.moduleCode,
    }))
  return [...permCatalog.value, ...extra]
})

function permKeysOf(action: PermissionAction): ApiId[] {
  return [...permActions.value].filter(([, value]) => value === action).map(([key]) => key)
}

const permGrantIds = computed(() => permKeysOf(PermissionAction.Grant))
const permDenyIds = computed(() => permKeysOf(PermissionAction.Deny))

async function loadPermCatalog() {
  if (permCatalog.value.length) {
    return
  }
  permCatalog.value = await permissionApi.catalog()
}

function derivePermActions() {
  permActions.value = new Map(permGrants.value.map(item => [item.permissionId, item.permissionAction] as const))
  permDirty.value = false
}

async function openPermGrantDrawer(row: UserListItemDto) {
  permGrantUser.value = row
  permGrantVisible.value = true
  permGrantTab.value = 'grant'
  permGrants.value = []
  derivePermActions()
  permGrantLoading.value = true
  try {
    const [grants] = await Promise.all([
      // 撤销直授是把行置为失效而非删行，只取有效行，否则撤销后仍显示为已直授
      userManagementApi.permissions.list(row.basicId, true),
      loadPermCatalog(),
    ])
    permGrants.value = grants
    derivePermActions()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('identity.user.grant_load_failed'))
  }
  finally {
    permGrantLoading.value = false
  }
}

function onPermTransfer(action: PermissionAction, next: ApiId[]) {
  if (permGrantLoading.value) {
    return
  }
  permActions.value = applyPermissionTransfer(permActions.value, action, next)
  permDirty.value = true
}

async function savePermGrants() {
  const user = permGrantUser.value
  if (!user || permGrantLoading.value) {
    return
  }
  const { grants, revokeUserPermissionIds } = diffPermissionGrants(permActions.value, permGrants.value)
  if (grants.length === 0 && revokeUserPermissionIds.length === 0) {
    toast.info(t('identity.user.grant_no_change'))
    permDirty.value = false
    return
  }
  permGrantLoading.value = true
  try {
    await userManagementApi.permissions.batchUpdate({ userId: user.basicId, grants, revokeUserPermissionIds })
    permGrants.value = await userManagementApi.permissions.list(user.basicId, true)
    derivePermActions()
    const denied = grants.filter(item => item.permissionAction === PermissionAction.Deny).length
    toast.success(t('identity.user.grant_perm_saved', { grant: grants.length - denied, deny: denied, revoke: revokeUserPermissionIds.length }))
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    permGrantLoading.value = false
  }
}

async function confirmDelete() {
  if (!delTarget.value)
    return
  try {
    await userManagementApi.delete(delTarget.value.id)
    toast.success(t('identity.user.msg_user_deleted'))
    closeModals()
    reloadList()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('common.messages.delete_failed'))
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <!-- 新建/编辑：统一编辑弹窗外壳 + 表单网格 -->
    <XEditModal
      v-model:show="showFormModal"
      :title="formTitle"
      :loading="submitLoading"
      :form-id="editFormId"
      @cancel="closeModals"
    >
      <XhAlertRoot v-if="identityReadonly" tone="info" class="mb-3">
        <XhAlertIndicator>
          <Icon icon="tabler:building-community" :size="16" />
        </XhAlertIndicator>
        <XhAlertContent>
          <XhAlertDescription>
            {{ t('identity.user.form_external_hint') }}
          </XhAlertDescription>
        </XhAlertContent>
      </XhAlertRoot>
      <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
      <XhTabsRoot v-model:value="formTab" variant="line">
        <XhTabsList>
          <XhTabsTrigger value="0">
            {{ t('identity.user.tab_basic') }}
          </XhTabsTrigger>
          <XhTabsTrigger value="1">
            {{ t('identity.user.tab_security') }}
          </XhTabsTrigger>
          <XhTabsTrigger value="2">
            {{ t('identity.user.tab_roles') }}
          </XhTabsTrigger>
          <XhTabsTrigger value="3">
            {{ t('identity.user.tab_departments') }}
          </XhTabsTrigger>
          <XhTabsIndicator />
        </XhTabsList>
        <XhTabsContent value="0">
          <!-- NForm 渲染真实 form 元素：密码输入必须在 form 内，否则浏览器告警 -->
          <XhFormRoot
            :id="editFormId"
            validate-on="blur"
            class="xh-edit-form-grid"
            @submit="saveUser"
          >
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.user.label_username') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput
                  v-model:value="userForm.userName"
                  :placeholder="t('identity.user.ph_username')"
                  :disabled="!!userForm.basicId"
                  autocomplete="off"
                />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.user.label_real_name') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="userForm.realName" :placeholder="t('identity.user.ph_real_name')" :disabled="identityReadonly" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.user.label_nickname') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="userForm.nickName" :placeholder="t('identity.user.ph_nickname')" :disabled="identityReadonly" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.user.label_email') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="userForm.email" :placeholder="t('identity.user.ph_email')" autocomplete="off" :disabled="identityReadonly" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.user.label_phone') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="userForm.phone" :placeholder="t('identity.user.ph_phone')" autocomplete="off" :disabled="identityReadonly" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.user.label_gender') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect v-model:value="userForm.gender" :options="genderEnumOptions" :disabled="identityReadonly" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.user.label_birthday') }}</XhFieldLabel>
              <XhFieldControl>
                <XDatePicker v-model:value="userForm.birthday" type="date" :disabled="identityReadonly" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.user.label_country') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="userForm.country" :placeholder="t('identity.user.ph_country')" :disabled="identityReadonly" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.user.label_status') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect v-model:value="userForm.status" :options="statusEnumOptions" :disabled="identityReadonly" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
            <XhFieldRoot v-if="!userForm.basicId">
              <XhFieldLabel>{{ t('identity.user.label_initial_password') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput
                  v-model:value="userForm.initialPassword"
                  type="password"
                  autocomplete="new-password"
                  :placeholder="t('identity.user.ph_initial_password')"
                />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
            <XhFieldRoot class="xh-span-2">
              <XhFieldLabel>{{ t('identity.user.label_remark') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput
                  v-model:value="userForm.remark"
                  type="textarea"
                  :rows="2"
                  :placeholder="t('identity.user.ph_remark')"
                  :disabled="identityReadonly"
                />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormRoot>
        </XhTabsContent>
        <XhTabsContent value="1">
          <div class="sec-panel">
            <div class="sec-block">
              <div class="sec-block-hd">
                <Icon icon="tabler:shield-lock" :size="14" />
                <span>{{ t('identity.user.sec_account_security') }}</span>
              </div>
              <div class="form-row">
                <div class="form-row-main">
                  <Icon icon="tabler:lock" :size="15" class="form-row-ico warn" />
                  <div>
                    <div class="lbl">
                      {{ t('identity.user.sec_account_lock') }}
                    </div>
                    <div class="sub">
                      {{ t('identity.user.sec_account_lock_hint') }}
                    </div>
                  </div>
                </div>
                <XhSwitch v-model:checked="userForm.isLocked" :disabled="identityReadonly" />
              </div>
            </div>
            <div class="sec-block">
              <div class="sec-block-hd">
                <Icon icon="tabler:devices" :size="14" />
                <span>{{ t('identity.user.sec_login_session') }}</span>
              </div>
              <div class="form-row">
                <div class="form-row-main">
                  <Icon icon="tabler:login" :size="15" class="form-row-ico ok" />
                  <div>
                    <div class="lbl">
                      {{ t('identity.user.sec_allow_multi_login') }}
                    </div>
                    <div class="sub">
                      {{ t('identity.user.sec_allow_multi_login_hint') }}
                    </div>
                  </div>
                </div>
                <XhSwitch v-model:checked="userForm.multiLogin" :disabled="identityReadonly" />
              </div>
              <div class="form-row">
                <div class="form-row-main">
                  <Icon icon="tabler:device-mobile" :size="15" class="form-row-ico" />
                  <div>
                    <div class="lbl">
                      {{ t('identity.user.sec_max_devices') }}
                    </div>
                    <div class="sub">
                      {{ t('identity.user.sec_max_devices_hint') }}
                    </div>
                  </div>
                </div>
                <XNumberInput
                  v-model:value="userForm.maxDev"
                  :min="0"
                  :max="99"
                  class="max-dev-input"
                  size="sm"
                  :show-button="false"
                  :disabled="identityReadonly"
                />
              </div>
            </div>
          </div>
        </XhTabsContent>
        <XhTabsContent value="2">
          <div class="pick-panel">
            <p class="pick-desc">
              {{ t('identity.user.pick_roles_desc') }}
            </p>
            <p v-if="selRoleIds.length" class="pick-summary">
              {{ t('identity.user.pick_selected') }}
              <strong>{{ selRoleIds.length }}</strong>
              {{ t('identity.user.pick_selected_unit') }}
            </p>
            <div class="pick-grid">
              <button
                v-for="r in roleOptions"
                :key="r.basicId"
                type="button"
                class="pick-chip" :class="[selRoleIds.includes(r.basicId) ? 'on' : '']"
                @click="togglePick(selRoleIds, r.basicId)"
              >
                <Icon icon="tabler:user-check" :size="13" />
                {{ r.roleName }}
              </button>
            </div>
          </div>
        </XhTabsContent>
        <XhTabsContent value="3">
          <div class="pick-panel">
            <p class="pick-desc">
              {{ t('identity.user.pick_depts_desc') }}
            </p>
            <p v-if="selDeptIds.length" class="pick-summary">
              {{ t('identity.user.pick_selected') }}
              <strong>{{ selDeptIds.length }}</strong>
              {{ t('identity.user.pick_selected_unit') }}
            </p>
            <div class="pick-grid">
              <button
                v-for="d in deptFlatOptions"
                :key="d.value"
                type="button"
                class="pick-chip" :class="[selDeptIds.includes(d.value) ? 'on' : '']"
                @click="togglePick(selDeptIds, d.value)"
              >
                <Icon icon="tabler:building" :size="13" />
                {{ d.label.trim() }}
              </button>
            </div>
          </div>
        </XhTabsContent>
      </XhTabsRoot>
    </XEditModal>

    <!-- 详情 -->
    <XhDialogRoot v-model:open="showDetModal" :close-on-interact-outside="false">
      <XhDialogContent style="--xh-dialog-max-w: 640px">
        <XhDialogTitle v-if="detUser">
          <div class="det-hd-user">
            <div class="av-lg" :style="{ background: detUser.avatar.bg, color: detUser.avatar.fg }">
              {{ detUser.initials }}
            </div>
            <div class="min-w-0">
              <div class="det-name">
                {{ detUser.displayName }}
              </div>
              <div class="det-sub">
                @{{ detUser.userName }}
              </div>
            </div>
          </div>
        </XhDialogTitle>
        <XhDialogCloseTrigger />

        <div v-if="detailLoading" class="modal-loading">
          {{ t('common.statuses.loading') }}
        </div>
        <template v-else-if="detUser">
          <div class="det-info-grid">
            <div>
              <span class="muted">{{ t('identity.user.detail.country') }}</span>
              {{ detUser.country }}
            </div>
            <div>
              <span class="muted">{{ t('identity.user.detail.gender') }}</span>
              {{ detUser.gender }}
            </div>
            <div>
              <span class="muted">{{ t('identity.user.detail.roles') }}</span>
              {{ detUser.roles.join('、') || '—' }}
            </div>
            <div>
              <span class="muted">{{ t('identity.user.detail.depts') }}</span>
              {{ detUser.depts.join('、') || '—' }}
            </div>
            <div v-if="detUser.remark" class="col-span-2">
              <span class="muted">{{ t('identity.user.detail.remark') }}</span>
              {{ detUser.remark }}
            </div>
          </div>
          <div class="det-badges">
            <span v-for="badge in detUser.badges" :key="badge.label" class="bdg" :class="[badge.cls]">
              <Icon :icon="badge.icon" :size="12" />
              {{ badge.label }}
            </span>
          </div>
          <div class="det-divider" />
          <div class="det-sec">
            <div class="det-sec-hd">
              <Icon icon="tabler:chart-bar" :size="14" />
              <span>{{ t('identity.user.detail.stats_today') }}</span>
            </div>
            <div class="det-stat-grid">
              <div v-for="m in detUser.metrics" :key="m.label" class="det-stat-card" :class="[m.cls]">
                <div class="det-stat-top">
                  <span class="det-stat-lbl">{{ m.label }}</span>
                  <Icon :icon="m.icon" :size="13" />
                </div>
                <div class="det-stat-val">
                  {{ m.value }}
                </div>
              </div>
            </div>
          </div>
          <div class="det-sec-hd">
            <Icon icon="tabler:device-desktop" :size="14" />
            <span>{{ t('identity.user.detail.login_session') }}</span>
          </div>
          <div v-if="detUser.online" class="s-row">
            <Icon icon="tabler:device-desktop" :size="18" class="session-ico" />
            <div class="flex-1 min-w-0">
              <div class="session-title">
                {{ detUser.sessionLabel }}
              </div>
              <div class="session-sub">
                {{ detUser.lastLoginIp }} · {{ detUser.lastLoginTime }}
              </div>
            </div>
            <span class="bdg bdg-ok">{{ t('identity.user.detail.online') }}</span>
          </div>
          <div v-else class="session-empty">
            {{ t('identity.user.detail.no_active_session') }}
          </div>
        </template>

        <div class="xh-dialog-footer">
          <XhFlex justify="end">
            <XhButton variant="subtle" size="sm" @click="closeModals">
              {{ t('common.actions.close') }}
            </XhButton>
          </XhFlex>
        </div>
      </XhDialogContent>
    </XhDialogRoot>

    <!-- 删除确认 -->
    <XhDialogRoot v-model:open="showDelModal" :close-on-interact-outside="false">
      <XhDialogContent style="--xh-dialog-max-w: 420px">
        <XhDialogTitle>{{ t('identity.user.del_title') }}</XhDialogTitle>
        <XhDialogCloseTrigger />
        <div class="del-body">
          <Icon icon="tabler:alert-triangle" :size="26" class="del-icon" />
          <div>
            <p class="del-title">
              {{ t('identity.user.del_confirm_prefix') }}
              <span class="name">{{ delTarget?.name }}</span>
              {{ t('identity.user.del_confirm_suffix') }}
            </p>
            <p class="del-desc">
              {{ t('identity.user.del_desc') }}
            </p>
          </div>
        </div>

        <div class="xh-dialog-footer">
          <XhFlex justify="end">
            <XhButton variant="subtle" size="sm" @click="closeModals">
              {{ t('common.actions.cancel') }}
            </XhButton>
            <XhButton variant="subtle" size="sm" tone="danger" @click="confirmDelete">
              {{ t('identity.user.del_confirm_btn') }}
            </XhButton>
          </XhFlex>
        </div>
      </XhDialogContent>
    </XhDialogRoot>

    <!-- 成员数据范围抽屉 -->
    <XhDrawerRoot v-model:open="scopeVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 640px">
        <XhDrawerTitle>{{ t('identity.data_scope.drawer_title', { name: scopeUser ? displayName(scopeUser) : '' }) }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div class="xh-loading-stage scope-stage" :class="{ 'is-loading': scopeLoading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <XhAlertRoot tone="info" class="mb-3">
            <XhAlertIndicator>
              <Icon icon="tabler:info-circle" :size="16" />
            </XhAlertIndicator>
            <XhAlertContent>
              <XhAlertDescription>
                {{ t('identity.data_scope.member_hint') }}
              </XhAlertDescription>
            </XhAlertContent>
          </XhAlertRoot>
          <DataScopeEditor v-model="scopeDraft" :department-tree="scopeTree" allow-inherit />
        </div>
        <div class="xh-dialog-footer">
          <XhButton variant="subtle" @click="scopeVisible = false">
            {{ t('common.actions.cancel') }}
          </XhButton>
          <XhButton variant="subtle" tone="brand" class="ml-2" :loading="scopeSubmitting" :disabled="!scopeDirty || scopeLoading" @click="saveScopes">
            {{ t('identity.data_scope.save') }}
          </XhButton>
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 角色直授抽屉 -->
    <XhDrawerRoot v-model:open="roleGrantVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 720px">
        <XhDrawerTitle>{{ t('identity.user.grant_role_title', { name: roleGrantUser?.userName ?? '' }) }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <p class="grant-tip">
          {{ t('identity.user.grant_role_tip') }}
        </p>
        <XGrantTransfer
          :items="roleGrantItems"
          :value="roleDraft"
          :groups="roleGrantGroups"
          :get-label="role => role.roleName"
          :get-description="role => role.roleCode"
          :loading="roleGrantLoading"
          :disabled="roleGrantLoading"
          :source-title="t('identity.user.grant_role_source')"
          :target-title="t('identity.user.grant_role_target')"
          :search-placeholder="t('identity.user.grant_role_search')"
          @update:value="onRoleTransfer"
        />
        <div class="xh-dialog-footer">
          <XhButton variant="subtle" @click="roleGrantVisible = false">
            {{ t('common.actions.cancel') }}
          </XhButton>
          <XhButton variant="subtle" tone="brand" :loading="roleGrantLoading" :disabled="!roleDirty" style="margin-left: 8px" @click="saveRoleGrants">
            {{ t('identity.user.grant_save') }}
          </XhButton>
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 权限直授抽屉：授予与拒绝各一个穿梭框，两者互斥 -->
    <XhDrawerRoot v-model:open="permGrantVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 980px">
        <XhDrawerTitle>{{ t('identity.user.grant_perm_title', { name: permGrantUser?.userName ?? '' }) }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <p class="grant-tip">
          {{ t('identity.user.grant_perm_tip') }}
        </p>
        <XhTabsRoot v-model:value="permGrantTab" class="grant-tabs" variant="line">
          <XhTabsList>
            <XhTabsTrigger value="grant">
              {{ t('identity.user.grant_perm_allow') }}
            </XhTabsTrigger>
            <XhTabsTrigger value="deny">
              {{ t('identity.user.grant_perm_deny') }}
            </XhTabsTrigger>
            <XhTabsIndicator />
          </XhTabsList>
          <XhTabsContent value="grant">
            <XPermissionTransfer
              :items="permGrantItems"
              :value="permGrantIds"
              :loading="permGrantLoading"
              :disabled="permGrantLoading"
              :source-title="t('identity.user.grant_perm_allow_source')"
              :target-title="t('identity.user.grant_perm_allow_target')"
              :search-placeholder="t('identity.user.grant_perm_search')"
              :other-group-label="t('identity.user.grant_perm_group_other')"
              @update:value="next => onPermTransfer(PermissionAction.Grant, next)"
            >
              <!-- 左栏标出已在另一页签里的：挪过来会把它从拒绝改成授予 -->
              <template #suffix="{ item, side }">
                <XhTagRoot v-if="side === 'source' && permActions.get(item.basicId) === PermissionAction.Deny" variant="subtle" size="sm" tone="danger">
                  <XhTagLabel>{{ t('identity.user.grant_perm_deny_target') }}</XhTagLabel>
                </XhTagRoot>
              </template>
            </XPermissionTransfer>
          </XhTabsContent>
          <XhTabsContent value="deny">
            <XPermissionTransfer
              :items="permGrantItems"
              :value="permDenyIds"
              :loading="permGrantLoading"
              :disabled="permGrantLoading"
              :source-title="t('identity.user.grant_perm_deny_source')"
              :target-title="t('identity.user.grant_perm_deny_target')"
              :search-placeholder="t('identity.user.grant_perm_search')"
              :other-group-label="t('identity.user.grant_perm_group_other')"
              @update:value="next => onPermTransfer(PermissionAction.Deny, next)"
            >
              <template #suffix="{ item, side }">
                <XhTagRoot v-if="side === 'source' && permActions.get(item.basicId) === PermissionAction.Grant" variant="subtle" size="sm" tone="success">
                  <XhTagLabel>{{ t('identity.user.grant_perm_allow_target') }}</XhTagLabel>
                </XhTagRoot>
              </template>
            </XPermissionTransfer>
          </XhTabsContent>
        </XhTabsRoot>
        <div class="xh-dialog-footer">
          <XhButton variant="subtle" @click="permGrantVisible = false">
            {{ t('common.actions.cancel') }}
          </XhButton>
          <XhButton variant="subtle" tone="brand" :loading="permGrantLoading" :disabled="!permDirty" style="margin-left: 8px" @click="savePermGrants">
            {{ t('identity.user.grant_save') }}
          </XhButton>
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>
  </SchemaPage>
</template>

<style scoped>
/* 成员数据范围抽屉：编辑区撑满剩余高度，保存/取消落在底部；部门多时在这里滚动 */
.scope-stage {
  flex: 1;
  min-block-size: 0;
  overflow-y: auto;
}

.tbl-cell-2l {
  min-width: 0;
  line-height: 1.4;
}

.tbl-cell-2l__primary,
.tbl-cell-2l__secondary {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.tbl-cell-2l__primary {
  font-size: 12px;
  color: hsl(var(--foreground));
}

.tbl-cell-2l__primary--strong {
  font-size: 13px;
  font-weight: 600;
}

.tbl-cell-2l__secondary {
  font-size: 11px;
  color: hsl(var(--muted-foreground));
}

.tbl-av {
  width: 30px;
  height: 30px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  font-weight: 500;
}

.sys-tag {
  font-size: 9px;
  padding: 1px 4px;
  margin-left: 4px;
  border-radius: 3px;
  background: var(--xh-color-warning-600);
  color: var(--xh-color-warning-500);
}

/* 图标语义色：勿加页面前缀，弹窗 Teleport 到 body 后不在该子树内 */
.sec-block-hd :deep(svg),
.det-sec-hd :deep(svg) {
  color: hsl(var(--primary));
}

.det-stat-primary .det-stat-top :deep(svg) {
  color: hsl(var(--primary));
}

.det-stat-info .det-stat-top :deep(svg) {
  color: var(--xh-color-info-500);
}

.det-stat-warning .det-stat-top :deep(svg) {
  color: var(--xh-color-warning-500);
}

.det-stat-muted .det-stat-top :deep(svg) {
  color: hsl(var(--muted-foreground));
}

.pick-chip :deep(svg) {
  color: hsl(var(--muted-foreground));
}

.pick-chip.on :deep(svg) {
  color: hsl(var(--primary-foreground));
}

.session-ico {
  color: var(--xh-color-info-500);
}

.del-icon {
  color: var(--xh-color-warning-500);
}

.bdg :deep(svg) {
  color: currentColor;
}

/* 弹窗内容卡 */
.modal-loading {
  padding: 48px 0;
  text-align: center;
  color: hsl(var(--muted-foreground));
}

.sec-panel {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.sec-block-hd {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  font-weight: 500;
  color: hsl(var(--muted-foreground));
  margin-bottom: 8px;
}

.form-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 12px;
  border: 1px solid hsl(var(--border));
  border-radius: var(--xh-shape-control);
  margin-bottom: 8px;
  background: hsl(var(--muted));
}

.form-row-main {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  flex: 1;
}

.form-row-ico :deep(svg) {
  color: hsl(var(--primary));
}

.form-row-ico.warn :deep(svg) {
  color: var(--xh-color-warning-500);
}

.form-row-ico.ok :deep(svg) {
  color: var(--xh-color-success-500);
}

.lbl {
  font-weight: 500;
  font-size: 13px;
  color: hsl(var(--foreground));
}

.sub {
  font-size: 11px;
  color: hsl(var(--muted-foreground));
  margin-top: 2px;
}

.pick-desc,
.pick-summary {
  font-size: 12px;
  color: hsl(var(--muted-foreground));
  margin: 0 0 8px;
}

.pick-summary strong {
  color: hsl(var(--primary));
}

.pick-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 7px;
  padding: 12px;
  background: hsl(var(--muted));
  border: 1px solid hsl(var(--border));
  border-radius: var(--xh-shape-control);
  min-height: 48px;
}

.pick-chip {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 4px 10px;
  border-radius: var(--xh-shape-control);
  font-size: 12px;
  font-weight: 500;
  cursor: pointer;
  border: 1px solid hsl(var(--border));
  background: hsl(var(--card));
  color: hsl(var(--muted-foreground));
  font-family: inherit;
}

.pick-chip.on {
  background: var(--xh-color-brand-600);
  border-color: var(--xh-color-brand-600);
  color: hsl(var(--primary-foreground));
}

/* 数字框的控件自带 12rem 最小宽，只收外层会被它顶破、把弹窗撑出横向滚动条，
   要连同组件库给的钩子一起收 */
.max-dev-input {
  --xh-number-field-control-min-w: 0;

  flex: none;
  inline-size: 76px;
}

.max-dev-input :deep([data-scope='number-field'][data-part='control']) {
  inline-size: 100%;
}

/* 详情 */
.det-hd-user {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.av-lg {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  font-weight: 500;
  flex-shrink: 0;
}

.det-name {
  font-size: 14px;
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: hsl(var(--foreground));
}

.det-sub {
  font-size: 11px;
  color: hsl(var(--muted-foreground));
}

.det-info-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 7px;
  margin-bottom: 14px;
  font-size: 12px;
  color: hsl(var(--foreground));
}

.det-info-grid .col-span-2 {
  grid-column: span 2;
}

.muted {
  color: hsl(var(--muted-foreground));
}

.det-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 5px;
  margin-bottom: 14px;
}

.bdg {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  padding: 2px 7px;
  border-radius: var(--xh-shape-control);
  font-size: 11px;
  font-weight: 500;
}

.bdg-ok {
  color: var(--xh-color-success-500);
  background: var(--xh-color-success-600);
}

.bdg-no {
  color: var(--xh-color-danger-500);
  background: var(--xh-color-danger-600);
}

.bdg-warn {
  color: var(--xh-color-warning-500);
  background: var(--xh-color-warning-600);
}

.bdg-info {
  color: var(--xh-color-info-500);
  background: var(--xh-color-info-600);
}

.bdg-gray {
  color: hsl(var(--muted-foreground));
  background: hsl(var(--muted));
  border: 1px solid hsl(var(--border));
}

.det-divider {
  height: 1px;
  background: hsl(var(--border));
  margin: 12px 0;
}

.det-sec-hd {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  font-weight: 500;
  color: hsl(var(--muted-foreground));
  margin-bottom: 8px;
}

.det-stat-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 8px;
}

.det-stat-card {
  position: relative;
  padding: 10px 11px;
  background: hsl(var(--card));
  border: 1px solid hsl(var(--border));
  border-radius: var(--xh-shape-control);
  overflow: hidden;
}

.det-stat-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: hsl(var(--border));
}

.det-stat-primary::before {
  background: hsl(var(--primary));
}

.det-stat-info::before {
  background: var(--xh-color-info-500);
}

.det-stat-warning::before {
  background: var(--xh-color-warning-500);
}

.det-stat-muted::before {
  background: hsl(var(--muted-foreground));
}

.det-stat-top {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 6px;
  color: hsl(var(--muted-foreground));
}

.det-stat-lbl {
  font-size: 10px;
  color: hsl(var(--muted-foreground));
}

.det-stat-val {
  font-size: 18px;
  font-weight: 600;
  color: hsl(var(--foreground));
  font-variant-numeric: tabular-nums;
}

.s-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 9px 10px;
  border: 1px solid hsl(var(--border));
  border-radius: var(--xh-shape-control);
  background: hsl(var(--muted));
}

.session-title {
  font-size: 13px;
  font-weight: 500;
  color: hsl(var(--foreground));
}

.session-sub {
  font-size: 11px;
  color: hsl(var(--muted-foreground));
}

.session-empty {
  font-size: 12px;
  color: hsl(var(--muted-foreground));
  padding: 8px 0;
}

.del-body {
  display: flex;
  gap: 12px;
  align-items: flex-start;
}

.del-title {
  margin: 0 0 8px;
  font-weight: 500;
  font-size: 14px;
  color: hsl(var(--foreground));
}

.del-desc {
  margin: 0;
  font-size: 12px;
  color: hsl(var(--muted-foreground));
  line-height: 1.55;
}

.del-title .name {
  color: var(--xh-color-danger-500);
}

/* 直授抽屉：说明一行，穿梭框吃满剩余高度 */
.grant-tip {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.grant-tabs {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-block-size: 0;
}

/* 当前页签的面板接着往下撑，穿梭框才拿得到剩余高度 */
.grant-tabs > [data-scope='tabs'][data-part='content'][data-state='active'] {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-block-size: 0;
}
</style>
