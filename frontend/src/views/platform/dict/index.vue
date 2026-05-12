<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type {
  DictCreateDto,
  DictDetailDto,
  DictItemCreateDto,
  DictItemListItemDto,
  DictItemUpdateDto,
  DictListItemDto,
  DictUpdateDto,
} from '@/api'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NEmpty,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NPopconfirm,
  NScrollbar,
  NSelect,
  NSpace,
  NSpin,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, reactive, ref } from 'vue'
import { createPageRequest, dictManagementApi, EnableStatus } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformDictPage' })

interface DictGridResult {
  items: DictListItemDto[]
  total: number
}

interface DictItemGridResult {
  items: DictItemListItemDto[]
  total: number
}

interface DictFormModel {
  basicId?: string
  dictCode: string
  dictDescription?: string | null
  dictName: string
  dictType: string
  sort: number
  status: EnableStatus
}

interface DictItemFormModel {
  basicId?: string
  dictId: string
  isDefault: boolean
  itemCode: string
  itemDescription?: string | null
  itemName: string
  itemValue?: string | null
  parentId?: string | null
  sort: number
  status: EnableStatus
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<DictListItemDto>>()
const xItemGrid = ref<VxeGridInstance<DictItemListItemDto>>()

const queryParams = reactive({
  isBuiltIn: undefined as number | undefined,
  keyword: '',
  status: undefined as EnableStatus | undefined,
})

const itemQueryParams = reactive({
  keyword: '',
})

const statusOptions = STATUS_OPTIONS

const builtInOptions = [
  { label: '内置', value: 1 },
  { label: '非内置', value: 0 },
]

// Dict modal state
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const dictForm = ref<DictFormModel>(createDefaultDictForm())

const modalTitle = computed(() => (dictForm.value.basicId ? '编辑字典' : '新增字典'))

// Dict detail drawer state
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<DictDetailDto | null>(null)
const currentDict = ref<DictListItemDto | null>(null)

// Dict item modal state
const itemModalVisible = ref(false)
const itemSubmitLoading = ref(false)
const itemEditingStatus = ref<EnableStatus | null>(null)
const itemForm = ref<DictItemFormModel>(createDefaultDictItemForm())

const itemModalTitle = computed(() => (itemForm.value.basicId ? '编辑字典项' : '新增字典项'))

function createDefaultDictForm(): DictFormModel {
  return {
    dictCode: '',
    dictDescription: null,
    dictName: '',
    dictType: '',
    sort: 100,
    status: EnableStatus.Enabled,
  }
}

function createDefaultDictItemForm(): DictItemFormModel {
  return {
    dictId: '',
    isDefault: false,
    itemCode: '',
    itemDescription: null,
    itemName: '',
    itemValue: null,
    parentId: null,
    sort: 100,
    status: EnableStatus.Enabled,
  }
}

function formatNullable(value: unknown) {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

function formatBoolean(value?: boolean | null) {
  if (value === undefined || value === null) {
    return '-'
  }
  return value ? '是' : '否'
}

function canMaintainDict(row: DictListItemDto) {
  return !row.isBuiltIn
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<DictGridResult> {
  return dictManagementApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      isBuiltIn: queryParams.isBuiltIn === undefined ? undefined : Boolean(queryParams.isBuiltIn),
      keyword: queryParams.keyword?.trim() || undefined,
      status: queryParams.status,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询字典失败')
      return { items: [], total: 0 }
    })
}

function handleItemQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<DictItemGridResult> {
  if (!currentDict.value)
    return Promise.resolve({ items: [], total: 0 })
  return dictManagementApi
    .itemPage({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      dictId: currentDict.value.basicId,
      keyword: itemQueryParams.keyword?.trim() || undefined,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询字典项失败')
      return { items: [], total: 0 }
    })
}

const tableOptions = useVxeTable<DictListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'dictName', minWidth: 140, showOverflow: 'tooltip', sortable: true, title: '字典名称' },
      { field: 'dictCode', minWidth: 140, showOverflow: 'tooltip', title: '字典编码' },
      { field: 'dictType', minWidth: 120, showOverflow: 'tooltip', title: '字典类型' },
      {
        field: 'isBuiltIn',
        slots: { default: 'col_builtin' },
        title: '内置',
        width: 70,
      },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '状态',
        width: 80,
      },
      { field: 'sort', sortable: true, title: '排序', width: 80 },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '创建时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 160,
      },
    ],
    id: 'sys_dict',
    name: '字典管理',
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleQueryApi(page),
      },
    },
  },
)

