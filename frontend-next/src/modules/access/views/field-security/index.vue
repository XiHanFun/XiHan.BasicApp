<script lang="ts" setup>
import type { FieldMaskStrategy, FieldRuleRow, FieldRuleStatus, FieldTargetKind } from './use-field-security'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhFieldControl, XhFieldDescription, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, onMounted, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, renderTag, SchemaPage, statusConfirmText, XCombobox, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { FIELD_MASK_STRATEGIES, FIELD_TARGET_KINDS, fieldSecurityResource, fieldTitleKey, MAX_MASK_KEEP, messageKeys, useFieldRuleEditor, useFieldSecurityEntities } from './use-field-security'

defineOptions({ name: 'AccessFieldSecurityView' })

const { t, te, locale, getLocaleMessage } = useI18n()
/** 当前语言的全部文案键：字段显示名按它兜底查找 */
const messageKeyList = computed(() => messageKeys(getLocaleMessage(locale.value)))
const resource = fieldSecurityResource()
const catalog = useFieldSecurityEntities()
const editor = useFieldRuleEditor(catalog.find)

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

onMounted(() => {
  catalog.load().catch((error: unknown) => toast.danger((error as Error).message || t('access.field_security.msg_load_entities_failed')))
})

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对
function strategyLabel(strategy: FieldMaskStrategy): string {
  switch (strategy) {
    case 'None': return t('access.field_security.strategies.none')
    case 'Hidden': return t('access.field_security.strategies.hidden')
    case 'FullMask': return t('access.field_security.strategies.full_mask')
    case 'PartialMask': return t('access.field_security.strategies.partial_mask')
    case 'Hash': return t('access.field_security.strategies.hash')
    case 'Redact': return t('access.field_security.strategies.redact')
  }
}

function targetKindLabel(kind: FieldTargetKind): string {
  switch (kind) {
    case 'User': return t('access.field_security.target_kinds.user')
    case 'Department': return t('access.field_security.target_kinds.department')
    default: return t('access.field_security.target_kinds.role')
  }
}

function statusLabel(status: FieldRuleStatus): string {
  return status === 'Disabled' ? t('access.field_security.statuses.disabled') : t('access.field_security.statuses.active')
}

/** 实体与字段的显示名是各包登记的界面文案键；文案没装（模块关闭）时显示原码 */
function titleOf(key: string, fallback: string): string {
  return te(key) ? t(key) : fallback
}

function entityTitle(code: string): string | null {
  const entity = catalog.find(code)
  return entity ? titleOf(entity.titleKey, entity.code) : null
}

/** 字段显示名：登记的键、同页的同名键与通用字段词表依次找，都没有即字段名 */
function fieldLabel(field: { titleKey: string, name: string }): string {
  const key = fieldTitleKey(field.titleKey, field.name, messageKeyList.value)
  return key ? t(key) : field.name
}

function fieldTitle(code: string, name: string): string | null {
  const field = catalog.find(code)?.fields.find(item => item.name === name)
  return field ? fieldLabel(field) : null
}

/** 确认框里的规则名：实体 · 字段 */
function ruleName(row: FieldRuleRow): string {
  return [entityTitle(row.entity) ?? row.entity, fieldTitle(row.entity, row.field) ?? row.field].join(' · ')
}

function strategySummary(row: FieldRuleRow): string {
  const label = strategyLabel(row.strategy)
  if (row.strategy === 'PartialMask')
    return `${label} · ${t('access.field_security.summary_keep', { head: row.keepHead ?? 0, tail: row.keepTail ?? 0 })}`
  if (row.strategy === 'Redact')
    return `${label} · ${row.replacement ?? ''}`
  return label
}

/** 目录里已没有的实体或字段（包关掉、字段下线）标「已失效」 */
function renderCatalogName(title: string | null, code: string) {
  return title
    ? h('span', { title: code }, title)
    : h('span', { class: 'fls-invalid' }, [code, ' ', h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: 'warning' }, () => h(XhTagLabel, () => t('access.field_security.invalid')))])
}

