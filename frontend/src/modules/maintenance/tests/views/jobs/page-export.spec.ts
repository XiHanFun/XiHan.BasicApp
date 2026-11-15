import type { PageSchema } from '~/components'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import JobsPage from '@/modules/maintenance/views/jobs/index.vue'
import { toExportFields } from '~/components/schema/selectors'
import { toCsv } from '~/components/schema/useSchemaExport'
import { i18n } from '~/locales'

function pageSchema(): PageSchema {
  const wrapper = mount(JobsPage, {
    global: {
      plugins: [i18n],
      stubs: { SchemaPage: true },
    },
  })
  return wrapper.findComponent({ name: 'SchemaPage' }).props('schema') as PageSchema
}

beforeEach(() => {
  setActivePinia(createPinia())
  i18n.global.locale.value = 'zh-CN'
})

describe('任务调度的导出', () => {
  it('按读权限出导出按钮，能提交到导出中心；浏览器里算的列取响应里的原值或不导', () => {
    const schema = pageSchema()
    expect(schema.exportPermission).toBe('maintenance.jobs.read')
    expect(schema.resource.export).toMatchObject({
      businessType: 'jobs.list',
      columnPaths: {
        group: null,
        runState: null,
        executedCount: null,
        kind: 'triggers.0.kind',
        lastRunAt: 'last.startedAt',
      },
    })
  })

  it('字段对齐列表的列，枚举与启停写文案', () => {
    const row = {
      title: '审计日志清理',
      name: 'auditing.retention',
      group: null,
      source: 'Code',
      kind: 'Cron',
      runState: 'Paused',
      paused: true,
      allowConcurrent: false,
      executedCount: 14,
      nextRunAt: null,
      lastRunAt: null,
      updatedAt: null,
    }
    const [header, line] = toCsv(toExportFields(pageSchema(), () => true), [row]).replace('﻿', '').split('\r\n')
    expect(header!.split(',').length).toBe(12)
    expect(line!.split(',').slice(0, 2)).toEqual(['审计日志清理', 'auditing.retention'])
    // 启停列在行里是布尔，文案来自筛选选项：匹配不上时会写成占位
    expect(line).toContain(i18n.global.t('common.statuses.disabled'))
    expect(line).toContain(i18n.global.t('maintenance.jobs.run_paused'))
  })
})
