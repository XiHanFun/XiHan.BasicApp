<script lang="ts" setup>
import type { DataTableColumns } from 'naive-ui'
import type { SysDict, SysDictItem } from '~/types'
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
import {
  createDictApi,
  createDictItemApi,
  deleteDictApi,
  deleteDictItemApi,
  getDictItemsApi,
  getDictPageApi,
  updateDictApi,
  updateDictItemApi,
} from '~/api'
import { DEFAULT_PAGE_SIZE, STATUS_OPTIONS } from '~/constants'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemDictPage' })

const message = useMessage()
const loading = ref(false)
const tableData = ref<SysDict[]>([])
const total = ref(0)

const queryParams = reactive({
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  keyword: '',
  status: undefined as number | undefined,
})

const modalVisible = ref(false)
const modalTitle = ref('新增字典')
const submitLoading = ref(false)

const formData = ref<Partial<SysDict>>({
  dictCode: '',
  dictName: '',
  dictType: '',
  dictDescription: '',
  status: 1,
  sort: 100,
  remark: '',
})

const itemModalVisible = ref(false)
const itemLoading = ref(false)
const itemRows = ref<SysDictItem[]>([])
const currentDictId = ref<string>('')
const currentDictName = ref('')
const currentDictCode = ref('')

const itemFormVisible = ref(false)
const itemSubmitLoading = ref(false)
const itemFormTitle = ref('新增字典项')
const itemFormData = ref<Partial<SysDictItem>>({
  itemCode: '',
  itemName: '',
  itemValue: '',
  status: 1,
  sort: 100,
  parentId: undefined,
})

async function fetchData() {
  try {
    loading.value = true
    const result = await getDictPageApi(queryParams)
    tableData.value = result.items
    total.value = result.total
  } catch {
    message.error('获取字典列表失败')
  } finally {
    loading.value = false
  }
}

function handleAdd() {
  modalTitle.value = '新增字典'
  formData.value = {
    dictCode: '',
    dictName: '',
    dictType: '',
    dictDescription: '',
    status: 1,
    sort: 100,
    remark: '',
  }
  modalVisible.value = true
}

