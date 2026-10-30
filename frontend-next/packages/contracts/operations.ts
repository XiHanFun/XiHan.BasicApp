// 由 scripts/gen-contracts.mjs 从 contracts/openapi.json 生成；不要手改，改后端契约后执行 pnpm gen:contracts。

import type { CallOptions, RouteDef } from '~/request'
import { invoke } from '~/request'
import type { operations } from './types'

/** 每个操作的 HTTP 落点：方法、路由模板、路由值与查询串里的字段名；其余字段进请求体（GET / DELETE 没有请求体）。 */
export const routes = {
  'access-logs.get': { method: 'GET', template: '/api/access-logs/{id}', path: ['id'], query: [], success: 200 },
  'access-logs.list': { method: 'POST', template: '/api/access-logs:list', path: [], query: [], success: 200 },
  'auth.activate-roles': { method: 'POST', template: '/api/auth:activate-roles', path: [], query: [], success: 200 },
  'auth.captcha': { method: 'POST', template: '/api/auth:captcha', path: [], query: [], success: 200 },
  'auth.external-login': { method: 'POST', template: '/api/auth:external-login', path: [], query: [], success: 200 },
  'auth.lock': { method: 'POST', template: '/api/auth:lock', path: [], query: [], success: 200 },
  'auth.login': { method: 'POST', template: '/api/auth:login', path: [], query: [], success: 200 },
  'auth.login-by-code': { method: 'POST', template: '/api/auth:login-by-code', path: [], query: [], success: 200 },
  'auth.login-config': { method: 'GET', template: '/api/auth:login-config', path: [], query: [], success: 200 },
  'auth.logout': { method: 'POST', template: '/api/auth:logout', path: [], query: [], success: 200 },
  'auth.me': { method: 'GET', template: '/api/auth:me', path: [], query: [], success: 200 },
  'auth.permissions': { method: 'GET', template: '/api/auth:permissions', path: [], query: [], success: 200 },
  'auth.refresh': { method: 'POST', template: '/api/auth:refresh', path: [], query: [], success: 200 },
  'auth.send-login-code': { method: 'POST', template: '/api/auth:send-login-code', path: [], query: [], success: 200 },
  'auth.send-two-factor-code': { method: 'POST', template: '/api/auth:send-two-factor-code', path: [], query: [], success: 200 },
  'auth.session-roles': { method: 'GET', template: '/api/auth:session-roles', path: [], query: [], success: 200 },
  'auth.switch-tenant': { method: 'POST', template: '/api/auth:switch-tenant', path: [], query: [], success: 200 },
  'auth.unlock': { method: 'POST', template: '/api/auth:unlock', path: [], query: [], success: 200 },
  'auth.verify-two-factor': { method: 'POST', template: '/api/auth:verify-two-factor', path: [], query: [], success: 200 },
  'bot-configs.create': { method: 'POST', template: '/api/bot-configs', path: [], query: [], success: 201 },
  'bot-configs.delete': { method: 'DELETE', template: '/api/bot-configs', path: [], query: ['ids'], success: 200 },
  'bot-configs.get': { method: 'GET', template: '/api/bot-configs/{id}', path: ['id'], query: [], success: 200 },
  'bot-configs.list': { method: 'POST', template: '/api/bot-configs:list', path: [], query: [], success: 200 },
  'bot-configs.set-default': { method: 'POST', template: '/api/bot-configs/{id}:set-default', path: ['id'], query: [], success: 200 },
  'bot-configs.update': { method: 'PUT', template: '/api/bot-configs/{id}', path: ['id'], query: [], success: 200 },
  'condition-hits.get': { method: 'GET', template: '/api/condition-hits/{id}', path: ['id'], query: [], success: 200 },
  'condition-hits.list': { method: 'POST', template: '/api/condition-hits:list', path: [], query: [], success: 200 },
  'configs.create': { method: 'POST', template: '/api/configs', path: [], query: [], success: 201 },
  'configs.delete': { method: 'DELETE', template: '/api/configs', path: [], query: ['ids'], success: 200 },
  'configs.get': { method: 'GET', template: '/api/configs/{id}', path: ['id'], query: [], success: 200 },
  'configs.list': { method: 'POST', template: '/api/configs:list', path: [], query: [], success: 200 },
  'configs.update': { method: 'PUT', template: '/api/configs/{id}', path: ['id'], query: [], success: 200 },
  'constraint-rules.create': { method: 'POST', template: '/api/constraint-rules', path: [], query: [], success: 201 },
  'constraint-rules.delete': { method: 'DELETE', template: '/api/constraint-rules', path: [], query: ['ids'], success: 200 },
  'constraint-rules.get': { method: 'GET', template: '/api/constraint-rules/{id}', path: ['id'], query: [], success: 200 },
  'constraint-rules.list': { method: 'POST', template: '/api/constraint-rules:list', path: [], query: [], success: 200 },
  'constraint-rules.set-status': { method: 'POST', template: '/api/constraint-rules/{id}:set-status', path: ['id'], query: [], success: 200 },
  'constraint-rules.update': { method: 'PUT', template: '/api/constraint-rules/{id}', path: ['id'], query: [], success: 200 },
  'delegations.create': { method: 'POST', template: '/api/delegations', path: [], query: [], success: 201 },
  'delegations.delete': { method: 'DELETE', template: '/api/delegations', path: [], query: ['ids'], success: 200 },
  'delegations.list': { method: 'POST', template: '/api/delegations:list', path: [], query: [], success: 200 },
  'delegations.revoke': { method: 'POST', template: '/api/delegations/{id}:revoke', path: ['id'], query: [], success: 200 },
  'departments.create': { method: 'POST', template: '/api/departments', path: [], query: [], success: 201 },
  'departments.delete': { method: 'DELETE', template: '/api/departments', path: [], query: ['ids'], success: 200 },
  'departments.get': { method: 'GET', template: '/api/departments/{id}', path: ['id'], query: [], success: 200 },
  'departments.move': { method: 'POST', template: '/api/departments/{id}:move', path: ['id'], query: [], success: 200 },
  'departments.tree': { method: 'GET', template: '/api/departments:tree', path: [], query: [], success: 200 },
  'departments.update': { method: 'PUT', template: '/api/departments/{id}', path: ['id'], query: [], success: 200 },
  'dict-items.create': { method: 'POST', template: '/api/dict-items', path: [], query: [], success: 201 },
  'dict-items.delete': { method: 'DELETE', template: '/api/dict-items', path: [], query: ['ids'], success: 200 },
  'dict-items.get': { method: 'GET', template: '/api/dict-items/{id}', path: ['id'], query: [], success: 200 },
  'dict-items.list': { method: 'GET', template: '/api/dict-items', path: [], query: ['dictId'], success: 200 },
  'dict-items.update': { method: 'PUT', template: '/api/dict-items/{id}', path: ['id'], query: [], success: 200 },
  'dicts.create': { method: 'POST', template: '/api/dicts', path: [], query: [], success: 201 },
  'dicts.delete': { method: 'DELETE', template: '/api/dicts', path: [], query: ['ids'], success: 200 },
  'dicts.get': { method: 'GET', template: '/api/dicts/{id}', path: ['id'], query: [], success: 200 },
  'dicts.list': { method: 'POST', template: '/api/dicts:list', path: [], query: [], success: 200 },
  'dicts.options': { method: 'GET', template: '/api/dicts:options', path: [], query: ['code'], success: 200 },
  'dicts.update': { method: 'PUT', template: '/api/dicts/{id}', path: ['id'], query: [], success: 200 },
  'diff-logs.get': { method: 'GET', template: '/api/diff-logs/{id}', path: ['id'], query: [], success: 200 },
  'diff-logs.list': { method: 'POST', template: '/api/diff-logs:list', path: [], query: [], success: 200 },
  'editions.create': { method: 'POST', template: '/api/editions', path: [], query: [], success: 201 },
  'editions.delete': { method: 'DELETE', template: '/api/editions', path: [], query: ['ids'], success: 200 },
  'editions.get': { method: 'GET', template: '/api/editions/{id}', path: ['id'], query: [], success: 200 },
  'editions.list': { method: 'GET', template: '/api/editions', path: [], query: [], success: 200 },
  'editions.set-permissions': { method: 'POST', template: '/api/editions/{id}:set-permissions', path: ['id'], query: [], success: 200 },
  'editions.update': { method: 'PUT', template: '/api/editions/{id}', path: ['id'], query: [], success: 200 },
  'email-configs.create': { method: 'POST', template: '/api/email-configs', path: [], query: [], success: 201 },
  'email-configs.delete': { method: 'DELETE', template: '/api/email-configs', path: [], query: ['ids'], success: 200 },
  'email-configs.get': { method: 'GET', template: '/api/email-configs/{id}', path: ['id'], query: [], success: 200 },
  'email-configs.list': { method: 'POST', template: '/api/email-configs:list', path: [], query: [], success: 200 },
  'email-configs.set-default': { method: 'POST', template: '/api/email-configs/{id}:set-default', path: ['id'], query: [], success: 200 },
  'email-configs.update': { method: 'PUT', template: '/api/email-configs/{id}', path: ['id'], query: [], success: 200 },
  'emails.cancel': { method: 'POST', template: '/api/emails:cancel', path: [], query: [], success: 200 },
  'emails.delete': { method: 'DELETE', template: '/api/emails', path: [], query: ['ids'], success: 200 },
  'emails.get': { method: 'GET', template: '/api/emails/{id}', path: ['id'], query: [], success: 200 },
  'emails.list': { method: 'POST', template: '/api/emails:list', path: [], query: [], success: 200 },
  'emails.resend': { method: 'POST', template: '/api/emails:resend', path: [], query: [], success: 200 },
  'exception-logs.get': { method: 'GET', template: '/api/exception-logs/{id}', path: ['id'], query: [], success: 200 },
  'exception-logs.list': { method: 'POST', template: '/api/exception-logs:list', path: [], query: [], success: 200 },
  'export-tasks.cancel': { method: 'POST', template: '/api/export-tasks/{id}:cancel', path: ['id'], query: [], success: 200 },
  'export-tasks.delete': { method: 'DELETE', template: '/api/export-tasks', path: [], query: ['ids'], success: 200 },
  'export-tasks.get': { method: 'GET', template: '/api/export-tasks/{id}', path: ['id'], query: [], success: 200 },
  'export-tasks.list': { method: 'POST', template: '/api/export-tasks:list', path: [], query: [], success: 200 },
  'export-tasks.submit': { method: 'POST', template: '/api/export-tasks:submit', path: [], query: [], success: 200 },
  'field-security.create': { method: 'POST', template: '/api/field-security', path: [], query: [], success: 201 },
  'field-security.delete': { method: 'DELETE', template: '/api/field-security', path: [], query: ['ids'], success: 200 },
  'field-security.entities': { method: 'GET', template: '/api/field-security:entities', path: [], query: [], success: 200 },
  'field-security.get': { method: 'GET', template: '/api/field-security/{id}', path: ['id'], query: [], success: 200 },
  'field-security.list': { method: 'POST', template: '/api/field-security:list', path: [], query: [], success: 200 },
  'field-security.update': { method: 'PUT', template: '/api/field-security/{id}', path: ['id'], query: [], success: 200 },
  'files.abort-upload': { method: 'POST', template: '/api/files:abort-upload', path: [], query: [], success: 200 },
  'files.complete-upload': { method: 'POST', template: '/api/files:complete-upload', path: [], query: [], success: 200 },
  'files.delete': { method: 'DELETE', template: '/api/files', path: [], query: ['ids'], success: 200 },
  'files.fast-upload': { method: 'POST', template: '/api/files:fast-upload', path: [], query: [], success: 200 },
  'files.get': { method: 'GET', template: '/api/files/{id}', path: ['id'], query: [], success: 200 },
  'files.list': { method: 'POST', template: '/api/files:list', path: [], query: [], success: 200 },
  'files.presign': { method: 'GET', template: '/api/files/{id}:presign', path: ['id'], query: ['expiresInSeconds', 'download'], success: 200 },
  'files.set-primary-storage': { method: 'POST', template: '/api/files/{id}:set-primary-storage', path: ['id'], query: [], success: 200 },
  'files.set-status': { method: 'POST', template: '/api/files:set-status', path: [], query: [], success: 200 },
  'files.set-storage-status': { method: 'POST', template: '/api/files/{id}:set-storage-status', path: ['id'], query: [], success: 200 },
  'files.sign-upload-parts': { method: 'POST', template: '/api/files:sign-upload-parts', path: [], query: [], success: 200 },
  'files.start-upload': { method: 'POST', template: '/api/files:start-upload', path: [], query: [], success: 200 },
  'files.storages': { method: 'GET', template: '/api/files/{id}:storages', path: ['id'], query: [], success: 200 },
  'files.update': { method: 'PUT', template: '/api/files/{id}', path: ['id'], query: [], success: 200 },
  'files.upload-session': { method: 'GET', template: '/api/files:upload-session', path: [], query: ['uploadId'], success: 200 },
  'files.verify-storage': { method: 'POST', template: '/api/files/{id}:verify-storage', path: ['id'], query: [], success: 200 },
  'impersonation.candidates': { method: 'GET', template: '/api/impersonation:candidates', path: [], query: ['tenantId', 'keyword', 'pageIndex', 'pageSize'], success: 200 },
  'impersonation.start': { method: 'POST', template: '/api/impersonation:start', path: [], query: [], success: 200 },
  'impersonation.stop': { method: 'POST', template: '/api/impersonation:stop', path: [], query: [], success: 200 },
  'import-histories.create': { method: 'POST', template: '/api/import-histories', path: [], query: [], success: 201 },
  'import-histories.recent': { method: 'GET', template: '/api/import-histories:recent', path: [], query: ['pageCode', 'count'], success: 200 },
  'inbox.banners': { method: 'GET', template: '/api/inbox:banners', path: [], query: [], success: 200 },
  'inbox.confirm': { method: 'POST', template: '/api/inbox/{id}:confirm', path: ['id'], query: [], success: 200 },
  'inbox.delete': { method: 'DELETE', template: '/api/inbox', path: [], query: ['ids'], success: 200 },
  'inbox.list': { method: 'POST', template: '/api/inbox:list', path: [], query: [], success: 200 },
  'inbox.mandatory': { method: 'GET', template: '/api/inbox:mandatory', path: [], query: [], success: 200 },
  'inbox.mark-all-read': { method: 'POST', template: '/api/inbox:mark-all-read', path: [], query: [], success: 200 },
  'inbox.mark-popup-shown': { method: 'POST', template: '/api/inbox/{id}:mark-popup-shown', path: ['id'], query: [], success: 200 },
  'inbox.mark-read': { method: 'POST', template: '/api/inbox:mark-read', path: [], query: [], success: 200 },
  'inbox.popups': { method: 'GET', template: '/api/inbox:popups', path: [], query: [], success: 200 },
  'job-runs.list': { method: 'POST', template: '/api/job-runs:list', path: [], query: [], success: 200 },
  'jobs.get': { method: 'GET', template: '/api/jobs/{id}', path: ['id'], query: [], success: 200 },
  'jobs.list': { method: 'POST', template: '/api/jobs:list', path: [], query: [], success: 200 },
  'jobs.pause': { method: 'POST', template: '/api/jobs:pause', path: [], query: [], success: 200 },
  'jobs.reschedule': { method: 'POST', template: '/api/jobs/{id}:reschedule', path: ['id'], query: [], success: 200 },
  'jobs.restore': { method: 'POST', template: '/api/jobs:restore', path: [], query: [], success: 200 },
  'jobs.resume': { method: 'POST', template: '/api/jobs:resume', path: [], query: [], success: 200 },
  'jobs.trigger': { method: 'POST', template: '/api/jobs/{id}:trigger', path: ['id'], query: [], success: 200 },
  'jobs.types': { method: 'GET', template: '/api/jobs:types', path: [], query: [], success: 200 },
  'jobs.validate-schedule': { method: 'GET', template: '/api/jobs:validate-schedule', path: [], query: ['expression', 'timeZone', 'count'], success: 200 },
  'login-bans.lift': { method: 'POST', template: '/api/login-bans:lift', path: [], query: [], success: 200 },
  'login-bans.list': { method: 'GET', template: '/api/login-bans', path: [], query: [], success: 200 },
  'login-logs.get': { method: 'GET', template: '/api/login-logs/{id}', path: ['id'], query: [], success: 200 },
  'login-logs.list': { method: 'POST', template: '/api/login-logs:list', path: [], query: [], success: 200 },
  'memberships.create': { method: 'POST', template: '/api/memberships', path: [], query: [], success: 201 },
  'memberships.delete': { method: 'DELETE', template: '/api/memberships', path: [], query: ['ids'], success: 200 },
  'memberships.list': { method: 'POST', template: '/api/memberships:list', path: [], query: [], success: 200 },
  'memberships.update': { method: 'PUT', template: '/api/memberships/{id}', path: ['id'], query: [], success: 200 },
  'menus.create': { method: 'POST', template: '/api/menus', path: [], query: [], success: 201 },
  'menus.delete': { method: 'DELETE', template: '/api/menus', path: [], query: ['ids'], success: 200 },
  'menus.get': { method: 'GET', template: '/api/menus/{id}', path: ['id'], query: [], success: 200 },
  'menus.list': { method: 'GET', template: '/api/menus', path: [], query: [], success: 200 },
  'menus.reset': { method: 'POST', template: '/api/menus/{id}:reset', path: ['id'], query: [], success: 200 },
  'menus.update': { method: 'PUT', template: '/api/menus/{id}', path: ['id'], query: [], success: 200 },
  'message-templates.create': { method: 'POST', template: '/api/message-templates', path: [], query: [], success: 201 },
  'message-templates.delete': { method: 'DELETE', template: '/api/message-templates', path: [], query: ['ids'], success: 200 },
  'message-templates.get': { method: 'GET', template: '/api/message-templates/{id}', path: ['id'], query: [], success: 200 },
  'message-templates.list': { method: 'POST', template: '/api/message-templates:list', path: [], query: [], success: 200 },
  'message-templates.update': { method: 'PUT', template: '/api/message-templates/{id}', path: ['id'], query: [], success: 200 },
  'migrations.get': { method: 'GET', template: '/api/migrations/{id}', path: ['id'], query: [], success: 200 },
  'migrations.list': { method: 'POST', template: '/api/migrations:list', path: [], query: [], success: 200 },
  'my-activity.summary': { method: 'GET', template: '/api/my-activity:summary', path: [], query: ['timeZone'], success: 200 },
  'my-api-credentials.create': { method: 'POST', template: '/api/my-api-credentials', path: [], query: [], success: 201 },
  'my-api-credentials.delete': { method: 'DELETE', template: '/api/my-api-credentials', path: [], query: ['ids'], success: 200 },
  'my-api-credentials.list': { method: 'GET', template: '/api/my-api-credentials', path: [], query: [], success: 200 },
  'my-api-credentials.rotate-secret': { method: 'POST', template: '/api/my-api-credentials/{id}:rotate-secret', path: ['id'], query: [], success: 200 },
  'my-api-credentials.settings': { method: 'GET', template: '/api/my-api-credentials:settings', path: [], query: [], success: 200 },
  'my-api-credentials.update': { method: 'PUT', template: '/api/my-api-credentials/{id}', path: ['id'], query: [], success: 200 },
  'my-api-credentials.update-settings': { method: 'POST', template: '/api/my-api-credentials:update-settings', path: [], query: [], success: 200 },
  'my-login-logs.list': { method: 'GET', template: '/api/my-login-logs', path: [], query: ['page', 'pageSize'], success: 200 },
  'my-oauth-apps.create': { method: 'POST', template: '/api/my-oauth-apps', path: [], query: [], success: 201 },
  'my-oauth-apps.delete': { method: 'DELETE', template: '/api/my-oauth-apps', path: [], query: ['ids'], success: 200 },
  'my-oauth-apps.get': { method: 'GET', template: '/api/my-oauth-apps/{id}', path: ['id'], query: [], success: 200 },
  'my-oauth-apps.list': { method: 'GET', template: '/api/my-oauth-apps', path: [], query: [], success: 200 },
  'my-oauth-apps.reset-secret': { method: 'POST', template: '/api/my-oauth-apps/{id}:reset-secret', path: ['id'], query: [], success: 200 },
  'my-oauth-apps.update': { method: 'PUT', template: '/api/my-oauth-apps/{id}', path: ['id'], query: [], success: 200 },
  'notifications.create': { method: 'POST', template: '/api/notifications', path: [], query: [], success: 201 },
  'notifications.delete': { method: 'DELETE', template: '/api/notifications', path: [], query: ['ids'], success: 200 },
  'notifications.get': { method: 'GET', template: '/api/notifications/{id}', path: ['id'], query: [], success: 200 },
  'notifications.list': { method: 'POST', template: '/api/notifications:list', path: [], query: [], success: 200 },
  'notifications.publish': { method: 'POST', template: '/api/notifications/{id}:publish', path: ['id'], query: [], success: 200 },
  'notifications.read-stats': { method: 'GET', template: '/api/notifications/{id}:read-stats', path: ['id'], query: [], success: 200 },
  'notifications.recipients': { method: 'POST', template: '/api/notifications/{id}:recipients', path: ['id'], query: [], success: 200 },
  'notifications.remind': { method: 'POST', template: '/api/notifications/{id}:remind', path: ['id'], query: [], success: 200 },
  'notifications.update': { method: 'PUT', template: '/api/notifications/{id}', path: ['id'], query: [], success: 200 },
  'numbering-rules.allocations': { method: 'POST', template: '/api/numbering-rules/{id}:allocations', path: ['id'], query: [], success: 200 },
  'numbering-rules.create': { method: 'POST', template: '/api/numbering-rules', path: [], query: [], success: 201 },
  'numbering-rules.delete': { method: 'DELETE', template: '/api/numbering-rules', path: [], query: ['ids'], success: 200 },
  'numbering-rules.generate': { method: 'POST', template: '/api/numbering-rules:generate', path: [], query: [], success: 200 },
  'numbering-rules.get': { method: 'GET', template: '/api/numbering-rules/{id}', path: ['id'], query: [], success: 200 },
  'numbering-rules.list': { method: 'POST', template: '/api/numbering-rules:list', path: [], query: [], success: 200 },
  'numbering-rules.preview': { method: 'GET', template: '/api/numbering-rules:preview', path: [], query: ['prefix', 'separator', 'dateFormat', 'serialLength', 'resetCycle', 'timeZoneId', 'startValue', 'count'], success: 200 },
  'numbering-rules.reset': { method: 'POST', template: '/api/numbering-rules/{id}:reset', path: ['id'], query: [], success: 200 },
  'numbering-rules.update': { method: 'PUT', template: '/api/numbering-rules/{id}', path: ['id'], query: [], success: 200 },
  'oauth-apps.create': { method: 'POST', template: '/api/oauth-apps', path: [], query: [], success: 201 },
  'oauth-apps.delete': { method: 'DELETE', template: '/api/oauth-apps', path: [], query: ['ids'], success: 200 },
  'oauth-apps.get': { method: 'GET', template: '/api/oauth-apps/{id}', path: ['id'], query: [], success: 200 },
  'oauth-apps.list': { method: 'POST', template: '/api/oauth-apps:list', path: [], query: [], success: 200 },
  'oauth-apps.reset-secret': { method: 'POST', template: '/api/oauth-apps/{id}:reset-secret', path: ['id'], query: [], success: 200 },
  'oauth-apps.update': { method: 'PUT', template: '/api/oauth-apps/{id}', path: ['id'], query: [], success: 200 },
  'oauth-consents.approve': { method: 'POST', template: '/api/oauth-consents:approve', path: [], query: [], success: 200 },
  'oauth-consents.deny': { method: 'POST', template: '/api/oauth-consents:deny', path: [], query: [], success: 200 },
  'oauth-consents.list': { method: 'GET', template: '/api/oauth-consents', path: [], query: [], success: 200 },
  'oauth-consents.resolve': { method: 'GET', template: '/api/oauth-consents:resolve', path: [], query: ['responseType', 'clientId', 'redirectUri', 'scope', 'state', 'codeChallenge', 'codeChallengeMethod', 'nonce'], success: 200 },
  'oauth-consents.revoke': { method: 'POST', template: '/api/oauth-consents:revoke', path: [], query: [], success: 200 },
  'open-api-logs.get': { method: 'GET', template: '/api/open-api-logs/{id}', path: ['id'], query: [], success: 200 },
  'open-api-logs.list': { method: 'POST', template: '/api/open-api-logs:list', path: [], query: [], success: 200 },
  'open.echo': { method: 'POST', template: '/api/open:echo', path: [], query: [], success: 200 },
  'open.ping': { method: 'GET', template: '/api/open:ping', path: [], query: [], success: 200 },
  'operation-logs.get': { method: 'GET', template: '/api/operation-logs/{id}', path: ['id'], query: [], success: 200 },
  'operation-logs.list': { method: 'POST', template: '/api/operation-logs:list', path: [], query: [], success: 200 },
  'permission-change-logs.get': { method: 'GET', template: '/api/permission-change-logs/{id}', path: ['id'], query: [], success: 200 },
  'permission-change-logs.list': { method: 'POST', template: '/api/permission-change-logs:list', path: [], query: [], success: 200 },
  'permission-conditions.attributes': { method: 'GET', template: '/api/permission-conditions:attributes', path: [], query: [], success: 200 },
  'permission-conditions.batch-trial': { method: 'GET', template: '/api/permission-conditions:batch-trial', path: [], query: ['subjectKind', 'subjectId', 'permission', 'ip', 'time', 'userAgent', 'path', 'method'], success: 200 },
  'permission-conditions.create': { method: 'POST', template: '/api/permission-conditions', path: [], query: [], success: 201 },
  'permission-conditions.delete': { method: 'DELETE', template: '/api/permission-conditions', path: [], query: ['ids'], success: 200 },
  'permission-conditions.inventory': { method: 'GET', template: '/api/permission-conditions:inventory', path: [], query: [], success: 200 },
  'permission-conditions.list': { method: 'GET', template: '/api/permission-conditions', path: [], query: ['subjectKind', 'subjectId'], success: 200 },
  'permission-conditions.trial': { method: 'GET', template: '/api/permission-conditions:trial', path: [], query: ['userId', 'permission', 'ip', 'time', 'userAgent', 'path', 'method', 'isImpersonating'], success: 200 },
  'permission-conditions.update': { method: 'PUT', template: '/api/permission-conditions/{id}', path: ['id'], query: [], success: 200 },
  'permissions.list': { method: 'GET', template: '/api/permissions', path: [], query: ['side'], success: 200 },
  'policies.attributes': { method: 'GET', template: '/api/policies:attributes', path: [], query: [], success: 200 },
  'policies.create': { method: 'POST', template: '/api/policies', path: [], query: [], success: 201 },
  'policies.delete': { method: 'DELETE', template: '/api/policies', path: [], query: ['ids'], success: 200 },
  'policies.get': { method: 'GET', template: '/api/policies/{id}', path: ['id'], query: [], success: 200 },
  'policies.list': { method: 'POST', template: '/api/policies:list', path: [], query: [], success: 200 },
  'policies.set-action': { method: 'POST', template: '/api/policies/{id}:set-action', path: ['id'], query: [], success: 200 },
  'policies.set-status': { method: 'POST', template: '/api/policies/{id}:set-status', path: ['id'], query: [], success: 200 },
  'policies.templates': { method: 'GET', template: '/api/policies:templates', path: [], query: [], success: 200 },
  'policies.trial': { method: 'POST', template: '/api/policies:trial', path: [], query: [], success: 200 },
  'policies.update': { method: 'PUT', template: '/api/policies/{id}', path: ['id'], query: [], success: 200 },
  'positions.create': { method: 'POST', template: '/api/positions', path: [], query: [], success: 201 },
  'positions.delete': { method: 'DELETE', template: '/api/positions', path: [], query: ['ids'], success: 200 },
  'positions.get': { method: 'GET', template: '/api/positions/{id}', path: ['id'], query: [], success: 200 },
  'positions.list': { method: 'POST', template: '/api/positions:list', path: [], query: [], success: 200 },
  'positions.update': { method: 'PUT', template: '/api/positions/{id}', path: ['id'], query: [], success: 200 },
  'profile.begin-external-bind': { method: 'POST', template: '/api/profile:begin-external-bind', path: [], query: [], success: 200 },
  'profile.change-contact': { method: 'POST', template: '/api/profile:change-contact', path: [], query: [], success: 200 },
  'profile.change-password': { method: 'POST', template: '/api/profile:change-password', path: [], query: [], success: 200 },
  'profile.change-username': { method: 'POST', template: '/api/profile:change-username', path: [], query: [], success: 200 },
  'profile.disable-two-factor': { method: 'POST', template: '/api/profile:disable-two-factor', path: [], query: [], success: 200 },
  'profile.enable-two-factor': { method: 'POST', template: '/api/profile:enable-two-factor', path: [], query: [], success: 200 },
  'profile.external-logins': { method: 'GET', template: '/api/profile:external-logins', path: [], query: [], success: 200 },
  'profile.get': { method: 'GET', template: '/api/profile', path: [], query: [], success: 200 },
  'profile.link-external': { method: 'POST', template: '/api/profile:link-external', path: [], query: [], success: 200 },
  'profile.notification-preference': { method: 'GET', template: '/api/profile:notification-preference', path: [], query: [], success: 200 },
  'profile.remove-avatar': { method: 'POST', template: '/api/profile:remove-avatar', path: [], query: [], success: 200 },
  'profile.revoke-other-sessions': { method: 'POST', template: '/api/profile:revoke-other-sessions', path: [], query: [], success: 200 },
  'profile.revoke-session': { method: 'POST', template: '/api/profile:revoke-session', path: [], query: [], success: 200 },
  'profile.send-change-contact-code': { method: 'POST', template: '/api/profile:send-change-contact-code', path: [], query: [], success: 200 },
  'profile.send-two-factor-code': { method: 'POST', template: '/api/profile:send-two-factor-code', path: [], query: [], success: 200 },
  'profile.send-verification-code': { method: 'POST', template: '/api/profile:send-verification-code', path: [], query: [], success: 200 },
  'profile.sessions': { method: 'GET', template: '/api/profile:sessions', path: [], query: [], success: 200 },
  'profile.setup-totp': { method: 'POST', template: '/api/profile:setup-totp', path: [], query: [], success: 200 },
  'profile.unlink-external': { method: 'POST', template: '/api/profile:unlink-external', path: [], query: [], success: 200 },
  'profile.update': { method: 'PUT', template: '/api/profile', path: [], query: [], success: 200 },
  'profile.update-notification-preference': { method: 'POST', template: '/api/profile:update-notification-preference', path: [], query: [], success: 200 },
  'profile.verify-contact': { method: 'POST', template: '/api/profile:verify-contact', path: [], query: [], success: 200 },
  'roles.assign': { method: 'POST', template: '/api/roles/{id}:assign', path: ['id'], query: [], success: 200 },
  'roles.by-user': { method: 'GET', template: '/api/roles:by-user', path: [], query: ['userId'], success: 200 },
  'roles.create': { method: 'POST', template: '/api/roles', path: [], query: [], success: 201 },
  'roles.delete': { method: 'DELETE', template: '/api/roles', path: [], query: ['ids'], success: 200 },
  'roles.deny': { method: 'POST', template: '/api/roles/{id}:deny', path: ['id'], query: [], success: 200 },
  'roles.get': { method: 'GET', template: '/api/roles/{id}', path: ['id'], query: [], success: 200 },
  'roles.grant': { method: 'POST', template: '/api/roles/{id}:grant', path: ['id'], query: [], success: 200 },
  'roles.hierarchy': { method: 'GET', template: '/api/roles/{id}:hierarchy', path: ['id'], query: [], success: 200 },
  'roles.list': { method: 'POST', template: '/api/roles:list', path: [], query: [], success: 200 },
  'roles.members': { method: 'GET', template: '/api/roles/{id}:members', path: ['id'], query: [], success: 200 },
  'roles.revoke': { method: 'POST', template: '/api/roles/{id}:revoke', path: ['id'], query: [], success: 200 },
  'roles.set-parents': { method: 'POST', template: '/api/roles/{id}:set-parents', path: ['id'], query: [], success: 200 },
  'roles.unassign': { method: 'POST', template: '/api/roles/{id}:unassign', path: ['id'], query: [], success: 200 },
  'roles.update': { method: 'PUT', template: '/api/roles/{id}', path: ['id'], query: [], success: 200 },
  'roles.update-member': { method: 'POST', template: '/api/roles/{id}:update-member', path: ['id'], query: [], success: 200 },
  'session-roles.get': { method: 'GET', template: '/api/session-roles/{id}', path: ['id'], query: [], success: 200 },
  'session-roles.list': { method: 'POST', template: '/api/session-roles:list', path: [], query: [], success: 200 },
  'sessions.list': { method: 'POST', template: '/api/sessions:list', path: [], query: [], success: 200 },
  'sessions.revoke': { method: 'POST', template: '/api/sessions:revoke', path: [], query: [], success: 200 },
  'sessions.summary': { method: 'GET', template: '/api/sessions:summary', path: [], query: [], success: 200 },
  'sms-configs.create': { method: 'POST', template: '/api/sms-configs', path: [], query: [], success: 201 },
  'sms-configs.delete': { method: 'DELETE', template: '/api/sms-configs', path: [], query: ['ids'], success: 200 },
  'sms-configs.get': { method: 'GET', template: '/api/sms-configs/{id}', path: ['id'], query: [], success: 200 },
  'sms-configs.list': { method: 'POST', template: '/api/sms-configs:list', path: [], query: [], success: 200 },
  'sms-configs.set-default': { method: 'POST', template: '/api/sms-configs/{id}:set-default', path: ['id'], query: [], success: 200 },
  'sms-configs.update': { method: 'PUT', template: '/api/sms-configs/{id}', path: ['id'], query: [], success: 200 },
  'sms.cancel': { method: 'POST', template: '/api/sms:cancel', path: [], query: [], success: 200 },
  'sms.delete': { method: 'DELETE', template: '/api/sms', path: [], query: ['ids'], success: 200 },
  'sms.get': { method: 'GET', template: '/api/sms/{id}', path: ['id'], query: [], success: 200 },
  'sms.list': { method: 'POST', template: '/api/sms:list', path: [], query: [], success: 200 },
  'sms.resend': { method: 'POST', template: '/api/sms:resend', path: [], query: [], success: 200 },
  'storage-configs.create': { method: 'POST', template: '/api/storage-configs', path: [], query: [], success: 201 },
  'storage-configs.delete': { method: 'DELETE', template: '/api/storage-configs', path: [], query: ['ids'], success: 200 },
  'storage-configs.get': { method: 'GET', template: '/api/storage-configs/{id}', path: ['id'], query: [], success: 200 },
  'storage-configs.list': { method: 'POST', template: '/api/storage-configs:list', path: [], query: [], success: 200 },
  'storage-configs.set-default': { method: 'POST', template: '/api/storage-configs/{id}:set-default', path: ['id'], query: [], success: 200 },
  'storage-configs.set-status': { method: 'POST', template: '/api/storage-configs/{id}:set-status', path: ['id'], query: [], success: 200 },
  'storage-configs.update': { method: 'PUT', template: '/api/storage-configs/{id}', path: ['id'], query: [], success: 200 },
  'subscription.get': { method: 'GET', template: '/api/subscription', path: [], query: [], success: 200 },
  'telegram-bots.create': { method: 'POST', template: '/api/telegram-bots', path: [], query: [], success: 201 },
  'telegram-bots.delete': { method: 'DELETE', template: '/api/telegram-bots', path: [], query: ['ids'], success: 200 },
  'telegram-bots.get': { method: 'GET', template: '/api/telegram-bots/{id}', path: ['id'], query: [], success: 200 },
  'telegram-bots.list': { method: 'POST', template: '/api/telegram-bots:list', path: [], query: [], success: 200 },
  'telegram-bots.update': { method: 'PUT', template: '/api/telegram-bots/{id}', path: ['id'], query: [], success: 200 },
  'tenant-members.create': { method: 'POST', template: '/api/tenant-members', path: [], query: [], success: 201 },
  'tenant-members.delete': { method: 'DELETE', template: '/api/tenant-members', path: [], query: ['ids'], success: 200 },
  'tenant-members.list': { method: 'GET', template: '/api/tenant-members', path: [], query: ['tenantId'], success: 200 },
  'tenants.change-edition': { method: 'POST', template: '/api/tenants/{id}:change-edition', path: ['id'], query: [], success: 200 },
  'tenants.configure-database': { method: 'POST', template: '/api/tenants/{id}:configure-database', path: ['id'], query: [], success: 200 },
  'tenants.create': { method: 'POST', template: '/api/tenants', path: [], query: [], success: 201 },
  'tenants.delete': { method: 'DELETE', template: '/api/tenants', path: [], query: ['ids'], success: 200 },
  'tenants.get': { method: 'GET', template: '/api/tenants/{id}', path: ['id'], query: [], success: 200 },
  'tenants.initialize-admin': { method: 'POST', template: '/api/tenants/{id}:initialize-admin', path: ['id'], query: [], success: 200 },
  'tenants.initialize-database': { method: 'POST', template: '/api/tenants/{id}:initialize-database', path: ['id'], query: [], success: 200 },
  'tenants.list': { method: 'POST', template: '/api/tenants:list', path: [], query: [], success: 200 },
  'tenants.mine': { method: 'GET', template: '/api/tenants:mine', path: [], query: [], success: 200 },
  'tenants.quotas': { method: 'GET', template: '/api/tenants:quotas', path: [], query: ['overOnly'], success: 200 },
  'tenants.resume': { method: 'POST', template: '/api/tenants:resume', path: [], query: [], success: 200 },
  'tenants.suspend': { method: 'POST', template: '/api/tenants:suspend', path: [], query: [], success: 200 },
  'tenants.transfer-owner': { method: 'POST', template: '/api/tenants/{id}:transfer-owner', path: ['id'], query: [], success: 200 },
  'tenants.update': { method: 'PUT', template: '/api/tenants/{id}', path: ['id'], query: [], success: 200 },
  'traces.timeline': { method: 'GET', template: '/api/traces:timeline', path: [], query: ['dimension', 'value', 'from', 'to', 'logTypes', 'maxPerType'], success: 200 },
  'user-data-scopes.get': { method: 'GET', template: '/api/user-data-scopes', path: [], query: ['userId'], success: 200 },
  'user-data-scopes.set': { method: 'POST', template: '/api/user-data-scopes:set', path: [], query: [], success: 200 },
  'user-grants.deny': { method: 'POST', template: '/api/user-grants:deny', path: [], query: [], success: 200 },
  'user-grants.grant': { method: 'POST', template: '/api/user-grants:grant', path: [], query: [], success: 200 },
  'user-grants.list': { method: 'GET', template: '/api/user-grants', path: [], query: ['userId'], success: 200 },
  'user-grants.revoke': { method: 'POST', template: '/api/user-grants:revoke', path: [], query: [], success: 200 },
  'user-settings.get': { method: 'GET', template: '/api/user-settings', path: [], query: ['scene', 'key'], success: 200 },
  'user-settings.update': { method: 'PUT', template: '/api/user-settings', path: [], query: [], success: 200 },
  'users.create': { method: 'POST', template: '/api/users', path: [], query: [], success: 201 },
  'users.delete': { method: 'DELETE', template: '/api/users', path: [], query: ['ids'], success: 200 },
  'users.get': { method: 'GET', template: '/api/users/{id}', path: ['id'], query: [], success: 200 },
  'users.list': { method: 'POST', template: '/api/users:list', path: [], query: [], success: 200 },
  'users.lock': { method: 'POST', template: '/api/users:lock', path: [], query: [], success: 200 },
  'users.reset-password': { method: 'POST', template: '/api/users/{id}:reset-password', path: ['id'], query: [], success: 200 },
  'users.reset-two-factor': { method: 'POST', template: '/api/users:reset-two-factor', path: [], query: [], success: 200 },
  'users.unlock': { method: 'POST', template: '/api/users:unlock', path: [], query: [], success: 200 },
  'users.update': { method: 'PUT', template: '/api/users/{id}', path: ['id'], query: [], success: 200 },
  'versions.get': { method: 'GET', template: '/api/versions/{id}', path: ['id'], query: [], success: 200 },
  'versions.list': { method: 'POST', template: '/api/versions:list', path: [], query: [], success: 200 },
} as const satisfies Record<string, RouteDef>

