/**
 * 业务模块 API 客户端的路由约定回归锚点（src/modules/<模块>/api）。
 *
 * 职责边界：模块侧的动作名普遍是短名（Page / Detail / Create），与 src/api 的资源前缀命名不同；
 * 且注释里逐条写明了「哪些前缀被后端剥离、哪些参数只能走 query」。这里为这些约定各配一条用例。
 * 不测 .vue，也不测导入了 @xihan-ui 的模块。
 */
import type { AxiosRequestConfig } from '~/request'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { aiAssistantApi } from './ai/api/assistant'
import { chatAssistantProviderApi } from './ai/api/chat-assistant'
import { knowledgeApi } from './ai/api/knowledge'
import { aiProviderApi } from './ai/api/provider'
import { chatApi } from './chat/api/chat'
import { chatAuditApi } from './chat/api/chat-audit'
import { codeGenTableColumnApi } from './codegen/api/codegen-table-column'
import { codeGenRuntimeApi } from './codegen/api/dynamic-runtime'
import { codeGenerationApi } from './codegen/api/generation'
import { printDataSourceApi } from './printing/api/print-data-source'
import { printTemplateApi } from './printing/api/print-template'
import { PRINT_PAPER_PRESETS } from './printing/views/setting/print-template/components/models'
import { workflowDefinitionApi } from './workflow/api/definition'
import { workflowTodoApi } from './workflow/api/todo'

interface RecordedCall {
  method: 'DELETE' | 'GET' | 'POST' | 'PUT'
  url: string
  body?: unknown
  params?: unknown
}

const hoisted = vi.hoisted(() => ({ calls: [] as RecordedCall[] }))

vi.mock('@/api/request', () => ({
  requestClient: {
    get(url: string, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'GET', url, params: config?.params })
      return Promise.resolve(null)
    },
    post(url: string, body?: unknown, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'POST', url, body, params: config?.params })
      return Promise.resolve(null)
    },
    put(url: string, body?: unknown, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'PUT', url, body, params: config?.params })
      return Promise.resolve(null)
    },
    delete(url: string, config?: AxiosRequestConfig) {
      hoisted.calls.push({ method: 'DELETE', url, params: config?.params })
      return Promise.resolve(undefined)
    },
  },
}))

const { calls } = hoisted

beforeEach(() => {
  calls.length = 0
})

function only(): RecordedCall {
  expect(calls).toHaveLength(1)
  const call = calls[0]
  if (!call) {
    throw new Error('没有记录到任何请求调用')
  }
  return call
}

describe('零代码只读运行时', () => {
  it('取表结构走 GET /DynamicRuntime/Schema，tableId 走查询串', async () => {
    await codeGenRuntimeApi.getSchema('101')

    expect(only()).toEqual({ method: 'GET', url: '/DynamicRuntime/Schema', params: { tableId: '101' } })
  })

  it('运行时分页是全仓唯一走 GET + 查询串的分页，且空值被裁掉', async () => {
    await codeGenRuntimeApi.page({ tableId: '101', pageIndex: 1, pageSize: 20 })

    expect(only()).toEqual({
      method: 'GET',
      url: '/DynamicRuntime/Page',
      params: { TableId: '101', PageIndex: 1, PageSize: 20 },
    })
  })

  it('运行时分页的页码 0 是有效取值不会被裁掉——只有 undefined/null/空串才滤', async () => {
    await codeGenRuntimeApi.page({ tableId: '101', pageIndex: 0, pageSize: 20 })

    expect(only().params).toEqual({ TableId: '101', PageIndex: 0, PageSize: 20 })
  })
})

describe('打印模板的路由段与作用域', () => {
  it('详情把主键编码后拼进路由段，作用域走查询串', async () => {
    await printTemplateApi.detail('1975', 'Tenant' as never)

    expect(only()).toEqual({
      method: 'GET',
      url: '/PrintTemplateQuery/PrintTemplateDetail/1975',
      params: { scope: 'Tenant' },
    })
  })

  it('主键里的斜杠与中文被编码，不会把一个主键拆成多个路由段', async () => {
    await printTemplateApi.detail('模板/1', 'Global' as never)

    expect(only().url).toBe('/PrintTemplateQuery/PrintTemplateDetail/%E6%A8%A1%E6%9D%BF%2F1')
  })

  it('删除把 DTO 显式展开成三个查询参数——框架的 DELETE 不发请求体', async () => {
    await printTemplateApi.delete({ basicId: '1', rowVersion: 'v2', scope: 'Tenant' } as never)

    expect(only()).toEqual({
      method: 'DELETE',
      url: '/PrintTemplate/PrintTemplate',
      params: { basicId: '1', rowVersion: 'v2', scope: 'Tenant' },
    })
  })

  it('按编码解析模板走 GET，编码与作用域都在查询串', async () => {
    await printTemplateApi.resolveByCode('sales-order', 'Auto' as never)

    expect(only()).toEqual({
      method: 'GET',
      url: '/PrintTemplateQuery/ResolvedPrintTemplateByCode',
      params: { scope: 'Auto', templateCode: 'sales-order' },
    })
  })

  it('数据源目录只有一个只读入口，走 GET /PrintDataSourceQuery/List', async () => {
    await printDataSourceApi.list()

    expect(only()).toEqual({ method: 'GET', url: '/PrintDataSourceQuery/List', params: undefined })
  })

  it('标准纸张预设无重复且全为大写编码', () => {
    expect(new Set(PRINT_PAPER_PRESETS).size).toBe(PRINT_PAPER_PRESETS.length)
    expect(PRINT_PAPER_PRESETS.filter(item => !/^[AB]\d$/.test(item))).toEqual([])
  })
})

