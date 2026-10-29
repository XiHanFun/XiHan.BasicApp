<script lang="ts" setup>
import type { SmsConfigForm, SmsConfigRow } from './sms-config'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SmsProvider } from '~/contracts'
import type { SelectOption } from '~/types'
import { XhFieldControl, XhFieldDescription, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSwitch } from '@xihan-ui/vue'
import { computed, reactive, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { renderTag, SchemaPage, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { SmsProvider as PROVIDERS, smsConfigs as smsConfigsApi } from '~/contracts'
import { enabledLabel, enabledOptions, errorText, smsProviderLabel } from '../../labels'
import { emptySmsConfigForm, findSmsConfigProblem, needsSdkAppId, smsConfigProblemMessage, smsConfigResource, toCreateBody, toSmsConfigForm, toUpdateBody } from './sms-config'

defineOptions({ name: 'NotificationSmsConfigsView' })

const { t } = useI18n()
const resource = smsConfigResource(t)

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

const asRow = (row: object) => row as SmsConfigRow
const providerOptions = computed<SelectOption[]>(() => PROVIDERS.map(provider => ({ label: smsProviderLabel(t, provider), value: provider })))
const statusOptions = computed<SelectOption[]>(() => enabledOptions(t))
/** 模板映射的示例：JSON 不进文案（花括号与引号要转义），各区域同一份 */
const TEMPLATE_MAP_EXAMPLE = '{ "auth-sms-login-code": "SMS_123456789" }'

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；可排序的只放后端白名单里的：排序、创建时间） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('notification.sms_configs.columns.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('notification.sms_configs.columns.keyword_placeholder'), order: 0 },
  { key: 'code', title: t('notification.sms_configs.columns.config_code'), dataType: 'string', minWidth: 140, order: 1 },
  { key: 'name', title: t('notification.sms_configs.columns.config_name'), dataType: 'string', minWidth: 140, order: 2 },
  { key: 'provider', title: t('notification.sms_configs.columns.provider'), dataType: 'enum', searchable: true, searchMultiple: true, options: providerOptions.value, searchPlaceholder: t('notification.sms_configs.columns.provider_placeholder'), width: 110, order: 3, render: row => renderTag(smsProviderLabel(t, asRow(row).provider), 'info') },
  { key: 'signName', title: t('notification.sms_configs.columns.sign_name'), dataType: 'string', minWidth: 120, order: 4 },
  { key: 'isDefault', title: t('notification.sms_configs.columns.is_default'), dataType: 'enum', width: 90, order: 5, render: row => (asRow(row).isDefault ? renderTag(t('notification.sms_configs.tag.default'), 'warning') : '-') },
  { key: 'isEnabled', title: t('notification.sms_configs.columns.status'), dataType: 'enum', searchable: true, options: statusOptions.value, searchPlaceholder: t('notification.sms_configs.columns.status_placeholder'), width: 90, order: 6, render: row => renderTag(enabledLabel(t, asRow(row).isEnabled), asRow(row).isEnabled ? 'success' : 'danger') },
  { key: 'order', title: t('notification.sms_configs.columns.sort'), dataType: 'number', sortable: true, width: 80, order: 7 },
  { key: 'createdAt', title: t('notification.sms_configs.columns.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 8 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'notification.sms-configs',
  pageName: t('notification.sms_configs.title'),
  statusPermission: 'notification.sms-configs.update',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('notification.sms_configs.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'notification.sms-configs.create' },
    { key: 'edit', title: t('notification.sms_configs.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'notification.sms-configs.update' },
    // 缺省配置不能停用也不能删除（后端拒绝），入口随之收起；停用的不能设为缺省
    { key: 'toggle', title: t('notification.sms_configs.actions.toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => toggleConfirmText(asRow(row)), visible: row => !asRow(row).isDefault, permission: 'notification.sms-configs.update' },
    { key: 'setDefault', title: t('notification.sms_configs.actions.set_default'), scope: 'row', icon: 'lucide:star', confirm: true, confirmText: row => t('notification.sms_configs.message.set_default_content', { name: asRow(row).name }), visible: row => !asRow(row).isDefault, disabled: row => !asRow(row).isEnabled, permission: 'notification.sms-configs.update' },
    { key: 'delete', title: t('notification.sms_configs.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => t('notification.sms_configs.message.delete_content', { name: asRow(row).name }), visible: row => !asRow(row).isDefault, permission: 'notification.sms-configs.delete' },
  ],
}))

function toggleConfirmText(row: SmsConfigRow): string {
  return row.isEnabled
    ? t('notification.sms_configs.message.disable_content', { name: row.name })
    : t('notification.sms_configs.message.enable_content', { name: row.name })
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
const form = reactive<SmsConfigForm>(emptySmsConfigForm())
const modalTitle = computed(() => (form.id ? t('notification.sms_configs.form.edit_title') : t('notification.sms_configs.form.add_title')))
const tencent = computed(() => needsSdkAppId(form.provider))
const secretPlaceholder = computed(() => (form.id && editingHasSecret.value ? t('notification.sms_configs.form.secret_configured') : t('notification.sms_configs.form.secret_placeholder')))

function openCreate() {
  Object.assign(form, emptySmsConfigForm())
  editingHasSecret.value = false
  submitError.value = null
  modalVisible.value = true
}

async function openEdit(row: SmsConfigRow) {
  try {
    const current = await smsConfigsApi.get({ id: row.id })
    Object.assign(form, toSmsConfigForm(current))
    editingHasSecret.value = current.hasAccessKeySecret
    submitError.value = null
    modalVisible.value = true
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.sms_configs.message.load_detail_failed'))
  }
}

function changeProvider(value: unknown) {
  form.provider = value as SmsProvider
}

async function onSubmit() {
  const problem = findSmsConfigProblem(form, editingHasSecret.value)
  if (problem) {
    toast.warning(smsConfigProblemMessage(t, problem))
    return
  }
  submitting.value = true
  submitError.value = null
  try {
    if (form.id)
      await smsConfigsApi.update(toUpdateBody({ ...form, id: form.id }))
    else
      await smsConfigsApi.create(toCreateBody(form))
    toast.success(t('notification.sms_configs.message.save_success'))
    modalVisible.value = false
    reload()
  }
  catch (error) {
    submitError.value = errorText(error) || t('notification.sms_configs.message.save_failed')
  }
  finally {
    submitting.value = false
  }
}

// ── 启停 / 设为缺省 / 删除 ──────────────────────────────────────
async function toggleStatus(row: SmsConfigRow) {
  try {
    await resource.updateStatus?.(row.id, !row.isEnabled)
    toast.success(t('notification.sms_configs.message.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.sms_configs.message.status_update_failed'))
  }
}

async function setDefault(row: SmsConfigRow) {
  try {
    await smsConfigsApi.setDefault({ id: row.id })
    toast.success(t('notification.sms_configs.message.set_default_success'))
    reload()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.sms_configs.message.set_default_failed'))
  }
}

async function removeRow(row: SmsConfigRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('notification.sms_configs.message.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.sms_configs.message.delete_failed'))
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <XEditModal v-model:show="modalVisible" :title="modalTitle" :loading="submitting" :form-id="editFormId">
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="sms-config-form" @submit="onSubmit">
        <XhFieldRoot :required="!form.id">
          <XhFieldLabel>{{ t('notification.sms_configs.form.config_code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.code" name="code" clearable :max-length="64" :disabled="Boolean(form.id)" :placeholder="t('notification.sms_configs.form.config_code_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('notification.sms_configs.form.config_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.name" name="name" clearable :max-length="128" :placeholder="t('notification.sms_configs.form.config_name_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('notification.sms_configs.form.provider') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect :value="form.provider" name="provider" :options="providerOptions" @update:value="changeProvider" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('notification.sms_configs.form.sign_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.signName" name="signName" clearable :max-length="64" :placeholder="t('notification.sms_configs.form.sign_name_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('notification.sms_configs.form.access_key_id') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.accessKeyId" name="accessKeyId" clearable :max-length="256" autocomplete="off" :placeholder="t('notification.sms_configs.form.access_key_id_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot :required="!(form.id && editingHasSecret)">
          <XhFieldLabel>{{ t('notification.sms_configs.form.access_key_secret') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.accessKeySecret" name="accessKeySecret" type="password" :max-length="512" autocomplete="new-password" :placeholder="secretPlaceholder" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-if="tencent" required>
          <XhFieldLabel>{{ t('notification.sms_configs.form.sdk_app_id') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.sdkAppId" name="sdkAppId" clearable :max-length="64" :placeholder="t('notification.sms_configs.form.sdk_app_id_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.sms_configs.form.region') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.region" name="region" clearable :max-length="64" :placeholder="tencent ? t('notification.sms_configs.form.region_placeholder_tencent') : t('notification.sms_configs.form.region_placeholder_aliyun')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('notification.sms_configs.form.template_map') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.templateMap" name="templateMap" type="textarea" :rows="4" :max-length="20000" :placeholder="TEMPLATE_MAP_EXAMPLE" />
          </XhFieldControl>
          <XhFieldDescription>{{ t('notification.sms_configs.form.template_map_hint') }}</XhFieldDescription>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.sms_configs.form.sort') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="form.order" name="order" :min="0" :max="100000" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-if="!form.id">
          <XhFieldLabel>{{ t('notification.sms_configs.form.is_enabled') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="form.isEnabled" name="isEnabled" />
          </XhFieldControl>
          <XhFieldDescription>{{ t('notification.sms_configs.form.first_default_hint') }}</XhFieldDescription>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('notification.sms_configs.form.remark') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.remark" name="remark" clearable :max-length="500" :placeholder="t('notification.sms_configs.form.remark_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <p v-if="submitError" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ submitError }}
        </p>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>
