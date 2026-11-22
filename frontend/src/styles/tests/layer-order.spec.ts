import { readFileSync } from 'node:fs'
import { describe, expect, it } from 'vitest'

/** 取出第一条层序声明里的层名 */
function layerOrder(file: string): string[] {
  const source = readFileSync(new URL(file, import.meta.url), 'utf8')
  const names = /@layer ([\w.,\s]+);/.exec(source)?.[1]
  if (!names)
    return []
  return names.split(',').map(name => name.trim())
}

describe('样式层序', () => {
  it('index.html 先于全部样式表声明的层序与样式总入口一致', () => {
    const declared = layerOrder('../index.css')
    expect(declared).toContain('components')
    expect(layerOrder('../../../index.html')).toEqual(declared)
  })
})
