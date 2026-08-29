<script setup lang="ts">
import type { EmailConfigListItemDto } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, emailConfigApi, querySortsFromSchema } from '@/api'
import { SchemaPage, XEditModal, XInput, XNumberInput } from '~/components'
import { dialog, toast } from '~/composables'

defineOptions({ name: 'SettingEmailConfigPage' })

interface EmailConfigFormModel {
  acceptInvalidCertificate: boolean
  basicId?: string
  configCode: string
  configName: string
  fromEmail: string
  fromName: string
  isBodyHtml: boolean
  isDefault: boolean
  isEnabled: boolean
  password: string | null
  remark: string | null
  smtpHost: string
  smtpPort: number
  sort: number
  userName: string | null
  useSsl: boolean
}

const { t } = useI18n()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadList() {
  void schemaPageRef.value?.reload()
}

// SchemaSelectOption.value 仅支持 string | number；布尔搜索项用 1/0，page() 里转回 boolean
const defaultOptions = computed(() => [
  { label: t('message.email_config.default.is_default'), value: 1 },
  { label: t('message.email_config.default.not_default'), value: 0 },
])
const enabledOptions = computed(() => [
  { label: t('message.email_config.enabled.enabled'), value: 1 },
  { label: t('message.email_config.enabled.disabled'), value: 0 },
])

