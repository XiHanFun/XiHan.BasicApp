import type { RouteRecordRaw } from 'vue-router'

export const systemRoutes: RouteRecordRaw = {
  path: '/system',
  name: 'System',
  redirect: '/system/user',
  meta: {
    title: 'menu.system',
    icon: 'lucide:settings',
    order: 99,
    roles: ['admin'],
  },
  children: [
    {
      path: 'user',
      name: 'SystemUser',
      component: () => import('@/views/system/user/index.vue'),
      meta: {
        title: 'menu.user',
        icon: 'lucide:users',
        keepAlive: true,
        permissions: ['system:user:list'],
      },
    },
    {
      path: 'role',
      name: 'SystemRole',
      component: () => import('@/views/system/role/index.vue'),
      meta: {
        title: 'menu.role',
        icon: 'lucide:shield',
        keepAlive: true,
        permissions: ['system:role:list'],
      },
    },
    {
      path: 'menu',
      name: 'SystemMenu',
      component: () => import('@/views/system/menu/index.vue'),
      meta: {
        title: 'menu.menu',
        icon: 'lucide:list-tree',
        keepAlive: true,
        permissions: ['system:menu:list'],
      },
    },
    {
      path: 'permission',
      name: 'SystemPermission',
      component: () => import('@/views/system/permission/index.vue'),
      meta: {
        title: 'menu.permission',
        icon: 'lucide:key-round',
        keepAlive: true,
      },
    },
    {
      path: 'department',
      name: 'SystemDepartment',
      component: () => import('@/views/system/department/index.vue'),
      meta: {
        title: 'menu.department',
        icon: 'lucide:building-2',
        keepAlive: true,
      },
    },
    {
      path: 'tenant',
      name: 'SystemTenant',
      component: () => import('@/views/system/tenant/index.vue'),
      meta: {
        title: 'menu.tenant',
        icon: 'lucide:blocks',
        keepAlive: true,
      },
    },
    // {
    //   path: 'logs',
    //   name: 'SystemLogs',
    //   component: () => import('@/views/system/logs/index.vue'),
    //   meta: {
    //     title: 'menu.logs',
    //     icon: 'lucide:history',
    //   },
    // },
  ],
}
