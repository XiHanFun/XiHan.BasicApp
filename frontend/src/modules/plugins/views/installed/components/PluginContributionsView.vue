<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { PluginContributions } from '../use-plugins'
import type { XDataTableColumn } from '~/components'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { renderTag, XDataTable } from '~/components'

defineOptions({ name: 'PluginContributionsView' })

const props = defineProps<{
  contributions: PluginContributions
}>()

const { t } = useI18n()

type PermissionRow = PluginContributions['permissions'][number]
type PageRow = PluginContributions['pages'][number]
type Scope = PermissionRow['scope']

function scopeLabel(scope: Scope): string {
  switch (scope) {
    case 'Platform': return t('plugins.installed.scopes.platform')
    case 'Both': return t('plugins.installed.scopes.both')
    default: return t('plugins.installed.scopes.tenant')
  }
}

const permissionColumns = computed<XDataTableColumn<PermissionRow>[]>(() => [
  {
    key: 'code',
    title: t('plugins.installed.contributions.permission_code'),
    minWidth: 220,
    ellipsis: true,
  },
  {
    key: 'scope',
    title: t('plugins.installed.contributions.scope'),
    width: 110,
    render: row => renderTag(scopeLabel(row.scope)),
  },
  {
    key: 'audited',
    title: t('plugins.installed.contributions.audited'),
    width: 90,
    render: row => row.audited
      ? t('common.statuses.yes')
      : t('common.statuses.no'),
  },
])

const pageColumns = computed<XDataTableColumn<PageRow>[]>(() => [
  {
    key: 'code',
    title: t('plugins.installed.contributions.page_code'),
    minWidth: 160,
    ellipsis: true,
  },
  {
    key: 'route',
    title: t('plugins.installed.contributions.route'),
    minWidth: 200,
    ellipsis: true,
  },
  {
    key: 'permission',
    title: t('plugins.installed.contributions.page_permission'),
    minWidth: 200,
    ellipsis: true,
    render: row => row.permission ?? '-',
  },
])

const operations = computed(() => props.contributions.operations)
</script>

<template>
  <div class="plugin-contributions">
    <section :aria-label="t('plugins.installed.contributions.operations')">
      <h4 class="plugin-contributions__title">
        {{ t('plugins.installed.contributions.operations') }}
      </h4>
      <ul v-if="operations.length > 0" class="plugin-contributions__codes">
        <li v-for="operation in operations" :key="operation">
          <code>{{ operation }}</code>
        </li>
      </ul>
      <p v-else class="text-caption text-fg-muted">
        {{ t('plugins.installed.contributions.none') }}
      </p>
    </section>
    <section :aria-label="t('plugins.installed.contributions.permissions')">
      <h4 class="plugin-contributions__title">
        {{ t('plugins.installed.contributions.permissions') }}
      </h4>
      <XDataTable :columns="permissionColumns" :data="contributions.permissions" row-key="code" size="sm" :empty-text="t('plugins.installed.contributions.none')" />
    </section>
    <section :aria-label="t('plugins.installed.contributions.pages')">
      <h4 class="plugin-contributions__title">
        {{ t('plugins.installed.contributions.pages') }}
      </h4>
      <XDataTable :columns="pageColumns" :data="contributions.pages" row-key="code" size="sm" :empty-text="t('plugins.installed.contributions.none')" />
    </section>
    <section v-if="contributions.callbacks.length > 0" :aria-label="t('plugins.installed.contributions.callbacks')">
      <h4 class="plugin-contributions__title">
        {{ t('plugins.installed.contributions.callbacks') }}
      </h4>
      <ul class="plugin-contributions__codes">
        <li v-for="code in contributions.callbacks" :key="code">
          <code>{{ code }}</code>
        </li>
      </ul>
    </section>
  </div>
</template>

<style scoped>
.plugin-contributions {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.plugin-contributions__title {
  margin: 0 0 var(--xh-space-1);
  font-size: var(--xh-font-size-sm);
  font-weight: 600;
  color: var(--xh-fg-default);
}

.plugin-contributions__codes {
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
