<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type {
  ApiId,
  RoleSelectItemDto,
  UserCreateDto,
  UserListItemDto,
  UserManagementDetailDto,
  UserUpdateDto,
} from '@/api'
import type { DepartmentTreeNodeDto } from '@/api/modules/organization/department.types'
import type { UserDepartmentListItemDto } from '@/api/modules/organization/user-department.types'
import type { UserRoleListItemDto } from '@/api/modules/authorization/user-role.types'
import {
  NButton,
  NCard,
  NConfigProvider,
  NDataTable,
  NDatePicker,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NPagination,
  NSelect,
  NSpace,
  NSwitch,
  NTabPane,
  NTabs,
  NTag,
  NTooltip,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, ref } from 'vue'
import {
  createPageRequest,
  EnableStatus,
  roleApi,
  StatisticsPeriod,
  TenantMemberInviteStatus,
  TenantMemberType,
  TwoFactorMethod,
  userManagementApi,
  UserGender,
} from '@/api'
import { Icon } from '~/components'
import { GENDER_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

const GENDER_TAG_TYPE: Record<UserGender, 'default' | 'info' | 'warning'> = {
  [UserGender.Unknown]: 'default',
  [UserGender.Male]: 'info',
  [UserGender.Female]: 'warning',
}

defineOptions({ name: 'SystemUserPage' })

/** 列表行扩展信息（由聚合详情填充） */
interface UserRowMeta {
  depts: string[]
  emailMasked: string | null
  emailVerified: boolean
  failedLoginAttempts: number
  isActive: boolean
  isLocked: boolean
  lastLoginIp: string | null
  online: boolean
  phoneVerified: boolean
  roles: string[]
  twoFactorEnabled: boolean
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
const pageSize = ref(20)

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

const tableLoading = ref(false)
const dataList = ref<UserListItemDto[]>([])
const rowMetaMap = ref<Record<string, UserRowMeta>>({})
const totalCount = ref(0)
const currentPage = ref(1)

const filterSearch = ref('')
const filterStatus = ref<EnableStatus | null>(null)
const filterGender = ref<UserGender | null>(null)
const filterDeptId = ref<ApiId | null>(null)
const deptFilterOptions = ref<{ label: string; value: ApiId }[]>([])

const checkedRowKeys = ref<ApiId[]>([])
const showFormModal = ref(false)
const showDetModal = ref(false)
const showDelModal = ref(false)
const formTab = ref('0')
const submitLoading = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<UserManagementDetailDto | null>(null)
const delTarget = ref<{ id: ApiId; name: string } | null>(null)

const roleOptions = ref<RoleSelectItemDto[]>([])
const selRoleIds = ref<ApiId[]>([])
const existingRoles = ref<UserRoleListItemDto[]>([])
const selDeptIds = ref<ApiId[]>([])
const existingDepts = ref<UserDepartmentListItemDto[]>([])

const userForm = ref<UserFormState>(createDefaultForm())

const formTitle = computed(() =>
  userForm.value.basicId ? `编辑用户 · ${userForm.value.userName}` : '新建用户',
)
const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

const statusFilterOptions = STATUS_OPTIONS.map((o) => ({
  label: o.label === '启用' ? '已启用' : '已禁用',
  value: o.value,
}))
const genderFilterOptions = GENDER_OPTIONS
const statusFormOptions = STATUS_OPTIONS.map((o) => ({ label: o.label, value: o.value }))

const detUser = computed(() => {
  const d = currentDetail.value
  if (!d) return null
  const u = d.user
  const sec = d.security
  const todayStat = d.statistics.find((s) => s.period === StatisticsPeriod.Today) ?? d.statistics[0]
  const onlineSession = d.sessions.find((s) => s.isOnline)
  const badges: { label: string; cls: string; icon: string }[] = []
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
    if (sec.isLocked) badges.push({ label: '账号已锁定', cls: 'bdg-no', icon: 'tabler:lock' })
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
    roles: d.roles.map((r) => r.roleName ?? '').filter(Boolean),
    depts: d.departments.map((dep) => dep.departmentName ?? '').filter(Boolean),
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
  if (method & TwoFactorMethod.Totp) parts.push('TOTP')
  if (method & TwoFactorMethod.Email) parts.push('邮箱')
  if (method & TwoFactorMethod.Phone) parts.push('短信')
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
): { label: string; value: ApiId }[] {
  const out: { label: string; value: ApiId }[] = []
  for (const n of nodes) {
    out.push({ label: `${'　'.repeat(depth)}${n.departmentName}`, value: n.basicId })
    if (n.children?.length) out.push(...flattenDeptOptions(n.children, depth + 1))
  }
  return out
}

function buildMetaFromDetail(detail: UserManagementDetailDto): UserRowMeta {
  const sec = detail.security
  const inviteAccepted = detail.tenantMembership?.inviteStatus === TenantMemberInviteStatus.Accepted
  return {
    isLocked: sec?.isLocked ?? false,
    twoFactorEnabled: sec?.twoFactorEnabled ?? false,
    failedLoginAttempts: sec?.failedLoginAttempts ?? 0,
    isActive: inviteAccepted && detail.user.status === EnableStatus.Enabled,
    online: detail.sessions.some((s) => s.isOnline),
    lastLoginIp: detail.sessions.find((s) => s.isOnline)?.ipAddressMasked ?? null,
    roles: detail.roles.map((r) => r.roleName ?? '').filter(Boolean),
    depts: detail.departments.map((d) => d.departmentName ?? '').filter(Boolean),
    emailMasked: detail.externalLogins[0]?.externalEmailMasked ?? null,
    emailVerified: sec?.emailVerified ?? false,
    phoneVerified: sec?.phoneVerified ?? false,
  }
}

async function enrichRows(rows: UserListItemDto[]) {
  const entries = await Promise.all(
    rows.map(async (row) => {
      try {
        const detail = await userManagementApi.detailView(row.basicId)
        if (!detail) return [String(row.basicId), null] as const
        return [String(row.basicId), buildMetaFromDetail(detail)] as const
      } catch {
        return [String(row.basicId), null] as const
      }
    }),
  )
  const next = { ...rowMetaMap.value }
  for (const [id, meta] of entries) {
    if (meta) next[id] = meta
  }
  rowMetaMap.value = next
}

async function fetchData() {
  tableLoading.value = true
  try {
    let deptUserIds: Set<ApiId> | null = null
    if (filterDeptId.value) {
      const bindings = await userManagementApi.userDepartments.departmentUsers(
        filterDeptId.value,
        true,
        true,
      )
      deptUserIds = new Set(bindings.map((b) => b.userId))
    }

    const result = await userManagementApi.page({
      ...createPageRequest({
        page: {
          pageIndex: filterDeptId.value ? 1 : currentPage.value,
          pageSize: filterDeptId.value ? 500 : pageSize.value,
        },
      }),
      gender: filterGender.value ?? undefined,
      keyword: normalizeStr(filterSearch.value),
      status: filterStatus.value ?? undefined,
    })

    let items = result.items ?? []
    if (deptUserIds) {
      items = items.filter((i) => deptUserIds!.has(i.basicId))
      totalCount.value = items.length
      const start = (currentPage.value - 1) * pageSize.value
      items = items.slice(start, start + pageSize.value)
    } else {
      totalCount.value = result.page.totalCount
    }
    dataList.value = items
    // 元数据异步补充，不阻塞表格展示
    void enrichRows(items)
  } catch {
    message.error('加载用户列表失败')
  } finally {
    tableLoading.value = false
  }
}

async function loadOptions() {
  try {
    const [roles, tree] = await Promise.all([
      roleApi.enabledList({ limit: 200 }),
      userManagementApi.departments.tree({ limit: 500, onlyEnabled: true }),
    ])
    roleOptions.value = roles
    deptFilterOptions.value = flattenDeptOptions(tree)
  } catch {
    message.warning('加载角色或部门选项失败')
  }
}

onMounted(() => {
  void loadOptions()
  void fetchData()
})

function applyFilter() {
  currentPage.value = 1
  void fetchData()
}

function resetFilter() {
  filterSearch.value = ''
  filterStatus.value = null
  filterGender.value = null
  filterDeptId.value = null
  currentPage.value = 1
  void fetchData()
}

function handlePageChange(page: number) {
  currentPage.value = page
  void fetchData()
}

function handlePageSizeChange(size: number) {
  pageSize.value = size
  currentPage.value = 1
  void fetchData()
}

function clearChecked() {
  checkedRowKeys.value = []
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
  selRoleIds.value = detail.roles.map((r) => r.roleId)
  selDeptIds.value = detail.departments.map((d) => d.departmentId)
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
  } catch {
    message.error('加载用户信息失败')
  }
}

async function openDetail(id: ApiId) {
  showDetModal.value = true
  detailLoading.value = true
  currentDetail.value = null
  try {
    currentDetail.value = await userManagementApi.detailView(id)
  } catch {
    message.error('加载用户详情失败')
  } finally {
    detailLoading.value = false
  }
}

function openDelete(row: UserListItemDto) {
  if (row.isSystemAccount) return
  delTarget.value = { id: row.basicId, name: row.userName }
  showDelModal.value = true
}

function togglePick(arr: ApiId[], id: ApiId) {
  const i = arr.indexOf(id)
  if (i >= 0) arr.splice(i, 1)
  else arr.push(id)
}

async function syncRoles(userId: ApiId) {
  const current = existingRoles.value
  const selected = new Set(selRoleIds.value)
  for (const role of roleOptions.value) {
    const bound = current.find((c) => c.roleId === role.basicId)
    const want = selected.has(role.basicId)
    if (want && !bound) {
      await userManagementApi.roles.grant({ userId, roleId: role.basicId })
    } else if (!want && bound) {
      await userManagementApi.roles.revoke(bound.basicId)
    }
  }
}

async function syncDepartments(userId: ApiId) {
  const current = existingDepts.value
  const selected = new Set(selDeptIds.value)
  for (const depId of deptFilterOptions.value.map((d) => d.value)) {
    const bound = current.find((c) => c.departmentId === depId)
    const want = selected.has(depId)
    if (want && !bound) {
      await userManagementApi.userDepartments.assign({
        userId,
        departmentId: depId,
        isMain: selected.size === 1 || !current.some((c) => c.isMain),
      })
    } else if (!want && bound) {
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
    } else {
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
    void fetchData()
  } catch {
    message.error('保存失败')
  } finally {
    submitLoading.value = false
  }
}

async function toggleLock(row: UserListItemDto) {
  const meta = rowMetaMap.value[String(row.basicId)]
  const locked = meta?.isLocked ?? false
  try {
    await userManagementApi.security.updateLock({
      userId: row.basicId,
      isLocked: !locked,
      lockoutEndTime: null,
    })
    message.success(locked ? '账号已解锁' : '账号已锁定')
    void fetchData()
  } catch {
    message.error('操作失败')
  }
}

async function forceLogout(row: UserListItemDto) {
  try {
    await userManagementApi.sessions.revokeUserSessions({
      userId: row.basicId,
      reason: '管理员强制下线',
    })
    message.success('已强制下线')
    void fetchData()
  } catch {
    message.error('强制下线失败')
  }
}

async function confirmDelete() {
  if (!delTarget.value) return
  try {
    await userManagementApi.delete(delTarget.value.id)
    message.success('用户已删除')
    closeModals()
    void fetchData()
  } catch {
    message.error('删除失败')
  }
}

function renderTag(label: string, type: 'default' | 'info' | 'success' | 'warning' | 'error') {
  return h(
    NTag,
    { size: 'small', type, bordered: false, style: { fontSize: '11px', fontWeight: 500 } },
    () => label,
  )
}

/** 双行单元格：主行 + 次要行 */
function renderTwoLine(primary: string, secondary?: string | null, primaryStrong = false) {
  return h('div', { class: 'tbl-cell-2l' }, [
    h(
      'div',
      {
        class: ['tbl-cell-2l__primary', primaryStrong ? 'tbl-cell-2l__primary--strong' : ''],
      },
      primary,
    ),
    secondary ? h('div', { class: 'tbl-cell-2l__secondary' }, secondary) : null,
  ])
}

function renderTextClamp2(text: string, title?: string) {
  return h(
    'span',
    {
      class: 'tbl-text-2l',
      title: title ?? (text === '—' ? undefined : text),
    },
    text,
  )
}

function joinOrDash(items: string[]) {
  const text = items.join('、')
  return text || '—'
}

const columns = computed<DataTableColumns<UserListItemDto>>(() => [
  { type: 'selection', fixed: 'left', width: 40 },
  {
    title: '头像',
    key: 'avatar',
    width: 52,
    align: 'center',
    render(row) {
      const c = getAvatarStyle(row.userName)
      return h(
        'div',
        {
          class: 'tbl-av',
          style: { background: c.bg, color: c.fg },
          title: row.realName || row.userName,
        },
        getInitials(row),
      )
    },
  },
  {
    title: '用户信息',
    key: 'user',
    minWidth: 180,
    render(row) {
      const display = row.realName || row.nickName || row.userName
      const nickLine = row.nickName && row.nickName !== display ? row.nickName : null
      const subLine = nickLine ? `${nickLine} · @${row.userName}` : `@${row.userName}`
      return h('div', { class: 'tbl-cell-2l' }, [
        h('div', { class: 'tbl-cell-2l__primary tbl-cell-2l__primary--strong' }, [
          display,
          row.isSystemAccount ? h('span', { class: 'sys-tag' }, '系统') : null,
        ]),
        h('div', { class: 'tbl-cell-2l__secondary' }, subLine),
      ])
    },
  },
  {
    title: '联系信息',
    key: 'contact',
    minWidth: 150,
    render(row) {
      const meta = rowMetaMap.value[String(row.basicId)]
      if (!meta) return h('span', { style: 'color:var(--n-text-color-3);font-size:12px' }, '—')
      const emailLine = meta.emailMasked || (meta.emailVerified ? '邮箱已验证' : '邮箱未验证')
      const phoneLine = meta.phoneVerified ? '手机已验证' : '手机未验证'
      return renderTwoLine(emailLine, phoneLine)
    },
  },
  {
    title: '性别',
    key: 'gender',
    width: 58,
    render(row) {
      const label = getOptionLabel(GENDER_OPTIONS, row.gender)
      return renderTag(label, GENDER_TAG_TYPE[row.gender] ?? 'default')
    },
  },
  {
    title: '角色',
    key: 'roles',
    minWidth: 120,
    render(row) {
      const meta = rowMetaMap.value[String(row.basicId)]
      const text = joinOrDash(meta?.roles ?? [])
      return renderTextClamp2(text)
    },
  },
  {
    title: '部门',
    key: 'depts',
    minWidth: 120,
    render(row) {
      const meta = rowMetaMap.value[String(row.basicId)]
      const text = joinOrDash(meta?.depts ?? [])
      return renderTextClamp2(text)
    },
  },
  {
    title: '地区/语言',
    key: 'locale',
    width: 130,
    render(row) {
      const region = [row.country, row.language].filter(Boolean).join(' · ') || '—'
      return renderTwoLine(region, row.timeZone ?? null)
    },
  },
  {
    title: '账号状态',
    key: 'status',
    width: 100,
    render(row) {
      const meta = rowMetaMap.value[String(row.basicId)]
      const tags = [
        renderTag(
          row.status === EnableStatus.Enabled ? '已启用' : '已禁用',
          row.status === EnableStatus.Enabled ? 'success' : 'error',
        ),
      ]
      if (meta && !meta.isActive) {
        tags.push(renderTag('未激活', 'warning'))
      }
      return h('div', { class: 'tbl-cell-tags' }, tags)
    },
  },
  {
    title: '安全标记',
    key: 'sec',
    width: 100,
    render(row) {
      const meta = rowMetaMap.value[String(row.basicId)]
      if (!meta) return h('span', { style: 'color:var(--n-text-color-3);font-size:12px' }, '—')
      const icons: ReturnType<typeof h>[] = []
      const secIco = (icon: string, tone: 'warn' | 'muted' | 'ok' | 'info') =>
        h('span', { class: ['tbl-sec-ico', `tbl-sec-ico--${tone}`] }, [
          h(Icon, { icon, width: 15 }),
        ])
      if (meta.isLocked) icons.push(secIco('tabler:lock', 'warn'))
      if (!meta.isActive) icons.push(secIco('tabler:clock-pause', 'muted'))
      if (meta.twoFactorEnabled) icons.push(secIco('tabler:shield-check', 'ok'))
      if (meta.failedLoginAttempts > 0) icons.push(secIco('tabler:alert-triangle', 'warn'))
      if (meta.online) icons.push(secIco('tabler:wifi', 'info'))
      return h('div', { class: 'tbl-cell-tags flex gap-[5px]' }, icons.length ? icons : '—')
    },
  },
  {
    title: '最后登录',
    key: 'login',
    minWidth: 150,
    render(row) {
      const ip = rowMetaMap.value[String(row.basicId)]?.lastLoginIp ?? '—'
      return renderTwoLine(formatNullableDate(row.lastLoginTime), ip)
    },
  },
  {
    title: '创建时间',
    key: 'createdTime',
    width: 158,
    render(row) {
      return h(
        'span',
        { style: 'font-size:12px;color:var(--n-text-color-3)' },
        formatDate(row.createdTime),
      )
    },
  },
  {
    title: '操作',
    key: 'actions',
    width: 196,
    fixed: 'right',
    render(row) {
      const meta = rowMetaMap.value[String(row.basicId)]
      const locked = meta?.isLocked ?? false
      return h(NSpace, { size: 4, wrap: false }, () => [
        h(NTooltip, {}, {
          trigger: () =>
            h(NButton, {
              'aria-label': '查看详情',
              circle: true,
              quaternary: true,
              size: 'small',
              onClick: () => openDetail(row.basicId),
            }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:eye' })) }),
          default: () => '查看详情',
        }),
        h(NTooltip, {}, {
          trigger: () =>
            h(NButton, {
              'aria-label': '编辑用户',
              circle: true,
              quaternary: true,
              size: 'small',
              type: 'primary',
              onClick: () => openEdit(row.basicId),
            }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:pencil' })) }),
          default: () => '编辑',
        }),
        h(NTooltip, {}, {
          trigger: () =>
            h(NButton, {
              'aria-label': locked ? '解锁账号' : '锁定账号',
              circle: true,
              quaternary: true,
              size: 'small',
              type: 'warning',
              onClick: () => toggleLock(row),
            }, {
              icon: () => h(NIcon, null, () => h(Icon, { icon: locked ? 'lucide:lock-open' : 'lucide:lock' })),
            }),
          default: () => (locked ? '解锁' : '锁定'),
        }),
        h(NTooltip, {}, {
          trigger: () =>
            h(NButton, {
              'aria-label': '强制下线',
              circle: true,
              quaternary: true,
              size: 'small',
              type: 'info',
              disabled: !meta?.online,
              onClick: () => forceLogout(row),
            }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:log-out' })) }),
          default: () => '强制下线',
        }),
        h(NTooltip, {}, {
          trigger: () =>
            h(NButton, {
              'aria-label': '删除用户',
              circle: true,
              quaternary: true,
              size: 'small',
              type: 'error',
              disabled: row.isSystemAccount,
              onClick: () => openDelete(row),
            }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:trash-2' })) }),
          default: () => '删除',
        }),
      ])
    },
  },
])
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full user-page">
    <!-- 筛选 -->
    <div class="xh-query-panel mb-2">
      <NInput
        v-model:value="filterSearch"
        placeholder="搜索用户名、姓名、邮箱…"
        clearable
        size="small"
        style="flex: 1; min-width: 200px; max-width: 360px"
        @keyup.enter="applyFilter"
      />
      <NSelect
        v-model:value="filterStatus"
        :options="statusFilterOptions"
        placeholder="全部状态"
        clearable
        size="small"
        style="width: 108px"
        @update:value="applyFilter"
      />
      <NSelect
        v-model:value="filterGender"
        :options="genderFilterOptions"
        placeholder="全部性别"
        clearable
        size="small"
        style="width: 96px"
        @update:value="applyFilter"
      />
      <NSelect
        v-model:value="filterDeptId"
        :options="deptFilterOptions"
        placeholder="全部部门"
        clearable
        size="small"
        style="width: 120px"
        @update:value="applyFilter"
      />
      <NButton size="small" @click="resetFilter">
        <template #icon>
          <NIcon><Icon icon="tabler:rotate" /></NIcon>
        </template>
        重置
      </NButton>
    </div>

    <!-- 列表 -->
    <NCard
      :bordered="false"
      class="flex-1"
      content-style="padding:0;display:flex;flex-direction:column;height:100%;"
      style="height: 0"
    >
      <div class="list-hd">
        <NButton type="primary" size="small" @click="openCreate">
          <template #icon>
            <NIcon><Icon icon="tabler:plus" /></NIcon>
          </template>
          新建用户
        </NButton>
      </div>

      <div v-if="checkedRowKeys.length" class="batch-bar">
        <span>
          已选
          <strong>{{ checkedRowKeys.length }}</strong>
          项
        </span>
        <NButton text size="tiny" @click="clearChecked">取消选择</NButton>
      </div>

      <NDataTable
        v-model:checked-row-keys="checkedRowKeys"
        class="user-table"
        :columns="columns"
        :data="dataList"
        :loading="tableLoading"
        :bordered="false"
        :single-line="false"
        size="small"
        striped
        flex-height
        :row-key="(row: UserListItemDto) => row.basicId"
        :scroll-x="2000"
        style="flex: 1"
      >
        <template #empty>
          <div class="empty-hint">
            <Icon icon="tabler:users-group" :size="32" class="empty-hint__ico" />
            暂无匹配用户
          </div>
        </template>
      </NDataTable>

      <div
        style="
          display: flex;
          align-items: center;
          justify-content: space-between;
          padding: 14px 20px;
          border-top: 1px solid var(--n-border-color);
          flex-shrink: 0;
        "
      >
        <div style="font-size: 13px; color: var(--n-text-color-3)">
          共
          <strong>{{ totalCount }}</strong>
          条，第
          <strong>{{ currentPage }}</strong>
          / {{ totalPages }} 页
        </div>
        <NPagination
          size="small"
          :page="currentPage"
          :page-count="totalPages"
          :page-slot="7"
          :page-sizes="[10, 20, 50, 100]"
          :page-size="pageSize"
          show-size-picker
          @update:page="handlePageChange"
          @update:page-size="handlePageSizeChange"
        />
      </div>
    </NCard>

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
                <NSelect v-model:value="userForm.gender" :options="genderFilterOptions" />
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
                <NSelect v-model:value="userForm.status" :options="statusFormOptions" />
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
                      <div class="lbl">账号锁定</div>
                      <div class="sub">锁定后用户无法登录</div>
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
                      <div class="lbl">允许多端登录</div>
                      <div class="sub">关闭后新登录会踢出旧会话</div>
                    </div>
                  </div>
                  <NSwitch v-model:value="userForm.multiLogin" />
                </div>
                <div class="form-row">
                  <div class="form-row-main">
                    <Icon icon="tabler:device-mobile" :size="15" class="form-row-ico" />
                    <div>
                      <div class="lbl">最大登录设备数</div>
                      <div class="sub">0 = 不限</div>
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
              <p class="pick-desc">选择要分配的角色，可多选</p>
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
                  :class="['pick-chip', selRoleIds.includes(r.basicId) ? 'on' : '']"
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
              <p class="pick-desc">选择所属部门，可多选</p>
              <p v-if="selDeptIds.length" class="pick-summary">
                已选
                <strong>{{ selDeptIds.length }}</strong>
                个
              </p>
              <div class="pick-grid">
                <button
                  v-for="d in deptFilterOptions"
                  :key="d.value"
                  type="button"
                  :class="['pick-chip', selDeptIds.includes(d.value) ? 'on' : '']"
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
          <NButton size="small" @click="closeModals">取消</NButton>
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
            <div class="det-sub">@{{ detUser.userName }}</div>
          </div>
        </div>
      </template>

      <div v-if="detailLoading" class="modal-loading">加载中…</div>
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
          <span v-for="t in detUser.badges" :key="t.label" :class="['bdg', t.cls]">
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
            <div v-for="m in detUser.metrics" :key="m.label" :class="['det-stat-card', m.cls]">
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
            <div class="session-sub">{{ detUser.lastLoginIp }} · {{ detUser.lastLoginTime }}</div>
          </div>
          <span class="bdg bdg-ok">在线</span>
        </div>
        <div v-else class="session-empty">暂无活跃会话</div>
      </template>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="closeModals">关闭</NButton>
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
          <p class="del-desc">此操作为软删除，保留审计记录；删除前将吊销该用户全部会话。</p>
        </div>
      </div>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="closeModals">取消</NButton>
          <NButton size="small" type="error" @click="confirmDelete">确认删除</NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>

<style scoped>
/* 表格单元格最多两行，避免行高过大 */
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

.tbl-text-2l {
  display: -webkit-box;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
  overflow: hidden;
  font-size: 12px;
  line-height: 1.4;
  color: var(--n-text-color);
  word-break: break-all;
}

.tbl-cell-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  align-items: center;
  max-height: calc(1.4em * 2 + 4px);
  overflow: hidden;
}

.tbl-sec-ico {
  display: inline-flex;
  line-height: 0;
}

.tbl-sec-ico--warn :deep(svg) {
  color: var(--n-warning-color);
}

.tbl-sec-ico--muted :deep(svg) {
  color: var(--n-text-color-3);
}

.tbl-sec-ico--ok :deep(svg) {
  color: var(--n-success-color);
}

.tbl-sec-ico--info :deep(svg) {
  color: var(--n-info-color);
}

.user-page :deep(.user-table .n-data-table-tbody .n-data-table-td) {
  vertical-align: middle;
  padding-top: 6px;
  padding-bottom: 6px;
}

/* 图标语义色：勿加 .user-page 前缀，弹窗 Teleport 到 body 后不在该子树内 */
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

.empty-hint__ico {
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

.list-hd {
  display: flex;
  align-items: center;
  padding: 12px 16px;
  flex-shrink: 0;
}

.batch-bar {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  margin: 0 0 4px;
  font-size: 12px;
  color: var(--n-text-color-2);
  background: var(--n-info-color-suppl);
  border-top: 1px solid var(--n-border-color);
  border-bottom: 1px solid var(--n-border-color);
}

.batch-bar strong {
  color: var(--n-info-color);
  font-weight: 600;
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

.empty-hint {
  text-align: center;
  padding: 48px 0;
  color: var(--n-text-color-3);
}

.empty-hint__ico {
  display: block;
  margin: 0 auto 8px;
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

.form-row-block {
  display: block;
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

.session-ico {
  color: var(--n-text-color-3);
  flex-shrink: 0;
}

.del-body {
  display: flex;
  gap: 12px;
  align-items: flex-start;
}

.del-icon {
  color: var(--n-warning-color);
  flex-shrink: 0;
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
</style>
