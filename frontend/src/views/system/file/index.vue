<script lang="ts" setup>
import type { DataTableColumns } from 'naive-ui'
import type { SysFile } from '~/types'
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
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { h, onMounted, reactive, ref } from 'vue'
import { createFileApi, deleteFileApi, getFilePageApi, updateFileApi } from '~/api'
import { DEFAULT_PAGE_SIZE, FILE_STATUS_OPTIONS, FILE_TYPE_OPTIONS } from '~/constants'
import { formatDate, formatFileSize, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemFilePage' })

const message = useMessage()
const loading = ref(false)
const tableData = ref<SysFile[]>([])
const total = ref(0)

const queryParams = reactive({
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  keyword: '',
  fileType: undefined as number | undefined,
  status: undefined as number | undefined,
})

const modalVisible = ref(false)
const modalTitle = ref('新增文件')
const submitLoading = ref(false)

const formData = ref<Partial<SysFile & { accessPermissions?: string }>>({
  fileName: '',
  originalName: '',
  fileType: 99,
  mimeType: '',
  fileSize: 0,
  fileHash: '',
  isPublic: true,
  requireAuth: false,
  accessPermissions: '',
  isTemporary: false,
  status: 0,
  tags: '',
  remark: '',
})

function getFileStatusType(status: number): 'default' | 'info' | 'success' | 'warning' | 'error' {
  if (status === 0)
    return 'success'
  if (status === 1 || status === 3)
    return 'info'
  if (status === 2 || status === 7 || status === 8)
    return 'error'
  if (status === 4 || status === 5 || status === 6)
    return 'warning'
  return 'default'
}

async function fetchData() {
  try {
    loading.value = true
    const result = await getFilePageApi(queryParams)
    tableData.value = result.items
    total.value = result.total
  }
  catch {
    message.error('获取文件列表失败')
  }
  finally {
    loading.value = false
  }
}

function handleAdd() {
  modalTitle.value = '新增文件'
  formData.value = {
    fileName: '',
    originalName: '',
    fileType: 99,
    mimeType: '',
    fileSize: 0,
    fileHash: '',
    isPublic: true,
    requireAuth: false,
    accessPermissions: '',
    isTemporary: false,
    status: 0,
    tags: '',
    remark: '',
  }
  modalVisible.value = true
}

function handleEdit(row: SysFile) {
  modalTitle.value = '编辑文件'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await deleteFileApi(id)
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
    if (formData.value.id) {
      await updateFileApi(formData.value.id, formData.value)
    }
    else {
      await createFileApi(formData.value)
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

const columns: DataTableColumns<SysFile> = [
  {
    title: '文件名',
    key: 'fileName',
    width: 220,
    ellipsis: { tooltip: true },
  },
  {
    title: '原始名称',
    key: 'originalName',
    width: 220,
    ellipsis: { tooltip: true },
  },
  {
    title: '类型',
    key: 'fileType',
    width: 100,
    render: row =>
      h(
        NTag,
        { size: 'small', bordered: false },
        { default: () => getOptionLabel(FILE_TYPE_OPTIONS, row.fileType) },
      ),
  },
  {
    title: '大小',
    key: 'fileSize',
    width: 120,
    render: row => formatFileSize(Number(row.fileSize || 0)),
  },
  {
    title: '状态',
    key: 'status',
    width: 100,
    render: row =>
      h(
        NTag,
        { type: getFileStatusType(row.status), size: 'small', round: true },
        { default: () => getOptionLabel(FILE_STATUS_OPTIONS, row.status) },
      ),
  },
  {
    title: '创建时间',
    key: 'createTime',
    width: 170,
    render: row => formatDate(row.createTime ?? ''),
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
                onPositiveClick: () => handleDelete(row.id),
              },
              {
                default: () => '确认删除该文件？',
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
          placeholder="搜索文件名/原始名称"
          style="width: 220px"
          clearable
          @keydown.enter="fetchData"
        />
        <NSelect
          v-model:value="queryParams.fileType"
          :options="FILE_TYPE_OPTIONS"
          placeholder="全部类型"
          style="width: 130px"
          clearable
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="FILE_STATUS_OPTIONS"
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
        <NButton
          @click="
            () => {
              queryParams.keyword = ''
              queryParams.fileType = undefined
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
          新增文件
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
        :scroll-x="1080"
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
      <NForm :model="formData" label-placement="left" label-width="96px">
        <NFormItem label="文件名" path="fileName">
          <NInput v-model:value="formData.fileName" placeholder="请输入文件名" />
        </NFormItem>
        <NFormItem label="原始名称" path="originalName">
          <NInput v-model:value="formData.originalName" placeholder="请输入原始文件名" />
        </NFormItem>
        <NFormItem label="文件类型" path="fileType">
          <NSelect v-model:value="formData.fileType" :options="FILE_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="MIME 类型" path="mimeType">
          <NInput v-model:value="formData.mimeType" placeholder="如: image/png" />
        </NFormItem>
        <NFormItem label="文件大小(B)" path="fileSize">
          <NInputNumber v-model:value="formData.fileSize" :min="0" class="w-full" />
        </NFormItem>
        <NFormItem label="文件哈希" path="fileHash">
          <NInput v-model:value="formData.fileHash" placeholder="可选，去重使用" />
        </NFormItem>
        <NFormItem label="访问权限" path="accessPermissions">
          <NInput v-model:value="formData.accessPermissions" placeholder="可选，逗号分隔" />
        </NFormItem>
        <NFormItem label="文件状态" path="status">
          <NSelect v-model:value="formData.status" :options="FILE_STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="公开访问" path="isPublic">
          <NSwitch v-model:value="formData.isPublic" />
        </NFormItem>
        <NFormItem label="需要鉴权" path="requireAuth">
          <NSwitch v-model:value="formData.requireAuth" />
        </NFormItem>
        <NFormItem label="临时文件" path="isTemporary">
          <NSwitch v-model:value="formData.isTemporary" />
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
