<script lang="ts" setup>
import type { ConfigDataType, ConfigRow, ConfigStatus } from './use-configs'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogFooter, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSwitch } from '@xihan-ui/vue'
import { computed, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, Icon, renderTag, SchemaPage, statusConfirmText, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { useAuthStore } from '~/stores'
import { formatDate } from '~/utils'
import { CONFIG_DATA_TYPES, configsResource, useConfigEditor } from './use-configs'

defineOptions({ name: 'SettingsConfigsView' })

const { t } = useI18n()
const auth = useAuthStore()
const isPlatformCaller = () => auth.tenantId === null
const editor = useConfigEditor(isPlatformCaller)
const resource = configsResource()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对
function dataTypeLabel(type: ConfigDataType): string {
  switch (type) {
    case 'Number': return t('settings.configs.data_types.number')
    case 'Boolean': return t('settings.configs.data_types.boolean')
    case 'Json': return t('settings.configs.data_types.json')
    case 'Array': return t('settings.configs.data_types.array')
    default: return t('settings.configs.data_types.text')
  }
}

function statusLabel(status: ConfigStatus): string {
  return status === 'Disabled' ? t('settings.configs.status.disabled') : t('settings.configs.status.active')
}

function ownerLabel(row: ConfigRow): string {
  return row.isPlatform ? t('settings.configs.owner.platform') : t('settings.configs.owner.tenant')
}

function valueText(row: ConfigRow, value: null | string | undefined): string {
  if (row.isEncrypted)
    return row.hasValue ? t('settings.configs.encrypted_value') : ''
  return value ?? ''
}

const dataTypeOptions = computed<SelectOption[]>(() => CONFIG_DATA_TYPES.map(type => ({ label: dataTypeLabel(type), value: type })))
const statusOptions = computed<SelectOption[]>(() => (['Active', 'Disabled'] as const).map(status => ({ label: statusLabel(status), value: status })))
const ownerOptions = computed(() => [
  { label: t('settings.configs.owner.platform'), value: 'platform' },
  { label: t('settings.configs.owner.tenant'), value: 'tenant' },
])
const yesNoOptions = computed(() => [
  { label: t('common.statuses.yes'), value: 'true' },
  { label: t('common.statuses.no'), value: 'false' },
])

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；order 控顺序；可排序的只放后端白名单里的） ──
// SchemaPage 的行类型在框架边界放宽（Record）：回调里按本页的行类型取用
const asRow = (row: object) => row as ConfigRow

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('settings.configs.search'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('settings.configs.keyword_placeholder'), order: 0 },
  { key: 'name', title: t('settings.configs.fields.name'), dataType: 'string', sortable: true, importable: true, required: true, exportable: true, minWidth: 160, order: 1 },
  { key: 'key', title: t('settings.configs.fields.key'), dataType: 'string', sortable: true, importable: true, required: true, exportable: true, minWidth: 180, order: 2 },
  { key: 'group', title: t('settings.configs.fields.group'), dataType: 'string', sortable: true, advancedSearch: true, importable: true, exportable: true, minWidth: 100, order: 3 },
  { key: 'dataType', title: t('settings.configs.fields.data_type'), dataType: 'enum', searchable: true, importable: true, exportable: true, options: dataTypeOptions.value, width: 110, order: 4 },
  { key: 'value', title: t('settings.configs.fields.value'), dataType: 'text', importable: true, render: row => valueText(asRow(row), asRow(row).value), ellipsis: true, minWidth: 160, order: 5 },
  { key: 'defaultValue', title: t('settings.configs.fields.default_value'), dataType: 'text', visible: false, importable: true, order: 5.5 },
  { key: 'owner', title: t('settings.configs.fields.owner'), dataType: 'enum', searchable: true, options: ownerOptions.value, render: row => ownerLabel(asRow(row)), width: 90, order: 6 },
  { key: 'isEncrypted', title: t('settings.configs.fields.encrypted'), dataType: 'boolean', advancedSearch: true, options: yesNoOptions.value, width: 90, order: 7 },
  { key: 'isBuiltIn', title: t('settings.configs.built_in'), dataType: 'boolean', width: 80, order: 8 },
  { key: 'allowTenantOverride', title: t('settings.configs.fields.allow_tenant_override'), dataType: 'boolean', width: 120, order: 9 },
  { key: 'status', title: t('settings.configs.fields.status'), dataType: 'enum', searchable: true, options: statusOptions.value, render: row => renderTag(statusLabel(asRow(row).status), asRow(row).status === 'Active' ? 'success' : 'danger'), width: 90, order: 10 },
  { key: 'order', title: t('settings.configs.fields.order'), dataType: 'number', sortable: true, importable: true, exportable: true, width: 80, order: 11 },
  { key: 'description', title: t('settings.configs.fields.description'), dataType: 'text', visible: false, importable: true, order: 11.5 },
  { key: 'createdAt', title: t('settings.configs.fields.created_at'), dataType: 'datetime', sortable: true, exportable: true, minWidth: 170, order: 12 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'settings.configs',
  pageName: t('settings.configs.title'),
  exportPermission: 'settings.configs.read',
  importPermission: 'settings.configs.create',
  batchRemovable: true,
  removePermission: 'settings.configs.delete',
  statusPermission: 'settings.configs.update',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('settings.configs.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'settings.configs.create' },
    { key: 'view', title: t('common.actions.view'), scope: 'row', icon: 'lucide:eye' },
    // 另一侧的参数只读：平台看得见租户参数、租户看得见平台参数，都改不了
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', visible: row => editor.isOwn(asRow(row)), permission: 'settings.configs.update' },
    // 租户覆盖平台参数：同键新建一条本租户的参数
    { key: 'override', title: t('settings.configs.actions.override'), scope: 'row', icon: 'lucide:copy-plus', visible: row => editor.canOverride(asRow(row)), permission: 'settings.configs.create' },
    { key: 'toggle', title: t('settings.configs.actions.toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => statusConfirmText(t, asRow(row).status === 'Active', asRow(row).name), visible: row => editor.isOwn(asRow(row)), permission: 'settings.configs.update' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => deleteConfirmText(t, asRow(row).name), visible: row => editor.isOwn(asRow(row)) && !asRow(row).isBuiltIn, permission: 'settings.configs.delete' },
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
    case 'override':
      if (row)
        editor.startOverride(row)
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

const modalTitle = computed(() => {
  switch (editor.mode.value) {
    case 'edit': return t('settings.configs.edit_title')
    case 'override': return t('settings.configs.override_title')
    default: return t('settings.configs.create_title')
  }
})

async function onSubmit() {
  if (await editor.submit()) {
    toast.success(t('common.messages.save_success'))
    reload()
  }
}

async function removeRow(row: ConfigRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

async function toggleStatus(row: ConfigRow) {
  try {
    await resource.updateStatus?.(row.id, row.status !== 'Active')
    toast.success(t('common.messages.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.status_failed'))
  }
}

// ── 查看详情 ──
const detailOpen = ref(false)
const detail = ref<ConfigRow | null>(null)

function openDetail(row: ConfigRow) {
  detail.value = row
  detailOpen.value = true
}

function formatNullable(value: unknown): string {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatBoolean(value: boolean): string {
  return value ? t('common.statuses.yes') : t('common.statuses.no')
}
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
                <Icon icon="lucide:sliders-horizontal" width="22" height="22" />
              </div>
              <div class="min-w-0">
                <div class="det-hd-name">
                  {{ detail.name }}
                </div>
                <div class="det-hd-sub">
                  {{ detail.key }}
                </div>
              </div>
            </div>
          </XhDialogTitle>
        </XhDialogHeader>
        <XhDialogCloseTrigger />
        <XhDialogBody class="xh-overlay-body">
          <XhDescriptionsRoot v-if="detail" :columns="2" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.configs.fields.group') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatNullable(detail.group) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.configs.fields.data_type') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ dataTypeLabel(detail.dataType) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.configs.fields.owner') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ ownerLabel(detail) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.configs.fields.status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ statusLabel(detail.status) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.configs.fields.encrypted') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatBoolean(detail.isEncrypted) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.configs.built_in') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatBoolean(detail.isBuiltIn) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.configs.fields.allow_tenant_override') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatBoolean(detail.allowTenantOverride) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.configs.fields.order') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.order }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.configs.fields.created_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(detail.createdAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.configs.fields.modified_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.modifiedAt ? formatDate(detail.modifiedAt) : '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <!-- 跨列项排在末尾：边框按子节点奇偶画竖线，跨列项之后的项奇偶会与实际列位错开 -->
            <XhDescriptionsItem style="grid-column: span 2">
              <XhDescriptionsLabel>{{ t('settings.configs.fields.value') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <span v-if="detail.isEncrypted" class="text-caption text-fg-muted">{{ t('settings.configs.encrypted_hint') }}</span>
                <pre v-else class="config-value-block">{{ formatNullable(detail.value) }}</pre>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem style="grid-column: span 2">
              <XhDescriptionsLabel>{{ t('settings.configs.fields.default_value') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <pre class="config-value-block">{{ formatNullable(detail.defaultValue) }}</pre>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem style="grid-column: span 2">
              <XhDescriptionsLabel>{{ t('settings.configs.fields.description') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatNullable(detail.description) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
        </XhDialogBody>
        <XhDialogFooter>
          <XhButton variant="subtle" size="sm" @click="detailOpen = false">
            {{ t('common.actions.close') }}
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
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="config-form" @submit="onSubmit">
        <XhFieldRoot :invalid="!!editor.fieldError('name')" required>
          <XhFieldLabel>{{ t('settings.configs.fields.name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.name" name="name" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('name')">
            {{ editor.fieldError('name') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('key')" :required="!editor.isEdit.value">
          <XhFieldLabel>{{ t('settings.configs.fields.key') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.key" name="key" clearable autocomplete="off" :disabled="editor.mode.value !== 'create'" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('key')">
            {{ editor.fieldError('key') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('group')">
          <XhFieldLabel>{{ t('settings.configs.fields.group') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.group" name="group" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('group')">
            {{ editor.fieldError('group') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('dataType')">
          <XhFieldLabel>{{ t('settings.configs.fields.data_type') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="editor.form.dataType" name="dataType" :options="dataTypeOptions" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('dataType')">
            {{ editor.fieldError('dataType') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" :invalid="!!editor.fieldError('value')">
          <XhFieldLabel>{{ t('settings.configs.fields.value') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="editor.form.value"
              name="value"
              type="textarea"
              :rows="4"
              clearable
              autocomplete="off"
              :placeholder="editor.isEdit.value && editor.form.isEncrypted ? t('settings.configs.encrypted_hint') : undefined"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('value')">
            {{ editor.fieldError('value') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" :invalid="!!editor.fieldError('defaultValue')">
          <XhFieldLabel>{{ t('settings.configs.fields.default_value') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.defaultValue" name="defaultValue" type="textarea" :rows="3" clearable autocomplete="off" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('defaultValue')">
            {{ editor.fieldError('defaultValue') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('settings.configs.fields.encrypted') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="editor.form.isEncrypted" name="isEncrypted" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-if="isPlatformCaller()">
          <XhFieldLabel>{{ t('settings.configs.fields.allow_tenant_override') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="editor.form.allowTenantOverride" name="allowTenantOverride" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('order')">
          <XhFieldLabel>{{ t('settings.configs.fields.order') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.order" name="order" :min="0" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('order')">
            {{ editor.fieldError('order') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-if="editor.isEdit.value">
          <XhFieldLabel>{{ t('settings.configs.fields.status') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="editor.form.status" name="status" :options="statusOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" :invalid="!!editor.fieldError('description')">
          <XhFieldLabel>{{ t('settings.configs.fields.description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.description" name="description" type="textarea" :rows="2" clearable />
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

<style scoped>
.config-value-block {
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
