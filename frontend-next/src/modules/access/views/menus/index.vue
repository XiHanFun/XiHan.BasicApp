<script lang="ts" setup>
import type { Tone } from '@xihan-ui/core'
import type { MenuKind, MenuRow, OperatorKind } from './use-menus'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance, XDataTableColumn } from '~/components'
import type { SelectOption, TreeSelectOption } from '~/types'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogFooter, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldDescription, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSwitch, XhTabsContent, XhTabsIndicator, XhTabsList, XhTabsRoot, XhTabsTrigger, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { actionConfirmText, deleteConfirmText, Icon, IconPicker, renderTag, SchemaPage, XDataTable, XEditModal, XInput, XNumberInput, XSelect, XTreeSelect } from '~/components'
import { toast } from '~/composables'
import { useAuthStore } from '~/stores'
import { CUSTOM_CODE_PREFIX, MENU_KINDS, menusResource, OPERATOR_KINDS, resetMenu, useMenuEditor, useMenuTree } from './use-menus'

defineOptions({ name: 'AccessMenusView' })

const { t, te } = useI18n()
const auth = useAuthStore()
const tree = useMenuTree()
const editor = useMenuEditor(tree.isChildPage)

/** 节点标题：运营节点与改过标题的页面是原文，其余按页面登记的文案键翻译 */
function titleOf(node: MenuRow): string {
  if (node.title)
    return node.title
  return registeredTitle(node)
}

function registeredTitle(node: MenuRow): string {
  return te(node.titleKey) ? t(node.titleKey) : node.titleKey
}

const resource = menusResource(titleOf, tree.setNodes)

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

// 种类是契约字面量，文案键静态写出以便 i18n 门禁核对
function kindLabel(kind: MenuKind): string {
  if (kind === 'Directory')
    return t('access.menus.kinds.directory')
  return kind === 'Link' ? t('access.menus.kinds.link') : t('access.menus.kinds.page')
}

/** 种类标签配色：按层级递减强调，目录最显眼（与 4.x 的菜单类型同一口径），外链最安静 */
function kindTone(kind: MenuKind): Tone {
  if (kind === 'Directory')
    return 'warning'
  return kind === 'Page' ? 'info' : 'neutral'
}

function targetOf(node: MenuRow): string {
  return node.kind === 'Link' ? node.externalUrl ?? '' : node.route
}

function formatBoolean(value: boolean): string {
  return value ? t('common.statuses.yes') : t('common.statuses.no')
}

