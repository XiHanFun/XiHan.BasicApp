<script setup lang="ts">
import type { BotConfigListItemDto } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { botConfigApi, BotProviderType, createPageRequest, querySortsFromSchema } from '@/api'
import { SchemaPage, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { dialog, toast } from '~/composables'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'SettingBotConfigPage' })

interface BotConfigFormModel {
  basicId?: string
  configCode: string
  configName: string
  isDefault: boolean
  isEnabled: boolean
  keyword: string | null
  provider: BotProviderType
  remark: string | null
  secret: string | null
  sort: number
  webhookUrl: string
}

const { t } = useI18n()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadList() {
  void schemaPageRef.value?.reload()
}

const providerOptions = computed(() => [
  { label: t('message.bot_config.provider.ding_talk'), value: BotProviderType.DingTalk },
  { label: t('message.bot_config.provider.lark'), value: BotProviderType.Lark },
  { label: t('message.bot_config.provider.we_com'), value: BotProviderType.WeCom },
])

// SchemaSelectOption.value 仅支持 string | number；布尔搜索项用 1/0，page() 里转回 boolean
const defaultOptions = computed(() => [
  { label: t('message.bot_config.default.is_default'), value: 1 },
  { label: t('message.bot_config.default.not_default'), value: 0 },
])
const enabledOptions = computed(() => [
  { label: t('message.bot_config.enabled.enabled'), value: 1 },
  { label: t('message.bot_config.enabled.disabled'), value: 0 },
])

