<script setup lang="ts">
import type { CascaderOption } from 'naive-ui'
import type { VxeGridInstance } from 'vxe-table'
import type { ApiId, MenuCreateDto, MenuDetailDto, MenuListItemDto, MenuTreeNodeDto, MenuUpdateDto } from '@/api'
import {
  NButton,
  NCascader,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, onMounted, reactive, ref } from 'vue'
import {
  createDefaultQueryBehavior,
  createPageRequest,
  EnableStatus,
  menuApi,
  MenuType,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemMenuPage' })

interface MenuFormModel extends MenuCreateDto {
  basicId?: ApiId
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<MenuListItemDto>>()
const loading = ref(false)
const modalVisible = ref(false)
const submitLoading = ref(false)
const menuForm = ref<MenuFormModel>(createDefaultForm())
const tableData = ref<MenuListItemDto[]>([])
const treeNodes = ref<MenuTreeNodeDto[]>([])

const queryParams = reactive({
  keyword: '',
  menuType: undefined as MenuType | undefined,
  status: undefined as EnableStatus | undefined,
})

const statusOptions = [
  { label: '启用', value: EnableStatus.Enabled },
  { label: '禁用', value: EnableStatus.Disabled },
]

const menuTypeOptions = [
  { label: '目录', value: MenuType.Directory },
  { label: '菜单', value: MenuType.Menu },
  { label: '按钮', value: MenuType.Button },
  { label: '外链', value: MenuType.ExternalLink },
]

const modalTitle = computed(() => (menuForm.value.basicId ? '编辑菜单' : '新增菜单'))

function treeToCascaderOptions(nodes: MenuTreeNodeDto[]): CascaderOption[] {
  return nodes.map(node => ({
    children: node.children.length > 0 ? treeToCascaderOptions(node.children) : undefined,
    label: `${node.menuName}（${node.path}）`,
    value: node.basicId,
  }))
}

const cascaderOptions = computed(() => treeToCascaderOptions(treeNodes.value))

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

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

async function loadTree() {
  try {
    treeNodes.value = await menuApi.tree({ keyword: null, limit: 3000, onlyEnabled: false })
  }
  catch {
    treeNodes.value = []
  }
}

async function loadTable() {
  loading.value = true
  try {
    const result = await menuApi.page({
      ...createPageRequest({
        behavior: createDefaultQueryBehavior({
          disablePaging: true,
        }),
        page: {
          pageIndex: 1,
          pageSize: 5000,
        },
      }),
      keyword: normalizeNullable(queryParams.keyword),
      menuType: queryParams.menuType,
      status: queryParams.status,
    })
    tableData.value = result.items
  }
  catch {
    message.error('查询菜单失败')
    tableData.value = []
  }
  finally {
    loading.value = false
  }
}

onMounted(async () => {
  await Promise.all([loadTree(), loadTable()])
})

const tableOptions = useVxeTable<MenuListItemDto>(
  {
    data: [],
    columns: [
      { field: 'menuName', minWidth: 180, showOverflow: 'tooltip', title: '菜单名称', treeNode: true },
      { field: 'menuCode', minWidth: 150, showOverflow: 'tooltip', title: '菜单编码' },
      {
        field: 'menuType',
        formatter: ({ cellValue }) => getOptionLabel(menuTypeOptions, cellValue),
        title: '类型',
        width: 80,
      },
      { field: 'path', minWidth: 200, showOverflow: 'tooltip', title: '路径' },
      { field: 'icon', minWidth: 160, showOverflow: 'tooltip', title: '图标' },
      {
        field: 'isVisible',
        slots: { default: 'col_visible' },
        title: '可见',
        width: 70,
      },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '状态',
        width: 80,
      },
      { field: 'sort', sortable: true, title: '排序', width: 80 },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '创建时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 180,
      },
    ],
    id: 'sys_menu',
    name: '菜单管理',
  },
  {
    pagerConfig: { enabled: false },
    sortConfig: { remote: false },
    treeConfig: {
      expandAll: false,
      parentField: 'parentId',
      rowField: 'basicId',
      transform: true,
    },
  },
)

function handleSearch() {
  void loadTable()
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.menuType = undefined
  queryParams.status = undefined
  void loadTable()
}

