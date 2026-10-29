<script lang="ts" setup>
import type { TelegramBotForm, TelegramBotRow } from './telegram-bot'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhFieldControl, XhFieldDescription, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSwitch } from '@xihan-ui/vue'
import { computed, reactive, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { renderTag, SchemaPage, XEditModal, XInput, XNumberInput } from '~/components'
import { toast } from '~/composables'
import { telegramBots as telegramBotsApi } from '~/contracts'
import { enabledLabel, enabledOptions, errorText } from '../../labels'
import { emptyTelegramBotForm, findTelegramBotProblem, telegramBotProblemMessage, telegramBotResource, toCreateBody, toTelegramBotForm, toUpdateBody } from './telegram-bot'

defineOptions({ name: 'NotificationTelegramBotsView' })

const { t } = useI18n()
const resource = telegramBotResource(t)

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

const asRow = (row: object) => row as TelegramBotRow
const statusOptions = computed<SelectOption[]>(() => enabledOptions(t))
/** 兜底回复的搜索选项：后端按 `enableFallbackReply eq true / false` 精确匹配 */
const fallbackOptions = computed<SelectOption[]>(() => [
  { label: t('notification.telegram_bots.fallback.on'), value: 'true' },
  { label: t('notification.telegram_bots.fallback.off'), value: 'false' },
])

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；可排序的只放后端白名单里的：名称、排序、创建时间） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('notification.telegram_bots.columns.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('notification.telegram_bots.columns.keyword_placeholder'), order: 0 },
  { key: 'name', title: t('notification.telegram_bots.columns.bot_name'), dataType: 'string', sortable: true, minWidth: 160, order: 1 },
  { key: 'hasToken', title: t('notification.telegram_bots.columns.has_token'), dataType: 'enum', width: 100, order: 2, render: row => (asRow(row).hasToken ? renderTag(t('notification.telegram_bots.tag.token_configured'), 'success') : renderTag(t('notification.telegram_bots.tag.token_missing'), 'warning')) },
  { key: 'enableFallbackReply', title: t('notification.telegram_bots.columns.fallback_reply'), dataType: 'enum', searchable: true, options: fallbackOptions.value, searchPlaceholder: t('notification.telegram_bots.columns.fallback_reply_placeholder'), width: 110, order: 3, render: row => (asRow(row).enableFallbackReply ? renderTag(t('notification.telegram_bots.fallback.on'), 'info') : '-') },
  { key: 'isEnabled', title: t('notification.telegram_bots.columns.status'), dataType: 'enum', searchable: true, options: statusOptions.value, searchPlaceholder: t('notification.telegram_bots.columns.status_placeholder'), width: 90, order: 4, render: row => renderTag(enabledLabel(t, asRow(row).isEnabled), asRow(row).isEnabled ? 'success' : 'danger') },
  { key: 'order', title: t('notification.telegram_bots.columns.sort'), dataType: 'number', sortable: true, width: 80, order: 5 },
  { key: 'createdAt', title: t('notification.telegram_bots.columns.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 6 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'notification.telegram-bots',
  pageName: t('notification.telegram_bots.title'),
  statusPermission: 'notification.telegram-bots.update',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('notification.telegram_bots.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'notification.telegram-bots.create' },
    { key: 'edit', title: t('notification.telegram_bots.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'notification.telegram-bots.update' },
    { key: 'toggle', title: t('notification.telegram_bots.actions.toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => toggleConfirmText(asRow(row)), permission: 'notification.telegram-bots.update' },
    { key: 'delete', title: t('notification.telegram_bots.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => t('notification.telegram_bots.message.delete_content', { name: asRow(row).name }), permission: 'notification.telegram-bots.delete' },
  ],
}))

function toggleConfirmText(row: TelegramBotRow): string {
  return row.isEnabled
    ? t('notification.telegram_bots.message.disable_content', { name: row.name })
    : t('notification.telegram_bots.message.enable_content', { name: row.name })
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
/** 编辑的机器人是否已存了令牌：已存的留空即保留，占位提示随之改 */
const editingHasToken = ref(false)
const form = reactive<TelegramBotForm>(emptyTelegramBotForm())
const modalTitle = computed(() => (form.id ? t('notification.telegram_bots.form.edit_title') : t('notification.telegram_bots.form.add_title')))
const tokenPlaceholder = computed(() => (form.id && editingHasToken.value ? t('notification.telegram_bots.form.token_configured') : t('notification.telegram_bots.form.token_placeholder')))

function openCreate() {
  Object.assign(form, emptyTelegramBotForm())
  editingHasToken.value = false
  submitError.value = null
  modalVisible.value = true
}

async function openEdit(row: TelegramBotRow) {
  try {
    const current = await telegramBotsApi.get({ id: row.id })
    Object.assign(form, toTelegramBotForm(current))
    editingHasToken.value = current.hasToken
    submitError.value = null
    modalVisible.value = true
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.telegram_bots.message.load_detail_failed'))
  }
}

async function onSubmit() {
  const problem = findTelegramBotProblem(form)
  if (problem) {
    toast.warning(telegramBotProblemMessage(t, problem))
    return
  }
  submitting.value = true
  submitError.value = null
  try {
    if (form.id)
      await telegramBotsApi.update(toUpdateBody({ ...form, id: form.id }))
    else
      await telegramBotsApi.create(toCreateBody(form))
    toast.success(t('notification.telegram_bots.message.save_success'))
    modalVisible.value = false
    reload()
  }
  catch (error) {
    submitError.value = errorText(error) || t('notification.telegram_bots.message.save_failed')
  }
  finally {
    submitting.value = false
  }
}

// ── 启停 / 删除 ─────────────────────────────────────────────────
async function toggleStatus(row: TelegramBotRow) {
  try {
    await resource.updateStatus?.(row.id, !row.isEnabled)
    toast.success(t('notification.telegram_bots.message.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.telegram_bots.message.status_update_failed'))
  }
}

async function removeRow(row: TelegramBotRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('notification.telegram_bots.message.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.telegram_bots.message.delete_failed'))
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <XEditModal v-model:show="modalVisible" :title="modalTitle" :loading="submitting" :form-id="editFormId">
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="telegram-bot-form" @submit="onSubmit">
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('notification.telegram_bots.form.bot_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.name" name="name" clearable :max-length="64" autocomplete="off" :placeholder="t('notification.telegram_bots.form.bot_name_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot :required="!(form.id && editingHasToken)">
          <XhFieldLabel>{{ t('notification.telegram_bots.form.token') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.token" name="token" type="password" :max-length="256" autocomplete="new-password" :placeholder="tokenPlaceholder" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('notification.telegram_bots.form.admin_users') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.adminUsers" name="adminUsers" clearable :max-length="500" :placeholder="t('notification.telegram_bots.form.admin_users_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('notification.telegram_bots.form.allowed_group_chat_ids') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.allowedGroupChatIds" name="allowedGroupChatIds" clearable :max-length="500" :placeholder="t('notification.telegram_bots.form.allowed_group_chat_ids_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('notification.telegram_bots.form.allowed_commands') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.allowedCommands" name="allowedCommands" clearable :max-length="500" :placeholder="t('notification.telegram_bots.form.allowed_commands_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('notification.telegram_bots.form.broadcast_chat_ids') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.broadcastChatIds" name="broadcastChatIds" clearable :max-length="500" :placeholder="t('notification.telegram_bots.form.broadcast_chat_ids_placeholder')" />
          </XhFieldControl>
          <XhFieldDescription>{{ t('notification.telegram_bots.form.broadcast_chat_ids_hint') }}</XhFieldDescription>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.telegram_bots.form.enable_fallback_reply') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="form.enableFallbackReply" name="enableFallbackReply" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.telegram_bots.form.sort') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="form.order" name="order" :min="0" :max="100000" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-if="!form.id">
          <XhFieldLabel>{{ t('notification.telegram_bots.form.is_enabled') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="form.isEnabled" name="isEnabled" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('notification.telegram_bots.form.remark') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.remark" name="remark" clearable :max-length="500" :placeholder="t('notification.telegram_bots.form.remark_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <p v-if="submitError" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ submitError }}
        </p>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>
