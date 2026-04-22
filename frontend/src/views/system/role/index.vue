<script lang="ts" setup>
import type { TreeOption } from 'naive-ui'
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysDepartment } from '@/api/modules/department'
import type { SysMenu } from '@/api/modules/menu'
import type { SysPermission } from '@/api/modules/permission'
import type { SysRole } from '@/api/modules/role'
import {
  NButton,
  NCheckbox,
  NEmpty,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NPopconfirm,
  NRadio,
  NRadioGroup,
  NSelect,
  NSpace,
  NSpin,
  NTabPane,
  NTabs,
  NTag,
  NTree,
  useMessage,
} from 'naive-ui'
import { computed, reactive, ref } from 'vue'
import { departmentApi, menuApi, permissionApi, roleApi } from '@/api'
import { XSystemQueryPanel } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemRolePage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

interface MenuTreeOption extends TreeOption {
  menuType?: number
  buttonGroup?: boolean
  children?: MenuTreeOption[]
}

// ==================== 常量 ====================

const ROLE_TYPE_OPTIONS = [
  { label: '系统角色', value: 0 },
  { label: '业务角色', value: 1 },
  { label: '自定义角色', value: 2 },
]

const DATA_SCOPE_OPTIONS = [
  { label: '仅本人', value: 0 },
  { label: '本部门', value: 1 },
  { label: '本部门及以下', value: 2 },
  { label: '全部数据', value: 3 },
  { label: '自定义', value: 99 },
]

const GLOBAL_ROLE_OPTIONS = [
  { label: '否', value: 0 },
  { label: '是', value: 1 },
]

const ROLE_TYPE_MAP: Record<number, string> = { 0: '系统角色', 1: '业务角色', 2: '自定义角色' }
const DATA_SCOPE_MAP: Record<number, string> = {
  0: '仅本人',
  1: '本部门',
  2: '本部门及以下',
  3: '全部',
  99: '自定义',
}

// ==================== 列表 ====================

const queryParams = reactive({
  keyword: '',
  status: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return roleApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    status: queryParams.status,
  })
}

const gridOptions = useVxeTable<SysRole>(
  {
    id: 'sys_role',
    name: '角色管理',
    columns: [
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      {
        field: 'roleName',
        title: '角色名称',
        minWidth: 150,
        showOverflow: 'tooltip',
        sortable: true,
      },
      { field: 'roleCode', title: '角色编码', minWidth: 150, showOverflow: 'tooltip' },
      { field: 'roleDescription', title: '描述', minWidth: 200, showOverflow: 'tooltip' },
      {
        field: 'roleType',
        title: '角色类型',
        width: 100,
        formatter: ({ cellValue }) => ROLE_TYPE_MAP[cellValue] ?? '-',
      },
      {
        field: 'isGlobal',
        title: '全局',
        width: 80,
        slots: { default: 'col_global' },
      },
      {
        field: 'dataScope',
        title: '数据范围',
        width: 120,
        formatter: ({ cellValue }) => DATA_SCOPE_MAP[cellValue] ?? '-',
      },
      { field: 'sort', title: '排序', width: 70 },
      {
        field: 'status',
        title: '状态',
        width: 80,
        slots: { default: 'col_status' },
      },
      {
        field: 'createTime',
        title: '创建时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
        sortable: true,
      },
      {
        field: 'actions',
        title: '操作',
        width: 140,
        fixed: 'right',
        slots: { default: 'col_actions' },
      },
    ],
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleQueryApi(page),
      },
    },
  },
)

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

