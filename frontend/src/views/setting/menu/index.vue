<script setup lang="ts">
import type { DataTableColumns, TreeSelectOption } from 'naive-ui'
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
  EnableStatus,
  menuManagementApi,
  MenuType,
} from '@/api'
import { Icon, IconPicker, SchemaPage } from '~/components'
import { useUserStore } from '~/stores'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformMenuPage' })

const userStore = useUserStore()

/**
 * 全局菜单(TenantId=0)仅平台运维态可维护；非平台态隐藏编辑/启停/删除入口，
 * 避免点击后撞后端「平台级全局菜单仅平台运维态可维护」错误。
 */
function canMaintainMenu(row: unknown): boolean {
  const menu = row as MenuListItemDto
  return !menu.isGlobal || (userStore.userInfo?.isPlatform ?? false)
}

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

const badgeTypeOptions = [
  { label: '默认', value: 'default' },
  { label: '主要', value: 'primary' },
  { label: '信息', value: 'info' },
  { label: '成功', value: 'success' },
  { label: '警告', value: 'warning' },
  { label: '错误', value: 'error' },
]

type BadgeTagType = 'default' | 'primary' | 'info' | 'success' | 'warning' | 'error'

const badgeDotColorMap: Record<string, string> = {
  default: '#909399',
  primary: '#2080f0',
  info: '#2080f0',
  success: '#18a058',
  warning: '#f0a020',
  error: '#d03050',
}

function badgeTagType(value?: string | null): BadgeTagType {
  return (value && badgeTypeOptions.some(o => o.value === value) ? value : 'default') as BadgeTagType
}

function badgeDotColor(value?: string | null) {
  return (value && badgeDotColorMap[value]) || badgeDotColorMap.default
}

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
    i18nKey: null,
  }
}

// --- 上级菜单 NTreeSelect（key-field=key / label-field=label） ---
function buildTreeSelectOptions(nodes: MenuTreeNodeDto[]): TreeSelectOption[] {
  return nodes.map(node => ({
    key: node.basicId,
    label: `${node.menuName}（${node.path}）`,
    children: node.children?.length ? buildTreeSelectOptions(node.children) : undefined,
  }))
}

const treeSelectOptions = computed(() => buildTreeSelectOptions(treeNodes.value))

