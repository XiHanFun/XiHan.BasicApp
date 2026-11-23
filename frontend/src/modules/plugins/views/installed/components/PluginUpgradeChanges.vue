<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { PluginUpgrade } from '../use-plugins'
import type { XDataTableColumn } from '~/components'
import {
  XhAlertContent,
  XhAlertDescription,
  XhAlertIndicator,
  XhAlertRoot,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, renderTag, XDataTable } from '~/components'
import { upgradeChanges } from '../use-plugins'

defineOptions({ name: 'PluginUpgradeChanges' })

const props = defineProps<{
  upgrade: PluginUpgrade
}>()

const { t } = useI18n()

type ChangeRow = ReturnType<typeof upgradeChanges>[number]

const rows = computed(() => upgradeChanges(props.upgrade))
const hasRemovals = computed(() => props.upgrade.removedPermissions.length > 0 || props.upgrade.removedPages.length > 0)

function kindLabel(kind: ChangeRow['kind']): string {
  switch (kind) {
    case 'operation': return t('plugins.installed.contributions.operations')
    case 'permission': return t('plugins.installed.contributions.permissions')
    default: return t('plugins.installed.contributions.pages')
  }
}

const columns = computed<XDataTableColumn<ChangeRow>[]>(() => [
  {
    key: 'kind',
    title: t('plugins.installed.changes.kind'),
    width: 120,
    render: row => kindLabel(row.kind),
  },
  {
    key: 'added',
    title: t('plugins.installed.changes.change'),
    width: 100,
    render: row => row.added
      ? renderTag(t('plugins.installed.changes.added'), 'success')
      : renderTag(t('plugins.installed.changes.removed'), 'warning'),
  },
  {
    key: 'item',
    title: t('plugins.installed.changes.item'),
    minWidth: 220,
    ellipsis: true,
  },
])
</script>

<template>
  <div class="plugin-upgrade-changes">
    <p v-if="rows.length === 0" class="text-caption text-fg-muted">
      {{ t('plugins.installed.changes.none') }}
    </p>
    <XDataTable v-else :columns="columns" :data="rows" row-key="key" size="sm" />
    <XhAlertRoot v-if="hasRemovals" tone="warning">
      <XhAlertIndicator>
        <Icon icon="lucide:triangle-alert" width="16" height="16" />
      </XhAlertIndicator>
      <XhAlertContent>
        <XhAlertDescription>{{ t('plugins.installed.changes.removal_note') }}</XhAlertDescription>
      </XhAlertContent>
    </XhAlertRoot>
  </div>
</template>

<style scoped>
.plugin-upgrade-changes {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
}
</style>
