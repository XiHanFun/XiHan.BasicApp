<script setup lang="ts">
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
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import {
  NButton,
  NCheckbox,
  NConfigProvider,
  NDatePicker,
  NDrawer,
  NDrawerContent,
  NEmpty,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NSelect,
  NSpace,
  NSpin,
  NSwitch,
  NTabPane,
  NTabs,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, ref } from 'vue'
import {
  createPageRequest,
  EnableStatus,
  PermissionAction,
  permissionApi,
  roleApi,
  SessionStatus,
  StatisticsPeriod,
  TenantMemberInviteStatus,
  TenantMemberType,
  TwoFactorMethod,
  UserGender,
  userManagementApi,
} from '@/api'
import { Icon, SchemaPage } from '~/components'
import { GENDER_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'
import UserAvatarCell from './UserAvatarCell.vue'

defineOptions({ name: 'SystemUserPage' })

const GENDER_TAG_TYPE: Record<UserGender, 'default' | 'info' | 'warning'> = {
  [UserGender.Unknown]: 'default',
  [UserGender.Male]: 'info',
  [UserGender.Female]: 'warning',
}

interface UserFormState {
  basicId?: ApiId
  userName: string
  realName: string
  nickName: string
  email: string
  phone: string
  gender: UserGender
  birthday: number | null
  language: string
  country: string
  timeZone: string
  status: EnableStatus
  remark: string
  initialPassword: string
  isLocked: boolean
  multiLogin: boolean
  maxDev: number
}

const message = useMessage()
const dialog = useDialog()

/** 头像色板：跟随 Naive 语义色，明暗主题均可用 */
const AVATAR_TONES = ['primary', 'info', 'success', 'warning', 'error'] as const

const languageOptions = [
  { label: '简体中文', value: 'zh-CN' },
  { label: '繁体中文', value: 'zh-TW' },
  { label: 'English', value: 'en-US' },
  { label: '日本語', value: 'ja-JP' },
]

const timezoneOptions = [
  { label: 'Asia/Shanghai (UTC+8)', value: 'Asia/Shanghai' },
  { label: 'Asia/Tokyo (UTC+9)', value: 'Asia/Tokyo' },
  { label: 'America/New_York (UTC-5)', value: 'America/New_York' },
  { label: 'Europe/London (UTC+0)', value: 'Europe/London' },
]

// 搜索/表单选项
const statusOptions = STATUS_OPTIONS.map(o => ({ label: o.label, value: o.value }))
const genderOptions = GENDER_OPTIONS

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

const formTitle = computed(() =>
  userForm.value.basicId ? `编辑用户 · ${userForm.value.userName}` : '新建用户',
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
        ? { label: '邮箱已验证', cls: 'bdg-ok', icon: 'tabler:mail' }
        : { label: '邮箱未验证', cls: 'bdg-gray', icon: 'tabler:mail' },
    )
    badges.push(
      sec.phoneVerified
        ? { label: '手机已验证', cls: 'bdg-ok', icon: 'tabler:phone' }
        : { label: '手机未验证', cls: 'bdg-gray', icon: 'tabler:phone' },
    )
    if (sec.twoFactorEnabled) {
      badges.push({
        label: `2FA: ${formatTwoFa(sec.twoFactorMethod)}`,
        cls: 'bdg-info',
        icon: 'tabler:shield-check',
      })
    }
    if (sec.isLocked)
      badges.push({ label: '账号已锁定', cls: 'bdg-no', icon: 'tabler:lock' })
    if (sec.failedLoginAttempts > 0) {
      badges.push({
        label: `失败登录 ${sec.failedLoginAttempts} 次`,
        cls: 'bdg-warn',
        icon: 'tabler:alert-triangle',
      })
    }
  }
  const inviteAccepted = d.tenantMembership?.inviteStatus === TenantMemberInviteStatus.Accepted
  if (d.tenantMembership && !inviteAccepted) {
    badges.push({ label: '未激活', cls: 'bdg-warn', icon: 'tabler:clock-pause' })
  }
  return {
    userName: u.userName,
    displayName: u.realName || u.nickName || u.userName,
    initials: getInitials(u),
    avatar: getAvatarStyle(u.userName),
    language: u.language ?? '—',
    timeZone: u.timeZone ?? '—',
    country: u.country ?? '—',
    gender: getOptionLabel(GENDER_OPTIONS, u.gender),
    roles: d.roles.map(r => r.roleName ?? '').filter(Boolean),
    depts: d.departments.map(dep => dep.departmentName ?? '').filter(Boolean),
    remark: u.remark,
    badges,
    metrics: [
      {
        label: '登录次数',
        value: todayStat?.loginCount ?? 0,
        icon: 'tabler:login-2',
        cls: 'det-stat-primary',
      },
      {
        label: '访问次数',
        value: todayStat?.accessCount ?? 0,
        icon: 'tabler:activity',
        cls: 'det-stat-info',
      },
      {
        label: '在线时长',
        value: `${Math.round((todayStat?.onlineTime ?? 0) / 3600)}h`,
        icon: 'tabler:clock',
        cls: 'det-stat-warning',
      },
      {
        label: '当前状态',
        value: onlineSession ? '在线' : '离线',
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
      ? `${onlineSession.deviceName || '设备'} · ${onlineSession.browser || ''}`
      : '',
  }
})

