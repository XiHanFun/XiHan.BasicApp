<script lang="ts" setup>
import type { DictItemNode, DictRow, SettingStatus } from './use-dicts'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { SelectOption } from '~/types'
import { XhButton, XhCardContent, XhCardRoot, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogFooter, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSwitch } from '@xihan-ui/vue'
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, Icon, renderTag, SchemaPage, statusConfirmText, XEditModal, XInput, XNumberInput, XSelect, XTreeSelect } from '~/components'
import { toast } from '~/composables'
import { useAuthStore } from '~/stores'
import { formatDate } from '~/utils'
import { dictItemsResource, dictsResource, findDictItem, useDictEditor, useDictItemEditor } from './use-dicts'

defineOptions({ name: 'SettingsDictsView' })

const { t } = useI18n()
const auth = useAuthStore()
const isPlatformCaller = () => auth.tenantId === null
const dictEditor = useDictEditor(isPlatformCaller)
const itemEditor = useDictItemEditor()
const resource = dictsResource()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const dictFormId = useId()
const itemFormId = useId()

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对
function statusLabel(status: SettingStatus): string {
  return status === 'Disabled' ? t('settings.dicts.status.disabled') : t('settings.dicts.status.active')
}

function ownerLabel(row: DictRow): string {
  return row.isPlatform ? t('settings.dicts.owner.platform') : t('settings.dicts.owner.tenant')
}

const statusOptions = computed<SelectOption[]>(() => (['Active', 'Disabled'] as const).map(status => ({ label: statusLabel(status), value: status })))
const ownerOptions = computed(() => [
  { label: t('settings.dicts.owner.platform'), value: 'platform' },
  { label: t('settings.dicts.owner.tenant'), value: 'tenant' },
])
const builtInOptions = computed(() => [
  { label: t('settings.dicts.built_in'), value: 'true' },
  { label: t('settings.dicts.not_built_in'), value: 'false' },
])

// SchemaPage 的行类型在框架边界放宽（Record）：回调里按本页的行类型取用
const asDict = (row: object) => row as DictRow
const asItem = (row: object) => row as DictItemNode

// ══════════════════════════════════════════════════════════════════
// 左栏：字典（主）
// ══════════════════════════════════════════════════════════════════
const dictPageRef = ref<{ reload: () => Promise<void>, rows: DictRow[] } | null>(null)
/** 当前选中的字典：右栏字典项的取数依据 */
const currentDict = ref<DictRow | null>(null)
/** 另一侧的字典只读：它的项也一样（平台看得见租户字典、租户看得见平台字典，都改不了） */
const canMaintainItems = computed(() => dictEditor.isOwn(currentDict.value))

function reloadDicts() {
  void dictPageRef.value?.reload()
}

// 左栏数据变化后同步当前项：仍在列表里就跟着刷新，否则改选首条（列表空了就回到未选）
watch(() => dictPageRef.value?.rows, (rows) => {
  if (!rows?.length) {
    currentDict.value = null
    return
  }
  const latest = currentDict.value ? rows.find(d => d.id === currentDict.value?.id) : undefined
  currentDict.value = latest ?? rows[0]!
})

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；order 控顺序；可排序的只放后端白名单里的） ──
const dictFields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('settings.dicts.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('settings.dicts.keyword_placeholder'), order: 0 },
  { key: 'name', title: t('settings.dicts.fields.name'), dataType: 'string', sortable: true, importable: true, required: true, exportable: true, minWidth: 140, order: 1 },
  { key: 'code', title: t('settings.dicts.fields.code'), dataType: 'string', sortable: true, importable: true, required: true, exportable: true, minWidth: 140, order: 2 },
  { key: 'category', title: t('settings.dicts.fields.category'), dataType: 'string', sortable: true, advancedSearch: true, importable: true, exportable: true, minWidth: 100, order: 3 },
  { key: 'owner', title: t('settings.dicts.fields.owner'), dataType: 'enum', searchable: true, options: ownerOptions.value, render: row => ownerLabel(asDict(row)), width: 90, order: 4 },
  { key: 'isBuiltIn', title: t('settings.dicts.built_in'), dataType: 'boolean', advancedSearch: true, options: builtInOptions.value, width: 80, order: 5 },
  { key: 'status', title: t('settings.dicts.fields.status'), dataType: 'enum', searchable: true, exportable: true, options: statusOptions.value, render: row => renderTag(statusLabel(asDict(row).status), asDict(row).status === 'Active' ? 'success' : 'danger'), width: 90, order: 6 },
  { key: 'order', title: t('settings.dicts.fields.order'), dataType: 'number', sortable: true, importable: true, exportable: true, width: 80, order: 7 },
  { key: 'description', title: t('settings.dicts.fields.description'), dataType: 'text', visible: false, importable: true, order: 7.5 },
  { key: 'createdAt', title: t('settings.dicts.fields.created_at'), dataType: 'datetime', sortable: true, visible: false, minWidth: 170, order: 8 },
])

