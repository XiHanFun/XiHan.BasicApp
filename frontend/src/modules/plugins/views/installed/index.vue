<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { PluginIsolation, PluginRow } from './use-plugins'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { renderTag, SchemaPage, statusConfirmText } from '~/components'
import { toast } from '~/composables'
import PluginDeleteDialog from './components/PluginDeleteDialog.vue'
import PluginDetailDrawer from './components/PluginDetailDrawer.vue'
import PluginInstallWizard from './components/PluginInstallWizard.vue'
import { isolationLabel, PLUGIN_ISOLATIONS, pluginsResource } from './use-plugins'

defineOptions({ name: 'PluginsInstalledView' })

const { t } = useI18n()
const resource = pluginsResource()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

// SchemaPage 的行类型在框架边界放宽（Record）：回调里按本页的行类型取用
const asRow = (row: object) => row as PluginRow

const isolationOptions = computed(() => PLUGIN_ISOLATIONS.map(isolation => ({
  label: isolationLabel(t, isolation),
  value: isolation,
})))

const enabledOptions = computed(() => [
  {
    label: t('plugins.installed.status.enabled'),
    value: 'true',
  },
  {
    label: t('plugins.installed.status.disabled'),
    value: 'false',
  },
])

const fields = computed<ListFieldSchema[]>(() => [
  {
    key: 'keyword',
    title: t('plugins.installed.search'),
    dataType: 'string',
    visible: false,
    searchable: true,
    searchPlaceholder: t('plugins.installed.keyword_placeholder'),
    order: 0,
  },
  {
    key: 'name',
    title: t('plugins.installed.fields.name'),
    dataType: 'string',
    sortable: true,
    minWidth: 120,
    order: 1,
  },
  {
    key: 'manifestId',
    title: t('plugins.installed.fields.manifest_id'),
    dataType: 'string',
    sortable: true,
    minWidth: 200,
    order: 2,
  },
  {
    key: 'version',
    title: t('plugins.installed.fields.version'),
    dataType: 'string',
    sortable: true,
    width: 110,
    order: 3,
  },
  {
    key: 'publisher',
    title: t('plugins.installed.fields.publisher'),
    dataType: 'string',
    minWidth: 120,
    order: 4,
  },
  {
    key: 'isolation',
    title: t('plugins.installed.fields.isolation'),
    dataType: 'enum',
    options: isolationOptions.value,
    advancedSearch: true,
    width: 120,
    order: 5,
    render: row => renderTag(isolationLabel(t, asRow(row).isolation as PluginIsolation)),
    ellipsis: false,
  },
  {
    key: 'isEnabled',
    title: t('plugins.installed.fields.enabled'),
    dataType: 'enum',
    options: enabledOptions.value,
    searchable: true,
    width: 110,
    order: 6,
    render: row => asRow(row).isEnabled
      ? renderTag(t('plugins.installed.status.enabled'), 'success')
      : renderTag(t('plugins.installed.status.disabled')),
    ellipsis: false,
  },
  {
    key: 'lastErrorCode',
    title: t('plugins.installed.fields.last_error'),
    dataType: 'string',
    minWidth: 180,
    order: 7,
    render: row => asRow(row).lastErrorCode
      ? renderTag(asRow(row).lastErrorCode!, 'danger')
      : '-',
    ellipsis: false,
  },
  {
    key: 'createdAt',
    title: t('plugins.installed.fields.created_at'),
    dataType: 'datetime',
    sortable: true,
    width: 170,
    order: 8,
  },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'plugins.installed',
  pageName: 'plugins.installed.title',
  resourceCode: 'plugins',
  rowKey: 'id',
  statusPermission: 'plugins.plugins.update',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    {
      key: 'install',
      title: t('plugins.installed.actions.install'),
      scope: 'page',
      tone: 'brand',
      icon: 'lucide:package-plus',
      permission: 'plugins.plugins.install',
    },
    {
      key: 'view',
      title: t('common.actions.view'),
      scope: 'row',
      icon: 'lucide:eye',
    },
    {
      key: 'toggle',
      title: t('plugins.installed.actions.toggle'),
      scope: 'row',
      icon: 'lucide:power',
      confirm: true,
      confirmText: row => statusConfirmText(t, asRow(row).isEnabled, asRow(row).name),
      permission: 'plugins.plugins.update',
    },
    {
      key: 'delete',
      title: t('plugins.installed.actions.uninstall'),
      scope: 'row',
      icon: 'lucide:trash-2',
      tone: 'danger',
      permission: 'plugins.plugins.delete',
    },
  ],
}))

const wizardOpen = ref(false)
const detailOpen = ref(false)
const detailId = ref<string | null>(null)
const deleteOpen = ref(false)
const deleting = ref<PluginRow | null>(null)

function onAction(payload: SchemaActionPayload) {
  const row = payload.row
    ? asRow(payload.row)
    : undefined
  switch (payload.key) {
    case 'install':
      wizardOpen.value = true
      break
    case 'view':
      if (row) {
        detailId.value = row.id
        detailOpen.value = true
      }
      break
    case 'toggle':
      if (row)
        void toggle(row)
      break
    case 'delete':
      if (row) {
        deleting.value = row
        deleteOpen.value = true
      }
      break
  }
}

async function toggle(row: PluginRow) {
  try {
    await resource.updateStatus?.(row.id, !row.isEnabled)
    toast.success(t('common.messages.status_updated'))
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.status_failed'))
  }
  finally {
    reload()
  }
}

function onDeleted() {
  toast.success(t('common.messages.delete_success'))
  reload()
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <PluginInstallWizard v-model:show="wizardOpen" @installed="reload" />
    <PluginDetailDrawer v-model:open="detailOpen" :plugin-id="detailId" @changed="reload" />
    <PluginDeleteDialog v-model:show="deleteOpen" :plugin="deleting" @deleted="onDeleted" />
  </SchemaPage>
</template>
