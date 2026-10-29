<script lang="ts" setup>
import type { StorageConfigForm, StorageConfigRow, StorageType } from './storage-config'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhFieldControl, XhFieldDescription, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSwitch } from '@xihan-ui/vue'
import { computed, reactive, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { renderTag, SchemaPage, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { storageConfigs as storageConfigsApi } from '~/contracts'
import { ApiError } from '~/request'
import {
  configProblemMessage,
  emptyStorageConfigForm,
  findConfigProblem,
  isRemote,
  STORAGE_CONFIG_STATUSES,
  STORAGE_TYPES,
  storageConfigResource,
  storageConfigStatusLabel,
  storageTypeLabel,
  toCreateBody,
  toStorageConfigForm,
  toUpdateBody,
} from './storage-config'

defineOptions({ name: 'FilesStorageConfigsView' })

const { t } = useI18n()
const resource = storageConfigResource(t)

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

const asRow = (row: object) => row as StorageConfigRow
const typeOptions = computed<SelectOption[]>(() => STORAGE_TYPES.map(type => ({ label: storageTypeLabel(t, type), value: type })))
const statusOptions = computed<SelectOption[]>(() => STORAGE_CONFIG_STATUSES.map(status => ({ label: storageConfigStatusLabel(t, status), value: status })))
const defaultOptions = computed<SelectOption[]>(() => [
  { label: t('files.storage_configs.default_options.yes'), value: 'true' },
  { label: t('files.storage_configs.default_options.no'), value: 'false' },
])

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；可排序的只放后端白名单里的） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('files.storage_configs.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('files.storage_configs.placeholders.keyword'), order: 0 },
  { key: 'code', title: t('files.storage_configs.fields.code'), dataType: 'string', sortable: true, minWidth: 150, order: 1 },
  { key: 'name', title: t('files.storage_configs.fields.name'), dataType: 'string', sortable: true, minWidth: 150, order: 2 },
  { key: 'type', title: t('files.storage_configs.fields.type'), dataType: 'enum', searchable: true, searchMultiple: true, sortable: true, options: typeOptions.value, searchPlaceholder: t('files.storage_configs.placeholders.type'), width: 110, order: 3, render: row => renderTag(storageTypeLabel(t, asRow(row).type), 'info') },
  { key: 'bucket', title: t('files.storage_configs.fields.bucket'), dataType: 'string', minWidth: 160, order: 4 },
  { key: 'isDefault', title: t('files.storage_configs.fields.is_default'), dataType: 'enum', searchable: true, options: defaultOptions.value, searchPlaceholder: t('files.storage_configs.placeholders.is_default'), width: 90, order: 5, render: row => (asRow(row).isDefault ? renderTag(t('files.storage_configs.default_tag'), 'warning') : '-') },
  { key: 'status', title: t('files.storage_configs.fields.status'), dataType: 'enum', searchable: true, searchMultiple: true, options: statusOptions.value, searchPlaceholder: t('files.storage_configs.placeholders.status'), width: 90, order: 6, render: row => renderTag(storageConfigStatusLabel(t, asRow(row).status), asRow(row).status === 'Active' ? 'success' : 'danger') },
  { key: 'order', title: t('files.storage_configs.fields.order'), dataType: 'number', sortable: true, width: 80, order: 7 },
  { key: 'createdAt', title: t('files.storage_configs.fields.created_at'), dataType: 'datetime', sortable: true, minWidth: 170, order: 8 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'files.storage-configs',
  pageName: t('files.storage_configs.title'),
  batchRemovable: true,
  removePermission: 'files.storage-configs.delete',
  statusPermission: 'files.storage-configs.update',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('files.storage_configs.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'files.storage-configs.create' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'files.storage-configs.update' },
    // 缺省配置不能停用也不能删除（后端拒绝），4.x 同样不给入口
    { key: 'toggle', title: t('files.storage_configs.actions.toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => toggleConfirmText(asRow(row)), visible: row => !asRow(row).isDefault, permission: 'files.storage-configs.update' },
    { key: 'setDefault', title: t('files.storage_configs.actions.set_default'), scope: 'row', icon: 'lucide:star', confirm: true, confirmText: row => t('files.storage_configs.confirm.set_default', { name: asRow(row).name }), visible: row => !asRow(row).isDefault, disabled: row => asRow(row).status !== 'Active', permission: 'files.storage-configs.update' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => t('files.storage_configs.confirm.delete', { name: asRow(row).name }), visible: row => !asRow(row).isDefault, permission: 'files.storage-configs.delete' },
  ],
}))

function toggleConfirmText(row: StorageConfigRow): string {
  return row.status === 'Active'
    ? t('files.storage_configs.confirm.disable', { name: row.name })
    : t('files.storage_configs.confirm.enable', { name: row.name })
}

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      openCreate()
      break
    case 'edit':
      if (row)
        void openEdit(row)
      break
    case 'toggle':
      if (row)
        void toggleStatus(row)
      break
    case 'setDefault':
      if (row)
        void setDefault(row)
      break
    case 'delete':
      if (row)
        void removeRow(row)
      break
  }
}

// ── 新建 / 编辑 ─────────────────────────────────────────────────
const modalVisible = ref(false)
const submitting = ref(false)
const submitError = ref<null | string>(null)
/** 编辑的配置是否已存了访问密钥：已存的留空即保留，占位提示随之改 */
const editingHasSecret = ref(false)
const form = reactive<StorageConfigForm>(emptyStorageConfigForm())
const modalTitle = computed(() => (form.id ? t('files.storage_configs.form.edit_title') : t('files.storage_configs.form.create_title')))
const remote = computed(() => isRemote(form.type))
const secretPlaceholder = computed(() => (form.id && editingHasSecret.value ? t('files.storage_configs.form.secret_configured') : t('files.storage_configs.form.secret_placeholder')))

function openCreate() {
  Object.assign(form, emptyStorageConfigForm())
  editingHasSecret.value = false
  submitError.value = null
  modalVisible.value = true
}

async function openEdit(row: StorageConfigRow) {
  try {
    const current = await storageConfigsApi.get({ id: row.id })
    Object.assign(form, toStorageConfigForm(current))
    editingHasSecret.value = current.hasSecretAccessKey
    submitError.value = null
    modalVisible.value = true
  }
  catch (error) {
    toast.danger((error as Error).message || t('files.storage_configs.messages.load_detail_failed'))
  }
}

function changeType(value: unknown) {
  form.type = value as StorageType
}

/** 停用的配置不能设为缺省（4.x 同）：关掉启用即一并取消缺省 */
function changeEnabled(enabled: boolean) {
  form.status = enabled ? 'Active' : 'Disabled'
  if (!enabled)
    form.isDefault = false
}

async function onSubmit() {
  const problem = findConfigProblem(form, editingHasSecret.value)
  if (problem) {
    toast.warning(configProblemMessage(t, problem))
    return
  }
  submitting.value = true
  submitError.value = null
  try {
    if (form.id)
      await storageConfigsApi.update(toUpdateBody({ ...form, id: form.id }))
    else
      await storageConfigsApi.create(toCreateBody(form))
    toast.success(t('common.messages.save_success'))
    modalVisible.value = false
    reload()
  }
  catch (error) {
    submitError.value = error instanceof ApiError ? error.detail : String(error)
  }
  finally {
    submitting.value = false
  }
}

// ── 启停 / 设为缺省 / 删除 ──────────────────────────────────────
async function toggleStatus(row: StorageConfigRow) {
  try {
    await resource.updateStatus?.(row.id, row.status !== 'Active')
    toast.success(t('common.messages.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.status_failed'))
  }
}

async function setDefault(row: StorageConfigRow) {
  try {
    await storageConfigsApi.setDefault({ id: row.id })
    toast.success(t('files.storage_configs.messages.set_default_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('files.storage_configs.messages.set_default_failed'))
  }
}

async function removeRow(row: StorageConfigRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <XEditModal v-model:show="modalVisible" :title="modalTitle" :loading="submitting" :form-id="editFormId">
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="storage-config-form" @submit="onSubmit">
        <XhFieldRoot :required="!form.id">
          <XhFieldLabel>{{ t('files.storage_configs.form.code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.code" name="code" clearable :max-length="100" :disabled="Boolean(form.id)" :placeholder="t('files.storage_configs.form.code_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('files.storage_configs.form.name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.name" name="name" clearable :max-length="200" :placeholder="t('files.storage_configs.form.name_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('files.storage_configs.form.type') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect :value="form.type" name="type" :options="typeOptions" @update:value="changeType" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('files.storage_configs.form.order') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="form.order" name="order" :min="0" :max="100000" />
          </XhFieldControl>
        </XhFieldRoot>

        <template v-if="remote">
          <XhFieldRoot required>
            <XhFieldLabel>{{ t('files.storage_configs.form.endpoint') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.endpoint" name="endpoint" clearable :max-length="500" :placeholder="t('files.storage_configs.form.endpoint_placeholder')" />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('files.storage_configs.form.region') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.region" name="region" clearable :max-length="100" :placeholder="t('files.storage_configs.form.region_placeholder')" />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot required>
            <XhFieldLabel>{{ t('files.storage_configs.form.bucket') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.bucket" name="bucket" clearable :max-length="63" :placeholder="t('files.storage_configs.form.bucket_placeholder')" />
            </XhFieldControl>
            <XhFieldDescription>{{ t('files.storage_configs.form.bucket_hint') }}</XhFieldDescription>
          </XhFieldRoot>
          <XhFieldRoot required>
            <XhFieldLabel>{{ t('files.storage_configs.form.access_key_id') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.accessKeyId" name="accessKeyId" clearable :max-length="200" placeholder="AccessKeyId" autocomplete="off" />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot class="xh-span-2" :required="!(form.id && editingHasSecret)">
            <XhFieldLabel>{{ t('files.storage_configs.form.secret_access_key') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.secretAccessKey" name="secretAccessKey" type="password" :max-length="500" autocomplete="new-password" :placeholder="secretPlaceholder" />
            </XhFieldControl>
          </XhFieldRoot>
        </template>
        <XhFieldRoot v-else class="xh-span-2" required>
          <XhFieldLabel>{{ t('files.storage_configs.form.local_bucket') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.bucket" name="bucket" clearable :max-length="63" :placeholder="t('files.storage_configs.form.local_bucket_placeholder')" />
          </XhFieldControl>
          <XhFieldDescription>{{ t('files.storage_configs.form.bucket_hint') }}</XhFieldDescription>
        </XhFieldRoot>

        <template v-if="!form.id">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('files.storage_configs.form.is_enabled') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch :checked="form.status === 'Active'" name="status" @update:checked="changeEnabled" />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('files.storage_configs.form.is_default') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="form.isDefault" name="isDefault" :disabled="form.status !== 'Active'" />
            </XhFieldControl>
          </XhFieldRoot>
        </template>

        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('files.storage_configs.form.remark') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.remark" name="remark" clearable :max-length="500" :placeholder="t('files.storage_configs.form.remark_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <p v-if="submitError" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ submitError }}
        </p>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>
