<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { ApiId, DepartmentCreateDto, DepartmentListItemDto, DepartmentTreeNodeDto, DepartmentUpdateDto } from '@/api'
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
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, onMounted, reactive, ref } from 'vue'
import {
  createPageRequest,
  departmentApi,
  DepartmentType,
  EnableStatus,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemDepartmentPage' })

interface DeptGridResult {
  items: DepartmentListItemDto[]
  total: number
}

interface DeptFormModel extends DepartmentCreateDto {
  basicId?: ApiId
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<DepartmentListItemDto>>()
const modalVisible = ref(false)
const submitLoading = ref(false)
const deptForm = ref<DeptFormModel>(createDefaultForm())
const treeNodes = ref<DepartmentTreeNodeDto[]>([])

const queryParams = reactive({
  departmentType: undefined as DepartmentType | undefined,
  keyword: '',
  status: undefined as EnableStatus | undefined,
})

const statusOptions = [
  { label: '启用', value: EnableStatus.Enabled },
  { label: '禁用', value: EnableStatus.Disabled },
]

const deptTypeOptions = [
  { label: '集团', value: DepartmentType.Corporation },
  { label: '总部', value: DepartmentType.Headquarters },
  { label: '公司', value: DepartmentType.Company },
  { label: '分公司', value: DepartmentType.Branch },
  { label: '事业部', value: DepartmentType.Division },
  { label: '中心', value: DepartmentType.Center },
  { label: '部门', value: DepartmentType.Department },
  { label: '科室', value: DepartmentType.Section },
  { label: '小组', value: DepartmentType.Team },
  { label: '组', value: DepartmentType.Group },
  { label: '项目组', value: DepartmentType.Project },
  { label: '工作组', value: DepartmentType.Workgroup },
  { label: '虚拟', value: DepartmentType.Virtual },
  { label: '办公室', value: DepartmentType.Office },
  { label: '子公司', value: DepartmentType.Subsidiary },
  { label: '其他', value: DepartmentType.Other },
]

const modalTitle = computed(() => (deptForm.value.basicId ? '编辑部门' : '新增部门'))

/** 将树节点转换为 NCascader 选项 */
function treeToCascaderOptions(nodes: DepartmentTreeNodeDto[]): any[] {
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

async function loadTree() {
  try {
    treeNodes.value = await departmentApi.tree({ keyword: null, limit: 2000, onlyEnabled: false })
  }
  catch {
    treeNodes.value = []
  }
}

onMounted(loadTree)

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<DeptGridResult> {
  return departmentApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      departmentType: queryParams.departmentType,
      keyword: normalizeNullable(queryParams.keyword),
      status: queryParams.status,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询部门失败')
      return { items: [], total: 0 }
    })
}

const tableOptions = useVxeTable<DepartmentListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'departmentName', minWidth: 160, showOverflow: 'tooltip', sortable: true, title: '部门名称' },
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
        width: 100,
      },
    ],
    id: 'sys_department',
    name: '部门管理',
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
  queryParams.departmentType = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

function handleAdd() {
  deptForm.value = createDefaultForm()
  modalVisible.value = true
}

function handleEdit(row: DepartmentListItemDto) {
  deptForm.value = {
    ...createDefaultForm(),
    basicId: row.basicId,
    departmentCode: row.departmentCode,
    departmentName: row.departmentName,
    departmentType: row.departmentType,
    email: row.email ?? null,
    leaderId: row.leaderId ?? null,
    parentId: row.parentId ?? null,
    phone: row.phone ?? null,
    sort: row.sort,
    status: row.status,
  }
  modalVisible.value = true
}

async function handleToggleStatus(row: DepartmentListItemDto) {
  const nextStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await departmentApi.updateStatus({ basicId: row.basicId, status: nextStatus })
    message.success('状态更新成功')
    xGrid.value?.commitProxy('query')
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
  if (!validateForm()) return

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
      await departmentApi.update(updateInput)
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
      await departmentApi.create(createInput)
    }

    message.success('保存成功')
    modalVisible.value = false
    xGrid.value?.commitProxy('query')
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
      <vxe-grid ref="xGrid" v-bind="tableOptions">
        <template #toolbar_buttons>
          <NButton size="small" type="primary" @click="handleAdd">
            <template #icon>
              <NIcon><Icon icon="lucide:plus" /></NIcon>
            </template>
            新增部门
          </NButton>
        </template>

        <template #col_status="{ row }">
          <NTag :type="row.status === EnableStatus.Enabled ? 'success' : 'error'" round size="small">
            {{ getOptionLabel(statusOptions, row.status) }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NButton aria-label="编辑" circle quaternary size="small" type="primary" @click="handleEdit(row)">
            <template #icon>
              <NIcon><Icon icon="lucide:pencil" /></NIcon>
            </template>
          </NButton>
          <NButton
            :type="row.status === EnableStatus.Enabled ? 'warning' : 'success'"
            aria-label="切换状态"
            circle
            quaternary
            size="small"
            @click="handleToggleStatus(row)"
          >
            <template #icon>
              <NIcon>
                <Icon :icon="row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:check'" />
              </NIcon>
            </template>
          </NButton>
        </template>
      </vxe-grid>
    </vxe-card>

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
