<script lang="ts" setup>
import type { DataTableColumns } from 'naive-ui'
import type { SysReview } from '~/types'
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
import { createReviewApi, deleteReviewApi, getReviewPageApi, updateReviewApi } from '~/api'
import {
  DEFAULT_PAGE_SIZE,
  REVIEW_RESULT_OPTIONS,
  REVIEW_STATUS_OPTIONS,
  STATUS_OPTIONS,
} from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemReviewPage' })

const message = useMessage()
const loading = ref(false)
const tableData = ref<SysReview[]>([])
const total = ref(0)

const queryParams = reactive({
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  keyword: '',
  reviewStatus: undefined as number | undefined,
  reviewResult: undefined as number | undefined,
  status: undefined as number | undefined,
})

const modalVisible = ref(false)
const modalTitle = ref('新增审查')
const submitLoading = ref(false)

const formData = ref<Partial<SysReview>>({
  reviewCode: '',
  reviewTitle: '',
  reviewType: '',
  reviewContent: '',
  reviewStatus: 0,
  reviewResult: undefined,
  priority: 3,
  submitUserId: undefined,
  currentReviewUserId: undefined,
  reviewLevel: 1,
  currentLevel: 1,
  status: 1,
  remark: '',
})

function getReviewStatusType(
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
    const result = await getReviewPageApi(queryParams)
    tableData.value = result.items
    total.value = result.total
  } catch {
    message.error('获取审查列表失败')
  } finally {
    loading.value = false
  }
}

function handleAdd() {
  modalTitle.value = '新增审查'
  formData.value = {
    reviewCode: '',
    reviewTitle: '',
    reviewType: '',
    reviewContent: '',
    reviewStatus: 0,
    reviewResult: undefined,
    priority: 3,
    submitUserId: undefined,
    currentReviewUserId: undefined,
    reviewLevel: 1,
    currentLevel: 1,
    status: 1,
    remark: '',
  }
  modalVisible.value = true
}

function handleEdit(row: SysReview) {
  modalTitle.value = '编辑审查'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await deleteReviewApi(id)
    message.success('删除成功')
    fetchData()
  } catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.id) {
      await updateReviewApi(formData.value.id, formData.value)
    } else {
      await createReviewApi(formData.value)
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

const columns: DataTableColumns<SysReview> = [
  {
    title: '审查编码',
    key: 'reviewCode',
    width: 150,
    render: (row) =>
      h(NTag, { type: 'info', size: 'small', bordered: false }, { default: () => row.reviewCode }),
  },
  {
    title: '审查标题',
    key: 'reviewTitle',
    width: 220,
    ellipsis: { tooltip: true },
  },
  {
    title: '审查类型',
    key: 'reviewType',
    width: 130,
  },
  {
    title: '审查状态',
    key: 'reviewStatus',
    width: 110,
    render: (row) =>
      h(
        NTag,
        { type: getReviewStatusType(row.reviewStatus), size: 'small', round: true },
        { default: () => getOptionLabel(REVIEW_STATUS_OPTIONS, row.reviewStatus) },
      ),
  },
  {
    title: '审查结果',
    key: 'reviewResult',
    width: 110,
    render: (row) => getOptionLabel(REVIEW_RESULT_OPTIONS, row.reviewResult, '-'),
  },
  {
    title: '当前层级',
    key: 'currentLevel',
    width: 100,
    render: (row) => `${row.currentLevel}/${row.reviewLevel}`,
  },
  {
    title: '提交时间',
    key: 'submitTime',
    width: 170,
    render: (row) => formatDate(row.submitTime),
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
                onPositiveClick: () => handleDelete(row.id),
              },
              {
                default: () => '确认删除该审查？',
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
          placeholder="搜索审查编码/标题/类型"
          style="width: 240px"
          clearable
          @keydown.enter="fetchData"
        />
        <NSelect
          v-model:value="queryParams.reviewStatus"
          :options="REVIEW_STATUS_OPTIONS"
          placeholder="审查状态"
          style="width: 130px"
          clearable
        />
        <NSelect
          v-model:value="queryParams.reviewResult"
          :options="REVIEW_RESULT_OPTIONS"
          placeholder="审查结果"
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
              queryParams.reviewStatus = undefined
              queryParams.reviewResult = undefined
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
          新增审查
        </NButton>
      </div>
    </NCard>

    <NCard :bordered="false">
      <NDataTable
        :columns="columns"
        :data="tableData"
        :loading="loading"
        :row-key="(row) => row.id"
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
      style="width: 680px"
      :auto-focus="false"
    >
      <NForm :model="formData" label-placement="left" label-width="98px">
        <NFormItem label="审查编码" path="reviewCode">
          <NInput
            v-model:value="formData.reviewCode"
            :disabled="!!formData.id"
            placeholder="请输入审查编码"
          />
        </NFormItem>
        <NFormItem label="审查标题" path="reviewTitle">
          <NInput v-model:value="formData.reviewTitle" placeholder="请输入审查标题" />
        </NFormItem>
        <NFormItem label="审查类型" path="reviewType">
          <NInput v-model:value="formData.reviewType" placeholder="如: expense, contract" />
        </NFormItem>
        <NFormItem label="审查内容" path="reviewContent">
          <NInput
            v-model:value="formData.reviewContent"
            type="textarea"
            :rows="3"
            placeholder="可选"
          />
        </NFormItem>
        <NFormItem label="审查状态" path="reviewStatus">
          <NSelect v-model:value="formData.reviewStatus" :options="REVIEW_STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="审查结果" path="reviewResult">
          <NSelect
            v-model:value="formData.reviewResult"
            :options="REVIEW_RESULT_OPTIONS"
            clearable
            placeholder="可选"
          />
        </NFormItem>
        <NFormItem label="优先级" path="priority">
          <NInputNumber v-model:value="formData.priority" :min="1" :max="5" class="w-full" />
        </NFormItem>
        <NFormItem label="提交人ID" path="submitUserId">
          <NInputNumber v-model:value="formData.submitUserId" :min="1" class="w-full" />
        </NFormItem>
        <NFormItem label="当前审查人ID" path="currentReviewUserId">
          <NInputNumber v-model:value="formData.currentReviewUserId" :min="1" class="w-full" />
        </NFormItem>
        <NFormItem label="审查层级" path="reviewLevel">
          <NInputNumber v-model:value="formData.reviewLevel" :min="1" :max="10" class="w-full" />
        </NFormItem>
        <NFormItem label="当前层级" path="currentLevel">
          <NInputNumber v-model:value="formData.currentLevel" :min="1" :max="10" class="w-full" />
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
