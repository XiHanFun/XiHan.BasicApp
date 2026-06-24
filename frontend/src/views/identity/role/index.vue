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
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  DataPermissionScope,
  departmentApi,
  EnableStatus,
  menuApi,
  PermissionAction,
  permissionApi,
  querySortsFromSchema,
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

const { t } = useI18n()
const message = useMessage()

const statusOptions = STATUS_OPTIONS
const roleTypeOptions = ROLE_TYPE_OPTIONS
const dataScopeOptions = DATA_SCOPE_OPTIONS
const validityStatusOptions = VALIDITY_STATUS_OPTIONS
const permissionActionOptions = PERMISSION_ACTION_OPTIONS

const globalOptions = computed(() => [
  { label: t('identity.role.global_role'), value: 1 },
  { label: t('identity.role.tenant_role'), value: 0 },
])

const maintainableRoleTypeOptions = computed(() => [
  { label: t('identity.role.role_type_business'), value: RoleType.Business },
  { label: t('identity.role.role_type_custom'), value: RoleType.Custom },
])

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
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不展示）
  { key: 'keyword', title: t('identity.role.col_keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('identity.role.keyword_placeholder'), width: 240, order: 0 },
  { key: 'roleName', title: t('identity.role.col_role_name'), dataType: 'string', sortable: true, minWidth: 150, order: 1 },
  { key: 'roleCode', title: t('identity.role.col_role_code'), dataType: 'string', sortable: true, minWidth: 150, order: 2 },
  { key: 'roleDescription', title: t('identity.role.col_description'), dataType: 'string', minWidth: 220, order: 3 },
  {
    key: 'roleType',
    title: t('identity.role.col_role_type'),
    dataType: 'enum',
    sortable: true,
    searchable: true,
    dictionaryCode: 'RoleType',
    options: roleTypeOptions,
    searchPlaceholder: t('identity.role.role_type_placeholder'),
    minWidth: 110,
    order: 4,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(roleTypeOptions, (row as unknown as RoleListItemDto).roleType)),
  },
  {
    key: 'isGlobal',
    title: t('identity.role.col_is_global'),
    dataType: 'boolean',
    searchable: true,
    options: globalOptions.value,
    searchPlaceholder: t('identity.role.is_global_placeholder'),
    width: 82,
    order: 5,
    render: row => h(NTag, { size: 'small', round: true, type: (row as unknown as RoleListItemDto).isGlobal ? 'warning' : 'default', bordered: false }, () => (row as unknown as RoleListItemDto).isGlobal ? t('common.statuses.yes') : t('common.statuses.no')),
  },
  {
    key: 'dataScope',
    title: t('identity.role.col_data_scope'),
    dataType: 'enum',
    sortable: true,
    searchable: true,
    dictionaryCode: 'DataPermissionScope',
    options: dataScopeOptions,
    searchPlaceholder: t('identity.role.data_scope_placeholder'),
    minWidth: 130,
    order: 6,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(dataScopeOptions, (row as unknown as RoleListItemDto).dataScope)),
  },
  { key: 'maxMembers', title: t('identity.role.col_max_members'), dataType: 'number', sortable: true, minWidth: 100, order: 7 },
  { key: 'sort', title: t('identity.role.col_sort'), dataType: 'number', sortable: true, minWidth: 80, order: 8 },
  {
    key: 'status',
    title: t('identity.role.col_status'),
    dataType: 'enum',
    sortable: true,
    searchable: true,
    dictionaryCode: 'EnableStatus',
    options: statusOptions,
    searchPlaceholder: t('identity.role.status_placeholder'),
    width: 82,
    order: 9,
    render: row => h(NTag, { size: 'small', round: true, type: (row as unknown as RoleListItemDto).status === EnableStatus.Enabled ? 'success' : 'error', bordered: false }, () => (row as unknown as RoleListItemDto).status === EnableStatus.Enabled ? t('common.statuses.enabled') : t('common.statuses.disabled')),
  },
  {
    key: 'createdTime',
    title: t('identity.role.col_create_time'),
    dataType: 'datetime',
    sortable: true,
    minWidth: 170,
    order: 10,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate((row as unknown as RoleListItemDto).createdTime)),
  },
])