function pickBoolean(value: unknown): boolean | undefined {
  return value === undefined || value === null || value === '' ? undefined : Boolean(Number(value))
}

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；order 控顺序） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('message.bot_config.columns.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('message.bot_config.columns.keyword_placeholder'), order: 0 },
  { key: 'configCode', title: t('message.bot_config.columns.config_code'), dataType: 'string', sortable: true, minWidth: 140, order: 1 },
  { key: 'configName', title: t('message.bot_config.columns.config_name'), dataType: 'string', sortable: true, minWidth: 140, order: 2 },
  {
    key: 'provider',
    title: t('message.bot_config.columns.provider'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: providerOptions.value,
    searchPlaceholder: t('message.bot_config.columns.provider_placeholder'),
    width: 110,
    order: 3,
    render: row => h(XhTagRoot, { variant: 'outline', tone: 'info' }, () => h(XhTagLabel, () => getOptionLabel(providerOptions.value, (row as unknown as BotConfigListItemDto).provider))),
  },
  {
    key: 'isDefault',
    title: t('message.bot_config.columns.is_default'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: defaultOptions.value,
    searchPlaceholder: t('message.bot_config.columns.is_default_placeholder'),
    width: 90,
    order: 4,
    render: (row) => {
      const isDefault = (row as unknown as BotConfigListItemDto).isDefault
      return isDefault
        ? h(XhTagRoot, { variant: 'outline', tone: 'warning' }, () => h(XhTagLabel, () => t('message.bot_config.tag.default')))
        : h('span', { style: 'opacity:.45' }, '—')
    },
  },
  {
    key: 'isEnabled',
    title: t('message.bot_config.columns.status'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: enabledOptions.value,
    searchPlaceholder: t('message.bot_config.columns.status_placeholder'),
    width: 90,
    order: 5,
    render: (row) => {
      const enabled = (row as unknown as BotConfigListItemDto).isEnabled
      return h(XhTagRoot, { variant: 'outline', tone: enabled ? 'success' : 'danger' }, () => h(XhTagLabel, () => enabled ? t('message.bot_config.tag.enabled') : t('message.bot_config.tag.disabled')))
    },
  },
  { key: 'sort', title: t('message.bot_config.columns.sort'), dataType: 'number', sortable: true, width: 80, order: 6 },
  { key: 'createdTime', title: t('message.bot_config.columns.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 7 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'setting.bot-config',
  exportPermission: 'setting.bot-config.export',
  pageName: t('message.bot_config.page_name'),
  statusPermission: 'setting.bot-config.status',
  rowKey: 'basicId',
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return botConfigApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 多选(provider) 等通用过滤统一走 conditions
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        isDefault: pickBoolean(f.isDefault),
        isEnabled: pickBoolean(f.isEnabled),
        keyword: (f.keyword as string | undefined)?.trim() || undefined,
        // provider 为多选，经 conditions.filters In 下发（不走 DTO 顶层 provider 单值字段）
      }) as unknown as Promise<import('@/api').PageResult<Record<string, unknown>>>
    },
    updateStatus: (id, enabled) => botConfigApi.updateStatus({ basicId: id, isEnabled: enabled }),
  },
  actions: [
    { key: 'create', title: t('message.bot_config.actions.create'), scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'setting.bot-config.create' },
    { key: 'edit', title: t('message.bot_config.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'setting.bot-config.update' },
    { key: 'toggle', title: t('message.bot_config.actions.toggle'), scope: 'row', icon: 'lucide:power', permission: 'setting.bot-config.status' },
    {
      key: 'setDefault',
      title: t('message.bot_config.actions.set_default'),
      scope: 'row',
      icon: 'lucide:star',
      permission: 'setting.bot-config.update',
      visible: row => !(row as unknown as BotConfigListItemDto).isDefault,
    },
    {
      key: 'delete',
      title: t('message.bot_config.actions.delete'),
      scope: 'row',
      icon: 'lucide:trash-2',
      type: 'error',
      permission: 'setting.bot-config.delete',
      visible: row => !(row as unknown as BotConfigListItemDto).isDefault,
    },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as BotConfigListItemDto | undefined
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
    case 'setDefault':
      if (row) {
        handleSetDefault(row)
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
const editingHasSecret = ref(false)
const form = ref<BotConfigFormModel>(createDefaultForm())

const modalTitle = computed(() => (form.value.basicId ? t('message.bot_config.form.edit_title') : t('message.bot_config.form.add_title')))
// 企业微信的 key 在 Webhook 地址里，无独立签名秘钥；关键词仅钉钉/飞书支持
const isWeCom = computed(() => form.value.provider === BotProviderType.WeCom)
const webhookPlaceholder = computed(() => {
  switch (form.value.provider) {
    case BotProviderType.Lark:
      return t('message.bot_config.form.webhook_url_placeholder_lark')
    case BotProviderType.WeCom:
      return t('message.bot_config.form.webhook_url_placeholder_we_com')
    default:
      return t('message.bot_config.form.webhook_url_placeholder_ding_talk')
  }
})
const secretPlaceholder = computed(() =>
  form.value.basicId && editingHasSecret.value ? t('message.bot_config.form.secret_configured') : t('message.bot_config.form.secret_placeholder'),
)

function createDefaultForm(): BotConfigFormModel {
  return {
    configCode: '',
    configName: '',
    isDefault: false,
    isEnabled: true,
    keyword: null,
    provider: BotProviderType.DingTalk,
    remark: null,
    secret: null,
    sort: 100,
    webhookUrl: '',
  }
}

function handleAdd() {
  editingHasSecret.value = false
  form.value = createDefaultForm()
  modalVisible.value = true
}

async function handleEdit(row: BotConfigListItemDto) {
  try {
    const detail = await botConfigApi.detail(row.basicId)
    if (!detail) {
      toast.warning(t('message.bot_config.message.detail_not_found'))
      return
    }

    editingHasSecret.value = detail.hasSecret
    form.value = {
      basicId: detail.basicId,
      configCode: detail.configCode,
      configName: detail.configName,
      isDefault: detail.isDefault,
      isEnabled: detail.isEnabled,
      keyword: detail.keyword ?? null,
      provider: detail.provider,
      remark: detail.remark ?? null,
      secret: null,
      sort: detail.sort,
      webhookUrl: detail.webhookUrl,
    }
    modalVisible.value = true
  }
  catch (e) {
    toast.error((e as Error).message || t('message.bot_config.message.load_detail_failed'))
  }
}

function validateForm() {
  if (!form.value.basicId && !form.value.configCode.trim()) {
    toast.warning(t('message.bot_config.message.input_config_code'))
    return false
  }

  if (!form.value.configName.trim()) {
    toast.warning(t('message.bot_config.message.input_config_name'))
    return false
  }

  if (!form.value.webhookUrl.trim()) {
    toast.warning(t('message.bot_config.message.input_webhook_url'))
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
    // 企业微信无独立秘钥/关键词，切到企微时不下发这两个字段
    const secret = isWeCom.value ? null : form.value.secret?.trim() || null
    const keyword = isWeCom.value ? null : form.value.keyword?.trim() || null
    if (form.value.basicId) {
      await botConfigApi.update({
        basicId: form.value.basicId,
        configName: form.value.configName.trim(),
        keyword,
        provider: form.value.provider,
        remark: form.value.remark,
        secret,
        sort: form.value.sort,
        webhookUrl: form.value.webhookUrl.trim(),
      })
    }
    else {
      await botConfigApi.create({
        configCode: form.value.configCode.trim(),
        configName: form.value.configName.trim(),
        isDefault: form.value.isDefault,
        isEnabled: form.value.isEnabled,
        keyword,
        provider: form.value.provider,
        remark: form.value.remark,
        secret,
        sort: form.value.sort,
        webhookUrl: form.value.webhookUrl.trim(),
      })
    }

    toast.success(t('message.bot_config.message.save_success'))
    modalVisible.value = false
    reloadList()
  }
  catch (e) {
    toast.error((e as Error).message || t('message.bot_config.message.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

function handleToggleStatus(row: BotConfigListItemDto) {
  const next = !row.isEnabled
  void dialog.confirm({
    badge: 'warning',
    title: next ? t('message.bot_config.message.enable_title') : t('message.bot_config.message.disable_title'),
    content: next
      ? t('message.bot_config.message.enable_content', { name: row.configName })
      : t('message.bot_config.message.disable_content', { name: row.configName }),
    okText: next ? t('message.bot_config.message.enable') : t('message.bot_config.message.disable'),
    cancelText: t('message.bot_config.form.cancel'),
    onOk: async () => {
      try {
        await botConfigApi.updateStatus({ basicId: row.basicId, isEnabled: next })
        toast.success(t('message.bot_config.message.status_updated'))
        reloadList()
      }
      catch (e) {
        toast.error((e as Error).message || t('message.bot_config.message.status_update_failed'))
      }
    },
  })
}

function handleSetDefault(row: BotConfigListItemDto) {
  void dialog.confirm({
    badge: 'info',
    title: t('message.bot_config.message.set_default_title'),
    content: t('message.bot_config.message.set_default_content', { name: row.configName }),
    okText: t('message.bot_config.message.set_default'),
    cancelText: t('message.bot_config.form.cancel'),
    onOk: async () => {
      try {
        await botConfigApi.setDefault({ basicId: row.basicId })
        toast.success(t('message.bot_config.message.set_default_success'))
        reloadList()
      }
      catch (e) {
        toast.error((e as Error).message || t('message.bot_config.message.set_default_failed'))
      }
    },
  })
}

function handleDelete(row: BotConfigListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('message.bot_config.message.delete_title'),
    content: t('message.bot_config.message.delete_content', { name: row.configName }),
    okText: t('message.bot_config.message.delete'),
    cancelText: t('message.bot_config.form.cancel'),
    onOk: async () => {
      try {
        await botConfigApi.delete(row.basicId)
        toast.success(t('message.bot_config.message.delete_success'))
        reloadList()
      }
      catch (e) {
        toast.error((e as Error).message || t('message.bot_config.message.delete_failed'))
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
        <XhFormFieldGroup value="configCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.bot_config.form.config_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="form.configCode"
                clearable
                :disabled="Boolean(form.basicId)"
                :placeholder="t('message.bot_config.form.config_code_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="configName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.bot_config.form.config_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.configName" clearable :placeholder="t('message.bot_config.form.config_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="provider">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.bot_config.form.provider') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.provider" :options="providerOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.bot_config.form.sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="webhookUrl" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.bot_config.form.webhook_url') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.webhookUrl" clearable :placeholder="webhookPlaceholder" autocomplete="off" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>

        <template v-if="!isWeCom">
          <XhFormFieldGroup value="secret">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('message.bot_config.form.secret') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput
                  v-model:value="form.secret"
                  type="password"
                  autocomplete="new-password"
                  :placeholder="secretPlaceholder"
                />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup value="keyword">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('message.bot_config.form.keyword') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="form.keyword" clearable :placeholder="t('message.bot_config.form.keyword_placeholder')" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
        </template>

        <template v-if="!form.basicId">
          <XhFormFieldGroup value="isEnabled">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('message.bot_config.form.is_enabled') }}</XhFieldLabel>
              <XhFieldControl>
                <XhSwitch v-model:checked="form.isEnabled" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup value="isDefault">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('message.bot_config.form.is_default') }}</XhFieldLabel>
              <XhFieldControl>
                <XhSwitch v-model:checked="form.isDefault" :disabled="!form.isEnabled" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
        </template>

        <XhFormFieldGroup value="remark" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.bot_config.form.remark') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.remark" clearable :placeholder="t('message.bot_config.form.remark_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>
