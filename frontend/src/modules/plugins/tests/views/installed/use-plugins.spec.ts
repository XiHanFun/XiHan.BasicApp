import { describe, expect, it } from 'vitest'
import { approvedGrants, carriedOptional, grantLabel, signatureLabel, signatureTone, stateTone, upgradeChanges } from '@/modules/plugins/views/installed/use-plugins'
import { $t } from '~/locales'

describe('插件的授予', () => {
  it('必须项一个不少，可选项只取勾上且在可选范围里的，按序数排好', () => {
    expect(approvedGrants(['config:XiHan:Weather', 'binding:cache'], ['data-scope:all'], ['data-scope:all', 'callback:files.files.read']))
      .toEqual(['binding:cache', 'config:XiHan:Weather', 'data-scope:all'])
    expect(approvedGrants(['binding:cache'], ['data-scope:all'], [])).toEqual(['binding:cache'])
  })

  it('按前缀说明授予的含义', () => {
    expect(grantLabel($t, 'data-scope:all')).toBe($t('plugins.installed.grants.data_scope_all'))
    expect(grantLabel($t, 'callback:files.files.read')).toBe($t('plugins.installed.grants.callback', { code: 'files.files.read' }))
    expect(grantLabel($t, 'config:XiHan:Weather')).toBe($t('plugins.installed.grants.config', { section: 'XiHan:Weather' }))
    expect(grantLabel($t, 'kernel.plugins')).toBe($t('plugins.installed.grants.other'))
  })

  it('运行状态的色调：就绪绿、降级与排空黄、失败红', () => {
    expect([
      stateTone('Ready'),
      stateTone('Degraded'),
      stateTone('Draining'),
      stateTone('Failed'),
      stateTone(null),
    ]).toEqual([
      'success',
      'warning',
      'warning',
      'danger',
      'neutral',
    ])
  })
})

describe('插件的升级与换版本', () => {
  it('升级对照按操作、权限码、页面依次摊开，各自先新增后去掉', () => {
    const rows = upgradeChanges({
      pluginId: 'p1',
      fromVersion: '1.0.0',
      addedOperations: ['weather-radar.list'],
      removedOperations: [],
      addedPermissions: ['weather.weather-radar.read'],
      removedPermissions: ['weather.weather.export'],
      addedPages: [],
      removedPages: ['weather.forecast'],
      grants: [],
    })
    expect(rows.map(row => [row.kind, row.added, row.item])).toEqual([
      ['operation', true, 'weather-radar.list'],
      ['permission', true, 'weather.weather-radar.read'],
      ['permission', false, 'weather.weather.export'],
      ['page', false, 'weather.forecast'],
    ])
    expect(new Set(rows.map(row => row.key)).size).toBe(rows.length)
  })

  it('换版本时只预选原来批准过、换上的版本也认的可选授予', () => {
    expect(carriedOptional(['data-scope:all'], ['binding:cache', 'data-scope:all'])).toEqual(['data-scope:all'])
    expect(carriedOptional(['data-scope:all'], ['binding:cache'])).toEqual([])
  })
})

describe('插件包的签名', () => {
  it('受信任绿、不受信任黄、没有签名灰，文案各自一条', () => {
    expect([
      signatureTone('Trusted'),
      signatureTone('Untrusted'),
      signatureTone('None'),
    ]).toEqual([
      'success',
      'warning',
      'neutral',
    ])
    expect(signatureLabel($t, 'Untrusted')).toBe($t('plugins.installed.signatures.untrusted'))
    expect(signatureLabel($t, 'None')).toBe($t('plugins.installed.signatures.none'))
  })
})
