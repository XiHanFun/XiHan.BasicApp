<script setup lang="ts">
import type {
  ApiId,
  DepartmentTreeNodeDto,
  MenuListItemDto,
  PageResult,
  PermissionListItemDto,
  RoleCreateDto,
  RoleDataScopeListItemDto,
  RoleDetailDto,
  RoleListItemDto,
  RoleManagementDetailDto,
  RolePermissionListItemDto,
  RoleUpdateDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { TreeSelectOption } from '~/types'
import { XhButton, XhCheckbox, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhEmptyStateDescription, XhEmptyStateIcon, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhSpinner, XhSwitch, XhTabsContent, XhTabsList, XhTabsRoot, XhTabsTrigger, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  DataPermissionScope,
  departmentApi,
  EnableStatus,
  menuApi,
  MenuType,
  permissionApi,
  querySortsFromSchema,
  roleDataScopeApi,
  roleManagementApi,
  rolePermissionApi,
  RoleType,
  ValidityStatus,
} from '@/api'
import { DATA_SCOPE_OPTIONS, PERMISSION_ACTION_OPTIONS, ROLE_TYPE_OPTIONS, STATUS_OPTIONS, VALIDITY_STATUS_OPTIONS } from '@/constants'
import { SchemaPage, XEditModal, XInput, XNumberInput, XPermissionGrantPanel, XSelect, XTree, XTreeSelect } from '~/components'
import { toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { Icon } from '~/iconify'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemRolePage' })

interface RoleFormModel extends RoleCreateDto {
  basicId?: RoleListItemDto['basicId']
}

const { t } = useI18n()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()

const statusOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)
const roleTypeOptions = useEnumOptions('RoleType', ROLE_TYPE_OPTIONS)
const dataScopeOptions = useEnumOptions('DataPermissionScope', DATA_SCOPE_OPTIONS)
const validityStatusOptions = useEnumOptions('ValidityStatus', VALIDITY_STATUS_OPTIONS)
const permissionActionOptions = useEnumOptions('PermissionAction', PERMISSION_ACTION_OPTIONS)

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
    searchMultiple: true,
    dictionaryCode: 'RoleType',
    options: roleTypeOptions.value,
    searchPlaceholder: t('identity.role.role_type_placeholder'),
    minWidth: 110,
    order: 4,
    render: row => h('span', { style: 'font-size:13px;color:hsl(var(--muted-foreground));' }, getOptionLabel(roleTypeOptions.value, (row as unknown as RoleListItemDto).roleType)),
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
    render: row => h(XhTagRoot, { variant: 'outline', tone: (row as unknown as RoleListItemDto).isGlobal ? 'warning' : 'neutral' }, () => h(XhTagLabel, () => (row as unknown as RoleListItemDto).isGlobal ? t('common.statuses.yes') : t('common.statuses.no'))),
  },
  {
    key: 'dataScope',
    title: t('identity.role.col_data_scope'),
    dataType: 'enum',
    sortable: true,
    searchable: true,
    searchMultiple: true,
    dictionaryCode: 'DataPermissionScope',
    options: dataScopeOptions.value,
    searchPlaceholder: t('identity.role.data_scope_placeholder'),
    minWidth: 130,
    order: 6,
    render: row => h('span', { style: 'font-size:13px;color:hsl(var(--muted-foreground));' }, getOptionLabel(dataScopeOptions.value, (row as unknown as RoleListItemDto).dataScope)),
  },
  { key: 'maxMembers', title: t('identity.role.col_max_members'), dataType: 'number', sortable: true, minWidth: 100, order: 7 },
  { key: 'sort', title: t('identity.role.col_sort'), dataType: 'number', sortable: true, minWidth: 80, order: 8 },
  {
    key: 'status',
    title: t('identity.role.col_status'),
    dataType: 'enum',
    sortable: true,
    searchable: true,
    searchMultiple: true,
    dictionaryCode: 'EnableStatus',
    options: statusOptions.value,
    searchPlaceholder: t('identity.role.status_placeholder'),
    width: 82,
    order: 9,
    render: row => h(XhTagRoot, { variant: 'outline', tone: (row as unknown as RoleListItemDto).status === EnableStatus.Enabled ? 'success' : 'danger' }, () => h(XhTagLabel, () => (row as unknown as RoleListItemDto).status === EnableStatus.Enabled ? t('common.statuses.enabled') : t('common.statuses.disabled'))),
  },
  {
    key: 'createdTime',
    title: t('identity.role.col_create_time'),
    dataType: 'datetime',
    sortable: true,
    minWidth: 170,
    order: 10,
    render: row => h('span', { style: 'font-size:13px;color:hsl(var(--muted-foreground));' }, formatDate((row as unknown as RoleListItemDto).createdTime)),
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
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return roleManagementApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 多选(roleType/dataScope/status) 等通用过滤统一走 conditions.filters In
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: toStr(f.keyword) ?? null,
        // roleType / dataScope / status 改为多选，经 conditions.filters In 下发（不再走 DTO 顶层单值字段）
        isGlobal: toBool(f.isGlobal),
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
        void handleEdit(row)
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
const permPanelRef = ref<{ reset: () => void } | null>(null)
const permChecked = ref<Set<ApiId>>(new Set())
const permDirty = ref(false)

/**
 * permissionId → 有效授权记录（收权时取记录主键）
 * 仅纳入 Status===Valid：撤销是软删除（Status=Invalid），列表接口默认返回含软删除的全集，
 * 若不过滤则收回后复选框仍判定为已授权而自动重新勾上，表现为「收回不生效」。
 */
const permGrantByPermissionId = computed(() => {
  const map = new Map<ApiId, RolePermissionListItemDto>()
  for (const grant of permGrants.value) {
    if (grant.status === ValidityStatus.Valid) {
      map.set(grant.permissionId, grant)
    }
  }
  return map
})

/** 权限目录一次取全 */
async function loadPermCatalog() {
  if (permCatalog.value.length > 0) {
    return
  }
  permCatalog.value = await permissionApi.catalog()
}

async function openPermissionDrawer(row: RoleListItemDto) {
  permissionRole.value = row
  permissionVisible.value = true
  permPanelRef.value?.reset()
  permLoading.value = true
  try {
    const [, grantsResult] = await Promise.all([loadPermCatalog(), rolePermissionApi.list(row.basicId)])
    permGrants.value = grantsResult
    derivePermChecked()
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('identity.role.perm_load_failed'))
  }
  finally {
    permLoading.value = false
  }
}

