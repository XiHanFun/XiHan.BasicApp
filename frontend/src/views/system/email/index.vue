<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysEmail } from '@/api'
import {
  NButton,
  NForm,
  NFormItem,
  NInput,
  NModal,
  NPopconfirm,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { emailApi } from '@/api'
import { EMAIL_STATUS_OPTIONS, EMAIL_TYPE_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemEmailPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
  emailType: undefined as number | undefined,
  emailStatus: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return emailApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    emailType: queryParams.emailType,
    emailStatus: queryParams.emailStatus,
  })
}

const options = useVxeTable<SysEmail>(
  {
    id: 'sys_email',
    name: '邮件管理',
    columns: [
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      { field: 'fromEmail', title: '发件人', minWidth: 180, showOverflow: 'tooltip' },
      { field: 'toEmail', title: '收件人', minWidth: 180, showOverflow: 'tooltip' },
      { field: 'ccEmail', title: '抄送', minWidth: 160, showOverflow: 'tooltip' },
      { field: 'subject', title: '主题', minWidth: 200, showOverflow: 'tooltip' },
      { field: 'fromName', title: '发件人名', minWidth: 120, showOverflow: 'tooltip' },
      {
        field: 'emailType',
        title: '类型',
        width: 110,
        formatter: ({ cellValue }) => getOptionLabel(EMAIL_TYPE_OPTIONS, cellValue),
      },
      {
        field: 'emailStatus',
        title: '发送状态',
        width: 100,
        slots: { default: 'col_emailStatus' },
      },
      { field: 'retryCount', title: '重试次数', width: 90 },
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
  queryParams.emailType = undefined
  queryParams.emailStatus = undefined
  xGrid.value?.commitProxy('reload')
}

const modalVisible = ref(false)
const modalTitle = ref('新增邮件')
const submitLoading = ref(false)
const formData = ref<Partial<SysEmail>>({})

function resetForm() {
  formData.value = {
    fromEmail: '',
    toEmail: '',
    subject: '',
    content: '',
    emailType: 0,
    isHtml: false,
  }
}
function handleAdd() {
  modalTitle.value = '新增邮件'
  resetForm()
  modalVisible.value = true
}
function handleEdit(row: SysEmail) {
  modalTitle.value = '编辑邮件'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await emailApi.delete(id)
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
      await emailApi.update(formData.value.basicId, formData.value)
    else await emailApi.create(formData.value)
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

function getEmailStatusType(status: number) {
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
    <vxe-card class="mb-2" style="padding: 10px 16px">
      <div class="flex flex-wrap gap-3 items-center">
        <vxe-input
          v-model="queryParams.keyword"
          placeholder="搜索发件人/收件人/主题"
          clearable
          style="width: 280px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.emailType"
          :options="EMAIL_TYPE_OPTIONS"
          placeholder="邮件类型"
          clearable
          style="width: 140px"
        />
        <NSelect
          v-model:value="queryParams.emailStatus"
          :options="EMAIL_STATUS_OPTIONS"
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
    </vxe-card>
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #toolbar_buttons>
          <NButton type="primary" size="small" @click="handleAdd">
            新增邮件
          </NButton>
        </template>
        <template #col_emailStatus="{ row }">
          <NTag :type="getEmailStatusType(row.emailStatus)" size="small">
            {{ getOptionLabel(EMAIL_STATUS_OPTIONS, row.emailStatus) }}
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
              确认删除该邮件？
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
      <NForm :model="formData" label-placement="left" label-width="80px">
        <NFormItem label="发件人" path="fromEmail">
          <NInput v-model:value="formData.fromEmail" placeholder="发件人邮箱" />
        </NFormItem>
        <NFormItem label="收件人" path="toEmail">
          <NInput v-model:value="formData.toEmail" placeholder="收件人邮箱" />
        </NFormItem>
        <NFormItem label="主题" path="subject">
          <NInput v-model:value="formData.subject" placeholder="邮件主题" />
        </NFormItem>
        <NFormItem label="类型" path="emailType">
          <NSelect v-model:value="formData.emailType" :options="EMAIL_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="HTML">
          <NSwitch v-model:value="formData.isHtml" />
        </NFormItem>
        <NFormItem label="内容" path="content">
          <NInput
            v-model:value="formData.content"
            type="textarea"
            :rows="5"
            placeholder="邮件内容"
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
