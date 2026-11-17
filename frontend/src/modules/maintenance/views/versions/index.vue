<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { VersionPlan, VersionRow } from './version'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import { XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, renderTag, SchemaPage } from '~/components'
import { dialog, toast } from '~/composables'
import { formatDate } from '~/utils'
import { errorText } from '../jobs/job'
import { canReleaseLock, canUpgrade, databaseLabel, getVersion, planVersion, releaseVersionLock, statusLabel, statusOptions, statusTone, upgradeVersion, versionResource } from './version'

defineOptions({ name: 'MaintenanceVersionsView' })

const { t } = useI18n()
const resource = versionResource(t)
const schemaPageRef = ref<SchemaPageInstance | null>(null)

/** 行对象在 SchemaPage 边界为宽松类型，这里收敛回台账行 */
const asRow = (row: unknown) => row as VersionRow

function reload() {
  void schemaPageRef.value?.reload()
}

// ── 字段单一事实源：列 + 常用搜索（可排序的只放后端白名单：应用版本、数据库版本、状态、最近升级时间） ──
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  {
    key: 'keyword',
    title: t('maintenance.versions.keyword'),
    dataType: 'string',
    visible: false,
    searchable: true,
    searchPlaceholder: t('maintenance.versions.keyword_placeholder'),
    width: 240,
    order: 0,
  },
  // 列 + 搜索
  {
    key: 'databaseName',
    title: t('maintenance.versions.database'),
    dataType: 'string',
    exportable: true,
    minWidth: 120,
    order: 1,
  },
  {
    key: 'provider',
    title: t('maintenance.versions.provider'),
    dataType: 'string',
    exportable: true,
    width: 110,
    order: 2,
  },
  {
    key: 'status',
    title: t('maintenance.versions.status'),
    dataType: 'enum',
    searchable: true,
    sortable: true,
    exportable: true,
    options: statusOptions(t),
    searchPlaceholder: t('maintenance.versions.status_placeholder'),
    width: 110,
    order: 3,
    render: row => renderTag(statusLabel(t, asRow(row).status), statusTone(asRow(row).status)),
  },
  {
    key: 'dbVersion',
    title: t('maintenance.versions.db_version'),
    dataType: 'string',
    searchable: true,
    sortable: true,
    exportable: true,
    searchPlaceholder: t('maintenance.versions.db_version_placeholder'),
    minWidth: 120,
    order: 4,
  },
  {
    key: 'targetVersion',
    title: t('maintenance.versions.target_version'),
    dataType: 'string',
    exportable: true,
    minWidth: 120,
    order: 5,
  },
  {
    key: 'appVersion',
    title: t('maintenance.versions.app_version'),
    dataType: 'string',
    searchable: true,
    sortable: true,
    exportable: true,
    searchPlaceholder: t('maintenance.versions.app_version_placeholder'),
    minWidth: 130,
    order: 6,
  },
  {
    key: 'upgradeNode',
    title: t('maintenance.versions.upgrade_node'),
    dataType: 'string',
    exportable: true,
    minWidth: 140,
    order: 7,
  },
  {
    key: 'upgradedAt',
    title: t('maintenance.versions.upgraded_time'),
    dataType: 'datetime',
    sortable: true,
    exportable: true,
    minWidth: 170,
    order: 8,
  },
  {
    key: 'lastError',
    title: t('maintenance.versions.last_error'),
    dataType: 'string',
    exportable: true,
    minWidth: 220,
    ellipsis: true,
    order: 9,
  },
  {
    key: 'upgradeStartedAt',
    title: t('maintenance.versions.upgrade_start_time'),
    dataType: 'datetime',
    visible: false,
    exportable: true,
    order: 10,
  },
  {
    key: 'lastFailedAt',
    title: t('maintenance.versions.last_failed_time'),
    dataType: 'datetime',
    visible: false,
    exportable: true,
    order: 11,
  },
  // 读不到台账的行状态为「不可达」：原因单独成一列，导出时一起带上
  {
    key: 'error',
    title: t('maintenance.versions.read_error'),
    dataType: 'string',
    visible: false,
    exportable: true,
    order: 12,
  },
])

