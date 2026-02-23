<script lang="ts" setup>
import { ref, h, reactive, onMounted } from 'vue'
import {
  NCard,
  NDataTable,
  NButton,
  NSpace,
  NInput,
  NSelect,
  NTag,
  NPopconfirm,
  NModal,
  NForm,
  NFormItem,
  NInputNumber,
  NIcon,
  NPagination,
  useMessage,
} from 'naive-ui'
import type { DataTableColumns } from 'naive-ui'
import { Icon } from '@iconify/vue'
import type { SysRole } from '~/types'
import { STATUS_OPTIONS, DEFAULT_PAGE_SIZE } from '~/constants'
import { formatDate, getStatusType } from '~/utils'
import { getRolePageApi, createRoleApi, updateRoleApi, deleteRoleApi } from '~/api'

defineOptions({ name: 'SystemRolePage' })

const message = useMessage()

const loading = ref(false)
const tableData = ref<SysRole[]>([])
const total = ref(0)

const queryParams = reactive({
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  keyword: '',
  status: undefined as number | undefined,
})

const modalVisible = ref(false)
const modalTitle = ref('新增角色')
const submitLoading = ref(false)

const formData = ref<Partial<SysRole>>({
  name: '',
  code: '',
  description: '',
  status: 1,
  sort: 100,
})

async function fetchData() {
  try {
    loading.value = true
    const result = await getRolePageApi(queryParams)
    tableData.value = result.items
    total.value = result.total
  } catch {
    message.error('获取角色列表失败')
  } finally {
    loading.value = false
  }
}

function handleAdd() {
  modalTitle.value = '新增角色'
  formData.value = { name: '', code: '', description: '', status: 1, sort: 100 }
  modalVisible.value = true
}

function handleEdit(row: SysRole) {
  modalTitle.value = '编辑角色'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await deleteRoleApi(id)
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
      await updateRoleApi(formData.value.id, formData.value)
    } else {
      await createRoleApi(formData.value)
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

const columns: DataTableColumns<SysRole> = [
  {
    title: '角色名称',
    key: 'name',
    width: 150,
  },
  {
    title: '角色编码',
    key: 'code',
    width: 150,
    render: (row) =>
      h(NTag, { type: 'info', size: 'small', bordered: false }, { default: () => row.code }),
  },
  {
    title: '描述',
    key: 'description',
    ellipsis: { tooltip: true },
  },
  {
    title: '排序',
    key: 'sort',
    width: 80,
    align: 'center',
  },
  {
    title: '状态',
    key: 'status',
    width: 90,
    render: (row) =>
      h(
        NTag,
        { type: getStatusType(row.status), size: 'small', round: true },
        { default: () => (row.status === 1 ? '启用' : '禁用') },
      ),
  },
  {
    title: '创建时间',
    key: 'createTime',
    width: 170,
    render: (row) => formatDate(row.createTime),
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
                default: () => '确认删除该角色？',
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
          placeholder="搜索角色名称/编码"
          style="width: 200px"
          clearable
          @keydown.enter="fetchData"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          placeholder="全部状态"
          style="width: 110px"
          clearable
        />
        <NButton type="primary" @click="fetchData">搜索</NButton>
        <NButton
          @click="
            () => {
              queryParams.keyword = ''
              queryParams.status = undefined
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
          新增角色
        </NButton>
      </div>
    </NCard>

    <NCard :bordered="false">
      <NDataTable
        :columns="columns"
        :data="tableData"
        :loading="loading"
        :row-key="(row) => row.id"
        :scroll-x="800"
        :pagination="false"
        size="small"
        striped
      />
      <div class="mt-4 flex justify-end">
        <NPagination
          v-model:page="queryParams.page"
          v-model:page-size="queryParams.pageSize"
          :item-count="total"
          :page-sizes="[10, 20, 50]"
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
      style="width: 480px"
      :auto-focus="false"
    >
      <NForm :model="formData" label-placement="left" label-width="80px">
        <NFormItem label="角色名称" path="name">
          <NInput v-model:value="formData.name" placeholder="请输入角色名称" />
        </NFormItem>
        <NFormItem label="角色编码" path="code">
          <NInput v-model:value="formData.code" placeholder="如: admin, editor" />
        </NFormItem>
        <NFormItem label="描述" path="description">
          <NInput
            v-model:value="formData.description"
            type="textarea"
            :rows="3"
            placeholder="请输入角色描述"
          />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="formData.sort" :min="0" :max="9999" />
        </NFormItem>
        <NFormItem label="状态" path="status">
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
