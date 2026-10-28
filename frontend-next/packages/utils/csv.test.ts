/**
 * packages/utils/csv.ts 单元测试。
 *
 * 职责边界：parseCsvRows 是应用层唯一的 CSV 入口（papaparse 被收敛在此包内），
 * 导入的表格数据全部经它落地，属注入面。用例覆盖分隔符探测、引号包裹、内嵌引号转义、
 * 内嵌逗号与换行、CRLF、BOM、空行跳过，以及公式注入前缀不被处理的现状。
 */
import { describe, expect, it } from 'vitest'
import { parseCsvRows } from './csv'

describe('parseCsvRows', () => {
  it('逗号分隔的多行文本按行拆成二维数组，首行即表头行', () => {
    expect(parseCsvRows('name,age\n羲和,3')).toEqual([
      ['name', 'age'],
      ['羲和', '3'],
    ])
  })

  it('回车换行 CRLF 与 LF 解析结果一致', () => {
    expect(parseCsvRows('a,b\r\nc,d')).toEqual(parseCsvRows('a,b\nc,d'))
  })

  it('结尾多余的换行不会产出一行空记录', () => {
    expect(parseCsvRows('a,b\r\nc,d\r\n')).toEqual([
      ['a', 'b'],
      ['c', 'd'],
    ])
  })

  it('中间的连续空行被跳过，行号不留空洞', () => {
    expect(parseCsvRows('a,b\n\n\nc,d')).toEqual([
      ['a', 'b'],
      ['c', 'd'],
    ])
  })

  it('文件头 UTF-8 BOM 被剥离，不会污染第一个表头字段', () => {
    const rows = parseCsvRows('﻿name,age\n羲和,3')
    expect(rows[0]).toEqual(['name', 'age'])
  })

  it('双引号包裹的字段可以内嵌逗号而不被拆列', () => {
    expect(parseCsvRows('a,"b,c",d')).toEqual([['a', 'b,c', 'd']])
  })

  it('字段内的双引号以两个双引号转义，还原为单个双引号', () => {
    expect(parseCsvRows('a,"他说""你好""",c')).toEqual([['a', '他说"你好"', 'c']])
  })

  it('引号内的换行属于字段内容，不切分成新行', () => {
    expect(parseCsvRows('a,"第一行\n第二行",c')).toEqual([['a', '第一行\n第二行', 'c']])
  })

  it('自动探测分隔符，分号与制表符同样能解析', () => {
    expect(parseCsvRows('a;b\n1;2')).toEqual([
      ['a', 'b'],
      ['1', '2'],
    ])
    expect(parseCsvRows('a\tb\n1\t2')).toEqual([
      ['a', 'b'],
      ['1', '2'],
    ])
  })

  it('空文本返回空数组而不是包含一行空串', () => {
    expect(parseCsvRows('')).toEqual([])
  })

  it('纯空白文本保留为单行单列，空白不会被自动去除', () => {
    expect(parseCsvRows('   ')).toEqual([['   ']])
  })

  it('单元格值一律保持字符串，数字与布尔不会被转型', () => {
    expect(parseCsvRows('1,true,null')).toEqual([['1', 'true', 'null']])
  })

  it('连续分隔符产出空串单元格，列数不塌陷', () => {
    expect(parseCsvRows('a,,c')).toEqual([['a', '', 'c']])
  })

  it('各行列数不一致时按各自实际列数返回，不做补齐或截断', () => {
    expect(parseCsvRows('a,b,c\n1,2')).toEqual([
      ['a', 'b', 'c'],
      ['1', '2'],
    ])
  })

  it('以等号开头的公式注入串原样返回，转义责任在写出侧而非解析侧', () => {
    expect(parseCsvRows('=1+1,@SUM(A1),+cmd,-cmd')).toEqual([
      ['=1+1', '@SUM(A1)', '+cmd', '-cmd'],
    ])
  })

  it('emoji 与全角标点不会被拆列或改写', () => {
    expect(parseCsvRows('🙂,中文，标点')).toEqual([['🙂', '中文，标点']])
  })

  it('超长单元格内容完整返回，不被截断', () => {
    const long = 'x'.repeat(20000)
    expect(parseCsvRows(`${long},b`)[0]?.[0]).toHaveLength(20000)
  })
})