export type OperationId = keyof typeof routes

type JsonBody<T> = T extends { content: { 'application/json': infer B } } ? B : never
type Params<T> = T extends { parameters: infer P } ? (P extends { path: infer A } ? (A extends object ? A : object) : object) & (P extends { query?: infer Q } ? (Q extends object ? Q : object) : object) : object
/** 操作的请求类型：有请求体取请求体，否则是路由值与查询串的并集。 */
type BodyOf<T> = T extends { requestBody?: infer R } ? JsonBody<NonNullable<R>> : never
export type RequestOf<Id extends OperationId> = [BodyOf<operations[Id]>] extends [never] ? Params<operations[Id]> : BodyOf<operations[Id]>
type SuccessCode<Id extends OperationId> = (typeof routes)[Id]['success']
/** 操作的成功响应类型。 */
export type ResponseOf<Id extends OperationId> = operations[Id]['responses'] extends infer R ? (SuccessCode<Id> extends keyof R ? JsonBody<R[SuccessCode<Id>]> : never) : never

/** 资源 `access-logs` 的操作。 */
export const accessLogs = {
  get: (request: RequestOf<'access-logs.get'>, options?: CallOptions) => invoke<RequestOf<'access-logs.get'>, ResponseOf<'access-logs.get'>>('access-logs.get', routes['access-logs.get'], request, options),
  list: (request: RequestOf<'access-logs.list'>, options?: CallOptions) => invoke<RequestOf<'access-logs.list'>, ResponseOf<'access-logs.list'>>('access-logs.list', routes['access-logs.list'], request, options),
}