/** 本地勾选态：打开抽屉时由有效授权推导，之后只改本地，保存时一次性提交 */
function derivePermChecked() {
  permChecked.value = new Set(permGrantByPermissionId.value.keys())
  permDirty.value = false
}

function togglePermission(permission: PermissionListItemDto, checked: boolean) {
  const next = new Set(permChecked.value)
  if (checked) {
    next.add(permission.basicId)
  }
  else {
    next.delete(permission.basicId)
  }
  permChecked.value = next
  permDirty.value = true
}

async function savePermGrants() {
  const role = permissionRole.value
  if (!role || permLoading.value) {
    return
  }
  const validGrants = permGrants.value.filter(grant => grant.status === ValidityStatus.Valid)
  const grantedPermIds = new Set(validGrants.map(grant => grant.permissionId))
  const toGrant = [...permChecked.value].filter(permId => !grantedPermIds.has(permId))
  const toRevoke = validGrants.filter(grant => !permChecked.value.has(grant.permissionId))
  if (toGrant.length === 0 && toRevoke.length === 0) {
    toast.info(t('identity.role.perm_no_change'))
    permDirty.value = false
    return
  }
  permLoading.value = true
  try {
    await rolePermissionApi.batchUpdate({
      roleId: role.basicId,
      grantPermissionIds: toGrant,
      revokeRolePermissionIds: toRevoke.map(grant => grant.basicId),
    })
    permGrants.value = await rolePermissionApi.list(role.basicId)
    derivePermChecked()
    toast.success(t('identity.role.perm_saved', { grant: toGrant.length, revoke: toRevoke.length }))
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    permLoading.value = false
  }
}

// ── 菜单授权抽屉（菜单关联权限，勾选即授予该权限） ───────────────
interface MenuNode {
  basicId: ApiId
  menuName: string
  menuType: MenuType
  permissionId?: ApiId | null
  // 叶子节点省略 children（undefined），树据此判定为末节点、不显示展开箭头
  children?: MenuNode[]
}

