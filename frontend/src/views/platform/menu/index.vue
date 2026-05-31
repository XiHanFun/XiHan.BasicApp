<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { ApiId, MenuCreateDto, MenuDetailDto, MenuListItemDto, MenuTreeNodeDto, MenuUpdateDto } from '@/api'
import type { PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import {
  NButton,
  NConfigProvider,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NEmpty,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NSelect,
  NSpace,
  NSwitch,
  NTabPane,
  NTabs,
  NTag,
  NTreeSelect,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, ref } from 'vue'
import {
  createDefaultQueryBehavior,
  createPageRequest,
  EnableStatus,
  menuManagementApi,
  MenuType,
} from '@/api'
import { Icon, SchemaPage } from '~/components'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformMenuPage' })

interface MenuFormModel extends MenuCreateDto {
  basicId?: ApiId
}

interface MenuTreeItem extends MenuListItemDto {
  children?: MenuTreeItem[]
}

const message = useMessage()

const statusOptions = [
  { label: '启用', value: EnableStatus.Enabled },
  { label: '禁用', value: EnableStatus.Disabled },
]

const menuTypeOptions = [
  { label: '目录', value: MenuType.Directory },
  { label: '菜单', value: MenuType.Menu },
  { label: '按钮', value: MenuType.Button },
]

// 上级菜单树（NTreeSelect 选项来源，独立 ref，增删改后与表格一起 reload）
const treeNodes = ref<MenuTreeNodeDto[]>([])

const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<MenuDetailDto | null>(null)

const modalVisible = ref(false)
const submitLoading = ref(false)
const menuForm = ref<MenuFormModel>(createDefaultForm())

const schemaPageRef = ref<InstanceType<typeof SchemaPage> | null>(null)

const modalTitle = computed(() => (menuForm.value.basicId ? '编辑菜单' : '新增菜单'))

function toStr(value?: string | null) {
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
  return value ? '是' : '否'
}

function formatStatus(value?: EnableStatus | null) {
  return getOptionLabel(statusOptions, value)
}

function createDefaultForm(): MenuFormModel {
  return {
    badge: null,
    badgeDot: false,
    badgeType: null,
    component: null,
    externalUrl: null,
    icon: null,
    isAffix: false,
    isCache: false,
    isExternal: false,
    isVisible: true,
    menuCode: '',
    menuName: '',
    menuType: MenuType.Menu,
    metadata: null,
    parentId: null,
    path: '',
    permissionId: null,
    redirect: null,
    remark: null,
    routeName: null,
    sort: 100,
    status: EnableStatus.Enabled,
    title: null,
  }
}

// --- 上级菜单 NTreeSelect ---
interface MenuTreeSelectOption {
  key: ApiId
  label: string
  children?: MenuTreeSelectOption[]
}

function buildTreeSelectOptions(nodes: MenuTreeNodeDto[]): MenuTreeSelectOption[] {
  return nodes.map(node => ({
    key: node.basicId,
    label: `${node.menuName}（${node.path}）`,
    children: node.children?.length ? buildTreeSelectOptions(node.children) : undefined,
  }))
}

const treeSelectOptions = computed(() => buildTreeSelectOptions(treeNodes.value))

async function loadTree() {
  try {
    treeNodes.value = await menuManagementApi.tree({ keyword: null, limit: 3000, onlyEnabled: false })
  }
  catch {
    treeNodes.value = []
  }
}

// --- 客户端组树（保留全部列字段：状态/可见/创建时间等，tree() DTO 不含这些字段，故由 page() 组树） ---
function buildTree(items: MenuListItemDto[]): MenuTreeItem[] {
  const map = new Map<ApiId, MenuTreeItem>()
  const roots: MenuTreeItem[] = []

  for (const item of items) {
    map.set(item.basicId, { ...item, children: [] })
  }

  for (const item of items) {
    const node = map.get(item.basicId)!
    if (item.parentId && map.has(item.parentId)) {
      map.get(item.parentId)!.children!.push(node)
    }
    else {
      roots.push(node)
    }
  }

  return roots
}

const schema: PageSchema<MenuListItemDto> = {
  pageCode: 'platform.menu',
  pageName: '菜单管理',
  rowKey: 'basicId',
  scrollX: 2000,
  tree: { childrenKey: 'children', defaultExpandAll: true },
  resource: {
    tree: (params: SchemaQueryParams) => {
      const result = menuManagementApi.page({
        ...createPageRequest({
          behavior: createDefaultQueryBehavior({ disablePaging: true }),
          page: { pageIndex: 1, pageSize: 5000 },
        }),
        keyword: toStr(params.filters.keyword as string | undefined),
        menuType: (params.filters.menuType as MenuType | undefined) || undefined,
        status: (params.filters.status as EnableStatus | undefined) || undefined,
      })
      return result.then(res => buildTree(res.items)) as unknown as Promise<MenuListItemDto[]>
    },
    remove: (id: ApiId) => menuManagementApi.delete(id),
  },
  fields: [
    {
      key: 'menuName',
      title: '菜单名称',
      dataType: 'string',
      treeColumn: true,
      searchable: true,
      searchPlaceholder: '搜索菜单名称/编码/路径',
      minWidth: 200,
      order: 0,
    },
    {
      key: 'menuCode',
      title: '菜单编码',
      dataType: 'string',
      minWidth: 150,
      order: 1,
    },
    {
      key: 'menuType',
      title: '类型',
      dataType: 'enum',
      options: menuTypeOptions,
      searchable: true,
      searchPlaceholder: '菜单类型',
      width: 90,
      order: 2,
      render: (row: MenuListItemDto) =>
        h(NTag, { size: 'small', round: true, bordered: false }, () => getOptionLabel(menuTypeOptions, row.menuType)),
    },
    {
      key: 'path',
      title: '路径',
      dataType: 'string',
      minWidth: 200,
      order: 3,
    },
    {
      key: 'icon',
      title: '图标',
      dataType: 'string',
      minWidth: 150,
      order: 4,
      render: (row: MenuListItemDto) => formatNullable(row.icon),
    },
    {
      key: 'isVisible',
      title: '可见',
      dataType: 'boolean',
      width: 80,
      order: 5,
      render: (row: MenuListItemDto) =>
        h(NTag, { size: 'small', round: true, type: row.isVisible ? 'success' : 'default' }, () => (row.isVisible ? '是' : '否')),
    },
    {
      key: 'status',
      title: '状态',
      dataType: 'enum',
      options: statusOptions,
      searchable: true,
      searchPlaceholder: '状态',
      width: 90,
      order: 6,
      render: (row: MenuListItemDto) =>
        h(NTag, { size: 'small', round: true, bordered: false, type: row.status === EnableStatus.Enabled ? 'success' : 'error' }, () => getOptionLabel(statusOptions, row.status)),
    },
    {
      key: 'sort',
      title: '排序',
      dataType: 'number',
      width: 80,
      order: 7,
    },
    {
      key: 'createdTime',
      title: '创建时间',
      dataType: 'datetime',
      minWidth: 170,
      order: 8,
      render: (row: MenuListItemDto) => formatDate(row.createdTime),
    },
  ],
  actions: [
    { key: 'create', title: '新增菜单', scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'addChild', title: '新增子项', scope: 'row', icon: 'lucide:plus', visible: (row: MenuListItemDto) => row.menuType !== MenuType.Button },
    { key: 'view', title: '详情', scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: '编辑', scope: 'row', icon: 'lucide:pen' },
    { key: 'toggle', title: '启停', scope: 'row', icon: 'lucide:power' },
    { key: 'delete', title: '删除', scope: 'row', type: 'error', icon: 'lucide:trash-2', confirm: true, confirmText: '确认删除该菜单？' },
  ],
}

function onAction(payload: SchemaActionPayload) {
  const { key, scope } = payload
  const row = payload.row as unknown as MenuListItemDto | undefined

  if (scope === 'page' && key === 'create') {
    openCreate()
    return
  }
  if (scope === 'row' && row) {
    if (key === 'addChild')
      openCreate(row.basicId)
    else if (key === 'view')
      void openDetail(row)
    else if (key === 'edit')
      void openEdit(row)
    else if (key === 'toggle')
      void toggleStatus(row)
    else if (key === 'delete')
      void removeRow(row)
  }
}

function openCreate(parentId?: ApiId | null) {
  menuForm.value = createDefaultForm()
  menuForm.value.parentId = parentId ?? null
  modalVisible.value = true
}

function buildFormModel(row: MenuDetailDto | MenuListItemDto): MenuFormModel {
  return {
    ...createDefaultForm(),
    badge: 'badge' in row ? row.badge ?? null : null,
    badgeDot: 'badgeDot' in row ? row.badgeDot : false,
    badgeType: 'badgeType' in row ? row.badgeType ?? null : null,
    basicId: row.basicId,
    component: row.component ?? null,
    externalUrl: 'externalUrl' in row ? row.externalUrl ?? null : null,
    icon: row.icon ?? null,
    isAffix: row.isAffix,
    isCache: row.isCache,
    isExternal: row.isExternal,
    isVisible: row.isVisible,
    menuCode: row.menuCode,
    menuName: row.menuName,
    menuType: row.menuType,
    metadata: 'metadata' in row ? row.metadata ?? null : null,
    parentId: row.parentId ?? null,
    path: row.path,
    permissionId: row.permissionId ?? null,
    redirect: row.redirect ?? null,
    remark: 'remark' in row ? row.remark ?? null : null,
    routeName: row.routeName ?? null,
    sort: row.sort,
    status: row.status,
    title: row.title ?? null,
  }
}

async function openEdit(row: MenuListItemDto) {
  try {
    const detail = await menuManagementApi.detail(row.basicId)
    menuForm.value = buildFormModel(detail ?? row)
  }
  catch {
    message.error('加载菜单详情失败')
    menuForm.value = buildFormModel(row)
  }
  modalVisible.value = true
}

async function openDetail(row: MenuListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null

  try {
    currentDetail.value = await menuManagementApi.detail(row.basicId)
    if (!currentDetail.value) {
      message.warning('未查询到菜单详情')
    }
  }
  catch {
    message.error('加载菜单详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

async function toggleStatus(row: MenuListItemDto) {
  const next = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await menuManagementApi.updateStatus({ basicId: row.basicId, status: next })
    message.success('状态更新成功')
    schemaPageRef.value?.reload()
    void loadTree()
  }
  catch {
    message.error('状态更新失败')
  }
}

async function removeRow(row: MenuListItemDto) {
  try {
    await menuManagementApi.delete(row.basicId)
    message.success('删除成功')
    schemaPageRef.value?.reload()
    void loadTree()
  }
  catch {
    message.error('删除失败')
  }
}

function validateForm() {
  if (!menuForm.value.menuName.trim()) {
    message.warning('请输入菜单名称')
    return false
  }
  if (!menuForm.value.basicId && !menuForm.value.menuCode.trim()) {
    message.warning('请输入菜单编码')
    return false
  }
  if (!menuForm.value.path.trim()) {
    message.warning('请输入路由路径')
    return false
  }
  return true
}

async function handleSubmit() {
  if (!validateForm())
    return

  submitLoading.value = true
  try {
    if (menuForm.value.basicId) {
      const updateInput: MenuUpdateDto = {
        badge: toStr(menuForm.value.badge),
        badgeDot: menuForm.value.badgeDot,
        badgeType: toStr(menuForm.value.badgeType),
        basicId: menuForm.value.basicId,
        component: toStr(menuForm.value.component),
        externalUrl: toStr(menuForm.value.externalUrl),
        icon: toStr(menuForm.value.icon),
        isAffix: menuForm.value.isAffix,
        isCache: menuForm.value.isCache,
        isExternal: menuForm.value.isExternal,
        isVisible: menuForm.value.isVisible,
        menuName: menuForm.value.menuName.trim(),
        menuType: menuForm.value.menuType,
        metadata: toStr(menuForm.value.metadata),
        parentId: menuForm.value.parentId,
        path: menuForm.value.path.trim(),
        permissionId: menuForm.value.permissionId,
        redirect: toStr(menuForm.value.redirect),
        remark: toStr(menuForm.value.remark),
        routeName: toStr(menuForm.value.routeName),
        sort: menuForm.value.sort,
        title: toStr(menuForm.value.title),
      }
      await menuManagementApi.update(updateInput)
    }
    else {
      const createInput: MenuCreateDto = {
        badge: toStr(menuForm.value.badge),
        badgeDot: menuForm.value.badgeDot,
        badgeType: toStr(menuForm.value.badgeType),
        component: toStr(menuForm.value.component),
        externalUrl: toStr(menuForm.value.externalUrl),
        icon: toStr(menuForm.value.icon),
        isAffix: menuForm.value.isAffix,
        isCache: menuForm.value.isCache,
        isExternal: menuForm.value.isExternal,
        isVisible: menuForm.value.isVisible,
        menuCode: menuForm.value.menuCode.trim(),
        menuName: menuForm.value.menuName.trim(),
        menuType: menuForm.value.menuType,
        metadata: toStr(menuForm.value.metadata),
        parentId: menuForm.value.parentId,
        path: menuForm.value.path.trim(),
        permissionId: menuForm.value.permissionId,
        redirect: toStr(menuForm.value.redirect),
        remark: toStr(menuForm.value.remark),
        routeName: toStr(menuForm.value.routeName),
        sort: menuForm.value.sort,
        status: menuForm.value.status,
        title: toStr(menuForm.value.title),
      }
      await menuManagementApi.create(createInput)
    }

    message.success('保存成功')
    modalVisible.value = false
    schemaPageRef.value?.reload()
    void loadTree()
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

// 详情弹窗子菜单（从菜单树定位当前节点的 children 展示）
const childMenuColumns: DataTableColumns<MenuTreeNodeDto> = [
  { title: '菜单名称', key: 'menuName', minWidth: 120, ellipsis: { tooltip: true } },
  { title: '编码', key: 'menuCode', width: 110, ellipsis: { tooltip: true } },
  {
    title: '类型',
    key: 'menuType',
    width: 80,
    render: row => getOptionLabel(menuTypeOptions, row.menuType),
  },
  {
    title: '路径',
    key: 'path',
    minWidth: 120,
    ellipsis: { tooltip: true },
    render: row => formatNullable(row.path),
  },
]

function findNodeChildren(nodes: MenuTreeNodeDto[], id: ApiId): MenuTreeNodeDto[] {
  for (const node of nodes) {
    if (node.basicId === id)
      return node.children ?? []
    if (node.children?.length) {
      const found = findNodeChildren(node.children, id)
      if (found.length)
        return found
    }
  }
  return []
}

const childMenus = computed(() => {
  if (!currentDetail.value)
    return []
  return findNodeChildren(treeNodes.value, currentDetail.value.basicId)
})

onMounted(() => {
  void loadTree()
})
</script>

<template>
  <div class="flex overflow-hidden flex-col h-full">
    <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction" />

    <NModal
      v-model:show="detailVisible"
      class="xh-mgmt-detail-modal"
      preset="card"
      :bordered="false"
      :mask-closable="true"
      style="width: 720px; max-width: calc(100vw - 32px);"
    >
      <template v-if="currentDetail" #header>
        <div class="det-hd-entity">
          <div class="det-hd-ico">
            <Icon icon="tabler:menu-2" :size="22" />
          </div>
          <div class="min-w-0">
            <div class="det-hd-name">
              {{ currentDetail.menuName }}
            </div>
            <div class="det-hd-sub">
              {{ currentDetail.menuCode }}
            </div>
          </div>
        </div>
      </template>

      <div v-if="detailLoading" class="modal-loading">
        加载中…
      </div>
      <NTabs v-else-if="currentDetail" type="line" animated size="small">
        <NTabPane name="overview" tab="概览">
          <NDescriptions :column="2" bordered size="small">
            <NDescriptionsItem label="菜单类型">
              {{ getOptionLabel(menuTypeOptions, currentDetail.menuType) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="状态">
              <NTag size="small" :type="currentDetail.status === EnableStatus.Enabled ? 'success' : 'error'" :bordered="false">
                {{ formatStatus(currentDetail.status) }}
              </NTag>
            </NDescriptionsItem>
            <NDescriptionsItem label="路由路径">
              {{ formatNullable(currentDetail.path) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="组件路径">
              {{ formatNullable(currentDetail.component) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="路由名称">
              {{ formatNullable(currentDetail.routeName) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="图标">
              {{ formatNullable(currentDetail.icon) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="标题">
              {{ formatNullable(currentDetail.title) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="是否外链">
              {{ formatBoolean(currentDetail.isExternal) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="是否缓存">
              {{ formatBoolean(currentDetail.isCache) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="是否可见">
              {{ formatBoolean(currentDetail.isVisible) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="是否固定">
              {{ formatBoolean(currentDetail.isAffix) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="排序">
              {{ currentDetail.sort }}
            </NDescriptionsItem>
            <NDescriptionsItem label="权限 ID">
              {{ formatNullable(currentDetail.permissionId) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="创建时间">
              {{ formatNullableDate(currentDetail.createdTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem v-if="currentDetail.remark" label="备注" :span="2">
              {{ currentDetail.remark }}
            </NDescriptionsItem>
          </NDescriptions>
        </NTabPane>
        <NTabPane name="children" :tab="`子菜单 (${childMenus.length})`">
          <div class="xh-detail-table-wrap">
            <NDataTable
              v-if="childMenus.length"
              :columns="childMenuColumns"
              :data="childMenus"
              :bordered="false"
              size="small"
              :row-key="(row: MenuTreeNodeDto) => row.basicId"
            />
            <NEmpty v-else description="暂无子菜单" style="padding: 32px 0" />
          </div>
        </NTabPane>
      </NTabs>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="detailVisible = false">
            关闭
          </NButton>
          <NButton
            v-if="currentDetail"
            size="small"
            type="primary"
            @click="detailVisible = false; openEdit(currentDetail as unknown as MenuListItemDto)"
          >
            编辑
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 800px; max-width: 92vw"
    >
      <NConfigProvider size="small" abstract>
        <NForm :model="menuForm" size="small" class="xh-edit-form-grid" label-placement="top">
          <NFormItem label="菜单名称" path="menuName">
            <NInput v-model:value="menuForm.menuName" clearable placeholder="请输入菜单名称" />
          </NFormItem>
          <NFormItem label="菜单编码" path="menuCode">
            <NInput
              v-model:value="menuForm.menuCode"
              :disabled="Boolean(menuForm.basicId)"
              clearable
              placeholder="如: system.user"
            />
          </NFormItem>
          <NFormItem label="上级菜单" path="parentId">
            <NTreeSelect
              v-model:value="menuForm.parentId"
              :options="treeSelectOptions"
              key-field="key"
              label-field="label"
              clearable
              placeholder="选择上级菜单（可留空，留空为顶级）"
            />
          </NFormItem>
          <NFormItem label="菜单类型" path="menuType">
            <NSelect v-model:value="menuForm.menuType" :options="menuTypeOptions" />
          </NFormItem>
          <NFormItem label="路由路径" path="path">
            <NInput v-model:value="menuForm.path" clearable placeholder="如: /system/user" />
          </NFormItem>
          <NFormItem label="路由名称" path="routeName">
            <NInput v-model:value="menuForm.routeName" clearable placeholder="如: SystemUser" />
          </NFormItem>
          <NFormItem label="组件路径" path="component">
            <NInput v-model:value="menuForm.component" clearable placeholder="如: system/user/index" />
          </NFormItem>
          <NFormItem label="重定向" path="redirect">
            <NInput v-model:value="menuForm.redirect" clearable placeholder="目录类型填入默认子路由" />
          </NFormItem>
          <NFormItem label="图标" path="icon">
            <NInput v-model:value="menuForm.icon" clearable placeholder="如: lucide:users" />
          </NFormItem>
          <NFormItem label="标题" path="title">
            <NInput v-model:value="menuForm.title" clearable placeholder="显示标题" />
          </NFormItem>
          <NFormItem label="排序" path="sort">
            <NInputNumber v-model:value="menuForm.sort" :min="0" style="width: 100%" />
          </NFormItem>
          <NFormItem label="可见">
            <NSwitch v-model:value="menuForm.isVisible" />
          </NFormItem>
          <NFormItem label="缓存">
            <NSwitch v-model:value="menuForm.isCache" />
          </NFormItem>
          <NFormItem label="固定标签">
            <NSwitch v-model:value="menuForm.isAffix" />
          </NFormItem>
          <NFormItem label="备注" path="remark">
            <NInput v-model:value="menuForm.remark" clearable placeholder="请输入备注" :rows="3" type="textarea" />
          </NFormItem>
        </NForm>
      </NConfigProvider>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="modalVisible = false">
            取消
          </NButton>
          <NButton size="small" :loading="submitLoading" type="primary" @click="handleSubmit">
            保存
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}
</style>