/** 资源 `auth` 的操作。 */
export const auth = {
  activateRoles: (request: RequestOf<'auth.activate-roles'>, options?: CallOptions) => invoke<RequestOf<'auth.activate-roles'>, ResponseOf<'auth.activate-roles'>>('auth.activate-roles', routes['auth.activate-roles'], request, options),
  captcha: (request: RequestOf<'auth.captcha'>, options?: CallOptions) => invoke<RequestOf<'auth.captcha'>, ResponseOf<'auth.captcha'>>('auth.captcha', routes['auth.captcha'], request, options),
  externalLogin: (request: RequestOf<'auth.external-login'>, options?: CallOptions) => invoke<RequestOf<'auth.external-login'>, ResponseOf<'auth.external-login'>>('auth.external-login', routes['auth.external-login'], request, options),
  lock: (request: RequestOf<'auth.lock'>, options?: CallOptions) => invoke<RequestOf<'auth.lock'>, ResponseOf<'auth.lock'>>('auth.lock', routes['auth.lock'], request, options),
  login: (request: RequestOf<'auth.login'>, options?: CallOptions) => invoke<RequestOf<'auth.login'>, ResponseOf<'auth.login'>>('auth.login', routes['auth.login'], request, options),
  loginByCode: (request: RequestOf<'auth.login-by-code'>, options?: CallOptions) => invoke<RequestOf<'auth.login-by-code'>, ResponseOf<'auth.login-by-code'>>('auth.login-by-code', routes['auth.login-by-code'], request, options),
  loginConfig: (request: RequestOf<'auth.login-config'>, options?: CallOptions) => invoke<RequestOf<'auth.login-config'>, ResponseOf<'auth.login-config'>>('auth.login-config', routes['auth.login-config'], request, options),
  logout: (request: RequestOf<'auth.logout'>, options?: CallOptions) => invoke<RequestOf<'auth.logout'>, ResponseOf<'auth.logout'>>('auth.logout', routes['auth.logout'], request, options),
  me: (request: RequestOf<'auth.me'>, options?: CallOptions) => invoke<RequestOf<'auth.me'>, ResponseOf<'auth.me'>>('auth.me', routes['auth.me'], request, options),
  permissions: (request: RequestOf<'auth.permissions'>, options?: CallOptions) => invoke<RequestOf<'auth.permissions'>, ResponseOf<'auth.permissions'>>('auth.permissions', routes['auth.permissions'], request, options),
  refresh: (request: RequestOf<'auth.refresh'>, options?: CallOptions) => invoke<RequestOf<'auth.refresh'>, ResponseOf<'auth.refresh'>>('auth.refresh', routes['auth.refresh'], request, options),
  sendLoginCode: (request: RequestOf<'auth.send-login-code'>, options?: CallOptions) => invoke<RequestOf<'auth.send-login-code'>, ResponseOf<'auth.send-login-code'>>('auth.send-login-code', routes['auth.send-login-code'], request, options),
  sendTwoFactorCode: (request: RequestOf<'auth.send-two-factor-code'>, options?: CallOptions) => invoke<RequestOf<'auth.send-two-factor-code'>, ResponseOf<'auth.send-two-factor-code'>>('auth.send-two-factor-code', routes['auth.send-two-factor-code'], request, options),
  sessionRoles: (request: RequestOf<'auth.session-roles'>, options?: CallOptions) => invoke<RequestOf<'auth.session-roles'>, ResponseOf<'auth.session-roles'>>('auth.session-roles', routes['auth.session-roles'], request, options),
  switchTenant: (request: RequestOf<'auth.switch-tenant'>, options?: CallOptions) => invoke<RequestOf<'auth.switch-tenant'>, ResponseOf<'auth.switch-tenant'>>('auth.switch-tenant', routes['auth.switch-tenant'], request, options),
  unlock: (request: RequestOf<'auth.unlock'>, options?: CallOptions) => invoke<RequestOf<'auth.unlock'>, ResponseOf<'auth.unlock'>>('auth.unlock', routes['auth.unlock'], request, options),
  verifyTwoFactor: (request: RequestOf<'auth.verify-two-factor'>, options?: CallOptions) => invoke<RequestOf<'auth.verify-two-factor'>, ResponseOf<'auth.verify-two-factor'>>('auth.verify-two-factor', routes['auth.verify-two-factor'], request, options),
}

