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
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhCardContent, XhCardRoot, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhSwitch } from '@xihan-ui/vue'
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, dictManagementApi, EnableStatus, querySortsFromSchema } from '@/api'
import { STATUS_OPTIONS } from '@/constants'
import { Icon, SchemaPage, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { useEnumOptions } from '~/hooks'

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
const dictFormId = useId()
const itemFormId = useId()
const statusOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)

// SchemaSelectOption.value 仅支持 string | number；布尔搜索项用 1/0，page() 里转回 boolean
const builtInOptions = computed(() => [
  { label: t('setting.dict.builtin'), value: 1 },
  { label: t('setting.dict.not_builtin'), value: 0 },
])

function toBool(value: unknown): boolean | undefined {
  return value === undefined || value === null || value === '' ? undefined : Boolean(Number(value))
}

function toKeyword(value: unknown): string | undefined {
  return (value as string | undefined)?.trim() || undefined
}

// ══════════════════════════════════════════════════════════════════
// 左栏：字典（主）
// ══════════════════════════════════════════════════════════════════
const dictPageRef = ref<{ reload: () => Promise<void>, rows: DictListItemDto[] } | null>(null)
/** 当前选中的字典：右栏字典项的取数依据 */
const currentDict = ref<DictListItemDto | null>(null)

function reloadDict() {
  void dictPageRef.value?.reload()
}

// 左栏数据变化后同步当前项：仍在列表里就跟着刷新，否则改选首条（列表空了就回到未选）
watch(() => dictPageRef.value?.rows, (rows) => {
  if (!rows?.length) {
    currentDict.value = null
    return
  }
  const latest = currentDict.value ? rows.find(d => d.basicId === currentDict.value?.basicId) : undefined
  currentDict.value = latest ?? rows[0]!
})

/** 内置字典由种子维护：后端 DeleteDictAsync 对内置字典直接抛错，列表不给删除入口 */
function canDeleteDict(row: DictListItemDto) {
  return !row.isBuiltIn
}

// ── 字段单一事实源（列 + searchable；仅搜索字段 visible:false；order 控顺序） ──
const dictFields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('setting.dict.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('setting.dict.dict_search_placeholder'), width: 220, order: 0 },
  { key: 'dictName', title: t('setting.dict.dict_name'), dataType: 'string', sortable: true, minWidth: 140, order: 1 },
  { key: 'dictCode', title: t('setting.dict.dict_code'), dataType: 'string', sortable: true, minWidth: 140, order: 2 },
  { key: 'dictType', title: t('setting.dict.dict_type'), dataType: 'string', sortable: true, minWidth: 100, order: 3 },
  // boolean / enum + options 由框架自动渲染为徽标，无需自定义 render
  { key: 'isBuiltIn', title: t('setting.dict.builtin'), dataType: 'boolean', sortable: true, searchable: true, options: builtInOptions.value, searchPlaceholder: t('setting.dict.builtin_placeholder'), width: 80, order: 4 },
  { key: 'status', title: t('setting.dict.status'), dataType: 'enum', sortable: true, searchable: true, searchMultiple: true, dictionaryCode: 'EnableStatus', options: statusOptions.value, searchPlaceholder: t('setting.dict.status_placeholder'), width: 90, order: 5 },
  { key: 'sort', title: t('setting.dict.sort'), dataType: 'number', sortable: true, width: 80, order: 6 },
  { key: 'createdTime', title: t('setting.dict.created_time'), dataType: 'datetime', sortable: true, visible: false, minWidth: 170, order: 7 },
])