async function loadTree() {
  try {
    treeNodes.value = await menuManagementApi.tree({ includeButtons: false, keyword: null, limit: 3000, onlyEnabled: false })
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

const schema: PageSchema = {
  pageCode: 'platform.menu',
  pageName: '菜单管理',
  rowKey: 'basicId',
  scrollX: 2000,
  tree: { childrenKey: 'children', defaultExpandAll: false },
  resource: {
    tree: (params: SchemaQueryParams) => {
      const result = menuManagementApi.list({
        keyword: toStr(params.filters.keyword as string | undefined),
        menuType: (params.filters.menuType as MenuType | undefined) || undefined,
        status: (params.filters.status as EnableStatus | undefined) || undefined,
      })
      return result.then(items => buildTree(items)) as unknown as Promise<Record<string, unknown>[]>
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
      render: (row) => {
        const item = row as unknown as MenuListItemDto
        return h('span', { style: 'display:inline-flex;align-items:center;gap:6px' }, [
          item.icon
            ? h(Icon, { icon: item.icon, width: 16 })
            : h('span', { style: 'display:inline-block;width:16px' }),
          h('span', item.menuName),
        ])
      },
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
      render: row =>
        h(NTag, { size: 'small', round: true, bordered: false }, () => getOptionLabel(menuTypeOptions, (row as unknown as MenuListItemDto).menuType)),
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
      minWidth: 170,
      order: 4,
      render: (row) => {
        const item = row as unknown as MenuListItemDto
        if (!item.icon) {
          return '-'
        }
        return h('span', { style: 'display:inline-flex;align-items:center;gap:6px' }, [
          h(Icon, { icon: item.icon, width: 16 }),
          h('span', item.icon),
        ])
      },
    },
    {
      key: 'badge',
      title: '标签',
      dataType: 'string',
      width: 110,
      order: 5,
      render: (row) => {
        const item = row as unknown as MenuListItemDto
        if (item.badgeDot) {
          return h('span', {
            style: {
              display: 'inline-block',
              width: '8px',
              height: '8px',
              borderRadius: '50%',
              backgroundColor: badgeDotColor(item.badgeType),
            },
          })
        }
        if (item.badge) {
          return h(NTag, { size: 'small', round: true, bordered: false, type: badgeTagType(item.badgeType) }, () => item.badge)
        }
        return '-'
      },
    },
    {
      key: 'isVisible',
      title: '可见',
      dataType: 'boolean',
      width: 80,
      order: 6,
      render: row =>
        h(NTag, { size: 'small', round: true, type: (row as unknown as MenuListItemDto).isVisible ? 'success' : 'default' }, () => ((row as unknown as MenuListItemDto).isVisible ? '是' : '否')),
    },
    {
      key: 'status',
      title: '状态',
      dataType: 'enum',
      options: statusOptions,
      searchable: true,
      searchPlaceholder: '状态',
      width: 90,
      order: 7,
      render: row =>
        h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as MenuListItemDto).status === EnableStatus.Enabled ? 'success' : 'error' }, () => getOptionLabel(statusOptions, (row as unknown as MenuListItemDto).status)),
    },
    {
      key: 'sort',
      title: '排序',
      dataType: 'number',
      width: 80,
      order: 8,
    },
    {
      key: 'createdTime',
      title: '创建时间',
      dataType: 'datetime',
      minWidth: 170,
      order: 9,
      render: row => formatDate((row as unknown as MenuListItemDto).createdTime),
    },
  ],
  actions: [
    { key: 'create', title: '新增菜单', scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'addChild', title: '新增子项', scope: 'row', icon: 'lucide:plus', visible: row => (row as unknown as MenuListItemDto).menuType !== MenuType.Button },
    { key: 'view', title: '详情', scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: '编辑', scope: 'row', icon: 'lucide:pen', visible: canMaintainMenu },
    { key: 'toggle', title: '启停', scope: 'row', icon: 'lucide:power', visible: canMaintainMenu },
    { key: 'delete', title: '删除', scope: 'row', type: 'error', icon: 'lucide:trash-2', confirm: true, confirmText: '确认删除该菜单？', visible: canMaintainMenu },
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
    i18nKey: row.i18nKey ?? null,
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
  const form = menuForm.value
  if (!form.menuName.trim()) {
    message.warning('请输入菜单名称')
    return false
  }
  if (!form.basicId && !form.menuCode.trim()) {
    message.warning('请输入菜单编码')
    return false
  }
  // 按钮无路由，目录/菜单需要路由路径
  if (form.menuType !== MenuType.Button && !form.path.trim()) {
    message.warning('请输入路由路径')
    return false
  }
  // 非外链菜单需要组件路径（与后端校验一致）
  if (form.menuType === MenuType.Menu && !form.isExternal && !form.component?.trim()) {
    message.warning('请输入组件路径')
    return false
  }
  // 外链菜单需要外链地址
  if (form.isExternal && !form.externalUrl?.trim()) {
    message.warning('请输入外链地址')
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
        i18nKey: toStr(menuForm.value.i18nKey),
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
        i18nKey: toStr(menuForm.value.i18nKey),
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
              <span v-if="currentDetail.icon" style="display: inline-flex; align-items: center; gap: 6px">
                <Icon :icon="currentDetail.icon" width="16" />
                <span>{{ currentDetail.icon }}</span>
              </span>
              <span v-else>-</span>
            </NDescriptionsItem>
            <NDescriptionsItem label="标题">
              {{ formatNullable(currentDetail.title) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="国际化键">
              {{ formatNullable(currentDetail.i18nKey) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="标签">
              <NTag v-if="currentDetail.badge" size="small" round :bordered="false" :type="badgeTagType(currentDetail.badgeType)">
                {{ currentDetail.badge }}
              </NTag>
              <span
                v-else-if="currentDetail.badgeDot"
                :style="{ display: 'inline-block', width: '8px', height: '8px', borderRadius: '50%', backgroundColor: badgeDotColor(currentDetail.badgeType) }"
              />
              <span v-else>-</span>
            </NDescriptionsItem>
            <NDescriptionsItem label="是否外链">
              {{ formatBoolean(currentDetail.isExternal) }}
            </NDescriptionsItem>
            <NDescriptionsItem v-if="currentDetail.isExternal" label="外链地址" :span="2">
              {{ formatNullable(currentDetail.externalUrl) }}
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
            <IconPicker v-model="menuForm.icon" placeholder="点击选择图标" />
          </NFormItem>
          <NFormItem label="标题" path="title">
            <NInput v-model:value="menuForm.title" clearable placeholder="显示标题" />
          </NFormItem>
          <NFormItem label="国际化键" path="i18nKey">
            <NInput v-model:value="menuForm.i18nKey" clearable placeholder="如 menu.identity_user（可选，按键翻译标题）" />
          </NFormItem>
          <NFormItem label="排序" path="sort">
            <NInputNumber v-model:value="menuForm.sort" :min="0" style="width: 100%" />
          </NFormItem>
          <NFormItem label="标签内容" path="badge">
            <NInput v-model:value="menuForm.badge" clearable placeholder="如: New / 3（显示在菜单项右侧）" />
          </NFormItem>
          <NFormItem label="标签类型" path="badgeType">
            <NSelect v-model:value="menuForm.badgeType" :options="badgeTypeOptions" clearable placeholder="标签颜色" />
          </NFormItem>
          <NFormItem label="标签圆点">
            <NSwitch v-model:value="menuForm.badgeDot" />
          </NFormItem>
          <NFormItem v-if="!menuForm.basicId" label="状态" path="status">
            <NSelect v-model:value="menuForm.status" :options="statusOptions" />
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
          <NFormItem label="外链">
            <NSwitch v-model:value="menuForm.isExternal" />
          </NFormItem>
          <NFormItem v-if="menuForm.isExternal" label="外链地址" path="externalUrl">
            <NInput v-model:value="menuForm.externalUrl" clearable placeholder="https://..." />
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