function createDefaultForm(): UserFormState {
  return {
    userName: '',
    realName: '',
    nickName: '',
    email: '',
    phone: '',
    gender: UserGender.Unknown,
    birthday: null,
    language: 'zh-CN',
    country: '',
    timeZone: 'Asia/Shanghai',
    status: EnableStatus.Enabled,
    remark: '',
    initialPassword: '',
    isLocked: false,
    multiLogin: true,
    maxDev: 0,
  }
}

function getAvatarStyle(name: string) {
  const tone = AVATAR_TONES[name.charCodeAt(0) % AVATAR_TONES.length]!
  return {
    bg: `color-mix(in srgb, var(--n-${tone}-color) 16%, var(--n-card-color))`,
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
    parts.push('邮箱')
  if (method & TwoFactorMethod.Phone)
    parts.push('短信')
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

/** 查询构建（resource.page 与导出快照复用） */
function buildUserQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
    keyword: toStr(f.keyword),
    gender: toStr(f.gender) as UserGender | undefined,
    status: toStr(f.status) as EnableStatus | undefined,
  }
}

// ── 字段单一事实源：列 + 常用搜索 ──────────────────────────────────
const fields: ListFieldSchema[] = [
  // 仅搜索（不作为列）
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索用户名、姓名、邮箱…', width: 240, order: 0 },
  // 头像（仅列）
  {
    key: 'avatar',
    title: '头像',
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
    title: '用户信息',
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
          r.isSystemAccount ? h('span', { class: 'sys-tag' }, '系统') : null,
        ]),
        h('div', { class: 'tbl-cell-2l__secondary' }, subLine),
      ])
    },
  },
  // 性别（常用搜索 + 列）
  {
    key: 'gender',
    title: '性别',
    dataType: 'enum',
    searchable: true,
    options: genderOptions,
    searchPlaceholder: '全部性别',
    width: 80,
    order: 3,
    render: (row) => {
      const r = row as unknown as UserListItemDto
      const label = getOptionLabel(GENDER_OPTIONS, r.gender)
      return h(
        NTag,
        { size: 'small', type: GENDER_TAG_TYPE[r.gender] ?? 'default', bordered: false, style: { fontSize: '11px', fontWeight: 500 } },
        () => label,
      )
    },
  },
  // 地区/语言（仅列）
  {
    key: 'locale',
    title: '地区/语言',
    dataType: 'string',
    width: 140,
    order: 4,
    render: (row) => {
      const r = row as unknown as UserListItemDto
      const region = [r.country, r.language].filter(Boolean).join(' · ') || '—'
      return h('div', { class: 'tbl-cell-2l' }, [
        h('div', { class: 'tbl-cell-2l__primary' }, region),
        r.timeZone ? h('div', { class: 'tbl-cell-2l__secondary' }, r.timeZone) : null,
      ])
    },
  },
  // 账号状态（常用搜索 + 列）
  {
    key: 'status',
    title: '账号状态',
    dataType: 'enum',
    searchable: true,
    options: statusOptions,
    searchPlaceholder: '全部状态',
    width: 100,
    order: 5,
    render: (row) => {
      const r = row as unknown as UserListItemDto
      return h(
        NTag,
        {
          size: 'small',
          type: r.status === EnableStatus.Enabled ? 'success' : 'error',
          bordered: false,
          style: { fontSize: '11px', fontWeight: 500 },
        },
        () => (r.status === EnableStatus.Enabled ? '已启用' : '已禁用'),
      )
    },
  },
  // 角色（仅列，来自后端批量聚合 roleNames）
  {
    key: 'roleNames',
    title: '角色',
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
        h(NTag, { size: 'small', round: true, bordered: false, type: 'info', style: { fontSize: '11px' } }, () => name)))
    },
  },
  // 部门（仅列，主部门名称）
  {
    key: 'departmentName',
    title: '部门',
    dataType: 'string',
    minWidth: 120,
    order: 5.2,
    render: (row) => {
      const r = row as unknown as UserListItemDto
      return r.departmentName || h('span', { class: 'text-foreground/40' }, '—')
    },
  },
  // 安全标记（仅列，锁定 / 双因素）
  {
    key: 'security',
    title: '安全',
    dataType: 'string',
    width: 120,
    order: 5.3,
    render: (row) => {
      const r = row as unknown as UserListItemDto
      const tags = []
      if (r.isLocked) {
        tags.push(h(NTag, { size: 'small', round: true, bordered: false, type: 'error', style: { fontSize: '11px' } }, () => '锁定'))
      }
      if (r.twoFactorEnabled) {
        tags.push(h(NTag, { size: 'small', round: true, bordered: false, type: 'success', style: { fontSize: '11px' } }, () => '2FA'))
      }
      if (tags.length === 0) {
        return h('span', { class: 'text-foreground/40' }, '正常')
      }
      return h('div', { class: 'flex flex-wrap gap-1' }, tags)
    },
  },
  // 最后登录（仅列）
  {
    key: 'lastLoginTime',
    title: '最后登录',
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
    title: '最后登录 IP',
    dataType: 'string',
    minWidth: 130,
    order: 6.1,
    render: (row) => {
      const r = row as unknown as UserListItemDto
      return r.lastLoginIp || h('span', { class: 'text-foreground/40' }, '—')
    },
  },
  // 创建时间（仅列）
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', sortable: true, width: 170, order: 7 },
]