const dictSchema = computed<PageSchema>(() => ({
  pageCode: 'platform.dict',
  pageName: t('setting.dict.page_name'),
  batchRemovable: true,
  removePermission: 'setting.dict.delete',
  statusPermission: 'setting.dict.status',
  rowKey: 'basicId',
  fields: dictFields.value,
  resource: {
    page: params => dictManagementApi.page({
      ...createPageRequest({
        page: { pageIndex: params.page, pageSize: params.pageSize },
        // 排序 + 状态多选等通用过滤统一走 conditions（多选经 filters In 下发，不再走 DTO 单值字段）
        conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
      }),
      isBuiltIn: toBool(params.filters.isBuiltIn),
      keyword: toKeyword(params.filters.keyword),
    }) as unknown as Promise<PageResult<Record<string, unknown>>>,
    remove: id => dictManagementApi.delete(id),
    updateStatus: (id, enabled) => dictManagementApi.updateStatus({
      basicId: id,
      status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled,
      remark: enabled ? t('setting.dict.batch_enable_dict_remark') : t('setting.dict.batch_disable_dict_remark'),
    }),
  },
  actions: [
    { key: 'create', title: t('setting.dict.add_dict'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pen' },
    { key: 'toggle', title: t('setting.dict.toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: t('setting.dict.confirm_toggle_dict') },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2', confirm: true, confirmText: t('setting.dict.confirm_delete_dict'), visible: row => canDeleteDict(row as unknown as DictListItemDto) },
  ],
}))

/** 主表整行点选：切换当前字典（右栏随之重建）；勾选框自身的点击不算 */
function dictRowProps(row: Record<string, unknown>) {
  const dict = row as unknown as DictListItemDto
  return {
    class: currentDict.value?.basicId === dict.basicId ? 'dict-row--active' : undefined,
    style: 'cursor: pointer;',
    onClick: (e: MouseEvent) => {
      if ((e.target as HTMLElement | null)?.closest('[data-scope="table"][data-part="row-select-trigger"], [data-scope="checkbox"]')) {
        return
      }
      currentDict.value = dict
    },
  }
}

function onDictAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as DictListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'edit':
      if (row) {
        void handleEdit(row)
      }
      break
    case 'toggle':
      if (row) {
        void handleToggleStatus(row)
      }
      break
    case 'delete':
      if (row) {
        void handleDelete(row)
      }
      break
  }
}

// ══════════════════════════════════════════════════════════════════
// 右栏：字典项（从，随左栏选中重建）
// ══════════════════════════════════════════════════════════════════
const itemPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadItems() {
  void itemPageRef.value?.reload()
}

const itemFields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('setting.dict.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('setting.dict.item_search_placeholder'), width: 220, order: 0 },
  { key: 'itemName', title: t('setting.dict.item_name'), dataType: 'string', sortable: true, minWidth: 130, order: 1 },
  { key: 'itemCode', title: t('setting.dict.code'), dataType: 'string', sortable: true, minWidth: 130, order: 2 },
  { key: 'itemValue', title: t('setting.dict.item_value'), dataType: 'string', minWidth: 90, order: 3 },
  { key: 'isDefault', title: t('setting.dict.default'), dataType: 'boolean', sortable: true, width: 80, order: 4 },
  { key: 'status', title: t('setting.dict.status'), dataType: 'enum', sortable: true, searchable: true, searchMultiple: true, dictionaryCode: 'EnableStatus', options: statusOptions.value, searchPlaceholder: t('setting.dict.status_placeholder'), width: 90, order: 5 },
  { key: 'sort', title: t('setting.dict.sort'), dataType: 'number', sortable: true, width: 80, order: 6 },
  { key: 'createdTime', title: t('setting.dict.created_time'), dataType: 'datetime', sortable: true, visible: false, minWidth: 170, order: 7 },
])

/**
 * 字典项 Schema。取数依赖当前字典，故整个右栏按 currentDict 用 v-if + key 重建：
 * 没选字典时不渲染（也就不会发请求），换字典时条件、排序与页码一并归零。
 */
const itemSchema = computed<PageSchema>(() => ({
  pageCode: 'platform.dict.item',
  pageName: t('setting.dict.items'),
  batchRemovable: true,
  removePermission: 'setting.dict.delete',
  statusPermission: 'setting.dict.status',
  rowKey: 'basicId',
  fields: itemFields.value,
  resource: {
    page: params => dictManagementApi.itemPage({
      ...createPageRequest({
        page: { pageIndex: params.page, pageSize: params.pageSize },
        conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
      }),
      dictId: currentDict.value!.basicId,
      keyword: toKeyword(params.filters.keyword),
    }) as unknown as Promise<PageResult<Record<string, unknown>>>,
    remove: id => dictManagementApi.itemDelete(id),
    updateStatus: (id, enabled) => dictManagementApi.itemUpdateStatus({
      basicId: id,
      status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled,
      remark: enabled ? t('setting.dict.batch_enable_item_remark') : t('setting.dict.batch_disable_item_remark'),
    }),
  },
  actions: [
    { key: 'create', title: t('setting.dict.add_item'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pen' },
    { key: 'toggle', title: t('setting.dict.toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: t('setting.dict.confirm_toggle_item') },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2', confirm: true, confirmText: t('setting.dict.confirm_delete_item') },
  ],
}))

function onItemAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as DictItemListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleItemAdd()
      break
    case 'edit':
      if (row) {
        void handleItemEdit(row)
      }
      break
    case 'toggle':
      if (row) {
        void handleItemToggleStatus(row)
      }
      break
    case 'delete':
      if (row) {
        void handleItemDelete(row)
      }
      break
  }
}

// ══════════════════════════════════════════════════════════════════
// 字典 表单与单条操作
// ══════════════════════════════════════════════════════════════════
const dictModalVisible = ref(false)
const dictSubmitLoading = ref(false)
const dictEditingStatus = ref<EnableStatus | null>(null)
const dictForm = ref<DictFormModel>(createDefaultDictForm())
const dictModalTitle = computed(() => (dictForm.value.basicId ? t('setting.dict.edit_dict_title') : t('setting.dict.add_dict_title')))

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

function handleAdd() {
  dictEditingStatus.value = null
  dictForm.value = createDefaultDictForm()
  dictModalVisible.value = true
}

async function handleEdit(row: DictListItemDto) {
  dictEditingStatus.value = row.status
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
  dictModalVisible.value = true
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

  dictSubmitLoading.value = true
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
      if (dictEditingStatus.value !== dictForm.value.status) {
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
    dictModalVisible.value = false
    reloadDict()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    dictSubmitLoading.value = false
  }
}

async function handleDelete(row: DictListItemDto) {
  try {
    await dictManagementApi.delete(row.basicId)
    toast.success(t('common.messages.delete_success'))
    // 删掉的正是当前选中项：右栏回到未选状态
    if (currentDict.value?.basicId === row.basicId) {
      currentDict.value = null
    }
    reloadDict()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('common.messages.delete_failed'))
  }
}