describe('聊天接口的动词前缀剥离', () => {
  it('建群的 Create 前缀被剥离，路由只剩 GroupConversation', async () => {
    await chatApi.createGroupConversation('研发群', ['1', '2'])

    expect(only()).toEqual({
      method: 'POST',
      url: '/Chat/GroupConversation',
      body: { conversationName: '研发群', memberUserIds: ['1', '2'] },
      params: undefined,
    })
  })

  it('加成员的 Add 前缀被剥离，移除成员的 Remove 前缀被剥离且降级为 DELETE + 查询串', async () => {
    await chatApi.addMembers('c1', ['9'])
    await chatApi.removeMember('c1', '9')

    expect(calls).toEqual([
      { method: 'POST', url: '/Chat/Members', body: { conversationId: 'c1', userIds: ['9'] }, params: undefined },
      { method: 'DELETE', url: '/Chat/Member', params: { conversationId: 'c1', userId: '9' } },
    ])
  })

  it('编辑消息与改会话信息的 Edit / Update 前缀映射为 PUT 且被剥离', async () => {
    await chatApi.editMessage('m1', '新内容')
    await chatApi.updateConversationInfo({ conversationId: 'c1' } as never)

    expect(calls.map(item => `${item.method} ${item.url}`)).toEqual([
      'PUT /Chat/Message',
      'PUT /Chat/ConversationInfo',
    ])
  })

  it('撤回消息的 messageId 走查询串，请求体为空（POST 不把 id 拼进路由段）', async () => {
    await chatApi.recallMessage('m9')

    expect(only()).toEqual({
      method: 'POST',
      url: '/Chat/RecallMessage',
      body: undefined,
      params: { messageId: 'm9' },
    })
  })

  it('非 CRUD 前缀的动作保留完整方法名并默认 POST', async () => {
    await chatApi.openSingleConversation('u1')
    await chatApi.sendMessage({ conversationId: 'c1' } as never)
    await chatApi.markRead('c1')
    await chatApi.pinMessage('m1')

    expect(calls.map(item => `${item.method} ${item.url}`)).toEqual([
      'POST /Chat/OpenSingleConversation',
      'POST /Chat/SendMessage',
      'POST /Chat/MarkRead',
      'POST /Chat/PinMessage',
    ])
  })

  it('轻量选人默认取 20 条，关键字为空时不下发 Keyword', async () => {
    await chatApi.userOptions('')
    await chatApi.userOptions('张', 5)

    expect(calls.map(item => item.params)).toEqual([
      { Limit: 20 },
      { Limit: 5, Keyword: '张' },
    ])
  })

  it('会话已读位置与置顶消息走独立的只读控制器 ChatQuery', async () => {
    await chatApi.readPositions('c1')
    await chatApi.pinnedMessages('c1')
    await chatApi.myConversations()

    expect(calls.map(item => `${item.method} ${item.url}`)).toEqual([
      'GET /ChatQuery/ReadPositions',
      'GET /ChatQuery/PinnedMessages',
      'GET /ChatQuery/MyConversations',
    ])
  })

  it('聊天审计分页走管理侧的 ChatAuditQuery，动作名保留 ChatMessagePage 全名', async () => {
    await chatAuditApi.page({ conditions: { filters: [], keyword: null, sorts: [] }, page: { pageIndex: 1, pageSize: 20 } } as never)

    expect(only()).toMatchObject({ method: 'POST', url: '/ChatAuditQuery/ChatMessagePage' })
  })
})

