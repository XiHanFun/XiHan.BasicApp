<script lang="ts" setup>
import type { DataTableColumns } from 'naive-ui'
import type { SysTask } from '~/types'
import { Icon } from '@iconify/vue'
import {
  NButton,
  NCard,
  NDataTable,
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
  NTag,
  useMessage,
} from 'naive-ui'
import { h, onMounted, reactive, ref } from 'vue'
import { createTaskApi, deleteTaskApi, getTaskPageApi, updateTaskApi } from '~/api'
import {
  DEFAULT_PAGE_SIZE,
  RUN_TASK_STATUS_OPTIONS,
  STATUS_OPTIONS,
  TRIGGER_TYPE_OPTIONS,
} from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemTaskPage' })

const message = useMessage()
const loading = ref(false)
const tableData = ref<SysTask[]>([])
const total = ref(0)

const queryParams = reactive({
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  keyword: '',
  triggerType: undefined as number | undefined,
  runTaskStatus: undefined as number | undefined,
  status: undefined as number | undefined,
})

const modalVisible = ref(false)
const modalTitle = ref('新增任务')
const submitLoading = ref(false)

const formData = ref<Partial<SysTask>>({
  taskCode: '',
  taskName: '',
  taskDescription: '',
  taskGroup: '',
  taskClass: '',
  taskMethod: '',
  taskParams: '',
  triggerType: 0,
  cronExpression: '',
  runTaskStatus: 0,
  priority: 3,
  status: 1,
  remark: '',
})

function getRunTaskStatusType(
  status: number,
): 'default' | 'info' | 'success' | 'warning' | 'error' {
  if (status === 0) return 'warning'
  if (status === 1) return 'info'
  if (status === 2) return 'success'
  if (status === 3) return 'error'
  return 'default'
}

async function fetchData() {
  try {
    loading.value = true
    const result = await getTaskPageApi(queryParams)
    tableData.value = result.items
    total.value = result.total
  } catch {
    message.error('获取任务列表失败')
  } finally {
    loading.value = false
  }
}

function handleAdd() {
  modalTitle.value = '新增任务'
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
    runTaskStatus: 0,
    priority: 3,
    status: 1,
    remark: '',
  }
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
    fetchData()
  } catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.basicId) {
      await updateTaskApi(formData.value.basicId, formData.value)
    } else {
      await createTaskApi(formData.value)
    }
    message.success('操作成功')
    modalVisible.value = false
    fetchData()
  } catch {
    message.error('操作失败')
  } finally {
    submitLoading.value = false
  }
}

const columns: DataTableColumns<SysTask> = [
  {
    title: '任务编码',
    key: 'taskCode',
    width: 150,
    render: (row) =>
      h(NTag, { type: 'info', size: 'small', bordered: false }, { default: () => row.taskCode }),
  },
  {
    title: '任务名称',
    key: 'taskName',
    width: 180,
  },
  {
    title: '任务类',
    key: 'taskClass',
    width: 220,
    ellipsis: { tooltip: true },
  },
  {
    title: '触发方式',
    key: 'triggerType',
    width: 110,
    render: (row) => getOptionLabel(TRIGGER_TYPE_OPTIONS, row.triggerType),
  },
  {
    title: '运行状态',
    key: 'runTaskStatus',
    width: 110,
    render: (row) =>
      h(
        NTag,
        { type: getRunTaskStatusType(row.runTaskStatus), size: 'small', round: true },
        { default: () => getOptionLabel(RUN_TASK_STATUS_OPTIONS, row.runTaskStatus) },
      ),
  },
  {
    title: '启用状态',
    key: 'status',
    width: 100,
    render: (row) =>
      h(
        NTag,
        { type: row.status === 1 ? 'success' : 'error', size: 'small', round: true },
        { default: () => (row.status === 1 ? '启用' : '禁用') },
      ),
  },
  {
    title: '创建时间',
    key: 'createTime',
    width: 170,
    render: (row) => formatDate(row.createTime ?? ''),
  },
  {
    title: '操作',
    key: 'actions',
    width: 160,
    fixed: 'right',
    render: (row) =>
      h(
        NSpace,
        { size: 'small' },
        {
          default: () => [
            h(
              NButton,
              {
                size: 'small',
                type: 'primary',
                ghost: true,
                onClick: () => handleEdit(row),
              },
              { default: () => '编辑' },
            ),
            h(
              NPopconfirm,
              {
                onPositiveClick: () => handleDelete(row.basicId),
              },
              {
                default: () => '确认删除该任务？',
                trigger: () =>
                  h(
                    NButton,
                    { size: 'small', type: 'error', ghost: true },
                    { default: () => '删除' },
                  ),
              },
            ),
          ],
        },
      ),
  },
]