function formatNullable(value: unknown): string {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

/** 图标 + 文字的单元格；没有图标的行留出同宽空位，标题列的文字才对得齐（render 的节点不带页面 scope，样式用工具类） */
function renderIconCell(node: MenuRow, text: string) {
  return h('span', { class: 'inline-flex items-center gap-1.5' }, [
    node.icon ? h(Icon, { icon: node.icon, width: 16 }) : h('span', { class: 'inline-block w-4' }),
    h('span', text),
  ])
}

const kindOptions = computed<SelectOption[]>(() => MENU_KINDS.map(kind => ({ label: kindLabel(kind), value: kind })))
const operatorKindOptions = computed<SelectOption[]>(() => OPERATOR_KINDS.map(kind => ({ label: kindLabel(kind), value: kind })))
const yesNoOptions = computed(() => [
  { label: t('common.statuses.yes'), value: 'true' },
  { label: t('common.statuses.no'), value: 'false' },
])

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；order 控顺序）。整树在本地筛选，后端不排序，故不声明 sortable ──
// SchemaPage 的行类型在框架边界放宽（Record）：回调里按本页的行类型取用
const asRow = (row: object) => row as MenuRow

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('access.menus.search'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('access.menus.keyword_placeholder'), order: 0 },
  { key: 'title', title: t('access.menus.fields.title'), dataType: 'string', treeColumn: true, exportable: true, render: row => renderIconCell(asRow(row), titleOf(asRow(row))), minWidth: 200, order: 1 },
  { key: 'code', title: t('access.menus.fields.code'), dataType: 'string', exportable: true, minWidth: 160, order: 2 },
  { key: 'kind', title: t('access.menus.fields.kind'), dataType: 'enum', searchable: true, exportable: true, options: kindOptions.value, render: row => renderTag(kindLabel(asRow(row).kind), kindTone(asRow(row).kind)), width: 90, order: 3 },
  { key: 'route', title: t('access.menus.fields.target'), dataType: 'string', exportable: true, render: row => formatNullable(targetOf(asRow(row))), ellipsis: true, minWidth: 200, order: 4 },
  { key: 'icon', title: t('access.menus.fields.icon'), dataType: 'string', render: row => (asRow(row).icon ? renderIconCell(asRow(row), asRow(row).icon!) : '-'), minWidth: 160, order: 5 },
  { key: 'permission', title: t('access.menus.fields.permission'), dataType: 'string', exportable: true, minWidth: 180, order: 6 },
  { key: 'isVisible', title: t('access.menus.fields.visible'), dataType: 'boolean', advancedSearch: true, exportable: true, options: yesNoOptions.value, width: 90, order: 7 },
  { key: 'keepAlive', title: t('access.menus.fields.keep_alive'), dataType: 'boolean', exportable: true, width: 90, order: 8 },
  { key: 'isAffix', title: t('access.menus.fields.affix'), dataType: 'boolean', exportable: true, width: 90, order: 9 },
  { key: 'isCustomized', title: t('access.menus.fields.customized'), dataType: 'boolean', advancedSearch: true, exportable: true, options: yesNoOptions.value, render: row => (asRow(row).kind === 'Page' ? renderTag(formatBoolean(asRow(row).isCustomized), asRow(row).isCustomized ? 'info' : 'neutral') : '-'), width: 90, order: 10 },
  { key: 'order', title: t('access.menus.fields.order'), dataType: 'number', exportable: true, width: 80, order: 11 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'access.menus',
  pageName: t('access.menus.title'),
  exportPermission: 'access.menus.read',
  batchRemovable: true,
  removePermission: 'access.menus.delete',
  rowKey: 'id',
  tree: { childrenKey: 'children', defaultExpandAll: false },
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('access.menus.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'access.menus.create' },
    // 目录下只能挂外链（子目录不允许，页面来自登记）
    { key: 'addLink', title: t('access.menus.actions.add_link'), scope: 'row', icon: 'lucide:link', visible: row => asRow(row).kind === 'Directory', permission: 'access.menus.create' },
    { key: 'view', title: t('common.actions.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'access.menus.update' },
    { key: 'reset', title: t('access.menus.actions.reset'), scope: 'row', icon: 'lucide:rotate-ccw', confirm: true, confirmText: row => actionConfirmText(t, t('access.menus.actions.reset'), titleOf(asRow(row))), visible: row => asRow(row).kind === 'Page' && asRow(row).isCustomized, permission: 'access.menus.update' },
    // 页面节点来自登记，不能删；还有子节点的目录由后端拒绝
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => deleteConfirmText(t, titleOf(asRow(row))), visible: row => asRow(row).kind !== 'Page', permission: 'access.menus.delete' },
  ],
}))

// ── 行 / 页面操作分发 ──
function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      editor.startCreate()
      break
    case 'addLink':
      if (row)
        editor.startCreate('Link', row.id)
      break
    case 'view':
      if (row)
        openDetail(row)
      break
    case 'edit':
      if (row)
        editor.startEdit(row)
      break
    case 'reset':
      if (row)
        void restoreDefaults(row)
      break
    case 'delete':
      if (row)
        void removeRow(row)
      break
  }
}

const modalTitle = computed(() => {
  if (editor.isEdit.value)
    return t('access.menus.edit_title')
  return editor.form.kind === 'Link' ? t('access.menus.create_link_title') : t('access.menus.create_title')
})

/** 所属目录的候选：顶层不选即可；子页面只出它登记的父页面（只读） */
const parentOptions = computed<TreeSelectOption[]>(() => {
  const current = editor.editing.value
  if (current && tree.isChildPage(current)) {
    const parent = tree.find(current.parentId)
    return parent ? [{ value: parent.id, label: titleOf(parent) }] : []
  }
  return tree.directories.value.filter(directory => directory.id !== current?.id).map(directory => ({ value: directory.id, label: titleOf(directory) }))
})

const parentValue = computed({
  get: () => editor.form.parentId,
  set: (value: null | number | string) => {
    editor.form.parentId = value === null || value === '' ? null : String(value)
  },
})

function onKindChange(value: unknown) {
  editor.changeKind(value as OperatorKind)
}

async function onSubmit() {
  if (await editor.submit()) {
    toast.success(t('common.messages.save_success'))
    reload()
  }
}

