<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { MigrationRow } from './migration'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { booleanTone } from '@/modules/auditing/components/log-tones'
import LogDetailDrawer from '@/modules/auditing/components/LogDetailDrawer.vue'
import { useLogDetail } from '@/modules/auditing/components/use-log-detail'
import { renderTag, SchemaPage } from '~/components'
import { toast } from '~/composables'
import { errorText } from '../jobs/job'
import { databaseOptions, getMigration, migrationDetailSchema, migrationResource, PLATFORM_DATABASE, successLabel, successOptions } from './migration'

defineOptions({ name: 'MaintenanceMigrationsView' })

const { t } = useI18n()
// 记录在各自的库里：列表记下每行所在的库，详情按它取
const rowDatabases = new Map<string, string>()
const resource = migrationResource(rowDatabases)
// 详情抽屉与审计包的日志页共用：先用列表行垫上，再取详情；取不到就留着列表行并提示
const { visible: detailVisible, loading: detailLoading, record: detailRecord, open: openDetail } = useLogDetail<MigrationRow>(id => getMigration(id, rowDatabases.get(id) ?? PLATFORM_DATABASE), () => t('maintenance.migrations.detail_load_failed'))

// 库的选项取自版本台账：平台库与已就绪的租户独立库
const databases = ref<{ label: string, value: string }[]>([])
onMounted(async () => {
  try {
    databases.value = await databaseOptions(t('maintenance.versions.platform_database'))
  }
  catch (error) {
    toast.danger(errorText(error) || t('maintenance.migrations.database_load_failed'))
  }
})

const asRow = (row: unknown) => row as MigrationRow

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索（可排序的只放后端白名单：版本、脚本名称、执行时间、耗时） ──
const fields = computed<ListFieldSchema[]>(() => [
  {
    key: 'keyword',
    title: t('maintenance.migrations.keyword'),
    dataType: 'string',
    visible: false,
    searchable: true,
    searchPlaceholder: t('maintenance.migrations.keyword_placeholder'),
    order: 0,
  },
  {
    key: 'database',
    title: t('maintenance.migrations.database'),
    dataType: 'enum',
    searchable: true,
    options: databases.value,
    searchPlaceholder: t('maintenance.migrations.database_placeholder'),
    width: 140,
    order: 1,
    render: row => asRow(row).database === PLATFORM_DATABASE
      ? t('maintenance.versions.platform_database')
      : asRow(row).database,
  },
  {
    key: 'runId',
    title: t('maintenance.migrations.run_id'),
    dataType: 'string',
    advancedSearch: true,
    searchPlaceholder: t('maintenance.migrations.run_id_placeholder'),
    width: 120,
    order: 2,
    // 批次号只显示前 8 位，同一次升级的步骤一眼能对上；完整值在详情里
    render: row => asRow(row).runId?.slice(0, 8) ?? '-',
  },
  {
    key: 'version',
    title: t('maintenance.migrations.version'),
    dataType: 'string',
    searchable: true,
    sortable: true,
    searchPlaceholder: t('maintenance.migrations.version_placeholder'),
    minWidth: 120,
    order: 10,
  },
  {
    key: 'scriptName',
    title: t('maintenance.migrations.script_name'),
    dataType: 'string',
    searchable: true,
    sortable: true,
    searchPlaceholder: t('maintenance.migrations.script_name_placeholder'),
    minWidth: 200,
    order: 11,
  },
  {
    key: 'success',
    title: t('maintenance.migrations.success'),
    dataType: 'enum',
    searchable: true,
    options: successOptions(t),
    searchPlaceholder: t('maintenance.migrations.success'),
    width: 110,
    order: 12,
    render: row => renderTag(successLabel(t, asRow(row).success), booleanTone(asRow(row).success)),
  },
  {
    key: 'executedAt',
    title: t('maintenance.migrations.executed_time'),
    dataType: 'datetime',
    sortable: true,
    searchRange: true,
    advancedSearch: true,
    minWidth: 170,
    order: 13,
  },
  {
    key: 'durationMs',
    title: t('maintenance.migrations.duration'),
    dataType: 'number',
    sortable: true,
    width: 110,
    order: 14,
    render: row => `${asRow(row).durationMs}ms`,
  },
  {
    key: 'nodeName',
    title: t('maintenance.migrations.node_name'),
    dataType: 'string',
    advancedSearch: true,
    searchPlaceholder: t('maintenance.migrations.node_name_placeholder'),
    minWidth: 150,
    order: 15,
  },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'maintenance.migrations',
  pageName: t('maintenance.migrations.title'),
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    {
      key: 'view',
      title: t('common.actions.detail'),
      scope: 'row',
      icon: 'lucide:eye',
    },
  ],
}))

const detailSchema = computed(() => migrationDetailSchema(t))

function onAction(payload: SchemaActionPayload) {
  if (payload.key === 'view' && payload.row)
    void openDetail(asRow(payload.row))
}
</script>

<template>
  <SchemaPage :schema="schema" @action="onAction">
    <LogDetailDrawer
      v-model:show="detailVisible"
      :schema="detailSchema"
      :loading="detailLoading"
      :record="detailRecord"
      :title="t('maintenance.migrations.detail_title')"
    />
  </SchemaPage>
</template>