// ── 资源适配器：归一化查询参数 → 后端 API ──────────────────────
const schema = computed<PageSchema>(() => ({
  pageCode: 'system.role',
  exportPermission: 'saas:role:export',
  pageName: t('identity.role.page_name'),
  batchRemovable: true,
  removePermission: 'saas:role:delete',
  statusPermission: 'saas:role:status',
  rowKey: 'basicId',
  scrollX: 1600,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return roleManagementApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sorts) },
        }),
        keyword: toStr(f.keyword) ?? null,
        // RoleType / DataPermissionScope / EnableStatus 均为后端字符串枚举，原样透传即可
        roleType: (f.roleType as RoleType | undefined) || undefined,
        dataScope: (f.dataScope as DataPermissionScope | undefined) || undefined,
        isGlobal: toBool(f.isGlobal),
        status: (f.status as EnableStatus | undefined) || undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => roleManagementApi.delete(id),
    updateStatus: (id, enabled) => roleManagementApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled, remark: enabled ? t('identity.role.batch_enable_remark') : t('identity.role.batch_disable_remark') }),
  },
  actions: [
    { key: 'create', title: t('identity.role.action_create'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'view', title: t('identity.role.action_view'), scope: 'row' },
    { key: 'edit', title: t('identity.role.action_edit'), scope: 'row', visible: row => canMaintainRole(row as unknown as RoleListItemDto) },
    { key: 'assignPermission', title: t('identity.role.action_assign_permission'), scope: 'row' },
    { key: 'assignMenu', title: t('identity.role.action_assign_menu'), scope: 'row' },
    { key: 'assignDataScope', title: t('identity.role.action_assign_data_scope'), scope: 'row' },
    { key: 'toggle', title: t('identity.role.action_toggle'), scope: 'row', visible: row => canMaintainRole(row as unknown as RoleListItemDto) },
    { key: 'delete', title: t('identity.role.action_delete'), scope: 'row', visible: row => canMaintainRole(row as unknown as RoleListItemDto) },
  ],
}))

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

/** 取权限码的资源段作为分组键：saas:{resource}:{action} → resource */
function permResourceKey(code: string): string {
  const parts = code.split(':')
  return parts.length >= 3 ? parts[1]! : (parts[0] ?? '')
}

/** 一组权限名的公共前缀，作为功能块显示名（如「租户」「权限定义」「角色」） */
function permCommonPrefix(names: string[]): string {
  if (names.length === 0) {
    return ''
  }
  let prefix = names[0]!
  for (const name of names) {
    let i = 0
    while (i < prefix.length && i < name.length && prefix[i] === name[i]) {
      i++
    }
    prefix = prefix.slice(0, i)
    if (!prefix) {
      break
    }
  }
  return prefix
}

/** 按资源（功能块）分组：以权限码资源段为键，组名取该组权限名公共前缀，使每个资源成为独立功能块 */
const permGroups = computed(() => {
  const map = new Map<string, PermissionListItemDto[]>()
  for (const permission of permFiltered.value) {
    // 组码优先用后端定义的 groupCode；缺省回退资源段推导（兼容后端未重建时）
    const key = permission.groupCode || permission.resourceName || permResourceKey(permission.permissionCode) || permission.moduleCode || t('identity.role.perm_group_other')
    const list = map.get(key)
    if (list) {
      list.push(permission)
    }
    else {
      map.set(key, [permission])
    }
  }
  return [...map.entries()].map(([key, items]) => ({
    key,
    name: items[0]?.groupName || permCommonPrefix(items.map(item => item.permissionName)) || key,
    items,
  }))
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
    message.error((e as Error)?.message || t('identity.role.perm_load_failed'))
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
      message.success(t('identity.role.perm_grant_done', { name: permission.permissionName }))
    }
    else {
      const grant = permGrantByPermissionId.value.get(permission.basicId)
      if (grant) {
        await rolePermissionApi.revoke(grant.basicId)
        message.success(t('identity.role.perm_revoke_done', { name: permission.permissionName }))
      }
    }
    permGrants.value = await rolePermissionApi.list(permissionRole.value.basicId)
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('common.messages.operation_failed'))
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
    message.error((e as Error)?.message || t('identity.role.menu_load_failed'))
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
    message.info(t('identity.role.menu_no_change'))
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
    message.success(t('identity.role.menu_saved', { grant: toGrant.length, revoke: toRevoke.length }))
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('common.messages.save_failed'))
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
    message.error((e as Error)?.message || t('identity.role.scope_load_failed'))
  }
  finally {
    scopeLoading.value = false
  }
}

