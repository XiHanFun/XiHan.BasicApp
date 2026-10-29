<script lang="ts" setup>
import type { BotConfigForm, BotConfigRow } from './bot-config'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { BotProvider } from '~/contracts'
import type { SelectOption } from '~/types'
import { XhFieldControl, XhFieldDescription, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSwitch } from '@xihan-ui/vue'
import { computed, reactive, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { renderTag, SchemaPage, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { botConfigs as botConfigsApi, BotProvider as PROVIDERS } from '~/contracts'
import { botProviderLabel, enabledLabel, enabledOptions, errorText } from '../../labels'
import { BOT_WEBHOOK_HOSTS, botConfigProblemMessage, botConfigResource, emptyBotConfigForm, findBotConfigProblem, supportsSigning, toBotConfigForm, toCreateBody, toUpdateBody } from './bot-config'

defineOptions({ name: 'NotificationBotConfigsView' })

const { t } = useI18n()
const resource = botConfigResource(t)

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

const asRow = (row: object) => row as BotConfigRow
const providerOptions = computed<SelectOption[]>(() => PROVIDERS.map(provider => ({ label: botProviderLabel(t, provider), value: provider })))
const statusOptions = computed<SelectOption[]>(() => enabledOptions(t))

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；可排序的只放后端白名单里的：排序、创建时间） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('notification.bot_configs.columns.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('notification.bot_configs.columns.keyword_placeholder'), order: 0 },
  { key: 'code', title: t('notification.bot_configs.columns.config_code'), dataType: 'string', minWidth: 140, order: 1 },
  { key: 'name', title: t('notification.bot_configs.columns.config_name'), dataType: 'string', minWidth: 140, order: 2 },
  { key: 'provider', title: t('notification.bot_configs.columns.provider'), dataType: 'enum', searchable: true, searchMultiple: true, options: providerOptions.value, searchPlaceholder: t('notification.bot_configs.columns.provider_placeholder'), width: 110, order: 3, render: row => renderTag(botProviderLabel(t, asRow(row).provider), 'info') },
  { key: 'isDefault', title: t('notification.bot_configs.columns.is_default'), dataType: 'enum', width: 90, order: 4, render: row => (asRow(row).isDefault ? renderTag(t('notification.bot_configs.tag.default'), 'warning') : '-') },
  { key: 'isEnabled', title: t('notification.bot_configs.columns.status'), dataType: 'enum', searchable: true, options: statusOptions.value, searchPlaceholder: t('notification.bot_configs.columns.status_placeholder'), width: 90, order: 5, render: row => renderTag(enabledLabel(t, asRow(row).isEnabled), asRow(row).isEnabled ? 'success' : 'danger') },
  { key: 'order', title: t('notification.bot_configs.columns.sort'), dataType: 'number', sortable: true, width: 80, order: 6 },
  { key: 'createdAt', title: t('notification.bot_configs.columns.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 7 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'notification.bot-configs',
  pageName: t('notification.bot_configs.title'),
  statusPermission: 'notification.bot-configs.update',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('notification.bot_configs.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'notification.bot-configs.create' },
    { key: 'edit', title: t('notification.bot_configs.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'notification.bot-configs.update' },
    // 默认机器人不能停用也不能删除（后端拒绝），入口随之收起；停用的不能设为默认
    { key: 'toggle', title: t('notification.bot_configs.actions.toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => toggleConfirmText(asRow(row)), visible: row => !asRow(row).isDefault, permission: 'notification.bot-configs.update' },
    { key: 'setDefault', title: t('notification.bot_configs.actions.set_default'), scope: 'row', icon: 'lucide:star', confirm: true, confirmText: row => t('notification.bot_configs.message.set_default_content', { name: asRow(row).name }), visible: row => !asRow(row).isDefault, disabled: row => !asRow(row).isEnabled, permission: 'notification.bot-configs.update' },
    { key: 'delete', title: t('notification.bot_configs.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => t('notification.bot_configs.message.delete_content', { name: asRow(row).name }), visible: row => !asRow(row).isDefault, permission: 'notification.bot-configs.delete' },
  ],
}))

function toggleConfirmText(row: BotConfigRow): string {
  return row.isEnabled
    ? t('notification.bot_configs.message.disable_content', { name: row.name })
    : t('notification.bot_configs.message.enable_content', { name: row.name })
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
/** 编辑的配置是否已存了签名密钥：已存的留空即保留，占位提示随之改 */
const editingHasSecret = ref(false)
/** 编辑的是不是这个服务商的默认机器人：默认的不能换服务商（后端拒绝） */
const editingIsDefault = ref(false)
const form = reactive<BotConfigForm>(emptyBotConfigForm())
const modalTitle = computed(() => (form.id ? t('notification.bot_configs.form.edit_title') : t('notification.bot_configs.form.add_title')))
const signing = computed(() => supportsSigning(form.provider))
const providerLocked = computed(() => Boolean(form.id) && editingIsDefault.value)
const secretPlaceholder = computed(() => (form.id && editingHasSecret.value ? t('notification.bot_configs.form.secret_configured') : t('notification.bot_configs.form.secret_placeholder')))
/** Webhook 的占位与提示随服务商（4.x 同）：只认官方 https 地址 */
const webhookPlaceholder = computed(() => {
  switch (form.provider) {
    case 'Lark': return t('notification.bot_configs.form.webhook_url_placeholder_lark')
    case 'WeCom': return t('notification.bot_configs.form.webhook_url_placeholder_we_com')
    default: return t('notification.bot_configs.form.webhook_url_placeholder_ding_talk')
  }
})
const webhookHint = computed(() => t('notification.bot_configs.form.webhook_url_hint', { hosts: BOT_WEBHOOK_HOSTS[form.provider].join(' / ') }))

function openCreate() {
  Object.assign(form, emptyBotConfigForm())
  editingHasSecret.value = false
  editingIsDefault.value = false
  submitError.value = null
  modalVisible.value = true
}

async function openEdit(row: BotConfigRow) {
  try {
    const current = await botConfigsApi.get({ id: row.id })
    Object.assign(form, toBotConfigForm(current))
    editingHasSecret.value = current.hasSecret
    editingIsDefault.value = current.isDefault
    submitError.value = null
    modalVisible.value = true
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.bot_configs.message.load_detail_failed'))
  }
}

function changeProvider(value: unknown) {
  form.provider = value as BotProvider
}

async function onSubmit() {
  const problem = findBotConfigProblem(form)
  if (problem) {
    toast.warning(botConfigProblemMessage(t, problem))
    return
  }
  submitting.value = true
  submitError.value = null
  try {
    if (form.id)
      await botConfigsApi.update(toUpdateBody({ ...form, id: form.id }))
    else
      await botConfigsApi.create(toCreateBody(form))
    toast.success(t('notification.bot_configs.message.save_success'))
    modalVisible.value = false
    reload()
  }
  catch (error) {
    submitError.value = errorText(error) || t('notification.bot_configs.message.save_failed')
  }
  finally {
    submitting.value = false
  }
}

// ── 启停 / 设为默认 / 删除 ──────────────────────────────────────
async function toggleStatus(row: BotConfigRow) {
  try {
    await resource.updateStatus?.(row.id, !row.isEnabled)
    toast.success(t('notification.bot_configs.message.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.bot_configs.message.status_update_failed'))
  }
}

async function setDefault(row: BotConfigRow) {
  try {
    await botConfigsApi.setDefault({ id: row.id })
    toast.success(t('notification.bot_configs.message.set_default_success'))
    reload()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.bot_configs.message.set_default_failed'))
  }
}

async function removeRow(row: BotConfigRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('notification.bot_configs.message.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.bot_configs.message.delete_failed'))
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <XEditModal v-model:show="modalVisible" :title="modalTitle" :loading="submitting" :form-id="editFormId">
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="bot-config-form" @submit="onSubmit">
        <XhFieldRoot :required="!form.id">
          <XhFieldLabel>{{ t('notification.bot_configs.form.config_code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.code" name="code" clearable :max-length="64" :disabled="Boolean(form.id)" :placeholder="t('notification.bot_configs.form.config_code_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('notification.bot_configs.form.config_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.name" name="name" clearable :max-length="128" :placeholder="t('notification.bot_configs.form.config_name_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('notification.bot_configs.form.provider') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect :value="form.provider" name="provider" :options="providerOptions" :disabled="providerLocked" @update:value="changeProvider" />
          </XhFieldControl>
          <XhFieldDescription v-if="providerLocked">
            {{ t('notification.bot_configs.form.provider_locked_hint') }}
          </XhFieldDescription>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.bot_configs.form.sort') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="form.order" name="order" :min="0" :max="100000" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" required>
          <XhFieldLabel>{{ t('notification.bot_configs.form.webhook_url') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.webhookUrl" name="webhookUrl" type="url" clearable :max-length="500" autocomplete="off" :placeholder="webhookPlaceholder" />
          </XhFieldControl>
          <XhFieldDescription>{{ webhookHint }}</XhFieldDescription>
        </XhFieldRoot>
        <template v-if="signing">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('notification.bot_configs.form.secret') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.secret" name="secret" type="password" :max-length="512" autocomplete="new-password" :placeholder="secretPlaceholder" />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('notification.bot_configs.form.keyword') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.keyword" name="keyword" clearable :max-length="100" :placeholder="t('notification.bot_configs.form.keyword_placeholder')" />
            </XhFieldControl>
          </XhFieldRoot>
        </template>
        <XhFieldRoot v-if="!form.id" class="xh-span-2">
          <XhFieldLabel>{{ t('notification.bot_configs.form.is_enabled') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="form.isEnabled" name="isEnabled" />
          </XhFieldControl>
          <XhFieldDescription>{{ t('notification.bot_configs.form.first_default_hint') }}</XhFieldDescription>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('notification.bot_configs.form.remark') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.remark" name="remark" clearable :max-length="500" :placeholder="t('notification.bot_configs.form.remark_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <p v-if="submitError" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ submitError }}
        </p>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>
