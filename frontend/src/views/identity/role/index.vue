<script setup lang="ts">
import type { TreeSelectOption } from 'naive-ui'
import type {
  ApiId,
  DepartmentTreeNodeDto,
  MenuListItemDto,
  PageResult,
  PermissionListItemDto,
  RoleCreateDto,
  RoleDataScopeListItemDto,
  RoleListItemDto,
  RoleManagementDetailDto,
  RolePermissionListItemDto,
  RoleUpdateDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NCheckbox,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NEmpty,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NScrollbar,
  NSelect,
  NSpace,
  NSpin,
  NSwitch,
  NTabPane,
  NTabs,
  NTag,
  NTree,
  NTreeSelect,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import {
  createPageRequest,
  DataPermissionScope,
  departmentApi,
  EnableStatus,
  menuApi,
  PermissionAction,
  permissionApi,
  roleDataScopeApi,
  roleManagementApi,
  rolePermissionApi,
  RoleType,
  ValidityStatus,
} from '@/api'
import { Icon, SchemaPage } from '~/components'
import {
  DATA_SCOPE_OPTIONS,
  PERMISSION_ACTION_OPTIONS,
  ROLE_TYPE_OPTIONS,
  STATUS_OPTIONS,
  VALIDITY_STATUS_OPTIONS,
} from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemRolePage' })

interface RoleFormModel extends RoleCreateDto {
  basicId?: RoleListItemDto['basicId']
}

const message = useMessage()

const statusOptions = STATUS_OPTIONS
const roleTypeOptions = ROLE_TYPE_OPTIONS
const dataScopeOptions = DATA_SCOPE_OPTIONS
const validityStatusOptions = VALIDITY_STATUS_OPTIONS
const permissionActionOptions = PERMISSION_ACTION_OPTIONS

const globalOptions = [
  { label: '全局角色', value: 1 },
  { label: '租户角色', value: 0 },
]

const maintainableRoleTypeOptions = [
  { label: '业务角色', value: RoleType.Business },
  { label: '自定义角色', value: RoleType.Custom },
]

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadRole() {
  void schemaPageRef.value?.reload()
}

// ── 过滤值清洗辅助 ──────────────────────────────────────────────
function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toBool(v: unknown): boolean | undefined {
  if (v == null || v === '') {
    return undefined
  }
  return Number(v) === 1
}

function canMaintainRole(row: RoleListItemDto) {
  return !row.isGlobal && row.roleType !== RoleType.System
}

// ── 字段单一事实源：列 + 搜索 ───────────────────────────────────
const fields: ListFieldSchema[] = [
  // 仅搜索（不展示）
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索角色名称/编码', width: 240, order: 0 },
  { key: 'roleName', title: '角色名称', dataType: 'string', sortable: true, minWidth: 150, order: 1 },
  { key: 'roleCode', title: '角色编码', dataType: 'string', minWidth: 150, order: 2 },
  { key: 'roleDescription', title: '描述', dataType: 'string', minWidth: 220, order: 3 },
  {
    key: 'roleType',
    title: '角色类型',
    dataType: 'enum',
    searchable: true,
    options: roleTypeOptions,
    searchPlaceholder: '角色类型',
    minWidth: 110,
    order: 4,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(roleTypeOptions, (row as unknown as RoleListItemDto).roleType)),
  },
  {
    key: 'isGlobal',
    title: '全局',
    dataType: 'boolean',
    searchable: true,
    options: globalOptions,
    searchPlaceholder: '全局',
    width: 82,
    order: 5,
    render: row => h(NTag, { size: 'small', round: true, type: (row as unknown as RoleListItemDto).isGlobal ? 'warning' : 'default', bordered: false }, () => (row as unknown as RoleListItemDto).isGlobal ? '是' : '否'),
  },
  {
    key: 'dataScope',
    title: '数据范围',
    dataType: 'enum',
    searchable: true,
    options: dataScopeOptions,
    searchPlaceholder: '数据范围',
    minWidth: 130,
    order: 6,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(dataScopeOptions, (row as unknown as RoleListItemDto).dataScope)),
  },
  { key: 'maxMembers', title: '成员上限', dataType: 'number', minWidth: 100, order: 7 },
  { key: 'sort', title: '排序', dataType: 'number', sortable: true, minWidth: 80, order: 8 },
  {
    key: 'status',
    title: '状态',
    dataType: 'enum',
    searchable: true,
    options: statusOptions,
    searchPlaceholder: '状态',
    width: 82,
    order: 9,
    render: row => h(NTag, { size: 'small', round: true, type: (row as unknown as RoleListItemDto).status === EnableStatus.Enabled ? 'success' : 'error', bordered: false }, () => (row as unknown as RoleListItemDto).status === EnableStatus.Enabled ? '启用' : '禁用'),
  },
  {
    key: 'createdTime',
    title: '创建时间',
    dataType: 'datetime',
    sortable: true,
    minWidth: 170,
    order: 10,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate((row as unknown as RoleListItemDto).createdTime)),
  },
]

