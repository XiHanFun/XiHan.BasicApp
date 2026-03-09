<script lang="ts" setup>
import type { DataTableColumns } from 'naive-ui'
import type { SysSms } from '~/types'
import { Icon } from '@iconify/vue'
import {
  NButton,
  NCard,
  NDataTable,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NModal,
  NPagination,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { h, onMounted, reactive, ref } from 'vue'
import { createSmsApi, deleteSmsApi, getPendingSmsApi, getSmsPageApi, updateSmsApi } from '~/api'
import { DEFAULT_PAGE_SIZE, SMS_STATUS_OPTIONS, SMS_TYPE_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemSmsPage' })

const message = useMessage()
const loading = ref(false)
const tableData = ref<SysSms[]>([])
const total = ref(0)

const queryParams = reactive({
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  keyword: '',
  smsType: undefined as number | undefined,
  smsStatus: undefined as number | undefined,
})

const modalVisible = ref(false)
const modalTitle = ref('新增短信')
const submitLoading = ref(false)

const formData = ref<Partial<SysSms>>({
  smsType: 1,
  toPhone: '',
  content: '',
  templateId: '',
  templateParams: '',
  provider: '',
  smsStatus: 0,
  remark: '',
})

function getSmsStatusType(status: number): 'default' | 'info' | 'success' | 'warning' | 'error' {
  if (status === 0)
    return 'warning'
  if (status === 1)
    return 'info'
  if (status === 2)
    return 'success'
  if (status === 3)
    return 'error'
  if (status === 4)
    return 'default'
  return 'default'
}

async function fetchData() {
  try {
    loading.value = true
    const result = await getSmsPageApi(queryParams)
    tableData.value = result.items
    total.value = result.total
  }
  catch {
    message.error('获取短信列表失败')
  }
  finally {
    loading.value = false
  }
}

async function handleLoadPending() {
  try {
    loading.value = true
    const pending = await getPendingSmsApi(200)
    tableData.value = pending
    total.value = pending.length
    queryParams.page = 1
  }
  catch {
    message.error('获取待发送短信失败')
  }
  finally {
    loading.value = false
  }
}

function handleAdd() {
  modalTitle.value = '新增短信'
  formData.value = {
    smsType: 1,
    toPhone: '',
    content: '',
    templateId: '',
    templateParams: '',
    provider: '',
    smsStatus: 0,
    remark: '',
  }
  modalVisible.value = true
}

function handleEdit(row: SysSms) {
  modalTitle.value = '编辑短信'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await deleteSmsApi(id)
    message.success('删除成功')
    fetchData()
  }
  catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.basicId) {
      await updateSmsApi(formData.value.basicId, formData.value)
    }
    else {
      await createSmsApi(formData.value)
    }
    message.success('操作成功')
    modalVisible.value = false
    fetchData()
  }
  catch {
    message.error('操作失败')
  }
  finally {
    submitLoading.value = false
  }
}

const columns: DataTableColumns<SysSms> = [
  {
    title: '接收手机号',
    key: 'toPhone',
    width: 170,
  },
  {
    title: '短信内容',
    key: 'content',
    minWidth: 260,
    ellipsis: { tooltip: true },
  },
  {
    title: '类型',
    key: 'smsType',
    width: 120,
    render: row => getOptionLabel(SMS_TYPE_OPTIONS, row.smsType),
  },
  {
    title: '状态',
    key: 'smsStatus',
    width: 110,
    render: row =>
      h(
        NTag,
        { type: getSmsStatusType(row.smsStatus), size: 'small', round: true },
        { default: () => getOptionLabel(SMS_STATUS_OPTIONS, row.smsStatus) },
      ),
  },
  {
    title: '发送时间',
    key: 'sendTime',
    width: 170,
    render: row => formatDate(row.sendTime ?? ''),
  },
  {
    title: '操作',
    key: 'actions',
    width: 160,
    fixed: 'right',
    render: row =>
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
                default: () => '确认删除该短信？',
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
          placeholder="搜索手机号/内容"
          style="width: 220px"
          clearable
          @keydown.enter="fetchData"
        />
        <NSelect
          v-model:value="queryParams.smsType"
          :options="SMS_TYPE_OPTIONS"
          placeholder="全部类型"
          style="width: 130px"
          clearable
        />
        <NSelect
          v-model:value="queryParams.smsStatus"
          :options="SMS_STATUS_OPTIONS"
          placeholder="全部状态"
          style="width: 130px"
          clearable
        />
        <NButton type="primary" @click="fetchData">
          <template #icon>
            <NIcon><Icon icon="lucide:search" width="14" /></NIcon>
          </template>
          搜索
        </NButton>
        <NButton @click="handleLoadPending">
          待发送
        </NButton>
        <NButton
          @click="
            () => {
              queryParams.keyword = ''
              queryParams.smsType = undefined
              queryParams.smsStatus = undefined
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
          新增短信
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
        :scroll-x="1020"
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
      style="width: 560px"
      :auto-focus="false"
    >
      <NForm :model="formData" label-placement="left" label-width="90px">
        <NFormItem label="短信类型" path="smsType">
          <NSelect v-model:value="formData.smsType" :options="SMS_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="接收手机号" path="toPhone">
          <NInput v-model:value="formData.toPhone" placeholder="请输入手机号，多个用逗号分隔" />
        </NFormItem>
        <NFormItem label="短信内容" path="content">
          <NInput
            v-model:value="formData.content"
            type="textarea"
            :rows="4"
            placeholder="请输入短信内容"
          />
        </NFormItem>
        <NFormItem label="模板ID" path="templateId">
          <NInput v-model:value="formData.templateId" placeholder="可选" />
        </NFormItem>
        <NFormItem label="模板参数" path="templateParams">
          <NInput v-model:value="formData.templateParams" placeholder="可选，JSON 字符串" />
        </NFormItem>
        <NFormItem label="服务商" path="provider">
          <NInput v-model:value="formData.provider" placeholder="可选，如 aliyun" />
        </NFormItem>
        <NFormItem label="短信状态" path="smsStatus">
          <NSelect v-model:value="formData.smsStatus" :options="SMS_STATUS_OPTIONS" />
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
