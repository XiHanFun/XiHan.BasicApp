import type { PageSchema } from '~/components'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import VersionsPage from '@/modules/maintenance/views/versions/index.vue'
import { toExportFields } from '~/components/schema/selectors'
import { toCsv } from '~/components/schema/useSchemaExport'
import { i18n } from '~/locales'
import { configureRequest } from '~/request'

function pageSchema(): PageSchema {
  const wrapper = mount(VersionsPage, {
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

describe('版本管理的导出', () => {
  it('按读权限出导出按钮，能提交到导出中心；库名取原值', () => {
    const schema = pageSchema()
    expect(schema.exportPermission).toBe('maintenance.versions.read')
    expect(schema.resource.export).toMatchObject({
      businessType: 'versions.list',
      columnPaths: { databaseName: 'database' },
    })
  })

  it('库名写显示文本，结构状态写文案，失败原因与读不到台账的原因各成一列', async () => {
    const row = {
      database: 'platform',
      provider: 'postgresql',
      status: 'Upgrading',
      appVersion: '6.0.0',
      dbVersion: '6.0.0',
      targetVersion: '6.0.0',
      isUpgrading: true,
      upgradeNode: null,
      upgradeStartedAt: null,
      upgradedAt: null,
      lastError: null,
      lastFailedAt: null,
      error: null,
    }
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async () => new Response(JSON.stringify({
        items: [
          row,
          {
            ...row,
            database: 'tenant_acme',
            provider: null,
            status: 'Unreachable',
            dbVersion: null,
            targetVersion: null,
            isUpgrading: false,
            error: 'boom',
          },
        ],
        total: 2,
        page: { index: 1, size: 20 },
      }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })) as typeof fetch,
    })
    const schema = pageSchema()
    const { items } = await schema.resource.page!({
      page: 1,
      pageSize: 20,
      filters: {},
      conditionFilters: [],
      sorts: [],
    })
    const [header, platform, tenant] = toCsv(toExportFields(schema, () => true), items).replace('﻿', '').split('\r\n')
    expect(header).toBe('数据库,库类型,结构状态,数据库版本,应用的结构版本,应用版本,升级节点,最近升级时间,最近一次失败的原因,升级开始时间,最近一次失败时间,读取失败的原因')
    expect(platform).toBe('平台库,postgresql,升级中,6.0.0,6.0.0,6.0.0,-,-,-,-,-,-')
    expect(tenant).toBe('tenant_acme,-,不可达,-,-,6.0.0,-,-,-,-,-,boom')
  })
})
