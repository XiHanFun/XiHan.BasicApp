/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { Tone } from '@xihan-ui/core'
import type { SchemaResource } from '~/components'
import type { PageQueryMapping, ResponseOf } from '~/contracts'
import { plugins as pluginsApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { throwOnFailed } from '~/request'

export type PluginRow = ResponseOf<'plugins.list'>['items'][number]
export type PluginIsolation = PluginRow['isolation']
export type PluginDetail = ResponseOf<'plugins.get'>
export type PluginContributions = PluginDetail['contributions']
export type PluginStatus = ResponseOf<'plugins.status'>
export type PluginLifecycleState = NonNullable<PluginStatus['state']>
export type PluginInspection = ResponseOf<'plugins.inspect'>
export type PluginSignatureStatus = PluginInspection['signature']['status']
export type PluginUpgrade = NonNullable<PluginInspection['upgrade']>
export type PluginVersion = PluginDetail['versions'][number]
export type PluginDeletePreview = ResponseOf<'plugins.delete-preview'>
export type PermissionReferenceKind = PluginDeletePreview['references'][number]['kind']

type Translate = (key: string, named?: Record<string, unknown>) => string

/** 列表的筛选登记：与后端 PluginQueries 的白名单一致 */
const PLUGIN_QUERY: PageQueryMapping = {
  filters: {
    isEnabled: 'eq',
    isolation: 'eq',
  },
}

export const PLUGIN_ISOLATIONS: readonly PluginIsolation[] = ['OutOfProcess', 'Wasm', 'InProcess']

/** 插件列表：分页、启停与卸载；启停与卸载是批量操作，逐条失败时抛出原因 */
export function pluginsResource(): SchemaResource<PluginRow> {
  return {
    page: async params => toSchemaPageResult(await pluginsApi.list(toPageRequest(params, PLUGIN_QUERY)), params),
    updateStatus: async (id, enabled) => {
      const ids = [
        String(id),
      ]
      throwOnFailed(enabled
        ? await pluginsApi.enable({ ids })
        : await pluginsApi.disable({ ids }))
    },
    remove: async id => throwOnFailed(await pluginsApi.delete({
      ids: [
        String(id),
      ],
    })),
  }
}

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对
export function isolationLabel(t: Translate, isolation: PluginIsolation): string {
  switch (isolation) {
    case 'Wasm': return t('plugins.installed.isolations.wasm')
    case 'InProcess': return t('plugins.installed.isolations.in_process')
    default: return t('plugins.installed.isolations.out_of_process')
  }
}

export function stateLabel(t: Translate, state: PluginLifecycleState | null | undefined): string {
  switch (state) {
    case 'Declared': return t('plugins.installed.states.declared')
    case 'Resolved': return t('plugins.installed.states.resolved')
    case 'Configured': return t('plugins.installed.states.configured')
    case 'Started': return t('plugins.installed.states.started')
    case 'Ready': return t('plugins.installed.states.ready')
    case 'Degraded': return t('plugins.installed.states.degraded')
    case 'Draining': return t('plugins.installed.states.draining')
    case 'Stopped': return t('plugins.installed.states.stopped')
    case 'Failed': return t('plugins.installed.states.failed')
    default: return t('plugins.installed.states.not_loaded')
  }
}

export function stateTone(state: PluginLifecycleState | null | undefined): Tone {
  switch (state) {
    case 'Ready': return 'success'
    case 'Degraded':
    case 'Draining': return 'warning'
    case 'Failed': return 'danger'
    default: return 'neutral'
  }
}

export function signatureLabel(t: Translate, status: PluginSignatureStatus): string {
  switch (status) {
    case 'Trusted': return t('plugins.installed.signatures.trusted')
    case 'Untrusted': return t('plugins.installed.signatures.untrusted')
    default: return t('plugins.installed.signatures.none')
  }
}

export function signatureTone(status: PluginSignatureStatus): Tone {
  switch (status) {
    case 'Trusted': return 'success'
    case 'Untrusted': return 'warning'
    default: return 'neutral'
  }
}

export function referenceKindLabel(t: Translate, kind: PermissionReferenceKind): string {
  switch (kind) {
    case 'RoleGrant': return t('plugins.installed.references.role_grant')
    case 'UserGrant': return t('plugins.installed.references.user_grant')
    case 'PermissionRule': return t('plugins.installed.references.permission_rule')
    case 'PermissionCondition': return t('plugins.installed.references.permission_condition')
    case 'Delegation': return t('plugins.installed.references.delegation')
    case 'ConstraintRule': return t('plugins.installed.references.constraint_rule')
    case 'Menu': return t('plugins.installed.references.menu')
    case 'Edition': return t('plugins.installed.references.edition')
    default: return t('plugins.installed.references.permission_request')
  }
}

/** 一项授予的含义：按前缀分，其余是插件清单申请的框架权限 */
export function grantLabel(t: Translate, grant: string): string {
  if (grant === 'data-scope:all')
    return t('plugins.installed.grants.data_scope_all')
  if (grant.startsWith('callback:')) {
    return t('plugins.installed.grants.callback', {
      code: grant.slice('callback:'.length),
    })
  }
  if (grant.startsWith('config:')) {
    return t('plugins.installed.grants.config', {
      section: grant.slice('config:'.length),
    })
  }
  if (grant.startsWith('binding:')) {
    return t('plugins.installed.grants.binding', {
      name: grant.slice('binding:'.length),
    })
  }
  if (grant.startsWith('capability:')) {
    return t('plugins.installed.grants.capability', {
      id: grant.slice('capability:'.length),
    })
  }
  return t('plugins.installed.grants.other')
}

/** 升级对照里的一项变化 */
export interface PluginChangeRow {
  key: string
  kind: 'operation' | 'permission' | 'page'
  added: boolean
  item: string
}

/** 升级对照摊成一张表：操作、权限码、页面依次，各自先新增后去掉 */
export function upgradeChanges(upgrade: PluginUpgrade): PluginChangeRow[] {
  const rows = (kind: PluginChangeRow['kind'], added: boolean, items: readonly string[]) => items.map(item => ({
    key: `${kind}:${added ? 'added' : 'removed'}:${item}`,
    kind,
    added,
    item,
  }))
  return [
    ...rows('operation', true, upgrade.addedOperations),
    ...rows('operation', false, upgrade.removedOperations),
    ...rows('permission', true, upgrade.addedPermissions),
    ...rows('permission', false, upgrade.removedPermissions),
    ...rows('page', true, upgrade.addedPages),
    ...rows('page', false, upgrade.removedPages),
  ]
}

/** 换版本时预选的可选授予：原来批准过、换上的版本也认的 */
export function carriedOptional(optional: readonly string[], approved: readonly string[]): string[] {
  return approved.filter(grant => optional.includes(grant))
}

/** 必须项一个不少，可选项只取勾上且在可选范围里的；整组按序数排好交给后端 */
export function approvedGrants(required: readonly string[], optional: readonly string[], chosen: readonly string[]): string[] {
  const allowed = new Set(optional)
  return [
    ...new Set([
      ...required,
      ...chosen.filter(grant => allowed.has(grant)),
    ]),
  ].sort()
}
