import { describe, expect, it } from 'vitest'
import { configureRequest } from '~/request'
import { buildArtifactTree, fileIconOf, templatesResource } from './use-templates'

describe('buildArtifactTree', () => {
  it('按路径组成目录树：目录在前、按名称排，文件节点带产物下标与只首次写的标记', () => {
    const tree = buildArtifactTree([
      { artifact: { path: 'backend/src/Shop/Domain/Entities/Product.Generated.cs', side: 'Backend', templateCode: 'backend.entity', writeMode: 'AlwaysOverwrite', content: '' }, index: 0 },
      { artifact: { path: 'backend/src/Shop/Domain/Entities/Product.cs', side: 'Backend', templateCode: 'backend.entity.manual', writeMode: 'WriteOnce', content: '' }, index: 1 },
      { artifact: { path: 'backend/src/Shop/ShopFeature.cs', side: 'Backend', templateCode: 'backend.package.feature', writeMode: 'WriteOnce', content: '' }, index: 2 },
      { artifact: { path: '_registration/products.md', side: 'Registration', templateCode: null, writeMode: 'AlwaysOverwrite', content: '' }, index: 3 },
    ])
    expect(tree.map(node => node.label)).toEqual(['_registration', 'backend'])
    const shop = tree[1]!.children![0]!.children![0]!
    expect(shop.children!.map(node => node.label)).toEqual(['Domain', 'ShopFeature.cs'])
    const entities = shop.children![0]!.children![0]!.children!
    expect(entities.map(node => [node.label, node.index, node.writeOnce])).toEqual([['Product.cs', 1, true], ['Product.Generated.cs', 0, false]])
  })

  it('文件类型图标按扩展名', () => {
    expect(fileIconOf('A.cs')).toBe('mdi:language-csharp')
    expect(fileIconOf('index.vue')).toBe('mdi:vuejs')
    expect(fileIconOf('X.csproj')).toBe('lucide:file-code')
    expect(fileIconOf('README')).toBe('lucide:file')
  })
})

describe('templatesResource', () => {
  it('生成模式、写入方式与状态多选合成 in', async () => {
    const calls: { url: string, body: unknown }[] = []
    configureRequest({
      baseUrl: '',
      getToken: () => 't',
      fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
        calls.push({ url: String(url), body: init?.body ? JSON.parse(String(init.body)) : undefined })
        return new Response(JSON.stringify({ items: [], total: 0, page: { index: 1, size: 20 } }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }) as unknown as typeof fetch,
    })
    await templatesResource().page!({
      page: 1,
      pageSize: 20,
      filters: {},
      conditionFilters: [{ field: 'writeMode', operator: 3000, values: ['WriteOnce'] }],
      sorts: [],
    } as never)
    expect(calls[0]).toMatchObject({ url: '/api/codegen-templates:list', body: { filters: [{ field: 'writeMode', operator: 'in', value: 'WriteOnce' }] } })
  })
})
