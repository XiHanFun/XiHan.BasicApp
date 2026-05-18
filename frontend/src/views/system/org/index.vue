<script setup lang="ts">
import type { CascaderOption, DataTableColumns } from 'naive-ui'
import type {
  ApiId,
  DepartmentCreateDto,
  DepartmentDetailDto,
  DepartmentListItemDto,
  DepartmentTreeNodeDto,
  DepartmentUpdateDto,
} from '@/api'
import {
  NButton,
  NCard,
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
  NPopconfirm,
  NScrollbar,
  NSelect,
  NSpace,
  NSpin,
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
  DepartmentType,
  EnableStatus,
  orgManagementApi,
} from '@/api'
import { Icon } from '~/components'
import { DEPARTMENT_TYPE_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemOrgPage' })

interface DeptFormModel extends DepartmentCreateDto {
  basicId?: ApiId
}

const message = useMessage()
const loading = ref(false)
const dataList = ref<DepartmentListItemDto[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(20)
const modalVisible = ref(false)
const submitLoading = ref(false)
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<DepartmentDetailDto | null>(null)
const deptForm = ref<DeptFormModel>(createDefaultForm())
const tableData = ref<DepartmentListItemDto[]>([])
const treeNodes = ref<DepartmentTreeNodeDto[]>([])
const expandedKeys = ref<Set<ApiId>>(new Set())

const queryParams = reactive({
  departmentType: undefined as DepartmentType | undefined,
  keyword: '',
  status: undefined as EnableStatus | undefined,
})

const statusOptions = STATUS_OPTIONS

const deptTypeOptions = DEPARTMENT_TYPE_OPTIONS

const modalTitle = computed(() => (deptForm.value.basicId ? '编辑部门' : '新增部门'))

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

function treeToCascaderOptions(nodes: DepartmentTreeNodeDto[]): CascaderOption[] {
  return nodes.map(node => ({
    children: node.children.length > 0 ? treeToCascaderOptions(node.children) : undefined,
    label: node.departmentName,
    value: node.basicId,
  }))
}

const cascaderOptions = computed(() => treeToCascaderOptions(treeNodes.value))

interface DepartmentTreeItem extends DepartmentListItemDto {
  children?: DepartmentTreeItem[]
}

function buildTree(items: DepartmentListItemDto[]): DepartmentTreeItem[] {
  const map = new Map<ApiId, DepartmentTreeItem>()
  const roots: DepartmentTreeItem[] = []

  for (const item of items) {
    map.set(item.basicId, { ...item, children: [] })
  }

  for (const item of items) {
    const node = map.get(item.basicId)!
    if (item.parentId && map.has(item.parentId)) {
      const parent = map.get(item.parentId)!
      parent.children!.push(node)
    }
    else {
      roots.push(node)
    }
  }

  return roots
}

const treeTableData = computed(() => buildTree(dataList.value))

function createDefaultForm(): DeptFormModel {
  return {
    address: null,
    departmentCode: '',
    departmentName: '',
    departmentType: DepartmentType.Department,
    email: null,
    leaderId: null,
    parentId: null,
    phone: null,
    remark: null,
    sort: 100,
    status: EnableStatus.Enabled,
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

function formatStatus(value?: EnableStatus | null) {
  return getOptionLabel(statusOptions, value)
}

function findDepartmentName(parentId: ApiId) {
  const dept = tableData.value.find(item => item.basicId === parentId)
  return dept?.departmentName ?? formatNullable(parentId)
}

const childDepartments = computed(() => {
  if (!currentDetail.value) return []
  return tableData.value.filter(item => item.parentId === currentDetail.value!.basicId)
})

async function loadTree() {
  try {
    treeNodes.value = await orgManagementApi.tree({ keyword: null, limit: 2000, onlyEnabled: false })
  }
  catch {
    treeNodes.value = []
  }
}

async function loadTable() {
  loading.value = true
  try {
    const result = await orgManagementApi.page({
      ...createPageRequest({
        behavior: createDefaultQueryBehavior({
          disablePaging: true,
        }),
        page: {
          pageIndex: 1,
          pageSize: 5000,
        },
      }),
      departmentType: queryParams.departmentType,
      keyword: normalizeNullable(queryParams.keyword),
      status: queryParams.status,
    })
    tableData.value = result.items
    dataList.value = result.items
    totalCount.value = result.items.length
  }
  catch {
    message.error('查询部门失败')
    tableData.value = []
    dataList.value = []
    totalCount.value = 0
  }
  finally {
    loading.value = false
  }
}

function toggleExpand(key: ApiId) {
  const set = new Set(expandedKeys.value)
  if (set.has(key)) {
    set.delete(key)
  }
  else {
    set.add(key)
  }
  expandedKeys.value = set
}

const tableColumns = computed<DataTableColumns<DepartmentTreeItem>>(() => [
  {
    key: 'departmentName',
    title: '部门名称',
    minWidth: 200,
    ellipsis: { tooltip: true },
    render(row) {
      const hasChildren = row.children && row.children.length > 0
      const isExpanded = expandedKeys.value.has(row.basicId)
      const indent = h('span', { style: 'display:inline-block;width:16px;' })
      const arrow = hasChildren
        ? h(NIcon, { size: 14, style: 'cursor:pointer;vertical-align:middle;margin-right:4px;', onClick: () => toggleExpand(row.basicId) }, () => h(Icon, { icon: isExpanded ? 'lucide:chevron-down' : 'lucide:chevron-right' }))
        : h('span', { style: 'display:inline-block;width:18px;' })
      return h('span', {}, [indent, arrow, row.departmentName])
    },
  },
  { key: 'departmentCode', title: '部门编码', minWidth: 130, ellipsis: { tooltip: true } },
  {
    key: 'departmentType',
    title: '类型',
    minWidth: 100,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(deptTypeOptions, row.departmentType))
    },
  },
  {
    key: 'status',
    title: '状态',
    width: 80,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: row.status === EnableStatus.Enabled ? 'success' : 'error', bordered: false }, () => getOptionLabel(statusOptions, row.status))
    },
  },
  { key: 'phone', title: '电话', minWidth: 130, ellipsis: { tooltip: true } },
  { key: 'email', title: '邮箱', minWidth: 180, ellipsis: { tooltip: true } },
  { key: 'sort', title: '排序', width: 80, sorter: true },
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
    render(row) {
      return h(NSpace, { size: 'small' }, () => [
        h(NTooltip, {}, {
          trigger: () =>
            h(NButton, { ariaLabel: '查看详情', circle: true, quaternary: true, size: 'small', onClick: () => handleView(row) }, {
              icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:eye' })),
            }),
          default: () => '查看详情',
        }),
        h(NButton, { size: 'small', text: true, type: 'info', onClick: () => handleAdd(row.basicId) }, () => '新增子部门'),
        h(NButton, { size: 'small', text: true, type: 'primary', onClick: () => handleEdit(row) }, () => '编辑'),
        h(NPopconfirm, { onPositiveClick: () => handleToggleStatus(row) }, {
          trigger: () =>
            h(NButton, { type: row.status === EnableStatus.Enabled ? 'warning' : 'success', size: 'small', text: true }, () => row.status === EnableStatus.Enabled ? '禁用' : '启用'),
          default: () => `确认${row.status === EnableStatus.Enabled ? '禁用' : '启用'}？`,
        }),
      ])
    },
  },
])