const itemTableOptions = useVxeTable<DictItemListItemDto>(
  {
    columns: [
      { title: '序号', type: 'seq', width: 60 },
      { field: 'itemName', minWidth: 130, showOverflow: 'tooltip', sortable: true, title: '字典项名称' },
      { field: 'itemCode', minWidth: 130, showOverflow: 'tooltip', title: '编码' },
      { field: 'itemValue', minWidth: 100, showOverflow: 'tooltip', title: '值' },
      {
        field: 'isDefault',
        slots: { default: 'col_item_default' },
        title: '默认',
        width: 70,
      },
      {
        field: 'status',
        slots: { default: 'col_item_status' },
        title: '状态',
        width: 80,
      },
      { field: 'sort', title: '排序', width: 70 },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_item_actions' },
        title: '操作',
        width: 128,
      },
    ],
    id: 'sys_dict_item',
    name: '字典项列表',
  },
  {
    proxyConfig: {
      autoLoad: false,
      ajax: {
        query: ({ page }) => handleItemQueryApi(page),
      },
    },
  },
)

// Dict grid search
function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.isBuiltIn = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

// Dict add/edit/view
function handleAdd() {
  editingStatus.value = null
  dictForm.value = createDefaultDictForm()
  modalVisible.value = true
}

function handleEdit(row: DictListItemDto) {
  editingStatus.value = row.status
  dictForm.value = {
    basicId: row.basicId,
    dictCode: row.dictCode,
    dictDescription: row.dictDescription ?? null,
    dictName: row.dictName,
    dictType: row.dictType,
    sort: row.sort,
    status: row.status,
  }
  modalVisible.value = true
}

async function handleView(row: DictListItemDto) {
  currentDict.value = row
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null

  try {
    currentDetail.value = await dictManagementApi.detail(row.basicId)
    if (!currentDetail.value) {
      message.warning('未查询到字典详情')
    }
  }
  catch {
    message.error('加载字典详情失败')
  }
  finally {
    detailLoading.value = false
  }

  itemQueryParams.keyword = ''
  setTimeout(() => {
    xItemGrid.value?.commitProxy('reload')
  }, 200)
}

function validateDictForm() {
  if (!dictForm.value.dictName.trim()) {
    message.warning('请输入字典名称')
    return false
  }

  if (!dictForm.value.basicId && !dictForm.value.dictCode.trim()) {
    message.warning('请输入字典编码')
    return false
  }

  return true
}

