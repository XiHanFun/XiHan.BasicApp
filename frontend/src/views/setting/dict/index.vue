<script setup lang="ts">
import type {
  DictCreateDto,
  DictDetailDto,
  DictItemCreateDto,
  DictItemDetailDto,
  DictItemListItemDto,
  DictItemUpdateDto,
  DictListItemDto,
  DictUpdateDto,
} from '@/api'
import type { XDataTableColumn } from '~/components'
import { XhButton, XhEmptyStateDescription, XhEmptyStateIcon, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFlex, XhFormFieldGroup, XhFormRoot, XhPopconfirmCancelTrigger, XhPopconfirmConfirmTrigger, XhPopconfirmContent, XhPopconfirmDescription, XhPopconfirmPositioner, XhPopconfirmRoot, XhPopconfirmTrigger, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, onMounted, reactive, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, dictManagementApi, EnableStatus } from '@/api'
import { STATUS_OPTIONS } from '@/constants'
import { Icon, SchemaPagination, XDataTable, XEditModal, XInput, XNumberInput, XPopconfirm, XSelect } from '~/components'
import { toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformDictPage' })

interface DictFormModel {
  basicId?: string
  dictCode: string
  dictDescription?: string | null
  dictName: string
  dictType: string
  // 无表单项，仅按详情原样回传，避免编辑时被清空
  remark?: string | null
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
  // 无表单项，仅按详情原样回传，避免编辑时被清空
  metadata?: string | null
  parentId?: string | null
  remark?: string | null
  sort: number
  status: EnableStatus
}

const { t } = useI18n()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()
const itemModalFormId = useId()
const statusEnumOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)

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
  catch (error) {
    toast.error((error as Error)?.message || t('setting.dict.query_dict_failed'))
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

function handleDictPageSizeChange(pageSize: number) {
  dictPageSize.value = pageSize
  dictPage.value = 1
  fetchDictData()
}

function reloadDict() {
  fetchDictData()
}

const dictColumns = computed<XDataTableColumn<DictListItemDto>[]>(() => [
  {
    key: 'dictName',
    title: t('setting.dict.dict_name'),
    minWidth: 140,
    ellipsis: true,
    render: (row: DictListItemDto) =>
      h('div', { class: 'dict-name' }, [
        h('span', { class: 'dict-name__text' }, row.dictName),
        row.isBuiltIn
          ? h(XhTagRoot, { variant: 'outline', tone: 'warning' }, () => h(XhTagLabel, () => t('setting.dict.builtin')))
          : null,
      ]),
  },
  {
    key: 'dictCode',
    title: t('setting.dict.code'),
    minWidth: 130,
    ellipsis: true,
  },
  {
    key: 'dictType',
    title: t('setting.dict.type'),
    minWidth: 110,
    ellipsis: true,
  },
  {
    key: 'status',
    title: t('setting.dict.status'),
    width: 72,
    align: 'center',
    render: (row: DictListItemDto) =>
      h(XhTagRoot, { variant: 'outline', tone: row.status === EnableStatus.Enabled ? 'success' : 'danger' }, () => h(XhTagLabel, () => getOptionLabel(statusEnumOptions.value, row.status))),
  },
  {
    key: 'actions',
    title: t('setting.dict.actions'),
    width: 132,
    align: 'center',
    render: (row: DictListItemDto) =>
      h(XhFlex, { gap: 'xs', justify: 'center', wrap: false }, () => [
        h(XhButton, { iconOnly: true, ariaLabel: t('common.actions.edit'), variant: 'ghost', size: 'sm', tone: 'brand', onClick: stopAnd(() => { void handleEdit(row) }) }, () => h(Icon, { icon: 'lucide:pencil' })),
        h(XPopconfirm, { onConfirm: () => handleToggleStatus(row) }, {
          trigger: () => h(XhButton, { iconOnly: true, ariaLabel: t('setting.dict.confirm_toggle_dict'), variant: 'ghost', size: 'sm', tone: 'warning', onClick: (e: MouseEvent) => e.stopPropagation() }, () => h(Icon, { icon: row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:circle-check' })),
          default: () => t('setting.dict.confirm_toggle_dict'),
        }),
        h(XPopconfirm, { onConfirm: () => handleDelete(row) }, {
          trigger: () => h(XhButton, { iconOnly: true, ariaLabel: t('common.actions.delete'), variant: 'ghost', size: 'sm', tone: 'danger', onClick: (e: MouseEvent) => e.stopPropagation() }, () => h(Icon, { icon: 'lucide:trash-2' })),
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
      if ((e.target as HTMLElement | null)?.closest('[data-scope="table"][data-part="row-select-trigger"], [data-scope="checkbox"]')) {
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
  catch (error) {
    toast.error((error as Error)?.message || t('setting.dict.query_item_failed'))
    itemList.value = []
    itemTotal.value = 0
  }
  finally {
    itemLoading.value = false
  }
}

const itemColumns = computed<XDataTableColumn<DictItemListItemDto>[]>(() => [
  {
    key: 'itemName',
    title: t('setting.dict.item_name'),
    minWidth: 130,
    ellipsis: true,
  },
  {
    key: 'itemCode',
    title: t('setting.dict.code'),
    minWidth: 130,
    ellipsis: true,
  },
  {
    key: 'itemValue',
    title: t('setting.dict.item_value'),
    minWidth: 100,
    ellipsis: true,
  },
  {
    key: 'isDefault',
    title: t('setting.dict.default'),
    width: 70,
    render: (row: DictItemListItemDto) =>
      h(XhTagRoot, { variant: 'outline', tone: row.isDefault ? 'info' : 'neutral' }, () => h(XhTagLabel, () => (row.isDefault ? t('common.statuses.yes') : t('common.statuses.no')))),
  },
  {
    key: 'status',
    title: t('setting.dict.status'),
    width: 80,
    render: (row: DictItemListItemDto) =>
      h(XhTagRoot, { variant: 'outline', tone: row.status === EnableStatus.Enabled ? 'success' : 'danger' }, () => h(XhTagLabel, () => getOptionLabel(statusEnumOptions.value, row.status))),
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
      h(XhFlex, { gap: 'sm' }, () => [
        h(XhButton, { iconOnly: true, ariaLabel: t('common.actions.edit'), variant: 'ghost', size: 'sm', tone: 'brand', onClick: () => { void handleItemEdit(row) } }, () => h(Icon, { icon: 'lucide:pencil' })),
        h(XPopconfirm, { onConfirm: () => handleItemToggleStatus(row) }, {
          trigger: () => h(XhButton, { iconOnly: true, ariaLabel: t('setting.dict.confirm_toggle_item'), variant: 'ghost', size: 'sm', tone: 'warning' }, () => h(Icon, { icon: row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:circle-check' })),
          default: () => t('setting.dict.confirm_toggle_item'),
        }),
        h(XPopconfirm, { onConfirm: () => handleItemDelete(row) }, {
          trigger: () => h(XhButton, { iconOnly: true, ariaLabel: t('common.actions.delete'), variant: 'ghost', size: 'sm', tone: 'danger' }, () => h(Icon, { icon: 'lucide:trash-2' })),
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

async function handleEdit(row: DictListItemDto) {
  editingStatus.value = row.status
  // 列表行不含备注，取详情回填；否则保存时会把备注覆盖为空
  let detail: DictDetailDto | null = null
  try {
    detail = await dictManagementApi.detail(row.basicId)
  }
  catch {
    detail = null
  }
  dictForm.value = {
    basicId: row.basicId,
    dictCode: detail?.dictCode ?? row.dictCode,
    dictDescription: detail?.dictDescription ?? row.dictDescription ?? null,
    dictName: detail?.dictName ?? row.dictName,
    dictType: detail?.dictType ?? row.dictType,
    remark: detail?.remark ?? null,
    sort: detail?.sort ?? row.sort,
    status: detail?.status ?? row.status,
  }
  modalVisible.value = true
}

function validateDictForm() {
  if (!dictForm.value.dictName.trim()) {
    toast.warning(t('setting.dict.validate_dict_name'))
    return false
  }

  if (!dictForm.value.basicId && !dictForm.value.dictCode.trim()) {
    toast.warning(t('setting.dict.validate_dict_code'))
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
        remark: dictForm.value.remark,
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

    toast.success(t('common.messages.save_success'))
    modalVisible.value = false
    reloadDict()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: DictListItemDto) {
  await dictManagementApi.delete(row.basicId)
  toast.success(t('common.messages.delete_success'))
  reloadDict()
}

async function handleToggleStatus(row: DictListItemDto) {
  await dictManagementApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? t('setting.dict.dict_disable_remark') : t('setting.dict.dict_enable_remark'),
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  toast.success(t('common.messages.status_updated'))
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
    toast.success(t('setting.dict.batch_deleted_dict', { count: ids.length }))
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.batch_delete_failed'))
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
    toast.success(t('common.messages.status_updated'))
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.batch_action_failed'))
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

async function handleItemEdit(row: DictItemListItemDto) {
  itemEditingStatus.value = row.status
  // 列表行不含元数据与备注，取详情回填；否则保存时会把两者覆盖为空
  let detail: DictItemDetailDto | null = null
  try {
    detail = await dictManagementApi.itemDetail(row.basicId)
  }
  catch {
    detail = null
  }
  itemForm.value = {
    basicId: row.basicId,
    dictId: detail?.dictId ?? row.dictId,
    isDefault: detail?.isDefault ?? row.isDefault,
    itemCode: detail?.itemCode ?? row.itemCode,
    itemDescription: detail?.itemDescription ?? row.itemDescription ?? null,
    itemName: detail?.itemName ?? row.itemName,
    itemValue: detail?.itemValue ?? row.itemValue ?? null,
    metadata: detail?.metadata ?? null,
    parentId: detail?.parentId ?? row.parentId ?? null,
    remark: detail?.remark ?? null,
    sort: detail?.sort ?? row.sort,
    status: detail?.status ?? row.status,
  }
  itemModalVisible.value = true
}

function validateDictItemForm() {
  if (!itemForm.value.itemName.trim()) {
    toast.warning(t('setting.dict.validate_item_name'))
    return false
  }

  if (!itemForm.value.basicId && !itemForm.value.itemCode.trim()) {
    toast.warning(t('setting.dict.validate_item_code'))
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
        metadata: itemForm.value.metadata,
        parentId: itemForm.value.parentId,
        remark: itemForm.value.remark,
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

    toast.success(t('common.messages.save_success'))
    itemModalVisible.value = false
    fetchItemData()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    itemSubmitLoading.value = false
  }
}

async function handleItemDelete(row: DictItemListItemDto) {
  await dictManagementApi.itemDelete(row.basicId)
  toast.success(t('common.messages.delete_success'))
  fetchItemData()
}

async function handleItemToggleStatus(row: DictItemListItemDto) {
  await dictManagementApi.itemUpdateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? t('setting.dict.item_disable_remark') : t('setting.dict.item_enable_remark'),
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  toast.success(t('common.messages.status_updated'))
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
    toast.success(t('setting.dict.batch_deleted_item', { count: ids.length }))
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.batch_delete_failed'))
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
    toast.success(t('common.messages.status_updated'))
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.batch_action_failed'))
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
        <XhButton size="sm" tone="brand" @click="handleAdd">
          <span><Icon icon="lucide:plus" /></span>
          {{ t('setting.dict.add_dict') }}
        </XhButton>
      </header>

      <div class="pane__filters">
        <XInput
          v-model:value="dictQueryParams.keyword"
          class="pane__kw"
          clearable
          :placeholder="t('setting.dict.dict_search_placeholder')"
          size="sm"
          @keyup.enter="handleDictSearch"
          @clear="handleDictSearch"
        />
        <XSelect
          v-model:value="dictQueryParams.status"
          class="pane__filter-select"
          clearable
          :options="statusEnumOptions"
          :placeholder="t('setting.dict.status_placeholder')"
          size="sm"
          @update:value="handleDictSearch"
        />
        <XSelect
          v-model:value="dictQueryParams.isBuiltIn"
          class="pane__filter-select"
          clearable
          :options="builtInOptions"
          :placeholder="t('setting.dict.builtin_placeholder')"
          size="sm"
          @update:value="handleDictSearch"
        />
        <XhButton class="pane__search" size="sm" tone="brand" @click="handleDictSearch">
          {{ t('common.actions.search') }}
        </XhButton>
      </div>

      <div class="pane__body">
        <XDataTable
          selectable
          :checked-row-keys="checkedDictKeys.map(String)" class="pane__table"
          :columns="dictColumns"
          :data="dictList"
          :loading="dictLoading"
          :row-key="(row: DictListItemDto) => row.basicId"
          :row-props="dictRowProps"
          size="sm"
          @update:checked-row-keys="(keys: string[]) => (checkedDictKeys = keys)"
        />
      </div>

      <footer class="pane__foot">
        <div class="pane__foot-left">
          <template v-if="checkedDictKeys.length">
            <span class="pane__sel">{{ t('setting.dict.selected', { count: checkedDictKeys.length }) }}</span>
            <XhButton size="sm" @click="handleBatchToggleDict(true)">
              {{ t('common.actions.enable') }}
            </XhButton>
            <XhButton size="sm" @click="handleBatchToggleDict(false)">
              {{ t('common.actions.disable') }}
            </XhButton>
            <XhPopconfirmRoot @confirm="handleBatchDeleteDict">
              <XhPopconfirmTrigger class="xh-linklike-trigger">
                {{ t('common.actions.delete') }}
              </XhPopconfirmTrigger>
              <XhPopconfirmPositioner>
                <XhPopconfirmContent>
                  <XhPopconfirmDescription>{{ t('setting.dict.confirm_batch_delete_dict', { count: checkedDictKeys.length }) }}</XhPopconfirmDescription>
                  <XhPopconfirmCancelTrigger>{{ t('common.actions.cancel') }}</XhPopconfirmCancelTrigger>
                  <XhPopconfirmConfirmTrigger>{{ t('common.actions.confirm') }}</XhPopconfirmConfirmTrigger>
                </XhPopconfirmContent>
              </XhPopconfirmPositioner>
            </XhPopconfirmRoot>
          </template>
        </div>
        <SchemaPagination
          v-model:page="dictPage"
          v-model:page-size="dictPageSize"
          :total="dictTotal"
          @update:page="handleDictPageChange"
          @update:page-size="handleDictPageSizeChange"
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
        <XhButton size="sm" tone="brand" :disabled="!currentDict" @click="handleItemAdd">
          <span><Icon icon="lucide:plus" /></span>
          {{ t('setting.dict.add_item') }}
        </XhButton>
      </header>

      <div class="pane__filters">
        <XInput
          v-model:value="itemQueryParams.keyword"
          class="pane__kw"
          clearable
          :disabled="!currentDict"
          :placeholder="t('setting.dict.item_search_placeholder')"
          size="sm"
          @keyup.enter="handleItemSearch"
          @clear="handleItemSearch"
        />
        <XhButton class="pane__search" size="sm" tone="brand" :disabled="!currentDict" @click="handleItemSearch">
          {{ t('common.actions.search') }}
        </XhButton>
      </div>

      <div class="pane__body">
        <XhEmptyStateRoot v-if="!currentDict" class="pane__empty">
          <XhEmptyStateIcon>
            <Icon icon="lucide:list-tree" width="28" />
          </XhEmptyStateIcon>
          <XhEmptyStateTitle>{{ t('setting.dict.select_dict_hint_title') }}</XhEmptyStateTitle>
          <XhEmptyStateDescription>{{ t('setting.dict.select_dict_hint') }}</XhEmptyStateDescription>
        </XhEmptyStateRoot>
        <XDataTable
          v-else
          selectable
          :checked-row-keys="checkedItemKeys.map(String)" class="pane__table"
          :columns="itemColumns"
          :data="itemList"
          :loading="itemLoading"
          :row-key="(row: DictItemListItemDto) => row.basicId"
          size="sm"
          @update:checked-row-keys="(keys: string[]) => (checkedItemKeys = keys)"
        />
      </div>

      <footer v-if="currentDict" class="pane__foot">
        <div class="pane__foot-left">
          <template v-if="checkedItemKeys.length">
            <span class="pane__sel">{{ t('setting.dict.selected', { count: checkedItemKeys.length }) }}</span>
            <XhButton size="sm" @click="handleBatchToggleItem(true)">
              {{ t('common.actions.enable') }}
            </XhButton>
            <XhButton size="sm" @click="handleBatchToggleItem(false)">
              {{ t('common.actions.disable') }}
            </XhButton>
            <XhPopconfirmRoot @confirm="handleBatchDeleteItem">
              <XhPopconfirmTrigger class="xh-linklike-trigger">
                {{ t('common.actions.delete') }}
              </XhPopconfirmTrigger>
              <XhPopconfirmPositioner>
                <XhPopconfirmContent>
                  <XhPopconfirmDescription>{{ t('setting.dict.confirm_batch_delete_item', { count: checkedItemKeys.length }) }}</XhPopconfirmDescription>
                  <XhPopconfirmCancelTrigger>{{ t('common.actions.cancel') }}</XhPopconfirmCancelTrigger>
                  <XhPopconfirmConfirmTrigger>{{ t('common.actions.confirm') }}</XhPopconfirmConfirmTrigger>
                </XhPopconfirmContent>
              </XhPopconfirmPositioner>
            </XhPopconfirmRoot>
          </template>
        </div>
        <SchemaPagination
          v-model:page="itemPage"
          v-model:page-size="itemPageSize"
          :total="itemTotal"
          :page-sizes="[10, 20, 50, 100]" @update:page="handleItemPageChange"
          @update:page-size="handleItemPageSizeChange"
        />
      </footer>
    </section>

    <!-- 字典 新增/编辑 -->
    <XEditModal
      v-model:show="modalVisible"
      :title="modalTitle"
      :loading="submitLoading"
      :form-id="editFormId"
    >
      <XhFormRoot
        :id="editFormId"
        v-model:values="dictForm"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleSubmit"
      >
        <XhFormFieldGroup value="dictCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.dict.dict_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="dictForm.dictCode"
                clearable
                :disabled="Boolean(dictForm.basicId)"
                :placeholder="t('setting.dict.dict_code_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="dictName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.dict.dict_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="dictForm.dictName" clearable :placeholder="t('setting.dict.dict_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="dictType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.dict.dict_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="dictForm.dictType" clearable :placeholder="t('setting.dict.dict_type_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="dictDescription" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.dict.description') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="dictForm.dictDescription"
                clearable
                :placeholder="t('setting.dict.description_placeholder')"
                :rows="3"
                type="textarea"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.dict.sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="dictForm.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup v-if="!dictForm.basicId" value="status">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.dict.status') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="dictForm.status" :options="statusEnumOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>

    <!-- 字典项 新增/编辑 -->
    <XEditModal
      v-model:show="itemModalVisible"
      :title="itemModalTitle"
      :loading="itemSubmitLoading"
      :form-id="itemModalFormId"
    >
      <XhFormRoot
        v-model:values="itemForm"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleItemSubmit"
      >
        <XhFormFieldGroup value="itemCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.dict.item_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="itemForm.itemCode"
                clearable
                :disabled="Boolean(itemForm.basicId)"
                :placeholder="t('setting.dict.item_code_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="itemName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.dict.item_name_label') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="itemForm.itemName" clearable :placeholder="t('setting.dict.item_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="itemValue">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.dict.item_value_label') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="itemForm.itemValue" clearable :placeholder="t('setting.dict.item_value_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="itemDescription" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.dict.description') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="itemForm.itemDescription"
                clearable
                :placeholder="t('setting.dict.description_placeholder')"
                :rows="2"
                type="textarea"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="isDefault">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.dict.is_default') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="itemForm.isDefault" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.dict.sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="itemForm.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup v-if="!itemForm.basicId" value="status">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.dict.status') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="itemForm.status" :options="statusEnumOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
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

/* 表格：占满 body 中段并在内部 tbody 滚动，不撑破容器；高度上限跟随 body 的高 */
.pane__table {
  flex: 1;
  min-height: 0;
  --xh-table-max-h: 100%;
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