// ── 资源适配器：归一化查询参数 → 后端 API ──────────────────────
const schema: PageSchema = {
  pageCode: 'system.role',
  exportPermission: 'saas:role:export',
  pageName: '角色管理',
  rowKey: 'basicId',
  scrollX: 1600,
  fields,
  resource: {
    page: (params) => {
      const f = params.filters
      return roleManagementApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(f.keyword) ?? null,
        // RoleType / DataPermissionScope / EnableStatus 均为后端字符串枚举，原样透传即可
        roleType: (f.roleType as RoleType | undefined) || undefined,
        dataScope: (f.dataScope as DataPermissionScope | undefined) || undefined,
        isGlobal: toBool(f.isGlobal),
        status: (f.status as EnableStatus | undefined) || undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => roleManagementApi.delete(id),
  },
  actions: [
    { key: 'create', title: '新增角色', scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'view', title: '查看详情', scope: 'row' },
    { key: 'edit', title: '编辑', scope: 'row', visible: row => canMaintainRole(row as unknown as RoleListItemDto) },
    { key: 'assignPermission', title: '权限分配', scope: 'row' },
    { key: 'assignMenu', title: '菜单授权', scope: 'row' },
    { key: 'assignDataScope', title: '数据范围', scope: 'row' },
    { key: 'toggle', title: '启用/停用', scope: 'row', visible: row => canMaintainRole(row as unknown as RoleListItemDto) },
    { key: 'delete', title: '删除', scope: 'row', visible: row => canMaintainRole(row as unknown as RoleListItemDto) },
  ],
}

// ── 行/页面操作分发 ─────────────────────────────────────────────
function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as RoleListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'view':
      if (row) {
        void handleView(row)
      }
      break
    case 'edit':
      if (row) {
        handleEdit(row)
      }
      break
    case 'toggle':
      if (row) {
        void handleToggleStatus(row)
      }
      break
    case 'delete':
      if (row) {
        void handleDelete(row)
      }
      break
    case 'assignPermission':
      if (row) {
        void openPermissionDrawer(row)
      }
      break
    case 'assignMenu':
      if (row) {
        void openMenuDrawer(row)
      }
      break
    case 'assignDataScope':
      if (row) {
        void openScopeDrawer(row)
      }
      break
  }
}

// ── 权限分配抽屉 ────────────────────────────────────────────────
const permissionVisible = ref(false)
const permissionRole = ref<RoleListItemDto | null>(null)
const permCatalog = ref<PermissionListItemDto[]>([])
const permGrants = ref<RolePermissionListItemDto[]>([])
const permLoading = ref(false)
const permKeyword = ref('')
const permTogglingId = ref<ApiId | null>(null)

/** permissionId → 授权记录（收权时取记录主键） */
const permGrantByPermissionId = computed(() => {
  const map = new Map<ApiId, RolePermissionListItemDto>()
  for (const grant of permGrants.value) {
    map.set(grant.permissionId, grant)
  }
  return map
})

const permFiltered = computed(() => {
  const kw = permKeyword.value.trim().toLowerCase()
  if (!kw) {
    return permCatalog.value
  }
  return permCatalog.value.filter(p =>
    p.permissionName.toLowerCase().includes(kw) || p.permissionCode.toLowerCase().includes(kw),
  )
})

/** 按资源（其次模块）分组 */
const permGroups = computed(() => {
  const map = new Map<string, PermissionListItemDto[]>()
  for (const permission of permFiltered.value) {
    const group = permission.resourceName || permission.moduleCode || '其他'
    const list = map.get(group)
    if (list) {
      list.push(permission)
    }
    else {
      map.set(group, [permission])
    }
  }
  return [...map.entries()].map(([name, items]) => ({ name, items }))
})

/** 权限目录翻页拉全集（后端 pageSize 受夹紧，按页计数停止） */
async function loadPermCatalog() {
  if (permCatalog.value.length > 0) {
    return
  }
  const all: PermissionListItemDto[] = []
  for (let page = 1; page <= 50; page++) {
    const result = await permissionApi.page(createPageRequest({ page: { pageIndex: page, pageSize: 100 } }))
    const items = result.items ?? []
    if (items.length === 0) {
      break
    }
    all.push(...items)
    if (all.length >= (result.page?.totalCount ?? all.length)) {
      break
    }
  }
  permCatalog.value = all
}

