<script setup lang="ts">
import type { TelegramBotListItemDto } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NConfigProvider,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NSpace,
  NSwitch,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, querySortsFromSchema, telegramBotApi } from '@/api'
import { SchemaPage } from '~/components'

defineOptions({ name: 'MessageTelegramBotPage' })

interface TelegramBotFormModel {
  adminUsers: string | null
  allowedCommands: string | null
  allowedGroupChatIds: string | null
  basicId?: string
  botName: string
  enableFallbackReply: boolean
  isEnabled: boolean
  remark: string | null
  sort: number
  token: string | null
}

const { t } = useI18n()
const message = useMessage()
const dialog = useDialog()

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadList() {
  void schemaPageRef.value?.reload()
}

// SchemaSelectOption.value 仅支持 string | number；布尔搜索项用 1/0，page() 里转回 boolean
const enabledOptions = computed(() => [
  { label: t('message.telegram_bot.enabled.enabled'), value: 1 },
  { label: t('message.telegram_bot.enabled.disabled'), value: 0 },
])

function pickBoolean(value: unknown): boolean | undefined {
  return value === undefined || value === null || value === '' ? undefined : Boolean(Number(value))
}

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；order 控顺序） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('message.telegram_bot.columns.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('message.telegram_bot.columns.keyword_placeholder'), order: 0 },
  { key: 'botName', title: t('message.telegram_bot.columns.bot_name'), dataType: 'string', sortable: true, minWidth: 160, order: 1 },
  {
    key: 'hasToken',
    title: t('message.telegram_bot.columns.has_token'),
    dataType: 'boolean',
    width: 100,
    order: 2,
    render: (row) => {
      const hasToken = (row as unknown as TelegramBotListItemDto).hasToken
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: hasToken ? 'success' : 'warning' },
        () => hasToken ? t('message.telegram_bot.tag.token_configured') : t('message.telegram_bot.tag.token_missing'),
      )
    },
  },
  {
    key: 'enableFallbackReply',
    title: t('message.telegram_bot.columns.fallback_reply'),
    dataType: 'boolean',
    sortable: true,
    width: 110,
    order: 3,
    render: (row) => {
      const enabled = (row as unknown as TelegramBotListItemDto).enableFallbackReply
      return enabled
        ? h(NTag, { size: 'small', round: true, bordered: false, type: 'info' }, () => t('message.telegram_bot.tag.fallback_on'))
        : h('span', { style: 'opacity:.45' }, '—')
    },
  },
  {
    key: 'isEnabled',
    title: t('message.telegram_bot.columns.status'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: enabledOptions.value,
    searchPlaceholder: t('message.telegram_bot.columns.status_placeholder'),
    width: 90,
    order: 4,
    render: (row) => {
      const enabled = (row as unknown as TelegramBotListItemDto).isEnabled
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: enabled ? 'success' : 'error' },
        () => enabled ? t('message.telegram_bot.tag.enabled') : t('message.telegram_bot.tag.disabled'),
      )
    },
  },
  { key: 'sort', title: t('message.telegram_bot.columns.sort'), dataType: 'number', sortable: true, width: 80, order: 5 },
  { key: 'createdTime', title: t('message.telegram_bot.columns.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 6 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'message.telegram-bot',
  exportPermission: 'saas:telegram-bot:export',
  pageName: t('message.telegram_bot.page_name'),
  statusPermission: 'saas:telegram-bot:status',
  rowKey: 'basicId',
  scrollX: 1000,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return telegramBotApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序等通用过滤统一走 conditions
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        isEnabled: pickBoolean(f.isEnabled),
        keyword: (f.keyword as string | undefined)?.trim() || undefined,
      }) as unknown as Promise<import('@/api').PageResult<Record<string, unknown>>>
    },
    updateStatus: (id, enabled) => telegramBotApi.updateStatus({ basicId: id, isEnabled: enabled }),
  },
  actions: [
    { key: 'create', title: t('message.telegram_bot.actions.create'), scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'saas:telegram-bot:create' },
    { key: 'edit', title: t('message.telegram_bot.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'saas:telegram-bot:update' },
    { key: 'toggle', title: t('message.telegram_bot.actions.toggle'), scope: 'row', icon: 'lucide:power', permission: 'saas:telegram-bot:status' },
    { key: 'delete', title: t('message.telegram_bot.actions.delete'), scope: 'row', icon: 'lucide:trash-2', type: 'error', permission: 'saas:telegram-bot:delete' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as TelegramBotListItemDto | undefined
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
        handleToggleStatus(row)
      }
      break
    case 'delete':
      if (row) {
        handleDelete(row)
      }
      break
  }
}

// ── 弹窗/表单 ──
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingHasToken = ref(false)
const form = ref<TelegramBotFormModel>(createDefaultForm())

const modalTitle = computed(() => (form.value.basicId ? t('message.telegram_bot.form.edit_title') : t('message.telegram_bot.form.add_title')))
const tokenPlaceholder = computed(() =>
  form.value.basicId && editingHasToken.value ? t('message.telegram_bot.form.token_configured') : t('message.telegram_bot.form.token_placeholder'),
)

function createDefaultForm(): TelegramBotFormModel {
  return {
    adminUsers: null,
    allowedCommands: null,
    allowedGroupChatIds: null,
    botName: '',
    enableFallbackReply: false,
    isEnabled: true,
    remark: null,
    sort: 100,
    token: null,
  }
}

function handleAdd() {
  editingHasToken.value = false
  form.value = createDefaultForm()
  modalVisible.value = true
}

async function handleEdit(row: TelegramBotListItemDto) {
  try {
    const detail = await telegramBotApi.detail(row.basicId)
    if (!detail) {
      message.warning(t('message.telegram_bot.message.detail_not_found'))
      return
    }

    editingHasToken.value = detail.hasToken
    form.value = {
      adminUsers: detail.adminUsers ?? null,
      allowedCommands: detail.allowedCommands ?? null,
      allowedGroupChatIds: detail.allowedGroupChatIds ?? null,
      basicId: detail.basicId,
      botName: detail.botName,
      enableFallbackReply: detail.enableFallbackReply,
      isEnabled: detail.isEnabled,
      remark: detail.remark ?? null,
      sort: detail.sort,
      token: null,
    }
    modalVisible.value = true
  }
  catch (e) {
    message.error((e as Error).message || t('message.telegram_bot.message.load_detail_failed'))
  }
}

const idListPattern = /^\s*-?\d+\s*(?:,\s*-?\d+\s*)*$/

function validateIdList(value: string | null): boolean {
  const trimmed = value?.trim()
  return !trimmed || idListPattern.test(trimmed)
}

function validateForm() {
  if (!form.value.botName.trim()) {
    message.warning(t('message.telegram_bot.message.input_bot_name'))
    return false
  }

  if (!form.value.basicId && !form.value.token?.trim()) {
    message.warning(t('message.telegram_bot.message.input_token'))
    return false
  }

  if (!validateIdList(form.value.adminUsers)) {
    message.warning(t('message.telegram_bot.message.admin_users_invalid'))
    return false
  }

  if (!validateIdList(form.value.allowedGroupChatIds)) {
    message.warning(t('message.telegram_bot.message.group_chat_ids_invalid'))
    return false
  }

  return true
}

async function handleSubmit() {
  if (!validateForm()) {
    return
  }

  submitLoading.value = true

  try {
    if (form.value.basicId) {
      await telegramBotApi.update({
        adminUsers: form.value.adminUsers?.trim() || null,
        allowedCommands: form.value.allowedCommands?.trim() || null,
        allowedGroupChatIds: form.value.allowedGroupChatIds?.trim() || null,
        basicId: form.value.basicId,
        botName: form.value.botName.trim(),
        enableFallbackReply: form.value.enableFallbackReply,
        remark: form.value.remark,
        sort: form.value.sort,
        token: form.value.token?.trim() || null,
      })
    }
    else {
      await telegramBotApi.create({
        adminUsers: form.value.adminUsers?.trim() || null,
        allowedCommands: form.value.allowedCommands?.trim() || null,
        allowedGroupChatIds: form.value.allowedGroupChatIds?.trim() || null,
        botName: form.value.botName.trim(),
        enableFallbackReply: form.value.enableFallbackReply,
        isEnabled: form.value.isEnabled,
        remark: form.value.remark,
        sort: form.value.sort,
        token: form.value.token?.trim() ?? '',
      })
    }

    message.success(t('message.telegram_bot.message.save_success'))
    modalVisible.value = false
    reloadList()
  }
  catch (e) {
    message.error((e as Error).message || t('message.telegram_bot.message.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

function handleToggleStatus(row: TelegramBotListItemDto) {
  const next = !row.isEnabled
  dialog.warning({
    title: next ? t('message.telegram_bot.message.enable_title') : t('message.telegram_bot.message.disable_title'),
    content: next
      ? t('message.telegram_bot.message.enable_content', { name: row.botName })
      : t('message.telegram_bot.message.disable_content', { name: row.botName }),
    positiveText: next ? t('message.telegram_bot.message.enable') : t('message.telegram_bot.message.disable'),
    negativeText: t('message.telegram_bot.form.cancel'),
    onPositiveClick: async () => {
      try {
        await telegramBotApi.updateStatus({ basicId: row.basicId, isEnabled: next })
        message.success(t('message.telegram_bot.message.status_updated'))
        reloadList()
      }
      catch (e) {
        message.error((e as Error).message || t('message.telegram_bot.message.status_update_failed'))
      }
    },
  })
}

function handleDelete(row: TelegramBotListItemDto) {
  dialog.warning({
    title: t('message.telegram_bot.message.delete_title'),
    content: t('message.telegram_bot.message.delete_content', { name: row.botName }),
    positiveText: t('message.telegram_bot.message.delete'),
    negativeText: t('message.telegram_bot.form.cancel'),
    onPositiveClick: async () => {
      try {
        await telegramBotApi.delete(row.basicId)
        message.success(t('message.telegram_bot.message.delete_success'))
        reloadList()
      }
      catch (e) {
        message.error((e as Error).message || t('message.telegram_bot.message.delete_failed'))
      }
    },
  })
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 680px; max-width: 92vw"
    >
      <NConfigProvider size="small" abstract>
        <NForm :model="form" size="small" class="xh-edit-form-grid" label-placement="top">
          <NFormItem :label="t('message.telegram_bot.form.bot_name')" path="botName">
            <NInput v-model:value="form.botName" clearable size="small" :placeholder="t('message.telegram_bot.form.bot_name_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('message.telegram_bot.form.token')" path="token">
            <NInput
              v-model:value="form.token"
              size="small"
              type="password"
              show-password-on="click"
              :placeholder="tokenPlaceholder"
            />
          </NFormItem>
          <NFormItem :label="t('message.telegram_bot.form.admin_users')" path="adminUsers" style="grid-column: span 2">
            <NInput v-model:value="form.adminUsers" clearable size="small" :placeholder="t('message.telegram_bot.form.admin_users_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('message.telegram_bot.form.allowed_group_chat_ids')" path="allowedGroupChatIds" style="grid-column: span 2">
            <NInput v-model:value="form.allowedGroupChatIds" clearable size="small" :placeholder="t('message.telegram_bot.form.allowed_group_chat_ids_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('message.telegram_bot.form.allowed_commands')" path="allowedCommands" style="grid-column: span 2">
            <NInput v-model:value="form.allowedCommands" clearable size="small" :placeholder="t('message.telegram_bot.form.allowed_commands_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('message.telegram_bot.form.enable_fallback_reply')" path="enableFallbackReply">
            <NSwitch v-model:value="form.enableFallbackReply" />
          </NFormItem>
          <NFormItem :label="t('message.telegram_bot.form.sort')" path="sort">
            <NInputNumber v-model:value="form.sort" :min="0" style="width: 100%" />
          </NFormItem>

          <template v-if="!form.basicId">
            <NFormItem :label="t('message.telegram_bot.form.is_enabled')" path="isEnabled">
              <NSwitch v-model:value="form.isEnabled" />
            </NFormItem>
          </template>

          <NFormItem :label="t('message.telegram_bot.form.remark')" path="remark" style="grid-column: span 2">
            <NInput v-model:value="form.remark" clearable size="small" :placeholder="t('message.telegram_bot.form.remark_placeholder')" />
          </NFormItem>
        </NForm>
      </NConfigProvider>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="modalVisible = false">
            {{ t('message.telegram_bot.form.cancel') }}
          </NButton>
          <NButton size="small" :loading="submitLoading" type="primary" @click="handleSubmit">
            {{ t('message.telegram_bot.form.save') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </SchemaPage>
</template>
