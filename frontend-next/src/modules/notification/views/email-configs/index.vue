<script lang="ts" setup>
import type { EmailConfigForm, EmailConfigRow } from './email-config'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhFieldControl, XhFieldDescription, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSwitch } from '@xihan-ui/vue'
import { computed, reactive, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { renderTag, SchemaPage, XEditModal, XInput, XNumberInput } from '~/components'
import { toast } from '~/composables'
import { emailConfigs as emailConfigsApi } from '~/contracts'
import { enabledLabel, enabledOptions, errorText } from '../../labels'
import { emailConfigProblemMessage, emailConfigResource, emptyEmailConfigForm, findEmailConfigProblem, toCreateBody, toEmailConfigForm, toUpdateBody } from './email-config'

defineOptions({ name: 'NotificationEmailConfigsView' })

const { t } = useI18n()
const resource = emailConfigResource(t)

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

const asRow = (row: object) => row as EmailConfigRow
const statusOptions = computed<SelectOption[]>(() => enabledOptions(t))

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；可排序的只放后端白名单里的：排序、创建时间） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('notification.email_configs.columns.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('notification.email_configs.columns.keyword_placeholder'), order: 0 },
  { key: 'code', title: t('notification.email_configs.columns.config_code'), dataType: 'string', minWidth: 140, order: 1 },
  { key: 'name', title: t('notification.email_configs.columns.config_name'), dataType: 'string', minWidth: 140, order: 2 },
  { key: 'smtpHost', title: t('notification.email_configs.columns.smtp_host'), dataType: 'string', minWidth: 160, order: 3 },
  { key: 'smtpPort', title: t('notification.email_configs.columns.smtp_port'), dataType: 'number', width: 90, order: 4 },
  { key: 'fromEmail', title: t('notification.email_configs.columns.from_email'), dataType: 'string', minWidth: 170, order: 5 },
  { key: 'fromName', title: t('notification.email_configs.columns.from_name'), dataType: 'string', minWidth: 120, order: 6 },
  { key: 'isDefault', title: t('notification.email_configs.columns.is_default'), dataType: 'enum', width: 90, order: 7, render: row => (asRow(row).isDefault ? renderTag(t('notification.email_configs.tag.default'), 'warning') : '-') },
  { key: 'isEnabled', title: t('notification.email_configs.columns.status'), dataType: 'enum', searchable: true, options: statusOptions.value, searchPlaceholder: t('notification.email_configs.columns.status_placeholder'), width: 90, order: 8, render: row => renderTag(enabledLabel(t, asRow(row).isEnabled), asRow(row).isEnabled ? 'success' : 'danger') },
  { key: 'order', title: t('notification.email_configs.columns.sort'), dataType: 'number', sortable: true, width: 80, order: 9 },
  { key: 'createdAt', title: t('notification.email_configs.columns.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 10 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'notification.email-configs',
  pageName: t('notification.email_configs.title'),
  statusPermission: 'notification.email-configs.update',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('notification.email_configs.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'notification.email-configs.create' },
    { key: 'edit', title: t('notification.email_configs.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'notification.email-configs.update' },
    // 缺省配置不能停用也不能删除（后端拒绝），入口随之收起；停用的不能设为缺省
    { key: 'toggle', title: t('notification.email_configs.actions.toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => toggleConfirmText(asRow(row)), visible: row => !asRow(row).isDefault, permission: 'notification.email-configs.update' },
    { key: 'setDefault', title: t('notification.email_configs.actions.set_default'), scope: 'row', icon: 'lucide:star', confirm: true, confirmText: row => t('notification.email_configs.message.set_default_content', { name: asRow(row).name }), visible: row => !asRow(row).isDefault, disabled: row => !asRow(row).isEnabled, permission: 'notification.email-configs.update' },
    { key: 'delete', title: t('notification.email_configs.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => t('notification.email_configs.message.delete_content', { name: asRow(row).name }), visible: row => !asRow(row).isDefault, permission: 'notification.email-configs.delete' },
  ],
}))

function toggleConfirmText(row: EmailConfigRow): string {
  return row.isEnabled
    ? t('notification.email_configs.message.disable_content', { name: row.name })
    : t('notification.email_configs.message.enable_content', { name: row.name })
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
/** 编辑的配置是否已存了密码：已存的留空即保留，占位提示随之改 */
const editingHasPassword = ref(false)
const form = reactive<EmailConfigForm>(emptyEmailConfigForm())
const modalTitle = computed(() => (form.id ? t('notification.email_configs.form.edit_title') : t('notification.email_configs.form.add_title')))
const passwordPlaceholder = computed(() => (form.id && editingHasPassword.value ? t('notification.email_configs.form.password_configured') : t('notification.email_configs.form.password_placeholder')))

function openCreate() {
  Object.assign(form, emptyEmailConfigForm())
  editingHasPassword.value = false
  submitError.value = null
  modalVisible.value = true
}

async function openEdit(row: EmailConfigRow) {
  try {
    const current = await emailConfigsApi.get({ id: row.id })
    Object.assign(form, toEmailConfigForm(current))
    editingHasPassword.value = current.hasPassword
    submitError.value = null
    modalVisible.value = true
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.email_configs.message.load_detail_failed'))
  }
}

async function onSubmit() {
  const problem = findEmailConfigProblem(form)
  if (problem) {
    toast.warning(emailConfigProblemMessage(t, problem))
    return
  }
  submitting.value = true
  submitError.value = null
  try {
    if (form.id)
      await emailConfigsApi.update(toUpdateBody({ ...form, id: form.id }))
    else
      await emailConfigsApi.create(toCreateBody(form))
    toast.success(t('notification.email_configs.message.save_success'))
    modalVisible.value = false
    reload()
  }
  catch (error) {
    submitError.value = errorText(error) || t('notification.email_configs.message.save_failed')
  }
  finally {
    submitting.value = false
  }
}

// ── 启停 / 设为缺省 / 删除 ──────────────────────────────────────
async function toggleStatus(row: EmailConfigRow) {
  try {
    await resource.updateStatus?.(row.id, !row.isEnabled)
    toast.success(t('notification.email_configs.message.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.email_configs.message.status_update_failed'))
  }
}

async function setDefault(row: EmailConfigRow) {
  try {
    await emailConfigsApi.setDefault({ id: row.id })
    toast.success(t('notification.email_configs.message.set_default_success'))
    reload()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.email_configs.message.set_default_failed'))
  }
}

async function removeRow(row: EmailConfigRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('notification.email_configs.message.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.email_configs.message.delete_failed'))
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <XEditModal v-model:show="modalVisible" :title="modalTitle" :loading="submitting" :form-id="editFormId">
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="email-config-form" @submit="onSubmit">
        <XhFieldRoot :required="!form.id">
          <XhFieldLabel>{{ t('notification.email_configs.form.config_code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.code" name="code" clearable :max-length="64" :disabled="Boolean(form.id)" :placeholder="t('notification.email_configs.form.config_code_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('notification.email_configs.form.config_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.name" name="name" clearable :max-length="128" :placeholder="t('notification.email_configs.form.config_name_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('notification.email_configs.form.smtp_host') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.smtpHost" name="smtpHost" clearable :max-length="256" :placeholder="t('notification.email_configs.form.smtp_host_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('notification.email_configs.form.smtp_port') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="form.smtpPort" name="smtpPort" :min="1" :max="65535" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('notification.email_configs.form.from_email') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.fromEmail" name="fromEmail" clearable :max-length="256" autocomplete="off" :placeholder="t('notification.email_configs.form.from_email_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.email_configs.form.from_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.fromName" name="fromName" clearable :max-length="128" :placeholder="t('notification.email_configs.form.from_name_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.email_configs.form.user_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.userName" name="userName" clearable :max-length="256" autocomplete="off" :placeholder="t('notification.email_configs.form.user_name_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.email_configs.form.password') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.password" name="password" type="password" :max-length="512" autocomplete="new-password" :placeholder="passwordPlaceholder" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.email_configs.form.use_ssl') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="form.useSsl" name="useSsl" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.email_configs.form.accept_invalid_certificate') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="form.acceptInvalidCertificate" name="acceptInvalidCertificate" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.email_configs.form.sort') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="form.order" name="order" :min="0" :max="100000" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-if="!form.id">
          <XhFieldLabel>{{ t('notification.email_configs.form.is_enabled') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="form.isEnabled" name="isEnabled" />
          </XhFieldControl>
          <XhFieldDescription>{{ t('notification.email_configs.form.first_default_hint') }}</XhFieldDescription>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('notification.email_configs.form.remark') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.remark" name="remark" clearable :max-length="500" :placeholder="t('notification.email_configs.form.remark_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <p v-if="submitError" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ submitError }}
        </p>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>