const schema: PageSchema = {
  pageCode: 'system.user',
  exportPermission: 'saas:user:export',
  pageName: '用户管理',
  batchRemovable: true,
  removePermission: 'saas:user:delete',
  statusPermission: 'saas:user:status',
  rowKey: 'basicId',
  scrollX: 1760,
  fields,
  resource: {
    page: params => userManagementApi.page(buildUserQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
    remove: id => userManagementApi.delete(id),
    updateStatus: (id, enabled) => userManagementApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled }),
    export: { businessType: 'system.user', buildQuery: buildUserQuery },
  },
  actions: [
    { key: 'create', title: '新建用户', scope: 'page', type: 'primary', icon: 'tabler:plus' },
    { key: 'view', title: '查看详情', scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: '编辑', scope: 'row', icon: 'lucide:pencil' },
    { key: 'grant', title: '权限直授', scope: 'row', icon: 'lucide:key-round' },
    { key: 'lock', title: '锁定/解锁', scope: 'row', icon: 'lucide:lock' },
    { key: 'resetPassword', title: '重置密码', scope: 'row', icon: 'lucide:key-square' },
    {
      key: 'resetOtp',
      title: '重置 OTP',
      scope: 'row',
      icon: 'lucide:shield-off',
      visible: row => (row as unknown as UserListItemDto).twoFactorEnabled,
    },
    { key: 'logout', title: '强制下线', scope: 'row', icon: 'lucide:log-out' },
    {
      key: 'delete',
      title: '删除',
      scope: 'row',
      icon: 'lucide:trash-2',
      visible: row => !(row as unknown as UserListItemDto).isSystemAccount,
    },
  ],
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
    case 'grant':
      if (row)
        void openGrantDrawer(row)
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
    message.warning('加载角色或部门选项失败')
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
    email: '',
    phone: '',
    gender: u.gender,
    birthday: null,
    language: u.language ?? 'zh-CN',
    country: u.country ?? '',
    timeZone: u.timeZone ?? 'Asia/Shanghai',
    status: u.status,
    remark: u.remark ?? '',
    initialPassword: '',
    isLocked: sec?.isLocked ?? false,
    multiLogin: sec?.allowMultiLogin ?? true,
    maxDev: sec?.maxLoginDevices ?? 0,
  }
  existingRoles.value = detail.roles
  existingDepts.value = detail.departments
  selRoleIds.value = detail.roles.map(r => r.roleId)
  selDeptIds.value = detail.departments.map(d => d.departmentId)
}

async function openEdit(id: ApiId) {
  try {
    const detail = await userManagementApi.detailView(id)
    if (!detail) {
      message.warning('未找到用户')
      return
    }
    await fillFormFromDetail(detail)
    formTab.value = '0'
    showFormModal.value = true
  }
  catch {
    message.error('加载用户信息失败')
  }
}