async function addScope() {
  if (!scopeRole.value || scopeSelectedDept.value == null) {
    message.warning(t('identity.role.scope_select_dept_required'))
    return
  }
  scopeSubmitting.value = true
  try {
    await roleDataScopeApi.grant({
      roleId: scopeRole.value.basicId,
      departmentId: scopeSelectedDept.value,
      includeChildren: scopeIncludeChildren.value,
    })
    message.success(t('identity.role.scope_added'))
    scopeSelectedDept.value = null
    scopeGrants.value = await roleDataScopeApi.list(scopeRole.value.basicId)
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('identity.role.scope_add_failed'))
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
    message.success(t('identity.role.scope_removed'))
    scopeGrants.value = await roleDataScopeApi.list(scopeRole.value.basicId)
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('identity.role.scope_remove_failed'))
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

const modalTitle = computed(() => (roleForm.value.basicId ? t('identity.role.form_edit_title') : t('identity.role.form_create_title')))

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
  return value ? t('common.statuses.yes') : t('common.statuses.no')
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
      message.warning(t('identity.role.msg_detail_not_found'))
    }
  }
  catch {
    message.error(t('identity.role.msg_load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

function validateRoleForm() {
  if (!roleForm.value.roleName.trim()) {
    message.warning(t('identity.role.msg_role_name_required'))
    return false
  }

  if (!roleForm.value.basicId && !roleForm.value.roleCode.trim()) {
    message.warning(t('identity.role.msg_role_code_required'))
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

    message.success(t('common.messages.save_success'))
    modalVisible.value = false
    reloadRole()
  }
  catch {
    message.error(t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: RoleListItemDto) {
  await roleManagementApi.delete(row.basicId)
  message.success(t('common.messages.delete_success'))
  reloadRole()
}

async function handleToggleStatus(row: RoleListItemDto) {
  await roleManagementApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? t('identity.role.front_disable_remark') : t('identity.role.front_enable_remark'),
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success(t('common.messages.status_updated'))
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
      <NDrawerContent closable :title="t('identity.role.detail_title')">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="xh-detail-empty" :description="t('identity.role.detail_empty')">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="currentDetail" style="max-height: calc(100vh - 120px)">
            <NTabs animated type="line">
              <NTabPane name="overview" :tab="t('identity.role.tab_overview')">
                <NDescriptions :column="2" bordered size="small">
                  <NDescriptionsItem :label="t('identity.role.label_role_name')">
                    {{ currentDetail.role.roleName }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.role.label_role_code')">
                    {{ currentDetail.role.roleCode }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.role.label_role_type')">
                    {{ getOptionLabel(roleTypeOptions, currentDetail.role.roleType) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.role.label_data_scope')">
                    {{ getOptionLabel(dataScopeOptions, currentDetail.role.dataScope) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.role.label_is_global')">
                    {{ formatBoolean(currentDetail.role.isGlobal) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.role.label_status')">
                    {{ formatStatus(currentDetail.role.status) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.role.label_max_members')">
                    {{ currentDetail.role.maxMembers }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.role.label_sort')">
                    {{ currentDetail.role.sort }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.role.label_description')">
                    {{ formatNullable(currentDetail.role.roleDescription) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.role.label_remark')">
                    {{ formatNullable(currentDetail.role.remark) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.role.label_create_time')">
                    {{ formatNullableDate(currentDetail.role.createdTime) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem :label="t('identity.role.label_generated_time')">
                    {{ formatNullableDate(currentDetail.generatedTime) }}
                  </NDescriptionsItem>
                </NDescriptions>
              </NTabPane>

              <NTabPane name="permissions" :tab="t('identity.role.tab_permissions', { count: currentDetail.permissions.length })">
                <table v-if="currentDetail.permissions.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>{{ t('identity.role.th_permission') }}</th>
                      <th>{{ t('identity.role.th_code') }}</th>
                      <th>{{ t('identity.role.th_action') }}</th>
                      <th>{{ t('identity.role.th_status') }}</th>
                      <th>{{ t('identity.role.th_validity') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.permissions" :key="item.basicId">
                      <td>{{ formatNullable(item.permissionName) }}</td>
                      <td>{{ formatNullable(item.permissionCode) }}</td>
                      <td>{{ getOptionLabel(permissionActionOptions, item.permissionAction) }}</td>
                      <td>{{ formatValidityStatus(item.status) }}</td>
                      <td>{{ t('identity.role.validity_range', { from: formatNullableDate(item.effectiveTime), to: formatNullableDate(item.expirationTime) }) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else :description="t('identity.role.empty_permissions')" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="dataScopes" :tab="t('identity.role.tab_data_scopes', { count: currentDetail.dataScopes.length })">
                <table v-if="currentDetail.dataScopes.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>{{ t('identity.role.th_department') }}</th>
                      <th>{{ t('identity.role.th_code') }}</th>
                      <th>{{ t('identity.role.th_include_children') }}</th>
                      <th>{{ t('identity.role.th_status') }}</th>
                      <th>{{ t('identity.role.th_validity') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.dataScopes" :key="item.basicId">
                      <td>{{ formatNullable(item.departmentName) }}</td>
                      <td>{{ formatNullable(item.departmentCode) }}</td>
                      <td>{{ formatBoolean(item.includeChildren) }}</td>
                      <td>{{ formatValidityStatus(item.status) }}</td>
                      <td>{{ t('identity.role.validity_range', { from: formatNullableDate(item.effectiveTime), to: formatNullableDate(item.expirationTime) }) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else :description="t('identity.role.empty_data_scopes')" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="ancestors" :tab="t('identity.role.tab_ancestors', { count: currentDetail.ancestors.length })">
                <table v-if="currentDetail.ancestors.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>{{ t('identity.role.th_parent_role') }}</th>
                      <th>{{ t('identity.role.th_code') }}</th>
                      <th>{{ t('identity.role.th_depth') }}</th>
                      <th>{{ t('identity.role.th_status') }}</th>
                      <th>{{ t('identity.role.th_path') }}</th>
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
                <NEmpty v-else :description="t('identity.role.empty_ancestors')" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="descendants" :tab="t('identity.role.tab_descendants', { count: currentDetail.descendants.length })">
                <table v-if="currentDetail.descendants.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>{{ t('identity.role.th_child_role') }}</th>
                      <th>{{ t('identity.role.th_code') }}</th>
                      <th>{{ t('identity.role.th_depth') }}</th>
                      <th>{{ t('identity.role.th_status') }}</th>
                      <th>{{ t('identity.role.th_path') }}</th>
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
                <NEmpty v-else :description="t('identity.role.empty_descendants')" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="grantedUsers" :tab="t('identity.role.tab_granted_users', { count: currentDetail.grantedUsers.length })">
                <table v-if="currentDetail.grantedUsers.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>{{ t('identity.role.th_user') }}</th>
                      <th>{{ t('identity.role.th_status') }}</th>
                      <th>{{ t('identity.role.th_expired') }}</th>
                      <th>{{ t('identity.role.th_grant_reason') }}</th>
                      <th>{{ t('identity.role.th_validity') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in currentDetail.grantedUsers" :key="item.basicId">
                      <td>{{ formatNullable(item.realName || item.nickName || item.userName) }}</td>
                      <td>{{ formatValidityStatus(item.status) }}</td>
                      <td>{{ formatBoolean(item.isExpired) }}</td>
                      <td>{{ formatNullable(item.grantReason) }}</td>
                      <td>{{ t('identity.role.validity_range', { from: formatNullableDate(item.effectiveTime), to: formatNullableDate(item.expirationTime) }) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else :description="t('identity.role.empty_granted_users')" style="padding: 40px 0" />
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
        <NFormItem :label="t('identity.role.label_role_name')" path="roleName">
          <NInput v-model:value="roleForm.roleName" clearable :placeholder="t('identity.role.ph_role_name')" />
        </NFormItem>
        <NFormItem :label="t('identity.role.label_role_code')" path="roleCode">
          <NInput
            v-model:value="roleForm.roleCode"
            clearable
            :disabled="Boolean(roleForm.basicId)"
            :placeholder="t('identity.role.ph_role_code')"
          />
        </NFormItem>
        <NFormItem :label="t('identity.role.label_role_type')" path="roleType">
          <NSelect v-model:value="roleForm.roleType" :options="maintainableRoleTypeOptions" />
        </NFormItem>
        <NFormItem :label="t('identity.role.label_data_scope')" path="dataScope">
          <NSelect v-model:value="roleForm.dataScope" :options="dataScopeOptions" />
        </NFormItem>
        <NFormItem :label="t('identity.role.label_max_members')" path="maxMembers">
          <NInputNumber v-model:value="roleForm.maxMembers" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('identity.role.label_sort')" path="sort">
          <NInputNumber v-model:value="roleForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('identity.role.label_status')" path="status">
          <NSelect v-model:value="roleForm.status" :options="statusOptions" />
        </NFormItem>
        <NFormItem :label="t('identity.role.label_remark')" path="remark">
          <NInput v-model:value="roleForm.remark" clearable :placeholder="t('identity.role.ph_remark')" />
        </NFormItem>
        <NFormItem :label="t('identity.role.label_description')" path="roleDescription">
          <NInput
            v-model:value="roleForm.roleDescription"
            clearable
            :placeholder="t('identity.role.ph_description')"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            {{ t('common.actions.cancel') }}
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            {{ t('common.actions.save') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <NDrawer v-model:show="permissionVisible" :width="760">
      <NDrawerContent closable :title="t('identity.role.perm_drawer_title', { name: permissionRole?.roleName ?? '' })">
        <div class="perm-toolbar">
          <NInput v-model:value="permKeyword" clearable :placeholder="t('identity.role.perm_search')" style="width: 240px" />
          <NTag round type="success" :bordered="false">
            {{ t('identity.role.perm_granted_count', { count: permGrants.length }) }}
          </NTag>
        </div>
        <NSpin :show="permLoading">
          <NEmpty v-if="permGroups.length === 0 && !permLoading" class="perm-empty" :description="t('identity.role.perm_no_match')" />
          <div v-else class="perm-groups">
            <section v-for="group in permGroups" :key="group.key" class="perm-group">
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
      <NDrawerContent closable :title="t('identity.role.menu_drawer_title', { name: menuRole?.roleName ?? '' })">
        <NSpin :show="menuLoading">
          <NEmpty v-if="menuTreeData.length === 0 && !menuLoading" class="perm-empty" :description="t('identity.role.menu_empty')" />
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
          {{ t('identity.role.menu_tip') }}
        </p>
        <template #footer>
          <NButton @click="menuVisible = false">
            {{ t('common.actions.cancel') }}
          </NButton>
          <NButton type="primary" :loading="menuLoading" :disabled="!menuDirty" style="margin-left: 8px" @click="saveMenuGrants">
            {{ t('identity.role.menu_save') }}
          </NButton>
        </template>
      </NDrawerContent>
    </NDrawer>

    <NDrawer v-model:show="scopeVisible" :width="560">
      <NDrawerContent closable :title="t('identity.role.scope_drawer_title', { name: scopeRole?.roleName ?? '' })">
        <div class="scope-add">
          <NTreeSelect
            v-model:value="scopeSelectedDept"
            clearable
            :options="scopeDeptOptions"
            :placeholder="t('identity.role.scope_select_dept')"
            style="flex: 1"
          />
          <NSwitch v-model:value="scopeIncludeChildren">
            <template #checked>
              {{ t('identity.role.scope_include_children') }}
            </template>
            <template #unchecked>
              {{ t('identity.role.scope_only_self') }}
            </template>
          </NSwitch>
          <NButton :loading="scopeSubmitting" type="primary" @click="addScope">
            {{ t('identity.role.scope_add') }}
          </NButton>
        </div>
        <NSpin :show="scopeLoading">
          <NEmpty v-if="scopeGrants.length === 0 && !scopeLoading" class="perm-empty" :description="t('identity.role.scope_empty')" />
          <div v-else class="scope-list">
            <div v-for="grant in scopeGrants" :key="String(grant.basicId)" class="scope-row">
              <span class="scope-dept">{{ grant.departmentName || grant.departmentId }}</span>
              <NTag :bordered="false" size="small" :type="grant.includeChildren ? 'info' : 'default'">
                {{ grant.includeChildren ? t('identity.role.scope_include_children') : t('identity.role.scope_only_self') }}
              </NTag>
              <NButton quaternary size="small" type="error" @click="removeScope(grant)">
                {{ t('identity.role.scope_remove') }}
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
