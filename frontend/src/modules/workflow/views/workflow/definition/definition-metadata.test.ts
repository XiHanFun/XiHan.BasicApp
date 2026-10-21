/**
 * 流程定义元数据（definition-metadata.ts）的单元测试。
 *
 * 锁定：从引擎定义 JSON 读出编码、名称、分类、描述；新建是一份没有节点与连线的空白流程，编码是审批业务时带上它声明的变量；
 * 编辑只改名称、分类、描述，编码与其它字段原样；选审批业务时编码换成业务编码，名称空着才带业务名。
 */
import type { WorkflowBusinessTypeDto } from '../../../api'
import { describe, expect, it } from 'vitest'
import {
  applyDefinitionMetadata,
  bindDefinitionBusiness,
  createDefinitionJson,
  emptyDefinitionMetadata,
  isDefinitionMetadataComplete,
  readDefinitionMetadata,
} from './definition-metadata'

const leave = {
  code: 'leave-request',
  name: '请假审批',
  variables: [
    { name: 'days', label: '天数', valueType: 'number' },
    { name: 'applicant', label: '申请人', valueType: 'user' },
  ],
  outcomes: ['approved', 'rejected'],
} as unknown as WorkflowBusinessTypeDto

describe('读出', () => {
  it('取编码、名称、分类、描述，缺的当空串', () => {
    expect(readDefinitionMetadata('{"code":"a","name":"A","category":null,"nodes":[]}')).toEqual({
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
  it('空白流程：去掉首尾空白，空的分类与描述记为 null，没有节点与连线', () => {
    const json = createDefinitionJson({ code: ' expense ', name: ' 报销 ', category: ' ', description: '' }, [leave])

    expect(JSON.parse(json)).toEqual({
      code: 'expense',
      name: '报销',
      category: null,
      description: null,
      variables: [],
      nodes: [],
      transitions: [],
    })
  })

  it('编码是审批业务时带上它声明的变量', () => {
    const json = createDefinitionJson({ ...emptyDefinitionMetadata(), code: 'leave-request', name: '请假审批' }, [leave])

    expect(JSON.parse(json).variables).toEqual([
      { name: 'days', required: false, defaultValue: null, description: '天数' },
      { name: 'applicant', required: false, defaultValue: null, description: '申请人' },
    ])
  })
})

describe('编辑', () => {
  it('只改名称、分类、描述，编码与节点等原样', () => {
    const original = JSON.stringify({ code: 'leave-request', name: '请假', version: 3, nodes: [{ id: 'start' }], extraProperties: { designerLayout: '{}' } })

    const json = applyDefinitionMetadata(original, { code: 'other', name: ' 请假审批 ', category: '人事', description: ' 年假与事假 ' })

    expect(JSON.parse(json)).toEqual({
      code: 'leave-request',
      name: '请假审批',
      version: 3,
      category: '人事',
      description: '年假与事假',
      nodes: [{ id: 'start' }],
      extraProperties: { designerLayout: '{}' },
    })
  })
})

describe('选审批业务', () => {
  it('编码换成业务编码，名称空着才带业务名', () => {
    expect(bindDefinitionBusiness(emptyDefinitionMetadata(), leave)).toMatchObject({ code: 'leave-request', name: '请假审批' })
    expect(bindDefinitionBusiness({ ...emptyDefinitionMetadata(), name: '年假' }, leave)).toMatchObject({ code: 'leave-request', name: '年假' })
  })
})

describe('必填', () => {
  it('编码与名称去掉空白后都不为空才算填好', () => {
    expect(isDefinitionMetadataComplete(emptyDefinitionMetadata())).toBe(false)
    expect(isDefinitionMetadataComplete({ ...emptyDefinitionMetadata(), code: 'a', name: ' ' })).toBe(false)
    expect(isDefinitionMetadataComplete({ ...emptyDefinitionMetadata(), code: 'a', name: 'A' })).toBe(true)
  })
})