async function handleDelete(id: string) {
  try {
    await roleApi.delete(id)
    message.success('删除成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('删除失败')
  }
}

// ==================== 弹窗状态 ====================

const modalVisible = ref(false)
const modalTitle = ref('')
const isEdit = ref(false)
const currentRoleId = ref('')
const activeTab = ref('basic')

// Tab 1: 基本信息
const formData = ref<Partial<SysRole>>({})
const basicSaving = ref(false)

// Tab 2: 菜单权限
const menuTreeData = ref<MenuTreeOption[]>([])
const checkedMenuKeys = ref<string[]>([])
const menuExpandedKeys = ref<string[]>([])
const menuLoading = ref(false)
const menuDataLoaded = ref(false)

// Tab 3: 操作权限
const allPermissions = ref<SysPermission[]>([])
const checkedPermKeys = ref<string[]>([])
const permLoading = ref(false)
const permSaving = ref(false)
const permDataLoaded = ref(false)

// Tab 4: 数据范围
const deptTreeData = ref<TreeOption[]>([])
const checkedDeptKeys = ref<string[]>([])
const deptExpandedKeys = ref<string[]>([])
const deptLoading = ref(false)
const deptSaving = ref(false)
const deptDataLoaded = ref(false)

// Tab 5: 角色继承
const parentRoleOptions = ref<Array<{ label: string, value: string }>>([])
const checkedParentRoleIds = ref<string[]>([])
const inheritLoading = ref(false)
const inheritSaving = ref(false)
const inheritDataLoaded = ref(false)

// ==================== 工具函数 ====================

function collectTreeKeys(nodes: TreeOption[]): string[] {
  const keys: string[] = []
  function walk(list: TreeOption[]) {
    list.forEach((n) => {
      keys.push(n.key as string)
      if (n.children)
        walk(n.children)
    })
  }
  walk(nodes)
  return keys
}

function menuToTree(menus: SysMenu[]): MenuTreeOption[] {
  return menus.map((m) => {
    const children = m.children?.length ? menuToTree(m.children) : undefined
    const buttonGroup = Array.isArray(children) && children.length > 0 && children.every(item => item.menuType === 2)

    return {
      key: m.basicId,
      label: m.menuType === 2
        ? `${m.menuName}（按钮）`
        : m.menuName,
      menuType: m.menuType,
      buttonGroup,
      class: [
        m.menuType === 2 ? 'xh-role-menu-button-node' : '',
        buttonGroup ? 'xh-role-menu-button-group' : '',
      ].filter(Boolean).join(' ') || undefined,
      children,
    }
  })
}

function deptToTree(depts: SysDepartment[]): TreeOption[] {
  return depts.map(d => ({
    key: d.basicId,
    label: d.departmentName,
    children: d.children?.length ? deptToTree(d.children) : undefined,
  }))
}

// ==================== 计算属性 ====================

const allMenuKeys = computed(() => collectTreeKeys(menuTreeData.value))

const isMenuAllChecked = computed(() =>
  allMenuKeys.value.length > 0
  && checkedMenuKeys.value.length === allMenuKeys.value.length,
)

const isMenuIndeterminate = computed(() => {
  const len = checkedMenuKeys.value.length
  return len > 0 && len < allMenuKeys.value.length
})

const isMenuAllExpanded = computed(() =>
  allMenuKeys.value.length > 0
  && menuExpandedKeys.value.length >= allMenuKeys.value.length,
)

// 权限按 groupName 分组
const permissionGroups = computed(() => {
  const groups = new Map<string, SysPermission[]>()
  allPermissions.value.forEach((p) => {
    const group = p.primaryTag || '未标记'
    if (!groups.has(group))
      groups.set(group, [])
    groups.get(group)!.push(p)
  })
  return groups
})

// ==================== 弹窗操作 ====================

function resetModalState() {
  menuTreeData.value = []
  checkedMenuKeys.value = []
  menuExpandedKeys.value = []
  menuDataLoaded.value = false
  allPermissions.value = []
  checkedPermKeys.value = []
  permDataLoaded.value = false
  deptTreeData.value = []
  checkedDeptKeys.value = []
  deptExpandedKeys.value = []
  deptDataLoaded.value = false
  parentRoleOptions.value = []
  checkedParentRoleIds.value = []
  inheritDataLoaded.value = false
}

function handleAdd() {
  resetModalState()
  modalTitle.value = '新增角色'
  isEdit.value = false
  currentRoleId.value = ''
  activeTab.value = 'basic'
  formData.value = {
    roleName: '',
    roleCode: '',
    roleDescription: '',
    roleType: 0,
    dataScope: 0,
    isGlobal: false,
    status: 1,
    sort: 100,
  }
  modalVisible.value = true
}

function handleEdit(row: SysRole) {
  resetModalState()
  modalTitle.value = '编辑角色'
  isEdit.value = true
  currentRoleId.value = row.basicId
  activeTab.value = 'basic'
  formData.value = { ...row, dataScope: row.dataScope ?? 0 }
  modalVisible.value = true
}

function handleTabChange(tab: string) {
  if (!currentRoleId.value)
    return
  if (tab === 'menu')
    loadMenuData()
  else if (tab === 'permission')
    loadPermissionData()
  else if (tab === 'dataScope')
    loadDataScopeData()
  else if (tab === 'inherit')
    loadInheritanceData()
}

// ==================== 数据加载 ====================

async function loadMenuData() {
  if (menuDataLoaded.value)
    return
  menuLoading.value = true
  try {
    const [tree, assigned] = await Promise.all([
      menuApi.tree(),
      currentRoleId.value
        ? menuApi.roleMenus(currentRoleId.value).then(items => items.map(item => item.basicId))
        : Promise.resolve([] as string[]),
    ])
    menuTreeData.value = menuToTree(tree)
    checkedMenuKeys.value = assigned
    menuDataLoaded.value = true
  }
  catch {
    message.error('加载菜单数据失败')
  }
  finally {
    menuLoading.value = false
  }
}

async function loadPermissionData() {
  if (permDataLoaded.value)
    return
  permLoading.value = true
  try {
    const [list, assigned] = await Promise.all([
      permissionApi.list(),
      currentRoleId.value
        ? roleApi.getRolePermissions(currentRoleId.value)
        : Promise.resolve([] as string[]),
    ])
    allPermissions.value = list
    checkedPermKeys.value = assigned
    permDataLoaded.value = true
  }
  catch {
    message.error('加载权限数据失败')
  }
  finally {
    permLoading.value = false
  }
}

async function loadDataScopeData() {
  if (deptDataLoaded.value)
    return
  deptLoading.value = true
  try {
    const [tree, assigned] = await Promise.all([
      departmentApi.tree(),
      currentRoleId.value
        ? roleApi.getRoleDataScopeDeptIds(currentRoleId.value)
        : Promise.resolve([] as string[]),
    ])
    deptTreeData.value = deptToTree(tree)
    checkedDeptKeys.value = assigned
    deptExpandedKeys.value = collectTreeKeys(deptTreeData.value)
    deptDataLoaded.value = true
  }
  catch {
    message.error('加载部门数据失败')
  }
  finally {
    deptLoading.value = false
  }
}

async function loadInheritanceData() {
  if (inheritDataLoaded.value)
    return
  inheritLoading.value = true
  try {
    const [allRoles, assigned] = await Promise.all([
      roleApi.list(),
      currentRoleId.value
        ? roleApi.getRoleParentRoleIds(currentRoleId.value)
        : Promise.resolve([] as string[]),
    ])
    parentRoleOptions.value = allRoles
      .filter(item => item.basicId !== currentRoleId.value)
      .map(item => ({
        label: `${item.roleName} (${item.roleCode})`,
        value: item.basicId,
      }))
    checkedParentRoleIds.value = assigned
    inheritDataLoaded.value = true
  }
  catch {
    message.error('加载角色继承数据失败')
  }
  finally {
    inheritLoading.value = false
  }
}

// ==================== 保存 ====================

async function handleSaveBasic() {
  if (!formData.value.roleName) {
    message.warning('请输入角色名称')
    return
  }
  if (!formData.value.roleCode) {
    message.warning('请输入角色编码')
    return
  }
  basicSaving.value = true
  try {
    if (isEdit.value && currentRoleId.value) {
      await roleApi.update(currentRoleId.value, formData.value)
      message.success('保存成功')
    }
    else {
      // eslint-disable-next-line ts/no-explicit-any
      const result: any = await roleApi.create(formData.value)
      const newId = result?.basicId ?? result?.data?.basicId ?? result?.id ?? result?.data?.id
      if (newId) {
        currentRoleId.value = String(newId)
        formData.value.basicId = String(newId)
        isEdit.value = true
        modalTitle.value = '编辑角色'
        message.success('创建成功，可继续配置权限')
      }
      else {
        message.success('创建成功')
        modalVisible.value = false
      }
    }
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('保存失败')
  }
  finally {
    basicSaving.value = false
  }
}

async function handleSavePermissions() {
  if (!currentRoleId.value) {
    message.warning('请先保存基本信息')
    return
  }
  permSaving.value = true
  try {
    await roleApi.assignPermissions(currentRoleId.value, checkedPermKeys.value)
    if (menuDataLoaded.value) {
      menuDataLoaded.value = false
      await loadMenuData()
    }
    message.success('操作权限保存成功')
  }
  catch {
    message.error('操作权限保存失败')
  }
  finally {
    permSaving.value = false
  }
}

async function handleSaveDataScope() {
  if (!currentRoleId.value) {
    message.warning('请先保存基本信息')
    return
  }
  deptSaving.value = true
  try {
    const scope = formData.value.dataScope ?? 0
    await roleApi.update(currentRoleId.value, { ...formData.value, dataScope: scope })
    if (scope === 99) {
      await roleApi.assignDataScope(currentRoleId.value, checkedDeptKeys.value)
    }
    message.success('数据范围保存成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('数据范围保存失败')
  }
  finally {
    deptSaving.value = false
  }
}

async function handleSaveInheritance() {
  if (!currentRoleId.value) {
    message.warning('请先保存基本信息')
    return
  }
  inheritSaving.value = true
  try {
    await roleApi.assignInheritance(currentRoleId.value, checkedParentRoleIds.value)
    message.success('角色继承保存成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('角色继承保存失败')
  }
  finally {
    inheritSaving.value = false
  }
}

// ==================== 菜单树操作 ====================

function handleMenuCheckAll(checked: boolean) {
  checkedMenuKeys.value = checked ? [...allMenuKeys.value] : []
}

function handleMenuExpandAll(expand: boolean) {
  menuExpandedKeys.value = expand ? [...allMenuKeys.value] : []
}

function onMenuCheckedKeysUpdate(keys: Array<string | number>) {
  checkedMenuKeys.value = keys.map(String)
}

function onMenuExpandedKeysUpdate(keys: Array<string | number>) {
  menuExpandedKeys.value = keys.map(String)
}

function menuNodeProps({ option }: { option: TreeOption }) {
  const menuNode = option as MenuTreeOption
  const classList: string[] = []

  if (menuNode.menuType === 2) {
    classList.push('xh-role-menu-button-node')
  }
  if (menuNode.buttonGroup) {
    classList.push('xh-role-menu-button-group')
  }

  return classList.length > 0
    ? { class: classList.join(' ') }
    : {}
}

// ==================== 权限操作 ====================

function handlePermToggle(id: string, checked: boolean) {
  if (checked) {
    checkedPermKeys.value = [...checkedPermKeys.value, id]
  }
  else {
    checkedPermKeys.value = checkedPermKeys.value.filter(k => k !== id)
  }
}

function handlePermGroupCheckAll(perms: SysPermission[], checked: boolean) {
  const ids = new Set(perms.map(p => p.basicId))
  if (checked) {
    const current = new Set(checkedPermKeys.value)
    ids.forEach(id => current.add(id))
    checkedPermKeys.value = [...current]
  }
  else {
    checkedPermKeys.value = checkedPermKeys.value.filter(k => !ids.has(k))
  }
}

function isGroupAllChecked(perms: SysPermission[]) {
  return perms.length > 0 && perms.every(p => checkedPermKeys.value.includes(p.basicId))
}

function isGroupIndeterminate(perms: SysPermission[]) {
  const count = perms.filter(p => checkedPermKeys.value.includes(p.basicId)).length
  return count > 0 && count < perms.length
}

// ==================== 数据范围树操作 ====================

function onDeptCheckedKeysUpdate(keys: Array<string | number>) {
  checkedDeptKeys.value = keys.map(String)
}

function onDeptExpandedKeysUpdate(keys: Array<string | number>) {
  deptExpandedKeys.value = keys.map(String)
}
</script>

<template>
  <div class="flex flex-col h-full">
    <!-- 搜索栏 -->
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          placeholder="搜索角色名称/编码"
          clearable
          style="width: 240px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          placeholder="状态"
          clearable
          style="width: 120px"
        />
        <NButton type="primary" size="small" @click="handleSearch">
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          重置
        </NButton>
      </div>
    </XSystemQueryPanel>

    <!-- 角色列表 -->
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="gridOptions">
        <template #toolbar_buttons>
          <NButton v-access="['role:create']" type="primary" size="small" @click="handleAdd">
            新增角色
          </NButton>
        </template>
        <template #col_status="{ row }">
          <NTag :type="row.status === 1 ? 'success' : 'error'" size="small" round>
            {{ row.status === 1 ? '启用' : '禁用' }}
          </NTag>
        </template>
        <template #col_global="{ row }">
          <NTag :type="row.isGlobal ? 'warning' : 'default'" size="small" round>
            {{ row.isGlobal ? '是' : '否' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton v-access="['role:update']" size="small" type="primary" text @click="handleEdit(row)">
              编辑
            </NButton>
            <NPopconfirm v-access="['role:delete']" @positive-click="handleDelete(row.basicId)">
              <template #trigger>
                <NButton size="small" type="error" text>
                  删除
                </NButton>
              </template>
              确认删除该角色？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <!-- 角色编辑弹窗 -->
    <NModal
      v-model:show="modalVisible"
      preset="card"
      :title="modalTitle"
      :style="{ width: '720px', maxWidth: '92vw' }"
      :bordered="false"
      @after-leave="resetModalState"
    >
      <div class="max-h-[min(70vh,640px)] overflow-y-auto pr-1">
        <NTabs v-model:value="activeTab" type="line" @update:value="handleTabChange">
          <!-- ===== 基本信息 ===== -->
          <NTabPane name="basic" tab="基本信息">
            <NForm class="xh-edit-form-grid" :model="formData" label-placement="top" label-width="80px">
              <NFormItem label="角色名称" path="roleName">
                <NInput v-model:value="formData.roleName" placeholder="请输入角色名称" />
              </NFormItem>
              <NFormItem label="角色编码" path="roleCode">
                <NInput
                  v-model:value="formData.roleCode"
                  :disabled="isEdit"
                  placeholder="如: admin, editor"
                />
              </NFormItem>
              <NFormItem label="描述" path="roleDescription">
                <NInput
                  v-model:value="formData.roleDescription"
                  type="textarea"
                  :rows="3"
                  placeholder="角色描述"
                />
              </NFormItem>
              <NFormItem label="角色类型" path="roleType">
                <NSelect v-model:value="formData.roleType" :options="ROLE_TYPE_OPTIONS" />
              </NFormItem>
              <NFormItem label="数据范围" path="dataScope">
                <NSelect v-model:value="formData.dataScope" :options="DATA_SCOPE_OPTIONS" />
              </NFormItem>
              <NFormItem label="全局角色" path="isGlobal">
                <NSelect
                  :value="formData.isGlobal ? 1 : 0"
                  :options="GLOBAL_ROLE_OPTIONS"
                  @update:value="value => (formData.isGlobal = value === 1)"
                />
              </NFormItem>
              <NFormItem label="排序" path="sort">
                <NInputNumber
                  v-model:value="formData.sort"
                  :min="0"
                  :max="9999"
                  style="width: 100%"
                />
              </NFormItem>
              <NFormItem label="状态" path="status">
                <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
              </NFormItem>
            </NForm>
            <div class="xh-form-actions">
              <NButton
                v-if="!isEdit"
                v-access="['role:create']"
                type="primary"
                :loading="basicSaving"
                @click="handleSaveBasic"
              >
                保存
              </NButton>
              <NButton
                v-else
                v-access="['role:update']"
                type="primary"
                :loading="basicSaving"
                @click="handleSaveBasic"
              >
                保存
              </NButton>
            </div>
          </NTabPane>

          <!-- ===== 菜单权限 ===== -->
          <NTabPane name="menu" tab="菜单权限" :disabled="!currentRoleId">
            <NSpin :show="menuLoading">
              <div class="flex items-center gap-4 mb-3">
                <NCheckbox
                  :checked="isMenuAllChecked"
                  :indeterminate="isMenuIndeterminate"
                  @update:checked="handleMenuCheckAll"
                >
                  全选
                </NCheckbox>
                <NCheckbox
                  :checked="isMenuAllExpanded"
                  @update:checked="handleMenuExpandAll"
                >
                  展开全部
                </NCheckbox>
                <span class="text-xs text-gray-500">
                  菜单可见性由角色权限自动推导，需调整请切到“操作权限”页签。
                </span>
              </div>
              <div style="max-height: 460px; overflow-y: auto">
                <NTree
                  class="xh-role-menu-tree"
                  :data="menuTreeData"
                  checkable
                  cascade
                  :checked-keys="checkedMenuKeys"
                  :expanded-keys="menuExpandedKeys"
                  :node-props="menuNodeProps"
                  check-strategy="all"
                  block-line
                  @update:checked-keys="onMenuCheckedKeysUpdate"
                  @update:expanded-keys="onMenuExpandedKeysUpdate"
                />
              </div>
              <NEmpty
                v-if="!menuLoading && menuDataLoaded && menuTreeData.length === 0"
                description="暂无菜单数据"
              />
            </NSpin>
          </NTabPane>

          <!-- ===== 操作权限 ===== -->
          <NTabPane name="permission" tab="操作权限" :disabled="!currentRoleId">
            <NSpin :show="permLoading">
              <template v-if="permissionGroups.size > 0">
                <div
                  v-for="[groupName, perms] of permissionGroups"
                  :key="groupName"
                  class="mb-4"
                >
                  <div
                    class="flex items-center gap-2 mb-2 pb-1"
                    style="border-bottom: 1px solid #efeff5"
                  >
                    <NCheckbox
                      :checked="isGroupAllChecked(perms)"
                      :indeterminate="isGroupIndeterminate(perms)"
                      @update:checked="(val) => handlePermGroupCheckAll(perms, val)"
                    >
                      <span class="font-semibold">{{ groupName }}</span>
                    </NCheckbox>
                    <span class="text-xs opacity-50">
                      {{ perms.filter(p => checkedPermKeys.includes(p.basicId)).length }}/{{ perms.length }}
                    </span>
                  </div>
                  <div class="flex flex-wrap gap-x-5 gap-y-1 pl-6">
                    <NCheckbox
                      v-for="p in perms"
                      :key="p.basicId"
                      :checked="checkedPermKeys.includes(p.basicId)"
                      @update:checked="(val) => handlePermToggle(p.basicId, val)"
                    >
                      {{ p.permissionName }}
                    </NCheckbox>
                  </div>
                </div>
              </template>
              <NEmpty
                v-if="!permLoading && permDataLoaded && allPermissions.length === 0"
                description="暂无权限数据"
              />
            </NSpin>
            <div class="xh-form-actions">
              <NButton v-access="['role:update']" type="primary" :loading="permSaving" @click="handleSavePermissions">
                保存
              </NButton>
            </div>
          </NTabPane>

          <!-- ===== 数据范围 ===== -->
          <NTabPane name="dataScope" tab="数据范围" :disabled="!currentRoleId">
            <NSpin :show="deptLoading">
              <div class="mb-4">
                <div class="mb-2 font-semibold text-sm">
                  数据范围类型
                </div>
                <NRadioGroup
                  :value="formData.dataScope ?? 0"
                  @update:value="(v) => { formData.dataScope = Number(v) }"
                >
                  <NSpace vertical>
                    <NRadio
                      v-for="opt in DATA_SCOPE_OPTIONS"
                      :key="opt.value"
                      :value="opt.value"
                    >
                      {{ opt.label }}
                    </NRadio>
                  </NSpace>
                </NRadioGroup>
              </div>
              <!-- 自定义范围时选择部门 -->
              <template v-if="formData.dataScope === 99">
                <div class="mb-2 font-semibold text-sm">
                  选择部门
                </div>
                <div style="max-height: 320px; overflow-y: auto">
                  <NTree
                    :data="deptTreeData"
                    checkable
                    cascade
                    :checked-keys="checkedDeptKeys"
                    :expanded-keys="deptExpandedKeys"
                    check-strategy="all"
                    block-line
                    @update:checked-keys="onDeptCheckedKeysUpdate"
                    @update:expanded-keys="onDeptExpandedKeysUpdate"
                  />
                </div>
                <NEmpty
                  v-if="!deptLoading && deptDataLoaded && deptTreeData.length === 0"
                  description="暂无部门数据"
                />
              </template>
            </NSpin>
            <div class="xh-form-actions">
              <NButton v-access="['role:update']" type="primary" :loading="deptSaving" @click="handleSaveDataScope">
                保存
              </NButton>
            </div>
          </NTabPane>

          <!-- ===== 角色继承 ===== -->
          <NTabPane name="inherit" tab="角色继承" :disabled="!currentRoleId">
            <NSpin :show="inheritLoading">
              <div class="mb-2 text-sm text-gray-500">
                选择当前角色继承的父角色，父角色的权限与数据范围会自动继承到当前角色。
              </div>
              <NSelect
                v-model:value="checkedParentRoleIds"
                :options="parentRoleOptions"
                multiple
                clearable
                filterable
                placeholder="请选择父角色"
              />
            </NSpin>
            <div class="xh-form-actions">
              <NButton v-access="['role:update']" type="primary" :loading="inheritSaving" @click="handleSaveInheritance">
                保存
              </NButton>
            </div>
          </NTabPane>
        </NTabs>
      </div>
    </NModal>
  </div>
</template>

<style scoped>
:deep(.xh-role-menu-tree .xh-role-menu-button-group > .n-tree-node-children) {
  padding-left: 36px;
  white-space: normal;
}

:deep(.xh-role-menu-tree .xh-role-menu-button-group > .n-tree-node-children > .n-tree-node) {
  display: inline-flex;
  margin-right: 10px;
  margin-bottom: 4px;
  width: auto;
}

:deep(.xh-role-menu-tree .xh-role-menu-button-group > .n-tree-node-children > .n-tree-node > .n-tree-node-content) {
  padding-right: 8px;
}

:deep(.xh-role-menu-tree .xh-role-menu-button-node .n-tree-node-content__text) {
  font-size: 13px;
}
</style>
