<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysTask } from '~/types'
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
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { createTaskApi, deleteTaskApi, updateTaskApi } from '@/api'
import { buildPageRequest, flattenPageResponse } from '@/api/helpers'
import requestClient from '@/api/request'
import { RUN_TASK_STATUS_OPTIONS, STATUS_OPTIONS, TRIGGER_TYPE_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemTaskPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
  status: undefined as number | undefined,
  runTaskStatus: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return requestClient.post('/api/Task/Page', buildPageRequest({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    status: queryParams.status,
    runTaskStatus: queryParams.runTaskStatus,
  }, {
    keywordFields: ['TaskCode', 'TaskName', 'TaskGroup', 'TaskClass'],
    filterFieldMap: { status: 'Status', runTaskStatus: 'RunTaskStatus' },
  })).then(flattenPageResponse)
}

const options = useVxeTable<SysTask>({
  id: 'sys_task',
  name: '任务管理',
  columns: [
    { type: 'seq', title: '序号', width: 60, fixed: 'left' },
    { field: 'taskName', title: '任务名称', minWidth: 160, showOverflow: 'tooltip', sortable: true },
    { field: 'taskCode', title: '任务编码', minWidth: 140, showOverflow: 'tooltip' },
    { field: 'taskGroup', title: '分组', minWidth: 100, showOverflow: 'tooltip' },
    { field: 'taskClass', title: '任务类', minWidth: 200, showOverflow: 'tooltip' },
    {
      field: 'triggerType',
      title: '触发方式',
      width: 110,
      formatter: ({ cellValue }) => getOptionLabel(TRIGGER_TYPE_OPTIONS, cellValue),
    },
    { field: 'cronExpression', title: 'Cron 表达式', minWidth: 140, showOverflow: 'tooltip' },
    { field: 'lastRunTime', title: '最后执行', width: 170, formatter: ({ cellValue }) => formatDate(cellValue) },
    { field: 'nextRunTime', title: '下次执行', width: 170, formatter: ({ cellValue }) => formatDate(cellValue) },
    {
      field: 'runTaskStatus',
      title: '运行状态',
      width: 100,
      slots: { default: 'col_runStatus' },
    },
    { field: 'priority', title: '优先级', width: 80 },
    { field: 'executedCount', title: '已执行', width: 80 },
    {
      field: 'status',
      title: '状态',
      width: 80,
      slots: { default: 'col_status' },
    },
    { field: 'createTime', title: '创建时间', width: 170, formatter: ({ cellValue }) => formatDate(cellValue), sortable: true },
    {
      title: '操作',
      width: 140,
      fixed: 'right',
      slots: { default: 'col_actions' },
    },
  ],
}, {
  proxyConfig: {
    autoLoad: true,
    ajax: { query: ({ page }) => handleQueryApi(page) },
  },
})

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}
function handleReset() {
  queryParams.keyword = ''
  queryParams.status = undefined
  queryParams.runTaskStatus = undefined
  xGrid.value?.commitProxy('reload')
}

const modalVisible = ref(false)
const modalTitle = ref('新增任务')
const submitLoading = ref(false)
const formData = ref<Partial<SysTask>>({})

function resetForm() {
  formData.value = {
    taskCode: '',
    taskName: '',
    taskDescription: '',
    taskGroup: '',
    taskClass: '',
    taskMethod: '',
    taskParams: '',
    triggerType: 0,
    cronExpression: '',
    priority: 0,
    status: 1,
  }
}
function handleAdd() {
  modalTitle.value = '新增任务'
  resetForm()
  modalVisible.value = true
}
function handleEdit(row: SysTask) {
  modalTitle.value = '编辑任务'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await deleteTaskApi(id)
    message.success('删除成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.basicId)
      await updateTaskApi(formData.value.basicId, formData.value)
    else await createTaskApi(formData.value)
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

function getRunStatusType(status: number) {
  const map: Record<number, 'default' | 'info' | 'success' | 'warning' | 'error'> = { 0: 'default', 1: 'info', 2: 'success', 3: 'error', 4: 'warning', 5: 'warning' }
  return map[status] ?? 'default'
}
</script>

<template>
  <div class="h-full flex flex-col gap-2 overflow-hidden p-3">
    <vxe-card style="padding: 10px 16px">
      <div class="flex items-center gap-3 flex-wrap">
        <vxe-input v-model="queryParams.keyword" placeholder="搜索任务名称/编码/类名" clearable style="width: 280px" @keyup.enter="handleSearch" />
        <NSelect v-model:value="queryParams.runTaskStatus" :options="RUN_TASK_STATUS_OPTIONS" placeholder="运行状态" clearable style="width: 130px" />
        <NSelect v-model:value="queryParams.status" :options="STATUS_OPTIONS" placeholder="状态" clearable style="width: 120px" />
        <NButton type="primary" size="small" @click="handleSearch">
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          重置
        </NButton>
      </div>
    </vxe-card>
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #toolbar_buttons>
          <NButton type="primary" size="small" @click="handleAdd">
            新增任务
          </NButton>
        </template>
        <template #col_runStatus="{ row }">
          <NTag :type="getRunStatusType(row.runTaskStatus)" size="small">
            {{ getOptionLabel(RUN_TASK_STATUS_OPTIONS, row.runTaskStatus) }}
          </NTag>
        </template>
        <template #col_status="{ row }">
          <NTag :type="row.status === 1 ? 'success' : 'error'" size="small" round>
            {{ row.status === 1 ? '启用' : '禁用' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton size="small" type="primary" text @click="handleEdit(row)">
              编辑
            </NButton>
            <NPopconfirm @positive-click="handleDelete(row.basicId)">
              <template #trigger>
                <NButton size="small" type="error" text>
                  删除
                </NButton>
              </template>
              确认删除该任务？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal v-model:show="modalVisible" :title="modalTitle" preset="card" style="width: 620px" :auto-focus="false">
      <NForm :model="formData" label-placement="left" label-width="100px">
        <NFormItem label="任务名称" path="taskName">
          <NInput v-model:value="formData.taskName" placeholder="请输入任务名称" />
        </NFormItem>
        <NFormItem label="任务编码" path="taskCode">
          <NInput v-model:value="formData.taskCode" placeholder="如: clean_log_job" />
        </NFormItem>
        <NFormItem label="任务分组" path="taskGroup">
          <NInput v-model:value="formData.taskGroup" placeholder="任务分组" />
        </NFormItem>
        <NFormItem label="任务类" path="taskClass">
          <NInput v-model:value="formData.taskClass" placeholder="完整类名" />
        </NFormItem>
        <NFormItem label="触发方式" path="triggerType">
          <NSelect v-model:value="formData.triggerType" :options="TRIGGER_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem v-if="formData.triggerType === 3" label="Cron 表达式" path="cronExpression">
          <NInput v-model:value="formData.cronExpression" placeholder="如: 0 0 2 * * ?" />
        </NFormItem>
        <NFormItem label="优先级" path="priority">
          <NInputNumber v-model:value="formData.priority" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="描述" path="taskDescription">
          <NInput v-model:value="formData.taskDescription" type="textarea" :rows="2" placeholder="任务描述" />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton type="primary" :loading="submitLoading" @click="handleSubmit">
            确认
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