async function openDetail(id: ApiId) {
  showDetModal.value = true
  detailLoading.value = true
  currentDetail.value = null
  try {
    currentDetail.value = await userManagementApi.detailView(id)
  }
  catch {
    message.error('加载用户详情失败')
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

async function syncRoles(userId: ApiId) {
  const current = existingRoles.value
  const selected = new Set(selRoleIds.value)
  for (const role of roleOptions.value) {
    const bound = current.find(c => c.roleId === role.basicId)
    const want = selected.has(role.basicId)
    if (want && !bound) {
      await userManagementApi.roles.grant({ userId, roleId: role.basicId })
    }
    else if (!want && bound) {
      await userManagementApi.roles.revoke(bound.basicId)
    }
  }
}

async function syncDepartments(userId: ApiId) {
  const current = existingDepts.value
  const selected = new Set(selDeptIds.value)
  for (const depId of deptFlatOptions.value.map(d => d.value)) {
    const bound = current.find(c => c.departmentId === depId)
    const want = selected.has(depId)
    if (want && !bound) {
      await userManagementApi.userDepartments.assign({
        userId,
        departmentId: depId,
        isMain: selected.size === 1 || !current.some(c => c.isMain),
      })
    }
    else if (!want && bound) {
      await userManagementApi.userDepartments.revoke(bound.basicId)
    }
  }
}

async function saveUser() {
  const form = userForm.value
  if (!form.userName.trim()) {
    message.warning('用户名不能为空')
    formTab.value = '0'
    return
  }
  if (!form.basicId && !form.initialPassword.trim()) {
    message.warning('请设置初始密码')
    formTab.value = '0'
    return
  }
  submitLoading.value = true
  try {
    let userId = form.basicId
    if (userId) {
      const updateInput: UserUpdateDto = {
        basicId: userId,
        avatar: null,
        birthday: form.birthday ? new Date(form.birthday).toISOString() : null,
        country: normalizeStr(form.country),
        email: normalizeStr(form.email),
        gender: form.gender,
        language: form.language,
        nickName: normalizeStr(form.nickName),
        phone: normalizeStr(form.phone),
        realName: normalizeStr(form.realName),
        remark: normalizeStr(form.remark),
        timeZone: normalizeStr(form.timeZone),
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
        language: form.language,
        country: normalizeStr(form.country),
        timeZone: normalizeStr(form.timeZone),
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

    message.success('保存成功')
    closeModals()
    reloadList()
  }
  catch {
    message.error('保存失败')
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
    message.success(locked ? '账号已解锁' : '账号已锁定')
    reloadList()
  }
  catch {
    message.error('操作失败')
  }
}

function displayName(row: UserListItemDto): string {
  return row.nickName || row.userName
}

function forceLogout(row: UserListItemDto) {
  dialog.warning({
    title: '强制下线',
    content: `将撤销用户「${displayName(row)}」的全部登录会话，其在线设备会立即被踢出。确认继续？`,
    positiveText: '确认下线',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await userManagementApi.sessions.revokeUserSessions({
          userId: row.basicId,
          reason: '管理员强制下线',
        })
        message.success('已强制下线')
        reloadList()
      }
      catch {
        message.error('强制下线失败')
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
  dialog.warning({
    title: '重置密码',
    content: `将为用户「${displayName(row)}」生成新的临时密码并立即生效，原密码作废、登录失败计数清零。确认继续？`,
    positiveText: '确认重置',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await userManagementApi.security.resetPassword({
          userId: row.basicId,
          newPassword: tempPassword,
          remark: '管理员重置密码',
        })
        dialog.success({
          title: '密码已重置',
          content: () => h('div', { class: 'space-y-2' }, [
            h('div', `用户「${displayName(row)}」的临时密码（仅本次显示，请立即转交并提醒用户修改）：`),
            h('div', {
              class: 'rounded bg-[hsl(var(--muted))] px-3 py-2 font-mono text-base font-semibold tracking-wider select-all',
            }, tempPassword),
          ]),
          positiveText: '复制密码',
          negativeText: '关闭',
          onPositiveClick: () => {
            void navigator.clipboard?.writeText(tempPassword)
            message.success('临时密码已复制')
            return false
          },
        })
      }
      catch {
        message.error('重置密码失败')
      }
    },
  })
}

function resetOtp(row: UserListItemDto) {
  dialog.warning({
    title: '重置 OTP',
    content: `将清除用户「${displayName(row)}」的双因素认证绑定（OTP），用户下次登录不再要求验证码，可在个人中心重新绑定。确认继续？`,
    positiveText: '确认重置',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await userManagementApi.security.resetTwoFactor({
          userId: row.basicId,
          remark: '管理员重置双因素认证',
        })
        message.success('OTP 已重置')
        reloadList()
      }
      catch {
        message.error('重置 OTP 失败')
      }
    },
  })
}

// ── 权限直授抽屉（角色直授 + 权限直授 Grant/Deny） ──────────────
const grantVisible = ref(false)
const grantUser = ref<UserListItemDto | null>(null)
const grantTab = ref('role')
const grantLoading = ref(false)
const grantRoleList = ref<UserRoleListItemDto[]>([])
const grantPermList = ref<UserPermissionListItemDto[]>([])
const permCatalog = ref<PermissionListItemDto[]>([])
const grantKeyword = ref('')
const grantBusyId = ref<ApiId | null>(null)

/** roleId → 用户角色授权记录 */
const grantRoleByRoleId = computed(() => {
  const map = new Map<ApiId, UserRoleListItemDto>()
  for (const item of grantRoleList.value) {
    map.set(item.roleId, item)
  }
  return map
})

/** permissionId → 用户权限直授记录 */
const grantPermByPermId = computed(() => {
  const map = new Map<ApiId, UserPermissionListItemDto>()
  for (const item of grantPermList.value) {
    map.set(item.permissionId, item)
  }
  return map
})

const grantPermFiltered = computed(() => {
  const kw = grantKeyword.value.trim().toLowerCase()
  if (!kw) {
    return permCatalog.value
  }
  return permCatalog.value.filter(p =>
    (p.permissionName ?? '').toLowerCase().includes(kw)
    || (p.permissionCode ?? '').toLowerCase().includes(kw),
  )
})

const grantPermGroups = computed(() => {
  const map = new Map<string, PermissionListItemDto[]>()
  for (const p of grantPermFiltered.value) {
    const key = p.moduleCode || '其它'
    const arr = map.get(key) ?? []
    arr.push(p)
    map.set(key, arr)
  }
  return [...map.entries()].map(([name, items]) => ({ name, items }))
})

async function loadPermCatalog() {
  if (permCatalog.value.length) {
    return
  }
  const all: PermissionListItemDto[] = []
  let pageIndex = 1
  for (;;) {
    const result = await permissionApi.page(createPageRequest({ page: { pageIndex, pageSize: 100 } }))
    all.push(...result.items)
    if (result.items.length === 0 || pageIndex * 100 >= result.page.totalCount) {
      break
    }
    pageIndex += 1
  }
  permCatalog.value = all
}

async function openGrantDrawer(row: UserListItemDto) {
  grantUser.value = row
  grantVisible.value = true
  grantTab.value = 'role'
  grantKeyword.value = ''
  grantLoading.value = true
  try {
    const [roles, perms] = await Promise.all([
      userManagementApi.roles.list(row.basicId),
      userManagementApi.permissions.list(row.basicId),
    ])
    grantRoleList.value = roles
    grantPermList.value = perms
    await loadPermCatalog()
  }
  catch {
    message.error('加载直授信息失败')
  }
  finally {
    grantLoading.value = false
  }
}

async function toggleGrantRole(role: RoleSelectItemDto, checked: boolean) {
  if (!grantUser.value || grantBusyId.value != null) {
    return
  }
  grantBusyId.value = role.basicId
  try {
    if (checked) {
      await userManagementApi.roles.grant({ userId: grantUser.value.basicId, roleId: role.basicId })
      message.success(`已授角色：${role.roleName}`)
    }
    else {
      const bound = grantRoleByRoleId.value.get(role.basicId)
      if (bound) {
        await userManagementApi.roles.revoke(bound.basicId)
        message.success(`已撤角色：${role.roleName}`)
      }
    }
    grantRoleList.value = await userManagementApi.roles.list(grantUser.value.basicId)
  }
  catch {
    message.error('操作失败')
  }
  finally {
    grantBusyId.value = null
  }
}

async function setPermGrant(permission: PermissionListItemDto, action: PermissionAction) {
  if (!grantUser.value || grantBusyId.value != null) {
    return
  }
  grantBusyId.value = permission.basicId
  try {
    const existing = grantPermByPermId.value.get(permission.basicId)
    if (existing && existing.permissionAction === action) {
      // 再次点击当前态 → 撤销直授
      await userManagementApi.permissions.revoke(existing.basicId)
    }
    else {
      if (existing) {
        await userManagementApi.permissions.revoke(existing.basicId)
      }
      await userManagementApi.permissions.grant({
        userId: grantUser.value.basicId,
        permissionId: permission.basicId,
        permissionAction: action,
      })
    }
    grantPermList.value = await userManagementApi.permissions.list(grantUser.value.basicId)
  }
  catch {
    message.error('操作失败')
  }
  finally {
    grantBusyId.value = null
  }
}

async function confirmDelete() {
  if (!delTarget.value)
    return
  try {
    await userManagementApi.delete(delTarget.value.id)
    message.success('用户已删除')
    closeModals()
    reloadList()
  }
  catch {
    message.error('删除失败')
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <!-- 新建/编辑：preset=card 使用 Naive 主题卡片背景，避免暗色下透明 -->
    <NModal
      v-model:show="showFormModal"
      :mask-closable="false"
      :auto-focus="false"
      :bordered="false"
      :title="formTitle"
      preset="card"
      style="width: 640px; max-width: calc(100vw - 32px)"
    >
      <NConfigProvider size="small" abstract>
        <NTabs v-model:value="formTab" type="line" animated>
          <NTabPane name="0" tab="基本信息" display-directive="show">
            <div class="form-grid">
              <NFormItem
                label="用户名 *"
                :show-feedback="false"
                label-style="font-size:11px;font-weight:500"
              >
                <NInput
                  v-model:value="userForm.userName"
                  placeholder="请输入用户名"
                  :disabled="!!userForm.basicId"
                />
              </NFormItem>
              <NFormItem
                label="真实姓名"
                :show-feedback="false"
                label-style="font-size:11px;font-weight:500"
              >
                <NInput v-model:value="userForm.realName" placeholder="请输入真实姓名" />
              </NFormItem>
              <NFormItem
                label="昵称"
                :show-feedback="false"
                label-style="font-size:11px;font-weight:500"
              >
                <NInput v-model:value="userForm.nickName" placeholder="请输入昵称" />
              </NFormItem>
              <NFormItem
                label="邮箱"
                :show-feedback="false"
                label-style="font-size:11px;font-weight:500"
              >
                <NInput v-model:value="userForm.email" placeholder="请输入邮箱" />
              </NFormItem>
              <NFormItem
                label="手机号"
                :show-feedback="false"
                label-style="font-size:11px;font-weight:500"
              >
                <NInput v-model:value="userForm.phone" placeholder="请输入手机号" />
              </NFormItem>
              <NFormItem
                label="性别"
                :show-feedback="false"
                label-style="font-size:11px;font-weight:500"
              >
                <NSelect v-model:value="userForm.gender" :options="genderOptions" />
              </NFormItem>
              <NFormItem
                label="生日"
                :show-feedback="false"
                label-style="font-size:11px;font-weight:500"
              >
                <NDatePicker v-model:value="userForm.birthday" type="date" class="w-full" />
              </NFormItem>
              <NFormItem
                label="语言"
                :show-feedback="false"
                label-style="font-size:11px;font-weight:500"
              >
                <NSelect v-model:value="userForm.language" :options="languageOptions" />
              </NFormItem>
              <NFormItem
                label="国家/地区"
                :show-feedback="false"
                label-style="font-size:11px;font-weight:500"
              >
                <NInput v-model:value="userForm.country" placeholder="如：CN" />
              </NFormItem>
              <NFormItem
                label="时区"
                :show-feedback="false"
                label-style="font-size:11px;font-weight:500"
              >
                <NSelect v-model:value="userForm.timeZone" :options="timezoneOptions" />
              </NFormItem>
              <NFormItem
                label="账号状态"
                :show-feedback="false"
                label-style="font-size:11px;font-weight:500"
              >
                <NSelect v-model:value="userForm.status" :options="statusOptions" />
              </NFormItem>
              <NFormItem
                v-if="!userForm.basicId"
                label="初始密码 *"
                :show-feedback="false"
                label-style="font-size:11px;font-weight:500"
              >
                <NInput
                  v-model:value="userForm.initialPassword"
                  type="password"
                  placeholder="请输入初始密码"
                />
              </NFormItem>
            </div>
            <NFormItem
              label="备注"
              :show-feedback="false"
              label-style="font-size:11px;font-weight:500"
              class="mt-1"
            >
              <NInput
                v-model:value="userForm.remark"
                type="textarea"
                :rows="2"
                placeholder="请输入备注信息"
              />
            </NFormItem>
          </NTabPane>

          <NTabPane name="1" tab="安全设置" display-directive="show">
            <div class="sec-panel">
              <div class="sec-block">
                <div class="sec-block-hd">
                  <Icon icon="tabler:shield-lock" :size="14" />
                  <span>账号安全</span>
                </div>
                <div class="form-row">
                  <div class="form-row-main">
                    <Icon icon="tabler:lock" :size="15" class="form-row-ico warn" />
                    <div>
                      <div class="lbl">
                        账号锁定
                      </div>
                      <div class="sub">
                        锁定后用户无法登录
                      </div>
                    </div>
                  </div>
                  <NSwitch v-model:value="userForm.isLocked" />
                </div>
              </div>
              <div class="sec-block">
                <div class="sec-block-hd">
                  <Icon icon="tabler:devices" :size="14" />
                  <span>登录会话</span>
                </div>
                <div class="form-row">
                  <div class="form-row-main">
                    <Icon icon="tabler:login" :size="15" class="form-row-ico ok" />
                    <div>
                      <div class="lbl">
                        允许多端登录
                      </div>
                      <div class="sub">
                        关闭后新登录会踢出旧会话
                      </div>
                    </div>
                  </div>
                  <NSwitch v-model:value="userForm.multiLogin" />
                </div>
                <div class="form-row">
                  <div class="form-row-main">
                    <Icon icon="tabler:device-mobile" :size="15" class="form-row-ico" />
                    <div>
                      <div class="lbl">
                        最大登录设备数
                      </div>
                      <div class="sub">
                        0 = 不限
                      </div>
                    </div>
                  </div>
                  <NInputNumber
                    v-model:value="userForm.maxDev"
                    :min="0"
                    :max="99"
                    class="max-dev-input"
                    size="small"
                    :show-button="false"
                  />
                </div>
              </div>
            </div>
          </NTabPane>

          <NTabPane name="2" tab="角色权限" display-directive="show">
            <div class="pick-panel">
              <p class="pick-desc">
                选择要分配的角色，可多选
              </p>
              <p v-if="selRoleIds.length" class="pick-summary">
                已选
                <strong>{{ selRoleIds.length }}</strong>
                个
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
          </NTabPane>

          <NTabPane name="3" tab="所属部门" display-directive="show">
            <div class="pick-panel">
              <p class="pick-desc">
                选择所属部门，可多选
              </p>
              <p v-if="selDeptIds.length" class="pick-summary">
                已选
                <strong>{{ selDeptIds.length }}</strong>
                个
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
          </NTabPane>
        </NTabs>
      </NConfigProvider>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="closeModals">
            取消
          </NButton>
          <NButton size="small" type="primary" :loading="submitLoading" @click="saveUser">
            保存
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <!-- 详情 -->
    <NModal
      v-model:show="showDetModal"
      :mask-closable="false"
      :auto-focus="false"
      :bordered="false"
      preset="card"
      title="用户详情"
      style="width: 640px; max-width: calc(100vw - 32px)"
    >
      <template v-if="detUser" #header>
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
      </template>

      <div v-if="detailLoading" class="modal-loading">
        加载中…
      </div>
      <template v-else-if="detUser">
        <div class="det-info-grid">
          <div>
            <span class="muted">语言/时区：</span>
            {{ detUser.language }} / {{ detUser.timeZone }}
          </div>
          <div>
            <span class="muted">国家：</span>
            {{ detUser.country }}
          </div>
          <div>
            <span class="muted">性别：</span>
            {{ detUser.gender }}
          </div>
          <div>
            <span class="muted">角色：</span>
            {{ detUser.roles.join('、') || '—' }}
          </div>
          <div>
            <span class="muted">部门：</span>
            {{ detUser.depts.join('、') || '—' }}
          </div>
          <div v-if="detUser.remark" class="col-span-2">
            <span class="muted">备注：</span>
            {{ detUser.remark }}
          </div>
        </div>
        <div class="det-badges">
          <span v-for="t in detUser.badges" :key="t.label" class="bdg" :class="[t.cls]">
            <Icon :icon="t.icon" :size="12" />
            {{ t.label }}
          </span>
        </div>
        <div class="det-divider" />
        <div class="det-sec">
          <div class="det-sec-hd">
            <Icon icon="tabler:chart-bar" :size="14" />
            <span>行为统计（今日）</span>
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
          <span>登录会话</span>
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
          <span class="bdg bdg-ok">在线</span>
        </div>
        <div v-else class="session-empty">
          暂无活跃会话
        </div>
      </template>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="closeModals">
            关闭
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <!-- 删除确认 -->
    <NModal
      v-model:show="showDelModal"
      :mask-closable="false"
      :auto-focus="false"
      :bordered="false"
      preset="card"
      title="确认删除"
      style="width: 420px; max-width: calc(100vw - 32px)"
    >
      <div class="del-body">
        <Icon icon="tabler:alert-triangle" :size="26" class="del-icon" />
        <div>
          <p class="del-title">
            确定要删除用户 "
            <span class="name">{{ delTarget?.name }}</span>
            " 吗？
          </p>
          <p class="del-desc">
            此操作为软删除，保留审计记录；删除前将吊销该用户全部会话。
          </p>
        </div>
      </div>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="closeModals">
            取消
          </NButton>
          <NButton size="small" type="error" @click="confirmDelete">
            确认删除
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <!-- 权限直授抽屉 -->
    <NDrawer v-model:show="grantVisible" :width="720">
      <NDrawerContent closable :title="`权限直授 · ${grantUser?.userName ?? ''}`">
        <NSpin :show="grantLoading">
          <NTabs v-model:value="grantTab" animated type="line">
            <NTabPane name="role" tab="角色直授">
              <p class="grant-desc">
                勾选即把角色直接授予该用户（绕过分组）。
              </p>
              <div class="grant-role-grid">
                <label
                  v-for="r in roleOptions"
                  :key="r.basicId"
                  class="grant-role-chip"
                >
                  <NCheckbox
                    :checked="grantRoleByRoleId.has(r.basicId)"
                    :disabled="grantBusyId === r.basicId"
                    @update:checked="(checked: boolean) => toggleGrantRole(r, checked)"
                  />
                  <span>{{ r.roleName }}</span>
                </label>
              </div>
            </NTabPane>
            <NTabPane name="perm" tab="权限直授">
              <div class="grant-toolbar">
                <NInput v-model:value="grantKeyword" clearable placeholder="搜索权限名称 / 编码" style="width: 240px" />
                <NTag :bordered="false" round type="success">
                  已直授 {{ grantPermList.length }} 项
                </NTag>
              </div>
              <NEmpty v-if="grantPermGroups.length === 0" class="grant-empty" description="无匹配权限" />
              <div v-else class="grant-perm-groups">
                <section v-for="group in grantPermGroups" :key="group.name" class="grant-perm-group">
                  <div class="grant-perm-head">
                    <span>{{ group.name }}</span>
                    <span class="grant-perm-count">{{ group.items.length }}</span>
                  </div>
                  <div class="grant-perm-list">
                    <div
                      v-for="permission in group.items"
                      :key="String(permission.basicId)"
                      class="grant-perm-item"
                    >
                      <span class="grant-perm-text">
                        <span class="grant-perm-name">{{ permission.permissionName }}</span>
                        <span class="grant-perm-code">{{ permission.permissionCode }}</span>
                      </span>
                      <NSpace :size="6">
                        <NButton
                          :disabled="grantBusyId === permission.basicId"
                          size="tiny"
                          :type="grantPermByPermId.get(permission.basicId)?.permissionAction === PermissionAction.Grant ? 'success' : 'default'"
                          @click="setPermGrant(permission, PermissionAction.Grant)"
                        >
                          授予
                        </NButton>
                        <NButton
                          :disabled="grantBusyId === permission.basicId"
                          size="tiny"
                          :type="grantPermByPermId.get(permission.basicId)?.permissionAction === PermissionAction.Deny ? 'error' : 'default'"
                          @click="setPermGrant(permission, PermissionAction.Deny)"
                        >
                          拒绝
                        </NButton>
                      </NSpace>
                    </div>
                  </div>
                </section>
              </div>
            </NTabPane>
          </NTabs>
        </NSpin>
      </NDrawerContent>
    </NDrawer>
  </SchemaPage>
</template>

<style scoped>
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
  color: var(--n-text-color);
}

.tbl-cell-2l__primary--strong {
  font-size: 13px;
  font-weight: 600;
}

.tbl-cell-2l__secondary {
  font-size: 11px;
  color: var(--n-text-color-3);
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
  background: var(--n-warning-color-suppl);
  color: var(--n-warning-color);
}

/* 图标语义色：勿加页面前缀，弹窗 Teleport 到 body 后不在该子树内 */
.sec-block-hd :deep(svg),
.det-sec-hd :deep(svg) {
  color: var(--n-primary-color);
}

.det-stat-primary .det-stat-top :deep(svg) {
  color: var(--n-primary-color);
}

.det-stat-info .det-stat-top :deep(svg) {
  color: var(--n-info-color);
}

.det-stat-warning .det-stat-top :deep(svg) {
  color: var(--n-warning-color);
}

.det-stat-muted .det-stat-top :deep(svg) {
  color: var(--n-text-color-3);
}

.pick-chip :deep(svg) {
  color: var(--n-text-color-3);
}

.pick-chip.on :deep(svg) {
  color: var(--n-primary-color);
}

.session-ico {
  color: var(--n-info-color);
}

.del-icon {
  color: var(--n-warning-color);
}

.bdg :deep(svg) {
  color: currentColor;
}

/* 弹窗内容卡 */
.modal-loading {
  padding: 48px 0;
  text-align: center;
  color: var(--n-text-color-3);
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 10px;
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
  color: var(--n-text-color-3);
  margin-bottom: 8px;
}

.form-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 12px;
  border: 1px solid var(--n-border-color);
  border-radius: var(--n-border-radius);
  margin-bottom: 8px;
  background: var(--n-action-color);
}

.form-row-main {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  flex: 1;
}

.form-row-ico :deep(svg) {
  color: var(--n-primary-color);
}

.form-row-ico.warn :deep(svg) {
  color: var(--n-warning-color);
}

.form-row-ico.ok :deep(svg) {
  color: var(--n-success-color);
}

.lbl {
  font-weight: 500;
  font-size: 13px;
  color: var(--n-text-color);
}

.sub {
  font-size: 11px;
  color: var(--n-text-color-3);
  margin-top: 2px;
}

.pick-desc,
.pick-summary {
  font-size: 12px;
  color: var(--n-text-color-3);
  margin: 0 0 8px;
}

.pick-summary strong {
  color: var(--n-primary-color);
}

.pick-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 7px;
  padding: 12px;
  background: var(--n-action-color);
  border: 1px solid var(--n-border-color);
  border-radius: var(--n-border-radius);
  min-height: 48px;
}

.pick-chip {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 4px 10px;
  border-radius: var(--n-border-radius);
  font-size: 12px;
  font-weight: 500;
  cursor: pointer;
  border: 1px solid var(--n-border-color);
  background: var(--n-card-color);
  color: var(--n-text-color-2);
  font-family: inherit;
}

.pick-chip.on {
  background: var(--n-primary-color-suppl);
  border-color: var(--n-primary-color);
  color: var(--n-primary-color);
}

.max-dev-input {
  width: 70px;
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
  color: var(--n-text-color);
}

.det-sub {
  font-size: 11px;
  color: var(--n-text-color-3);
}

.det-info-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 7px;
  margin-bottom: 14px;
  font-size: 12px;
  color: var(--n-text-color);
}

