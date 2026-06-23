<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type {
  DictCreateDto,
  DictItemCreateDto,
  DictItemListItemDto,
  DictItemUpdateDto,
  DictListItemDto,
  DictUpdateDto,
} from '@/api'
import {
  NButton,
  NDataTable,
  NEmpty,
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
import { computed, h, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, dictManagementApi, EnableStatus } from '@/api'
import { Icon } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformDictPage' })

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

const { t } = useI18n()
const message = useMessage()
const statusOptions = STATUS_OPTIONS

const builtInOptions = computed(() => [
  { label: t('setting.dict.builtin'), value: 1 },
  { label: t('setting.dict.not_builtin'), value: 0 },
])

// 行内操作按钮：先阻止冒泡（避免触发整行选中），再执行动作
function stopAnd(action: () => void) {
  return (e: MouseEvent) => {
    e.stopPropagation()
    action()
  }
}

// ── 右侧：字典项列表状态（从，随左侧选中刷新；声明前置供主表选中逻辑引用） ──
const itemLoading = ref(false)
const itemList = ref<DictItemListItemDto[]>([])
const itemTotal = ref(0)
const itemPage = ref(1)
const itemPageSize = ref(20)
const itemQueryParams = reactive({ keyword: '' })
const checkedItemKeys = ref<Array<string | number>>([])

// ── 左侧：字典列表（主表，选中驱动右侧字典项刷新） ───────────────
const dictLoading = ref(false)
const dictList = ref<DictListItemDto[]>([])
const dictTotal = ref(0)
const dictPage = ref(1)
const dictPageSize = ref(20)
const dictQueryParams = reactive({
  keyword: '',
  status: null as EnableStatus | null,
  isBuiltIn: null as number | null,
})

const currentDict = ref<DictListItemDto | null>(null)
const checkedDictKeys = ref<Array<string | number>>([])

async function fetchDictData() {
  dictLoading.value = true
  try {
    const result = await dictManagementApi.page({
      ...createPageRequest({ page: { pageIndex: dictPage.value, pageSize: dictPageSize.value } }),
      isBuiltIn: dictQueryParams.isBuiltIn == null ? undefined : dictQueryParams.isBuiltIn === 1,
      keyword: dictQueryParams.keyword?.trim() || undefined,
      status: dictQueryParams.status ?? undefined,
    })
    dictList.value = result.items
    dictTotal.value = result.page.totalCount
    syncSelectionAfterDictLoad()
  }
  catch {
    message.error(t('setting.dict.query_dict_failed'))
    dictList.value = []
    dictTotal.value = 0
    currentDict.value = null
    itemList.value = []
    itemTotal.value = 0
  }
  finally {
    dictLoading.value = false
  }
}

// 列表刷新后维持选中：当前选中仍在则同步为最新数据，否则自动选中首条
function syncSelectionAfterDictLoad() {
  if (!dictList.value.length) {
    currentDict.value = null
    itemList.value = []
    itemTotal.value = 0
    return
  }
  const latest = currentDict.value
    ? dictList.value.find(d => d.basicId === currentDict.value?.basicId)
    : undefined
  if (latest) {
    currentDict.value = latest
    return
  }
  const first = dictList.value[0]
  if (first) {
    selectDict(first)
  }
}

function selectDict(row: DictListItemDto) {
  if (currentDict.value?.basicId === row.basicId) {
    return
  }
  currentDict.value = row
  itemQueryParams.keyword = ''
  itemPage.value = 1
  checkedItemKeys.value = []
  fetchItemData()
}

function handleDictSearch() {
  dictPage.value = 1
  fetchDictData()
}

function handleDictPageChange(page: number) {
  dictPage.value = page
  fetchDictData()
}

function reloadDict() {
  fetchDictData()
}

const dictColumns = computed<DataTableColumns<DictListItemDto>>(() => [
  { type: 'selection', width: 40 },
  {
    key: 'dictName',
    title: t('setting.dict.dict_name'),
    minWidth: 140,
    ellipsis: { tooltip: true },
    render: (row: DictListItemDto) =>
      h('div', { class: 'dict-name' }, [
        h('span', { class: 'dict-name__text' }, row.dictName),
        row.isBuiltIn
          ? h(NTag, { size: 'tiny', type: 'warning', round: true, bordered: false }, () => t('setting.dict.builtin'))
          : null,
      ]),
  },
  {
    key: 'dictCode',
    title: t('setting.dict.code'),
    minWidth: 130,
    ellipsis: { tooltip: true },
  },
  {
    key: 'dictType',
    title: t('setting.dict.type'),
    minWidth: 110,
    ellipsis: { tooltip: true },
  },
  {
    key: 'status',
    title: t('setting.dict.status'),
    width: 72,
    align: 'center',
    render: (row: DictListItemDto) =>
      h(NTag, { type: row.status === EnableStatus.Enabled ? 'success' : 'error', round: true, size: 'small', bordered: false }, () => getOptionLabel(statusOptions, row.status)),
  },
  {
    key: 'actions',
    title: t('setting.dict.actions'),
    width: 110,
    align: 'center',
    render: (row: DictListItemDto) =>
      h(NSpace, { size: 4, justify: 'center', wrap: false }, () => [
        h(NButton, { ariaLabel: t('common.actions.edit'), circle: true, quaternary: true, size: 'small', type: 'primary', onClick: stopAnd(() => handleEdit(row)) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:pencil' })) }),
        h(NPopconfirm, { onPositiveClick: () => handleToggleStatus(row) }, {
          trigger: () => h(NButton, { ariaLabel: t('setting.dict.confirm_toggle_dict'), circle: true, quaternary: true, size: 'small', type: 'warning', onClick: (e: MouseEvent) => e.stopPropagation() }, { icon: () => h(NIcon, null, () => h(Icon, { icon: row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:circle-check' })) }),
          default: () => t('setting.dict.confirm_toggle_dict'),
        }),
        h(NPopconfirm, { onPositiveClick: () => handleDelete(row) }, {
          trigger: () => h(NButton, { ariaLabel: t('common.actions.delete'), circle: true, quaternary: true, size: 'small', type: 'error', onClick: (e: MouseEvent) => e.stopPropagation() }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:trash-2' })) }),
          default: () => t('setting.dict.confirm_delete_dict'),
        }),
      ]),
  },
])

function dictRowProps(row: DictListItemDto) {
  return {
    class: currentDict.value?.basicId === row.basicId ? 'dict-row--active' : '',
    style: 'cursor: pointer;',
    onClick: (e: MouseEvent) => {
      // 点击多选框列不触发整行选中（避免误切当前字典）
      if ((e.target as HTMLElement | null)?.closest('.n-data-table-td--selection, .n-checkbox')) {
        return
      }
      selectDict(row)
    },
  }
}

// ── 右侧：字典项列表（从表，随左侧选中刷新） ────────────────────
async function fetchItemData() {
  if (!currentDict.value) {
    itemList.value = []
    itemTotal.value = 0
    return
  }
  itemLoading.value = true
  try {
    const result = await dictManagementApi.itemPage({
      ...createPageRequest({
        page: {
          pageIndex: itemPage.value,
          pageSize: itemPageSize.value,
        },
      }),
      dictId: currentDict.value.basicId,
      keyword: itemQueryParams.keyword?.trim() || undefined,
    })
    itemList.value = result.items
    itemTotal.value = result.page.totalCount
  }
  catch {
    message.error(t('setting.dict.query_item_failed'))
    itemList.value = []
    itemTotal.value = 0
  }
  finally {
    itemLoading.value = false
  }
}

const itemColumns = computed<DataTableColumns<DictItemListItemDto>>(() => [
  { type: 'selection' },
  {
    key: 'itemName',
    title: t('setting.dict.item_name'),
    minWidth: 130,
    ellipsis: { tooltip: true },
  },
  {
    key: 'itemCode',
    title: t('setting.dict.code'),
    minWidth: 130,
    ellipsis: { tooltip: true },
  },
  {
    key: 'itemValue',
    title: t('setting.dict.item_value'),
    minWidth: 100,
    ellipsis: { tooltip: true },
  },
  {
    key: 'isDefault',
    title: t('setting.dict.default'),
    width: 70,
    render: (row: DictItemListItemDto) =>
      h(NTag, { type: row.isDefault ? 'info' : 'default', round: true, size: 'small' }, () => (row.isDefault ? t('common.statuses.yes') : t('common.statuses.no'))),
  },
  {
    key: 'status',
    title: t('setting.dict.status'),
    width: 80,
    render: (row: DictItemListItemDto) =>
      h(NTag, { type: row.status === EnableStatus.Enabled ? 'success' : 'error', round: true, size: 'small' }, () => getOptionLabel(statusOptions, row.status)),
  },
  {
    key: 'sort',
    title: t('setting.dict.sort'),
    width: 70,
  },
  {
    key: 'actions',
    title: t('setting.dict.actions'),
    width: 128,
    render: (row: DictItemListItemDto) =>
      h(NSpace, { size: 'small' }, () => [
        h(NButton, { ariaLabel: t('common.actions.edit'), circle: true, quaternary: true, size: 'small', type: 'primary', onClick: () => handleItemEdit(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:pencil' })) }),
        h(NPopconfirm, { onPositiveClick: () => handleItemToggleStatus(row) }, {
          trigger: () => h(NButton, { ariaLabel: t('setting.dict.confirm_toggle_item'), circle: true, quaternary: true, size: 'small', type: 'warning' }, { icon: () => h(NIcon, null, () => h(Icon, { icon: row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:circle-check' })) }),
          default: () => t('setting.dict.confirm_toggle_item'),
        }),
        h(NPopconfirm, { onPositiveClick: () => handleItemDelete(row) }, {
          trigger: () => h(NButton, { ariaLabel: t('common.actions.delete'), circle: true, quaternary: true, size: 'small', type: 'error' }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:trash-2' })) }),
          default: () => t('setting.dict.confirm_delete_item'),
        }),
      ]),
  },
])

function handleItemSearch() {
  itemPage.value = 1
  fetchItemData()
}

function handleItemPageChange(page: number) {
  itemPage.value = page
  fetchItemData()
}

function handleItemPageSizeChange(pageSize: number) {
  itemPageSize.value = pageSize
  itemPage.value = 1
  fetchItemData()
}

// ── 字典 表单/弹窗 ──────────────────────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const dictForm = ref<DictFormModel>(createDefaultDictForm())
const modalTitle = computed(() => (dictForm.value.basicId ? t('setting.dict.edit_dict_title') : t('setting.dict.add_dict_title')))

const itemModalVisible = ref(false)
const itemSubmitLoading = ref(false)
const itemEditingStatus = ref<EnableStatus | null>(null)
const itemForm = ref<DictItemFormModel>(createDefaultDictItemForm())
const itemModalTitle = computed(() => (itemForm.value.basicId ? t('setting.dict.edit_item_title') : t('setting.dict.add_item_title')))

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

function validateDictForm() {
  if (!dictForm.value.dictName.trim()) {
    message.warning(t('setting.dict.validate_dict_name'))
    return false
  }

  if (!dictForm.value.basicId && !dictForm.value.dictCode.trim()) {
    message.warning(t('setting.dict.validate_dict_code'))
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
          remark: t('setting.dict.dict_status_update_remark'),
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

    message.success(t('common.messages.save_success'))
    modalVisible.value = false
    reloadDict()
  }
  catch {
    message.error(t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: DictListItemDto) {
  await dictManagementApi.delete(row.basicId)
  message.success(t('common.messages.delete_success'))
  reloadDict()
}

async function handleToggleStatus(row: DictListItemDto) {
  await dictManagementApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? t('setting.dict.dict_disable_remark') : t('setting.dict.dict_enable_remark'),
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success(t('common.messages.status_updated'))
  reloadDict()
}

// ── 字典 批量操作 ───────────────────────────────────────────────
async function handleBatchDeleteDict() {
  const ids = [...checkedDictKeys.value]
  if (!ids.length) {
    return
  }
  try {
    await Promise.all(ids.map(id => dictManagementApi.delete(String(id))))
    message.success(t('setting.dict.batch_deleted_dict', { count: ids.length }))
  }
  catch {
    message.error(t('common.messages.batch_delete_failed'))
  }
  finally {
    checkedDictKeys.value = []
    reloadDict()
  }
}

async function handleBatchToggleDict(enable: boolean) {
  const ids = [...checkedDictKeys.value]
  if (!ids.length) {
    return
  }
  try {
    await Promise.all(ids.map(id => dictManagementApi.updateStatus({
      basicId: String(id),
      remark: enable ? t('setting.dict.batch_enable_dict_remark') : t('setting.dict.batch_disable_dict_remark'),
      status: enable ? EnableStatus.Enabled : EnableStatus.Disabled,
    })))
    message.success(t('common.messages.status_updated'))
  }
  catch {
    message.error(t('common.messages.batch_action_failed'))
  }
  finally {
    checkedDictKeys.value = []
    reloadDict()
  }
}

// ── 字典项 表单/弹窗 ────────────────────────────────────────────
function handleItemAdd() {
  if (!currentDict.value) {
    return
  }
  itemEditingStatus.value = null
  itemForm.value = createDefaultDictItemForm()
  itemForm.value.dictId = currentDict.value.basicId
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
    message.warning(t('setting.dict.validate_item_name'))
    return false
  }

  if (!itemForm.value.basicId && !itemForm.value.itemCode.trim()) {
    message.warning(t('setting.dict.validate_item_code'))
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
          remark: t('setting.dict.item_status_update_remark'),
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

    message.success(t('common.messages.save_success'))
    itemModalVisible.value = false
    fetchItemData()
  }
  catch {
    message.error(t('common.messages.save_failed'))
  }
  finally {
    itemSubmitLoading.value = false
  }
}

async function handleItemDelete(row: DictItemListItemDto) {
  await dictManagementApi.itemDelete(row.basicId)
  message.success(t('common.messages.delete_success'))
  fetchItemData()
}

async function handleItemToggleStatus(row: DictItemListItemDto) {
  await dictManagementApi.itemUpdateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? t('setting.dict.item_disable_remark') : t('setting.dict.item_enable_remark'),
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success(t('common.messages.status_updated'))
  fetchItemData()
}

// ── 字典项 批量操作 ─────────────────────────────────────────────
async function handleBatchDeleteItem() {
  const ids = [...checkedItemKeys.value]
  if (!ids.length) {
    return
  }
  try {
    await Promise.all(ids.map(id => dictManagementApi.itemDelete(String(id))))
    message.success(t('setting.dict.batch_deleted_item', { count: ids.length }))
  }
  catch {
    message.error(t('common.messages.batch_delete_failed'))
  }
  finally {
    checkedItemKeys.value = []
    fetchItemData()
  }
}

async function handleBatchToggleItem(enable: boolean) {
  const ids = [...checkedItemKeys.value]
  if (!ids.length) {
    return
  }
  try {
    await Promise.all(ids.map(id => dictManagementApi.itemUpdateStatus({
      basicId: String(id),
      remark: enable ? t('setting.dict.batch_enable_item_remark') : t('setting.dict.batch_disable_item_remark'),
      status: enable ? EnableStatus.Enabled : EnableStatus.Disabled,
    })))
    message.success(t('common.messages.status_updated'))
  }
  catch {
    message.error(t('common.messages.batch_action_failed'))
  }
  finally {
    checkedItemKeys.value = []
    fetchItemData()
  }
}

onMounted(fetchDictData)
</script>

<template>
  <div class="dict-mgmt">
    <!-- 左侧：字典列表（主） -->
    <section class="pane pane--master">
      <header class="pane__head">
        <div class="pane__title-row">
          <span class="pane__title">{{ t('setting.dict.dict_list') }}</span>
          <span class="pane__count">{{ dictTotal }}</span>
        </div>
        <NButton size="small" type="primary" @click="handleAdd">
          <template #icon>
            <NIcon><Icon icon="lucide:plus" /></NIcon>
          </template>
          {{ t('setting.dict.add_dict') }}
        </NButton>
      </header>

      <div class="pane__filters">
        <NInput
          v-model:value="dictQueryParams.keyword"
          class="pane__kw"
          clearable
          :placeholder="t('setting.dict.dict_search_placeholder')"
          size="small"
          @keyup.enter="handleDictSearch"
          @clear="handleDictSearch"
        />
        <NSelect
          v-model:value="dictQueryParams.status"
          class="pane__filter-select"
          clearable
          :options="statusOptions"
          :placeholder="t('setting.dict.status_placeholder')"
          size="small"
          @update:value="handleDictSearch"
        />
        <NSelect
          v-model:value="dictQueryParams.isBuiltIn"
          class="pane__filter-select"
          clearable
          :options="builtInOptions"
          :placeholder="t('setting.dict.builtin_placeholder')"
          size="small"
          @update:value="handleDictSearch"
        />
        <NButton class="pane__search" size="small" type="primary" @click="handleDictSearch">
          {{ t('common.actions.search') }}
        </NButton>
      </div>

      <div class="pane__body">
        <NDataTable
          v-model:checked-row-keys="checkedDictKeys"
          class="pane__table"
          flex-height
          :columns="dictColumns"
          :data="dictList"
          :loading="dictLoading"
          :row-key="(row: DictListItemDto) => row.basicId"
          :row-props="dictRowProps"
          size="small"
        />
      </div>

      <footer class="pane__foot">
        <div class="pane__foot-left">
          <template v-if="checkedDictKeys.length">
            <span class="pane__sel">{{ t('setting.dict.selected', { count: checkedDictKeys.length }) }}</span>
            <NButton size="tiny" @click="handleBatchToggleDict(true)">
              {{ t('common.actions.enable') }}
            </NButton>
            <NButton size="tiny" @click="handleBatchToggleDict(false)">
              {{ t('common.actions.disable') }}
            </NButton>
            <NPopconfirm @positive-click="handleBatchDeleteDict">
              <template #trigger>
                <NButton size="tiny" type="error">
                  {{ t('common.actions.delete') }}
                </NButton>
              </template>
              {{ t('setting.dict.confirm_batch_delete_dict', { count: checkedDictKeys.length }) }}
            </NPopconfirm>
          </template>
        </div>
        <NPagination
          v-model:page="dictPage"
          :item-count="dictTotal"
          :page-size="dictPageSize"
          :page-slot="5"
          size="small"
          @update:page="handleDictPageChange"
        />
      </footer>
    </section>

    <!-- 右侧：字典项列表（从，随左侧选中刷新） -->
    <section class="pane pane--detail">
      <header class="pane__head">
        <div class="pane__title-row">
          <span class="pane__title">{{ currentDict ? currentDict.dictName : t('setting.dict.no_dict_selected') }}</span>
          <span v-if="currentDict" class="pane__count">{{ t('setting.dict.item_count', { count: itemTotal }) }}</span>
        </div>
        <NButton size="small" type="primary" :disabled="!currentDict" @click="handleItemAdd">
          <template #icon>
            <NIcon><Icon icon="lucide:plus" /></NIcon>
          </template>
          {{ t('setting.dict.add_item') }}
        </NButton>
      </header>

      <div class="pane__filters">
        <NInput
          v-model:value="itemQueryParams.keyword"
          class="pane__kw"
          clearable
          :disabled="!currentDict"
          :placeholder="t('setting.dict.item_search_placeholder')"
          size="small"
          @keyup.enter="handleItemSearch"
          @clear="handleItemSearch"
        />
        <NButton class="pane__search" size="small" type="primary" :disabled="!currentDict" @click="handleItemSearch">
          {{ t('common.actions.search') }}
        </NButton>
      </div>

      <div class="pane__body">
        <NEmpty v-if="!currentDict" class="pane__empty" :description="t('setting.dict.select_dict_hint')">
          <template #icon>
            <NIcon><Icon icon="lucide:list-tree" /></NIcon>
          </template>
        </NEmpty>
        <NDataTable
          v-else
          v-model:checked-row-keys="checkedItemKeys"
          class="pane__table"
          flex-height
          :columns="itemColumns"
          :data="itemList"
          :loading="itemLoading"
          :row-key="(row: DictItemListItemDto) => row.basicId"
          :scroll-x="640"
          size="small"
          striped
        />
      </div>

      <footer v-if="currentDict" class="pane__foot">
        <div class="pane__foot-left">
          <template v-if="checkedItemKeys.length">
            <span class="pane__sel">{{ t('setting.dict.selected', { count: checkedItemKeys.length }) }}</span>
            <NButton size="tiny" @click="handleBatchToggleItem(true)">
              {{ t('common.actions.enable') }}
            </NButton>
            <NButton size="tiny" @click="handleBatchToggleItem(false)">
              {{ t('common.actions.disable') }}
            </NButton>
            <NPopconfirm @positive-click="handleBatchDeleteItem">
              <template #trigger>
                <NButton size="tiny" type="error">
                  {{ t('common.actions.delete') }}
                </NButton>
              </template>
              {{ t('setting.dict.confirm_batch_delete_item', { count: checkedItemKeys.length }) }}
            </NPopconfirm>
          </template>
        </div>
        <NPagination
          v-model:page="itemPage"
          v-model:page-size="itemPageSize"
          :item-count="itemTotal"
          :page-sizes="[10, 20, 50, 100]"
          show-size-picker
          @update:page="handleItemPageChange"
          @update:page-size="handleItemPageSizeChange"
        />
      </footer>
    </section>

    <!-- 字典 新增/编辑 -->
    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 680px; max-width: 92vw"
    >
      <NForm :model="dictForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem :label="t('setting.dict.dict_code')" path="dictCode">
          <NInput
            v-model:value="dictForm.dictCode"
            clearable
            :disabled="Boolean(dictForm.basicId)"
            :placeholder="t('setting.dict.dict_code_placeholder')"
          />
        </NFormItem>
        <NFormItem :label="t('setting.dict.dict_name')" path="dictName">
          <NInput v-model:value="dictForm.dictName" clearable :placeholder="t('setting.dict.dict_name_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('setting.dict.dict_type')" path="dictType">
          <NInput v-model:value="dictForm.dictType" clearable :placeholder="t('setting.dict.dict_type_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('setting.dict.description')" path="dictDescription">
          <NInput
            v-model:value="dictForm.dictDescription"
            clearable
            :placeholder="t('setting.dict.description_placeholder')"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
        <NFormItem :label="t('setting.dict.sort')" path="sort">
          <NInputNumber v-model:value="dictForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem v-if="!dictForm.basicId" :label="t('setting.dict.status')" path="status">
          <NSelect v-model:value="dictForm.status" :options="statusOptions" />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            {{ t('common.actions.cancel') }}
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            {{ t('common.actions.save') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <!-- 字典项 新增/编辑 -->
    <NModal
      v-model:show="itemModalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="itemModalTitle"
      preset="card"
      style="width: 600px; max-width: 92vw"
    >
      <NForm :model="itemForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem :label="t('setting.dict.item_code')" path="itemCode">
          <NInput
            v-model:value="itemForm.itemCode"
            clearable
            :disabled="Boolean(itemForm.basicId)"
            :placeholder="t('setting.dict.item_code_placeholder')"
          />
        </NFormItem>
        <NFormItem :label="t('setting.dict.item_name_label')" path="itemName">
          <NInput v-model:value="itemForm.itemName" clearable :placeholder="t('setting.dict.item_name_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('setting.dict.item_value_label')" path="itemValue">
          <NInput v-model:value="itemForm.itemValue" clearable :placeholder="t('setting.dict.item_value_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('setting.dict.description')" path="itemDescription">
          <NInput
            v-model:value="itemForm.itemDescription"
            clearable
            :placeholder="t('setting.dict.description_placeholder')"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
        <NFormItem :label="t('setting.dict.is_default')" path="isDefault">
          <NSwitch v-model:value="itemForm.isDefault" />
        </NFormItem>
        <NFormItem :label="t('setting.dict.sort')" path="sort">
          <NInputNumber v-model:value="itemForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem v-if="!itemForm.basicId" :label="t('setting.dict.status')" path="status">
          <NSelect v-model:value="itemForm.status" :options="statusOptions" />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="itemModalVisible = false">
            {{ t('common.actions.cancel') }}
          </NButton>
          <NButton :loading="itemSubmitLoading" type="primary" @click="handleItemSubmit">
            {{ t('common.actions.save') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>

<style scoped>
.dict-mgmt {
  display: flex;
  gap: 12px;
  height: 100%;
  padding: 12px;
  box-sizing: border-box;
  overflow: hidden;
}

.pane {
  display: flex;
  flex-direction: column;
  min-height: 0;
  background: hsl(var(--card));
  border: 1px solid hsl(var(--border));
  border-radius: 10px;
  overflow: hidden;
}

/* 左右等宽：两栏各占一半 */
.pane--master,
.pane--detail {
  flex: 1 1 0;
  min-width: 0;
}

.pane__head {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  min-height: 52px;
  padding: 8px 16px;
  border-bottom: 1px solid hsl(var(--border));
}

/* 标题区：单行，标题/计数同行排列 */
.pane__title-row {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.pane__title {
  font-size: 16px;
  font-weight: 600;
  line-height: 1.25;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 字典编码：等宽小药丸，与名称区分但不抢眼 */
/* 计数：轻量徽标 */
.pane__count {
  flex-shrink: 0;
  font-size: 12px;
  font-weight: 500;
  color: var(--text-secondary);
}

.pane__filters {
  display: flex;
  flex-shrink: 0;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
  border-bottom: 1px solid hsl(var(--border));
}

/* 搜索条件：固定宽度，靠左排列 */
.pane__kw {
  width: 240px;
}

.pane__filter-select {
  width: 110px;
  flex-shrink: 0;
}

/* 查询按钮：推到筛选区最右侧 */
.pane__search {
  margin-left: auto;
}

.pane__body {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  padding: 8px 14px;
}

/* flex-height 表格：占满 body 中段并在内部 tbody 滚动，不撑破容器 */
.pane__table {
  flex: 1;
  min-height: 0;
}

.pane__empty {
  padding: 64px 0;
}

.pane__foot {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 10px 14px;
  border-top: 1px solid hsl(var(--border));
}

/* 批量操作条：选中行后在底部左侧出现 */
.pane__foot-left {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.pane__sel {
  font-size: 12px;
  font-weight: 500;
  color: var(--text-secondary);
}

/* 主表行：选中态用主色淡染（不使用侧边色条），名称转主色，选中一目了然 */
.pane :deep(.dict-row--active > td) {
  background-color: hsl(var(--primary) / 0.08);
}

.pane :deep(.dict-row--active) .dict-name__text {
  color: hsl(var(--primary));
}

.dict-name {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
}

.dict-name__text {
  font-weight: 500;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

@media (max-width: 1024px) {
  /* 窄屏：上下堆叠，整页可滚动；每栏表格给定高度以便内部滚动 */
  .dict-mgmt {
    flex-direction: column;
    height: auto;
    min-height: 100%;
    overflow: visible;
  }

  .pane--master,
  .pane--detail {
    flex: none;
  }

  .pane__body {
    flex: none;
    height: 56vh;
  }
}
</style>
