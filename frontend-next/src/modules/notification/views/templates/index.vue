<script lang="ts" setup>
import type { TemplateForm, TemplateRow } from './template'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { TemplateChannel } from '~/contracts'
import type { SelectOption } from '~/types'
import {
  XhDescriptionsItem,
  XhDescriptionsLabel,
  XhDescriptionsRoot,
  XhDescriptionsValue,
  XhDialogBody,
  XhDialogCloseTrigger,
  XhDialogContent,
  XhDialogHeader,
  XhDialogRoot,
  XhDialogTitle,
  XhFieldControl,
  XhFieldDescription,
  XhFieldLabel,
  XhFieldRoot,
  XhFormRoot,
  XhSwitch,
} from '@xihan-ui/vue'
import { computed, reactive, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { renderTag, SchemaPage, statusConfirmText, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { TemplateChannel as CHANNELS, messageTemplates as templatesApi } from '~/contracts'
import { useAuthStore } from '~/stores'
import { enabledLabel, enabledOptions, errorText, templateChannelLabel } from '../../labels'
import { canOverrideTemplate, emptyTemplateForm, findTemplateProblem, hasSubject, isEmailChannel, isOwnTemplate, templateProblemMessage, templateResource, toCreateBody, toOverrideForm, toTemplateForm, toUpdateBody } from './template'

defineOptions({ name: 'NotificationTemplatesView' })

const { t } = useI18n()
const resource = templateResource(t)
const auth = useAuthStore()
/** 平台一侧：平台的模板是自己的；租户一侧看得到平台的、只能覆盖（4.x 同） */
const isPlatformCaller = () => auth.tenantId === null

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

const asRow = (row: object) => row as TemplateRow
const channelOptions = computed<SelectOption[]>(() => CHANNELS.map(channel => ({ label: templateChannelLabel(t, channel), value: channel })))
const statusOptions = computed<SelectOption[]>(() => enabledOptions(t))

function yesNo(value: boolean): string {
  return value ? t('common.statuses.yes') : t('common.statuses.no')
}

function ownerLabel(row: TemplateRow): string {
  return row.isPlatform ? t('notification.templates.owner_platform') : t('notification.templates.owner_tenant')
}

const isOwn = (row: object) => isOwnTemplate(asRow(row), isPlatformCaller())

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；可排序的只放后端白名单里的：编码、排序） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('notification.templates.col_keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('notification.templates.search_keyword_placeholder'), order: 0 },
  { key: 'code', title: t('notification.templates.col_code'), dataType: 'string', sortable: true, minWidth: 200, order: 10 },
  { key: 'channel', title: t('notification.templates.col_channel'), dataType: 'enum', searchable: true, searchMultiple: true, options: channelOptions.value, searchPlaceholder: t('notification.templates.search_channel_placeholder'), width: 100, order: 11, render: row => templateChannelLabel(t, asRow(row).channel) },
  { key: 'name', title: t('notification.templates.col_name'), dataType: 'string', minWidth: 140, order: 12 },
  { key: 'subject', title: t('notification.templates.col_subject'), dataType: 'string', minWidth: 200, order: 13 },
  { key: 'isPlatform', title: t('notification.templates.col_owner'), dataType: 'enum', width: 90, order: 14, render: row => renderTag(ownerLabel(asRow(row)), asRow(row).isPlatform ? 'info' : 'neutral') },
  { key: 'isHtml', title: t('notification.templates.col_is_html'), dataType: 'enum', width: 80, order: 15, render: row => yesNo(asRow(row).isHtml) },
  { key: 'description', title: t('notification.templates.col_description'), dataType: 'string', minWidth: 220, order: 16 },
  { key: 'isEnabled', title: t('notification.templates.col_status'), dataType: 'enum', searchable: true, options: statusOptions.value, searchPlaceholder: t('notification.templates.search_status_placeholder'), width: 90, order: 17, render: row => renderTag(enabledLabel(t, asRow(row).isEnabled), asRow(row).isEnabled ? 'success' : 'danger') },
  { key: 'order', title: t('notification.templates.col_order'), dataType: 'number', sortable: true, width: 80, order: 18 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'notification.templates',
  pageName: t('notification.templates.title'),
  exportPermission: 'notification.message-templates.read',
  statusPermission: 'notification.message-templates.update',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('notification.templates.action_create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'notification.message-templates.create' },
    { key: 'view', title: t('notification.templates.action_view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('notification.templates.action_edit'), scope: 'row', icon: 'lucide:pen', visible: isOwn, permission: 'notification.message-templates.update' },
    { key: 'override', title: t('notification.templates.action_override'), scope: 'row', icon: 'lucide:copy-plus', visible: row => canOverrideTemplate(asRow(row), isPlatformCaller()), permission: 'notification.message-templates.create' },
    { key: 'toggle', title: t('notification.templates.action_toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => statusConfirmText(t, asRow(row).isEnabled, asRow(row).name), visible: isOwn, permission: 'notification.message-templates.update' },
    { key: 'delete', title: t('notification.templates.action_delete'), scope: 'row', tone: 'danger', icon: 'lucide:trash-2', confirm: true, confirmText: t('notification.templates.confirm_delete'), visible: isOwn, permission: 'notification.message-templates.delete' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      openCreate()
      break
    case 'view':
      if (row)
        void openDetail(row)
      break
    case 'edit':
      if (row)
        void openEdit(row)
      break
    case 'override':
      if (row)
        void openOverride(row)
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

// ── 详情 ─────────────────────────────────────────────────────────
const detailVisible = ref(false)
const currentDetail = ref<TemplateRow | null>(null)

async function openDetail(row: TemplateRow) {
  try {
    currentDetail.value = await templatesApi.get({ id: row.id })
    detailVisible.value = true
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.templates.msg_load_detail_failed'))
  }
}

// ── 新建 / 编辑 ─────────────────────────────────────────────────
const modalVisible = ref(false)
const submitting = ref(false)
const submitError = ref<null | string>(null)
const form = reactive<TemplateForm>(emptyTemplateForm())
/** 覆盖平台模板：照平台那份新建，编码与渠道不改 */
const overriding = ref(false)
const modalTitle = computed(() => {
  if (form.id)
    return t('notification.templates.edit_title')
  return overriding.value ? t('notification.templates.override_title') : t('notification.templates.add_title')
})
const emailChannel = computed(() => isEmailChannel(form.channel))
const subjectChannel = computed(() => hasSubject(form.channel))
/** 站内通知的主题即通知标题 */
const subjectLabel = computed(() => form.channel === 'InApp' ? t('notification.templates.form_notice_title') : t('notification.templates.form_subject'))
const codeLocked = computed(() => Boolean(form.id) || overriding.value)

function openCreate() {
  Object.assign(form, emptyTemplateForm())
  overriding.value = false
  submitError.value = null
  modalVisible.value = true
}

async function openOverride(row: TemplateRow) {
  try {
    Object.assign(form, toOverrideForm(await templatesApi.get({ id: row.id })))
    overriding.value = true
    submitError.value = null
    modalVisible.value = true
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.templates.msg_load_failed'))
  }
}

async function openEdit(row: TemplateRow) {
  try {
    Object.assign(form, toTemplateForm(await templatesApi.get({ id: row.id })))
    overriding.value = false
    submitError.value = null
    modalVisible.value = true
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.templates.msg_load_failed'))
  }
}

function changeChannel(value: unknown) {
  form.channel = value as TemplateChannel
}

async function onSubmit() {
  const problem = findTemplateProblem(form)
  if (problem) {
    toast.warning(templateProblemMessage(t, problem))
    return
  }
  submitting.value = true
  submitError.value = null
  try {
    if (form.id) {
      await templatesApi.update(toUpdateBody({ ...form, id: form.id }))
      toast.success(t('notification.templates.msg_update_success'))
    }
    else {
      await templatesApi.create(toCreateBody(form))
      toast.success(t('notification.templates.msg_create_success'))
    }
    modalVisible.value = false
    reload()
  }
  catch (error) {
    submitError.value = errorText(error) || t('notification.templates.msg_save_failed')
  }
  finally {
    submitting.value = false
  }
}

// ── 启停 / 删除 ─────────────────────────────────────────────────
async function toggleStatus(row: TemplateRow) {
  try {
    await resource.updateStatus?.(row.id, !row.isEnabled)
    toast.success(t('notification.templates.msg_status_updated'))
    reload()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.templates.msg_status_update_failed'))
  }
}

async function removeRow(row: TemplateRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('notification.templates.msg_delete_success'))
    reload()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.templates.msg_delete_failed'))
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <!-- 新建 / 编辑 -->
    <XEditModal v-model:show="modalVisible" :title="modalTitle" :loading="submitting" :form-id="editFormId" width="760px">
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="template-form" @submit="onSubmit">
        <XhFieldRoot :required="!form.id">
          <XhFieldLabel>{{ t('notification.templates.form_code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.code" name="code" clearable :max-length="64" :disabled="codeLocked" :placeholder="t('notification.templates.form_code_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('notification.templates.form_channel') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect :value="form.channel" name="channel" :disabled="codeLocked" :options="channelOptions" @update:value="changeChannel" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('notification.templates.form_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.name" name="name" clearable :max-length="128" :placeholder="t('notification.templates.form_name_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.templates.form_order') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="form.order" name="order" :min="0" :max="100000" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-if="subjectChannel" class="xh-span-2">
          <XhFieldLabel>{{ subjectLabel }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.subject" name="subject" clearable :max-length="256" :placeholder="emailChannel ? t('notification.templates.form_subject_placeholder') : undefined" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" required>
          <XhFieldLabel>{{ t('notification.templates.form_content') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.content" name="content" type="textarea" show-count :max-length="100000" :autosize="{ minRows: 6, maxRows: 16 }" :placeholder="t('notification.templates.form_content_placeholder')" />
          </XhFieldControl>
          <XhFieldDescription>{{ t('notification.templates.form_content_hint') }}</XhFieldDescription>
        </XhFieldRoot>
        <XhFieldRoot v-if="emailChannel">
          <XhFieldLabel>{{ t('notification.templates.form_is_html') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="form.isHtml" name="isHtml" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-if="!form.id">
          <XhFieldLabel>{{ t('notification.templates.form_is_enabled') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="form.isEnabled" name="isEnabled" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('notification.templates.form_description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.description" name="description" clearable :max-length="512" :placeholder="t('notification.templates.form_description_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('notification.templates.form_remark') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.remark" name="remark" clearable :max-length="500" :placeholder="t('notification.templates.form_remark_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <p v-if="submitError" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ submitError }}
        </p>
      </XhFormRoot>
    </XEditModal>

    <!-- 详情 -->
    <XhDialogRoot v-model:open="detailVisible">
      <XhDialogContent style="--xh-dialog-max-w: 45rem">
        <XhDialogHeader>
          <XhDialogTitle>{{ t('notification.templates.detail_title') }}</XhDialogTitle>
        </XhDialogHeader>
        <XhDialogCloseTrigger />
        <XhDialogBody v-if="currentDetail" class="template-detail">
          <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.templates.detail_code') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentDetail.code }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.templates.detail_channel') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ templateChannelLabel(t, currentDetail.channel) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.templates.detail_name') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentDetail.name }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.templates.detail_status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ enabledLabel(t, currentDetail.isEnabled) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.templates.col_owner') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ ownerLabel(currentDetail) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
          <XhDescriptionsRoot :columns="1" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.templates.detail_subject') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentDetail.subject || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.templates.detail_description') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentDetail.description || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
          <div class="template-detail__caption">
            {{ t('notification.templates.detail_content_title') }}
          </div>
          <pre class="template-detail__content">{{ currentDetail.content }}</pre>
        </XhDialogBody>
      </XhDialogContent>
    </XhDialogRoot>
  </SchemaPage>
</template>

<style scoped>
.template-detail {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.template-detail__caption {
  font-size: var(--xh-text-secondary-size);
  color: var(--xh-fg-subtle);
}

/* 模板正文按源码显示（4.x 同）：保留换行、限高滚动 */
.template-detail__content {
  max-height: 20rem;
  margin: 0;
  padding: var(--xh-space-3);
  overflow: auto;
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-text-secondary-size);
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-word;
  background: var(--xh-bg-muted);
  border: 1px solid var(--xh-border-subtle);
  border-radius: var(--xh-shape-inset);
}
</style>
