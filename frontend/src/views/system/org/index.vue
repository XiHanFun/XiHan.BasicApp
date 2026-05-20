<script setup lang="ts">
import type { CascaderOption, DataTableColumns } from 'naive-ui'
import type {
  ApiId,
  DepartmentCreateDto,
  DepartmentDetailDto,
  DepartmentListItemDto,
  DepartmentManagementDetailDto,
  DepartmentManagementMemberDto,
  DepartmentTreeNodeDto,
  DepartmentUpdateDto,
} from '@/api'
import {
  NButton,
  NCard,
  NConfigProvider,
  NCascader,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NEmpty,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NPagination,
  NPopconfirm,
  NSelect,
  NSpace,
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
  ValidityStatus,
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
const managementDetail = ref<DepartmentManagementDetailDto | null>(null)
const deptForm = ref<DeptFormModel>(createDefaultForm())
const tableData = ref<DepartmentListItemDto[]>([])
const treeNodes = ref<DepartmentTreeNodeDto[]>([])
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
    children: node.children && node.children.length > 0 ? treeToCascaderOptions(node.children) : undefined,
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

const detDept = computed(() => managementDetail.value?.department ?? null)

const childDeptColumns: DataTableColumns<DepartmentListItemDto> = [
  { title: '部门名称', key: 'departmentName', minWidth: 120, ellipsis: { tooltip: true } },
  { title: '编码', key: 'departmentCode', width: 100, ellipsis: { tooltip: true } },
  {
    title: '类型',
    key: 'departmentType',
    width: 90,
    render: row => getOptionLabel(deptTypeOptions, row.departmentType),
  },
  {
    title: '状态',
    key: 'status',
    width: 72,
    render: row => h(NTag, { size: 'small', round: true, type: row.status === EnableStatus.Enabled ? 'success' : 'error', bordered: false }, () => formatStatus(row.status)),
  },
]

const memberColumns: DataTableColumns<DepartmentManagementMemberDto> = [
  {
    title: '用户',
    key: 'user',
    minWidth: 140,
    render: row => row.realName || row.nickName || row.userName || String(row.userId),
  },
  { title: '用户名', key: 'userName', width: 110, ellipsis: { tooltip: true }, render: row => row.userName ?? '—' },
  {
    title: '主部门',
    key: 'isMain',
    width: 72,
    render: row => row.isMain
      ? h(NTag, { size: 'small', type: 'info', bordered: false }, () => '是')
      : h('span', { style: 'color:var(--n-text-color-3)' }, '—'),
  },
  {
    title: '状态',
    key: 'status',
    width: 72,
    render: row => h(NTag, {
      size: 'small',
      round: true,
      type: row.status === ValidityStatus.Valid ? 'success' : 'default',
      bordered: false,
    }, () => (row.status === ValidityStatus.Valid ? '有效' : '无效')),
  },
]

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

const tableColumns = computed<DataTableColumns<DepartmentTreeItem>>(() => [
  {
    key: 'departmentName',
    title: '部门名称',
    minWidth: 200,
    tree: true,
    ellipsis: { tooltip: true },
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
    email: 'email' in row ? row.email ?? null : null,
    leaderId: row.leaderId ?? null,
    parentId: row.parentId ?? null,
    phone: 'phone' in row ? row.phone ?? null : null,
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
  managementDetail.value = null

  try {
    managementDetail.value = await orgManagementApi.detailView(row.basicId)
    if (!managementDetail.value) {
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
        status: deptForm.value.status,
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
    <div class="xh-query-panel mb-2">
      <NInput
        v-model:value="queryParams.keyword"
        clearable
        size="small"
        placeholder="搜索部门名称/编码"
        style="width: 220px"
        @keyup.enter="handleSearch"
      />
      <NSelect
        v-model:value="queryParams.departmentType"
        :options="deptTypeOptions"
        clearable
        size="small"
        placeholder="部门类型"
        style="width: 120px"
      />
      <NSelect
        v-model:value="queryParams.status"
        :options="statusOptions"
        clearable
        size="small"
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
        <NPagination size="small" :page="currentPage" :page-count="totalPages" :page-slot="7" :page-sizes="[10,20,50,100]" :page-size="pageSize" show-size-picker @update:page="handlePageChange" @update:page-size="handlePageSizeChange" />
      </div>
    </NCard>

    <NModal
      v-model:show="detailVisible"
      class="xh-mgmt-detail-modal"
      preset="card"
      :bordered="false"
      :mask-closable="true"
      style="width: 720px; max-width: calc(100vw - 32px);"
    >
      <template v-if="detDept" #header>
        <div class="det-hd-entity">
          <div class="det-hd-ico">
            <Icon icon="tabler:building" :size="22" />
          </div>
          <div class="min-w-0">
            <div class="det-hd-name">
              {{ detDept.departmentName }}
            </div>
            <div class="det-hd-sub">
              {{ detDept.departmentCode }}
            </div>
          </div>
        </div>
      </template>

      <div v-if="detailLoading" class="modal-loading">
        加载中…
      </div>
      <NTabs v-else-if="managementDetail" type="line" animated size="small">
        <NTabPane name="overview" tab="概览">
          <NDescriptions :column="2" bordered size="small">
            <NDescriptionsItem label="部门类型">
              {{ getOptionLabel(deptTypeOptions, detDept!.departmentType) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="父级部门">
              {{ detDept!.parentId ? findDepartmentName(detDept!.parentId) : '—' }}
            </NDescriptionsItem>
            <NDescriptionsItem label="负责人 ID">
              {{ formatNullable(detDept!.leaderId) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="状态">
              <NTag size="small" :type="detDept!.status === EnableStatus.Enabled ? 'success' : 'error'" :bordered="false">
                {{ formatStatus(detDept!.status) }}
              </NTag>
            </NDescriptionsItem>
            <NDescriptionsItem label="电话">
              {{ formatNullable(detDept!.phone) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="邮箱">
              {{ formatNullable(detDept!.email) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="地址" :span="2">
              {{ formatNullable(detDept!.address) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="排序">
              {{ detDept!.sort }}
            </NDescriptionsItem>
            <NDescriptionsItem label="创建时间">
              {{ formatNullableDate(detDept!.createdTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem v-if="detDept!.remark" label="备注" :span="2">
              {{ detDept!.remark }}
            </NDescriptionsItem>
          </NDescriptions>
        </NTabPane>
        <NTabPane name="children" :tab="`子部门 (${managementDetail.childDepartments.length})`">
          <div class="xh-detail-table-wrap">
            <NDataTable
              v-if="managementDetail.childDepartments.length"
              :columns="childDeptColumns"
              :data="managementDetail.childDepartments"
              :bordered="false"
              size="small"
              :row-key="(row: DepartmentListItemDto) => row.basicId"
            />
            <NEmpty v-else description="暂无子部门" style="padding: 32px 0" />
          </div>
        </NTabPane>
        <NTabPane name="members" :tab="`部门成员 (${managementDetail.members.length})`">
          <div class="xh-detail-table-wrap">
            <NDataTable
              v-if="managementDetail.members.length"
              :columns="memberColumns"
              :data="managementDetail.members"
              :bordered="false"
              size="small"
              :row-key="(row: DepartmentManagementMemberDto) => row.basicId"
            />
            <NEmpty v-else description="暂无部门成员" style="padding: 32px 0" />
          </div>
        </NTabPane>
      </NTabs>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="detailVisible = false">
            关闭
          </NButton>
          <NButton
            v-if="detDept"
            size="small"
            type="primary"
            @click="detailVisible = false; handleEdit(detDept as DepartmentListItemDto)"
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
      style="width: 720px; max-width: 92vw"
    >
      <NConfigProvider size="small" abstract>
        <NForm :model="deptForm" size="small" class="xh-edit-form-grid" label-placement="top">
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
