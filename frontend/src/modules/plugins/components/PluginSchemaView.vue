<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { PluginFormValue, PluginRow, PluginSchemaField } from '../schema-view'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import { XhEmptyStateDescription, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldErrorText, XhFieldRoot, XhFormRoot, XhSwitch } from '@xihan-ui/vue'
import { computed, reactive, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, SchemaPage, XEditModal, XFieldLabel, XInput, XNumberInput } from '~/components'
import { toast } from '~/composables'
import { ApiError } from '~/request'
import { findPluginPage, invokePlugin, pluginPermission } from '../runtime'
import { columnDataType, formRequest, initialForm, pageRows, parseSchemaView, rowKeyOf } from '../schema-view'

defineOptions({ name: 'PluginSchemaView' })

const props = defineProps<{
  /** 菜单节点的组件路径：`plugins/<短名>/<页>/index` */
  component: string
}>()

const { t } = useI18n()

const found = computed(() => findPluginPage(props.component))
const view = computed(() => found.value
  ? parseSchemaView(found.value.page.viewJson)
  : null)
const rowKey = computed(() => view.value
  ? rowKeyOf(view.value)
  : 'id')
const formFields = computed<readonly PluginSchemaField[]>(() => view.value?.form ?? [])

const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

/** 行的显示名：第一列的值，用在删除确认里 */
function rowName(row: PluginRow): string {
  const first = view.value?.columns[0]?.key
  return first
    ? String(row[first] ?? '')
    : String(row[rowKey.value] ?? '')
}

const schema = computed<PageSchema | null>(() => {
  const current = found.value
  const description = view.value
  if (!current || !description)
    return null
  const { plugin, page } = current
  const fields: ListFieldSchema[] = [
    {
      key: 'keyword',
      title: t('plugins.page.search'),
      dataType: 'string',
      visible: false,
      searchable: true,
      searchPlaceholder: t('plugins.page.keyword_placeholder'),
      order: 0,
    },
    ...description.columns.map((column, index): ListFieldSchema => ({
      key: column.key,
      title: t(column.titleKey),
      dataType: columnDataType(column.type),
      minWidth: 120,
      order: index + 1,
    })),
  ]
  return {
    pageCode: `plugins.${page.code}`,
    pageName: page.titleKey,
    rowKey: rowKey.value,
    fields,
    resource: {
      page: async params => pageRows((await invokePlugin<{ items?: PluginRow[] }>(plugin, description.list, {})).items ?? [], description, params),
      remove: description.delete
        ? async (id) => {
          await invokePlugin(plugin, description.delete!, { [rowKey.value]: id })
        }
        : undefined,
    },
    actions: [
      ...(description.create
        ? [
            {
              key: 'create',
              title: t('plugins.page.create'),
              scope: 'page' as const,
              tone: 'brand' as const,
              icon: 'lucide:plus',
              permission: pluginPermission(plugin, description.create),
            },
          ]
        : []),
      ...(description.update
        ? [
            {
              key: 'edit',
              title: t('common.actions.edit'),
              scope: 'row' as const,
              icon: 'lucide:pencil',
              permission: pluginPermission(plugin, description.update),
            },
          ]
        : []),
      ...(description.delete
        ? [
            {
              key: 'delete',
              title: t('common.actions.delete'),
              scope: 'row' as const,
              icon: 'lucide:trash-2',
              tone: 'danger' as const,
              confirm: true,
              confirmText: (row: PluginRow) => deleteConfirmText(t, rowName(row)),
              permission: pluginPermission(plugin, description.delete),
            },
          ]
        : []),
    ],
  }
})

// ── 新建与编辑弹窗 ──
const editorOpen = ref(false)
const editing = ref<PluginRow | null>(null)
const form = reactive<Record<string, PluginFormValue>>({})
const fieldErrors = ref<Record<string, string[]>>({})
const submitError = ref<string | null>(null)
const submitting = ref(false)
const editFormId = useId()

function openEditor(row?: PluginRow) {
  editing.value = row ?? null
  for (const key of Object.keys(form))
    delete form[key]
  Object.assign(form, initialForm(formFields.value, row))
  fieldErrors.value = {}
  submitError.value = null
  editorOpen.value = true
}

function fieldError(key: string): string | undefined {
  return fieldErrors.value[key]?.[0]
}

function textOf(key: string): string {
  const value = form[key]
  return typeof value === 'string'
    ? value
    : ''
}

function numberOf(key: string): number | null {
  const value = form[key]
  return typeof value === 'number'
    ? value
    : null
}

async function submit() {
  const current = found.value
  const description = view.value
  if (!current || !description)
    return
  submitting.value = true
  fieldErrors.value = {}
  submitError.value = null
  try {
    const row = editing.value
    if (row && description.update)
      await invokePlugin(current.plugin, description.update, formRequest(formFields.value, form, { name: rowKey.value, value: row[rowKey.value] }))
    else if (description.create)
      await invokePlugin(current.plugin, description.create, formRequest(formFields.value, form))
    editorOpen.value = false
    toast.success(t('common.messages.save_success'))
    reload()
  }
  catch (error) {
    if (error instanceof ApiError) {
      fieldErrors.value = error.errors ?? {}
      if (!error.hasFieldErrors)
        submitError.value = error.detail
    }
    else {
      submitError.value = String(error)
    }
  }
  finally {
    submitting.value = false
  }
}

async function removeRow(row: PluginRow) {
  try {
    await schema.value?.resource.remove?.(row[rowKey.value] as string)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as PluginRow | undefined
  switch (payload.key) {
    case 'create':
      openEditor()
      break
    case 'edit':
      if (row)
        openEditor(row)
      break
    case 'delete':
      if (row)
        void removeRow(row)
      break
  }
}

const modalTitle = computed(() => editing.value
  ? t('plugins.page.edit_title')
  : t('plugins.page.create_title'))
</script>

<template>
  <SchemaPage
    v-if="schema"
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <XEditModal
      v-model:show="editorOpen"
      :title="modalTitle"
      :loading="submitting"
      :form-id="editFormId"
    >
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" novalidate @submit="submit">
        <XhFieldRoot
          v-for="field in formFields"
          :key="field.key"
          :class="{ 'xh-span-2': field.type === 'textarea' }"
          :invalid="!!fieldError(field.key)"
          :required="field.required"
        >
          <XFieldLabel :help="field.helpKey ? t(field.helpKey) : null">
            {{ t(field.labelKey) }}
          </XFieldLabel>
          <XhFieldControl>
            <XhSwitch
              v-if="field.type === 'switch'"
              :checked="form[field.key] === true"
              :name="field.key"
              @update:checked="(value: boolean) => (form[field.key] = value)"
            />
            <XNumberInput
              v-else-if="field.type === 'number'"
              :value="numberOf(field.key)"
              :name="field.key"
              @update:value="(value: number | null) => (form[field.key] = value)"
            />
            <XInput
              v-else
              :value="textOf(field.key)"
              :name="field.key"
              :type="field.type === 'textarea' ? 'textarea' : 'text'"
              :rows="field.type === 'textarea' ? 4 : undefined"
              clearable
              autocomplete="off"
              @update:value="(value: string) => (form[field.key] = value)"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError(field.key)">
            {{ fieldError(field.key) }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <p v-if="submitError" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ submitError }}
        </p>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
  <XhEmptyStateRoot v-else>
    <XhEmptyStateTitle>{{ t('plugins.page.unavailable') }}</XhEmptyStateTitle>
    <XhEmptyStateDescription>{{ t('plugins.page.unavailable_hint') }}</XhEmptyStateDescription>
  </XhEmptyStateRoot>
</template>