/** 资源 `bot-configs` 的操作。 */
export const botConfigs = {
  create: (request: RequestOf<'bot-configs.create'>, options?: CallOptions) => invoke<RequestOf<'bot-configs.create'>, ResponseOf<'bot-configs.create'>>('bot-configs.create', routes['bot-configs.create'], request, options),
  delete: (request: RequestOf<'bot-configs.delete'>, options?: CallOptions) => invoke<RequestOf<'bot-configs.delete'>, ResponseOf<'bot-configs.delete'>>('bot-configs.delete', routes['bot-configs.delete'], request, options),
  get: (request: RequestOf<'bot-configs.get'>, options?: CallOptions) => invoke<RequestOf<'bot-configs.get'>, ResponseOf<'bot-configs.get'>>('bot-configs.get', routes['bot-configs.get'], request, options),
  list: (request: RequestOf<'bot-configs.list'>, options?: CallOptions) => invoke<RequestOf<'bot-configs.list'>, ResponseOf<'bot-configs.list'>>('bot-configs.list', routes['bot-configs.list'], request, options),
  setDefault: (request: RequestOf<'bot-configs.set-default'>, options?: CallOptions) => invoke<RequestOf<'bot-configs.set-default'>, ResponseOf<'bot-configs.set-default'>>('bot-configs.set-default', routes['bot-configs.set-default'], request, options),
  update: (request: RequestOf<'bot-configs.update'>, options?: CallOptions) => invoke<RequestOf<'bot-configs.update'>, ResponseOf<'bot-configs.update'>>('bot-configs.update', routes['bot-configs.update'], request, options),
}

/** 资源 `condition-hits` 的操作。 */
export const conditionHits = {
  get: (request: RequestOf<'condition-hits.get'>, options?: CallOptions) => invoke<RequestOf<'condition-hits.get'>, ResponseOf<'condition-hits.get'>>('condition-hits.get', routes['condition-hits.get'], request, options),
  list: (request: RequestOf<'condition-hits.list'>, options?: CallOptions) => invoke<RequestOf<'condition-hits.list'>, ResponseOf<'condition-hits.list'>>('condition-hits.list', routes['condition-hits.list'], request, options),
}

/** 资源 `configs` 的操作。 */
export const configs = {
  create: (request: RequestOf<'configs.create'>, options?: CallOptions) => invoke<RequestOf<'configs.create'>, ResponseOf<'configs.create'>>('configs.create', routes['configs.create'], request, options),
  delete: (request: RequestOf<'configs.delete'>, options?: CallOptions) => invoke<RequestOf<'configs.delete'>, ResponseOf<'configs.delete'>>('configs.delete', routes['configs.delete'], request, options),
  get: (request: RequestOf<'configs.get'>, options?: CallOptions) => invoke<RequestOf<'configs.get'>, ResponseOf<'configs.get'>>('configs.get', routes['configs.get'], request, options),
  list: (request: RequestOf<'configs.list'>, options?: CallOptions) => invoke<RequestOf<'configs.list'>, ResponseOf<'configs.list'>>('configs.list', routes['configs.list'], request, options),
  update: (request: RequestOf<'configs.update'>, options?: CallOptions) => invoke<RequestOf<'configs.update'>, ResponseOf<'configs.update'>>('configs.update', routes['configs.update'], request, options),
}

/** 资源 `constraint-rules` 的操作。 */
export const constraintRules = {
  create: (request: RequestOf<'constraint-rules.create'>, options?: CallOptions) => invoke<RequestOf<'constraint-rules.create'>, ResponseOf<'constraint-rules.create'>>('constraint-rules.create', routes['constraint-rules.create'], request, options),
  delete: (request: RequestOf<'constraint-rules.delete'>, options?: CallOptions) => invoke<RequestOf<'constraint-rules.delete'>, ResponseOf<'constraint-rules.delete'>>('constraint-rules.delete', routes['constraint-rules.delete'], request, options),
  get: (request: RequestOf<'constraint-rules.get'>, options?: CallOptions) => invoke<RequestOf<'constraint-rules.get'>, ResponseOf<'constraint-rules.get'>>('constraint-rules.get', routes['constraint-rules.get'], request, options),
  list: (request: RequestOf<'constraint-rules.list'>, options?: CallOptions) => invoke<RequestOf<'constraint-rules.list'>, ResponseOf<'constraint-rules.list'>>('constraint-rules.list', routes['constraint-rules.list'], request, options),
  setStatus: (request: RequestOf<'constraint-rules.set-status'>, options?: CallOptions) => invoke<RequestOf<'constraint-rules.set-status'>, ResponseOf<'constraint-rules.set-status'>>('constraint-rules.set-status', routes['constraint-rules.set-status'], request, options),
  update: (request: RequestOf<'constraint-rules.update'>, options?: CallOptions) => invoke<RequestOf<'constraint-rules.update'>, ResponseOf<'constraint-rules.update'>>('constraint-rules.update', routes['constraint-rules.update'], request, options),
}

/** 资源 `delegations` 的操作。 */
export const delegations = {
  create: (request: RequestOf<'delegations.create'>, options?: CallOptions) => invoke<RequestOf<'delegations.create'>, ResponseOf<'delegations.create'>>('delegations.create', routes['delegations.create'], request, options),
  delete: (request: RequestOf<'delegations.delete'>, options?: CallOptions) => invoke<RequestOf<'delegations.delete'>, ResponseOf<'delegations.delete'>>('delegations.delete', routes['delegations.delete'], request, options),
  list: (request: RequestOf<'delegations.list'>, options?: CallOptions) => invoke<RequestOf<'delegations.list'>, ResponseOf<'delegations.list'>>('delegations.list', routes['delegations.list'], request, options),
  revoke: (request: RequestOf<'delegations.revoke'>, options?: CallOptions) => invoke<RequestOf<'delegations.revoke'>, ResponseOf<'delegations.revoke'>>('delegations.revoke', routes['delegations.revoke'], request, options),
}

/** 资源 `departments` 的操作。 */
export const departments = {
  create: (request: RequestOf<'departments.create'>, options?: CallOptions) => invoke<RequestOf<'departments.create'>, ResponseOf<'departments.create'>>('departments.create', routes['departments.create'], request, options),
  delete: (request: RequestOf<'departments.delete'>, options?: CallOptions) => invoke<RequestOf<'departments.delete'>, ResponseOf<'departments.delete'>>('departments.delete', routes['departments.delete'], request, options),
  get: (request: RequestOf<'departments.get'>, options?: CallOptions) => invoke<RequestOf<'departments.get'>, ResponseOf<'departments.get'>>('departments.get', routes['departments.get'], request, options),
  move: (request: RequestOf<'departments.move'>, options?: CallOptions) => invoke<RequestOf<'departments.move'>, ResponseOf<'departments.move'>>('departments.move', routes['departments.move'], request, options),
  tree: (request: RequestOf<'departments.tree'>, options?: CallOptions) => invoke<RequestOf<'departments.tree'>, ResponseOf<'departments.tree'>>('departments.tree', routes['departments.tree'], request, options),
  update: (request: RequestOf<'departments.update'>, options?: CallOptions) => invoke<RequestOf<'departments.update'>, ResponseOf<'departments.update'>>('departments.update', routes['departments.update'], request, options),
}

/** 资源 `dict-items` 的操作。 */
export const dictItems = {
  create: (request: RequestOf<'dict-items.create'>, options?: CallOptions) => invoke<RequestOf<'dict-items.create'>, ResponseOf<'dict-items.create'>>('dict-items.create', routes['dict-items.create'], request, options),
  delete: (request: RequestOf<'dict-items.delete'>, options?: CallOptions) => invoke<RequestOf<'dict-items.delete'>, ResponseOf<'dict-items.delete'>>('dict-items.delete', routes['dict-items.delete'], request, options),
  get: (request: RequestOf<'dict-items.get'>, options?: CallOptions) => invoke<RequestOf<'dict-items.get'>, ResponseOf<'dict-items.get'>>('dict-items.get', routes['dict-items.get'], request, options),
  list: (request: RequestOf<'dict-items.list'>, options?: CallOptions) => invoke<RequestOf<'dict-items.list'>, ResponseOf<'dict-items.list'>>('dict-items.list', routes['dict-items.list'], request, options),
  update: (request: RequestOf<'dict-items.update'>, options?: CallOptions) => invoke<RequestOf<'dict-items.update'>, ResponseOf<'dict-items.update'>>('dict-items.update', routes['dict-items.update'], request, options),
}

/** 资源 `dicts` 的操作。 */
export const dicts = {
  create: (request: RequestOf<'dicts.create'>, options?: CallOptions) => invoke<RequestOf<'dicts.create'>, ResponseOf<'dicts.create'>>('dicts.create', routes['dicts.create'], request, options),
  delete: (request: RequestOf<'dicts.delete'>, options?: CallOptions) => invoke<RequestOf<'dicts.delete'>, ResponseOf<'dicts.delete'>>('dicts.delete', routes['dicts.delete'], request, options),
  get: (request: RequestOf<'dicts.get'>, options?: CallOptions) => invoke<RequestOf<'dicts.get'>, ResponseOf<'dicts.get'>>('dicts.get', routes['dicts.get'], request, options),
  list: (request: RequestOf<'dicts.list'>, options?: CallOptions) => invoke<RequestOf<'dicts.list'>, ResponseOf<'dicts.list'>>('dicts.list', routes['dicts.list'], request, options),
  options: (request: RequestOf<'dicts.options'>, options?: CallOptions) => invoke<RequestOf<'dicts.options'>, ResponseOf<'dicts.options'>>('dicts.options', routes['dicts.options'], request, options),
  update: (request: RequestOf<'dicts.update'>, options?: CallOptions) => invoke<RequestOf<'dicts.update'>, ResponseOf<'dicts.update'>>('dicts.update', routes['dicts.update'], request, options),
}

/** 资源 `diff-logs` 的操作。 */
export const diffLogs = {
  get: (request: RequestOf<'diff-logs.get'>, options?: CallOptions) => invoke<RequestOf<'diff-logs.get'>, ResponseOf<'diff-logs.get'>>('diff-logs.get', routes['diff-logs.get'], request, options),
  list: (request: RequestOf<'diff-logs.list'>, options?: CallOptions) => invoke<RequestOf<'diff-logs.list'>, ResponseOf<'diff-logs.list'>>('diff-logs.list', routes['diff-logs.list'], request, options),
}

/** 资源 `editions` 的操作。 */
export const editions = {
  create: (request: RequestOf<'editions.create'>, options?: CallOptions) => invoke<RequestOf<'editions.create'>, ResponseOf<'editions.create'>>('editions.create', routes['editions.create'], request, options),
  delete: (request: RequestOf<'editions.delete'>, options?: CallOptions) => invoke<RequestOf<'editions.delete'>, ResponseOf<'editions.delete'>>('editions.delete', routes['editions.delete'], request, options),
  get: (request: RequestOf<'editions.get'>, options?: CallOptions) => invoke<RequestOf<'editions.get'>, ResponseOf<'editions.get'>>('editions.get', routes['editions.get'], request, options),
  list: (request: RequestOf<'editions.list'>, options?: CallOptions) => invoke<RequestOf<'editions.list'>, ResponseOf<'editions.list'>>('editions.list', routes['editions.list'], request, options),
  setPermissions: (request: RequestOf<'editions.set-permissions'>, options?: CallOptions) => invoke<RequestOf<'editions.set-permissions'>, ResponseOf<'editions.set-permissions'>>('editions.set-permissions', routes['editions.set-permissions'], request, options),
  update: (request: RequestOf<'editions.update'>, options?: CallOptions) => invoke<RequestOf<'editions.update'>, ResponseOf<'editions.update'>>('editions.update', routes['editions.update'], request, options),
}

/** 资源 `email-configs` 的操作。 */
export const emailConfigs = {
  create: (request: RequestOf<'email-configs.create'>, options?: CallOptions) => invoke<RequestOf<'email-configs.create'>, ResponseOf<'email-configs.create'>>('email-configs.create', routes['email-configs.create'], request, options),
  delete: (request: RequestOf<'email-configs.delete'>, options?: CallOptions) => invoke<RequestOf<'email-configs.delete'>, ResponseOf<'email-configs.delete'>>('email-configs.delete', routes['email-configs.delete'], request, options),
  get: (request: RequestOf<'email-configs.get'>, options?: CallOptions) => invoke<RequestOf<'email-configs.get'>, ResponseOf<'email-configs.get'>>('email-configs.get', routes['email-configs.get'], request, options),
  list: (request: RequestOf<'email-configs.list'>, options?: CallOptions) => invoke<RequestOf<'email-configs.list'>, ResponseOf<'email-configs.list'>>('email-configs.list', routes['email-configs.list'], request, options),
  setDefault: (request: RequestOf<'email-configs.set-default'>, options?: CallOptions) => invoke<RequestOf<'email-configs.set-default'>, ResponseOf<'email-configs.set-default'>>('email-configs.set-default', routes['email-configs.set-default'], request, options),
  update: (request: RequestOf<'email-configs.update'>, options?: CallOptions) => invoke<RequestOf<'email-configs.update'>, ResponseOf<'email-configs.update'>>('email-configs.update', routes['email-configs.update'], request, options),
}

/** 资源 `emails` 的操作。 */
export const emails = {
  cancel: (request: RequestOf<'emails.cancel'>, options?: CallOptions) => invoke<RequestOf<'emails.cancel'>, ResponseOf<'emails.cancel'>>('emails.cancel', routes['emails.cancel'], request, options),
  delete: (request: RequestOf<'emails.delete'>, options?: CallOptions) => invoke<RequestOf<'emails.delete'>, ResponseOf<'emails.delete'>>('emails.delete', routes['emails.delete'], request, options),
  get: (request: RequestOf<'emails.get'>, options?: CallOptions) => invoke<RequestOf<'emails.get'>, ResponseOf<'emails.get'>>('emails.get', routes['emails.get'], request, options),
  list: (request: RequestOf<'emails.list'>, options?: CallOptions) => invoke<RequestOf<'emails.list'>, ResponseOf<'emails.list'>>('emails.list', routes['emails.list'], request, options),
  resend: (request: RequestOf<'emails.resend'>, options?: CallOptions) => invoke<RequestOf<'emails.resend'>, ResponseOf<'emails.resend'>>('emails.resend', routes['emails.resend'], request, options),
}

/** 资源 `exception-logs` 的操作。 */
export const exceptionLogs = {
  get: (request: RequestOf<'exception-logs.get'>, options?: CallOptions) => invoke<RequestOf<'exception-logs.get'>, ResponseOf<'exception-logs.get'>>('exception-logs.get', routes['exception-logs.get'], request, options),
  list: (request: RequestOf<'exception-logs.list'>, options?: CallOptions) => invoke<RequestOf<'exception-logs.list'>, ResponseOf<'exception-logs.list'>>('exception-logs.list', routes['exception-logs.list'], request, options),
}

/** 资源 `export-tasks` 的操作。 */
export const exportTasks = {
  cancel: (request: RequestOf<'export-tasks.cancel'>, options?: CallOptions) => invoke<RequestOf<'export-tasks.cancel'>, ResponseOf<'export-tasks.cancel'>>('export-tasks.cancel', routes['export-tasks.cancel'], request, options),
  delete: (request: RequestOf<'export-tasks.delete'>, options?: CallOptions) => invoke<RequestOf<'export-tasks.delete'>, ResponseOf<'export-tasks.delete'>>('export-tasks.delete', routes['export-tasks.delete'], request, options),
  get: (request: RequestOf<'export-tasks.get'>, options?: CallOptions) => invoke<RequestOf<'export-tasks.get'>, ResponseOf<'export-tasks.get'>>('export-tasks.get', routes['export-tasks.get'], request, options),
  list: (request: RequestOf<'export-tasks.list'>, options?: CallOptions) => invoke<RequestOf<'export-tasks.list'>, ResponseOf<'export-tasks.list'>>('export-tasks.list', routes['export-tasks.list'], request, options),
  submit: (request: RequestOf<'export-tasks.submit'>, options?: CallOptions) => invoke<RequestOf<'export-tasks.submit'>, ResponseOf<'export-tasks.submit'>>('export-tasks.submit', routes['export-tasks.submit'], request, options),
}