.det-info-grid .col-span-2 {
  grid-column: span 2;
}

.muted {
  color: var(--n-text-color-3);
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
  border-radius: var(--n-border-radius);
  font-size: 11px;
  font-weight: 500;
}

.bdg-ok {
  color: var(--n-success-color);
  background: var(--n-success-color-suppl);
}

.bdg-no {
  color: var(--n-error-color);
  background: var(--n-error-color-suppl);
}

.bdg-warn {
  color: var(--n-warning-color);
  background: var(--n-warning-color-suppl);
}

.bdg-info {
  color: var(--n-info-color);
  background: var(--n-info-color-suppl);
}

.bdg-gray {
  color: var(--n-text-color-3);
  background: var(--n-action-color);
  border: 1px solid var(--n-border-color);
}

.det-divider {
  height: 1px;
  background: var(--n-border-color);
  margin: 12px 0;
}

.det-sec-hd {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  font-weight: 500;
  color: var(--n-text-color-3);
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
  background: var(--n-card-color);
  border: 1px solid var(--n-border-color);
  border-radius: var(--n-border-radius);
  overflow: hidden;
}

.det-stat-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: var(--n-border-color);
}

.det-stat-primary::before {
  background: var(--n-primary-color);
}

.det-stat-info::before {
  background: var(--n-info-color);
}

