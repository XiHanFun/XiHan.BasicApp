<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { PluginDeletePreview, PluginRow } from '../use-plugins'
import type { XDataTableColumn } from '~/components'
import {
  XhAlertContent,
  XhAlertDescription,
  XhAlertIndicator,
  XhAlertRoot,
  XhButton,
  XhDialogBody,
  XhDialogCloseTrigger,
  XhDialogContent,
  XhDialogFooter,
  XhDialogHeader,
  XhDialogRoot,
  XhDialogTitle,
  XhSpinner,
} from '@xihan-ui/vue'
import { computed, ref, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { dialogWidthStyle, Icon, renderTag, XDataTable } from '~/components'
import { plugins as pluginsApi } from '~/contracts'
import { ApiError, throwOnFailed } from '~/request'
import { referenceKindLabel } from '../use-plugins'

defineOptions({ name: 'PluginDeleteDialog' })

const props = defineProps<{
  plugin: PluginRow | null
}>()

const emit = defineEmits<{
  deleted: []
}>()

const show = defineModel<boolean>('show', { required: true })

const { t } = useI18n()

const loading = ref(false)
const deleting = ref(false)
const preview = shallowRef<PluginDeletePreview | null>(null)
const problem = ref<string | null>(null)

type ReferenceRow = PluginDeletePreview['references'][number]

function messageOf(error: unknown): string {
  return error instanceof ApiError
    ? error.detail
    : (error as Error).message || t('common.messages.operation_failed')
}

watch(show, async (value) => {
  const plugin = props.plugin
  if (!value || !plugin)
    return
  preview.value = null
  problem.value = null
  loading.value = true
  try {
    preview.value = await pluginsApi.deletePreview({ id: plugin.id })
  }
  catch (error) {
    problem.value = messageOf(error)
  }
  finally {
    loading.value = false
  }
})

async function confirm() {
  const plugin = props.plugin
  if (!plugin)
    return
  deleting.value = true
  problem.value = null
  try {
    throwOnFailed(await pluginsApi.delete({
      ids: [plugin.id],
    }))
    show.value = false
    emit('deleted')
  }
  catch (error) {
    problem.value = messageOf(error)
  }
  finally {
    deleting.value = false
  }
}

const referenceColumns = computed<XDataTableColumn<ReferenceRow>[]>(() => [
  {
    key: 'kind',
    title: t('plugins.installed.references.kind'),
    width: 140,
    render: row => renderTag(referenceKindLabel(t, row.kind), row.kind === 'PermissionRequest'
      ? 'neutral'
      : 'warning'),
  },
  {
    key: 'side',
    title: t('plugins.installed.references.side'),
    width: 130,
    render: row => row.side === 'platform'
      ? t('plugins.installed.references.side_platform')
      : row.side === '*'
        ? t('plugins.installed.references.side_every_tenant')
        : row.side,
  },
  {
    key: 'subject',
    title: t('plugins.installed.references.subject'),
    minWidth: 160,
    ellipsis: true,
  },
  {
    key: 'permission',
    title: t('plugins.installed.references.permission'),
    minWidth: 200,
    ellipsis: true,
  },
])
</script>

<template>
  <XhDialogRoot
    :open="show"
    :close-on-escape="!deleting"
    :close-on-interact-outside="!deleting"
    @update:open="(value: boolean) => (show = value)"
  >
    <XhDialogContent :style="dialogWidthStyle(720)">
      <XhDialogHeader>
        <XhDialogTitle>{{ t('plugins.installed.delete.title', { name: plugin?.name ?? '' }) }}</XhDialogTitle>
      </XhDialogHeader>
      <XhDialogCloseTrigger v-if="!deleting" />
      <XhDialogBody>
        <div class="plugin-delete">
          <div v-if="loading" class="plugin-delete__loading">
            <XhSpinner />
          </div>
          <template v-else-if="preview">
            <XhAlertRoot tone="warning">
              <XhAlertIndicator>
                <Icon icon="lucide:triangle-alert" width="16" height="16" />
              </XhAlertIndicator>
              <XhAlertContent>
                <XhAlertDescription>
                  {{ t('plugins.installed.delete.summary', { permissions: preview.permissions.length, references: preview.references.length }) }}
                </XhAlertDescription>
              </XhAlertContent>
            </XhAlertRoot>
            <ul v-if="preview.permissions.length > 0" class="plugin-delete__codes" :aria-label="t('plugins.installed.contributions.permissions')">
              <li v-for="code in preview.permissions" :key="code">
                <code>{{ code }}</code>
              </li>
            </ul>
            <XDataTable
              v-if="preview.references.length > 0"
              :columns="referenceColumns"
              :data="preview.references"
              :row-key="(row: ReferenceRow) => `${row.kind}|${row.side}|${row.subject}|${row.permission}`"
              size="sm"
              max-height="20rem"
            />
            <p v-if="preview.references.some(reference => reference.kind === 'PermissionRequest')" class="text-caption text-fg-muted">
              {{ t('plugins.installed.delete.requests_kept') }}
            </p>
          </template>
          <XhAlertRoot v-if="problem" tone="danger">
            <XhAlertIndicator>
              <Icon icon="lucide:circle-alert" width="16" height="16" />
            </XhAlertIndicator>
            <XhAlertContent>
              <XhAlertDescription>{{ problem }}</XhAlertDescription>
            </XhAlertContent>
          </XhAlertRoot>
        </div>
      </XhDialogBody>
      <XhDialogFooter>
        <XhButton variant="subtle" size="sm" :disabled="deleting" @click="show = false">
          {{ t('common.actions.cancel') }}
        </XhButton>
        <XhButton tone="danger" size="sm" :loading="deleting" :disabled="loading || !preview" @click="confirm">
          {{ t('plugins.installed.delete.confirm') }}
        </XhButton>
      </XhDialogFooter>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
.plugin-delete {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.plugin-delete__loading {
  display: flex;
  justify-content: center;
  padding: var(--xh-space-6) 0;
}

.plugin-delete__codes {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-1) var(--xh-space-3);
  margin: 0;
  padding: 0;
  list-style: none;
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-text-caption-size);
}
</style>
