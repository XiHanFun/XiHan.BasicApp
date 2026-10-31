/** 字典显示契约：预设/自定义色、布尔false、格式化导出与原值保持一致。 */
import type { VNode } from 'vue'
import { describe, expect, it } from 'vitest'
import { dictTagCss, dictTagIcon, renderDictTag } from '../common/dict-tag'
import { formatFieldText, renderFieldCell } from './renderer'

describe('系统字典标签样式', () => {
  it('字典名称和样式驱动渲染，需要审核true不沿用默认成功绿', () => {
    const field = { key: 'held', title: '需要审核', dataType: 'boolean' as const, dictCode: 'sample_review', options: [{ value: 1, label: '需审核', tagStyle: { tone: 'warning' as const, backgroundColor: '#FBEAC1', textColor: '#643A10' } }, { value: 0, label: '无需审核', tagStyle: { tone: 'info' as const } }] }
    const tag = renderFieldCell(field, { held: true }) as VNode
    expect(tag.props).toMatchObject({ tone: 'warning', style: { '--xh-tag-bg': '#FBEAC1', '--xh-tag-fg': '#643A10' } })
    expect(renderFieldCell(field, { held: false })).toBeTruthy()
    expect(formatFieldText(field, { held: null })).toBe('-')
  })
  it('自定义渲染与非字典字符串保持原行为', () => {
    expect(renderFieldCell({ key: 'name', title: '名称', dataType: 'string', dictCode: 'sample', render: () => 'custom' }, { name: 'a' })).toBe('custom')
    expect(renderFieldCell({ key: 'name', title: '名称', dataType: 'string' }, { name: 'a' })).toBe('a')
  })
  it('未知值保留原码并使用中性色，不默认标成功', () => {
    const field = { key: 'state', title: '状态', dataType: 'enum' as const, dictCode: 'sample_status', options: [] }
    expect((renderFieldCell(field, { state: 'FUTURE' }) as VNode).props?.tone).toBe('neutral')
    const tag = renderFieldCell(field, { state: 'FUTURE' }) as VNode
    const children = (tag.children as { default: () => VNode[] }).default()
    expect((children[1]!.children as { default: () => string }).default()).toBe('FUTURE')
  })
  it('只接受公开色槽和六位颜色，不透传任意CSS', () => {
    expect(dictTagCss({ backgroundColor: 'url(https://example.test)', textColor: '#112233;display:none' })).toEqual({})
    expect(dictTagCss(null)).toEqual({})
  })
  it('成功与信息保留不同预设，并以不同的装饰图形辅助辨识', () => {
    const success = renderDictTag('已完成', { tone: 'success' }) as VNode
    const info = renderDictTag('已接收', { tone: 'info' }) as VNode
    expect(success.props?.style).not.toEqual(info.props?.style)
    const successChildren = (success.children as { default: () => VNode[] }).default()
    const infoChildren = (info.children as { default: () => VNode[] }).default()
    expect(successChildren[0]?.props).toMatchObject({ 'icon': 'lucide:check', 'aria-hidden': true })
    expect(infoChildren[0]?.props).toMatchObject({ 'icon': 'lucide:info', 'aria-hidden': true })
  })
  it('人工颜色优先且不污染下一枚标签的预设', () => {
    const before = dictTagCss({ tone: 'info' })
    expect(dictTagCss({ tone: 'info', backgroundColor: '#E0F2FE', textColor: '#075985' })).toEqual({ '--xh-tag-bg': '#E0F2FE', '--xh-tag-fg': '#075985' })
    expect(dictTagCss({ tone: 'info' })).toEqual(before)
    expect(dictTagCss({ tone: 'info', backgroundColor: 'url(https://example.test)' })).toEqual(before)
  })
  it('关闭图标不保留空位，不影响名称、颜色和导出文字', () => {
    const field = { key: 'state', title: '状态', dataType: 'enum' as const, dictCode: 'state', options: [{ value: 'DONE', label: '已完成', tagStyle: { tone: 'success' as const, showIcon: false, icon: 'ep:check' } }] }
    const tag = renderFieldCell(field, { state: 'DONE' }) as VNode
    expect((tag.children as { default: () => VNode[] }).default()).toHaveLength(1)
    expect(tag.props?.tone).toBe('success')
    expect(formatFieldText(field, { state: 'DONE' })).toBe('已完成')
    expect(dictTagIcon(field.options[0]?.tagStyle)).toBeNull()
    expect(dictTagIcon({ ...field.options[0]?.tagStyle, showIcon: true })).toBe('ep:check')
  })
  it('自定义选择优先于配色，清除后恢复当前配色默认，非法标识不透传', () => {
    expect(dictTagIcon({ tone: 'warning', icon: 'carbon:checkmark' })).toBe('carbon:checkmark')
    expect(dictTagIcon({ tone: 'success', icon: 'carbon:checkmark' })).toBe('carbon:checkmark')
    expect(dictTagIcon({ tone: 'success', icon: null })).toBe('lucide:check')
    expect(dictTagIcon({ tone: 'success', icon: 'https://example.test/icon.svg' })).toBe('lucide:check')
    expect(dictTagIcon({ tone: 'info', icon: 'lucide:<svg>' })).toBe('lucide:info')
  })
  it.each([
    [undefined, 'lucide:circle'],
    [null, 'lucide:circle'],
    ['neutral', 'lucide:circle'],
    ['success', 'lucide:check'],
    ['info', 'lucide:info'],
    ['brand', 'lucide:hourglass'],
    ['warning', 'lucide:triangle-alert'],
    ['danger', 'lucide:x'],
  ] as const)('预设%s与默认状态均保留一个装饰图标', (tone, icon) => {
    const tag = renderDictTag('原有名称', { tone }) as VNode
    const children = (tag.children as { default: () => VNode[] }).default()
    expect(children).toHaveLength(2)
    expect(children[0]?.props).toMatchObject({ 'icon': icon, 'aria-hidden': true })
    expect(tag.props?.tone).toBe(tone ?? 'neutral')
  })
})