const dictSchema = computed<PageSchema>(() => ({
  pageCode: 'settings.dicts',
  pageName: t('settings.dicts.title'),
  exportPermission: 'settings.dicts.read',
  importPermission: 'settings.dicts.create',
  batchRemovable: true,
  removePermission: 'settings.dicts.delete',
  statusPermission: 'settings.dicts.update',
  rowKey: 'id',
  fields: dictFields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('settings.dicts.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'settings.dicts.create' },
    { key: 'view', title: t('common.actions.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', visible: row => dictEditor.isOwn(asDict(row)), permission: 'settings.dicts.update' },
    { key: 'toggle', title: t('settings.dicts.actions.toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => statusConfirmText(t, asDict(row).status === 'Active', asDict(row).name), visible: row => dictEditor.isOwn(asDict(row)), permission: 'settings.dicts.update' },
    // 内建字典由种子维护，还有项的字典后端也拒绝删除
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => deleteConfirmText(t, asDict(row).name), visible: row => dictEditor.isOwn(asDict(row)) && !asDict(row).isBuiltIn, permission: 'settings.dicts.delete' },
  ],
}))

/** 主表整行点选：切换当前字典（右栏随之重建）；勾选框自身的点击不算 */
function dictRowProps(row: Record<string, unknown>) {
  const dict = asDict(row)
  return {
    class: currentDict.value?.id === dict.id ? 'dict-row--active' : undefined,
    style: 'cursor: pointer;',
    onClick: (e: MouseEvent) => {
      if ((e.target as HTMLElement | null)?.closest('[data-scope="table"][data-part="row-select-trigger"], [data-scope="checkbox"]'))
        return
      currentDict.value = dict
    },
  }
}

function onDictAction(payload: SchemaActionPayload) {
  const row = payload.row ? asDict(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      dictEditor.startCreate()
      break
    case 'view':
      if (row)
        openDictDetail(row)
      break
    case 'edit':
      if (row)
        dictEditor.startEdit(row)
      break
    case 'toggle':
      if (row)
        void toggleDict(row)
      break
    case 'delete':
      if (row)
        void removeDict(row)
      break
  }
}

async function onDictSubmit() {
  if (await dictEditor.submit()) {
    toast.success(t('common.messages.save_success'))
    reloadDicts()
  }
}

async function removeDict(row: DictRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('common.messages.delete_success'))
    // 删掉的正是当前选中项：右栏回到未选状态
    if (currentDict.value?.id === row.id)
      currentDict.value = null
    reloadDicts()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

async function toggleDict(row: DictRow) {
  try {
    await resource.updateStatus?.(row.id, row.status !== 'Active')
    toast.success(t('common.messages.status_updated'))
    reloadDicts()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.status_failed'))
  }
}

// ══════════════════════════════════════════════════════════════════
// 右栏：字典项（从，随左栏选中重建；树形，按上级挂成层级）
// ══════════════════════════════════════════════════════════════════
const itemPageRef = ref<{ reload: () => Promise<void>, rows: DictItemNode[] } | null>(null)

function reloadItems() {
  void itemPageRef.value?.reload()
}

/** 当前项树（右栏 SchemaPage 取回的嵌套行）：上级下拉与详情里的上级名称都从它取 */
function itemTree(): DictItemNode[] {
  return itemPageRef.value?.rows ?? []
}

/** 另一侧的字典只带取数：批量删除 / 启停与导入都依赖资源上的对应函数，没有就不出 */
const itemResource = computed(() => currentDict.value ? dictItemsResource(currentDict.value.id, canMaintainItems.value) : null)

const itemFields = computed<ListFieldSchema[]>(() => [
  { key: 'name', title: t('settings.dicts.item_fields.name'), dataType: 'string', treeColumn: true, importable: true, required: true, exportable: true, minWidth: 160, order: 1 },
  { key: 'code', title: t('settings.dicts.item_fields.code'), dataType: 'string', importable: true, required: true, exportable: true, minWidth: 130, order: 2 },
  { key: 'value', title: t('settings.dicts.item_fields.value'), dataType: 'string', importable: true, exportable: true, minWidth: 90, order: 3 },
  { key: 'isDefault', title: t('settings.dicts.item_fields.default'), dataType: 'boolean', importable: true, exportable: true, width: 80, order: 4 },
  { key: 'status', title: t('settings.dicts.item_fields.status'), dataType: 'enum', exportable: true, options: statusOptions.value, render: row => renderTag(statusLabel(asItem(row).status), asItem(row).status === 'Active' ? 'success' : 'danger'), width: 90, order: 5 },
  { key: 'order', title: t('settings.dicts.item_fields.order'), dataType: 'number', importable: true, exportable: true, width: 80, order: 6 },
  { key: 'description', title: t('settings.dicts.item_fields.description'), dataType: 'text', visible: false, importable: true, order: 6.5 },
  { key: 'metadata', title: t('settings.dicts.item_fields.metadata'), dataType: 'text', visible: false, order: 6.6 },
  { key: 'createdAt', title: t('settings.dicts.item_fields.created_at'), dataType: 'datetime', visible: false, minWidth: 170, order: 7 },
])

/**
 * 字典项 Schema。取数依赖当前字典，故整个右栏按 currentDict 用 v-if + key 重建：
 * 没选字典时不渲染（也就不会发请求），换字典时展开状态一并归零。后端一次返回整本字典的项，不分页、不搜索。
 */
const itemSchema = computed<PageSchema>(() => ({
  pageCode: 'settings.dict-items',
  pageName: t('settings.dicts.items'),
  exportPermission: 'settings.dict-items.read',
  importPermission: 'settings.dict-items.create',
  batchRemovable: true,
  removePermission: 'settings.dict-items.delete',
  statusPermission: 'settings.dict-items.update',
  rowKey: 'id',
  tree: { childrenKey: 'children', defaultExpandAll: true },
  fields: itemFields.value,
  resource: (itemResource.value ?? {}) as unknown as PageSchema['resource'],
  actions: [
    // SchemaActionPanel 仅按权限筛选页面级动作，因此只在可维护的字典里组装「新增」按钮
    ...(canMaintainItems.value
      ? [{ key: 'create', title: t('settings.dicts.actions.create_item'), scope: 'page' as const, tone: 'brand' as const, icon: 'lucide:plus', permission: 'settings.dict-items.create' }]
      : []),
    { key: 'view', title: t('common.actions.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', visible: () => canMaintainItems.value, permission: 'settings.dict-items.update' },
    { key: 'toggle', title: t('settings.dicts.actions.toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => statusConfirmText(t, asItem(row).status === 'Active', asItem(row).name), visible: () => canMaintainItems.value, permission: 'settings.dict-items.update' },
    // 还有下级的项后端拒绝删除
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => deleteConfirmText(t, asItem(row).name), visible: () => canMaintainItems.value, permission: 'settings.dict-items.delete' },
  ],
}))

function onItemAction(payload: SchemaActionPayload) {
  const row = payload.row ? asItem(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      if (currentDict.value)
        itemEditor.startCreate(currentDict.value, itemTree())
      break
    case 'view':
      if (row)
        openItemDetail(row)
      break
    case 'edit':
      if (row)
        itemEditor.startEdit(row, itemTree())
      break
    case 'toggle':
      if (row)
        void toggleItem(row)
      break
    case 'delete':
      if (row)
        void removeItem(row)
      break
  }
}

async function onItemSubmit() {
  if (await itemEditor.submit()) {
    toast.success(t('common.messages.save_success'))
    reloadItems()
  }
}

async function removeItem(row: DictItemNode) {
  try {
    await itemResource.value?.remove?.(row.id)
    toast.success(t('common.messages.delete_success'))
    reloadItems()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

async function toggleItem(row: DictItemNode) {
  try {
    await itemResource.value?.updateStatus?.(row.id, row.status !== 'Active')
    toast.success(t('common.messages.status_updated'))
    reloadItems()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.status_failed'))
  }
}

// ── 查看详情 ──
const dictDetailOpen = ref(false)
const dictDetail = ref<DictRow | null>(null)
const itemDetailOpen = ref(false)
const itemDetail = ref<DictItemNode | null>(null)

function openDictDetail(row: DictRow) {
  dictDetail.value = row
  dictDetailOpen.value = true
}

function openItemDetail(row: DictItemNode) {
  itemDetail.value = row
  itemDetailOpen.value = true
}

function parentLabel(row: DictItemNode): string {
  const parent = findDictItem(itemTree(), row.parentId)
  return parent ? `${parent.code} · ${parent.name}` : t('settings.dicts.top_level')
}

function formatNullable(value: unknown): string {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatBoolean(value: boolean): string {
  return value ? t('common.statuses.yes') : t('common.statuses.no')
}
</script>

<template>
  <!-- 主从两栏：左字典、右字典项，各自是一整套 SchemaPage（搜索设置 / 列设置 / 批量操作全部照旧）；
       宽屏并排各占一半、整页不滚，窄屏上下堆叠、每栏定高、整页滚动 -->
  <div class="dict-mgmt flex min-h-full flex-col lg:h-full lg:flex-row lg:overflow-hidden">
    <div class="h-[70vh] min-w-0 flex-none lg:h-auto lg:flex-1">
      <!-- 每栏是一整套 SchemaPage：它自带 p-3 内衬，两栏之间只把相邻侧收窄一档，不另加间距 -->
      <SchemaPage
        ref="dictPageRef"
        class="lg:pe-1.5"
        :schema="dictSchema"
        :row-props="dictRowProps"
        @action="onDictAction"
      >
        <template #toolbar-leading>
          <span class="dict-mgmt__pane-title">{{ t('settings.dicts.dict_list') }}</span>
        </template>

        <XhDialogRoot v-model:open="dictDetailOpen">
          <XhDialogContent class="xh-mgmt-detail-modal" style="--xh-dialog-max-w: 640px">
            <XhDialogHeader>
              <XhDialogTitle>
                <div v-if="dictDetail" class="det-hd-entity">
                  <div class="det-hd-ico">
                    <Icon icon="lucide:book-open" width="22" height="22" />
                  </div>
                  <div class="min-w-0">
                    <div class="det-hd-name">
                      {{ dictDetail.name }}
                    </div>
                    <div class="det-hd-sub">
                      {{ dictDetail.code }}
                    </div>
                  </div>
                </div>
              </XhDialogTitle>
            </XhDialogHeader>
            <XhDialogCloseTrigger />
            <XhDialogBody class="xh-overlay-body">
              <XhDescriptionsRoot v-if="dictDetail" :columns="2" variant="outline" size="sm">
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('settings.dicts.fields.category') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatNullable(dictDetail.category) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('settings.dicts.fields.owner') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ ownerLabel(dictDetail) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('settings.dicts.fields.status') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ statusLabel(dictDetail.status) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('settings.dicts.built_in') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatBoolean(dictDetail.isBuiltIn) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('settings.dicts.fields.order') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ dictDetail.order }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('settings.dicts.fields.created_at') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatDate(dictDetail.createdAt) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <!-- 跨列项排在末尾：边框按子节点奇偶画竖线，跨列项之后的项奇偶会与实际列位错开 -->
                <XhDescriptionsItem style="grid-column: span 2">
                  <XhDescriptionsLabel>{{ t('settings.dicts.fields.description') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatNullable(dictDetail.description) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
              </XhDescriptionsRoot>
            </XhDialogBody>
            <XhDialogFooter>
              <XhButton variant="subtle" size="sm" @click="dictDetailOpen = false">
                {{ t('common.actions.close') }}
              </XhButton>
            </XhDialogFooter>
          </XhDialogContent>
        </XhDialogRoot>

        <!-- 字典 新增/编辑 -->
        <XEditModal
          v-model:show="dictEditor.open.value"
          :title="dictEditor.isEdit.value ? t('settings.dicts.edit_title') : t('settings.dicts.create_title')"
          :loading="dictEditor.submitting.value"
          :form-id="dictFormId"
        >
          <XhFormRoot :id="dictFormId" class="xh-edit-form-grid" data-testid="dict-form" @submit="onDictSubmit">
            <XhFieldRoot :invalid="!!dictEditor.fieldError('code')" :required="!dictEditor.isEdit.value">
              <XhFieldLabel>{{ t('settings.dicts.fields.code') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="dictEditor.form.code" name="code" clearable autocomplete="off" :disabled="dictEditor.isEdit.value" :placeholder="t('settings.dicts.code_placeholder')" />
              </XhFieldControl>
              <XhFieldErrorText v-if="dictEditor.fieldError('code')">
                {{ dictEditor.fieldError('code') }}
              </XhFieldErrorText>
            </XhFieldRoot>
            <XhFieldRoot :invalid="!!dictEditor.fieldError('name')" required>
              <XhFieldLabel>{{ t('settings.dicts.fields.name') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="dictEditor.form.name" name="name" clearable />
              </XhFieldControl>
              <XhFieldErrorText v-if="dictEditor.fieldError('name')">
                {{ dictEditor.fieldError('name') }}
              </XhFieldErrorText>
            </XhFieldRoot>
            <XhFieldRoot :invalid="!!dictEditor.fieldError('category')">
              <XhFieldLabel>{{ t('settings.dicts.fields.category') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="dictEditor.form.category" name="category" clearable />
              </XhFieldControl>
              <XhFieldErrorText v-if="dictEditor.fieldError('category')">
                {{ dictEditor.fieldError('category') }}
              </XhFieldErrorText>
            </XhFieldRoot>
            <XhFieldRoot :invalid="!!dictEditor.fieldError('order')">
              <XhFieldLabel>{{ t('settings.dicts.fields.order') }}</XhFieldLabel>
              <XhFieldControl>
                <XNumberInput v-model:value="dictEditor.form.order" name="order" :min="0" :precision="0" />
              </XhFieldControl>
              <XhFieldErrorText v-if="dictEditor.fieldError('order')">
                {{ dictEditor.fieldError('order') }}
              </XhFieldErrorText>
            </XhFieldRoot>
            <XhFieldRoot v-if="dictEditor.isEdit.value">
              <XhFieldLabel>{{ t('settings.dicts.fields.status') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect v-model:value="dictEditor.form.status" name="status" :options="statusOptions" />
              </XhFieldControl>
            </XhFieldRoot>
            <XhFieldRoot class="xh-span-2" :invalid="!!dictEditor.fieldError('description')">
              <XhFieldLabel>{{ t('settings.dicts.fields.description') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="dictEditor.form.description" name="description" type="textarea" :rows="3" clearable />
              </XhFieldControl>
              <XhFieldErrorText v-if="dictEditor.fieldError('description')">
                {{ dictEditor.fieldError('description') }}
              </XhFieldErrorText>
            </XhFieldRoot>
            <p v-if="dictEditor.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
              {{ dictEditor.submitError.value }}
            </p>
          </XhFormRoot>
        </XEditModal>
      </SchemaPage>
    </div>

    <div class="h-[70vh] min-w-0 flex-none lg:h-auto lg:flex-1">
      <!-- 右栏按当前字典重建：没选字典时不渲染（也就不发请求），换字典即重置展开状态 -->
      <SchemaPage
        v-if="currentDict"
        :key="currentDict.id"
        ref="itemPageRef"
        class="lg:ps-1.5"
        :schema="itemSchema"
        @action="onItemAction"
      >
        <template #toolbar-leading>
          <span class="dict-mgmt__pane-title">{{ currentDict.name }}</span>
        </template>

        <XhDialogRoot v-model:open="itemDetailOpen">
          <XhDialogContent class="xh-mgmt-detail-modal" style="--xh-dialog-max-w: 640px">
            <XhDialogHeader>
              <XhDialogTitle>
                <div v-if="itemDetail" class="det-hd-entity">
                  <div class="det-hd-ico">
                    <Icon icon="lucide:list-tree" width="22" height="22" />
                  </div>
                  <div class="min-w-0">
                    <div class="det-hd-name">
                      {{ itemDetail.name }}
                    </div>
                    <div class="det-hd-sub">
                      {{ itemDetail.code }}
                    </div>
                  </div>
                </div>
              </XhDialogTitle>
            </XhDialogHeader>
            <XhDialogCloseTrigger />
            <XhDialogBody class="xh-overlay-body">
              <XhDescriptionsRoot v-if="itemDetail" :columns="2" variant="outline" size="sm">
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('settings.dicts.item_fields.value') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatNullable(itemDetail.value) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('settings.dicts.item_fields.parent') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ parentLabel(itemDetail) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('settings.dicts.item_fields.default') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatBoolean(itemDetail.isDefault) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('settings.dicts.item_fields.status') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ statusLabel(itemDetail.status) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('settings.dicts.item_fields.order') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ itemDetail.order }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('settings.dicts.item_fields.created_at') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatDate(itemDetail.createdAt) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <!-- 跨列项排在末尾：边框按子节点奇偶画竖线，跨列项之后的项奇偶会与实际列位错开 -->
                <XhDescriptionsItem style="grid-column: span 2">
                  <XhDescriptionsLabel>{{ t('settings.dicts.item_fields.description') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatNullable(itemDetail.description) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem style="grid-column: span 2">
                  <XhDescriptionsLabel>{{ t('settings.dicts.item_fields.metadata') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    <pre class="dict-mgmt__value-block">{{ formatNullable(itemDetail.metadata) }}</pre>
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
              </XhDescriptionsRoot>
            </XhDialogBody>
            <XhDialogFooter>
              <XhButton variant="subtle" size="sm" @click="itemDetailOpen = false">
                {{ t('common.actions.close') }}
              </XhButton>
            </XhDialogFooter>
          </XhDialogContent>
        </XhDialogRoot>

        <!-- 字典项 新增/编辑 -->
        <XEditModal
          v-model:show="itemEditor.open.value"
          :title="itemEditor.isEdit.value ? t('settings.dicts.edit_item_title') : t('settings.dicts.create_item_title')"
          :loading="itemEditor.submitting.value"
          :form-id="itemFormId"
        >
          <XhFormRoot :id="itemFormId" class="xh-edit-form-grid" data-testid="dict-item-form" @submit="onItemSubmit">
            <XhFieldRoot :invalid="!!itemEditor.fieldError('code')" :required="!itemEditor.isEdit.value">
              <XhFieldLabel>{{ t('settings.dicts.item_fields.code') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="itemEditor.form.code" name="code" clearable autocomplete="off" :disabled="itemEditor.isEdit.value" :placeholder="t('settings.dicts.item_code_placeholder')" />
              </XhFieldControl>
              <XhFieldErrorText v-if="itemEditor.fieldError('code')">
                {{ itemEditor.fieldError('code') }}
              </XhFieldErrorText>
            </XhFieldRoot>
            <XhFieldRoot :invalid="!!itemEditor.fieldError('name')" required>
              <XhFieldLabel>{{ t('settings.dicts.item_fields.name') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="itemEditor.form.name" name="name" clearable />
              </XhFieldControl>
              <XhFieldErrorText v-if="itemEditor.fieldError('name')">
                {{ itemEditor.fieldError('name') }}
              </XhFieldErrorText>
            </XhFieldRoot>
            <!-- 上级只能选同一字典里不是自己、也不是自己下级的项；不选即顶层 -->
            <XhFieldRoot :invalid="!!itemEditor.fieldError('parentId')">
              <XhFieldLabel>{{ t('settings.dicts.item_fields.parent') }}</XhFieldLabel>
              <XhFieldControl>
                <XTreeSelect
                  :value="itemEditor.form.parentId"
                  :options="itemEditor.parentOptions.value"
                  clearable
                  :placeholder="t('settings.dicts.top_level')"
                  @update:value="value => itemEditor.form.parentId = value == null ? null : String(value)"
                />
              </XhFieldControl>
              <XhFieldErrorText v-if="itemEditor.fieldError('parentId')">
                {{ itemEditor.fieldError('parentId') }}
              </XhFieldErrorText>
            </XhFieldRoot>
            <XhFieldRoot :invalid="!!itemEditor.fieldError('value')">
              <XhFieldLabel>{{ t('settings.dicts.item_fields.value') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="itemEditor.form.value" name="value" clearable />
              </XhFieldControl>
              <XhFieldErrorText v-if="itemEditor.fieldError('value')">
                {{ itemEditor.fieldError('value') }}
              </XhFieldErrorText>
            </XhFieldRoot>
            <XhFieldRoot :invalid="!!itemEditor.fieldError('order')">
              <XhFieldLabel>{{ t('settings.dicts.item_fields.order') }}</XhFieldLabel>
              <XhFieldControl>
                <XNumberInput v-model:value="itemEditor.form.order" name="order" :min="0" :precision="0" />
              </XhFieldControl>
              <XhFieldErrorText v-if="itemEditor.fieldError('order')">
                {{ itemEditor.fieldError('order') }}
              </XhFieldErrorText>
            </XhFieldRoot>
            <XhFieldRoot>
              <XhFieldLabel>{{ t('settings.dicts.item_fields.default') }}</XhFieldLabel>
              <XhFieldControl>
                <XhSwitch v-model:checked="itemEditor.form.isDefault" name="isDefault" />
              </XhFieldControl>
            </XhFieldRoot>
            <XhFieldRoot v-if="itemEditor.isEdit.value">
              <XhFieldLabel>{{ t('settings.dicts.item_fields.status') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect v-model:value="itemEditor.form.status" name="status" :options="statusOptions" />
              </XhFieldControl>
            </XhFieldRoot>
            <XhFieldRoot class="xh-span-2" :invalid="!!itemEditor.fieldError('description')">
              <XhFieldLabel>{{ t('settings.dicts.item_fields.description') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="itemEditor.form.description" name="description" type="textarea" :rows="2" clearable />
              </XhFieldControl>
              <XhFieldErrorText v-if="itemEditor.fieldError('description')">
                {{ itemEditor.fieldError('description') }}
              </XhFieldErrorText>
            </XhFieldRoot>
            <XhFieldRoot class="xh-span-2" :invalid="!!itemEditor.fieldError('metadata')">
              <XhFieldLabel>{{ t('settings.dicts.item_fields.metadata') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="itemEditor.form.metadata" name="metadata" type="textarea" :rows="3" clearable autocomplete="off" />
              </XhFieldControl>
              <XhFieldErrorText v-if="itemEditor.fieldError('metadata')">
                {{ itemEditor.fieldError('metadata') }}
              </XhFieldErrorText>
            </XhFieldRoot>
            <p v-if="itemEditor.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
              {{ itemEditor.submitError.value }}
            </p>
          </XhFormRoot>
        </XEditModal>
      </SchemaPage>

      <!-- 未选字典：右栏占位，提示先在左侧选一个字典 -->
      <div v-else class="flex h-full flex-col p-3 lg:ps-1.5">
        <XhCardRoot class="dict-mgmt__empty-card" variant="outline">
          <XhCardContent>
            <XhEmptyStateRoot>
              <XhEmptyStateIndicator>
                <Icon icon="lucide:list-tree" width="28" />
              </XhEmptyStateIndicator>
              <XhEmptyStateTitle>{{ t('settings.dicts.select_hint_title') }}</XhEmptyStateTitle>
              <XhEmptyStateDescription>{{ t('settings.dicts.select_hint') }}</XhEmptyStateDescription>
            </XhEmptyStateRoot>
          </XhCardContent>
        </XhCardRoot>
      </div>
    </div>
  </div>
</template>

<style scoped>
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
.dict-mgmt__empty-card {
  --xh-card-p: var(--xh-surface-pad-md) var(--xh-surface-pad-lg);

  flex: 1;
}

/* 主表当前行：淡淡的品牌底 + 名称转品牌色，与勾选态（品牌淡底）拉开一档 */
.dict-mgmt :deep(.dict-row--active) {
  --xh-collection-bg-rest: color-mix(in srgb, hsl(var(--primary)) 6%, hsl(var(--card)));
  --xh-collection-fg-rest: var(--xh-fg-brand);
}

.dict-mgmt__value-block {
  margin: 0;
  max-height: calc(var(--xh-space-8) * 7);
  overflow: auto;
  padding: var(--xh-space-2) var(--xh-space-3);
  border-radius: var(--xh-radius-sm);
  background: var(--xh-bg-subtle);
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-text-caption-size);
  line-height: 1.5;
  white-space: pre-wrap;
  word-break: break-all;
}
</style>