/** 树选项再带一条排布标记：树按它决定这一层子节点横排还是竖排 */
interface MenuTreeOption extends TreeSelectOption {
  childrenOrientation?: 'horizontal' | 'vertical'
  children?: MenuTreeOption[]
}

const menuVisible = ref(false)
const menuRole = ref<RoleListItemDto | null>(null)
const menuTreeData = ref<MenuNode[]>([])

/** 菜单节点 → 通用树选项：键与文本显式映射。
    子节点全是按钮的菜单标上横排，一行铺完；其余层不标，保持竖排读层级 */
function toMenuOptions(nodes: MenuNode[]): MenuTreeOption[] {
  return nodes.map((node) => {
    const children = node.children ?? []
    const buttonsOnly = children.length > 0 && children.every(child => child.menuType === MenuType.Button)
    return {
      value: String(node.basicId),
      label: node.menuName,
      ...(buttonsOnly ? { childrenOrientation: 'horizontal' as const } : {}),
      ...(children.length ? { children: toMenuOptions(children) } : {}),
    }
  })
}
const menuTreeOptions = computed(() => toMenuOptions(menuTreeData.value))
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

function buildMenuTree(flat: MenuListItemDto[]): MenuNode[] {
  const byId = new Map<ApiId, MenuNode>()
  const roots: MenuNode[] = []
  const sorted = [...flat].sort((a, b) => a.sort - b.sort)
  for (const item of sorted) {
    byId.set(item.basicId, {
      basicId: item.basicId,
      menuName: item.menuName,
      menuType: item.menuType,
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
  // 末节点的 children 置为 undefined（而非空数组），使树视其为叶子、不显示展开箭头
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
    toast.error((e as Error)?.message || t('identity.role.menu_load_failed'))
  }
  finally {
    menuLoading.value = false
  }
}

/** 勾选变更仅更新本地状态，点「保存授权」后统一提交差异。
    父子联动由树自己做（cascade），回传的已是收敛后的整份勾中集 */
function onMenuCheck(keys: Array<string | number>) {
  if (menuLoading.value) {
    return
  }
  menuCheckedKeys.value = keys.map(String)
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
    toast.info(t('identity.role.menu_no_change'))
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
    toast.success(t('identity.role.menu_saved', { grant: toGrant.length, revoke: toRevoke.length }))
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('common.messages.save_failed'))
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
    value: node.basicId,
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
    toast.error((e as Error)?.message || t('identity.role.scope_load_failed'))
  }
  finally {
    scopeLoading.value = false
  }
}

async function addScope() {
  if (!scopeRole.value || scopeSelectedDept.value == null) {
    toast.warning(t('identity.role.scope_select_dept_required'))
    return
  }
  scopeSubmitting.value = true
  try {
    await roleDataScopeApi.grant({
      roleId: scopeRole.value.basicId,
      departmentId: scopeSelectedDept.value,
      includeChildren: scopeIncludeChildren.value,
    })
    toast.success(t('identity.role.scope_added'))
    scopeSelectedDept.value = null
    scopeGrants.value = await roleDataScopeApi.list(scopeRole.value.basicId)
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('identity.role.scope_add_failed'))
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
    toast.success(t('identity.role.scope_removed'))
    scopeGrants.value = await roleDataScopeApi.list(scopeRole.value.basicId)
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('identity.role.scope_remove_failed'))
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
  return getOptionLabel(statusOptions.value, value)
}

function formatValidityStatus(value?: ValidityStatus | null) {
  return getOptionLabel(validityStatusOptions.value, value)
}

function handleAdd() {
  editingStatus.value = null
  roleForm.value = createDefaultRoleForm()
  modalVisible.value = true
}

