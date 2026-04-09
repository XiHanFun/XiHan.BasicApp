<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysDict, SysDictItem } from '@/api'
import {
  NButton,
  NDrawer,
  NDrawerContent,
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
import { dictApi } from '@/api'
import { STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemDictPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
  status: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return dictApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    status: queryParams.status,
  })
}

const options = useVxeTable<SysDict>({
  id: 'sys_dict',
  name: '字典管理',
  columns: [
    { type: 'seq', title: '序号', width: 60, fixed: 'left' },
    { field: 'dictName', title: '字典名称', minWidth: 160, showOverflow: 'tooltip', sortable: true },
    { field: 'dictCode', title: '字典编码', minWidth: 160, showOverflow: 'tooltip' },
    { field: 'dictType', title: '字典类型', minWidth: 120, showOverflow: 'tooltip' },
    { field: 'dictDescription', title: '描述', minWidth: 200, showOverflow: 'tooltip' },
    { field: 'sort', title: '排序', width: 70 },
    {
      field: 'status',
      title: '状态',
      width: 80,
      slots: { default: 'col_status' },
    },
    { field: 'createTime', title: '创建时间', width: 170, formatter: ({ cellValue }) => formatDate(cellValue), sortable: true },
    {
      field: 'actions',
      title: '操作',
      width: 180,
      fixed: 'right',
      slots: { default: 'col_actions' },
    },
  ],
}, {
  proxyConfig: {
    autoLoad: true,
    ajax: {
      query: ({ page }) => handleQueryApi(page),
    },
  },
})

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

// ==================== 字典 CRUD ====================

const modalVisible = ref(false)
const modalTitle = ref('新增字典')
const submitLoading = ref(false)
const formData = ref<Partial<SysDict>>({})

function resetForm() {
  formData.value = { dictName: '', dictCode: '', dictType: '', dictDescription: '', sort: 0, status: 1 }
}

function handleAdd() {
  modalTitle.value = '新增字典'
  resetForm()
  modalVisible.value = true
}

