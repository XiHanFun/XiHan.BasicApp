<script setup lang="ts">
import type { TelegramBotListItemDto } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhBadge, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhSwitch } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, querySortsFromSchema, telegramBotApi } from '@/api'
import { SchemaPage, XEditModal, XInput, XNumberInput } from '~/components'
import { dialog, toast } from '~/composables'

defineOptions({ name: 'SettingTelegramBotPage' })

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

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()

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
      return h(XhBadge, { variant: 'subtle', size: 'sm', tone: hasToken ? 'success' : 'warning' }, () => hasToken ? t('message.telegram_bot.tag.token_configured') : t('message.telegram_bot.tag.token_missing'))
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
        ? h(XhBadge, { variant: 'subtle', size: 'sm', tone: 'info' }, () => t('message.telegram_bot.tag.fallback_on'))
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
      return h(XhBadge, { variant: 'subtle', size: 'sm', tone: enabled ? 'success' : 'danger' }, () => enabled ? t('message.telegram_bot.tag.enabled') : t('message.telegram_bot.tag.disabled'))
    },
  },
  { key: 'sort', title: t('message.telegram_bot.columns.sort'), dataType: 'number', sortable: true, width: 80, order: 5 },
  { key: 'createdTime', title: t('message.telegram_bot.columns.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 6 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'setting.telegram-bot',
  exportPermission: 'saas:telegram-bot:export',
  pageName: t('message.telegram_bot.page_name'),
  statusPermission: 'saas:telegram-bot:status',
  rowKey: 'basicId',
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
      toast.warning(t('message.telegram_bot.message.detail_not_found'))
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
    toast.error((e as Error).message || t('message.telegram_bot.message.load_detail_failed'))
  }
}

const idListPattern = /^\s*-?\d+\s*(?:,\s*-?\d+\s*)*$/

function validateIdList(value: string | null): boolean {
  const trimmed = value?.trim()
  return !trimmed || idListPattern.test(trimmed)
}

function validateForm() {
  if (!form.value.botName.trim()) {
    toast.warning(t('message.telegram_bot.message.input_bot_name'))
    return false
  }

  if (!form.value.basicId && !form.value.token?.trim()) {
    toast.warning(t('message.telegram_bot.message.input_token'))
    return false
  }

  if (!validateIdList(form.value.adminUsers)) {
    toast.warning(t('message.telegram_bot.message.admin_users_invalid'))
    return false
  }

  if (!validateIdList(form.value.allowedGroupChatIds)) {
    toast.warning(t('message.telegram_bot.message.group_chat_ids_invalid'))
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

    toast.success(t('message.telegram_bot.message.save_success'))
    modalVisible.value = false
    reloadList()
  }
  catch (e) {
    toast.error((e as Error).message || t('message.telegram_bot.message.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

function handleToggleStatus(row: TelegramBotListItemDto) {
  const next = !row.isEnabled
  void dialog.confirm({
    badge: 'warning',
    title: next ? t('message.telegram_bot.message.enable_title') : t('message.telegram_bot.message.disable_title'),
    content: next
      ? t('message.telegram_bot.message.enable_content', { name: row.botName })
      : t('message.telegram_bot.message.disable_content', { name: row.botName }),
    okText: next ? t('message.telegram_bot.message.enable') : t('message.telegram_bot.message.disable'),
    cancelText: t('message.telegram_bot.form.cancel'),
    onOk: async () => {
      try {
        await telegramBotApi.updateStatus({ basicId: row.basicId, isEnabled: next })
        toast.success(t('message.telegram_bot.message.status_updated'))
        reloadList()
      }
      catch (e) {
        toast.error((e as Error).message || t('message.telegram_bot.message.status_update_failed'))
      }
    },
  })
}

function handleDelete(row: TelegramBotListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('message.telegram_bot.message.delete_title'),
    content: t('message.telegram_bot.message.delete_content', { name: row.botName }),
    okText: t('message.telegram_bot.message.delete'),
    cancelText: t('message.telegram_bot.form.cancel'),
    onOk: async () => {
      try {
        await telegramBotApi.delete(row.basicId)
        toast.success(t('message.telegram_bot.message.delete_success'))
        reloadList()
      }
      catch (e) {
        toast.error((e as Error).message || t('message.telegram_bot.message.delete_failed'))
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
    <XEditModal
      v-model:show="modalVisible"
      :title="modalTitle"
      :loading="submitLoading"
      :form-id="editFormId"
    >
      <XhFormRoot
        :id="editFormId"
        v-model:values="form"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleSubmit"
      >
        <XhFormFieldGroup value="botName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.telegram_bot.form.bot_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.botName" clearable :placeholder="t('message.telegram_bot.form.bot_name_placeholder')" autocomplete="off" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="token">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.telegram_bot.form.token') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="form.token"
                type="password"
                autocomplete="new-password"
                :placeholder="tokenPlaceholder"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="adminUsers" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.telegram_bot.form.admin_users') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.adminUsers" clearable :placeholder="t('message.telegram_bot.form.admin_users_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="allowedGroupChatIds" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.telegram_bot.form.allowed_group_chat_ids') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.allowedGroupChatIds" clearable :placeholder="t('message.telegram_bot.form.allowed_group_chat_ids_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="allowedCommands" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.telegram_bot.form.allowed_commands') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.allowedCommands" clearable :placeholder="t('message.telegram_bot.form.allowed_commands_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="enableFallbackReply">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.telegram_bot.form.enable_fallback_reply') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="form.enableFallbackReply" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.telegram_bot.form.sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>

        <template v-if="!form.basicId">
          <XhFormFieldGroup value="isEnabled">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('message.telegram_bot.form.is_enabled') }}</XhFieldLabel>
              <XhFieldControl>
                <XhSwitch v-model:checked="form.isEnabled" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
        </template>

        <XhFormFieldGroup value="remark" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.telegram_bot.form.remark') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.remark" clearable :placeholder="t('message.telegram_bot.form.remark_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>
