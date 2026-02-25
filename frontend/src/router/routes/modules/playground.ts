import type { RouteRecordRaw } from 'vue-router'

export const playgroundRoutes: RouteRecordRaw = {
  path: '/playground',
  name: 'Playground',
  redirect: '/playground/access',
  meta: {
    title: 'menu.playground',
    icon: 'lucide:flask-conical',
    order: 80,
  },
  children: [
    {
      path: 'access',
      name: 'PlaygroundAccess',
      component: () => import('@/views/playground/access/index.vue'),
      meta: {
        title: 'menu.playground_access',
        icon: 'lucide:shield-check',
      },
    },
    {
      path: 'theme',
      name: 'PlaygroundTheme',
      component: () => import('@/views/playground/theme/index.vue'),
      meta: {
        title: 'menu.playground_theme',
        icon: 'lucide:palette',
      },
    },
    {
      path: 'table',
      name: 'PlaygroundTable',
      redirect: '/playground/table/basic',
      meta: {
        title: 'menu.playground_table',
        icon: 'lucide:table',
      },
      children: [
        {
          path: 'basic',
          name: 'PlaygroundTableBasic',
          component: () => import('@/views/playground/table/basic/index.vue'),
          meta: { title: 'menu.playground_table_basic', icon: 'lucide:table-2' },
        },
        {
          path: 'advanced',
          name: 'PlaygroundTableAdvanced',
          component: () => import('@/views/playground/table/advanced/index.vue'),
          meta: { title: 'menu.playground_table_advanced', icon: 'lucide:table-properties' },
        },
        {
          path: 'virtual',
          name: 'PlaygroundTableVirtual',
          component: () => import('@/views/playground/table/virtual/index.vue'),
          meta: { title: 'menu.playground_table_virtual', icon: 'lucide:rows-3' },
        },
      ],
    },
    {
      path: 'form',
      name: 'PlaygroundForm',
      redirect: '/playground/form/basic',
      meta: {
        title: 'menu.playground_form',
        icon: 'lucide:clipboard-list',
      },
      children: [
        {
          path: 'basic',
          name: 'PlaygroundFormBasic',
          component: () => import('@/views/playground/form/basic/index.vue'),
          meta: { title: 'menu.playground_form_basic', icon: 'lucide:file-text' },
        },
        {
          path: 'step',
          name: 'PlaygroundFormStep',
          component: () => import('@/views/playground/form/step/index.vue'),
          meta: { title: 'menu.playground_form_step', icon: 'lucide:list-ordered' },
        },
        {
          path: 'dynamic',
          name: 'PlaygroundFormDynamic',
          component: () => import('@/views/playground/form/dynamic/index.vue'),
          meta: { title: 'menu.playground_form_dynamic', icon: 'lucide:plus-circle' },
        },
      ],
    },
    {
      path: 'cache',
      name: 'PlaygroundCache',
      component: () => import('@/views/playground/cache/index.vue'),
      meta: {
        title: 'menu.playground_cache',
        icon: 'lucide:database',
      },
    },
  ],
}