async function handleEdit(row: RoleListItemDto) {
  editingStatus.value = row.status
  // 列表行不含备注，取详情回填；否则保存时会把备注覆盖为空
  let detail: RoleDetailDto | null = null
  try {
    detail = await roleManagementApi.detail(row.basicId)
  }
  catch {
    detail = null
  }
  roleForm.value = {
    basicId: row.basicId,
    dataScope: detail?.dataScope ?? row.dataScope,
    maxMembers: detail?.maxMembers ?? row.maxMembers,
    remark: detail?.remark ?? null,
    roleCode: detail?.roleCode ?? row.roleCode,
    roleDescription: detail?.roleDescription ?? row.roleDescription,
    roleName: detail?.roleName ?? row.roleName,
    roleType: detail?.roleType ?? row.roleType,
    sort: detail?.sort ?? row.sort,
    status: detail?.status ?? row.status,
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
      toast.warning(t('identity.role.msg_detail_not_found'))
    }
  }
  catch (error) {
    toast.error((error as Error)?.message || t('identity.role.msg_load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

function validateRoleForm() {
  if (!roleForm.value.roleName.trim()) {
    toast.warning(t('identity.role.msg_role_name_required'))
    return false
  }

  if (!roleForm.value.basicId && !roleForm.value.roleCode.trim()) {
    toast.warning(t('identity.role.msg_role_code_required'))
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

    toast.success(t('common.messages.save_success'))
    modalVisible.value = false
    reloadRole()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: RoleListItemDto) {
  await roleManagementApi.delete(row.basicId)
  toast.success(t('common.messages.delete_success'))
  reloadRole()
}

async function handleToggleStatus(row: RoleListItemDto) {
  await roleManagementApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? t('identity.role.front_disable_remark') : t('identity.role.front_enable_remark'),
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  toast.success(t('common.messages.status_updated'))
  reloadRole()
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 900px">
        <XhDrawerTitle>{{ t('identity.role.detail_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div class="xh-loading-stage" :class="{ 'is-loading': detailLoading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <XhEmptyStateRoot v-if="!detailLoading && !currentDetail" class="xh-detail-empty">
            <XhEmptyStateIcon>
              <Icon icon="lucide:inbox" width="28" />
            </XhEmptyStateIcon>
            <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('identity.role.detail_empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
          <div v-else-if="currentDetail" class="xh-scroll-area" style="max-height: calc(100vh - 120px)">
            <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
            <XhTabsRoot default-value="overview" variant="line">
              <XhTabsList>
                <XhTabsTrigger value="overview">
                  {{ t('identity.role.tab_overview') }}
                </XhTabsTrigger>
                <XhTabsTrigger value="permissions">
                  {{ t('identity.role.tab_permissions', { count: currentDetail.permissions.length }) }}
                </XhTabsTrigger>
                <XhTabsTrigger value="dataScopes">
                  {{ t('identity.role.tab_data_scopes', { count: currentDetail.dataScopes.length }) }}
                </XhTabsTrigger>
                <XhTabsTrigger value="ancestors">
                  {{ t('identity.role.tab_ancestors', { count: currentDetail.ancestors.length }) }}
                </XhTabsTrigger>
                <XhTabsTrigger value="descendants">
                  {{ t('identity.role.tab_descendants', { count: currentDetail.descendants.length }) }}
                </XhTabsTrigger>
                <XhTabsTrigger value="grantedUsers">
                  {{ t('identity.role.tab_granted_users', { count: currentDetail.grantedUsers.length }) }}
                </XhTabsTrigger>
              </XhTabsList>
              <XhTabsContent value="overview">
                <XhDescriptionsRoot :columns="2" bordered size="sm">
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('identity.role.label_role_name') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ currentDetail.role.roleName }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('identity.role.label_role_code') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ currentDetail.role.roleCode }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('identity.role.label_role_type') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ getOptionLabel(roleTypeOptions, currentDetail.role.roleType) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('identity.role.label_data_scope') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ getOptionLabel(dataScopeOptions, currentDetail.role.dataScope) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('identity.role.label_is_global') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatBoolean(currentDetail.role.isGlobal) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('identity.role.label_status') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatStatus(currentDetail.role.status) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('identity.role.label_max_members') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ currentDetail.role.maxMembers }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('identity.role.label_sort') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ currentDetail.role.sort }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('identity.role.label_description') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatNullable(currentDetail.role.roleDescription) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('identity.role.label_remark') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatNullable(currentDetail.role.remark) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('identity.role.label_create_time') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatNullableDate(currentDetail.role.createdTime) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('identity.role.label_generated_time') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      {{ formatNullableDate(currentDetail.generatedTime) }}
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                </XhDescriptionsRoot>
              </XhTabsContent>
              <XhTabsContent value="permissions">
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
                <XhEmptyStateRoot v-else style="padding: 40px 0">
                  <XhEmptyStateIcon>
                    <Icon icon="lucide:inbox" width="28" />
                  </XhEmptyStateIcon>
                  <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('identity.role.empty_permissions') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
              </XhTabsContent>
              <XhTabsContent value="dataScopes">
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
                <XhEmptyStateRoot v-else style="padding: 40px 0">
                  <XhEmptyStateIcon>
                    <Icon icon="lucide:inbox" width="28" />
                  </XhEmptyStateIcon>
                  <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('identity.role.empty_data_scopes') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
              </XhTabsContent>
              <XhTabsContent value="ancestors">
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
                <XhEmptyStateRoot v-else style="padding: 40px 0">
                  <XhEmptyStateIcon>
                    <Icon icon="lucide:inbox" width="28" />
                  </XhEmptyStateIcon>
                  <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('identity.role.empty_ancestors') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
              </XhTabsContent>
              <XhTabsContent value="descendants">
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
                <XhEmptyStateRoot v-else style="padding: 40px 0">
                  <XhEmptyStateIcon>
                    <Icon icon="lucide:inbox" width="28" />
                  </XhEmptyStateIcon>
                  <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('identity.role.empty_descendants') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
              </XhTabsContent>
              <XhTabsContent value="grantedUsers">
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
                <XhEmptyStateRoot v-else style="padding: 40px 0">
                  <XhEmptyStateIcon>
                    <Icon icon="lucide:inbox" width="28" />
                  </XhEmptyStateIcon>
                  <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('identity.role.empty_granted_users') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
              </XhTabsContent>
            </XhTabsRoot>
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
        v-model:values="roleForm"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleSubmit"
      >
        <XhFormFieldGroup value="roleName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.role.label_role_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="roleForm.roleName" clearable :placeholder="t('identity.role.ph_role_name')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="roleCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.role.label_role_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="roleForm.roleCode"
                clearable
                :disabled="Boolean(roleForm.basicId)"
                :placeholder="t('identity.role.ph_role_code')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="roleType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.role.label_role_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="roleForm.roleType" :options="maintainableRoleTypeOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="dataScope">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.role.label_data_scope') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="roleForm.dataScope" :options="dataScopeOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="maxMembers">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.role.label_max_members') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="roleForm.maxMembers" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.role.label_sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="roleForm.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="status">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.role.label_status') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="roleForm.status" :options="statusOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="remark">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.role.label_remark') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="roleForm.remark" clearable :placeholder="t('identity.role.ph_remark')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="roleDescription" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.role.label_description') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="roleForm.roleDescription"
                clearable
                :placeholder="t('identity.role.ph_description')"
                :rows="3"
                type="textarea"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>

    <XhDrawerRoot v-model:open="permissionVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 760px">
        <XhDrawerTitle>{{ t('identity.role.perm_drawer_title', { name: permissionRole?.roleName ?? '' }) }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <XPermissionGrantPanel
          ref="permPanelRef"
          :items="permCatalog"
          :loading="permLoading"
          :search-placeholder="t('identity.role.perm_search')"
          :granted-count-label="t('identity.role.perm_granted_count', { count: permChecked.size })"
          :empty-description="t('identity.role.perm_no_match')"
          :other-group-label="t('identity.role.perm_group_other')"
        >
          <template #action="{ item }">
            <XhCheckbox
              :checked="permChecked.has(item.basicId)"
              :disabled="permLoading"
              @update:checked="(checked: boolean) => togglePermission(item as PermissionListItemDto, checked)"
            />
          </template>
        </XPermissionGrantPanel>
        <div class="xh-dialog-footer">
          <XhButton @click="permissionVisible = false">
            {{ t('common.actions.cancel') }}
          </XhButton>
          <XhButton tone="brand" :loading="permLoading" :disabled="!permDirty" style="margin-left: 8px" @click="savePermGrants">
            {{ t('identity.role.perm_save') }}
          </XhButton>
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>

    <XhDrawerRoot v-model:open="menuVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 520px">
        <XhDrawerTitle>{{ t('identity.role.menu_drawer_title', { name: menuRole?.roleName ?? '' }) }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div class="xh-loading-stage menu-tree-stage" :class="{ 'is-loading': menuLoading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <XhEmptyStateRoot v-if="menuTreeData.length === 0 && !menuLoading" size="sm" class="perm-empty">
            <XhEmptyStateIcon>
              <Icon icon="lucide:inbox" width="28" height="28" />
            </XhEmptyStateIcon>
            <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('identity.role.menu_empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
          <XTree
            v-else
            :data="menuTreeOptions"
            selection-mode="multiple"
            cascade
            checked-strategy="all"
            :selected-keys="menuCheckedKeys.map(String)"
            @update:selected-keys="onMenuCheck"
          />
        </div>
        <p class="perm-tip">
          {{ t('identity.role.menu_tip') }}
        </p>
        <div class="xh-dialog-footer">
          <XhButton @click="menuVisible = false">
            {{ t('common.actions.cancel') }}
          </XhButton>
          <XhButton tone="brand" :loading="menuLoading" :disabled="!menuDirty" style="margin-left: 8px" @click="saveMenuGrants">
            {{ t('identity.role.menu_save') }}
          </XhButton>
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>

    <XhDrawerRoot v-model:open="scopeVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 560px">
        <XhDrawerTitle>{{ t('identity.role.scope_drawer_title', { name: scopeRole?.roleName ?? '' }) }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div class="scope-add">
          <XTreeSelect
            v-model:value="scopeSelectedDept"
            clearable
            :options="scopeDeptOptions"
            :placeholder="t('identity.role.scope_select_dept')" style="flex: 1"
          />
          <!-- 开关旁的文字随状态切换：含下级 / 仅本级 -->
          <XhSwitch v-model:checked="scopeIncludeChildren">
            {{ scopeIncludeChildren ? t('identity.role.scope_include_children') : t('identity.role.scope_only_self') }}
          </XhSwitch>
          <XhButton :loading="scopeSubmitting" tone="brand" @click="addScope">
            {{ t('identity.role.scope_add') }}
          </XhButton>
        </div>
        <div class="xh-loading-stage" :class="{ 'is-loading': scopeLoading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <XhEmptyStateRoot v-if="scopeGrants.length === 0 && !scopeLoading" size="sm" class="perm-empty">
            <XhEmptyStateIcon>
              <Icon icon="lucide:inbox" width="28" height="28" />
            </XhEmptyStateIcon>
            <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('identity.role.scope_empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
          <div v-else class="scope-list">
            <div v-for="grant in scopeGrants" :key="String(grant.basicId)" class="scope-row">
              <span class="scope-dept">{{ grant.departmentName || grant.departmentId }}</span>
              <XhTagRoot variant="subtle" size="sm" :tone="grant.includeChildren ? 'info' : 'neutral'">
                <XhTagLabel>
                  {{ grant.includeChildren ? t('identity.role.scope_include_children') : t('identity.role.scope_only_self') }}
                </XhTagLabel>
              </XhTagRoot>
              <XhButton variant="ghost" size="sm" tone="danger" @click="removeScope(grant)">
                {{ t('identity.role.scope_remove') }}
              </XhButton>
            </div>
          </div>
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>
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
  border: 1px solid hsl(var(--border));
  text-align: left;
  vertical-align: top;
}

.xh-detail-table th {
  background: hsl(var(--muted));
  font-weight: 500;
}

/* 权限分配抽屉 */

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
  border: 1px solid hsl(var(--border));
  border-radius: 8px;
}

.scope-dept {
  flex: 1;
  font-size: 13px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 菜单树撑满抽屉剩余高度：树自带 24rem 的最大高，不放开就是一块矮框加大片空白 */
.menu-tree-stage {
  --xh-tree-max-h: 100%;

  display: flex;
  flex: 1;
  min-block-size: 0;
}

.menu-tree-stage :deep(.x-tree) {
  flex: 1;
  min-block-size: 0;
}

.menu-tree-stage :deep([data-scope='tree'][data-part='tree']) {
  flex: 1;
  min-block-size: 0;
}
</style>