function handleSearch() {
  void loadTable()
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.departmentType = undefined
  queryParams.status = undefined
  void loadTable()
}

function handlePageChange(page: number) {
  currentPage.value = page
}

function handlePageSizeChange(size: number) {
  pageSize.value = size
  currentPage.value = 1
}

function handleAdd(parentId?: ApiId) {
  deptForm.value = createDefaultForm()
  deptForm.value.parentId = parentId ?? null
  modalVisible.value = true
}

function buildFormModel(row: DepartmentDetailDto | DepartmentListItemDto): DeptFormModel {
  return {
    ...createDefaultForm(),
    address: 'address' in row ? row.address ?? null : null,
    basicId: row.basicId,
    departmentCode: row.departmentCode,
    departmentName: row.departmentName,
    departmentType: row.departmentType,
    email: row.email ?? null,
    leaderId: row.leaderId ?? null,
    parentId: row.parentId ?? null,
    phone: row.phone ?? null,
    remark: 'remark' in row ? row.remark ?? null : null,
    sort: row.sort,
    status: row.status,
  }
}

async function handleEdit(row: DepartmentListItemDto) {
  try {
    const detail = await orgManagementApi.detail(row.basicId)
    deptForm.value = buildFormModel(detail ?? row)
  }
  catch {
    message.error('加载部门详情失败')
    deptForm.value = buildFormModel(row)
  }
  modalVisible.value = true
}