function handleEdit(row: SysDict) {
  modalTitle.value = '编辑字典'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await dictApi.delete(id)
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
    if (formData.value.basicId) {
      await dictApi.update(formData.value.basicId, formData.value)
    }
    else {
      await dictApi.create(formData.value)
    }
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

// ==================== 字典项管理（抽屉） ====================

const drawerVisible = ref(false)
const currentDict = ref<SysDict | null>(null)
const dictItems = ref<SysDictItem[]>([])
const dictItemsLoading = ref(false)

const itemModalVisible = ref(false)
const itemModalTitle = ref('新增字典项')
const itemSubmitLoading = ref(false)
const itemFormData = ref<Partial<SysDictItem>>({})

async function handleManageItems(row: SysDict) {
  currentDict.value = row
  drawerVisible.value = true
  await fetchDictItems(row.basicId)
}

async function fetchDictItems(dictId: string) {
  try {
    dictItemsLoading.value = true
    dictItems.value = await dictApi.getItems(dictId)
  }
  catch {
    message.error('获取字典项失败')
  }
  finally {
    dictItemsLoading.value = false
  }
}

function handleAddItem() {
  if (!currentDict.value)
    return
  itemModalTitle.value = '新增字典项'
  itemFormData.value = {
    dictId: currentDict.value.basicId,
    dictCode: currentDict.value.dictCode,
    itemCode: '',
    itemName: '',
    itemValue: '',
    sort: 0,
    status: 1,
  }
  itemModalVisible.value = true
}

function handleEditItem(row: SysDictItem) {
  itemModalTitle.value = '编辑字典项'
  itemFormData.value = { ...row }
  itemModalVisible.value = true
}

async function handleDeleteItem(id: string) {
  try {
    await dictApi.deleteItem(id)
    message.success('删除成功')
    if (currentDict.value)
      fetchDictItems(currentDict.value.basicId)
  }
  catch {
    message.error('删除失败')
  }
}

async function handleItemSubmit() {
  try {
    itemSubmitLoading.value = true
    if (itemFormData.value.basicId) {
      await dictApi.updateItem(itemFormData.value.basicId, itemFormData.value)
    }
    else {
      await dictApi.createItem(itemFormData.value)
    }
    message.success('操作成功')
    itemModalVisible.value = false
    if (currentDict.value)
      fetchDictItems(currentDict.value.basicId)
  }
  catch {
    message.error('操作失败')
  }
  finally {
    itemSubmitLoading.value = false
  }
}
</script>

<template>
  <div class="h-full flex flex-col">
    <vxe-card class="mb-2" style="padding: 10px 16px">
      <div class="flex items-center gap-3 flex-wrap">
        <vxe-input v-model="queryParams.keyword" placeholder="搜索字典名称/编码" clearable style="width: 260px" @keyup.enter="handleSearch" />
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
            新增字典
          </NButton>
        </template>
        <template #col_status="{ row }">
          <NTag :type="row.status === 1 ? 'success' : 'error'" size="small" round>
            {{ row.status === 1 ? '启用' : '禁用' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton size="small" type="info" text @click="handleManageItems(row)">
              字典项
            </NButton>
            <NButton size="small" type="primary" text @click="handleEdit(row)">
              编辑
            </NButton>
            <NPopconfirm @positive-click="handleDelete(row.basicId)">
              <template #trigger>
                <NButton size="small" type="error" text>
                  删除
                </NButton>
              </template>
              确认删除该字典？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <!-- 字典编辑弹窗 -->
    <NModal v-model:show="modalVisible" :title="modalTitle" preset="card" style="width: 520px" :auto-focus="false">
      <NForm :model="formData" label-placement="left" label-width="90px">
        <NFormItem label="字典名称" path="dictName">
          <NInput v-model:value="formData.dictName" placeholder="请输入字典名称" />
        </NFormItem>
        <NFormItem label="字典编码" path="dictCode">
          <NInput v-model:value="formData.dictCode" placeholder="如: sys_gender" />
        </NFormItem>
        <NFormItem label="字典类型" path="dictType">
          <NInput v-model:value="formData.dictType" placeholder="如: system" />
        </NFormItem>
        <NFormItem label="描述" path="dictDescription">
          <NInput v-model:value="formData.dictDescription" type="textarea" :rows="2" placeholder="字典描述" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="formData.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
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

    <!-- 字典项抽屉 -->
    <NDrawer v-model:show="drawerVisible" :width="600">
      <NDrawerContent :title="`字典项管理 - ${currentDict?.dictName ?? ''}`" closable>
        <div class="mb-3">
          <NButton type="primary" size="small" @click="handleAddItem">
            新增字典项
          </NButton>
        </div>
        <div v-if="dictItemsLoading" class="py-8 text-center text-gray-400">
          加载中...
        </div>
        <div v-else-if="!dictItems.length" class="py-8 text-center text-gray-400">
          暂无字典项
        </div>
        <div v-else class="space-y-2">
          <div v-for="item in dictItems" :key="item.basicId" class="flex items-center justify-between rounded border p-3">
            <div>
              <div class="font-medium">
                {{ item.itemName }}
              </div>
              <div class="text-xs text-gray-400">
                编码: {{ item.itemCode }} | 值: {{ item.itemValue }} | 排序: {{ item.sort }}
              </div>
            </div>
            <NSpace size="small">
              <NTag :type="item.status === 1 ? 'success' : 'error'" size="small">
                {{ item.status === 1 ? '启用' : '禁用' }}
              </NTag>
              <NButton size="tiny" type="primary" text @click="handleEditItem(item)">
                编辑
              </NButton>
              <NPopconfirm @positive-click="handleDeleteItem(item.basicId)">
                <template #trigger>
                  <NButton size="tiny" type="error" text>
                    删除
                  </NButton>
                </template>
                确认删除该字典项？
              </NPopconfirm>
            </NSpace>
          </div>
        </div>
      </NDrawerContent>
    </NDrawer>

    <!-- 字典项编辑弹窗 -->
    <NModal v-model:show="itemModalVisible" :title="itemModalTitle" preset="card" style="width: 460px" :auto-focus="false">
      <NForm :model="itemFormData" label-placement="left" label-width="80px">
        <NFormItem label="项名称" path="itemName">
          <NInput v-model:value="itemFormData.itemName" placeholder="字典项名称" />
        </NFormItem>
        <NFormItem label="项编码" path="itemCode">
          <NInput v-model:value="itemFormData.itemCode" placeholder="字典项编码" />
        </NFormItem>
        <NFormItem label="项值" path="itemValue">
          <NInput v-model:value="itemFormData.itemValue" placeholder="字典项值" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="itemFormData.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="itemFormData.status" :options="STATUS_OPTIONS" />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="itemModalVisible = false">
            取消
          </NButton>
          <NButton type="primary" :loading="itemSubmitLoading" @click="handleItemSubmit">
            确认
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