async function handleSubmit() {
  if (!validateDictForm()) {
    return
  }

  submitLoading.value = true

  try {
    if (dictForm.value.basicId) {
      const updateInput: DictUpdateDto = {
        basicId: dictForm.value.basicId,
        dictDescription: dictForm.value.dictDescription,
        dictName: dictForm.value.dictName.trim(),
        dictType: dictForm.value.dictType.trim(),
        sort: dictForm.value.sort,
      }

      await dictManagementApi.update(updateInput)
      if (editingStatus.value !== dictForm.value.status) {
        await dictManagementApi.updateStatus({
          basicId: dictForm.value.basicId,
          remark: '前端更新字典状态',
          status: dictForm.value.status,
        })
      }
    }
    else {
      const createInput: DictCreateDto = {
        dictCode: dictForm.value.dictCode.trim(),
        dictDescription: dictForm.value.dictDescription,
        dictName: dictForm.value.dictName.trim(),
        dictType: dictForm.value.dictType.trim(),
        sort: dictForm.value.sort,
        status: dictForm.value.status,
      }

      await dictManagementApi.create(createInput)
    }

    message.success('保存成功')
    modalVisible.value = false
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: DictListItemDto) {
  await dictManagementApi.delete(row.basicId)
  message.success('删除成功')
  xGrid.value?.commitProxy('query')
}

async function handleToggleStatus(row: DictListItemDto) {
  await dictManagementApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? '前端停用字典' : '前端启用字典',
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success('状态已更新')
  xGrid.value?.commitProxy('query')
}

// Dict item search
function handleItemSearch() {
  xItemGrid.value?.commitProxy('reload')
}

// Dict item add/edit
function handleItemAdd() {
  itemEditingStatus.value = null
  itemForm.value = createDefaultDictItemForm()
  itemForm.value.dictId = currentDict.value?.basicId ?? ''
  itemModalVisible.value = true
}

function handleItemEdit(row: DictItemListItemDto) {
  itemEditingStatus.value = row.status
  itemForm.value = {
    basicId: row.basicId,
    dictId: row.dictId,
    isDefault: row.isDefault,
    itemCode: row.itemCode,
    itemDescription: row.itemDescription ?? null,
    itemName: row.itemName,
    itemValue: row.itemValue ?? null,
    parentId: row.parentId ?? null,
    sort: row.sort,
    status: row.status,
  }
  itemModalVisible.value = true
}

function validateDictItemForm() {
  if (!itemForm.value.itemName.trim()) {
    message.warning('请输入字典项名称')
    return false
  }

  if (!itemForm.value.basicId && !itemForm.value.itemCode.trim()) {
    message.warning('请输入字典项编码')
    return false
  }

  return true
}

async function handleItemSubmit() {
  if (!validateDictItemForm()) {
    return
  }

  itemSubmitLoading.value = true

  try {
    if (itemForm.value.basicId) {
      const updateInput: DictItemUpdateDto = {
        basicId: itemForm.value.basicId,
        isDefault: itemForm.value.isDefault,
        itemDescription: itemForm.value.itemDescription,
        itemName: itemForm.value.itemName.trim(),
        itemValue: itemForm.value.itemValue,
        parentId: itemForm.value.parentId,
        sort: itemForm.value.sort,
      }

      await dictManagementApi.itemUpdate(updateInput)
      if (itemEditingStatus.value !== itemForm.value.status) {
        await dictManagementApi.itemUpdateStatus({
          basicId: itemForm.value.basicId,
          remark: '前端更新字典项状态',
          status: itemForm.value.status,
        })
      }
    }
    else {
      const createInput: DictItemCreateDto = {
        dictId: itemForm.value.dictId,
        isDefault: itemForm.value.isDefault,
        itemCode: itemForm.value.itemCode.trim(),
        itemDescription: itemForm.value.itemDescription,
        itemName: itemForm.value.itemName.trim(),
        itemValue: itemForm.value.itemValue,
        parentId: itemForm.value.parentId,
        sort: itemForm.value.sort,
        status: itemForm.value.status,
      }

      await dictManagementApi.itemCreate(createInput)
    }

    message.success('保存成功')
    itemModalVisible.value = false
    xItemGrid.value?.commitProxy('query')
  }
  catch {
    message.error('保存失败')
  }
  finally {
    itemSubmitLoading.value = false
  }
}

async function handleItemDelete(row: DictItemListItemDto) {
  await dictManagementApi.itemDelete(row.basicId)
  message.success('删除成功')
  xItemGrid.value?.commitProxy('query')
}

async function handleItemToggleStatus(row: DictItemListItemDto) {
  await dictManagementApi.itemUpdateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? '前端停用字典项' : '前端启用字典项',
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success('状态已更新')
  xItemGrid.value?.commitProxy('query')
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索字典名称/编码/类型"
          style="width: 250px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.isBuiltIn"
          :options="builtInOptions"
          clearable
          placeholder="是否内置"
          style="width: 110px"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="statusOptions"
          clearable
          placeholder="状态"
          style="width: 100px"
        />
        <NButton size="small" type="primary" @click="handleSearch">
          <template #icon>
            <NIcon><Icon icon="lucide:search" /></NIcon>
          </template>
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          <template #icon>
            <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
          </template>
          重置
        </NButton>
      </div>
    </XSystemQueryPanel>

    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="tableOptions">
        <template #toolbar_buttons>
          <NButton size="small" type="primary" @click="handleAdd">
            <template #icon>
              <NIcon><Icon icon="lucide:plus" /></NIcon>
            </template>
            新增字典
          </NButton>
        </template>

        <template #col_builtin="{ row }">
          <NTag :type="row.isBuiltIn ? 'warning' : 'default'" round size="small">
            {{ row.isBuiltIn ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_status="{ row }">
          <NTag :type="row.status === EnableStatus.Enabled ? 'success' : 'error'" round size="small">
            {{ getOptionLabel(statusOptions, row.status) }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton aria-label="查看详情" circle quaternary size="small" @click="handleView(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:eye" /></NIcon>
              </template>
            </NButton>

            <NButton
              :disabled="!canMaintainDict(row)"
              aria-label="编辑"
              circle
              quaternary
              size="small"
              type="primary"
              @click="handleEdit(row)"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:pencil" /></NIcon>
              </template>
            </NButton>

            <NPopconfirm
              :disabled="!canMaintainDict(row)"
              @positive-click="handleToggleStatus(row)"
            >
              <template #trigger>
                <NButton :disabled="!canMaintainDict(row)" aria-label="停用或启用" circle quaternary size="small" type="warning">
                  <template #icon>
                    <NIcon>
                      <Icon :icon="row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:circle-check'" />
                    </NIcon>
                  </template>
                </NButton>
              </template>
              确认更新字典状态？
            </NPopconfirm>

            <NPopconfirm :disabled="!canMaintainDict(row)" @positive-click="handleDelete(row)">
              <template #trigger>
                <NButton :disabled="!canMaintainDict(row)" aria-label="删除" circle quaternary size="small" type="error">
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                </NButton>
              </template>
              确认删除该字典？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <!-- Dict Detail Drawer -->
    <NDrawer v-model:show="detailVisible" :width="800">
      <NDrawerContent :title="`字典详情 - ${currentDict?.dictName ?? ''}`" closable>
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="xh-detail-empty" description="暂无字典详情" />
          <NScrollbar v-else-if="currentDetail" style="max-height: calc(100vh - 120px)">
            <NDescriptions :column="2" bordered class="mb-4" size="small">
              <NDescriptionsItem label="字典名称">
                {{ currentDetail.dictName }}
              </NDescriptionsItem>
              <NDescriptionsItem label="字典编码">
                {{ currentDetail.dictCode }}
              </NDescriptionsItem>
              <NDescriptionsItem label="字典类型">
                {{ formatNullable(currentDetail.dictType) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="是否内置">
                {{ formatBoolean(currentDetail.isBuiltIn) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="排序">
                {{ currentDetail.sort }}
              </NDescriptionsItem>
              <NDescriptionsItem label="状态">
                {{ getOptionLabel(statusOptions, currentDetail.status) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="描述">
                {{ formatNullable(currentDetail.dictDescription) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="创建时间">
                {{ formatNullableDate(currentDetail.createdTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="修改时间">
                {{ formatNullableDate(currentDetail.modifiedTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="创建人">
                {{ formatNullable(currentDetail.createdBy) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="修改人">
                {{ formatNullable(currentDetail.modifiedBy) }}
              </NDescriptionsItem>
            </NDescriptions>

            <NSpace class="mb-3" vertical>
              <div class="flex gap-2 items-center">
                <vxe-input
                  v-model="itemQueryParams.keyword"
                  clearable
                  placeholder="搜索字典项"
                  style="width: 220px"
                  @keyup.enter="handleItemSearch"
                />
                <NButton size="small" type="primary" @click="handleItemSearch">
                  查询
                </NButton>
                <div class="flex-1" />
                <NButton size="small" type="primary" @click="handleItemAdd">
                  <template #icon>
                    <NIcon><Icon icon="lucide:plus" /></NIcon>
                  </template>
                  新增字典项
                </NButton>
              </div>
            </NSpace>
            <vxe-grid ref="xItemGrid" v-bind="itemTableOptions">
              <template #col_item_default="{ row }">
                <NTag :type="row.isDefault ? 'info' : 'default'" round size="small">
                  {{ row.isDefault ? '是' : '否' }}
                </NTag>
              </template>
              <template #col_item_status="{ row }">
                <NTag :type="row.status === EnableStatus.Enabled ? 'success' : 'error'" round size="small">
                  {{ getOptionLabel(statusOptions, row.status) }}
                </NTag>
              </template>
              <template #col_item_actions="{ row }">
                <NSpace size="small">
                  <NButton
                    aria-label="编辑"
                    circle
                    quaternary
                    size="small"
                    type="primary"
                    @click="handleItemEdit(row)"
                  >
                    <template #icon>
                      <NIcon><Icon icon="lucide:pencil" /></NIcon>
                    </template>
                  </NButton>

                  <NPopconfirm @positive-click="handleItemToggleStatus(row)">
                    <template #trigger>
                      <NButton aria-label="停用或启用" circle quaternary size="small" type="warning">
                        <template #icon>
                          <NIcon>
                            <Icon :icon="row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:circle-check'" />
                          </NIcon>
                        </template>
                      </NButton>
                    </template>
                    确认更新字典项状态？
                  </NPopconfirm>

                  <NPopconfirm @positive-click="handleItemDelete(row)">
                    <template #trigger>
                      <NButton aria-label="删除" circle quaternary size="small" type="error">
                        <template #icon>
                          <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                        </template>
                      </NButton>
                    </template>
                    确认删除该字典项？
                  </NPopconfirm>
                </NSpace>
              </template>
            </vxe-grid>
          </NScrollbar>
        </NSpin>
      </NDrawerContent>
    </NDrawer>

    <!-- Dict Edit/Create Modal -->
    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 680px; max-width: 92vw"
    >
      <NForm :model="dictForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="字典编码" path="dictCode">
          <NInput
            v-model:value="dictForm.dictCode"
            clearable
            :disabled="Boolean(dictForm.basicId)"
            placeholder="如: gender"
          />
        </NFormItem>
        <NFormItem label="字典名称" path="dictName">
          <NInput v-model:value="dictForm.dictName" clearable placeholder="请输入字典名称" />
        </NFormItem>
        <NFormItem label="字典类型" path="dictType">
          <NInput v-model:value="dictForm.dictType" clearable placeholder="请输入字典类型" />
        </NFormItem>
        <NFormItem label="描述" path="dictDescription">
          <NInput
            v-model:value="dictForm.dictDescription"
            clearable
            placeholder="请输入描述"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="dictForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem v-if="!dictForm.basicId" label="状态" path="status">
          <NSelect v-model:value="dictForm.status" :options="statusOptions" />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            保存
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <!-- Dict Item Edit/Create Modal -->
    <NModal
      v-model:show="itemModalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="itemModalTitle"
      preset="card"
      style="width: 600px; max-width: 92vw"
    >
      <NForm :model="itemForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="项编码" path="itemCode">
          <NInput
            v-model:value="itemForm.itemCode"
            clearable
            :disabled="Boolean(itemForm.basicId)"
            placeholder="如: male"
          />
        </NFormItem>
        <NFormItem label="项名称" path="itemName">
          <NInput v-model:value="itemForm.itemName" clearable placeholder="请输入字典项名称" />
        </NFormItem>
        <NFormItem label="项值" path="itemValue">
          <NInput v-model:value="itemForm.itemValue" clearable placeholder="请输入字典项值" />
        </NFormItem>
        <NFormItem label="描述" path="itemDescription">
          <NInput
            v-model:value="itemForm.itemDescription"
            clearable
            placeholder="请输入描述"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="是否默认" path="isDefault">
          <NSwitch v-model:value="itemForm.isDefault" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="itemForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem v-if="!itemForm.basicId" label="状态" path="status">
          <NSelect v-model:value="itemForm.status" :options="statusOptions" />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="itemModalVisible = false">
            取消
          </NButton>
          <NButton :loading="itemSubmitLoading" type="primary" @click="handleItemSubmit">
            保存
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}
</style>