async function handleToggleStatus(row: DictListItemDto) {
  try {
    await dictManagementApi.updateStatus({
      basicId: row.basicId,
      remark: row.status === EnableStatus.Enabled ? t('setting.dict.dict_disable_remark') : t('setting.dict.dict_enable_remark'),
      status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
    })
    toast.success(t('common.messages.status_updated'))
    reloadDict()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('common.messages.status_failed'))
  }
}

// ══════════════════════════════════════════════════════════════════
// 字典项 表单与单条操作
// ══════════════════════════════════════════════════════════════════
const itemModalVisible = ref(false)
const itemSubmitLoading = ref(false)
const itemEditingStatus = ref<EnableStatus | null>(null)
const itemForm = ref<DictItemFormModel>(createDefaultDictItemForm())
const itemModalTitle = computed(() => (itemForm.value.basicId ? t('setting.dict.edit_item_title') : t('setting.dict.add_item_title')))

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
    reloadItems()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    itemSubmitLoading.value = false
  }
}

async function handleItemDelete(row: DictItemListItemDto) {
  try {
    await dictManagementApi.itemDelete(row.basicId)
    toast.success(t('common.messages.delete_success'))
    reloadItems()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('common.messages.delete_failed'))
  }
}

async function handleItemToggleStatus(row: DictItemListItemDto) {
  try {
    await dictManagementApi.itemUpdateStatus({
      basicId: row.basicId,
      remark: row.status === EnableStatus.Enabled ? t('setting.dict.item_disable_remark') : t('setting.dict.item_enable_remark'),
      status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
    })
    toast.success(t('common.messages.status_updated'))
    reloadItems()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('common.messages.status_failed'))
  }
}
</script>