onMounted(fetchData)
</script>

<template>
  <div class="space-y-4">
    <NCard :bordered="false">
      <div class="flex flex-wrap items-center gap-3">
        <NInput
          v-model:value="queryParams.keyword"
          placeholder="搜索任务编码/任务名称"
          style="width: 220px"
          clearable
          @keydown.enter="fetchData"
        />
        <NSelect
          v-model:value="queryParams.triggerType"
          :options="TRIGGER_TYPE_OPTIONS"
          placeholder="触发类型"
          style="width: 130px"
          clearable
        />
        <NSelect
          v-model:value="queryParams.runTaskStatus"
          :options="RUN_TASK_STATUS_OPTIONS"
          placeholder="运行状态"
          style="width: 130px"
          clearable
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          placeholder="启用状态"
          style="width: 120px"
          clearable
        />
        <NButton type="primary" @click="fetchData">
          <template #icon>
            <NIcon><Icon icon="lucide:search" width="14" /></NIcon>
          </template>
          搜索
        </NButton>
        <NButton
          @click="
            () => {
              queryParams.keyword = ''
              queryParams.triggerType = undefined
              queryParams.runTaskStatus = undefined
              queryParams.status = undefined
              queryParams.page = 1
              fetchData()
            }
          "
        >
          重置
        </NButton>
        <NButton class="ml-auto" type="primary" @click="handleAdd">
          <template #icon>
            <NIcon><Icon icon="lucide:plus" width="14" /></NIcon>
          </template>
          新增任务
        </NButton>
      </div>
    </NCard>

    <NCard :bordered="false">
      <NDataTable
        :columns="columns"
        :data="tableData"
        :loading="loading"
        :row-key="(row) => row.basicId"
        :pagination="false"
        :scroll-x="1250"
        size="small"
        striped
      />
      <div class="mt-4 flex justify-end">
        <NPagination
          v-model:page="queryParams.page"
          v-model:page-size="queryParams.pageSize"
          :item-count="total"
          :page-sizes="[10, 20, 50, 100]"
          show-size-picker
          @update:page="fetchData"
          @update:page-size="
            () => {
              queryParams.page = 1
              fetchData()
            }
          "
        />
      </div>
    </NCard>

    <NModal
      v-model:show="modalVisible"
      :title="modalTitle"
      preset="card"
      style="width: 640px"
      :auto-focus="false"
    >
      <NForm :model="formData" label-placement="left" label-width="90px">
        <NFormItem label="任务编码" path="taskCode">
          <NInput
            v-model:value="formData.taskCode"
            :disabled="!!formData.basicId"
            placeholder="请输入任务编码"
          />
        </NFormItem>
        <NFormItem label="任务名称" path="taskName">
          <NInput v-model:value="formData.taskName" placeholder="请输入任务名称" />
        </NFormItem>
        <NFormItem label="任务类名" path="taskClass">
          <NInput v-model:value="formData.taskClass" placeholder="请输入任务类名" />
        </NFormItem>
        <NFormItem label="执行方法" path="taskMethod">
          <NInput v-model:value="formData.taskMethod" placeholder="可选，默认执行入口方法" />
        </NFormItem>
        <NFormItem label="任务描述" path="taskDescription">
          <NInput
            v-model:value="formData.taskDescription"
            type="textarea"
            :rows="2"
            placeholder="可选"
          />
        </NFormItem>
        <NFormItem label="触发方式" path="triggerType">
          <NSelect v-model:value="formData.triggerType" :options="TRIGGER_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem v-if="formData.triggerType === 3" label="Cron 表达式" path="cronExpression">
          <NInput v-model:value="formData.cronExpression" placeholder="如: 0 */5 * * * ?" />
        </NFormItem>
        <NFormItem label="优先级" path="priority">
          <NInputNumber v-model:value="formData.priority" :min="1" :max="5" class="w-full" />
        </NFormItem>
        <NFormItem label="运行状态" path="runTaskStatus">
          <NSelect v-model:value="formData.runTaskStatus" :options="RUN_TASK_STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="启用状态" path="status">
          <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">取消</NButton>
          <NButton type="primary" :loading="submitLoading" @click="handleSubmit">确认</NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