async function openPermissionDrawer(row: RoleListItemDto) {
  permissionRole.value = row
  permissionVisible.value = true
  permKeyword.value = ''
  permLoading.value = true
  try {
    const [, grantsResult] = await Promise.all([loadPermCatalog(), rolePermissionApi.list(row.basicId)])
    permGrants.value = grantsResult
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '加载权限失败')
  }
  finally {
    permLoading.value = false
  }
}

async function togglePermission(permission: PermissionListItemDto, checked: boolean) {
  if (!permissionRole.value || permTogglingId.value != null) {
    return
  }
  permTogglingId.value = permission.basicId
  try {
    if (checked) {
      await rolePermissionApi.grant({
        roleId: permissionRole.value.basicId,
        permissionId: permission.basicId,
        permissionAction: PermissionAction.Grant,
      })
      message.success(`已授权：${permission.permissionName}`)
    }
    else {
      const grant = permGrantByPermissionId.value.get(permission.basicId)
      if (grant) {
        await rolePermissionApi.revoke(grant.basicId)
        message.success(`已收回：${permission.permissionName}`)
      }
    }
    permGrants.value = await rolePermissionApi.list(permissionRole.value.basicId)
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '操作失败')
  }
  finally {
    permTogglingId.value = null
  }
}

// ── 菜单授权抽屉（菜单关联权限，勾选即授予该权限） ───────────────
interface MenuNode {
  basicId: ApiId
  menuName: string
  permissionId?: ApiId | null
  // 叶子节点省略 children（undefined），NTree 据此判定为末节点、不显示展开箭头
  children?: MenuNode[]
}

const menuVisible = ref(false)
const menuRole = ref<RoleListItemDto | null>(null)
const menuTreeData = ref<MenuNode[]>([])
const menuGrants = ref<RolePermissionListItemDto[]>([])
const menuCheckedKeys = ref<ApiId[]>([])
const menuLoading = ref(false)
/** 是否有未保存的勾选改动（统一保存模式） */
const menuDirty = ref(false)

/** 菜单 ID → 关联权限 ID（仅含已配置权限的菜单，保存时按勾选计算目标权限集） */
const menuPermIdById = computed(() => {
  const map = new Map<ApiId, ApiId>()
  const walk = (nodes: MenuNode[]) => {
    for (const node of nodes) {
      if (node.permissionId != null) {
        map.set(node.basicId, node.permissionId)
      }
      walk(node.children ?? [])
    }
  }
  walk(menuTreeData.value)
  return map
})

/** 已授权权限对应的菜单节点设为勾选；目录在其所有可授权后代均已授权时一并勾选 */
function deriveMenuChecked() {
  // 仅「有效」的授权才算已勾选（撤销为软删除 Status=Invalid，需排除，否则撤销后仍显示勾选）
  const granted = new Set(
    menuGrants.value
      .filter(grant => grant.status === ValidityStatus.Valid)
      .map(grant => grant.permissionId),
  )
  const checked: ApiId[] = []
  function visit(node: MenuNode): { hasGrantable: boolean, allGranted: boolean } {
    let hasGrantable = false
    let allGranted = true
    if (node.permissionId != null) {
      hasGrantable = true
      if (granted.has(node.permissionId)) {
        checked.push(node.basicId)
      }
      else {
        allGranted = false
      }
    }
    for (const child of node.children ?? []) {
      const result = visit(child)
      if (result.hasGrantable) {
        hasGrantable = true
        if (!result.allGranted) {
          allGranted = false
        }
      }
    }
    // 目录（无关联权限）：其下所有可授权菜单均已授权时，目录也显示勾选
    if (node.permissionId == null && hasGrantable && allGranted) {
      checked.push(node.basicId)
    }
    return { hasGrantable, allGranted }
  }
  for (const root of menuTreeData.value) {
    visit(root)
  }
  menuCheckedKeys.value = checked
}

/** 收集指定菜单节点（含自身）子树内所有节点 ID（目录勾选时级联其下全部子节点） */
function collectSubtreeMenuIds(menuKey: string): ApiId[] {
  const ids: ApiId[] = []
  function collect(node: MenuNode) {
    ids.push(node.basicId)
    for (const child of node.children ?? []) {
      collect(child)
    }
  }
  function locate(nodes: MenuNode[]): boolean {
    for (const node of nodes) {
      if (String(node.basicId) === menuKey) {
        collect(node)
        return true
      }
      if (locate(node.children ?? [])) {
        return true
      }
    }
    return false
  }
  locate(menuTreeData.value)
  return ids
}