<template>
  <!-- 主从两栏：左字典、右字典项，各自是一整套 SchemaPage（搜索设置 / 列设置 / 批量操作全部照旧） -->
  <div class="dict-mgmt">
    <SchemaPage
      ref="dictPageRef"
      class="dict-mgmt__pane"
      :schema="dictSchema"
      :row-props="dictRowProps"
      @action="onDictAction"
    >
      <template #toolbar-leading>
        <span class="dict-mgmt__pane-title">{{ t('setting.dict.dict_list') }}</span>
      </template>

      <!-- 字典 新增/编辑 -->
      <XEditModal
        v-model:show="dictModalVisible"
        :title="dictModalTitle"
        :loading="dictSubmitLoading"
        :form-id="dictFormId"
      >
        <XhFormRoot
          :id="dictFormId"
          v-model:values="dictForm"
          validate-on="blur"
          class="xh-edit-form-grid"
          @submit="handleSubmit"
        >
          <XhFormFieldGroup name="dictCode">
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
          <XhFormFieldGroup name="dictName">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('setting.dict.dict_name') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="dictForm.dictName" clearable :placeholder="t('setting.dict.dict_name_placeholder')" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="dictType">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('setting.dict.dict_type') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="dictForm.dictType" clearable :placeholder="t('setting.dict.dict_type_placeholder')" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="dictDescription" class="xh-span-2">
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
          <XhFormFieldGroup name="sort">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('setting.dict.sort') }}</XhFieldLabel>
              <XhFieldControl>
                <XNumberInput v-model:value="dictForm.sort" :min="0" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup v-if="!dictForm.basicId" name="status">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('setting.dict.status') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect v-model:value="dictForm.status" :options="statusOptions" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
        </XhFormRoot>
      </XEditModal>
    </SchemaPage>

    <!-- 右栏按当前字典重建：没选字典时不渲染（也就不发请求），换字典即重置条件与分页 -->
    <SchemaPage
      v-if="currentDict"
      :key="currentDict.basicId"
      ref="itemPageRef"
      class="dict-mgmt__pane"
      :schema="itemSchema"
      @action="onItemAction"
    >
      <template #toolbar-leading>
        <span class="dict-mgmt__pane-title">{{ currentDict.dictName }}</span>
      </template>

      <!-- 字典项 新增/编辑 -->
      <XEditModal
        v-model:show="itemModalVisible"
        :title="itemModalTitle"
        :loading="itemSubmitLoading"
        :form-id="itemFormId"
      >
        <XhFormRoot
          :id="itemFormId"
          v-model:values="itemForm"
          validate-on="blur"
          class="xh-edit-form-grid"
          @submit="handleItemSubmit"
        >
          <XhFormFieldGroup name="itemCode">
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
          <XhFormFieldGroup name="itemName">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('setting.dict.item_name_label') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="itemForm.itemName" clearable :placeholder="t('setting.dict.item_name_placeholder')" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="itemValue">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('setting.dict.item_value_label') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="itemForm.itemValue" clearable :placeholder="t('setting.dict.item_value_placeholder')" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="itemDescription" class="xh-span-2">
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
          <XhFormFieldGroup name="isDefault">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('setting.dict.is_default') }}</XhFieldLabel>
              <XhFieldControl>
                <XhSwitch v-model:checked="itemForm.isDefault" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="sort">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('setting.dict.sort') }}</XhFieldLabel>
              <XhFieldControl>
                <XNumberInput v-model:value="itemForm.sort" :min="0" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup v-if="!itemForm.basicId" name="status">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('setting.dict.status') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect v-model:value="itemForm.status" :options="statusOptions" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
        </XhFormRoot>
      </XEditModal>
    </SchemaPage>

    <!-- 未选字典：右栏占位，提示先在左侧选一个字典 -->
    <div v-else class="dict-mgmt__pane dict-mgmt__pane--empty">
      <XhCardRoot class="dict-mgmt__empty-card" variant="outline">
        <XhCardContent>
          <XhEmptyStateRoot>
            <XhEmptyStateIndicator>
              <Icon icon="lucide:list-tree" width="28" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('setting.dict.select_dict_hint_title') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('setting.dict.select_dict_hint') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
        </XhCardContent>
      </XhCardRoot>
    </div>
  </div>
</template>

<style scoped>
/* 两栏并排、各占一半；整页不滚，滚动只发生在两侧表格内部 */
.dict-mgmt {
  display: flex;
  height: 100%;
  overflow: hidden;
}

/* 每栏是一整套 SchemaPage：它自带 p-3 内衬，两栏之间只把相邻侧收窄一档，不另加间距 */
.dict-mgmt__pane {
  flex: 1 1 0;
  min-width: 0;
}

.dict-mgmt__pane:first-child {
  padding-inline-end: var(--xh-space-1_5);
}

.dict-mgmt__pane:last-child {
  padding-inline-start: var(--xh-space-1_5);
}

/* 工具条上的栏标题：与页面级操作按钮同排 */
.dict-mgmt__pane-title {
  flex-shrink: 0;
  margin-inline-end: var(--xh-space-1);
  font-size: var(--xh-font-size-md);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-default);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 未选字典时的右栏：与 SchemaPage 的卡片同款内衬 */
.dict-mgmt__pane--empty {
  display: flex;
  flex-direction: column;
  padding: var(--xh-space-3);
}

.dict-mgmt__empty-card {
  --xh-card-p: var(--xh-surface-pad-md) var(--xh-surface-pad-lg);

  flex: 1;
}

/* 主表当前行：淡淡的品牌底 + 名称转品牌色，与勾选态（品牌淡底）拉开一档 */
.dict-mgmt :deep(.dict-row--active) {
  --xh-collection-bg-rest: color-mix(in srgb, hsl(var(--primary)) 6%, hsl(var(--card)));
  --xh-collection-fg-rest: var(--xh-fg-brand);
}

@media (max-width: 1024px) {
  /* 窄屏：上下堆叠，整页可滚动；每栏给定高度以便表格内部滚动 */
  .dict-mgmt {
    flex-direction: column;
    height: auto;
    min-height: 100%;
    overflow: visible;
  }

  .dict-mgmt__pane {
    flex: none;
    height: 70vh;
  }

  .dict-mgmt__pane:first-child,
  .dict-mgmt__pane:last-child {
    padding-inline: var(--xh-space-3);
  }
}
</style>