async function restoreDefaults(row: MenuRow) {
  try {
    await resetMenu(row.id)
    toast.success(t('access.menus.reset_done'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.operation_failed'))
  }
}

async function removeRow(row: MenuRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

// ── 查看详情 ──
const detailOpen = ref(false)
const detail = ref<MenuRow | null>(null)

function openDetail(row: MenuRow) {
  detail.value = row
  detailOpen.value = true
}

function editFromDetail() {
  const row = detail.value
  detailOpen.value = false
  if (row)
    editor.startEdit(row)
}

const childColumns = computed<XDataTableColumn<MenuRow>[]>(() => [
  { title: t('access.menus.fields.title'), key: 'title', minWidth: 120, ellipsis: true, render: row => titleOf(row) },
  { title: t('access.menus.fields.code'), key: 'code', width: 160, ellipsis: true },
  { title: t('access.menus.fields.kind'), key: 'kind', width: 80, render: row => h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: kindTone(row.kind) }, () => h(XhTagLabel, null, () => kindLabel(row.kind))) },
  { title: t('access.menus.fields.target'), key: 'route', minWidth: 120, ellipsis: true, render: row => formatNullable(targetOf(row)) },
])
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <XhDialogRoot v-model:open="detailOpen">
      <XhDialogContent class="xh-mgmt-detail-modal" style="--xh-dialog-max-w: 720px">
        <XhDialogHeader>
          <XhDialogTitle>
            <div v-if="detail" class="det-hd-entity">
              <div class="det-hd-ico">
                <Icon :icon="detail.icon || 'lucide:menu'" width="22" height="22" />
              </div>
              <div class="min-w-0">
                <div class="det-hd-name">
                  {{ titleOf(detail) }}
                </div>
                <div class="det-hd-sub">
                  {{ detail.code }}
                </div>
              </div>
            </div>
          </XhDialogTitle>
        </XhDialogHeader>
        <XhDialogCloseTrigger />
        <XhDialogBody class="xh-overlay-body">
          <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
          <XhTabsRoot v-if="detail" default-value="overview" variant="line">
            <XhTabsList>
              <XhTabsTrigger value="overview">
                {{ t('access.menus.tabs.overview') }}
              </XhTabsTrigger>
              <XhTabsTrigger value="children">
                {{ t('access.menus.tabs.children', { count: detail.children.length }) }}
              </XhTabsTrigger>
              <XhTabsIndicator />
            </XhTabsList>
            <XhTabsContent value="overview">
              <p v-if="detail.kind === 'Page'" class="menu-page-hint">
                {{ t('access.menus.page_hint') }}
              </p>
              <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('access.menus.fields.kind') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    <XhTagRoot variant="subtle" size="sm" :tone="kindTone(detail.kind)">
                      <XhTagLabel>{{ kindLabel(detail.kind) }}</XhTagLabel>
                    </XhTagRoot>
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('access.menus.fields.package') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ detail.package }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('access.menus.fields.route') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatNullable(detail.route) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('access.menus.fields.component') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatNullable(detail.component) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('access.menus.fields.title_key') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatNullable(detail.titleKey) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('access.menus.fields.title_override') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatNullable(detail.title) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('access.menus.fields.icon') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    <span v-if="detail.icon" class="inline-flex items-center gap-1.5">
                      <Icon :icon="detail.icon" width="16" />
                      <span>{{ detail.icon }}</span>
                    </span>
                    <span v-else>-</span>
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('access.menus.fields.permission') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatNullable(detail.permission) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('access.menus.fields.visible') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatBoolean(detail.isVisible) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('access.menus.fields.keep_alive') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatBoolean(detail.keepAlive) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('access.menus.fields.affix') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatBoolean(detail.isAffix) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('access.menus.fields.customized') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ detail.kind === 'Page' ? formatBoolean(detail.isCustomized) : '-' }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('access.menus.fields.order') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ detail.order }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('access.menus.fields.parent') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ tree.find(detail.parentId) ? titleOf(tree.find(detail.parentId)!) : t('access.menus.top_level') }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <!-- 跨列项排在末尾：边框按子节点奇偶画竖线，跨列项之后的项奇偶会与实际列位错开 -->
                <XhDescriptionsItem v-if="detail.kind === 'Link'" style="grid-column: span 2">
                  <XhDescriptionsLabel>{{ t('access.menus.fields.url') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatNullable(detail.externalUrl) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
              </XhDescriptionsRoot>
            </XhTabsContent>
            <XhTabsContent value="children">
              <div class="xh-detail-table-wrap">
                <XDataTable
                  v-if="detail.children.length"
                  :columns="childColumns"
                  :data="detail.children"
                  row-key="id"
                />
                <XhEmptyStateRoot v-else size="sm" class="menu-children-empty">
                  <XhEmptyStateIndicator>
                    <Icon icon="lucide:inbox" width="24" />
                  </XhEmptyStateIndicator>
                  <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('access.menus.no_children') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
              </div>
            </XhTabsContent>
          </XhTabsRoot>
        </XhDialogBody>
        <XhDialogFooter>
          <XhButton variant="subtle" size="sm" @click="detailOpen = false">
            {{ t('common.actions.close') }}
          </XhButton>
          <XhButton v-if="detail && auth.can('access.menus.update')" variant="subtle" size="sm" tone="brand" @click="editFromDetail">
            {{ t('common.actions.edit') }}
          </XhButton>
        </XhDialogFooter>
      </XhDialogContent>
    </XhDialogRoot>

    <XEditModal
      v-model:show="editor.open.value"
      :title="modalTitle"
      :loading="editor.submitting.value"
      :form-id="editFormId"
    >
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="menu-form" @submit="onSubmit">
        <p v-if="editor.isPage.value" class="xh-span-2 menu-page-hint">
          {{ t('access.menus.page_hint') }}
        </p>
        <XhFieldRoot :invalid="!!editor.fieldError('kind')">
          <XhFieldLabel>{{ t('access.menus.fields.kind') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-if="!editor.isEdit.value" :value="editor.form.kind" name="kind" :options="operatorKindOptions" @update:value="onKindChange" />
            <XSelect v-else :value="editor.form.kind" name="kind" :options="kindOptions" disabled />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('kind')">
            {{ editor.fieldError('kind') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('code')" :required="!editor.isEdit.value">
          <XhFieldLabel>{{ t('access.menus.fields.code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.code" name="code" clearable autocomplete="off" :disabled="editor.isEdit.value" :placeholder="t('access.menus.code_placeholder')" />
          </XhFieldControl>
          <XhFieldDescription v-if="!editor.isEdit.value">
            {{ t('access.menus.code_hint', { prefix: CUSTOM_CODE_PREFIX }) }}
          </XhFieldDescription>
          <XhFieldErrorText v-if="editor.fieldError('code')">
            {{ editor.fieldError('code') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('title')" :required="!editor.isPage.value">
          <XhFieldLabel>{{ t('access.menus.fields.title') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.title" name="title" clearable :placeholder="editor.isPage.value && editor.editing.value ? registeredTitle(editor.editing.value) : t('access.menus.title_placeholder')" />
          </XhFieldControl>
          <XhFieldDescription v-if="editor.isPage.value">
            {{ t('access.menus.title_override_hint') }}
          </XhFieldDescription>
          <XhFieldErrorText v-if="editor.fieldError('title')">
            {{ editor.fieldError('title') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('parentId')">
          <XhFieldLabel>{{ t('access.menus.fields.parent') }}</XhFieldLabel>
          <XhFieldControl>
            <XTreeSelect v-model:value="parentValue" name="parentId" :options="parentOptions" clearable :disabled="editor.parentLocked.value" :placeholder="t('access.menus.top_level')" />
          </XhFieldControl>
          <XhFieldDescription v-if="editor.form.kind === 'Directory'">
            {{ t('access.menus.directory_top_level_hint') }}
          </XhFieldDescription>
          <XhFieldDescription v-else-if="editor.parentLocked.value">
            {{ t('access.menus.registered_parent_hint') }}
          </XhFieldDescription>
          <XhFieldErrorText v-if="editor.fieldError('parentId')">
            {{ editor.fieldError('parentId') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('icon')">
          <XhFieldLabel>{{ t('access.menus.fields.icon') }}</XhFieldLabel>
          <XhFieldControl :as-child="false">
            <IconPicker v-model="editor.form.icon" :placeholder="t('access.menus.icon_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('icon')">
            {{ editor.fieldError('icon') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('order')">
          <XhFieldLabel>{{ t('access.menus.fields.order') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.order" name="order" :min="0" :max="100000" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('order')">
            {{ editor.fieldError('order') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-if="editor.form.kind === 'Link'" class="xh-span-2" :invalid="!!editor.fieldError('externalUrl')" required>
          <XhFieldLabel>{{ t('access.menus.fields.url') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.externalUrl" name="externalUrl" type="url" clearable autocomplete="off" placeholder="https://" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('externalUrl')">
            {{ editor.fieldError('externalUrl') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-if="!editor.isPage.value" class="xh-span-2" :invalid="!!editor.fieldError('permission')">
          <XhFieldLabel>{{ t('access.menus.fields.permission') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.permission" name="permission" clearable autocomplete="off" :placeholder="t('access.menus.permission_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('permission')">
            {{ editor.fieldError('permission') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('access.menus.fields.visible') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="editor.form.isVisible" name="isVisible" />
          </XhFieldControl>
        </XhFieldRoot>
        <template v-if="editor.isPage.value">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('access.menus.fields.keep_alive') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="editor.form.keepAlive" name="keepAlive" />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('access.menus.fields.affix') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="editor.form.isAffix" name="isAffix" />
            </XhFieldControl>
          </XhFieldRoot>
        </template>
        <p v-if="editor.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ editor.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>

<style scoped>
.menu-page-hint {
  margin: 0 0 var(--xh-space-2);
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.menu-children-empty {
  padding-block: var(--xh-space-6);
}
</style>