.det-stat-warning::before {
  background: var(--n-warning-color);
}

.det-stat-muted::before {
  background: var(--n-text-color-3);
}

.det-stat-top {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 6px;
  color: var(--n-text-color-3);
}

.det-stat-lbl {
  font-size: 10px;
  color: var(--n-text-color-3);
}

.det-stat-val {
  font-size: 18px;
  font-weight: 600;
  color: var(--n-text-color);
  font-variant-numeric: tabular-nums;
}

.s-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 9px 10px;
  border: 1px solid var(--n-border-color);
  border-radius: var(--n-border-radius);
  background: var(--n-action-color);
}

.session-title {
  font-size: 13px;
  font-weight: 500;
  color: var(--n-text-color);
}

.session-sub {
  font-size: 11px;
  color: var(--n-text-color-3);
}

.session-empty {
  font-size: 12px;
  color: var(--n-text-color-3);
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
  color: var(--n-text-color);
}

.del-desc {
  margin: 0;
  font-size: 12px;
  color: var(--n-text-color-3);
  line-height: 1.55;
}

.del-title .name {
  color: var(--n-error-color);
}

/* 权限直授抽屉 */
.grant-desc {
  margin: 0 0 12px;
  font-size: 12px;
  color: var(--n-text-color-3);
}

.grant-role-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 4px 12px;
}

.grant-role-chip {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 8px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 13px;
}

.grant-role-chip:hover {
  background: rgb(0 0 0 / 0.03);
}

.grant-toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 14px;
}

.grant-empty {
  padding: 40px 0;
}

.grant-perm-groups {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.grant-perm-group {
  border: 1px solid var(--n-border-color);
  border-radius: 8px;
  overflow: hidden;
}

.grant-perm-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 12px;
  background: var(--n-merged-th-color, rgb(0 0 0 / 0.02));
  font-size: 13px;
  font-weight: 600;
}

.grant-perm-count {
  font-size: 12px;
  font-weight: 400;
  opacity: 0.55;
}

.grant-perm-list {
  display: flex;
  flex-direction: column;
}

.grant-perm-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 8px 12px;
  border-top: 1px solid var(--n-border-color);
}

.grant-perm-item:first-child {
  border-top: none;
}

.grant-perm-text {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.grant-perm-name {
  font-size: 13px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.grant-perm-code {
  font-size: 11.5px;
  opacity: 0.6;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