describe('智能助手模块的三段式控制器', () => {
  it('助手管理的读写分处 AiAssistant 与 AiAssistantQuery 两个控制器', async () => {
    await aiAssistantApi.create({} as never)
    await aiAssistantApi.updateStatus({} as never)
    await aiAssistantApi.delete('1')
    await aiAssistantApi.page({} as never)
    await aiAssistantApi.detail('1')

    expect(calls.map(item => `${item.method} ${item.url}`)).toEqual([
      'POST /AiAssistant/Create',
      'PUT /AiAssistant/Status',
      'DELETE /AiAssistant/Delete',
      'POST /AiAssistantQuery/Page',
      'GET /AiAssistantQuery/Detail',
    ])
  })

  it('设为默认把主键包成 { basicId } 上送，删除则把主键放查询串', async () => {
    await aiAssistantApi.setDefault('7')
    await aiAssistantApi.delete('7')

    expect(calls).toEqual([
      { method: 'POST', url: '/AiAssistant/SetDefault', body: { basicId: '7' }, params: undefined },
      { method: 'DELETE', url: '/AiAssistant/Delete', params: { id: '7' } },
    ])
  })

  it('供应商连通性测试与设为默认同样用 { basicId } 请求体', async () => {
    await aiProviderApi.testConnection('3')

    expect(only()).toEqual({
      method: 'POST',
      url: '/AiProvider/TestConnection',
      body: { basicId: '3' },
      params: undefined,
    })
  })

  it('知识库落在三个控制器：写入 KnowledgeDocument、读取 KnowledgeDocumentQuery、检索 KnowledgeQuery', async () => {
    await knowledgeApi.ingest({} as never)
    await knowledgeApi.detail('1')
    await knowledgeApi.query({} as never)

    expect(calls.map(item => `${item.method} ${item.url}`)).toEqual([
      'POST /KnowledgeDocument/Ingest',
      'GET /KnowledgeDocumentQuery/Detail',
      'POST /KnowledgeQuery/Query',
    ])
  })

  it('聊天助手的可用列表挂在 AiAssistantQuery/Available（只要登录态，不看助手管理权限）', async () => {
    await chatAssistantProviderApi.availableAssistants()
    await chatAssistantProviderApi.openConversation('a1')
    await chatAssistantProviderApi.reply('c1', 'r1')

    expect(calls.map(item => `${item.method} ${item.url}`)).toEqual([
      'GET /AiAssistantQuery/Available',
      'POST /ChatAssistant/OpenConversation',
      'POST /ChatAssistant/Reply',
    ])
  })
})

describe('代码生成与工作流的动作名', () => {
  it('代码生成编排的动作名直调，不走资源前缀命名', async () => {
    await codeGenerationApi.importTable({} as never)
    await codeGenerationApi.preview({} as never)
    await codeGenerationApi.generate({} as never)
    await codeGenerationApi.syncSchema('5')

    expect(calls.map(item => `${item.method} ${item.url}`)).toEqual([
      'POST /CodeGeneration/ImportTable',
      'POST /CodeGeneration/Preview',
      'POST /CodeGeneration/Generate',
      'POST /CodeGeneration/SyncSchema',
    ])
  })

  it('列可导入数据库表走 GET 并裁掉空关键字', async () => {
    await codeGenerationApi.listDatabaseTables({ dataSourceId: '1', keyword: '' })

    expect(only()).toEqual({
      method: 'GET',
      url: '/CodeGeneration/DatabaseTables',
      params: { DataSourceId: '1' },
    })
  })

  it('列配置批量保存走 POST BatchSave，单条更新走 PUT Update', async () => {
    await codeGenTableColumnApi.batchSave({} as never)
    await codeGenTableColumnApi.update({} as never)
    await codeGenTableColumnApi.getByTable('9')

    expect(calls.map(item => `${item.method} ${item.url}`)).toEqual([
      'POST /CodeGenTableColumn/BatchSave',
      'PUT /CodeGenTableColumn/Update',
      'GET /CodeGenTableColumnQuery/ByTable',
    ])
  })

  it('流程定义的草稿更新是 PUT /WorkflowDefinition/Draft，其余状态流转保留完整动作名走 POST', async () => {
    await workflowDefinitionApi.updateDraft({} as never)
    await workflowDefinitionApi.publish({} as never)
    await workflowDefinitionApi.newVersion({} as never)
    await workflowDefinitionApi.disable({} as never)
    await workflowDefinitionApi.archive({} as never)

    expect(calls.map(item => `${item.method} ${item.url}`)).toEqual([
      'PUT /WorkflowDefinition/Draft',
      'POST /WorkflowDefinition/Publish',
      'POST /WorkflowDefinition/NewVersion',
      'POST /WorkflowDefinition/Disable',
      'POST /WorkflowDefinition/Archive',
    ])
  })

  it('待办只有分页一个读入口，受理人由服务端锁定所以不带用户参数', async () => {
    await workflowTodoApi.page({ conditions: { filters: [], keyword: null, sorts: [] }, page: { pageIndex: 1, pageSize: 20 } } as never)

    expect(only()).toMatchObject({ method: 'POST', url: '/WorkflowTodoQuery/Page', params: undefined })
  })

  it('待办的三个写动作都保留完整方法名走 POST', async () => {
    await workflowTodoApi.complete({} as never)
    await workflowTodoApi.transfer({} as never)
    await workflowTodoApi.addAssignees({} as never)

    expect(calls.map(item => `${item.method} ${item.url}`)).toEqual([
      'POST /WorkflowTodo/Complete',
      'POST /WorkflowTodo/Transfer',
      'POST /WorkflowTodo/AddAssignees',
    ])
  })
})
