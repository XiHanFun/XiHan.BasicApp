<script setup lang="ts">
import type { CascaderOption } from 'naive-ui'
import type { VxeGridInstance } from 'vxe-table'
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
  NCascader,
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
  NPopconfirm,
  NScrollbar,
  NSelect,
  NSpace,
  NSpin,
  NTabPane,
  NTabs,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, onMounted, reactive, ref } from 'vue'
import {
  createDefaultQueryBehavior,
  createPageRequest,
  DepartmentType,
  EnableStatus,
  orgManagementApi,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { DEPARTMENT_TYPE_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemOrgPage' })

interface DeptFormModel extends DepartmentCreateDto {
  basicId?: ApiId
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<DepartmentListItemDto>>()
const loading = ref(false)
const modalVisible = ref(false)
const submitLoading = ref(false)
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<DepartmentDetailDto | null>(null)
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

/** 将树节点转换为 NCascader 选项 */
function treeToCascaderOptions(nodes: DepartmentTreeNodeDto[]): CascaderOption[] {
  return nodes.map(node => ({
    children: node.children.length > 0 ? treeToCascaderOptions(node.children) : undefined,
    label: node.departmentName,
    value: node.basicId,
  }))
}

const cascaderOptions = computed(() => treeToCascaderOptions(treeNodes.value))

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
  }
  catch {
    message.error('查询部门失败')
    tableData.value = []
  }
  finally {
    loading.value = false
  }
}

onMounted(async () => {
  await Promise.all([loadTree(), loadTable()])
})

const tableOptions = useVxeTable<DepartmentListItemDto>(
  {
    data: [],
    columns: [
      { field: 'departmentName', minWidth: 200, showOverflow: 'tooltip', title: '部门名称', treeNode: true },
      { field: 'departmentCode', minWidth: 130, showOverflow: 'tooltip', title: '部门编码' },
      {
        field: 'departmentType',
        formatter: ({ cellValue }) => getOptionLabel(deptTypeOptions, cellValue),
        minWidth: 100,
        title: '类型',
      },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '状态',
        width: 80,
      },
      { field: 'phone', minWidth: 130, showOverflow: 'tooltip', title: '电话' },
      { field: 'email', minWidth: 180, showOverflow: 'tooltip', title: '邮箱' },
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
        width: 216,
      },
    ],
    id: 'sys_department',
    name: '部门管理',
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
  queryParams.departmentType = undefined
  queryParams.status = undefined
  void loadTable()
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
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
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
    </XSystemQueryPanel>

    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="tableOptions" :data="tableData" :loading="loading">
        <template #toolbar_buttons>
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
        </template>

        <template #col_status="{ row }">
          <NTag :type="row.status === EnableStatus.Enabled ? 'success' : 'error'" round size="small">
            {{ getOptionLabel(statusOptions, row.status) }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton aria-label="查看详情" circle quaternary size="small" @click="handleView(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:eye" /></NIcon>
              </template>
            </NButton>

            <NButton size="small" text type="info" @click="handleAdd(row.basicId)">
              新增子部门
            </NButton>
            <NButton size="small" text type="primary" @click="handleEdit(row)">
              编辑
            </NButton>
            <NPopconfirm @positive-click="handleToggleStatus(row)">
              <template #trigger>
                <NButton
                  :type="row.status === EnableStatus.Enabled ? 'warning' : 'success'"
                  size="small"
                  text
                >
                  {{ row.status === EnableStatus.Enabled ? '禁用' : '启用' }}
                </NButton>
              </template>
              确认{{ row.status === EnableStatus.Enabled ? '禁用' : '启用' }}？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NDrawer v-model:show="detailVisible" :width="820">
      <NDrawerContent closable title="部门详情">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="xh-detail-empty" description="暂无部门详情" />
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
                <NEmpty v-else description="暂无子部门" />
              </NTabPane>

              <NTabPane name="members" tab="部门成员">
                <NEmpty description="暂无部门成员" />
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