function handleAdd(parentId?: ApiId) {
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

async function handleEdit(row: MenuListItemDto) {
  try {
    const detail = await menuApi.detail(row.basicId)
    menuForm.value = buildFormModel(detail ?? row)
  }
  catch {
    message.error('加载菜单详情失败')
    menuForm.value = buildFormModel(row)
  }
  modalVisible.value = true
}

async function handleToggleStatus(row: MenuListItemDto) {
  const nextStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await menuApi.updateStatus({ basicId: row.basicId, status: nextStatus })
    message.success('状态更新成功')
    await loadTable()
    await loadTree()
  }
  catch {
    message.error('状态更新失败')
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
        badge: normalizeNullable(menuForm.value.badge),
        badgeDot: menuForm.value.badgeDot,
        badgeType: normalizeNullable(menuForm.value.badgeType),
        basicId: menuForm.value.basicId,
        component: normalizeNullable(menuForm.value.component),
        externalUrl: normalizeNullable(menuForm.value.externalUrl),
        icon: normalizeNullable(menuForm.value.icon),
        isAffix: menuForm.value.isAffix,
        isCache: menuForm.value.isCache,
        isExternal: menuForm.value.isExternal,
        isVisible: menuForm.value.isVisible,
        menuName: menuForm.value.menuName.trim(),
        menuType: menuForm.value.menuType,
        metadata: normalizeNullable(menuForm.value.metadata),
        parentId: menuForm.value.parentId,
        path: menuForm.value.path.trim(),
        permissionId: menuForm.value.permissionId,
        redirect: normalizeNullable(menuForm.value.redirect),
        remark: normalizeNullable(menuForm.value.remark),
        routeName: normalizeNullable(menuForm.value.routeName),
        sort: menuForm.value.sort,
        title: normalizeNullable(menuForm.value.title),
      }
      await menuApi.update(updateInput)
    }
    else {
      const createInput: MenuCreateDto = {
        badge: normalizeNullable(menuForm.value.badge),
        badgeDot: menuForm.value.badgeDot,
        badgeType: normalizeNullable(menuForm.value.badgeType),
        component: normalizeNullable(menuForm.value.component),
        externalUrl: normalizeNullable(menuForm.value.externalUrl),
        icon: normalizeNullable(menuForm.value.icon),
        isAffix: menuForm.value.isAffix,
        isCache: menuForm.value.isCache,
        isExternal: menuForm.value.isExternal,
        isVisible: menuForm.value.isVisible,
        menuCode: menuForm.value.menuCode.trim(),
        menuName: menuForm.value.menuName.trim(),
        menuType: menuForm.value.menuType,
        metadata: normalizeNullable(menuForm.value.metadata),
        parentId: menuForm.value.parentId,
        path: menuForm.value.path.trim(),
        permissionId: menuForm.value.permissionId,
        redirect: normalizeNullable(menuForm.value.redirect),
        remark: normalizeNullable(menuForm.value.remark),
        routeName: normalizeNullable(menuForm.value.routeName),
        sort: menuForm.value.sort,
        status: menuForm.value.status,
        title: normalizeNullable(menuForm.value.title),
      }
      await menuApi.create(createInput)
    }

    message.success('保存成功')
    modalVisible.value = false
    await loadTable()
    await loadTree()
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索菜单名称/编码/路径"
          style="width: 250px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.menuType"
          :options="menuTypeOptions"
          clearable
          placeholder="菜单类型"
          style="width: 110px"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="statusOptions"
          clearable
          placeholder="状态"
          style="width: 100px"
        />
        <NButton size="small" type="primary" @click="handleSearch">
          <template #icon>
            <NIcon><Icon icon="lucide:search" /></NIcon>
          </template>
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          <template #icon>
            <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
          </template>
          重置
        </NButton>
      </div>
    </XSystemQueryPanel>

    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="tableOptions" :data="tableData" :loading="loading">
        <template #toolbar_buttons>
          <NButton size="small" type="primary" @click="handleAdd()">
            <template #icon>
              <NIcon><Icon icon="lucide:plus" /></NIcon>
            </template>
            新增菜单
          </NButton>
          <NButton size="small" @click="loadTable">
            <template #icon>
              <NIcon><Icon icon="lucide:refresh-cw" /></NIcon>
            </template>
            刷新
          </NButton>
        </template>

        <template #col_visible="{ row }">
          <NTag :type="row.isVisible ? 'success' : 'default'" round size="small">
            {{ row.isVisible ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_status="{ row }">
          <NTag :type="row.status === EnableStatus.Enabled ? 'success' : 'error'" round size="small">
            {{ getOptionLabel(statusOptions, row.status) }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton v-if="row.menuType !== MenuType.Button" size="small" text type="info" @click="handleAdd(row.basicId)">
              新增子项
            </NButton>
            <NButton size="small" text type="primary" @click="handleEdit(row)">
              编辑
            </NButton>
            <NButton
              :type="row.status === EnableStatus.Enabled ? 'warning' : 'success'"
              size="small"
              text
              @click="handleToggleStatus(row)"
            >
              {{ row.status === EnableStatus.Enabled ? '禁用' : '启用' }}
            </NButton>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 800px; max-width: 92vw"
    >
      <NForm :model="menuForm" class="xh-edit-form-grid" label-placement="top">
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
          <NCascader
            v-model:value="menuForm.parentId"
            :options="cascaderOptions"
            check-strategy="child"
            clearable
            placeholder="选择上级菜单（可留空）"
            style="width: 100%"
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
  </div>
</template>