/** 资源 `field-security` 的操作。 */
export const fieldSecurity = {
  create: (request: RequestOf<'field-security.create'>, options?: CallOptions) => invoke<RequestOf<'field-security.create'>, ResponseOf<'field-security.create'>>('field-security.create', routes['field-security.create'], request, options),
  delete: (request: RequestOf<'field-security.delete'>, options?: CallOptions) => invoke<RequestOf<'field-security.delete'>, ResponseOf<'field-security.delete'>>('field-security.delete', routes['field-security.delete'], request, options),
  entities: (request: RequestOf<'field-security.entities'>, options?: CallOptions) => invoke<RequestOf<'field-security.entities'>, ResponseOf<'field-security.entities'>>('field-security.entities', routes['field-security.entities'], request, options),
  get: (request: RequestOf<'field-security.get'>, options?: CallOptions) => invoke<RequestOf<'field-security.get'>, ResponseOf<'field-security.get'>>('field-security.get', routes['field-security.get'], request, options),
  list: (request: RequestOf<'field-security.list'>, options?: CallOptions) => invoke<RequestOf<'field-security.list'>, ResponseOf<'field-security.list'>>('field-security.list', routes['field-security.list'], request, options),
  update: (request: RequestOf<'field-security.update'>, options?: CallOptions) => invoke<RequestOf<'field-security.update'>, ResponseOf<'field-security.update'>>('field-security.update', routes['field-security.update'], request, options),
}

/** 资源 `files` 的操作。 */
export const files = {
  abortUpload: (request: RequestOf<'files.abort-upload'>, options?: CallOptions) => invoke<RequestOf<'files.abort-upload'>, ResponseOf<'files.abort-upload'>>('files.abort-upload', routes['files.abort-upload'], request, options),
  completeUpload: (request: RequestOf<'files.complete-upload'>, options?: CallOptions) => invoke<RequestOf<'files.complete-upload'>, ResponseOf<'files.complete-upload'>>('files.complete-upload', routes['files.complete-upload'], request, options),
  delete: (request: RequestOf<'files.delete'>, options?: CallOptions) => invoke<RequestOf<'files.delete'>, ResponseOf<'files.delete'>>('files.delete', routes['files.delete'], request, options),
  fastUpload: (request: RequestOf<'files.fast-upload'>, options?: CallOptions) => invoke<RequestOf<'files.fast-upload'>, ResponseOf<'files.fast-upload'>>('files.fast-upload', routes['files.fast-upload'], request, options),
  get: (request: RequestOf<'files.get'>, options?: CallOptions) => invoke<RequestOf<'files.get'>, ResponseOf<'files.get'>>('files.get', routes['files.get'], request, options),
  list: (request: RequestOf<'files.list'>, options?: CallOptions) => invoke<RequestOf<'files.list'>, ResponseOf<'files.list'>>('files.list', routes['files.list'], request, options),
  presign: (request: RequestOf<'files.presign'>, options?: CallOptions) => invoke<RequestOf<'files.presign'>, ResponseOf<'files.presign'>>('files.presign', routes['files.presign'], request, options),
  setPrimaryStorage: (request: RequestOf<'files.set-primary-storage'>, options?: CallOptions) => invoke<RequestOf<'files.set-primary-storage'>, ResponseOf<'files.set-primary-storage'>>('files.set-primary-storage', routes['files.set-primary-storage'], request, options),
  setStatus: (request: RequestOf<'files.set-status'>, options?: CallOptions) => invoke<RequestOf<'files.set-status'>, ResponseOf<'files.set-status'>>('files.set-status', routes['files.set-status'], request, options),
  setStorageStatus: (request: RequestOf<'files.set-storage-status'>, options?: CallOptions) => invoke<RequestOf<'files.set-storage-status'>, ResponseOf<'files.set-storage-status'>>('files.set-storage-status', routes['files.set-storage-status'], request, options),
  signUploadParts: (request: RequestOf<'files.sign-upload-parts'>, options?: CallOptions) => invoke<RequestOf<'files.sign-upload-parts'>, ResponseOf<'files.sign-upload-parts'>>('files.sign-upload-parts', routes['files.sign-upload-parts'], request, options),
  startUpload: (request: RequestOf<'files.start-upload'>, options?: CallOptions) => invoke<RequestOf<'files.start-upload'>, ResponseOf<'files.start-upload'>>('files.start-upload', routes['files.start-upload'], request, options),
  storages: (request: RequestOf<'files.storages'>, options?: CallOptions) => invoke<RequestOf<'files.storages'>, ResponseOf<'files.storages'>>('files.storages', routes['files.storages'], request, options),
  update: (request: RequestOf<'files.update'>, options?: CallOptions) => invoke<RequestOf<'files.update'>, ResponseOf<'files.update'>>('files.update', routes['files.update'], request, options),
  uploadSession: (request: RequestOf<'files.upload-session'>, options?: CallOptions) => invoke<RequestOf<'files.upload-session'>, ResponseOf<'files.upload-session'>>('files.upload-session', routes['files.upload-session'], request, options),
  verifyStorage: (request: RequestOf<'files.verify-storage'>, options?: CallOptions) => invoke<RequestOf<'files.verify-storage'>, ResponseOf<'files.verify-storage'>>('files.verify-storage', routes['files.verify-storage'], request, options),
}

/** 资源 `impersonation` 的操作。 */
export const impersonation = {
  candidates: (request: RequestOf<'impersonation.candidates'>, options?: CallOptions) => invoke<RequestOf<'impersonation.candidates'>, ResponseOf<'impersonation.candidates'>>('impersonation.candidates', routes['impersonation.candidates'], request, options),
  start: (request: RequestOf<'impersonation.start'>, options?: CallOptions) => invoke<RequestOf<'impersonation.start'>, ResponseOf<'impersonation.start'>>('impersonation.start', routes['impersonation.start'], request, options),
  stop: (request: RequestOf<'impersonation.stop'>, options?: CallOptions) => invoke<RequestOf<'impersonation.stop'>, ResponseOf<'impersonation.stop'>>('impersonation.stop', routes['impersonation.stop'], request, options),
}

/** 资源 `import-histories` 的操作。 */
export const importHistories = {
  create: (request: RequestOf<'import-histories.create'>, options?: CallOptions) => invoke<RequestOf<'import-histories.create'>, ResponseOf<'import-histories.create'>>('import-histories.create', routes['import-histories.create'], request, options),
  recent: (request: RequestOf<'import-histories.recent'>, options?: CallOptions) => invoke<RequestOf<'import-histories.recent'>, ResponseOf<'import-histories.recent'>>('import-histories.recent', routes['import-histories.recent'], request, options),
}

/** 资源 `inbox` 的操作。 */
export const inbox = {
  banners: (request: RequestOf<'inbox.banners'>, options?: CallOptions) => invoke<RequestOf<'inbox.banners'>, ResponseOf<'inbox.banners'>>('inbox.banners', routes['inbox.banners'], request, options),
  confirm: (request: RequestOf<'inbox.confirm'>, options?: CallOptions) => invoke<RequestOf<'inbox.confirm'>, ResponseOf<'inbox.confirm'>>('inbox.confirm', routes['inbox.confirm'], request, options),
  delete: (request: RequestOf<'inbox.delete'>, options?: CallOptions) => invoke<RequestOf<'inbox.delete'>, ResponseOf<'inbox.delete'>>('inbox.delete', routes['inbox.delete'], request, options),
  list: (request: RequestOf<'inbox.list'>, options?: CallOptions) => invoke<RequestOf<'inbox.list'>, ResponseOf<'inbox.list'>>('inbox.list', routes['inbox.list'], request, options),
  mandatory: (request: RequestOf<'inbox.mandatory'>, options?: CallOptions) => invoke<RequestOf<'inbox.mandatory'>, ResponseOf<'inbox.mandatory'>>('inbox.mandatory', routes['inbox.mandatory'], request, options),
  markAllRead: (request: RequestOf<'inbox.mark-all-read'>, options?: CallOptions) => invoke<RequestOf<'inbox.mark-all-read'>, ResponseOf<'inbox.mark-all-read'>>('inbox.mark-all-read', routes['inbox.mark-all-read'], request, options),
  markPopupShown: (request: RequestOf<'inbox.mark-popup-shown'>, options?: CallOptions) => invoke<RequestOf<'inbox.mark-popup-shown'>, ResponseOf<'inbox.mark-popup-shown'>>('inbox.mark-popup-shown', routes['inbox.mark-popup-shown'], request, options),
  markRead: (request: RequestOf<'inbox.mark-read'>, options?: CallOptions) => invoke<RequestOf<'inbox.mark-read'>, ResponseOf<'inbox.mark-read'>>('inbox.mark-read', routes['inbox.mark-read'], request, options),
  popups: (request: RequestOf<'inbox.popups'>, options?: CallOptions) => invoke<RequestOf<'inbox.popups'>, ResponseOf<'inbox.popups'>>('inbox.popups', routes['inbox.popups'], request, options),
}

/** 资源 `job-runs` 的操作。 */
export const jobRuns = {
  list: (request: RequestOf<'job-runs.list'>, options?: CallOptions) => invoke<RequestOf<'job-runs.list'>, ResponseOf<'job-runs.list'>>('job-runs.list', routes['job-runs.list'], request, options),
}

/** 资源 `jobs` 的操作。 */
export const jobs = {
  get: (request: RequestOf<'jobs.get'>, options?: CallOptions) => invoke<RequestOf<'jobs.get'>, ResponseOf<'jobs.get'>>('jobs.get', routes['jobs.get'], request, options),
  list: (request: RequestOf<'jobs.list'>, options?: CallOptions) => invoke<RequestOf<'jobs.list'>, ResponseOf<'jobs.list'>>('jobs.list', routes['jobs.list'], request, options),
  pause: (request: RequestOf<'jobs.pause'>, options?: CallOptions) => invoke<RequestOf<'jobs.pause'>, ResponseOf<'jobs.pause'>>('jobs.pause', routes['jobs.pause'], request, options),
  reschedule: (request: RequestOf<'jobs.reschedule'>, options?: CallOptions) => invoke<RequestOf<'jobs.reschedule'>, ResponseOf<'jobs.reschedule'>>('jobs.reschedule', routes['jobs.reschedule'], request, options),
  restore: (request: RequestOf<'jobs.restore'>, options?: CallOptions) => invoke<RequestOf<'jobs.restore'>, ResponseOf<'jobs.restore'>>('jobs.restore', routes['jobs.restore'], request, options),
  resume: (request: RequestOf<'jobs.resume'>, options?: CallOptions) => invoke<RequestOf<'jobs.resume'>, ResponseOf<'jobs.resume'>>('jobs.resume', routes['jobs.resume'], request, options),
  trigger: (request: RequestOf<'jobs.trigger'>, options?: CallOptions) => invoke<RequestOf<'jobs.trigger'>, ResponseOf<'jobs.trigger'>>('jobs.trigger', routes['jobs.trigger'], request, options),
  types: (request: RequestOf<'jobs.types'>, options?: CallOptions) => invoke<RequestOf<'jobs.types'>, ResponseOf<'jobs.types'>>('jobs.types', routes['jobs.types'], request, options),
  validateSchedule: (request: RequestOf<'jobs.validate-schedule'>, options?: CallOptions) => invoke<RequestOf<'jobs.validate-schedule'>, ResponseOf<'jobs.validate-schedule'>>('jobs.validate-schedule', routes['jobs.validate-schedule'], request, options),
}

/** 资源 `login-bans` 的操作。 */
export const loginBans = {
  lift: (request: RequestOf<'login-bans.lift'>, options?: CallOptions) => invoke<RequestOf<'login-bans.lift'>, ResponseOf<'login-bans.lift'>>('login-bans.lift', routes['login-bans.lift'], request, options),
  list: (request: RequestOf<'login-bans.list'>, options?: CallOptions) => invoke<RequestOf<'login-bans.list'>, ResponseOf<'login-bans.list'>>('login-bans.list', routes['login-bans.list'], request, options),
}

/** 资源 `login-logs` 的操作。 */
export const loginLogs = {
  get: (request: RequestOf<'login-logs.get'>, options?: CallOptions) => invoke<RequestOf<'login-logs.get'>, ResponseOf<'login-logs.get'>>('login-logs.get', routes['login-logs.get'], request, options),
  list: (request: RequestOf<'login-logs.list'>, options?: CallOptions) => invoke<RequestOf<'login-logs.list'>, ResponseOf<'login-logs.list'>>('login-logs.list', routes['login-logs.list'], request, options),
}

/** 资源 `memberships` 的操作。 */
export const memberships = {
  create: (request: RequestOf<'memberships.create'>, options?: CallOptions) => invoke<RequestOf<'memberships.create'>, ResponseOf<'memberships.create'>>('memberships.create', routes['memberships.create'], request, options),
  delete: (request: RequestOf<'memberships.delete'>, options?: CallOptions) => invoke<RequestOf<'memberships.delete'>, ResponseOf<'memberships.delete'>>('memberships.delete', routes['memberships.delete'], request, options),
  list: (request: RequestOf<'memberships.list'>, options?: CallOptions) => invoke<RequestOf<'memberships.list'>, ResponseOf<'memberships.list'>>('memberships.list', routes['memberships.list'], request, options),
  update: (request: RequestOf<'memberships.update'>, options?: CallOptions) => invoke<RequestOf<'memberships.update'>, ResponseOf<'memberships.update'>>('memberships.update', routes['memberships.update'], request, options),
}

/** 资源 `menus` 的操作。 */
export const menus = {
  create: (request: RequestOf<'menus.create'>, options?: CallOptions) => invoke<RequestOf<'menus.create'>, ResponseOf<'menus.create'>>('menus.create', routes['menus.create'], request, options),
  delete: (request: RequestOf<'menus.delete'>, options?: CallOptions) => invoke<RequestOf<'menus.delete'>, ResponseOf<'menus.delete'>>('menus.delete', routes['menus.delete'], request, options),
  get: (request: RequestOf<'menus.get'>, options?: CallOptions) => invoke<RequestOf<'menus.get'>, ResponseOf<'menus.get'>>('menus.get', routes['menus.get'], request, options),
  list: (request: RequestOf<'menus.list'>, options?: CallOptions) => invoke<RequestOf<'menus.list'>, ResponseOf<'menus.list'>>('menus.list', routes['menus.list'], request, options),
  reset: (request: RequestOf<'menus.reset'>, options?: CallOptions) => invoke<RequestOf<'menus.reset'>, ResponseOf<'menus.reset'>>('menus.reset', routes['menus.reset'], request, options),
  update: (request: RequestOf<'menus.update'>, options?: CallOptions) => invoke<RequestOf<'menus.update'>, ResponseOf<'menus.update'>>('menus.update', routes['menus.update'], request, options),
}

/** 资源 `message-templates` 的操作。 */
export const messageTemplates = {
  create: (request: RequestOf<'message-templates.create'>, options?: CallOptions) => invoke<RequestOf<'message-templates.create'>, ResponseOf<'message-templates.create'>>('message-templates.create', routes['message-templates.create'], request, options),
  delete: (request: RequestOf<'message-templates.delete'>, options?: CallOptions) => invoke<RequestOf<'message-templates.delete'>, ResponseOf<'message-templates.delete'>>('message-templates.delete', routes['message-templates.delete'], request, options),
  get: (request: RequestOf<'message-templates.get'>, options?: CallOptions) => invoke<RequestOf<'message-templates.get'>, ResponseOf<'message-templates.get'>>('message-templates.get', routes['message-templates.get'], request, options),
  list: (request: RequestOf<'message-templates.list'>, options?: CallOptions) => invoke<RequestOf<'message-templates.list'>, ResponseOf<'message-templates.list'>>('message-templates.list', routes['message-templates.list'], request, options),
  update: (request: RequestOf<'message-templates.update'>, options?: CallOptions) => invoke<RequestOf<'message-templates.update'>, ResponseOf<'message-templates.update'>>('message-templates.update', routes['message-templates.update'], request, options),
}

