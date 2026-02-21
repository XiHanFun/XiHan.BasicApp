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
      component: () => import('@/views/playground/table/index.vue'),
      meta: {
        title: 'menu.playground_table',
        icon: 'lucide:table',
      },
    },
    {
      path: 'form',
      name: 'PlaygroundForm',
      component: () => import('@/views/playground/form/index.vue'),
      meta: {
        title: 'menu.playground_form',
        icon: 'lucide:clipboard-list',
      },
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
