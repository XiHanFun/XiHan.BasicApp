<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysPermission } from '@/api'
import {
  NButton,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NPopconfirm,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { permissionApi } from '@/api'
import { XSystemQueryPanel } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemPermissionPage' })

/** 标签列颜色池：同名字符串哈希后稳定取色 */
const GROUP_TAG_TYPES = ['info', 'success', 'warning', 'error', 'primary'] as const

function groupNameTagType(name: string | undefined) {
  if (!name?.trim())
    return 'default' as const
  let h = 0
  for (let i = 0; i < name.length; i++)
    h = (h * 31 + name.charCodeAt(i)) >>> 0
  return GROUP_TAG_TYPES[h % GROUP_TAG_TYPES.length]
}

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
  status: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return permissionApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    status: queryParams.status,
  })
}

const options = useVxeTable<SysPermission>(
  {
    id: 'sys_permission',
    name: '权限管理',
    columns: [
      { type: 'checkbox', width: 42, fixed: 'left' },
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      {
        field: 'permissionName',
        title: '权限名称',
        minWidth: 160,
        showOverflow: 'tooltip',
        sortable: true,
      },
      { field: 'permissionCode', title: '权限编码', minWidth: 180, showOverflow: 'tooltip' },
      {
        field: 'primaryTag',
        title: '主标签',
        minWidth: 120,
        slots: { default: 'col_primaryTag' },
      },
      { field: 'tags', title: '标签', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'permissionDescription', title: '描述', minWidth: 200, showOverflow: 'tooltip' },
      { field: 'priority', title: '优先级', width: 80 },
      {
        field: 'isRequireAudit',
        title: '需要审计',
        width: 90,
        slots: { default: 'col_audit' },
      },
      {
        field: 'isGlobal',
        title: '全局',
        width: 80,
        slots: { default: 'col_global' },
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
    checkboxConfig: { range: true, reserve: true },
    rowConfig: { keyField: 'basicId' },
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

const modalVisible = ref(false)
const modalTitle = ref('新增权限')
const submitLoading = ref(false)
const formData = ref<Partial<SysPermission>>({})

function resetForm() {
  formData.value = {
    resourceId: '',
    operationId: '',
    permissionName: '',
    permissionCode: '',
    permissionDescription: '',
    tags: '',
    isGlobal: false,
    isRequireAudit: false,
    priority: 0,
    remark: '',
    sort: 0,
    status: 1,
  }
}

function handleAdd() {
  modalTitle.value = '新增权限'
  resetForm()
  modalVisible.value = true
}

function handleEdit(row: SysPermission) {
  modalTitle.value = '编辑权限'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await permissionApi.delete(id)
    message.success('删除成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    if (!formData.value.resourceId || !formData.value.operationId) {
      message.warning('请输入资源 ID 和操作 ID')
      return
    }
    submitLoading.value = true
    if (formData.value.basicId) {
      await permissionApi.update(formData.value.basicId, formData.value)
    }
    else {
      await permissionApi.create(formData.value)
    }
    message.success('操作成功')
    modalVisible.value = false
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('操作失败')
  }
  finally {
    submitLoading.value = false
  }
}

/** 当前勾选行（含跨页保留） */
function getSelectedRows(): SysPermission[] {
  const grid = xGrid.value
  if (!grid)
    return []
  const cur = grid.getCheckboxRecords() as SysPermission[]
  const reserved = grid.getCheckboxReserveRecords() as SysPermission[]
  const map = new Map<string, SysPermission>()
  for (const r of reserved)
    map.set(r.basicId, r)
  for (const r of cur)
    map.set(r.basicId, r)
  return [...map.values()]
}

/** 逐条调用删除接口（无专用批量接口时） */
async function handleBatchDelete() {
  const rows = getSelectedRows()
  if (!rows.length) {
    message.warning('请先勾选要删除的权限')
    return
  }
  try {
    for (const row of rows)
      await permissionApi.delete(row.basicId)
    message.success(`已删除 ${rows.length} 条`)
    xGrid.value?.clearCheckboxRow()
    xGrid.value?.clearCheckboxReserve()
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('批量删除失败')
  }
}

/** 批量改状态：保留行内其它字段再提交更新 */
async function handleBatchSetStatus(status: number) {
  const rows = getSelectedRows()
  if (!rows.length) {
    message.warning('请先勾选权限')
    return
  }
  try {
    for (const row of rows)
      await permissionApi.update(row.basicId, { ...row, status })
    message.success(status === 1 ? '批量启用成功' : '批量禁用成功')
    xGrid.value?.clearCheckboxRow()
    xGrid.value?.clearCheckboxReserve()
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('批量更新状态失败')
  }
}
</script>

<template>
  <div class="flex flex-col h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          placeholder="搜索权限名称/编码"
          clearable
          style="width: 260px"
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
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #toolbar_buttons>
          <NSpace align="center" size="small" wrap>
            <NButton v-access="['permission:create']" type="primary" size="small" @click="handleAdd">
              新增权限
            </NButton>
            <NButton v-access="['permission:update']" size="small" type="success" @click="handleBatchSetStatus(1)">
              批量启用
            </NButton>
            <NButton v-access="['permission:update']" size="small" type="warning" @click="handleBatchSetStatus(0)">
              批量禁用
            </NButton>
            <NPopconfirm v-access="['permission:delete']" @positive-click="handleBatchDelete">
              <template #trigger>
                <NButton size="small" type="error">
                  批量删除
                </NButton>
              </template>
              确认删除已勾选的全部权限？
            </NPopconfirm>
          </NSpace>
        </template>
        <template #col_primaryTag="{ row }">
          <NTag v-if="row.primaryTag" :type="groupNameTagType(row.primaryTag)" size="small" round>
            {{ row.primaryTag }}
          </NTag>
          <span v-else class="text-gray-400">—</span>
        </template>
        <template #col_audit="{ row }">
          <NTag :type="row.isRequireAudit ? 'error' : 'success'" size="small" round>
            {{ row.isRequireAudit ? '是' : '否' }}
          </NTag>
        </template>
        <template #col_global="{ row }">
          <NTag :type="row.isGlobal ? 'warning' : 'default'" size="small" round>
            {{ row.isGlobal ? '是' : '否' }}
          </NTag>
        </template>
        <template #col_status="{ row }">
          <NTag :type="row.status === 1 ? 'success' : 'error'" size="small" round>
            {{ row.status === 1 ? '启用' : '禁用' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton v-access="['permission:update']" size="small" type="primary" text @click="handleEdit(row)">
              编辑
            </NButton>
            <NPopconfirm v-access="['permission:delete']" @positive-click="handleDelete(row.basicId)">
              <template #trigger>
                <NButton size="small" type="error" text>
                  删除
                </NButton>
              </template>
              确认删除该权限？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="modalVisible"
      :title="modalTitle"
      preset="card"
      style="width: 600px"
      :auto-focus="false"
    >
      <NForm class="xh-edit-form-grid" :model="formData" label-placement="top" label-width="90px">
        <NFormItem label="资源 ID" path="resourceId">
          <NInput v-model:value="formData.resourceId" placeholder="请输入资源 ID" />
        </NFormItem>
        <NFormItem label="操作 ID" path="operationId">
          <NInput v-model:value="formData.operationId" placeholder="请输入操作 ID" />
        </NFormItem>
        <NFormItem label="权限名称" path="permissionName">
          <NInput v-model:value="formData.permissionName" placeholder="请输入权限名称" />
        </NFormItem>
        <NFormItem label="权限编码" path="permissionCode">
          <NInput v-model:value="formData.permissionCode" placeholder="如: system:user:list" />
        </NFormItem>
        <NFormItem label="标签" path="tags">
          <NInput v-model:value="formData.tags" placeholder="多个标签使用逗号分隔" />
        </NFormItem>
        <NFormItem label="描述" path="permissionDescription">
          <NInput
            v-model:value="formData.permissionDescription"
            type="textarea"
            :rows="2"
            placeholder="权限描述"
          />
        </NFormItem>
        <NFormItem label="需要审计" path="isRequireAudit">
          <NSwitch v-model:value="formData.isRequireAudit" />
        </NFormItem>
        <NFormItem label="全局权限" path="isGlobal">
          <NSwitch v-model:value="formData.isGlobal" />
        </NFormItem>
        <NFormItem label="优先级" path="priority">
          <NInputNumber v-model:value="formData.priority" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="formData.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput
            v-model:value="formData.remark"
            type="textarea"
            :rows="2"
            placeholder="备注说明"
          />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton
            v-if="!formData.basicId"
            v-access="['permission:create']"
            type="primary"
            :loading="submitLoading"
            @click="handleSubmit"
          >
            确认
          </NButton>
          <NButton
            v-else
            v-access="['permission:update']"
            type="primary"
            :loading="submitLoading"
            @click="handleSubmit"
          >
            确认
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