function buildMenuTree(flat: MenuListItemDto[]): MenuNode[] {
  const byId = new Map<ApiId, MenuNode>()
  const roots: MenuNode[] = []
  const sorted = [...flat].sort((a, b) => a.sort - b.sort)
  for (const item of sorted) {
    byId.set(item.basicId, {
      basicId: item.basicId,
      menuName: item.menuName,
      permissionId: item.permissionId,
      children: [],
    })
  }
  for (const item of sorted) {
    const node = byId.get(item.basicId)!
    const parent = item.parentId != null ? byId.get(item.parentId) : undefined
    if (parent) {
      (parent.children ??= []).push(node)
    }
    else {
      roots.push(node)
    }
  }
  // 末节点的 children 置为 undefined（而非空数组），使 NTree 视其为叶子、不显示展开箭头
  const prune = (nodes: MenuNode[]) => {
    for (const node of nodes) {
      if (node.children && node.children.length > 0) {
        prune(node.children)
      }
      else {
        node.children = undefined
      }
    }
  }
  prune(roots)
  return roots
}

async function loadAllMenus(): Promise<MenuListItemDto[]> {
  return [...await menuApi.list()]
}

async function openMenuDrawer(row: RoleListItemDto) {
  menuRole.value = row
  menuVisible.value = true
  menuLoading.value = true
  try {
    const [flat, grants] = await Promise.all([
      loadAllMenus(),
      rolePermissionApi.list(row.basicId),
    ])
    menuTreeData.value = buildMenuTree(flat)
    menuGrants.value = grants
    deriveMenuChecked()
    menuDirty.value = false
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '加载菜单失败')
  }
  finally {
    menuLoading.value = false
  }
}

/** 勾选变更仅更新本地状态（目录级联其整个子树），点「保存授权」后统一提交差异 */
function onMenuCheck(keys: Array<string | number>) {
  if (menuLoading.value) {
    return
  }
  const nextKeys = keys.map(String)
  const prevKeys = menuCheckedKeys.value.map(String)
  const added = nextKeys.filter(key => !prevKeys.includes(key))
  const removed = prevKeys.filter(key => !nextKeys.includes(key))
  const changedKey = added[0] ?? removed[0]
  if (changedKey == null) {
    return
  }
  const subtreeIds = collectSubtreeMenuIds(changedKey).map(String)
  const set = new Set(prevKeys)
  if (added.length > 0) {
    for (const id of subtreeIds) {
      set.add(id)
    }
  }
  else {
    for (const id of subtreeIds) {
      set.delete(id)
    }
  }
  menuCheckedKeys.value = [...set]
  menuDirty.value = true
}

/** 统一保存：按当前勾选计算目标权限集，与已授权对比，批量授权新增、收回移除 */
async function saveMenuGrants() {
  const role = menuRole.value
  if (!role || menuLoading.value) {
    return
  }
  const checkedSet = new Set(menuCheckedKeys.value.map(String))
  const targetPermIds = new Set<ApiId>()
  for (const [menuId, permId] of menuPermIdById.value) {
    if (checkedSet.has(String(menuId))) {
      targetPermIds.add(permId)
    }
  }
  // 仅基于「有效」授权计算差异：已生效的才算已授权，撤销也只撤有效项
  const validGrants = menuGrants.value.filter(grant => grant.status === ValidityStatus.Valid)
  const grantedPermIds = new Set(validGrants.map(grant => grant.permissionId))
  const toGrant = [...targetPermIds].filter(permId => !grantedPermIds.has(permId))
  const toRevoke = validGrants.filter(grant => !targetPermIds.has(grant.permissionId))
  if (toGrant.length === 0 && toRevoke.length === 0) {
    message.info('授权无变化')
    menuDirty.value = false
    return
  }
  menuLoading.value = true
  try {
    // 一次性提交本次授权改动（单请求、后端单事务）
    await rolePermissionApi.batchUpdate({
      roleId: role.basicId,
      grantPermissionIds: toGrant,
      revokeRolePermissionIds: toRevoke.map(grant => grant.basicId),
    })
    menuGrants.value = await rolePermissionApi.list(role.basicId)
    deriveMenuChecked()
    menuDirty.value = false
    message.success(`已保存（授权 ${toGrant.length} 项，收回 ${toRevoke.length} 项）`)
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '保存失败')
  }
  finally {
    menuLoading.value = false
  }
}