// 台账由升级执行器写入：页面能预览变更、重试租户独立库的升级、释放已退出的节点留下的锁
const schema = computed<PageSchema>(() => ({
  pageCode: 'maintenance.versions',
  pageName: t('maintenance.versions.title'),
  exportPermission: 'maintenance.versions.read',
  rowKey: 'database',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    {
      key: 'view',
      title: t('maintenance.versions.view'),
      scope: 'row',
      icon: 'lucide:eye',
    },
    {
      key: 'plan',
      title: t('maintenance.versions.plan'),
      scope: 'row',
      icon: 'lucide:list-checks',
      visible: row => asRow(row).status !== 'Unreachable',
    },
    {
      key: 'upgrade',
      title: t('maintenance.versions.upgrade'),
      scope: 'row',
      icon: 'lucide:refresh-cw',
      visible: row => canUpgrade(asRow(row)),
      permission: 'maintenance.versions.upgrade',
    },
    {
      key: 'releaseLock',
      title: t('maintenance.versions.release_lock'),
      scope: 'row',
      tone: 'danger',
      icon: 'lucide:lock-open',
      visible: row => canReleaseLock(asRow(row)),
      permission: 'maintenance.versions.release-lock',
    },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row
    ? asRow(payload.row)
    : undefined
  if (!row) {
    return
  }

  switch (payload.key) {
    case 'view':
      void handleDetail(row)
      break
    case 'plan':
      void handlePlan(row)
      break
    case 'upgrade':
      handleUpgrade(row)
      break
    case 'releaseLock':
      handleReleaseLock(row)
      break
  }
}

// ── 详情抽屉（版本信息） ─────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<VersionRow | null>(null)

async function handleDetail(row: VersionRow) {
  detailVisible.value = true
  detailLoading.value = true
  detailData.value = null
  try {
    detailData.value = await getVersion(row.database)
  }
  catch (error) {
    toast.danger(errorText(error) || t('maintenance.versions.load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

// ── 升级预览抽屉 ─────────────────────────────────────────────
const planVisible = ref(false)
const planLoading = ref(false)
const planData = ref<VersionPlan | null>(null)
const planDatabase = ref('')
const planIsEmpty = computed(() => !!planData.value && planData.value.scripts.length === 0 && planData.value.tables.length === 0 && planData.value.indexes.length === 0)

async function handlePlan(row: VersionRow) {
  planVisible.value = true
  planLoading.value = true
  planData.value = null
  planDatabase.value = databaseLabel(t, row.database)
  try {
    planData.value = await planVersion(row.database)
  }
  catch (error) {
    toast.danger(errorText(error) || t('maintenance.versions.plan_load_failed'))
  }
  finally {
    planLoading.value = false
  }
}

// ── 重试升级与释放锁：确认放在处理函数里，行内动作与以后别处的入口都覆盖 ──
function handleUpgrade(row: VersionRow) {
  void dialog.confirm({
    badge: 'warning',
    title: t('maintenance.versions.upgrade'),
    content: t('maintenance.versions.upgrade_confirm', { database: row.database }),
    okText: t('common.actions.confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        const result = await upgradeVersion(row.database)
        toast.success(t('maintenance.versions.upgraded', { count: result.applied.length, version: result.to ?? '-' }))
      }
      catch (error) {
        toast.danger(errorText(error) || t('maintenance.versions.upgrade_failed'))
      }
      finally {
        reload()
      }
    },
  })
}

function handleReleaseLock(row: VersionRow) {
  const node = row.upgradeNode
  if (!node) {
    return
  }

  void dialog.confirm({
    tone: 'danger',
    badge: 'warning',
    title: t('maintenance.versions.release_lock'),
    content: t('maintenance.versions.release_lock_confirm', {
      database: databaseLabel(t, row.database),
      node,
    }),
    okText: t('common.actions.confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await releaseVersionLock(row.database, node)
        toast.success(t('maintenance.versions.lock_released'))
      }
      catch (error) {
        toast.danger(errorText(error) || t('maintenance.versions.release_lock_failed'))
      }
      finally {
        reload()
      }
    },
  })
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <template #toolbar>
      <span class="xh-version-hint">
        <Icon width="14" height="14" icon="lucide:info" />
        {{ t('maintenance.versions.engine_managed_hint') }}
      </span>
    </template>

    <!-- 详情抽屉：版本信息 -->
    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 720px">
        <XhDrawerTitle>{{ t('maintenance.versions.detail_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div class="xh-loading-stage" :class="{ 'is-loading': detailLoading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <XhEmptyStateRoot v-if="!detailLoading && !detailData" class="xh-detail-empty">
            <XhEmptyStateIndicator>
              <Icon icon="lucide:inbox" width="28" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('maintenance.versions.detail_empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
          <div v-else-if="detailData" class="xh-scroll-area" style="max-height: calc(100vh - 120px)">
            <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.database') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ databaseLabel(t, detailData.database) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.provider') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.provider || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.status') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <XhTagRoot variant="subtle" :tone="statusTone(detailData.status)" size="sm">
                    <XhTagLabel>
                      {{ statusLabel(t, detailData.status) }}
                    </XhTagLabel>
                  </XhTagRoot>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.app_version') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.appVersion || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.db_version') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.dbVersion || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.target_version') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.targetVersion || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.upgrade_node') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.upgradeNode || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.upgrade_start_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatDate(detailData.upgradeStartedAt) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.upgraded_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatDate(detailData.upgradedAt) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.last_failed_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatDate(detailData.lastFailedAt) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem v-if="detailData.lastError" :span="2">
                <XhDescriptionsLabel>{{ t('maintenance.versions.last_error') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <pre class="xh-version-pre is-error">{{ detailData.lastError }}</pre>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem v-if="detailData.error" :span="2">
                <XhDescriptionsLabel>{{ t('maintenance.versions.read_error') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <pre class="xh-version-pre is-error">{{ detailData.error }}</pre>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
            </XhDescriptionsRoot>
          </div>
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 升级预览抽屉：只读，列出要执行的脚本、要建的表与列、要建的索引 -->
    <XhDrawerRoot v-model:open="planVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 860px">
        <XhDrawerTitle>{{ t('maintenance.versions.plan_title', { database: planDatabase }) }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div class="xh-loading-stage" :class="{ 'is-loading': planLoading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <div v-if="planData" class="xh-scroll-area xh-version-plan" style="max-height: calc(100vh - 120px)">
            <XhDescriptionsRoot :columns="3" variant="outline" size="sm">
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.provider') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ planData.provider }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.plan_versions') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ planData.from || '-' }} → {{ planData.to }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.plan_sync') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ planData.syncDue ? t('maintenance.versions.plan_sync_due') : t('maintenance.versions.plan_sync_skipped') }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
            </XhDescriptionsRoot>

            <XhEmptyStateRoot v-if="planIsEmpty" class="xh-detail-empty">
              <XhEmptyStateIndicator>
                <Icon icon="lucide:circle-check" width="28" />
              </XhEmptyStateIndicator>
              <XhEmptyStateTitle>{{ t('maintenance.versions.plan_empty') }}</XhEmptyStateTitle>
            </XhEmptyStateRoot>

            <section v-if="planData.scripts.length > 0" class="xh-version-plan__section">
              <h4 class="xh-version-plan__heading">
                {{ t('maintenance.versions.plan_scripts', { count: planData.scripts.length }) }}
              </h4>
              <div v-for="script in planData.scripts" :key="`${script.version}/${script.script}`" class="xh-version-plan__item">
                <div class="xh-version-plan__name">
                  {{ script.version }} / {{ script.script }}
                </div>
                <pre class="xh-version-pre">{{ script.sql }}</pre>
              </div>
            </section>

            <section v-if="planData.tables.length > 0" class="xh-version-plan__section">
              <h4 class="xh-version-plan__heading">
                {{ t('maintenance.versions.plan_tables', { count: planData.tables.length }) }}
              </h4>
              <div v-for="table in planData.tables" :key="table.table" class="xh-version-plan__item">
                <div class="xh-version-plan__name">
                  {{ table.table }}
                  <XhTagRoot variant="subtle" :tone="table.isNew ? 'success' : 'info'" size="sm">
                    <XhTagLabel>
                      {{ table.isNew ? t('maintenance.versions.plan_new_table') : t('maintenance.versions.plan_add_columns') }}
                    </XhTagLabel>
                  </XhTagRoot>
                </div>
                <ul class="xh-version-plan__columns">
                  <li v-for="column in table.columns" :key="column.column">
                    <code>{{ column.column }}</code>
                    <span class="xh-version-plan__type">{{ column.type }}</span>
                    <span v-if="!column.isNullable" class="xh-version-plan__type">NOT NULL</span>
                  </li>
                </ul>
              </div>
            </section>

            <section v-if="planData.indexes.length > 0" class="xh-version-plan__section">
              <h4 class="xh-version-plan__heading">
                {{ t('maintenance.versions.plan_indexes', { count: planData.indexes.length }) }}
              </h4>
              <ul class="xh-version-plan__columns">
                <li v-for="index in planData.indexes" :key="index.index">
                  <code>{{ index.index }}</code>
                  <span class="xh-version-plan__type">{{ index.table }}</span>
                </li>
              </ul>
            </section>
          </div>
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-empty {
  padding-block: var(--xh-space-8);
}

.xh-version-hint {
  display: inline-flex;
  align-items: center;
  gap: var(--xh-space-1);
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
}

.xh-version-pre {
  margin: 0;
  padding: var(--xh-space-2) var(--xh-space-3);
  border-radius: var(--xh-shape-inset);
  background: var(--xh-bg-subtle);
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-font-size-xs);
  line-height: var(--xh-leading-relaxed);
  white-space: pre-wrap;
  word-break: break-all;
}

.xh-version-pre.is-error {
  color: var(--xh-fg-danger);
}

.xh-version-plan {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-4);
}

.xh-version-plan__section {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
}

.xh-version-plan__heading {
  margin: 0;
  font-size: var(--xh-font-size-sm);
  font-weight: var(--xh-font-weight-semibold);
}

.xh-version-plan__item {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1);
}

.xh-version-plan__name {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-font-size-xs);
}

.xh-version-plan__columns {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1);
  margin: 0;
  padding-inline-start: var(--xh-space-4);
  font-size: var(--xh-font-size-xs);
}

.xh-version-plan__type {
  margin-inline-start: var(--xh-space-2);
  color: var(--xh-fg-muted);
}
</style>