async function handleView(row: DepartmentListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null

  try {
    currentDetail.value = await orgManagementApi.detail(row.basicId)
    if (!currentDetail.value) {
      message.warning('未查询到部门详情')
    }
  }
  catch {
    message.error('加载部门详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

async function handleToggleStatus(row: DepartmentListItemDto) {
  const nextStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await orgManagementApi.updateStatus({ basicId: row.basicId, status: nextStatus })
    message.success('状态更新成功')
    await loadTable()
    await loadTree()
  }
  catch {
    message.error('状态更新失败')
  }
}

function validateForm() {
  if (!deptForm.value.departmentName.trim()) {
    message.warning('请输入部门名称')
    return false
  }
  if (!deptForm.value.basicId && !deptForm.value.departmentCode.trim()) {
    message.warning('请输入部门编码')
    return false
  }
  return true
}

async function handleSubmit() {
  if (!validateForm())
    return

  submitLoading.value = true
  try {
    if (deptForm.value.basicId) {
      const updateInput: DepartmentUpdateDto = {
        address: normalizeNullable(deptForm.value.address),
        basicId: deptForm.value.basicId,
        departmentName: deptForm.value.departmentName.trim(),
        departmentType: deptForm.value.departmentType,
        email: normalizeNullable(deptForm.value.email),
        leaderId: deptForm.value.leaderId,
        parentId: deptForm.value.parentId,
        phone: normalizeNullable(deptForm.value.phone),
        remark: normalizeNullable(deptForm.value.remark),
        sort: deptForm.value.sort,
      }
      await orgManagementApi.update(updateInput)
    }
    else {
      const createInput: DepartmentCreateDto = {
        address: normalizeNullable(deptForm.value.address),
        departmentCode: deptForm.value.departmentCode.trim(),
        departmentName: deptForm.value.departmentName.trim(),
        departmentType: deptForm.value.departmentType,
        email: normalizeNullable(deptForm.value.email),
        leaderId: deptForm.value.leaderId,
        parentId: deptForm.value.parentId,
        phone: normalizeNullable(deptForm.value.phone),
        remark: normalizeNullable(deptForm.value.remark),
        sort: deptForm.value.sort,
        status: deptForm.value.status,
      }
      await orgManagementApi.create(createInput)
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

onMounted(async () => {
  await Promise.all([loadTree(), loadTable()])
})
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <div class="xh-query-panel mb-2" style="padding:10px 16px;background:var(--n-card-color);border-radius:var(--n-border-radius);">
      <NInput
        v-model:value="queryParams.keyword"
        clearable
        placeholder="搜索部门名称/编码"
        style="width: 220px"
        @keyup.enter="handleSearch"
      />
      <NSelect
        v-model:value="queryParams.departmentType"
        :options="deptTypeOptions"
        clearable
        placeholder="部门类型"
        style="width: 120px"
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

    <NCard content-style="padding:0;display:flex;flex-direction:column;height:100%;" :bordered="false" class="flex-1" style="height:0;">
      <div style="padding:12px 16px;flex-shrink:0;">
        <NButton size="small" type="primary" @click="handleAdd()">
          <template #icon>
            <NIcon><Icon icon="lucide:plus" /></NIcon>
          </template>
          新增部门
        </NButton>
        <NButton size="small" @click="loadTable">
          <template #icon>
            <NIcon><Icon icon="lucide:refresh-cw" /></NIcon>
          </template>
          刷新
        </NButton>
      </div>

      <NDataTable
        :columns="tableColumns"
        :data="treeTableData"
        :loading="loading"
        :bordered="false"
        :single-line="false"
        :row-key="(row: DepartmentListItemDto) => row.basicId"
        :scroll-x="2000"
        size="small"
        striped
        flex-height
        style="flex:1;"
      />

      <div style="display:flex;align-items:center;justify-content:space-between;padding:14px 20px;border-top:1px solid var(--n-border-color);flex-shrink:0;">
        <div style="font-size:13px;color:var(--n-text-color-3);">共 <strong>{{ totalCount }}</strong> 条，第 <strong>{{ currentPage }}</strong> / {{ totalPages }} 页</div>
        <NPagination :page="currentPage" :page-count="totalPages" :page-slot="7" :page-sizes="[10,20,50,100]" :page-size="pageSize" show-size-picker @update:page="handlePageChange" @update:page-size="handlePageSizeChange" />
      </div>
    </NCard>

    <NDrawer v-model:show="detailVisible" :width="820">
      <NDrawerContent closable title="部门详情">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="xh-detail-empty" description="暂无部门详情">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="currentDetail" style="max-height: calc(100vh - 120px)">
            <NTabs animated type="line">
              <NTabPane name="overview" tab="概览">
                <NDescriptions :column="2" bordered size="small">
                  <NDescriptionsItem label="部门名称">
                    {{ currentDetail.departmentName }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="部门编码">
                    {{ currentDetail.departmentCode }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="部门类型">
                    {{ getOptionLabel(deptTypeOptions, currentDetail.departmentType) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="父级部门">
                    {{ currentDetail.parentId ? findDepartmentName(currentDetail.parentId) : '-' }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="负责人">
                    {{ formatNullable(currentDetail.leaderId) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="电话">
                    {{ formatNullable(currentDetail.phone) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="邮箱">
                    {{ formatNullable(currentDetail.email) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="地址">
                    {{ formatNullable(currentDetail.address) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="排序">
                    {{ currentDetail.sort }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="状态">
                    {{ formatStatus(currentDetail.status) }}
                  </NDescriptionsItem>
                  <NDescriptionsItem label="创建时间">
                    {{ formatNullableDate(currentDetail.createdTime) }}
                  </NDescriptionsItem>
                </NDescriptions>
              </NTabPane>

              <NTabPane name="children" :tab="`子部门 (${childDepartments.length})`">
                <table v-if="childDepartments.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>部门名称</th>
                      <th>编码</th>
                      <th>类型</th>
                      <th>电话</th>
                      <th>状态</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in childDepartments" :key="item.basicId">
                      <td>{{ item.departmentName }}</td>
                      <td>{{ item.departmentCode }}</td>
                      <td>{{ getOptionLabel(deptTypeOptions, item.departmentType) }}</td>
                      <td>{{ formatNullable(item.phone) }}</td>
                      <td>{{ formatStatus(item.status) }}</td>
                    </tr>
                  </tbody>
                </table>
                <NEmpty v-else description="暂无子部门" style="padding: 40px 0" />
              </NTabPane>

              <NTabPane name="members" tab="部门成员">
                <NEmpty description="暂无部门成员" style="padding: 40px 0" />
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
      style="width: 720px; max-width: 92vw"
    >
      <NForm :model="deptForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="部门名称" path="departmentName">
          <NInput v-model:value="deptForm.departmentName" clearable placeholder="请输入部门名称" />
        </NFormItem>
        <NFormItem label="部门编码" path="departmentCode">
          <NInput
            v-model:value="deptForm.departmentCode"
            :disabled="Boolean(deptForm.basicId)"
            clearable
            placeholder="如: tech_dept"
          />
        </NFormItem>
        <NFormItem label="上级部门" path="parentId">
          <NCascader
            v-model:value="deptForm.parentId"
            :options="cascaderOptions"
            check-strategy="child"
            clearable
            placeholder="选择上级部门（可留空）"
            style="width: 100%"
          />
        </NFormItem>
        <NFormItem label="部门类型" path="departmentType">
          <NSelect v-model:value="deptForm.departmentType" :options="deptTypeOptions" />
        </NFormItem>
        <NFormItem label="电话" path="phone">
          <NInput v-model:value="deptForm.phone" clearable placeholder="请输入电话" />
        </NFormItem>
        <NFormItem label="邮箱" path="email">
          <NInput v-model:value="deptForm.email" clearable placeholder="请输入邮箱" />
        </NFormItem>
        <NFormItem label="地址" path="address">
          <NInput v-model:value="deptForm.address" clearable placeholder="请输入地址" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="deptForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="deptForm.remark" clearable placeholder="请输入备注" :rows="3" type="textarea" />
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