// ── 数据范围抽屉（按部门授予角色数据范围） ──────────────────────
const scopeVisible = ref(false)
const scopeRole = ref<RoleListItemDto | null>(null)
const scopeGrants = ref<RoleDataScopeListItemDto[]>([])
const scopeDeptOptions = ref<TreeSelectOption[]>([])
const scopeSelectedDept = ref<ApiId | null>(null)
const scopeIncludeChildren = ref(true)
const scopeLoading = ref(false)
const scopeSubmitting = ref(false)

function toDeptOptions(nodes: DepartmentTreeNodeDto[]): TreeSelectOption[] {
  return nodes.map(node => ({
    key: node.basicId,
    label: node.departmentName,
    children: node.children?.length ? toDeptOptions(node.children) : undefined,
  }))
}

async function openScopeDrawer(row: RoleListItemDto) {
  scopeRole.value = row
  scopeVisible.value = true
  scopeSelectedDept.value = null
  scopeIncludeChildren.value = true
  scopeLoading.value = true
  try {
    const [tree, grants] = await Promise.all([
      departmentApi.tree({ limit: 1000 }),
      roleDataScopeApi.list(row.basicId),
    ])
    scopeDeptOptions.value = toDeptOptions(tree)
    scopeGrants.value = grants
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '加载数据范围失败')
  }
  finally {
    scopeLoading.value = false
  }
}

async function addScope() {
  if (!scopeRole.value || scopeSelectedDept.value == null) {
    message.warning('请先选择部门')
    return
  }
  scopeSubmitting.value = true
  try {
    await roleDataScopeApi.grant({
      roleId: scopeRole.value.basicId,
      departmentId: scopeSelectedDept.value,
      includeChildren: scopeIncludeChildren.value,
    })
    message.success('已添加数据范围')
    scopeSelectedDept.value = null
    scopeGrants.value = await roleDataScopeApi.list(scopeRole.value.basicId)
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '添加失败')
  }
  finally {
    scopeSubmitting.value = false
  }
}

async function removeScope(grant: RoleDataScopeListItemDto) {
  if (!scopeRole.value) {
    return
  }
  try {
    await roleDataScopeApi.revoke(grant.basicId)
    message.success('已移除')
    scopeGrants.value = await roleDataScopeApi.list(scopeRole.value.basicId)
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '移除失败')
  }
}

// ── 表单 / 详情（保留页面自有逻辑） ─────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<RoleManagementDetailDto | null>(null)
const roleForm = ref<RoleFormModel>(createDefaultRoleForm())

const modalTitle = computed(() => (roleForm.value.basicId ? '编辑角色' : '新增角色'))

function createDefaultRoleForm(): RoleFormModel {
  return {
    dataScope: DataPermissionScope.SelfOnly,
    maxMembers: 0,
    remark: null,
    roleCode: '',
    roleDescription: null,
    roleName: '',
    roleType: RoleType.Custom,
    sort: 100,
    status: EnableStatus.Enabled,
  }
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
  return value ? '是' : '否'
}

function formatStatus(value?: EnableStatus | null) {
  return getOptionLabel(statusOptions, value)
}

function formatValidityStatus(value?: ValidityStatus | null) {
  return getOptionLabel(validityStatusOptions, value)
}

function handleAdd() {
  editingStatus.value = null
  roleForm.value = createDefaultRoleForm()
  modalVisible.value = true
}

function handleEdit(row: RoleListItemDto) {
  editingStatus.value = row.status
  roleForm.value = {
    basicId: row.basicId,
    dataScope: row.dataScope,
    maxMembers: row.maxMembers,
    remark: null,
    roleCode: row.roleCode,
    roleDescription: row.roleDescription,
    roleName: row.roleName,
    roleType: row.roleType,
    sort: row.sort,
    status: row.status,
  }
  modalVisible.value = true
}