/** 资源 `migrations` 的操作。 */
export const migrations = {
  get: (request: RequestOf<'migrations.get'>, options?: CallOptions) => invoke<RequestOf<'migrations.get'>, ResponseOf<'migrations.get'>>('migrations.get', routes['migrations.get'], request, options),
  list: (request: RequestOf<'migrations.list'>, options?: CallOptions) => invoke<RequestOf<'migrations.list'>, ResponseOf<'migrations.list'>>('migrations.list', routes['migrations.list'], request, options),
}

/** 资源 `my-activity` 的操作。 */
export const myActivity = {
  summary: (request: RequestOf<'my-activity.summary'>, options?: CallOptions) => invoke<RequestOf<'my-activity.summary'>, ResponseOf<'my-activity.summary'>>('my-activity.summary', routes['my-activity.summary'], request, options),
}

/** 资源 `my-api-credentials` 的操作。 */
export const myApiCredentials = {
  create: (request: RequestOf<'my-api-credentials.create'>, options?: CallOptions) => invoke<RequestOf<'my-api-credentials.create'>, ResponseOf<'my-api-credentials.create'>>('my-api-credentials.create', routes['my-api-credentials.create'], request, options),
  delete: (request: RequestOf<'my-api-credentials.delete'>, options?: CallOptions) => invoke<RequestOf<'my-api-credentials.delete'>, ResponseOf<'my-api-credentials.delete'>>('my-api-credentials.delete', routes['my-api-credentials.delete'], request, options),
  list: (request: RequestOf<'my-api-credentials.list'>, options?: CallOptions) => invoke<RequestOf<'my-api-credentials.list'>, ResponseOf<'my-api-credentials.list'>>('my-api-credentials.list', routes['my-api-credentials.list'], request, options),
  rotateSecret: (request: RequestOf<'my-api-credentials.rotate-secret'>, options?: CallOptions) => invoke<RequestOf<'my-api-credentials.rotate-secret'>, ResponseOf<'my-api-credentials.rotate-secret'>>('my-api-credentials.rotate-secret', routes['my-api-credentials.rotate-secret'], request, options),
  settings: (request: RequestOf<'my-api-credentials.settings'>, options?: CallOptions) => invoke<RequestOf<'my-api-credentials.settings'>, ResponseOf<'my-api-credentials.settings'>>('my-api-credentials.settings', routes['my-api-credentials.settings'], request, options),
  update: (request: RequestOf<'my-api-credentials.update'>, options?: CallOptions) => invoke<RequestOf<'my-api-credentials.update'>, ResponseOf<'my-api-credentials.update'>>('my-api-credentials.update', routes['my-api-credentials.update'], request, options),
  updateSettings: (request: RequestOf<'my-api-credentials.update-settings'>, options?: CallOptions) => invoke<RequestOf<'my-api-credentials.update-settings'>, ResponseOf<'my-api-credentials.update-settings'>>('my-api-credentials.update-settings', routes['my-api-credentials.update-settings'], request, options),
}

/** 资源 `my-login-logs` 的操作。 */
export const myLoginLogs = {
  list: (request: RequestOf<'my-login-logs.list'>, options?: CallOptions) => invoke<RequestOf<'my-login-logs.list'>, ResponseOf<'my-login-logs.list'>>('my-login-logs.list', routes['my-login-logs.list'], request, options),
}

/** 资源 `my-oauth-apps` 的操作。 */
export const myOauthApps = {
  create: (request: RequestOf<'my-oauth-apps.create'>, options?: CallOptions) => invoke<RequestOf<'my-oauth-apps.create'>, ResponseOf<'my-oauth-apps.create'>>('my-oauth-apps.create', routes['my-oauth-apps.create'], request, options),
  delete: (request: RequestOf<'my-oauth-apps.delete'>, options?: CallOptions) => invoke<RequestOf<'my-oauth-apps.delete'>, ResponseOf<'my-oauth-apps.delete'>>('my-oauth-apps.delete', routes['my-oauth-apps.delete'], request, options),
  get: (request: RequestOf<'my-oauth-apps.get'>, options?: CallOptions) => invoke<RequestOf<'my-oauth-apps.get'>, ResponseOf<'my-oauth-apps.get'>>('my-oauth-apps.get', routes['my-oauth-apps.get'], request, options),
  list: (request: RequestOf<'my-oauth-apps.list'>, options?: CallOptions) => invoke<RequestOf<'my-oauth-apps.list'>, ResponseOf<'my-oauth-apps.list'>>('my-oauth-apps.list', routes['my-oauth-apps.list'], request, options),
  resetSecret: (request: RequestOf<'my-oauth-apps.reset-secret'>, options?: CallOptions) => invoke<RequestOf<'my-oauth-apps.reset-secret'>, ResponseOf<'my-oauth-apps.reset-secret'>>('my-oauth-apps.reset-secret', routes['my-oauth-apps.reset-secret'], request, options),
  update: (request: RequestOf<'my-oauth-apps.update'>, options?: CallOptions) => invoke<RequestOf<'my-oauth-apps.update'>, ResponseOf<'my-oauth-apps.update'>>('my-oauth-apps.update', routes['my-oauth-apps.update'], request, options),
}

/** 资源 `notifications` 的操作。 */
export const notifications = {
  create: (request: RequestOf<'notifications.create'>, options?: CallOptions) => invoke<RequestOf<'notifications.create'>, ResponseOf<'notifications.create'>>('notifications.create', routes['notifications.create'], request, options),
  delete: (request: RequestOf<'notifications.delete'>, options?: CallOptions) => invoke<RequestOf<'notifications.delete'>, ResponseOf<'notifications.delete'>>('notifications.delete', routes['notifications.delete'], request, options),
  get: (request: RequestOf<'notifications.get'>, options?: CallOptions) => invoke<RequestOf<'notifications.get'>, ResponseOf<'notifications.get'>>('notifications.get', routes['notifications.get'], request, options),
  list: (request: RequestOf<'notifications.list'>, options?: CallOptions) => invoke<RequestOf<'notifications.list'>, ResponseOf<'notifications.list'>>('notifications.list', routes['notifications.list'], request, options),
  publish: (request: RequestOf<'notifications.publish'>, options?: CallOptions) => invoke<RequestOf<'notifications.publish'>, ResponseOf<'notifications.publish'>>('notifications.publish', routes['notifications.publish'], request, options),
  readStats: (request: RequestOf<'notifications.read-stats'>, options?: CallOptions) => invoke<RequestOf<'notifications.read-stats'>, ResponseOf<'notifications.read-stats'>>('notifications.read-stats', routes['notifications.read-stats'], request, options),
  recipients: (request: RequestOf<'notifications.recipients'>, options?: CallOptions) => invoke<RequestOf<'notifications.recipients'>, ResponseOf<'notifications.recipients'>>('notifications.recipients', routes['notifications.recipients'], request, options),
  remind: (request: RequestOf<'notifications.remind'>, options?: CallOptions) => invoke<RequestOf<'notifications.remind'>, ResponseOf<'notifications.remind'>>('notifications.remind', routes['notifications.remind'], request, options),
  update: (request: RequestOf<'notifications.update'>, options?: CallOptions) => invoke<RequestOf<'notifications.update'>, ResponseOf<'notifications.update'>>('notifications.update', routes['notifications.update'], request, options),
}

/** 资源 `numbering-rules` 的操作。 */
export const numberingRules = {
  allocations: (request: RequestOf<'numbering-rules.allocations'>, options?: CallOptions) => invoke<RequestOf<'numbering-rules.allocations'>, ResponseOf<'numbering-rules.allocations'>>('numbering-rules.allocations', routes['numbering-rules.allocations'], request, options),
  create: (request: RequestOf<'numbering-rules.create'>, options?: CallOptions) => invoke<RequestOf<'numbering-rules.create'>, ResponseOf<'numbering-rules.create'>>('numbering-rules.create', routes['numbering-rules.create'], request, options),
  delete: (request: RequestOf<'numbering-rules.delete'>, options?: CallOptions) => invoke<RequestOf<'numbering-rules.delete'>, ResponseOf<'numbering-rules.delete'>>('numbering-rules.delete', routes['numbering-rules.delete'], request, options),
  generate: (request: RequestOf<'numbering-rules.generate'>, options?: CallOptions) => invoke<RequestOf<'numbering-rules.generate'>, ResponseOf<'numbering-rules.generate'>>('numbering-rules.generate', routes['numbering-rules.generate'], request, options),
  get: (request: RequestOf<'numbering-rules.get'>, options?: CallOptions) => invoke<RequestOf<'numbering-rules.get'>, ResponseOf<'numbering-rules.get'>>('numbering-rules.get', routes['numbering-rules.get'], request, options),
  list: (request: RequestOf<'numbering-rules.list'>, options?: CallOptions) => invoke<RequestOf<'numbering-rules.list'>, ResponseOf<'numbering-rules.list'>>('numbering-rules.list', routes['numbering-rules.list'], request, options),
  preview: (request: RequestOf<'numbering-rules.preview'>, options?: CallOptions) => invoke<RequestOf<'numbering-rules.preview'>, ResponseOf<'numbering-rules.preview'>>('numbering-rules.preview', routes['numbering-rules.preview'], request, options),
  reset: (request: RequestOf<'numbering-rules.reset'>, options?: CallOptions) => invoke<RequestOf<'numbering-rules.reset'>, ResponseOf<'numbering-rules.reset'>>('numbering-rules.reset', routes['numbering-rules.reset'], request, options),
  update: (request: RequestOf<'numbering-rules.update'>, options?: CallOptions) => invoke<RequestOf<'numbering-rules.update'>, ResponseOf<'numbering-rules.update'>>('numbering-rules.update', routes['numbering-rules.update'], request, options),
}

/** 资源 `oauth-apps` 的操作。 */
export const oauthApps = {
  create: (request: RequestOf<'oauth-apps.create'>, options?: CallOptions) => invoke<RequestOf<'oauth-apps.create'>, ResponseOf<'oauth-apps.create'>>('oauth-apps.create', routes['oauth-apps.create'], request, options),
  delete: (request: RequestOf<'oauth-apps.delete'>, options?: CallOptions) => invoke<RequestOf<'oauth-apps.delete'>, ResponseOf<'oauth-apps.delete'>>('oauth-apps.delete', routes['oauth-apps.delete'], request, options),
  get: (request: RequestOf<'oauth-apps.get'>, options?: CallOptions) => invoke<RequestOf<'oauth-apps.get'>, ResponseOf<'oauth-apps.get'>>('oauth-apps.get', routes['oauth-apps.get'], request, options),
  list: (request: RequestOf<'oauth-apps.list'>, options?: CallOptions) => invoke<RequestOf<'oauth-apps.list'>, ResponseOf<'oauth-apps.list'>>('oauth-apps.list', routes['oauth-apps.list'], request, options),
  resetSecret: (request: RequestOf<'oauth-apps.reset-secret'>, options?: CallOptions) => invoke<RequestOf<'oauth-apps.reset-secret'>, ResponseOf<'oauth-apps.reset-secret'>>('oauth-apps.reset-secret', routes['oauth-apps.reset-secret'], request, options),
  update: (request: RequestOf<'oauth-apps.update'>, options?: CallOptions) => invoke<RequestOf<'oauth-apps.update'>, ResponseOf<'oauth-apps.update'>>('oauth-apps.update', routes['oauth-apps.update'], request, options),
}

/** 资源 `oauth-consents` 的操作。 */
export const oauthConsents = {
  approve: (request: RequestOf<'oauth-consents.approve'>, options?: CallOptions) => invoke<RequestOf<'oauth-consents.approve'>, ResponseOf<'oauth-consents.approve'>>('oauth-consents.approve', routes['oauth-consents.approve'], request, options),
  deny: (request: RequestOf<'oauth-consents.deny'>, options?: CallOptions) => invoke<RequestOf<'oauth-consents.deny'>, ResponseOf<'oauth-consents.deny'>>('oauth-consents.deny', routes['oauth-consents.deny'], request, options),
  list: (request: RequestOf<'oauth-consents.list'>, options?: CallOptions) => invoke<RequestOf<'oauth-consents.list'>, ResponseOf<'oauth-consents.list'>>('oauth-consents.list', routes['oauth-consents.list'], request, options),
  resolve: (request: RequestOf<'oauth-consents.resolve'>, options?: CallOptions) => invoke<RequestOf<'oauth-consents.resolve'>, ResponseOf<'oauth-consents.resolve'>>('oauth-consents.resolve', routes['oauth-consents.resolve'], request, options),
  revoke: (request: RequestOf<'oauth-consents.revoke'>, options?: CallOptions) => invoke<RequestOf<'oauth-consents.revoke'>, ResponseOf<'oauth-consents.revoke'>>('oauth-consents.revoke', routes['oauth-consents.revoke'], request, options),
}

/** 资源 `open-api-logs` 的操作。 */
export const openApiLogs = {
  get: (request: RequestOf<'open-api-logs.get'>, options?: CallOptions) => invoke<RequestOf<'open-api-logs.get'>, ResponseOf<'open-api-logs.get'>>('open-api-logs.get', routes['open-api-logs.get'], request, options),
  list: (request: RequestOf<'open-api-logs.list'>, options?: CallOptions) => invoke<RequestOf<'open-api-logs.list'>, ResponseOf<'open-api-logs.list'>>('open-api-logs.list', routes['open-api-logs.list'], request, options),
}

/** 资源 `open` 的操作。 */
export const open = {
  echo: (request: RequestOf<'open.echo'>, options?: CallOptions) => invoke<RequestOf<'open.echo'>, ResponseOf<'open.echo'>>('open.echo', routes['open.echo'], request, options),
  ping: (request: RequestOf<'open.ping'>, options?: CallOptions) => invoke<RequestOf<'open.ping'>, ResponseOf<'open.ping'>>('open.ping', routes['open.ping'], request, options),
}

/** 资源 `operation-logs` 的操作。 */
export const operationLogs = {
  get: (request: RequestOf<'operation-logs.get'>, options?: CallOptions) => invoke<RequestOf<'operation-logs.get'>, ResponseOf<'operation-logs.get'>>('operation-logs.get', routes['operation-logs.get'], request, options),
  list: (request: RequestOf<'operation-logs.list'>, options?: CallOptions) => invoke<RequestOf<'operation-logs.list'>, ResponseOf<'operation-logs.list'>>('operation-logs.list', routes['operation-logs.list'], request, options),
}

/** 资源 `permission-change-logs` 的操作。 */
export const permissionChangeLogs = {
  get: (request: RequestOf<'permission-change-logs.get'>, options?: CallOptions) => invoke<RequestOf<'permission-change-logs.get'>, ResponseOf<'permission-change-logs.get'>>('permission-change-logs.get', routes['permission-change-logs.get'], request, options),
  list: (request: RequestOf<'permission-change-logs.list'>, options?: CallOptions) => invoke<RequestOf<'permission-change-logs.list'>, ResponseOf<'permission-change-logs.list'>>('permission-change-logs.list', routes['permission-change-logs.list'], request, options),
}

