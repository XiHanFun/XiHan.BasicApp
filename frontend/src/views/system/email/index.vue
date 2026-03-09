<script lang="ts" setup>
import type { DataTableColumns } from 'naive-ui'
import type { SysEmail } from '~/types'
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
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { h, onMounted, reactive, ref } from 'vue'
import {
  createEmailApi,
  deleteEmailApi,
  getEmailPageApi,
  getPendingEmailsApi,
  updateEmailApi,
} from '~/api'
import { DEFAULT_PAGE_SIZE, EMAIL_STATUS_OPTIONS, EMAIL_TYPE_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemEmailPage' })

const message = useMessage()
const loading = ref(false)
const tableData = ref<SysEmail[]>([])
const total = ref(0)

const queryParams = reactive({
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  keyword: '',
  emailType: undefined as number | undefined,
  emailStatus: undefined as number | undefined,
})

const modalVisible = ref(false)
const modalTitle = ref('新增邮件')
const submitLoading = ref(false)

const formData = ref<Partial<SysEmail>>({
  emailType: 0,
  fromEmail: '',
  toEmail: '',
  subject: '',
  content: '',
  isHtml: true,
  emailStatus: 0,
  scheduledTime: undefined,
  remark: '',
})

function getEmailStatusType(status: number): 'default' | 'info' | 'success' | 'warning' | 'error' {
  if (status === 0) return 'warning'
  if (status === 1) return 'info'
  if (status === 2) return 'success'
  if (status === 3) return 'error'
  if (status === 4) return 'default'
  return 'default'
}

async function fetchData() {
  try {
    loading.value = true
    const result = await getEmailPageApi(queryParams)
    tableData.value = result.items
    total.value = result.total
  } catch {
    message.error('获取邮件列表失败')
  } finally {
    loading.value = false
  }
}

async function handleLoadPending() {
  try {
    loading.value = true
    const pending = await getPendingEmailsApi(200)
    tableData.value = pending
    total.value = pending.length
    queryParams.page = 1
  } catch {
    message.error('获取待发送邮件失败')
  } finally {
    loading.value = false
  }
}

function handleAdd() {
  modalTitle.value = '新增邮件'
  formData.value = {
    emailType: 0,
    fromEmail: '',
    toEmail: '',
    subject: '',
    content: '',
    isHtml: true,
    emailStatus: 0,
    scheduledTime: undefined,
    remark: '',
  }
  modalVisible.value = true
}

function handleEdit(row: SysEmail) {
  modalTitle.value = '编辑邮件'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await deleteEmailApi(id)
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
      await updateEmailApi(formData.value.basicId, formData.value)
    } else {
      await createEmailApi(formData.value)
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

const columns: DataTableColumns<SysEmail> = [
  {
    title: '发件邮箱',
    key: 'fromEmail',
    width: 200,
    ellipsis: { tooltip: true },
  },
  {
    title: '收件邮箱',
    key: 'toEmail',
    width: 220,
    ellipsis: { tooltip: true },
  },
  {
    title: '主题',
    key: 'subject',
    width: 220,
    ellipsis: { tooltip: true },
  },
  {
    title: '类型',
    key: 'emailType',
    width: 110,
    render: (row) => getOptionLabel(EMAIL_TYPE_OPTIONS, row.emailType),
  },
  {
    title: '状态',
    key: 'emailStatus',
    width: 100,
    render: (row) =>
      h(
        NTag,
        { type: getEmailStatusType(row.emailStatus), size: 'small', round: true },
        { default: () => getOptionLabel(EMAIL_STATUS_OPTIONS, row.emailStatus) },
      ),
  },
  {
    title: '发送时间',
    key: 'sendTime',
    width: 170,
    render: (row) => formatDate(row.sendTime ?? ''),
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
                default: () => '确认删除该邮件？',
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
          placeholder="搜索发件人/收件人/主题"
          style="width: 240px"
          clearable
          @keydown.enter="fetchData"
        />
        <NSelect
          v-model:value="queryParams.emailType"
          :options="EMAIL_TYPE_OPTIONS"
          placeholder="全部类型"
          style="width: 130px"
          clearable
        />
        <NSelect
          v-model:value="queryParams.emailStatus"
          :options="EMAIL_STATUS_OPTIONS"
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
        <NButton @click="handleLoadPending">待发送</NButton>
        <NButton
          @click="
            () => {
              queryParams.keyword = ''
              queryParams.emailType = undefined
              queryParams.emailStatus = undefined
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
          新增邮件
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
        :scroll-x="1180"
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
      style="width: 600px"
      :auto-focus="false"
    >
      <NForm :model="formData" label-placement="left" label-width="88px">
        <NFormItem label="邮件类型" path="emailType">
          <NSelect v-model:value="formData.emailType" :options="EMAIL_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="发件邮箱" path="fromEmail">
          <NInput v-model:value="formData.fromEmail" placeholder="请输入发件邮箱" />
        </NFormItem>
        <NFormItem label="收件邮箱" path="toEmail">
          <NInput v-model:value="formData.toEmail" placeholder="请输入收件邮箱，多个用逗号分隔" />
        </NFormItem>
        <NFormItem label="邮件主题" path="subject">
          <NInput v-model:value="formData.subject" placeholder="请输入邮件主题" />
        </NFormItem>
        <NFormItem label="邮件内容" path="content">
          <NInput
            v-model:value="formData.content"
            type="textarea"
            :rows="4"
            placeholder="请输入邮件内容"
          />
        </NFormItem>
        <NFormItem label="邮件状态" path="emailStatus">
          <NSelect v-model:value="formData.emailStatus" :options="EMAIL_STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="HTML邮件" path="isHtml">
          <NSwitch v-model:value="formData.isHtml" />
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
