<script lang="ts" setup>
import type { PositionRow, PositionStatus } from './use-positions'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogFooter, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, Icon, renderTag, SchemaPage, statusConfirmText, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { usePermission } from '~/hooks'
import { formatDate } from '~/utils'
import { positionsResource, usePositionEditor } from './use-positions'

defineOptions({ name: 'OrganizationPositionsView' })

const { t } = useI18n()
const { hasPermission } = usePermission()
const editor = usePositionEditor()
const resource = positionsResource()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

// 状态值是契约字面量，文案键静态写出以便 i18n 门禁核对
function statusLabel(status: PositionStatus): string {
  return status === 'Disabled' ? t('organization.positions.status.disabled') : t('organization.positions.status.active')
}

function statusTone(status: PositionStatus) {
  return status === 'Active' ? 'success' : 'danger'
}

const statusOptions = computed<SelectOption[]>(() => (['Active', 'Disabled'] as const).map(status => ({ label: statusLabel(status), value: status })))

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；order 控顺序；可排序的只放后端白名单里的） ──
// SchemaPage 的行类型在框架边界放宽（Record）：回调里按本页的行类型取用
const asRow = (row: object) => row as PositionRow

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('organization.positions.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('organization.positions.keyword_placeholder'), order: 0 },
  { key: 'name', title: t('organization.positions.fields.name'), dataType: 'string', sortable: true, importable: true, required: true, exportable: true, minWidth: 180, order: 1 },
  { key: 'code', title: t('organization.positions.fields.code'), dataType: 'string', sortable: true, importable: true, required: true, exportable: true, minWidth: 160, order: 2 },
  { key: 'status', title: t('organization.positions.fields.status'), dataType: 'enum', searchable: true, exportable: true, options: statusOptions.value, render: row => renderTag(statusLabel(asRow(row).status), statusTone(asRow(row).status)), width: 100, order: 3 },
  { key: 'order', title: t('organization.positions.fields.order'), dataType: 'number', sortable: true, importable: true, exportable: true, width: 90, order: 4 },
  { key: 'description', title: t('organization.positions.fields.description'), dataType: 'text', visible: false, importable: true, exportable: true, order: 4.5 },
  { key: 'createdAt', title: t('organization.positions.fields.created_at'), dataType: 'datetime', sortable: true, exportable: true, minWidth: 170, order: 5 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'organization.positions',
  pageName: t('organization.positions.title'),
  exportPermission: 'organization.positions.read',
  importPermission: 'organization.positions.create',
  batchRemovable: true,
  removePermission: 'organization.positions.delete',
  statusPermission: 'organization.positions.update',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('organization.positions.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'organization.positions.create' },
    { key: 'view', title: t('common.actions.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'organization.positions.update' },
    { key: 'toggle', title: t('organization.positions.actions.toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => statusConfirmText(t, asRow(row).status === 'Active', asRow(row).name), permission: 'organization.positions.update' },
    // 还被归属引用的由后端逐项拒绝（ORGANIZATION_CONFLICT_004）
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => deleteConfirmText(t, asRow(row).name), permission: 'organization.positions.delete' },
  ],
}))

// ── 行 / 页面操作分发 ──
function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      editor.startCreate()
      break
    case 'view':
      if (row)
        openDetail(row)
      break
    case 'edit':
      if (row)
        editor.startEdit(row)
      break
    case 'toggle':
      if (row)
        void toggleStatus(row)
      break
    case 'delete':
      if (row)
        void removeRow(row)
      break
  }
}

const modalTitle = computed(() => editor.isEdit.value ? t('organization.positions.edit_title') : t('organization.positions.create_title'))

async function onSubmit() {
  if (await editor.submit()) {
    toast.success(t('common.messages.save_success'))
    reload()
  }
}

async function removeRow(row: PositionRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

async function toggleStatus(row: PositionRow) {
  try {
    await resource.updateStatus?.(row.id, row.status !== 'Active')
    toast.success(t('common.messages.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.status_failed'))
  }
}

// ── 查看详情（列表行即完整岗位） ──
const detailOpen = ref(false)
const detail = ref<PositionRow | null>(null)

function openDetail(row: PositionRow) {
  detail.value = row
  detailOpen.value = true
}

function editFromDetail() {
  const row = detail.value
  if (!row)
    return
  detailOpen.value = false
  editor.startEdit(row)
}

function formatNullable(value: unknown): string {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <XhDialogRoot v-model:open="detailOpen">
      <XhDialogContent class="xh-mgmt-detail-modal" style="--xh-dialog-max-w: 640px">
        <XhDialogHeader>
          <XhDialogTitle>
            <div v-if="detail" class="det-hd-entity">
              <div class="det-hd-ico">
                <Icon icon="lucide:briefcase" width="22" height="22" />
              </div>
              <div class="min-w-0">
                <div class="det-hd-name">
                  {{ detail.name }}
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
          <XhDescriptionsRoot v-if="detail" :columns="2" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('organization.positions.fields.code') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.code }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('organization.positions.fields.status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <XhTagRoot variant="subtle" size="sm" :tone="statusTone(detail.status)">
                  <XhTagLabel>{{ statusLabel(detail.status) }}</XhTagLabel>
                </XhTagRoot>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('organization.positions.fields.order') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.order }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('organization.positions.fields.created_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(detail.createdAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem style="grid-column: span 2">
              <XhDescriptionsLabel>{{ t('organization.positions.fields.description') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatNullable(detail.description) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
        </XhDialogBody>
        <XhDialogFooter>
          <XhButton variant="subtle" size="sm" @click="detailOpen = false">
            {{ t('common.actions.close') }}
          </XhButton>
          <XhButton
            v-if="detail && hasPermission('organization.positions.update')"
            variant="subtle"
            size="sm"
            tone="brand"
            @click="editFromDetail"
          >
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
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="position-form" @submit="onSubmit">
        <XhFieldRoot :invalid="!!editor.fieldError('name')" required>
          <XhFieldLabel>{{ t('organization.positions.fields.name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.name" name="name" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('name')">
            {{ editor.fieldError('name') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('code')" :required="!editor.isEdit.value">
          <XhFieldLabel>{{ t('organization.positions.fields.code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.code" name="code" clearable autocomplete="off" :disabled="editor.isEdit.value" :placeholder="t('organization.positions.code_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('code')">
            {{ editor.fieldError('code') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('order')">
          <XhFieldLabel>{{ t('organization.positions.fields.order') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.order" name="order" :min="0" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('order')">
            {{ editor.fieldError('order') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-if="editor.isEdit.value">
          <XhFieldLabel>{{ t('organization.positions.fields.status') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="editor.form.status" name="status" :options="statusOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" :invalid="!!editor.fieldError('description')">
          <XhFieldLabel>{{ t('organization.positions.fields.description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.description" name="description" type="textarea" :rows="3" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('description')">
            {{ editor.fieldError('description') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <p v-if="editor.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ editor.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>