function handleEdit(row: SysDict) {
  modalTitle.value = '编辑字典'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await deleteDictApi(id)
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
      await updateDictApi(formData.value.id, formData.value)
    } else {
      await createDictApi(formData.value)
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

async function openItems(row: SysDict) {
  currentDictId.value = row.id
  currentDictName.value = row.dictName
  currentDictCode.value = row.dictCode
  itemModalVisible.value = true
  await fetchItems()
}

async function fetchItems() {
  if (!currentDictId.value) return
  try {
    itemLoading.value = true
    itemRows.value = await getDictItemsApi(currentDictId.value)
  } catch {
    message.error('获取字典项失败')
  } finally {
    itemLoading.value = false
  }
}

function handleAddItem() {
  itemFormTitle.value = '新增字典项'
  itemFormData.value = {
    itemCode: '',
    itemName: '',
    itemValue: '',
    status: 1,
    sort: 100,
    parentId: undefined,
  }
  itemFormVisible.value = true
}

function handleEditItem(row: SysDictItem) {
  itemFormTitle.value = '编辑字典项'
  itemFormData.value = { ...row }
  itemFormVisible.value = true
}

async function handleDeleteItem(id: string) {
  try {
    await deleteDictItemApi(id)
    message.success('删除字典项成功')
    fetchItems()
  } catch {
    message.error('删除字典项失败')
  }
}

async function handleSubmitItem() {
  if (!currentDictId.value) {
    message.warning('未选择字典')
    return
  }

  try {
    itemSubmitLoading.value = true
    const payload = {
      ...itemFormData.value,
      dictId: Number(currentDictId.value),
      dictCode: currentDictCode.value,
    }

    if (itemFormData.value.id) {
      await updateDictItemApi(itemFormData.value.id, payload)
    } else {
      await createDictItemApi(payload)
    }

    message.success('字典项操作成功')
    itemFormVisible.value = false
    fetchItems()
  } catch {
    message.error('字典项操作失败')
  } finally {
    itemSubmitLoading.value = false
  }
}

const columns: DataTableColumns<SysDict> = [
  {
    title: '字典名称',
    key: 'dictName',
    width: 180,
  },
  {
    title: '字典编码',
    key: 'dictCode',
    width: 180,
    render: (row) =>
      h(NTag, { type: 'info', size: 'small', bordered: false }, { default: () => row.dictCode }),
  },
  {
    title: '字典类型',
    key: 'dictType',
    width: 150,
  },
  {
    title: '状态',
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
    title: '排序',
    key: 'sort',
    width: 90,
    align: 'center',
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
    width: 240,
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
                ghost: true,
                onClick: () => openItems(row),
              },
              { default: () => '字典项' },
            ),
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
                default: () => '确认删除该字典？',
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

const itemColumns: DataTableColumns<SysDictItem> = [
  {
    title: '项编码',
    key: 'itemCode',
    width: 150,
  },
  {
    title: '项名称',
    key: 'itemName',
    width: 160,
  },
  {
    title: '项值',
    key: 'itemValue',
    minWidth: 180,
    ellipsis: { tooltip: true },
  },
  {
    title: '状态',
    key: 'status',
    width: 90,
    render: (row) =>
      h(
        NTag,
        { type: row.status === 1 ? 'success' : 'error', size: 'small', round: true },
        { default: () => (row.status === 1 ? '启用' : '禁用') },
      ),
  },
  {
    title: '排序',
    key: 'sort',
    width: 80,
    align: 'center',
  },
  {
    title: '操作',
    key: 'actions',
    width: 160,
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
                onClick: () => handleEditItem(row),
              },
              { default: () => '编辑' },
            ),
            h(
              NPopconfirm,
              {
                onPositiveClick: () => handleDeleteItem(row.id),
              },
              {
                default: () => '确认删除该字典项？',
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
          placeholder="搜索字典名称/编码/类型"
          style="width: 240px"
          clearable
          @keydown.enter="fetchData"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          placeholder="状态"
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
          新增字典
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
        :scroll-x="1280"
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
      <NForm :model="formData" label-placement="left" label-width="90px">
        <NFormItem label="字典编码" path="dictCode">
          <NInput v-model:value="formData.dictCode" placeholder="请输入字典编码" />
        </NFormItem>
        <NFormItem label="字典名称" path="dictName">
          <NInput v-model:value="formData.dictName" placeholder="请输入字典名称" />
        </NFormItem>
        <NFormItem label="字典类型" path="dictType">
          <NInput v-model:value="formData.dictType" placeholder="如: gender, status" />
        </NFormItem>
        <NFormItem label="字典描述" path="dictDescription">
          <NInput
            v-model:value="formData.dictDescription"
            type="textarea"
            :rows="2"
            placeholder="可选"
          />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="formData.sort" :min="0" :max="9999" class="w-full" />
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

    <NModal
      v-model:show="itemModalVisible"
      :title="`${currentDictName} 字典项`"
      preset="card"
      style="width: 860px"
      :auto-focus="false"
    >
      <div class="mb-3 flex justify-end">
        <NButton type="primary" @click="handleAddItem">
          <template #icon>
            <NIcon><Icon icon="lucide:plus" width="14" /></NIcon>
          </template>
          新增字典项
        </NButton>
      </div>
      <NDataTable
        :columns="itemColumns"
        :data="itemRows"
        :loading="itemLoading"
        :row-key="(row) => row.id"
        :pagination="{ pageSize: 10 }"
        :scroll-x="900"
        size="small"
      />
    </NModal>

    <NModal
      v-model:show="itemFormVisible"
      :title="itemFormTitle"
      preset="card"
      style="width: 560px"
      :auto-focus="false"
    >
      <NForm :model="itemFormData" label-placement="left" label-width="90px">
        <NFormItem label="项编码" path="itemCode">
          <NInput v-model:value="itemFormData.itemCode" placeholder="请输入项编码" />
        </NFormItem>
        <NFormItem label="项名称" path="itemName">
          <NInput v-model:value="itemFormData.itemName" placeholder="请输入项名称" />
        </NFormItem>
        <NFormItem label="项值" path="itemValue">
          <NInput v-model:value="itemFormData.itemValue" placeholder="请输入项值" />
        </NFormItem>
        <NFormItem label="父级ID" path="parentId">
          <NInputNumber v-model:value="itemFormData.parentId" :min="1" class="w-full" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="itemFormData.sort" :min="0" :max="9999" class="w-full" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="itemFormData.status" :options="STATUS_OPTIONS" />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="itemFormVisible = false">取消</NButton>
          <NButton type="primary" :loading="itemSubmitLoading" @click="handleSubmitItem">确认</NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