const entityOptions = computed<SelectOption[]>(() => catalog.entities.value.map(entity => ({ label: titleOf(entity.titleKey, entity.code), value: entity.code })))
const targetKindOptions = computed<SelectOption[]>(() => FIELD_TARGET_KINDS.map(kind => ({ label: targetKindLabel(kind), value: kind })))
const strategyOptions = computed<SelectOption[]>(() => FIELD_MASK_STRATEGIES.map(strategy => ({ label: strategyLabel(strategy), value: strategy })))
const statusOptions = computed<SelectOption[]>(() => (['Active', 'Disabled'] as const).map(status => ({ label: statusLabel(status), value: status })))
/** 表单里只列当前字段能用的读取方式 */
const formStrategyOptions = computed(() => strategyOptions.value.map(option => ({ ...option, disabled: !editor.strategyAllowed(option.value as FieldMaskStrategy) })))
const comboEntityOptions = computed(() => catalog.entities.value.map(entity => ({ value: entity.code, label: titleOf(entity.titleKey, entity.code), description: entity.code })))
const comboFieldOptions = computed(() => (editor.currentEntity.value?.fields ?? []).map(field => ({ value: field.name, label: fieldLabel(field), description: field.name })))

const targetPlaceholder = computed(() => {
  switch (editor.form.targetKind) {
    case 'User': return t('access.field_security.ph_target_user')
    case 'Department': return t('access.field_security.ph_target_department')
    default: return t('access.field_security.ph_target_role')
  }
})

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；可排序的只放后端白名单里的） ──
const asRow = (row: object) => row as FieldRuleRow

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('access.field_security.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('access.field_security.keyword_placeholder'), width: 240, order: 0 },
  { key: 'entity', title: t('access.field_security.fields.entity'), dataType: 'enum', searchable: true, searchMultiple: true, sortable: true, options: entityOptions.value, searchPlaceholder: t('access.field_security.entity_placeholder'), minWidth: 150, order: 1, render: row => renderCatalogName(entityTitle(asRow(row).entity), asRow(row).entity) },
  { key: 'field', title: t('access.field_security.fields.field'), dataType: 'string', sortable: true, minWidth: 150, order: 2, render: row => renderCatalogName(fieldTitle(asRow(row).entity, asRow(row).field), asRow(row).field) },
  { key: 'targetKind', title: t('access.field_security.fields.target_kind'), dataType: 'enum', searchable: true, searchMultiple: true, sortable: true, options: targetKindOptions.value, searchPlaceholder: t('access.field_security.target_kind_placeholder'), width: 110, order: 3, render: row => targetKindLabel(asRow(row).targetKind) },
  { key: 'targetName', title: t('access.field_security.fields.target'), dataType: 'string', minWidth: 160, order: 4, render: row => asRow(row).targetName ?? asRow(row).targetId },
  { key: 'strategy', title: t('access.field_security.fields.strategy'), dataType: 'enum', searchable: true, searchMultiple: true, sortable: true, options: strategyOptions.value, searchPlaceholder: t('access.field_security.strategy_placeholder'), minWidth: 160, order: 5, render: row => strategySummary(asRow(row)) },
  { key: 'isEditable', title: t('access.field_security.fields.write_mode'), dataType: 'boolean', sortable: true, width: 90, order: 6, render: row => asRow(row).isEditable ? renderTag(t('access.field_security.write_only'), 'info') : renderTag(t('access.field_security.read_only'), 'neutral') },
  { key: 'status', title: t('access.field_security.fields.status'), dataType: 'enum', searchable: true, searchMultiple: true, sortable: true, options: statusOptions.value, searchPlaceholder: t('access.field_security.status_placeholder'), width: 90, order: 7, render: row => renderTag(statusLabel(asRow(row).status), asRow(row).status === 'Active' ? 'success' : 'danger') },
  { key: 'remark', title: t('access.field_security.fields.remark'), dataType: 'text', visible: false, order: 7.5 },
  { key: 'createdAt', title: t('access.field_security.fields.created_at'), dataType: 'datetime', sortable: true, minWidth: 170, order: 8 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'access.field-security',
  pageName: t('access.field_security.title'),
  exportPermission: 'access.field-security.read',
  batchRemovable: true,
  removePermission: 'access.field-security.delete',
  statusPermission: 'access.field-security.update',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('access.field_security.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'access.field-security.create' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'access.field-security.update' },
    { key: 'toggle', title: t('access.field_security.actions.toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => statusConfirmText(t, asRow(row).status === 'Active', ruleName(asRow(row))), permission: 'access.field-security.update' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => deleteConfirmText(t, ruleName(asRow(row))), permission: 'access.field-security.delete' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      editor.startCreate().catch((error: unknown) => toast.danger((error as Error).message || t('access.field_security.msg_load_target_failed')))
      break
    case 'edit':
      if (row)
        editor.startEdit(row)
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

function onSearchTargets(keyword: string) {
  editor.searchTargets(keyword).catch((error: unknown) => toast.danger((error as Error).message || t('access.field_security.msg_load_target_failed')))
}

/** 4.x 的提交前校验：缺什么先提示，不发请求 */
function validate(): boolean {
  const form = editor.form
  if (!form.targetId) {
    toast.warning(t('access.field_security.msg_target_required'))
    return false
  }
  if (!form.entity) {
    toast.warning(t('access.field_security.msg_entity_required'))
    return false
  }
  if (!form.field) {
    toast.warning(t('access.field_security.msg_field_required'))
    return false
  }
  if (form.strategy === 'PartialMask' && form.keepHead + form.keepTail === 0) {
    toast.warning(t('access.field_security.msg_keep_zero'))
    return false
  }
  if (form.strategy === 'Redact' && !form.replacement.trim()) {
    toast.warning(t('access.field_security.msg_replacement_required'))
    return false
  }
  return true
}

async function onSubmit() {
  if (!validate())
    return
  if (await editor.submit()) {
    toast.success(t('common.messages.save_success'))
    reload()
  }
}

async function removeRow(row: FieldRuleRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

async function toggleStatus(row: FieldRuleRow) {
  try {
    await resource.updateStatus?.(row.id, row.status !== 'Active')
    toast.success(t('common.messages.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.status_failed'))
  }
}

const modalTitle = computed(() => (editor.isEdit.value ? t('access.field_security.edit_title') : t('access.field_security.create_title')))
const fieldHint = computed(() => {
  const field = editor.currentField.value
  if (field && !field.isText)
    return t('access.field_security.hint_non_text')
  if (field && !field.isNullable)
    return t('access.field_security.hint_not_nullable')
  return null
})
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <XEditModal
      v-model:show="editor.open.value"
      :title="modalTitle"
      :loading="editor.submitting.value"
      :form-id="editFormId"
    >
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="field-rule-form" @submit="onSubmit">
        <XhFieldRoot :invalid="!!editor.fieldError('targetKind')" required>
          <XhFieldLabel>{{ t('access.field_security.fields.target_kind') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect :value="editor.form.targetKind" name="targetKind" :options="targetKindOptions" @update:value="value => editor.changeTargetKind(value as FieldTargetKind)" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('targetKind')">
            {{ editor.fieldError('targetKind') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('targetId')" required>
          <XhFieldLabel>{{ t('access.field_security.fields.target') }}</XhFieldLabel>
          <XhFieldControl>
            <XCombobox
              class="fls-combobox"
              remote
              :options="editor.targetOptions.value"
              :value="editor.form.targetId || null"
              :loading="editor.targetLoading.value"
              :placeholder="targetPlaceholder"
              @search="onSearchTargets"
              @update:value="value => editor.form.targetId = typeof value === 'string' ? value : ''"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('targetId')">
            {{ editor.fieldError('targetId') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('entity')" required>
          <XhFieldLabel>{{ t('access.field_security.fields.entity') }}</XhFieldLabel>
          <XhFieldControl>
            <XCombobox
              class="fls-combobox"
              :options="comboEntityOptions"
              :value="editor.form.entity || null"
              :loading="catalog.loading.value"
              :placeholder="t('access.field_security.ph_entity')"
              @update:value="value => editor.changeEntity(typeof value === 'string' ? value : '')"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('entity')">
            {{ editor.fieldError('entity') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('field')" required>
          <XhFieldLabel>{{ t('access.field_security.fields.field') }}</XhFieldLabel>
          <XhFieldControl>
            <XCombobox
              class="fls-combobox"
              :options="comboFieldOptions"
              :value="editor.form.field || null"
              :disabled="!editor.currentEntity.value"
              :placeholder="t('access.field_security.ph_field')"
              @update:value="value => editor.changeField(typeof value === 'string' ? value : '')"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('field')">
            {{ editor.fieldError('field') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('strategy')" required>
          <XhFieldLabel>{{ t('access.field_security.fields.strategy') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect :value="editor.form.strategy" name="strategy" :options="formStrategyOptions" @update:value="value => editor.changeStrategy(value as FieldMaskStrategy)" />
          </XhFieldControl>
          <XhFieldDescription v-if="fieldHint">
            {{ fieldHint }}
          </XhFieldDescription>
          <XhFieldErrorText v-if="editor.fieldError('strategy')">
            {{ editor.fieldError('strategy') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('access.field_security.fields.editable') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="editor.form.isEditable" name="isEditable" :disabled="!editor.isMasked.value" />
          </XhFieldControl>
          <XhFieldDescription>
            {{ editor.isMasked.value ? t('access.field_security.hint_write_only') : t('access.field_security.hint_plain_read_only') }}
          </XhFieldDescription>
        </XhFieldRoot>
        <template v-if="editor.form.strategy === 'PartialMask'">
          <XhFieldRoot :invalid="!!editor.fieldError('keepHead')" required>
            <XhFieldLabel>{{ t('access.field_security.fields.keep_head') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="editor.form.keepHead" name="keepHead" :min="0" :max="MAX_MASK_KEEP" />
            </XhFieldControl>
            <XhFieldErrorText v-if="editor.fieldError('keepHead')">
              {{ editor.fieldError('keepHead') }}
            </XhFieldErrorText>
          </XhFieldRoot>
          <XhFieldRoot :invalid="!!editor.fieldError('keepTail')" required>
            <XhFieldLabel>{{ t('access.field_security.fields.keep_tail') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="editor.form.keepTail" name="keepTail" :min="0" :max="MAX_MASK_KEEP" />
            </XhFieldControl>
            <XhFieldErrorText v-if="editor.fieldError('keepTail')">
              {{ editor.fieldError('keepTail') }}
            </XhFieldErrorText>
          </XhFieldRoot>
        </template>
        <XhFieldRoot v-if="editor.form.strategy === 'Redact'" :invalid="!!editor.fieldError('replacement')" required>
          <XhFieldLabel>{{ t('access.field_security.fields.replacement') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.replacement" name="replacement" clearable :max-length="100" :placeholder="t('access.field_security.ph_replacement')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('replacement')">
            {{ editor.fieldError('replacement') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('access.field_security.fields.status') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="editor.form.status" name="status" :options="statusOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" :invalid="!!editor.fieldError('remark')">
          <XhFieldLabel>{{ t('access.field_security.fields.remark') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.remark" name="remark" clearable :placeholder="t('access.field_security.ph_remark')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('remark')">
            {{ editor.fieldError('remark') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <p v-if="editor.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ editor.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>

<style scoped>
/* 组合框默认按内容定宽，表单里与下拉框一样铺满字段 */
.fls-combobox {
  inline-size: 100%;
}

/* 列 render 的 h() 节点不带本页 scope，样式须经 :deep() 才能命中 */
:deep(.fls-invalid) {
  display: inline-flex;
  align-items: center;
  gap: var(--xh-space-1);
  color: var(--xh-fg-muted);
}
</style>