/** 资源 `permission-conditions` 的操作。 */
export const permissionConditions = {
  attributes: (request: RequestOf<'permission-conditions.attributes'>, options?: CallOptions) => invoke<RequestOf<'permission-conditions.attributes'>, ResponseOf<'permission-conditions.attributes'>>('permission-conditions.attributes', routes['permission-conditions.attributes'], request, options),
  batchTrial: (request: RequestOf<'permission-conditions.batch-trial'>, options?: CallOptions) => invoke<RequestOf<'permission-conditions.batch-trial'>, ResponseOf<'permission-conditions.batch-trial'>>('permission-conditions.batch-trial', routes['permission-conditions.batch-trial'], request, options),
  create: (request: RequestOf<'permission-conditions.create'>, options?: CallOptions) => invoke<RequestOf<'permission-conditions.create'>, ResponseOf<'permission-conditions.create'>>('permission-conditions.create', routes['permission-conditions.create'], request, options),
  delete: (request: RequestOf<'permission-conditions.delete'>, options?: CallOptions) => invoke<RequestOf<'permission-conditions.delete'>, ResponseOf<'permission-conditions.delete'>>('permission-conditions.delete', routes['permission-conditions.delete'], request, options),
  inventory: (request: RequestOf<'permission-conditions.inventory'>, options?: CallOptions) => invoke<RequestOf<'permission-conditions.inventory'>, ResponseOf<'permission-conditions.inventory'>>('permission-conditions.inventory', routes['permission-conditions.inventory'], request, options),
  list: (request: RequestOf<'permission-conditions.list'>, options?: CallOptions) => invoke<RequestOf<'permission-conditions.list'>, ResponseOf<'permission-conditions.list'>>('permission-conditions.list', routes['permission-conditions.list'], request, options),
  trial: (request: RequestOf<'permission-conditions.trial'>, options?: CallOptions) => invoke<RequestOf<'permission-conditions.trial'>, ResponseOf<'permission-conditions.trial'>>('permission-conditions.trial', routes['permission-conditions.trial'], request, options),
  update: (request: RequestOf<'permission-conditions.update'>, options?: CallOptions) => invoke<RequestOf<'permission-conditions.update'>, ResponseOf<'permission-conditions.update'>>('permission-conditions.update', routes['permission-conditions.update'], request, options),
}

/** 资源 `permissions` 的操作。 */
export const permissions = {
  list: (request: RequestOf<'permissions.list'>, options?: CallOptions) => invoke<RequestOf<'permissions.list'>, ResponseOf<'permissions.list'>>('permissions.list', routes['permissions.list'], request, options),
}

/** 资源 `policies` 的操作。 */
export const policies = {
  attributes: (request: RequestOf<'policies.attributes'>, options?: CallOptions) => invoke<RequestOf<'policies.attributes'>, ResponseOf<'policies.attributes'>>('policies.attributes', routes['policies.attributes'], request, options),
  create: (request: RequestOf<'policies.create'>, options?: CallOptions) => invoke<RequestOf<'policies.create'>, ResponseOf<'policies.create'>>('policies.create', routes['policies.create'], request, options),
  delete: (request: RequestOf<'policies.delete'>, options?: CallOptions) => invoke<RequestOf<'policies.delete'>, ResponseOf<'policies.delete'>>('policies.delete', routes['policies.delete'], request, options),
  get: (request: RequestOf<'policies.get'>, options?: CallOptions) => invoke<RequestOf<'policies.get'>, ResponseOf<'policies.get'>>('policies.get', routes['policies.get'], request, options),
  list: (request: RequestOf<'policies.list'>, options?: CallOptions) => invoke<RequestOf<'policies.list'>, ResponseOf<'policies.list'>>('policies.list', routes['policies.list'], request, options),
  setAction: (request: RequestOf<'policies.set-action'>, options?: CallOptions) => invoke<RequestOf<'policies.set-action'>, ResponseOf<'policies.set-action'>>('policies.set-action', routes['policies.set-action'], request, options),
  setStatus: (request: RequestOf<'policies.set-status'>, options?: CallOptions) => invoke<RequestOf<'policies.set-status'>, ResponseOf<'policies.set-status'>>('policies.set-status', routes['policies.set-status'], request, options),
  templates: (request: RequestOf<'policies.templates'>, options?: CallOptions) => invoke<RequestOf<'policies.templates'>, ResponseOf<'policies.templates'>>('policies.templates', routes['policies.templates'], request, options),
  trial: (request: RequestOf<'policies.trial'>, options?: CallOptions) => invoke<RequestOf<'policies.trial'>, ResponseOf<'policies.trial'>>('policies.trial', routes['policies.trial'], request, options),
  update: (request: RequestOf<'policies.update'>, options?: CallOptions) => invoke<RequestOf<'policies.update'>, ResponseOf<'policies.update'>>('policies.update', routes['policies.update'], request, options),
}

/** 资源 `positions` 的操作。 */
export const positions = {
  create: (request: RequestOf<'positions.create'>, options?: CallOptions) => invoke<RequestOf<'positions.create'>, ResponseOf<'positions.create'>>('positions.create', routes['positions.create'], request, options),
  delete: (request: RequestOf<'positions.delete'>, options?: CallOptions) => invoke<RequestOf<'positions.delete'>, ResponseOf<'positions.delete'>>('positions.delete', routes['positions.delete'], request, options),
  get: (request: RequestOf<'positions.get'>, options?: CallOptions) => invoke<RequestOf<'positions.get'>, ResponseOf<'positions.get'>>('positions.get', routes['positions.get'], request, options),
  list: (request: RequestOf<'positions.list'>, options?: CallOptions) => invoke<RequestOf<'positions.list'>, ResponseOf<'positions.list'>>('positions.list', routes['positions.list'], request, options),
  update: (request: RequestOf<'positions.update'>, options?: CallOptions) => invoke<RequestOf<'positions.update'>, ResponseOf<'positions.update'>>('positions.update', routes['positions.update'], request, options),
}

/** 资源 `profile` 的操作。 */
export const profile = {
  beginExternalBind: (request: RequestOf<'profile.begin-external-bind'>, options?: CallOptions) => invoke<RequestOf<'profile.begin-external-bind'>, ResponseOf<'profile.begin-external-bind'>>('profile.begin-external-bind', routes['profile.begin-external-bind'], request, options),
  changeContact: (request: RequestOf<'profile.change-contact'>, options?: CallOptions) => invoke<RequestOf<'profile.change-contact'>, ResponseOf<'profile.change-contact'>>('profile.change-contact', routes['profile.change-contact'], request, options),
  changePassword: (request: RequestOf<'profile.change-password'>, options?: CallOptions) => invoke<RequestOf<'profile.change-password'>, ResponseOf<'profile.change-password'>>('profile.change-password', routes['profile.change-password'], request, options),
  changeUsername: (request: RequestOf<'profile.change-username'>, options?: CallOptions) => invoke<RequestOf<'profile.change-username'>, ResponseOf<'profile.change-username'>>('profile.change-username', routes['profile.change-username'], request, options),
  disableTwoFactor: (request: RequestOf<'profile.disable-two-factor'>, options?: CallOptions) => invoke<RequestOf<'profile.disable-two-factor'>, ResponseOf<'profile.disable-two-factor'>>('profile.disable-two-factor', routes['profile.disable-two-factor'], request, options),
  enableTwoFactor: (request: RequestOf<'profile.enable-two-factor'>, options?: CallOptions) => invoke<RequestOf<'profile.enable-two-factor'>, ResponseOf<'profile.enable-two-factor'>>('profile.enable-two-factor', routes['profile.enable-two-factor'], request, options),
  externalLogins: (request: RequestOf<'profile.external-logins'>, options?: CallOptions) => invoke<RequestOf<'profile.external-logins'>, ResponseOf<'profile.external-logins'>>('profile.external-logins', routes['profile.external-logins'], request, options),
  get: (request: RequestOf<'profile.get'>, options?: CallOptions) => invoke<RequestOf<'profile.get'>, ResponseOf<'profile.get'>>('profile.get', routes['profile.get'], request, options),
  linkExternal: (request: RequestOf<'profile.link-external'>, options?: CallOptions) => invoke<RequestOf<'profile.link-external'>, ResponseOf<'profile.link-external'>>('profile.link-external', routes['profile.link-external'], request, options),
  notificationPreference: (request: RequestOf<'profile.notification-preference'>, options?: CallOptions) => invoke<RequestOf<'profile.notification-preference'>, ResponseOf<'profile.notification-preference'>>('profile.notification-preference', routes['profile.notification-preference'], request, options),
  removeAvatar: (request: RequestOf<'profile.remove-avatar'>, options?: CallOptions) => invoke<RequestOf<'profile.remove-avatar'>, ResponseOf<'profile.remove-avatar'>>('profile.remove-avatar', routes['profile.remove-avatar'], request, options),
  revokeOtherSessions: (request: RequestOf<'profile.revoke-other-sessions'>, options?: CallOptions) => invoke<RequestOf<'profile.revoke-other-sessions'>, ResponseOf<'profile.revoke-other-sessions'>>('profile.revoke-other-sessions', routes['profile.revoke-other-sessions'], request, options),
  revokeSession: (request: RequestOf<'profile.revoke-session'>, options?: CallOptions) => invoke<RequestOf<'profile.revoke-session'>, ResponseOf<'profile.revoke-session'>>('profile.revoke-session', routes['profile.revoke-session'], request, options),
  sendChangeContactCode: (request: RequestOf<'profile.send-change-contact-code'>, options?: CallOptions) => invoke<RequestOf<'profile.send-change-contact-code'>, ResponseOf<'profile.send-change-contact-code'>>('profile.send-change-contact-code', routes['profile.send-change-contact-code'], request, options),
  sendTwoFactorCode: (request: RequestOf<'profile.send-two-factor-code'>, options?: CallOptions) => invoke<RequestOf<'profile.send-two-factor-code'>, ResponseOf<'profile.send-two-factor-code'>>('profile.send-two-factor-code', routes['profile.send-two-factor-code'], request, options),
  sendVerificationCode: (request: RequestOf<'profile.send-verification-code'>, options?: CallOptions) => invoke<RequestOf<'profile.send-verification-code'>, ResponseOf<'profile.send-verification-code'>>('profile.send-verification-code', routes['profile.send-verification-code'], request, options),
  sessions: (request: RequestOf<'profile.sessions'>, options?: CallOptions) => invoke<RequestOf<'profile.sessions'>, ResponseOf<'profile.sessions'>>('profile.sessions', routes['profile.sessions'], request, options),
  setupTotp: (request: RequestOf<'profile.setup-totp'>, options?: CallOptions) => invoke<RequestOf<'profile.setup-totp'>, ResponseOf<'profile.setup-totp'>>('profile.setup-totp', routes['profile.setup-totp'], request, options),
  unlinkExternal: (request: RequestOf<'profile.unlink-external'>, options?: CallOptions) => invoke<RequestOf<'profile.unlink-external'>, ResponseOf<'profile.unlink-external'>>('profile.unlink-external', routes['profile.unlink-external'], request, options),
  update: (request: RequestOf<'profile.update'>, options?: CallOptions) => invoke<RequestOf<'profile.update'>, ResponseOf<'profile.update'>>('profile.update', routes['profile.update'], request, options),
  updateNotificationPreference: (request: RequestOf<'profile.update-notification-preference'>, options?: CallOptions) => invoke<RequestOf<'profile.update-notification-preference'>, ResponseOf<'profile.update-notification-preference'>>('profile.update-notification-preference', routes['profile.update-notification-preference'], request, options),
  verifyContact: (request: RequestOf<'profile.verify-contact'>, options?: CallOptions) => invoke<RequestOf<'profile.verify-contact'>, ResponseOf<'profile.verify-contact'>>('profile.verify-contact', routes['profile.verify-contact'], request, options),
}

/** 资源 `roles` 的操作。 */
export const roles = {
  assign: (request: RequestOf<'roles.assign'>, options?: CallOptions) => invoke<RequestOf<'roles.assign'>, ResponseOf<'roles.assign'>>('roles.assign', routes['roles.assign'], request, options),
  byUser: (request: RequestOf<'roles.by-user'>, options?: CallOptions) => invoke<RequestOf<'roles.by-user'>, ResponseOf<'roles.by-user'>>('roles.by-user', routes['roles.by-user'], request, options),
  create: (request: RequestOf<'roles.create'>, options?: CallOptions) => invoke<RequestOf<'roles.create'>, ResponseOf<'roles.create'>>('roles.create', routes['roles.create'], request, options),
  delete: (request: RequestOf<'roles.delete'>, options?: CallOptions) => invoke<RequestOf<'roles.delete'>, ResponseOf<'roles.delete'>>('roles.delete', routes['roles.delete'], request, options),
  deny: (request: RequestOf<'roles.deny'>, options?: CallOptions) => invoke<RequestOf<'roles.deny'>, ResponseOf<'roles.deny'>>('roles.deny', routes['roles.deny'], request, options),
  get: (request: RequestOf<'roles.get'>, options?: CallOptions) => invoke<RequestOf<'roles.get'>, ResponseOf<'roles.get'>>('roles.get', routes['roles.get'], request, options),
  grant: (request: RequestOf<'roles.grant'>, options?: CallOptions) => invoke<RequestOf<'roles.grant'>, ResponseOf<'roles.grant'>>('roles.grant', routes['roles.grant'], request, options),
  hierarchy: (request: RequestOf<'roles.hierarchy'>, options?: CallOptions) => invoke<RequestOf<'roles.hierarchy'>, ResponseOf<'roles.hierarchy'>>('roles.hierarchy', routes['roles.hierarchy'], request, options),
  list: (request: RequestOf<'roles.list'>, options?: CallOptions) => invoke<RequestOf<'roles.list'>, ResponseOf<'roles.list'>>('roles.list', routes['roles.list'], request, options),
  members: (request: RequestOf<'roles.members'>, options?: CallOptions) => invoke<RequestOf<'roles.members'>, ResponseOf<'roles.members'>>('roles.members', routes['roles.members'], request, options),
  revoke: (request: RequestOf<'roles.revoke'>, options?: CallOptions) => invoke<RequestOf<'roles.revoke'>, ResponseOf<'roles.revoke'>>('roles.revoke', routes['roles.revoke'], request, options),
  setParents: (request: RequestOf<'roles.set-parents'>, options?: CallOptions) => invoke<RequestOf<'roles.set-parents'>, ResponseOf<'roles.set-parents'>>('roles.set-parents', routes['roles.set-parents'], request, options),
  unassign: (request: RequestOf<'roles.unassign'>, options?: CallOptions) => invoke<RequestOf<'roles.unassign'>, ResponseOf<'roles.unassign'>>('roles.unassign', routes['roles.unassign'], request, options),
  update: (request: RequestOf<'roles.update'>, options?: CallOptions) => invoke<RequestOf<'roles.update'>, ResponseOf<'roles.update'>>('roles.update', routes['roles.update'], request, options),
  updateMember: (request: RequestOf<'roles.update-member'>, options?: CallOptions) => invoke<RequestOf<'roles.update-member'>, ResponseOf<'roles.update-member'>>('roles.update-member', routes['roles.update-member'], request, options),
}

/** 资源 `session-roles` 的操作。 */
export const sessionRoles = {
  get: (request: RequestOf<'session-roles.get'>, options?: CallOptions) => invoke<RequestOf<'session-roles.get'>, ResponseOf<'session-roles.get'>>('session-roles.get', routes['session-roles.get'], request, options),
  list: (request: RequestOf<'session-roles.list'>, options?: CallOptions) => invoke<RequestOf<'session-roles.list'>, ResponseOf<'session-roles.list'>>('session-roles.list', routes['session-roles.list'], request, options),
}

/** 资源 `sessions` 的操作。 */
export const sessions = {
  list: (request: RequestOf<'sessions.list'>, options?: CallOptions) => invoke<RequestOf<'sessions.list'>, ResponseOf<'sessions.list'>>('sessions.list', routes['sessions.list'], request, options),
  revoke: (request: RequestOf<'sessions.revoke'>, options?: CallOptions) => invoke<RequestOf<'sessions.revoke'>, ResponseOf<'sessions.revoke'>>('sessions.revoke', routes['sessions.revoke'], request, options),
  summary: (request: RequestOf<'sessions.summary'>, options?: CallOptions) => invoke<RequestOf<'sessions.summary'>, ResponseOf<'sessions.summary'>>('sessions.summary', routes['sessions.summary'], request, options),
}

