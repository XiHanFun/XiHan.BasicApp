<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { PluginDetail, PluginStatus } from '../use-plugins'
import type { XDataTableColumn } from '~/components'
import {
  XhButton,
  XhCheckboxGroupIndicator,
  XhCheckboxGroupItem,
  XhCheckboxGroupItemText,
  XhCheckboxGroupRoot,
  XhDescriptionsItem,
  XhDescriptionsLabel,
  XhDescriptionsRoot,
  XhDescriptionsValue,
  XhDrawerCloseTrigger,
  XhDrawerContent,
  XhDrawerRoot,
  XhDrawerTitle,
  XhFieldControl,
  XhFieldLabel,
  XhFieldRoot,
  XhSpinner,
  XhTabsContent,
  XhTabsIndicator,
  XhTabsList,
  XhTabsRoot,
  XhTabsTrigger,
  XhTagLabel,
  XhTagRoot,
} from '@xihan-ui/vue'
import { computed, h, ref, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { renderRowActionMenu, renderTag, XDataTable } from '~/components'
import { toast } from '~/composables'
import { plugins as pluginsApi } from '~/contracts'
import { ApiError } from '~/request'
import { useAuthStore } from '~/stores'
import { formatDate, formatFileSize } from '~/utils'
import { approvedGrants, grantLabel, isolationLabel, signatureLabel, signatureTone, stateLabel, stateTone } from '../use-plugins'
import PluginContributionsView from './PluginContributionsView.vue'
import PluginRollbackDialog from './PluginRollbackDialog.vue'

defineOptions({ name: 'PluginDetailDrawer' })

const props = defineProps<{
  pluginId: string | null
}>()

const emit = defineEmits<{
  changed: []
}>()

const open = defineModel<boolean>('open', { required: true })

const { t } = useI18n()
const auth = useAuthStore()
const canGrant = computed(() => auth.can('plugins.plugins.grant'))
const canSwitch = computed(() => auth.can('plugins.plugins.install'))

const loading = ref(false)
const detail = shallowRef<PluginDetail | null>(null)
const status = shallowRef<PluginStatus | null>(null)
const chosenOptional = ref<string[]>([])
const saving = ref(false)

type VersionRow = PluginDetail['versions'][number]

const switchOpen = ref(false)
const switching = shallowRef<VersionRow | null>(null)

function openSwitch(row: VersionRow) {
  switching.value = row
  switchOpen.value = true
}

async function onSwitched() {
  emit('changed')
  if (detail.value)
    await load(detail.value.plugin.id)
}

async function load(id: string) {
  loading.value = true
  try {
    const [loaded, runtime] = await Promise.all([
      pluginsApi.get({ id }),
      pluginsApi.status({ id }),
    ])
    detail.value = loaded
    status.value = runtime
    chosenOptional.value = loaded.grants.filter(grant => loaded.optionalGrants.includes(grant))
  }
  catch (error) {
    detail.value = null
    toast.danger((error as Error).message || t('common.messages.load_failed'))
  }
  finally {
    loading.value = false
  }
}

watch([
  open,
  () => props.pluginId,
], ([isOpen, id]) => {
  if (isOpen && id)
    void load(id)
})

const grantsChanged = computed(() => {
  const current = detail.value
  if (!current)
    return false
  const next = approvedGrants(current.requiredGrants, current.optionalGrants, chosenOptional.value)
  return next.join('\n') !== [...current.grants].sort().join('\n')
})

async function saveGrants() {
  const current = detail.value
  if (!current)
    return
  saving.value = true
  try {
    await pluginsApi.grant({
      id: current.plugin.id,
      concurrencyStamp: current.plugin.concurrencyStamp,
      grants: approvedGrants(current.requiredGrants, current.optionalGrants, chosenOptional.value),
    })
    toast.success(t('common.messages.save_success'))
    emit('changed')
    await load(current.plugin.id)
  }
  catch (error) {
    toast.danger(error instanceof ApiError
      ? error.detail
      : (error as Error).message || t('common.messages.save_failed'))
  }
  finally {
    saving.value = false
  }
}

const versionColumns = computed<XDataTableColumn<VersionRow>[]>(() => [
  {
    key: 'version',
    title: t('plugins.installed.fields.version'),
    width: 140,
    render: row => h('span', { class: 'plugin-version' }, [
      row.version,
      row.isCurrent
        ? renderTag(t('plugins.installed.versions.current'), 'success')
        : null,
    ]),
  },
  {
    key: 'signature',
    title: t('plugins.installed.fields.signature'),
    width: 150,
    render: row => renderTag(signatureLabel(t, row.signature.status), signatureTone(row.signature.status)),
  },
  {
    key: 'size',
    title: t('plugins.installed.fields.size'),
    width: 100,
    render: row => formatFileSize(row.size),
  },
  {
    key: 'sha256',
    title: t('plugins.installed.fields.sha256'),
    minWidth: 120,
    ellipsis: true,
  },
  {
    key: 'createdAt',
    title: t('plugins.installed.versions.uploaded_at'),
    width: 160,
    render: row => formatDate(row.createdAt),
  },
  ...(canSwitch.value
    ? [
        {
          key: 'actions',
          title: t('component.schema_page.actions_column'),
          width: 90,
          fixed: 'right' as const,
          render: (row: VersionRow) => renderRowActionMenu([
            {
              key: 'switch',
              title: t('plugins.installed.versions.switch'),
              icon: 'lucide:history',
              disabled: row.isCurrent,
            },
          ], t('component.schema_page.more'), () => openSwitch(row)),
        },
      ]
    : []),
])
</script>

<template>
  <XhDrawerRoot v-model:open="open" side="right">
    <XhDrawerContent class="w-4xl">
      <XhDrawerTitle>{{ detail ? detail.plugin.name : t('plugins.installed.detail_title') }}</XhDrawerTitle>
      <XhDrawerCloseTrigger />
      <div class="xh-loading-stage" :class="{ 'is-loading': loading }">
        <div class="xh-loading-stage__veil">
          <XhSpinner />
        </div>
        <div v-if="detail" class="xh-scroll-area app-fade max-h-[calc(100vh-(--spacing(34)))]">
          <XhTabsRoot default-value="overview" variant="line">
            <XhTabsList>
              <XhTabsTrigger value="overview">
                {{ t('plugins.installed.tabs.overview') }}
              </XhTabsTrigger>
              <XhTabsTrigger value="contributions">
                {{ t('plugins.installed.tabs.contributions') }}
              </XhTabsTrigger>
              <XhTabsTrigger value="grants">
                {{ t('plugins.installed.tabs.grants') }}
              </XhTabsTrigger>
              <XhTabsTrigger value="versions">
                {{ t('plugins.installed.tabs.versions', { count: detail.versions.length }) }}
              </XhTabsTrigger>
              <XhTabsIndicator />
            </XhTabsList>

            <XhTabsContent value="overview">
              <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('plugins.installed.fields.manifest_id') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ detail.plugin.manifestId }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('plugins.installed.fields.version') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ detail.plugin.version }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('plugins.installed.fields.publisher') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ detail.plugin.publisher || '-' }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('plugins.installed.fields.isolation') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ isolationLabel(t, detail.plugin.isolation) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('plugins.installed.fields.enabled') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    <XhTagRoot variant="subtle" size="sm" :tone="detail.plugin.isEnabled ? 'success' : 'neutral'">
                      <XhTagLabel>{{ detail.plugin.isEnabled ? t('plugins.installed.status.enabled') : t('plugins.installed.status.disabled') }}</XhTagLabel>
                    </XhTagRoot>
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('plugins.installed.fields.state') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    <XhTagRoot variant="subtle" size="sm" :tone="stateTone(status?.isLoaded ? status.state : null)">
                      <XhTagLabel>{{ stateLabel(t, status?.isLoaded ? status.state : null) }}</XhTagLabel>
                    </XhTagRoot>
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('plugins.installed.fields.loaded_version') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ status?.loadedVersion || '-' }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('plugins.installed.fields.last_error') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    <code v-if="status?.errorCode || detail.plugin.lastErrorCode">{{ status?.errorCode || detail.plugin.lastErrorCode }}</code>
                    <template v-else>
                      -
                    </template>
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('plugins.installed.fields.created_at') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatDate(detail.plugin.createdAt) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('plugins.installed.fields.modified_at') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ detail.plugin.modifiedAt ? formatDate(detail.plugin.modifiedAt) : '-' }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem style="grid-column: span 2">
                  <XhDescriptionsLabel>{{ t('plugins.installed.fields.remark') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ detail.plugin.remark || '-' }}</XhDescriptionsValue>
                </XhDescriptionsItem>
              </XhDescriptionsRoot>
            </XhTabsContent>

            <XhTabsContent value="contributions">
              <PluginContributionsView :contributions="detail.contributions" />
            </XhTabsContent>

            <XhTabsContent value="grants">
              <div class="plugin-grants">
                <p class="text-caption text-fg-muted">
                  {{ detail.requiredGrants.length > 0 ? t('plugins.installed.wizard.required_grants') : t('plugins.installed.wizard.no_required_grants') }}
                </p>
                <ul v-if="detail.requiredGrants.length > 0" class="plugin-grants__list">
                  <li v-for="grant in detail.requiredGrants" :key="grant">
                    <code>{{ grant }}</code>
                    <span class="text-caption text-fg-muted">{{ grantLabel(t, grant) }}</span>
                  </li>
                </ul>
                <XhFieldRoot v-if="detail.optionalGrants.length > 0">
                  <XhFieldLabel>{{ t('plugins.installed.wizard.optional_grants') }}</XhFieldLabel>
                  <XhFieldControl>
                    <XhCheckboxGroupRoot name="optionalGrants" :value="chosenOptional" :disabled="!canGrant" @update:value="(value: string[]) => (chosenOptional = value)">
                      <XhCheckboxGroupItem v-for="grant in detail.optionalGrants" :key="grant" :value="grant">
                        <XhCheckboxGroupIndicator />
                        <XhCheckboxGroupItemText>{{ grant }}（{{ grantLabel(t, grant) }}）</XhCheckboxGroupItemText>
                      </XhCheckboxGroupItem>
                    </XhCheckboxGroupRoot>
                  </XhFieldControl>
                </XhFieldRoot>
                <div v-if="canGrant && detail.optionalGrants.length > 0">
                  <XhButton tone="brand" size="sm" :loading="saving" :disabled="!grantsChanged" @click="saveGrants">
                    {{ t('plugins.installed.grants.save') }}
                  </XhButton>
                </div>
              </div>
            </XhTabsContent>

            <XhTabsContent value="versions">
              <XDataTable :columns="versionColumns" :data="detail.versions" row-key="id" size="sm" />
              <PluginRollbackDialog v-model:show="switchOpen" :detail="detail" :version="switching" @switched="onSwitched" />
            </XhTabsContent>
          </XhTabsRoot>
        </div>
      </div>
    </XhDrawerContent>
  </XhDrawerRoot>
</template>

<style scoped>
/* 版本列是渲染函数节点，不带本组件的 scope */
:deep(.plugin-version) {
  display: inline-flex;
  gap: var(--xh-space-2);
  align-items: center;
}

.plugin-grants {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
}

.plugin-grants__list {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1);
  margin: 0;
  padding: 0;
  list-style: none;
}

.plugin-grants__list li {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-2);
  align-items: baseline;
}

.plugin-grants__list code {
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-text-caption-size);
  word-break: break-all;
}
</style>
