<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysSms } from '@/api'
import {
  NButton,
  NForm,
  NFormItem,
  NInput,
  NModal,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { smsApi } from '@/api'
import { XSystemQueryPanel } from '~/components'
import { SMS_STATUS_OPTIONS, SMS_TYPE_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemSmsPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
  smsType: undefined as number | undefined,
  smsStatus: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return smsApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    smsType: queryParams.smsType,
    smsStatus: queryParams.smsStatus,
  })
}

const options = useVxeTable<SysSms>(
  {
    id: 'sys_sms',
    name: '短信管理',
    columns: [
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      { field: 'toPhone', title: '接收号码', minWidth: 140, showOverflow: 'tooltip' },
      { field: 'content', title: '内容', minWidth: 260, showOverflow: 'tooltip' },
      {
        field: 'smsType',
        title: '类型',
        width: 100,
        formatter: ({ cellValue }) => getOptionLabel(SMS_TYPE_OPTIONS, cellValue),
      },
      { field: 'provider', title: '服务商', minWidth: 100, showOverflow: 'tooltip' },
      { field: 'templateId', title: '模板ID', minWidth: 120, showOverflow: 'tooltip' },
      {
        field: 'smsStatus',
        title: '发送状态',
        width: 100,
        slots: { default: 'col_smsStatus' },
      },
      {
        field: 'sendTime',
        title: '发送时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
        sortable: true,
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
      ajax: { query: ({ page }) => handleQueryApi(page) },
    },
  },
)

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}
function handleReset() {
  queryParams.keyword = ''
  queryParams.smsType = undefined
  queryParams.smsStatus = undefined
  xGrid.value?.commitProxy('reload')
}

const modalVisible = ref(false)
const modalTitle = ref('新增短信')
const submitLoading = ref(false)
const formData = ref<Partial<SysSms>>({})

function resetForm() {
  formData.value = { toPhone: '', content: '', smsType: 0, templateId: '', provider: '' }
}
function handleAdd() {
  modalTitle.value = '新增短信'
  resetForm()
  modalVisible.value = true
}
function handleEdit(row: SysSms) {
  modalTitle.value = '编辑短信'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await smsApi.delete(id)
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
      await smsApi.update(formData.value.basicId, formData.value)
    else await smsApi.create(formData.value)
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

function getSmsStatusType(status: number) {
  const map: Record<number, 'default' | 'info' | 'success' | 'warning' | 'error'> = {
    0: 'default',
    1: 'info',
    2: 'success',
    3: 'error',
    4: 'warning',
  }
  return map[status] ?? 'default'
}
</script>

<template>
  <div class="flex flex-col h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          placeholder="搜索接收号码/内容"
          clearable
          style="width: 260px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.smsType"
          :options="SMS_TYPE_OPTIONS"
          placeholder="短信类型"
          clearable
          style="width: 130px"
        />
        <NSelect
          v-model:value="queryParams.smsStatus"
          :options="SMS_STATUS_OPTIONS"
          placeholder="发送状态"
          clearable
          style="width: 130px"
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
          <NButton type="primary" size="small" @click="handleAdd">
            新增短信
          </NButton>
        </template>
        <template #col_smsStatus="{ row }">
          <NTag :type="getSmsStatusType(row.smsStatus)" size="small">
            {{ getOptionLabel(SMS_STATUS_OPTIONS, row.smsStatus) }}
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
              确认删除该短信记录？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="modalVisible"
      :title="modalTitle"
      preset="card"
      style="width: 520px"
      :auto-focus="false"
    >
      <NForm class="xh-edit-form-grid" :model="formData" label-placement="top" label-width="80px">
        <NFormItem label="接收号码" path="toPhone">
          <NInput v-model:value="formData.toPhone" placeholder="手机号码" />
        </NFormItem>
        <NFormItem label="短信类型" path="smsType">
          <NSelect v-model:value="formData.smsType" :options="SMS_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="模板ID" path="templateId">
          <NInput v-model:value="formData.templateId" placeholder="短信模板ID" />
        </NFormItem>
        <NFormItem label="服务商" path="provider">
          <NInput v-model:value="formData.provider" placeholder="短信服务商" />
        </NFormItem>
        <NFormItem label="内容" path="content">
          <NInput
            v-model:value="formData.content"
            type="textarea"
            :rows="4"
            placeholder="短信内容"
          />
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
