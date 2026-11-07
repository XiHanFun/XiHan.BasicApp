import type { PageSchema } from '~/components'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { toExportFields } from '~/components/schema/selectors'
import { toCsv } from '~/components/schema/useSchemaExport'
import { i18n } from '~/locales'
import { configureRequest } from '~/request'
import VersionsPage from './index.vue'

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
  it('按读权限出导出按钮，是本地 CSV，不提交到导出中心', () => {
    const schema = pageSchema()
    expect(schema.exportPermission).toBe('maintenance.versions.read')
    expect(schema.resource.export).toBeUndefined()
  })

  it('库名写显示文本，升级状态写文案，读不到台账的原因单独成列', async () => {
    const row = {
      database: 'platform',
      appVersion: '6.0.0',
      dbVersion: '6.0.0',
      isUpgrading: true,
      upgradeNode: null,
      upgradeStartedAt: null,
      upgradedAt: null,
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
    expect(header).toBe('数据库,应用版本,数据库版本,升级状态,升级节点,升级开始时间,最近升级时间,读取失败的原因')
    expect(platform).toBe('平台库,6.0.0,6.0.0,升级中,-,-,-,-')
    expect(tenant).toBe('tenant_acme,6.0.0,6.0.0,正常,-,-,-,boom')
  })
})