async function handleView(row: RoleListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null

  try {
    currentDetail.value = await roleManagementApi.detailView(row.basicId)
    if (!currentDetail.value) {
      message.warning('未查询到角色详情')
    }
  }
  catch {
    message.error('加载角色详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

function validateRoleForm() {
  if (!roleForm.value.roleName.trim()) {
    message.warning('请输入角色名称')
    return false
  }

  if (!roleForm.value.basicId && !roleForm.value.roleCode.trim()) {
    message.warning('请输入角色编码')
    return false
  }

  return true
}

async function handleSubmit() {
  if (!validateRoleForm()) {
    return
  }

  submitLoading.value = true

  try {
    if (roleForm.value.basicId) {
      const updateInput: RoleUpdateDto = {
        basicId: roleForm.value.basicId,
        dataScope: roleForm.value.dataScope,
        maxMembers: roleForm.value.maxMembers,
        remark: roleForm.value.remark,
        roleDescription: roleForm.value.roleDescription,
        roleName: roleForm.value.roleName.trim(),
        roleType: roleForm.value.roleType,
        sort: roleForm.value.sort,
      }

      await roleManagementApi.update(updateInput)
      if (editingStatus.value !== roleForm.value.status) {
        await roleManagementApi.updateStatus({
          basicId: roleForm.value.basicId,
          remark: roleForm.value.remark,
          status: roleForm.value.status,
        })
      }
    }
    else {
      const createInput: RoleCreateDto = {
        dataScope: roleForm.value.dataScope,
        maxMembers: roleForm.value.maxMembers,
        remark: roleForm.value.remark,
        roleCode: roleForm.value.roleCode.trim(),
        roleDescription: roleForm.value.roleDescription,
        roleName: roleForm.value.roleName.trim(),
        roleType: roleForm.value.roleType,
        sort: roleForm.value.sort,
        status: roleForm.value.status,
      }

      await roleManagementApi.create(createInput)
    }

    message.success('保存成功')
    modalVisible.value = false
    reloadRole()
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: RoleListItemDto) {
  await roleManagementApi.delete(row.basicId)
  message.success('删除成功')
  reloadRole()
}

async function handleToggleStatus(row: RoleListItemDto) {
  await roleManagementApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? '前端停用角色' : '前端启用角色',
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success('状态已更新')
  reloadRole()
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <NDrawer v-model:show="detailVisible" :width="900">
      <NDrawerContent closable title="角色详情">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="xh-detail-empty" description="暂无角色详情">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="currentDetail" style="max-height: calc(100vh - 120px)">
            <NTabs animated type="line">
              <NTabPane name="overview" tab="概览">
                <NDescriptions :column="2" bordered size="small">
                  <NDescriptionsItem label="角色名称">
                    {{ currentDetail.role.roleName }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="角色编码">
                    {{ currentDetail.role.roleCode }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="角色类型">
                    {{ getOptionLabel(roleTypeOptions, currentDetail.role.roleType) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="数据范围">
                    {{ getOptionLabel(dataScopeOptions, currentDetail.role.dataScope) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="全局角色">
                    {{ formatBoolean(currentDetail.role.isGlobal) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="状态">
                    {{ formatStatus(currentDetail.role.status) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="成员上限">
                    {{ currentDetail.role.maxMembers }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="排序">
                    {{ currentDetail.role.sort }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="描述">
                    {{ formatNullable(currentDetail.role.roleDescription) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="备注">
                    {{ formatNullable(currentDetail.role.remark) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="创建时间">
                    {{ formatNullableDate(currentDetail.role.createdTime) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="聚合时间">
                    {{ formatNullableDate(currentDetail.generatedTime) }}
                  </NDescriptionsItem>
                </NDescriptions>
              </NTabPane>

              <NTabPane name="permissions" :tab="`权限 (${currentDetail.permissions.length})`">
                <table v-if="currentDetail.permissions.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>权限</th>
                      <th>编码</th>
                      <th>动作</th>
                      <th>状态</th>
                      <th>有效期</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.permissions" :key="item.basicId">
                      <td>{{ formatNullable(item.permissionName) }}</td>
                      <td>{{ formatNullable(item.permissionCode) }}</td>
                      <td>{{ getOptionLabel(permissionActionOptions, item.permissionAction) }}</td>
                      <td>{{ formatValidityStatus(item.status) }}</td>
                      <td>{{ formatNullableDate(item.effectiveTime) }} 至 {{ formatNullableDate(item.expirationTime) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无权限分配" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="dataScopes" :tab="`数据范围 (${currentDetail.dataScopes.length})`">
                <table v-if="currentDetail.dataScopes.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>部门</th>
                      <th>编码</th>
                      <th>包含子部门</th>
                      <th>状态</th>
                      <th>有效期</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.dataScopes" :key="item.basicId">
                      <td>{{ formatNullable(item.departmentName) }}</td>
                      <td>{{ formatNullable(item.departmentCode) }}</td>
                      <td>{{ formatBoolean(item.includeChildren) }}</td>
                      <td>{{ formatValidityStatus(item.status) }}</td>
                      <td>{{ formatNullableDate(item.effectiveTime) }} 至 {{ formatNullableDate(item.expirationTime) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无角色数据范围" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="ancestors" :tab="`祖先链 (${currentDetail.ancestors.length})`">
                <table v-if="currentDetail.ancestors.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>上级角色</th>
                      <th>编码</th>
                      <th>深度</th>
                      <th>状态</th>
                      <th>路径</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.ancestors" :key="item.basicId">
                      <td>{{ formatNullable(item.ancestorRoleName) }}</td>
                      <td>{{ formatNullable(item.ancestorRoleCode) }}</td>
                      <td>{{ item.depth }}</td>
                      <td>{{ formatStatus(item.ancestorStatus) }}</td>
                      <td>{{ formatNullable(item.path) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无祖先角色" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="descendants" :tab="`后代链 (${currentDetail.descendants.length})`">
                <table v-if="currentDetail.descendants.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>下级角色</th>
                      <th>编码</th>
                      <th>深度</th>
                      <th>状态</th>
                      <th>路径</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.descendants" :key="item.basicId">
                      <td>{{ formatNullable(item.descendantRoleName) }}</td>
                      <td>{{ formatNullable(item.descendantRoleCode) }}</td>
                      <td>{{ item.depth }}</td>
                      <td>{{ formatStatus(item.descendantStatus) }}</td>
                      <td>{{ formatNullable(item.path) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无后代角色" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="grantedUsers" :tab="`授权用户 (${currentDetail.grantedUsers.length})`">
                <table v-if="currentDetail.grantedUsers.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>用户</th>
                      <th>状态</th>
                      <th>已过期</th>
                      <th>授权原因</th>
                      <th>有效期</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.grantedUsers" :key="item.basicId">
                      <td>{{ formatNullable(item.realName || item.nickName || item.userName) }}</td>
                      <td>{{ formatValidityStatus(item.status) }}</td>
                      <td>{{ formatBoolean(item.isExpired) }}</td>
                      <td>{{ formatNullable(item.grantReason) }}</td>
                      <td>{{ formatNullableDate(item.effectiveTime) }} 至 {{ formatNullableDate(item.expirationTime) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无授权用户" style="padding: 40px 0" />
              </NTabPane>
            </NTabs>
          </NScrollbar>
        </NSpin>
      </NDrawerContent>
    </NDrawer>

    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 680px; max-width: 92vw"
    >
      <NForm :model="roleForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="角色名称" path="roleName">
          <NInput v-model:value="roleForm.roleName" clearable placeholder="请输入角色名称" />
        </NFormItem>
        <NFormItem label="角色编码" path="roleCode">
          <NInput
            v-model:value="roleForm.roleCode"
            clearable
            :disabled="Boolean(roleForm.basicId)"
            placeholder="如: business_admin"
          />
        </NFormItem>
        <NFormItem label="角色类型" path="roleType">
          <NSelect v-model:value="roleForm.roleType" :options="maintainableRoleTypeOptions" />
        </NFormItem>
        <NFormItem label="数据范围" path="dataScope">
          <NSelect v-model:value="roleForm.dataScope" :options="dataScopeOptions" />
        </NFormItem>
        <NFormItem label="成员上限" path="maxMembers">
          <NInputNumber v-model:value="roleForm.maxMembers" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="roleForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="roleForm.status" :options="statusOptions" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="roleForm.remark" clearable placeholder="请输入备注" />
        </NFormItem>
        <NFormItem label="描述" path="roleDescription">
          <NInput
            v-model:value="roleForm.roleDescription"
            clearable
            placeholder="请输入角色描述"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            保存
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <NDrawer v-model:show="permissionVisible" :width="760">
      <NDrawerContent closable :title="`权限分配 · ${permissionRole?.roleName ?? ''}`">
        <div class="perm-toolbar">
          <NInput v-model:value="permKeyword" clearable placeholder="搜索权限名称 / 编码" style="width: 240px" />
          <NTag round type="success" :bordered="false">
            已授权 {{ permGrants.length }} 项
          </NTag>
        </div>
        <NSpin :show="permLoading">
          <NEmpty v-if="permGroups.length === 0 && !permLoading" class="perm-empty" description="无匹配权限" />
          <div v-else class="perm-groups">
            <section v-for="group in permGroups" :key="group.name" class="perm-group">
              <div class="perm-group-head">
                <span>{{ group.name }}</span>
                <span class="perm-group-count">{{ group.items.length }}</span>
              </div>
              <div class="perm-list">
                <label
                  v-for="permission in group.items"
                  :key="String(permission.basicId)"
                  class="perm-item"
                >
                  <NCheckbox
                    :checked="permGrantByPermissionId.has(permission.basicId)"
                    :disabled="permTogglingId === permission.basicId"
                    @update:checked="(checked: boolean) => togglePermission(permission, checked)"
                  />
                  <span class="perm-text">
                    <span class="perm-name">{{ permission.permissionName }}</span>
                    <span class="perm-code">{{ permission.permissionCode }}</span>
                  </span>
                </label>
              </div>
            </section>
          </div>
        </NSpin>
      </NDrawerContent>
    </NDrawer>

    <NDrawer v-model:show="menuVisible" :width="520">
      <NDrawerContent closable :title="`菜单授权 · ${menuRole?.roleName ?? ''}`">
        <NSpin :show="menuLoading">
          <NEmpty v-if="menuTreeData.length === 0 && !menuLoading" class="perm-empty" description="暂无菜单" />
          <NTree
            v-else
            block-line
            checkable
            :cascade="false"
            :checked-keys="menuCheckedKeys"
            children-field="children"
            :data="menuTreeData"
            :default-expand-all="false"
            key-field="basicId"
            label-field="menuName"
            :selectable="false"
            @update:checked-keys="onMenuCheck"
          />
        </NSpin>
        <p class="perm-tip">
          勾选菜单授予其关联权限；勾选目录级联其下子菜单。改动需点「保存授权」统一提交。
        </p>
        <template #footer>
          <NButton @click="menuVisible = false">
            取消
          </NButton>
          <NButton type="primary" :loading="menuLoading" :disabled="!menuDirty" style="margin-left: 8px" @click="saveMenuGrants">
            保存授权
          </NButton>
        </template>
      </NDrawerContent>
    </NDrawer>

    <NDrawer v-model:show="scopeVisible" :width="560">
      <NDrawerContent closable :title="`数据范围 · ${scopeRole?.roleName ?? ''}`">
        <div class="scope-add">
          <NTreeSelect
            v-model:value="scopeSelectedDept"
            clearable
            :options="scopeDeptOptions"
            placeholder="选择部门"
            style="flex: 1"
          />
          <NSwitch v-model:value="scopeIncludeChildren">
            <template #checked>
              含子部门
            </template>
            <template #unchecked>
              仅本部门
            </template>
          </NSwitch>
          <NButton :loading="scopeSubmitting" type="primary" @click="addScope">
            添加
          </NButton>
        </div>
        <NSpin :show="scopeLoading">
          <NEmpty v-if="scopeGrants.length === 0 && !scopeLoading" class="perm-empty" description="未配置数据范围" />
          <div v-else class="scope-list">
            <div v-for="grant in scopeGrants" :key="String(grant.basicId)" class="scope-row">
              <span class="scope-dept">{{ grant.departmentName || grant.departmentId }}</span>
              <NTag :bordered="false" size="small" :type="grant.includeChildren ? 'info' : 'default'">
                {{ grant.includeChildren ? '含子部门' : '仅本部门' }}
              </NTag>
              <NButton quaternary size="small" type="error" @click="removeScope(grant)">
                移除
              </NButton>
            </div>
          </div>
        </NSpin>
      </NDrawerContent>
    </NDrawer>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}

.xh-detail-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}

.xh-detail-table th,
.xh-detail-table td {
  padding: 9px 10px;
  border: 1px solid var(--n-border-color);
  text-align: left;
  vertical-align: top;
}

.xh-detail-table th {
  background: var(--n-merged-th-color);
  font-weight: 500;
}

/* 权限分配抽屉 */
.perm-toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 14px;
}

.perm-empty {
  padding: 48px 0;
}

.perm-groups {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.perm-group {
  border: 1px solid var(--n-border-color);
  border-radius: 8px;
  overflow: hidden;
}

.perm-group-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 12px;
  background: var(--n-merged-th-color, rgb(0 0 0 / 0.02));
  font-size: 13px;
  font-weight: 600;
}

.perm-group-count {
  font-size: 12px;
  font-weight: 400;
  opacity: 0.55;
}

.perm-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(230px, 1fr));
  gap: 2px 12px;
  padding: 10px 12px;
}

.perm-item {
  display: flex;
  align-items: center;
  gap: 9px;
  padding: 5px 6px;
  border-radius: 6px;
  cursor: pointer;
}

.perm-item:hover {
  background: rgb(0 0 0 / 0.03);
}

.perm-text {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.perm-name {
  font-size: 13px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.perm-code {
  font-size: 11.5px;
  opacity: 0.6;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 菜单授权提示 */
.perm-tip {
  margin: 14px 0 0;
  font-size: 12px;
  opacity: 0.6;
}

/* 数据范围抽屉 */
.scope-add {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}

.scope-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.scope-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 12px;
  border: 1px solid var(--n-border-color);
  border-radius: 8px;
}

.scope-dept {
  flex: 1;
  font-size: 13px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
