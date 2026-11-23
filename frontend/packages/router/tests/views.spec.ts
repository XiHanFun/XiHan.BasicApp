import { describe, expect, it } from 'vitest'
import { prepareViewSources, registerViewSource, resolveView, viewKeys } from '~/router/views'

describe('view resolution', () => {
  it('maps a kebab component path to the module view file', () => {
    expect(viewKeys()).toContain('identity/users/index')
    expect(typeof resolveView('identity/users/index')).toBe('function')
  })

  it('maps a shell page that belongs to no module to its core view', () => {
    expect(viewKeys()).toContain('about/project/index')
    expect(typeof resolveView('about/project/index')).toBe('function')
  })

  it('fails loudly for an unknown component path', () => {
    expect(() => resolveView('identity/nope/index')).toThrow(/is not found/)
  })

  it('hands only the paths the module views lack to the view sources, then resolves them there', async () => {
    const prepared: string[][] = []
    const loader = () => Promise.resolve({ default: {} })
    registerViewSource({
      prepare: async (components) => {
        prepared.push([...components])
      },
      resolve: component => component === 'plugins/crm/customers/index'
        ? loader
        : undefined,
    })

    await prepareViewSources(['identity/users/index', 'about/project/index'])
    expect(prepared).toEqual([])

    await prepareViewSources(['identity/users/index', 'plugins/crm/customers/index', 'plugins/crm/customers/index'])
    expect(prepared).toEqual([
      ['plugins/crm/customers/index'],
    ])
    expect(resolveView('plugins/crm/customers/index')).toBe(loader)
    expect(typeof resolveView('identity/users/index')).toBe('function')
    expect(() => resolveView('plugins/crm/orders/index')).toThrow(/is not found/)
  })
})
