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
    {
      path: 'config',
      name: 'SystemConfig',
      component: () => import('@/views/system/config/index.vue'),
      meta: {
        title: 'menu.config',
        icon: 'lucide:settings-2',
        keepAlive: true,
      },
    },
    {
      path: 'dict',
      name: 'SystemDict',
      component: () => import('@/views/system/dict/index.vue'),
      meta: {
        title: 'menu.dict',
        icon: 'lucide:book-open',
        keepAlive: true,
      },
    },
    {
      path: 'notification',
      name: 'SystemNotification',
      component: () => import('@/views/system/notification/index.vue'),
      meta: {
        title: 'menu.notification',
        icon: 'lucide:bell-ring',
        keepAlive: true,
      },
    },
    {
      path: 'file',
      name: 'SystemFile',
      component: () => import('@/views/system/file/index.vue'),
      meta: {
        title: 'menu.file',
        icon: 'lucide:folder-open',
        keepAlive: true,
      },
    },
    {
      path: 'email',
      name: 'SystemEmail',
      component: () => import('@/views/system/email/index.vue'),
      meta: {
        title: 'menu.email',
        icon: 'lucide:mail',
        keepAlive: true,
      },
    },
    {
      path: 'sms',
      name: 'SystemSms',
      component: () => import('@/views/system/sms/index.vue'),
      meta: {
        title: 'menu.sms',
        icon: 'lucide:message-square',
        keepAlive: true,
      },
    },
    {
      path: 'task',
      name: 'SystemTask',
      component: () => import('@/views/system/task/index.vue'),
      meta: {
        title: 'menu.task',
        icon: 'lucide:calendar-clock',
        keepAlive: true,
      },
    },
    {
      path: 'oauth-app',
      name: 'SystemOAuthApp',
      component: () => import('@/views/system/oauth-app/index.vue'),
      meta: {
        title: 'menu.oauthApp',
        icon: 'lucide:shield-check',
        keepAlive: true,
      },
    },
    {
      path: 'review',
      name: 'SystemReview',
      component: () => import('@/views/system/review/index.vue'),
      meta: {
        title: 'menu.review',
        icon: 'lucide:clipboard-check',
        keepAlive: true,
      },
    },
    {
      path: 'user-session',
      name: 'SystemUserSession',
      component: () => import('@/views/system/user-session/index.vue'),
      meta: {
        title: 'menu.userSession',
        icon: 'lucide:laptop',
        keepAlive: true,
      },
    },
    {
      path: 'logs',
      name: 'SystemLogs',
      component: () => import('@/views/system/logs/index.vue'),
      meta: {
        title: 'menu.logs',
        icon: 'lucide:history',
      },
    },
  ],
}