function pickBoolean(value: unknown): boolean | undefined {
  return value === undefined || value === null || value === '' ? undefined : Boolean(Number(value))
}

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；order 控顺序） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('message.email_config.columns.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('message.email_config.columns.keyword_placeholder'), order: 0 },
  { key: 'configCode', title: t('message.email_config.columns.config_code'), dataType: 'string', sortable: true, minWidth: 140, order: 1 },
  { key: 'configName', title: t('message.email_config.columns.config_name'), dataType: 'string', sortable: true, minWidth: 140, order: 2 },
  { key: 'smtpHost', title: t('message.email_config.columns.smtp_host'), dataType: 'string', sortable: true, minWidth: 160, order: 3 },
  { key: 'smtpPort', title: t('message.email_config.columns.smtp_port'), dataType: 'number', sortable: true, width: 90, order: 4 },
  { key: 'fromEmail', title: t('message.email_config.columns.from_email'), dataType: 'string', sortable: true, minWidth: 170, order: 5 },
  { key: 'fromName', title: t('message.email_config.columns.from_name'), dataType: 'string', sortable: true, minWidth: 120, order: 6 },
  {
    key: 'isDefault',
    title: t('message.email_config.columns.is_default'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: defaultOptions.value,
    searchPlaceholder: t('message.email_config.columns.is_default_placeholder'),
    width: 90,
    order: 7,
    render: (row) => {
      const isDefault = (row as unknown as EmailConfigListItemDto).isDefault
      return isDefault
        ? h(XhTagRoot, { variant: 'outline', tone: 'warning' }, () => h(XhTagLabel, () => t('message.email_config.tag.default')))
        : h('span', { style: 'opacity:.45' }, '—')
    },
  },
  {
    key: 'isEnabled',
    title: t('message.email_config.columns.status'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: enabledOptions.value,
    searchPlaceholder: t('message.email_config.columns.status_placeholder'),
    width: 90,
    order: 8,
    render: (row) => {
      const enabled = (row as unknown as EmailConfigListItemDto).isEnabled
      return h(XhTagRoot, { variant: 'outline', tone: enabled ? 'success' : 'danger' }, () => h(XhTagLabel, () => enabled ? t('message.email_config.tag.enabled') : t('message.email_config.tag.disabled')))
    },
  },
  { key: 'sort', title: t('message.email_config.columns.sort'), dataType: 'number', sortable: true, width: 80, order: 9 },
  { key: 'createdTime', title: t('message.email_config.columns.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 10 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'setting.email-config',
  exportPermission: 'setting.email-config.export',
  pageName: t('message.email_config.page_name'),
  statusPermission: 'setting.email-config.status',
  rowKey: 'basicId',
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return emailConfigApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序等通用过滤统一走 conditions
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        isDefault: pickBoolean(f.isDefault),
        isEnabled: pickBoolean(f.isEnabled),
        keyword: (f.keyword as string | undefined)?.trim() || undefined,
      }) as unknown as Promise<import('@/api').PageResult<Record<string, unknown>>>
    },
    updateStatus: (id, enabled) => emailConfigApi.updateStatus({ basicId: id, isEnabled: enabled }),
  },
  actions: [
    { key: 'create', title: t('message.email_config.actions.create'), scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'setting.email-config.create' },
    { key: 'edit', title: t('message.email_config.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'setting.email-config.update' },
    { key: 'toggle', title: t('message.email_config.actions.toggle'), scope: 'row', icon: 'lucide:power', permission: 'setting.email-config.status' },
    {
      key: 'setDefault',
      title: t('message.email_config.actions.set_default'),
      scope: 'row',
      icon: 'lucide:star',
      permission: 'setting.email-config.update',
      visible: row => !(row as unknown as EmailConfigListItemDto).isDefault,
    },
    {
      key: 'delete',
      title: t('message.email_config.actions.delete'),
      scope: 'row',
      icon: 'lucide:trash-2',
      type: 'error',
      permission: 'setting.email-config.delete',
      visible: row => !(row as unknown as EmailConfigListItemDto).isDefault,
    },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as EmailConfigListItemDto | undefined
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
const editingHasPassword = ref(false)
const form = ref<EmailConfigFormModel>(createDefaultForm())

const modalTitle = computed(() => (form.value.basicId ? t('message.email_config.form.edit_title') : t('message.email_config.form.add_title')))
const passwordPlaceholder = computed(() =>
  form.value.basicId && editingHasPassword.value ? t('message.email_config.form.password_configured') : t('message.email_config.form.password_placeholder'),
)

function createDefaultForm(): EmailConfigFormModel {
  return {
    acceptInvalidCertificate: false,
    configCode: '',
    configName: '',
    fromEmail: '',
    fromName: '',
    isBodyHtml: true,
    isDefault: false,
    isEnabled: true,
    password: null,
    remark: null,
    smtpHost: '',
    smtpPort: 587,
    sort: 100,
    userName: null,
    useSsl: true,
  }
}

function handleAdd() {
  editingHasPassword.value = false
  form.value = createDefaultForm()
  modalVisible.value = true
}

async function handleEdit(row: EmailConfigListItemDto) {
  try {
    const detail = await emailConfigApi.detail(row.basicId)
    if (!detail) {
      toast.warning(t('message.email_config.message.detail_not_found'))
      return
    }

    editingHasPassword.value = detail.hasPassword
    form.value = {
      acceptInvalidCertificate: detail.acceptInvalidCertificate,
      basicId: detail.basicId,
      configCode: detail.configCode,
      configName: detail.configName,
      fromEmail: detail.fromEmail,
      fromName: detail.fromName,
      isBodyHtml: detail.isBodyHtml,
      isDefault: detail.isDefault,
      isEnabled: detail.isEnabled,
      password: null,
      remark: detail.remark ?? null,
      smtpHost: detail.smtpHost,
      smtpPort: detail.smtpPort,
      sort: detail.sort,
      userName: detail.userName ?? null,
      useSsl: detail.useSsl,
    }
    modalVisible.value = true
  }
  catch (e) {
    toast.error((e as Error).message || t('message.email_config.message.load_detail_failed'))
  }
}

function validateForm() {
  if (!form.value.basicId && !form.value.configCode.trim()) {
    toast.warning(t('message.email_config.message.input_config_code'))
    return false
  }

  if (!form.value.configName.trim()) {
    toast.warning(t('message.email_config.message.input_config_name'))
    return false
  }

  if (!form.value.smtpHost.trim()) {
    toast.warning(t('message.email_config.message.input_smtp_host'))
    return false
  }

  if (!form.value.fromEmail.trim() || !form.value.fromEmail.includes('@')) {
    toast.warning(t('message.email_config.message.input_from_email'))
    return false
  }

  if (!form.value.fromName.trim()) {
    toast.warning(t('message.email_config.message.input_from_name'))
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
      await emailConfigApi.update({
        acceptInvalidCertificate: form.value.acceptInvalidCertificate,
        basicId: form.value.basicId,
        configName: form.value.configName.trim(),
        fromEmail: form.value.fromEmail.trim(),
        fromName: form.value.fromName.trim(),
        isBodyHtml: form.value.isBodyHtml,
        password: form.value.password?.trim() || null,
        remark: form.value.remark,
        smtpHost: form.value.smtpHost.trim(),
        smtpPort: form.value.smtpPort,
        sort: form.value.sort,
        userName: form.value.userName,
        useSsl: form.value.useSsl,
      })
    }
    else {
      await emailConfigApi.create({
        acceptInvalidCertificate: form.value.acceptInvalidCertificate,
        configCode: form.value.configCode.trim(),
        configName: form.value.configName.trim(),
        fromEmail: form.value.fromEmail.trim(),
        fromName: form.value.fromName.trim(),
        isBodyHtml: form.value.isBodyHtml,
        isDefault: form.value.isDefault,
        isEnabled: form.value.isEnabled,
        password: form.value.password?.trim() || null,
        remark: form.value.remark,
        smtpHost: form.value.smtpHost.trim(),
        smtpPort: form.value.smtpPort,
        sort: form.value.sort,
        userName: form.value.userName,
        useSsl: form.value.useSsl,
      })
    }

    toast.success(t('message.email_config.message.save_success'))
    modalVisible.value = false
    reloadList()
  }
  catch (e) {
    toast.error((e as Error).message || t('message.email_config.message.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

function handleToggleStatus(row: EmailConfigListItemDto) {
  const next = !row.isEnabled
  void dialog.confirm({
    badge: 'warning',
    title: next ? t('message.email_config.message.enable_title') : t('message.email_config.message.disable_title'),
    content: next
      ? t('message.email_config.message.enable_content', { name: row.configName })
      : t('message.email_config.message.disable_content', { name: row.configName }),
    okText: next ? t('message.email_config.message.enable') : t('message.email_config.message.disable'),
    cancelText: t('message.email_config.form.cancel'),
    onOk: async () => {
      try {
        await emailConfigApi.updateStatus({ basicId: row.basicId, isEnabled: next })
        toast.success(t('message.email_config.message.status_updated'))
        reloadList()
      }
      catch (e) {
        toast.error((e as Error).message || t('message.email_config.message.status_update_failed'))
      }
    },
  })
}

function handleSetDefault(row: EmailConfigListItemDto) {
  void dialog.confirm({
    badge: 'info',
    title: t('message.email_config.message.set_default_title'),
    content: t('message.email_config.message.set_default_content', { name: row.configName }),
    okText: t('message.email_config.message.set_default'),
    cancelText: t('message.email_config.form.cancel'),
    onOk: async () => {
      try {
        await emailConfigApi.setDefault({ basicId: row.basicId })
        toast.success(t('message.email_config.message.set_default_success'))
        reloadList()
      }
      catch (e) {
        toast.error((e as Error).message || t('message.email_config.message.set_default_failed'))
      }
    },
  })
}

function handleDelete(row: EmailConfigListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('message.email_config.message.delete_title'),
    content: t('message.email_config.message.delete_content', { name: row.configName }),
    okText: t('message.email_config.message.delete'),
    cancelText: t('message.email_config.form.cancel'),
    onOk: async () => {
      try {
        await emailConfigApi.delete(row.basicId)
        toast.success(t('message.email_config.message.delete_success'))
        reloadList()
      }
      catch (e) {
        toast.error((e as Error).message || t('message.email_config.message.delete_failed'))
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
            <XhFieldLabel>{{ t('message.email_config.form.config_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="form.configCode"
                clearable
                :disabled="Boolean(form.basicId)"
                :placeholder="t('message.email_config.form.config_code_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="configName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.email_config.form.config_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.configName" clearable :placeholder="t('message.email_config.form.config_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="smtpHost">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.email_config.form.smtp_host') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.smtpHost" clearable :placeholder="t('message.email_config.form.smtp_host_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="smtpPort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.email_config.form.smtp_port') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.smtpPort" :min="1" :max="65535" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="fromEmail">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.email_config.form.from_email') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.fromEmail" clearable :placeholder="t('message.email_config.form.from_email_placeholder')" autocomplete="off" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="fromName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.email_config.form.from_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.fromName" clearable :placeholder="t('message.email_config.form.from_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="userName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.email_config.form.user_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.userName" clearable :placeholder="t('message.email_config.form.user_name_placeholder')" autocomplete="off" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="password">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.email_config.form.password') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="form.password"
                type="password"
                autocomplete="new-password"
                :placeholder="passwordPlaceholder"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="useSsl">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.email_config.form.use_ssl') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="form.useSsl" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="acceptInvalidCertificate">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.email_config.form.accept_invalid_certificate') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="form.acceptInvalidCertificate" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="isBodyHtml">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.email_config.form.is_body_html') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="form.isBodyHtml" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.email_config.form.sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>

        <template v-if="!form.basicId">
          <XhFormFieldGroup value="isEnabled">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('message.email_config.form.is_enabled') }}</XhFieldLabel>
              <XhFieldControl>
                <XhSwitch v-model:checked="form.isEnabled" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup value="isDefault">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('message.email_config.form.is_default') }}</XhFieldLabel>
              <XhFieldControl>
                <XhSwitch v-model:checked="form.isDefault" :disabled="!form.isEnabled" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
        </template>

        <XhFormFieldGroup value="remark" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.email_config.form.remark') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.remark" clearable :placeholder="t('message.email_config.form.remark_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>
