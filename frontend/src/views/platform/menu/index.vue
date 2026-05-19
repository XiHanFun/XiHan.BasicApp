<script setup lang="ts">
import type { CascaderOption, DataTableColumns } from 'naive-ui'
import type { ApiId, MenuCreateDto, MenuDetailDto, MenuListItemDto, MenuTreeNodeDto, MenuUpdateDto } from '@/api'
import {
  NButton,
  NCard,
  NConfigProvider,
  NCascader,
  NDataTable,
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
  NPagination,
  NScrollbar,
  NSelect,
  NSpace,
  NSpin,
  NSwitch,
  NTabPane,
  NTabs,
  NTag,
  NTooltip,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'
import {
  createDefaultQueryBehavior,
  createPageRequest,
  EnableStatus,
  menuManagementApi,
  MenuType,
} from '@/api'
import { Icon } from '~/components'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformMenuPage' })

interface MenuFormModel extends MenuCreateDto {
  basicId?: ApiId
}

const message = useMessage()
const tableLoading = ref(false)
const dataList = ref<MenuListItemDto[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(20)
const modalVisible = ref(false)
const submitLoading = ref(false)
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<MenuDetailDto | null>(null)
const menuForm = ref<MenuFormModel>(createDefaultForm())
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

const childMenus = computed(() => {
  if (!currentDetail.value) return []
  return dataList.value.filter(item => item.parentId === currentDetail.value!.basicId)
})

async function loadTree() {
  try {
    treeNodes.value = await menuManagementApi.tree({ keyword: null, limit: 3000, onlyEnabled: false })
  }
  catch {
    treeNodes.value = []
  }
}

async function fetchData() {
  tableLoading.value = true
  try {
    const result = await menuManagementApi.page({
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
    dataList.value = result.items
    totalCount.value = result.page.totalCount
  }
  catch {
    message.error('查询菜单失败')
    dataList.value = []
    totalCount.value = 0
  }
  finally {
    tableLoading.value = false
  }
}

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

function handlePageChange(page: number) {
  currentPage.value = page
}

function handlePageSizeChange(size: number) {
  pageSize.value = size
  currentPage.value = 1
}

const tableColumns = computed<DataTableColumns<MenuListItemDto>>(() => [
  {
    key: 'menuName',
    title: '菜单名称',
    minWidth: 180,
    ellipsis: { tooltip: true },
  },
  {
    key: 'menuCode',
    title: '菜单编码',
    minWidth: 150,
    ellipsis: { tooltip: true },
  },
  {
    key: 'menuType',
    title: '类型',
    width: 80,
    render(row) {
      return h('span', { style: 'font-size:13px;' }, getOptionLabel(menuTypeOptions, row.menuType))
    },
  },
  {
    key: 'path',
    title: '路径',
    minWidth: 200,
    ellipsis: { tooltip: true },
  },
  {
    key: 'icon',
    title: '图标',
    minWidth: 160,
    ellipsis: { tooltip: true },
  },
  {
    key: 'isVisible',
    title: '可见',
    width: 70,
    render(row) {
      return h(NTag, { type: row.isVisible ? 'success' : 'default', round: true, size: 'small' }, () => row.isVisible ? '是' : '否')
    },
  },
  {
    key: 'status',
    title: '状态',
    width: 80,
    render(row) {
      return h(NTag, { type: row.status === EnableStatus.Enabled ? 'success' : 'error', round: true, size: 'small' }, () => getOptionLabel(statusOptions, row.status))
    },
  },
  {
    key: 'sort',
    title: '排序',
    width: 80,
    sorter: true,
  },
  {
    key: 'createdTime',
    title: '创建时间',
    minWidth: 170,
    sorter: true,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate(row.createdTime))
    },
  },
  {
    key: 'actions',
    title: '操作',
    width: 216,
    fixed: 'right',
    render(row) {
      return h(NSpace, { size: 'small' }, () => [
        h(NTooltip, null, {
          trigger: () =>
            h(NButton, {
              ariaLabel: '查看详情',
              circle: true,
              quaternary: true,
              size: 'small',
              onClick: () => handleView(row),
            }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:eye' })) }),
          default: () => '查看详情',
        }),
        row.menuType !== MenuType.Button
          ? h(NButton, { size: 'small', text: true, type: 'info', onClick: () => handleAdd(row.basicId) }, () => '新增子项')
          : null,
        h(NButton, { size: 'small', text: true, type: 'primary', onClick: () => handleEdit(row) }, () => '编辑'),
        h(NButton, {
          type: row.status === EnableStatus.Enabled ? 'warning' : 'success',
          size: 'small',
          text: true,
          onClick: () => handleToggleStatus(row),
        }, () => row.status === EnableStatus.Enabled ? '禁用' : '启用'),
      ])
    },
  },
])

function handleSearch() {
  void fetchData()
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.menuType = undefined
  queryParams.status = undefined
  void fetchData()
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
    const detail = await menuManagementApi.detail(row.basicId)
    menuForm.value = buildFormModel(detail ?? row)
  }
  catch {
    message.error('加载菜单详情失败')
    menuForm.value = buildFormModel(row)
  }
  modalVisible.value = true
}

async function handleView(row: MenuListItemDto) {
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

async function handleToggleStatus(row: MenuListItemDto) {
  const nextStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await menuManagementApi.updateStatus({ basicId: row.basicId, status: nextStatus })
    message.success('状态更新成功')
    await fetchData()
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
      await menuManagementApi.update(updateInput)
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
      await menuManagementApi.create(createInput)
    }

    message.success('保存成功')
    modalVisible.value = false
    await fetchData()
    await loadTree()
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

onMounted(async () => {
  await Promise.all([loadTree(), fetchData()])
})
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <div class="xh-query-panel mb-2" style="flex-shrink:0;padding:10px 16px;background:var(--n-card-color);border-radius:var(--n-border-radius);">
      <NConfigProvider size="small" abstract>
        <div class="xh-query-panel__content">
        <NInput
          v-model:value="queryParams.keyword"
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
      </NConfigProvider>
    </div>

    <NCard content-style="padding:0;display:flex;flex-direction:column;height:100%;" :bordered="false" class="flex-1" style="height:0;">
      <div style="padding:12px 16px;flex-shrink:0;">
        <NButton size="small" type="primary" @click="handleAdd()">
          <template #icon>
            <NIcon><Icon icon="lucide:plus" /></NIcon>
          </template>
          新增菜单
        </NButton>
        <NButton size="small" @click="fetchData" style="margin-left:8px;">
          <template #icon>
            <NIcon><Icon icon="lucide:refresh-cw" /></NIcon>
          </template>
          刷新
        </NButton>
      </div>

      <NDataTable
        :columns="tableColumns"
        :data="dataList"
        :loading="tableLoading"
        :bordered="false"
        :single-line="false"
        :row-key="(row: MenuListItemDto) => row.basicId"
        :scroll-x="2000"
        size="small"
        striped
        flex-height
        style="flex:1;"
      />
      <div style="display:flex;align-items:center;justify-content:space-between;padding:14px 20px;border-top:1px solid var(--n-border-color);flex-shrink:0;">
        <div style="font-size:13px;color:var(--n-text-color-3);">
          共 <strong>{{ totalCount }}</strong> 条，第 <strong>{{ currentPage }}</strong> / {{ totalPages }} 页
        </div>
        <NPagination
          size="small"
          :page="currentPage"
          :page-size="pageSize"
          :page-count="totalPages"
          :page-sizes="[10, 20, 50, 100]"
          show-size-picker
          @update:page="handlePageChange"
          @update:page-size="handlePageSizeChange"
        />
      </div>
    </NCard>

    <NDrawer v-model:show="detailVisible" :width="820">
      <NDrawerContent closable title="菜单详情">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="xh-detail-empty" description="暂无菜单详情">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="currentDetail" style="max-height: calc(100vh - 120px)">
            <NTabs animated type="line">
              <NTabPane name="overview" tab="概览">
                <NDescriptions :column="2" bordered size="small">
                  <NDescriptionsItem label="菜单名称">
                    {{ currentDetail.menuName }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="菜单编码">
                    {{ currentDetail.menuCode }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="菜单类型">
                    {{ getOptionLabel(menuTypeOptions, currentDetail.menuType) }}
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
                  <NDescriptionsItem label="状态">
                    {{ formatStatus(currentDetail.status) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="权限ID">
                    {{ formatNullable(currentDetail.permissionId) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="创建时间">
                    {{ formatNullableDate(currentDetail.createdTime) }}
                  </NDescriptionsItem>
                </NDescriptions>
              </NTabPane>

              <NTabPane name="children" :tab="`子菜单 (${childMenus.length})`">
                <table v-if="childMenus.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>菜单名称</th>
                      <th>编码</th>
                      <th>类型</th>
                      <th>路径</th>
                      <th>状态</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in childMenus" :key="item.basicId">
                      <td>{{ item.menuName }}</td>
                      <td>{{ item.menuCode }}</td>
                      <td>{{ getOptionLabel(menuTypeOptions, item.menuType) }}</td>
                      <td>{{ formatNullable(item.path) }}</td>
                      <td>{{ formatStatus(item.status) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无子菜单" style="padding: 40px 0" />
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
</style>
