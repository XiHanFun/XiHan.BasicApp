/**
 * 流程定义元数据（definition-metadata.ts）的单元测试
 *
 * 锁定：从定义文档（WorkflowJson）读出编码（name）、名称（displayName）、分类、描述；新建是一份没有节点与连线的空白流程，
 * 编码是审批业务时带上它声明的变量；编辑只改名称、分类、描述，编码与其它字段原样；选审批业务时编码换成业务编码，名称空着才带业务名；
 * 编码要合框架的定义名语法
 */
import type { WorkflowBusinessType } from './definition-metadata'
import { describe, expect, it } from 'vitest'
import {
  applyDefinitionMetadata,
  bindDefinitionBusiness,
  createDefinitionJson,
  emptyDefinitionMetadata,
  isDefinitionMetadataComplete,
  isWorkflowName,
  readDefinitionMetadata,
} from './definition-metadata'

const leave: WorkflowBusinessType = {
  code: 'leave-request',
  nameKey: 'workflow.business.leave',
  formComponent: 'hr/leave-approval/index',
  variables: [
    { name: 'days', labelKey: 'hr.leave.days', valueType: 'number' },
    { name: 'applicant', labelKey: 'hr.leave.applicant', valueType: 'user' },
  ],
  outcomes: ['approved', 'rejected'],
}

const translate = (key: string) => `「${key}」`

describe('读出', () => {
  it('编码取 name、名称取 displayName，取分类、描述，缺的当空串', () => {
    expect(readDefinitionMetadata('{"name":"a","displayName":"A","category":null,"nodes":[]}')).toEqual({
      code: 'a',
      name: 'A',
      category: '',
      description: '',
    })
  })

  it('不是 JSON 对象时抛错', () => {
    expect(() => readDefinitionMetadata('[]')).toThrow()
  })
})

describe('新建', () => {
  it('空白流程：去掉首尾空白，空的分类与描述记为 null，没有节点与连线，ID 与版本留给后端', () => {
    const json = createDefinitionJson({
      code: ' expense ',
      name: ' 报销 ',
      category: ' ',
      description: '',
    }, [leave], translate)

    expect(JSON.parse(json)).toEqual({
      name: 'expense',
      displayName: '报销',
      category: null,
      description: null,
      variables: [],
      nodes: [],
      transitions: [],
    })
  })

  it('编码是审批业务时带上它声明的变量，说明是变量显示名的译文', () => {
    const json = createDefinitionJson({
      ...emptyDefinitionMetadata(),
      code: 'leave-request',
      name: '请假审批',
    }, [leave], translate)

    expect(JSON.parse(json).variables).toEqual([
      { name: 'days', required: false, description: '「hr.leave.days」' },
      { name: 'applicant', required: false, description: '「hr.leave.applicant」' },
    ])
  })
})

describe('编辑', () => {
  it('只改名称、分类、描述，编码与节点等原样', () => {
    const original = JSON.stringify({
      id: 'd-1',
      name: 'leave-request',
      displayName: '请假',
      version: 3,
      nodes: [
        { id: 'start' },
      ],
      properties: { designerLayout: {} },
    })

    const json = applyDefinitionMetadata(original, {
      code: 'other',
      name: ' 请假审批 ',
      category: '人事',
      description: ' 年假与事假 ',
    })

    expect(JSON.parse(json)).toEqual({
      id: 'd-1',
      name: 'leave-request',
      displayName: '请假审批',
      version: 3,
      category: '人事',
      description: '年假与事假',
      nodes: [
        { id: 'start' },
      ],
      properties: { designerLayout: {} },
    })
  })
})

describe('选审批业务', () => {
  it('编码换成业务编码，名称空着才带业务名', () => {
    expect(bindDefinitionBusiness(emptyDefinitionMetadata(), leave, '请假审批')).toMatchObject({ code: 'leave-request', name: '请假审批' })
    expect(bindDefinitionBusiness({
      ...emptyDefinitionMetadata(),
      name: '年假',
    }, leave, '请假审批')).toMatchObject({ code: 'leave-request', name: '年假' })
  })
})

describe('必填与编码语法', () => {
  it('编码合语法、名称去掉空白后不为空才算填好', () => {
    expect(isDefinitionMetadataComplete(emptyDefinitionMetadata())).toBe(false)
    expect(isDefinitionMetadataComplete({
      ...emptyDefinitionMetadata(),
      code: 'a',
      name: ' ',
    })).toBe(false)
    expect(isDefinitionMetadataComplete({
      ...emptyDefinitionMetadata(),
      code: 'a',
      name: 'A',
    })).toBe(true)
    expect(isDefinitionMetadataComplete({
      ...emptyDefinitionMetadata(),
      code: 'Leave',
      name: 'A',
    })).toBe(false)
  })

  it('小写字母或数字开头，其后是小写字母、数字、点、下划线与连字符，最长 128 个字符', () => {
    expect(['leave-request', 'hr.leave_v2', '9to5'].every(isWorkflowName)).toBe(true)
    expect([
      '',
      '-leave',
      'Leave',
      'leave request',
      'a'.repeat(129),
    ].some(isWorkflowName)).toBe(false)
    expect(isWorkflowName('a'.repeat(128))).toBe(true)
  })
})
