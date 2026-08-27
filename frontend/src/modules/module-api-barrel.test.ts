/**
 * 模块 API 桶文件的导出完整性测试。
 *
 * 职责边界：ai / codegen / workflow 三个模块用 `export *` 聚合各自的 api 目录，
 * 同名导出会被静默丢弃。这里逐个子模块反查其运行时导出是否都能从模块桶取到，
 * 并锁定「聊天助手提供方不进桶文件」这条刻意的例外（它只经 setup.ts 注册进聊天扩展点）。
 */
import type { AxiosRequestConfig } from '~/request'
import { describe, expect, it, vi } from 'vitest'
import * as aiBarrel from './ai/api'
import * as aiEnums from './ai/api/ai.enums'
import * as aiAssistant from './ai/api/assistant'
import * as chatAssistant from './ai/api/chat-assistant'
import * as aiKnowledge from './ai/api/knowledge'
import * as knowledgeEnums from './ai/api/knowledge.enums'
import * as aiPrompt from './ai/api/prompt'
import * as aiProvider from './ai/api/provider'
import * as codegenBarrel from './codegen/api'
import * as codegenHistory from './codegen/api/codegen-history'
import * as codegenColumn from './codegen/api/codegen-table-column'
import * as codegenEnums from './codegen/api/codegen.enums'
import * as codegenDatasource from './codegen/api/datasource'
import * as codegenRuntime from './codegen/api/dynamic-runtime'
import * as codegenGeneration from './codegen/api/generation'
import * as codegenTable from './codegen/api/table'
import * as codegenTemplate from './codegen/api/template'
import * as workflowBarrel from './workflow/api'
import * as workflowDefinition from './workflow/api/definition'
import * as workflowInstance from './workflow/api/instance'
import * as workflowTodo from './workflow/api/todo'

vi.mock('@/api/request', () => ({
  requestClient: {
    get: (_url: string, _config?: AxiosRequestConfig) => Promise.resolve(null),
    post: () => Promise.resolve(null),
    put: () => Promise.resolve(null),
    delete: () => Promise.resolve(undefined),
  },
}))

type Namespace = Record<string, unknown>

/** 模块桶 → 它应当聚合的子模块清单 */
const barrels: [string, Namespace, [string, Namespace][]][] = [
  ['ai/api', aiBarrel, [
    ['ai.enums', aiEnums],
    ['assistant', aiAssistant],
    ['knowledge', aiKnowledge],
    ['knowledge.enums', knowledgeEnums],
    ['prompt', aiPrompt],
    ['provider', aiProvider],
  ]],
  ['codegen/api', codegenBarrel, [
    ['codegen-history', codegenHistory],
    ['codegen-table-column', codegenColumn],
    ['codegen.enums', codegenEnums],
    ['datasource', codegenDatasource],
    ['dynamic-runtime', codegenRuntime],
    ['generation', codegenGeneration],
    ['table', codegenTable],
    ['template', codegenTemplate],
  ]],
  ['workflow/api', workflowBarrel, [
    ['definition', workflowDefinition],
    ['instance', workflowInstance],
    ['todo', workflowTodo],
  ]],
]

function runtimeExports(namespace: Namespace) {
  return Object.keys(namespace).filter(key => key !== 'default')
}

describe('模块 API 桶文件的聚合完整性', () => {
  it('每个子模块的运行时导出都能从模块桶取到，没有被同名导出吞掉', () => {
    const missing: string[] = []
    for (const [barrelName, barrel, children] of barrels) {
      for (const [childName, child] of children) {
        for (const name of runtimeExports(child)) {
          if (!(name in barrel)) {
            missing.push(`${barrelName} 缺 ${childName}.${name}`)
          }
        }
      }
    }

    expect(missing).toEqual([])
  })

  it('从桶里取到的是子模块的同一个绑定', () => {
    const mismatched: string[] = []
    for (const [barrelName, barrel, children] of barrels) {
      for (const [childName, child] of children) {
        for (const name of runtimeExports(child)) {
          if (barrel[name] !== child[name]) {
            mismatched.push(`${barrelName} → ${childName}.${name}`)
          }
        }
      }
    }

    expect(mismatched).toEqual([])
  })

  it('三个模块桶都聚合到了各自的门面对象', () => {
    expect(aiBarrel.aiAssistantApi).toBe(aiAssistant.aiAssistantApi)
    expect(aiBarrel.knowledgeApi).toBe(aiKnowledge.knowledgeApi)
    expect(aiBarrel.aiPromptApi).toBe(aiPrompt.aiPromptApi)
    expect(codegenBarrel.codeGenTableApi).toBe(codegenTable.codeGenTableApi)
    expect(codegenBarrel.codeGenerationApi).toBe(codegenGeneration.codeGenerationApi)
    expect(workflowBarrel.workflowInstanceApi).toBe(workflowInstance.workflowInstanceApi)
  })

  it('聊天助手提供方刻意不进 ai 桶——它只经 setup.ts 注册进聊天扩展点，桶里出现就说明有人绕过了注册', () => {
    expect(runtimeExports(aiBarrel)).not.toContain('chatAssistantProviderApi')
    expect(typeof chatAssistant.chatAssistantProviderApi.availableAssistants).toBe('function')
  })
})