/** 资源 `sms-configs` 的操作。 */
export const smsConfigs = {
  create: (request: RequestOf<'sms-configs.create'>, options?: CallOptions) => invoke<RequestOf<'sms-configs.create'>, ResponseOf<'sms-configs.create'>>('sms-configs.create', routes['sms-configs.create'], request, options),
  delete: (request: RequestOf<'sms-configs.delete'>, options?: CallOptions) => invoke<RequestOf<'sms-configs.delete'>, ResponseOf<'sms-configs.delete'>>('sms-configs.delete', routes['sms-configs.delete'], request, options),
  get: (request: RequestOf<'sms-configs.get'>, options?: CallOptions) => invoke<RequestOf<'sms-configs.get'>, ResponseOf<'sms-configs.get'>>('sms-configs.get', routes['sms-configs.get'], request, options),
  list: (request: RequestOf<'sms-configs.list'>, options?: CallOptions) => invoke<RequestOf<'sms-configs.list'>, ResponseOf<'sms-configs.list'>>('sms-configs.list', routes['sms-configs.list'], request, options),
  setDefault: (request: RequestOf<'sms-configs.set-default'>, options?: CallOptions) => invoke<RequestOf<'sms-configs.set-default'>, ResponseOf<'sms-configs.set-default'>>('sms-configs.set-default', routes['sms-configs.set-default'], request, options),
  update: (request: RequestOf<'sms-configs.update'>, options?: CallOptions) => invoke<RequestOf<'sms-configs.update'>, ResponseOf<'sms-configs.update'>>('sms-configs.update', routes['sms-configs.update'], request, options),
}

/** 资源 `sms` 的操作。 */
export const sms = {
  cancel: (request: RequestOf<'sms.cancel'>, options?: CallOptions) => invoke<RequestOf<'sms.cancel'>, ResponseOf<'sms.cancel'>>('sms.cancel', routes['sms.cancel'], request, options),
  delete: (request: RequestOf<'sms.delete'>, options?: CallOptions) => invoke<RequestOf<'sms.delete'>, ResponseOf<'sms.delete'>>('sms.delete', routes['sms.delete'], request, options),
  get: (request: RequestOf<'sms.get'>, options?: CallOptions) => invoke<RequestOf<'sms.get'>, ResponseOf<'sms.get'>>('sms.get', routes['sms.get'], request, options),
  list: (request: RequestOf<'sms.list'>, options?: CallOptions) => invoke<RequestOf<'sms.list'>, ResponseOf<'sms.list'>>('sms.list', routes['sms.list'], request, options),
  resend: (request: RequestOf<'sms.resend'>, options?: CallOptions) => invoke<RequestOf<'sms.resend'>, ResponseOf<'sms.resend'>>('sms.resend', routes['sms.resend'], request, options),
}

/** 资源 `storage-configs` 的操作。 */
export const storageConfigs = {
  create: (request: RequestOf<'storage-configs.create'>, options?: CallOptions) => invoke<RequestOf<'storage-configs.create'>, ResponseOf<'storage-configs.create'>>('storage-configs.create', routes['storage-configs.create'], request, options),
  delete: (request: RequestOf<'storage-configs.delete'>, options?: CallOptions) => invoke<RequestOf<'storage-configs.delete'>, ResponseOf<'storage-configs.delete'>>('storage-configs.delete', routes['storage-configs.delete'], request, options),
  get: (request: RequestOf<'storage-configs.get'>, options?: CallOptions) => invoke<RequestOf<'storage-configs.get'>, ResponseOf<'storage-configs.get'>>('storage-configs.get', routes['storage-configs.get'], request, options),
  list: (request: RequestOf<'storage-configs.list'>, options?: CallOptions) => invoke<RequestOf<'storage-configs.list'>, ResponseOf<'storage-configs.list'>>('storage-configs.list', routes['storage-configs.list'], request, options),
  setDefault: (request: RequestOf<'storage-configs.set-default'>, options?: CallOptions) => invoke<RequestOf<'storage-configs.set-default'>, ResponseOf<'storage-configs.set-default'>>('storage-configs.set-default', routes['storage-configs.set-default'], request, options),
  setStatus: (request: RequestOf<'storage-configs.set-status'>, options?: CallOptions) => invoke<RequestOf<'storage-configs.set-status'>, ResponseOf<'storage-configs.set-status'>>('storage-configs.set-status', routes['storage-configs.set-status'], request, options),
  update: (request: RequestOf<'storage-configs.update'>, options?: CallOptions) => invoke<RequestOf<'storage-configs.update'>, ResponseOf<'storage-configs.update'>>('storage-configs.update', routes['storage-configs.update'], request, options),
}

/** 资源 `subscription` 的操作。 */
export const subscription = {
  get: (request: RequestOf<'subscription.get'>, options?: CallOptions) => invoke<RequestOf<'subscription.get'>, ResponseOf<'subscription.get'>>('subscription.get', routes['subscription.get'], request, options),
}

/** 资源 `telegram-bots` 的操作。 */
export const telegramBots = {
  create: (request: RequestOf<'telegram-bots.create'>, options?: CallOptions) => invoke<RequestOf<'telegram-bots.create'>, ResponseOf<'telegram-bots.create'>>('telegram-bots.create', routes['telegram-bots.create'], request, options),
  delete: (request: RequestOf<'telegram-bots.delete'>, options?: CallOptions) => invoke<RequestOf<'telegram-bots.delete'>, ResponseOf<'telegram-bots.delete'>>('telegram-bots.delete', routes['telegram-bots.delete'], request, options),
  get: (request: RequestOf<'telegram-bots.get'>, options?: CallOptions) => invoke<RequestOf<'telegram-bots.get'>, ResponseOf<'telegram-bots.get'>>('telegram-bots.get', routes['telegram-bots.get'], request, options),
  list: (request: RequestOf<'telegram-bots.list'>, options?: CallOptions) => invoke<RequestOf<'telegram-bots.list'>, ResponseOf<'telegram-bots.list'>>('telegram-bots.list', routes['telegram-bots.list'], request, options),
  update: (request: RequestOf<'telegram-bots.update'>, options?: CallOptions) => invoke<RequestOf<'telegram-bots.update'>, ResponseOf<'telegram-bots.update'>>('telegram-bots.update', routes['telegram-bots.update'], request, options),
}

/** 资源 `tenant-members` 的操作。 */
export const tenantMembers = {
  create: (request: RequestOf<'tenant-members.create'>, options?: CallOptions) => invoke<RequestOf<'tenant-members.create'>, ResponseOf<'tenant-members.create'>>('tenant-members.create', routes['tenant-members.create'], request, options),
  delete: (request: RequestOf<'tenant-members.delete'>, options?: CallOptions) => invoke<RequestOf<'tenant-members.delete'>, ResponseOf<'tenant-members.delete'>>('tenant-members.delete', routes['tenant-members.delete'], request, options),
  list: (request: RequestOf<'tenant-members.list'>, options?: CallOptions) => invoke<RequestOf<'tenant-members.list'>, ResponseOf<'tenant-members.list'>>('tenant-members.list', routes['tenant-members.list'], request, options),
}

/** 资源 `tenants` 的操作。 */
export const tenants = {
  changeEdition: (request: RequestOf<'tenants.change-edition'>, options?: CallOptions) => invoke<RequestOf<'tenants.change-edition'>, ResponseOf<'tenants.change-edition'>>('tenants.change-edition', routes['tenants.change-edition'], request, options),
  configureDatabase: (request: RequestOf<'tenants.configure-database'>, options?: CallOptions) => invoke<RequestOf<'tenants.configure-database'>, ResponseOf<'tenants.configure-database'>>('tenants.configure-database', routes['tenants.configure-database'], request, options),
  create: (request: RequestOf<'tenants.create'>, options?: CallOptions) => invoke<RequestOf<'tenants.create'>, ResponseOf<'tenants.create'>>('tenants.create', routes['tenants.create'], request, options),
  delete: (request: RequestOf<'tenants.delete'>, options?: CallOptions) => invoke<RequestOf<'tenants.delete'>, ResponseOf<'tenants.delete'>>('tenants.delete', routes['tenants.delete'], request, options),
  get: (request: RequestOf<'tenants.get'>, options?: CallOptions) => invoke<RequestOf<'tenants.get'>, ResponseOf<'tenants.get'>>('tenants.get', routes['tenants.get'], request, options),
  initializeAdmin: (request: RequestOf<'tenants.initialize-admin'>, options?: CallOptions) => invoke<RequestOf<'tenants.initialize-admin'>, ResponseOf<'tenants.initialize-admin'>>('tenants.initialize-admin', routes['tenants.initialize-admin'], request, options),
  initializeDatabase: (request: RequestOf<'tenants.initialize-database'>, options?: CallOptions) => invoke<RequestOf<'tenants.initialize-database'>, ResponseOf<'tenants.initialize-database'>>('tenants.initialize-database', routes['tenants.initialize-database'], request, options),
  list: (request: RequestOf<'tenants.list'>, options?: CallOptions) => invoke<RequestOf<'tenants.list'>, ResponseOf<'tenants.list'>>('tenants.list', routes['tenants.list'], request, options),
  mine: (request: RequestOf<'tenants.mine'>, options?: CallOptions) => invoke<RequestOf<'tenants.mine'>, ResponseOf<'tenants.mine'>>('tenants.mine', routes['tenants.mine'], request, options),
  quotas: (request: RequestOf<'tenants.quotas'>, options?: CallOptions) => invoke<RequestOf<'tenants.quotas'>, ResponseOf<'tenants.quotas'>>('tenants.quotas', routes['tenants.quotas'], request, options),
  resume: (request: RequestOf<'tenants.resume'>, options?: CallOptions) => invoke<RequestOf<'tenants.resume'>, ResponseOf<'tenants.resume'>>('tenants.resume', routes['tenants.resume'], request, options),
  suspend: (request: RequestOf<'tenants.suspend'>, options?: CallOptions) => invoke<RequestOf<'tenants.suspend'>, ResponseOf<'tenants.suspend'>>('tenants.suspend', routes['tenants.suspend'], request, options),
  transferOwner: (request: RequestOf<'tenants.transfer-owner'>, options?: CallOptions) => invoke<RequestOf<'tenants.transfer-owner'>, ResponseOf<'tenants.transfer-owner'>>('tenants.transfer-owner', routes['tenants.transfer-owner'], request, options),
  update: (request: RequestOf<'tenants.update'>, options?: CallOptions) => invoke<RequestOf<'tenants.update'>, ResponseOf<'tenants.update'>>('tenants.update', routes['tenants.update'], request, options),
}

/** 资源 `traces` 的操作。 */
export const traces = {
  timeline: (request: RequestOf<'traces.timeline'>, options?: CallOptions) => invoke<RequestOf<'traces.timeline'>, ResponseOf<'traces.timeline'>>('traces.timeline', routes['traces.timeline'], request, options),
}

/** 资源 `user-data-scopes` 的操作。 */
export const userDataScopes = {
  get: (request: RequestOf<'user-data-scopes.get'>, options?: CallOptions) => invoke<RequestOf<'user-data-scopes.get'>, ResponseOf<'user-data-scopes.get'>>('user-data-scopes.get', routes['user-data-scopes.get'], request, options),
  set: (request: RequestOf<'user-data-scopes.set'>, options?: CallOptions) => invoke<RequestOf<'user-data-scopes.set'>, ResponseOf<'user-data-scopes.set'>>('user-data-scopes.set', routes['user-data-scopes.set'], request, options),
}

/** 资源 `user-grants` 的操作。 */
export const userGrants = {
  deny: (request: RequestOf<'user-grants.deny'>, options?: CallOptions) => invoke<RequestOf<'user-grants.deny'>, ResponseOf<'user-grants.deny'>>('user-grants.deny', routes['user-grants.deny'], request, options),
  grant: (request: RequestOf<'user-grants.grant'>, options?: CallOptions) => invoke<RequestOf<'user-grants.grant'>, ResponseOf<'user-grants.grant'>>('user-grants.grant', routes['user-grants.grant'], request, options),
  list: (request: RequestOf<'user-grants.list'>, options?: CallOptions) => invoke<RequestOf<'user-grants.list'>, ResponseOf<'user-grants.list'>>('user-grants.list', routes['user-grants.list'], request, options),
  revoke: (request: RequestOf<'user-grants.revoke'>, options?: CallOptions) => invoke<RequestOf<'user-grants.revoke'>, ResponseOf<'user-grants.revoke'>>('user-grants.revoke', routes['user-grants.revoke'], request, options),
}

/** 资源 `user-settings` 的操作。 */
export const userSettings = {
  get: (request: RequestOf<'user-settings.get'>, options?: CallOptions) => invoke<RequestOf<'user-settings.get'>, ResponseOf<'user-settings.get'>>('user-settings.get', routes['user-settings.get'], request, options),
  update: (request: RequestOf<'user-settings.update'>, options?: CallOptions) => invoke<RequestOf<'user-settings.update'>, ResponseOf<'user-settings.update'>>('user-settings.update', routes['user-settings.update'], request, options),
}

/** 资源 `users` 的操作。 */
export const users = {
  create: (request: RequestOf<'users.create'>, options?: CallOptions) => invoke<RequestOf<'users.create'>, ResponseOf<'users.create'>>('users.create', routes['users.create'], request, options),
  delete: (request: RequestOf<'users.delete'>, options?: CallOptions) => invoke<RequestOf<'users.delete'>, ResponseOf<'users.delete'>>('users.delete', routes['users.delete'], request, options),
  get: (request: RequestOf<'users.get'>, options?: CallOptions) => invoke<RequestOf<'users.get'>, ResponseOf<'users.get'>>('users.get', routes['users.get'], request, options),
  list: (request: RequestOf<'users.list'>, options?: CallOptions) => invoke<RequestOf<'users.list'>, ResponseOf<'users.list'>>('users.list', routes['users.list'], request, options),
  lock: (request: RequestOf<'users.lock'>, options?: CallOptions) => invoke<RequestOf<'users.lock'>, ResponseOf<'users.lock'>>('users.lock', routes['users.lock'], request, options),
  resetPassword: (request: RequestOf<'users.reset-password'>, options?: CallOptions) => invoke<RequestOf<'users.reset-password'>, ResponseOf<'users.reset-password'>>('users.reset-password', routes['users.reset-password'], request, options),
  resetTwoFactor: (request: RequestOf<'users.reset-two-factor'>, options?: CallOptions) => invoke<RequestOf<'users.reset-two-factor'>, ResponseOf<'users.reset-two-factor'>>('users.reset-two-factor', routes['users.reset-two-factor'], request, options),
  unlock: (request: RequestOf<'users.unlock'>, options?: CallOptions) => invoke<RequestOf<'users.unlock'>, ResponseOf<'users.unlock'>>('users.unlock', routes['users.unlock'], request, options),
  update: (request: RequestOf<'users.update'>, options?: CallOptions) => invoke<RequestOf<'users.update'>, ResponseOf<'users.update'>>('users.update', routes['users.update'], request, options),
}

/** 资源 `versions` 的操作。 */
export const versions = {
  get: (request: RequestOf<'versions.get'>, options?: CallOptions) => invoke<RequestOf<'versions.get'>, ResponseOf<'versions.get'>>('versions.get', routes['versions.get'], request, options),
  list: (request: RequestOf<'versions.list'>, options?: CallOptions) => invoke<RequestOf<'versions.list'>, ResponseOf<'versions.list'>>('versions.list', routes['versions.list'], request, options),
}
