export const API_CONTRACT = {
  auth: {
    login: '/auth/login',
    refreshToken: '/auth/refresh-token',
    logout: '/auth/logout',
    currentUser: '/auth/current-user',
    userMenus: '/auth/menus',
    codes: '/auth/codes',
    captcha: '/auth/captcha',
  },
  system: {
    users: '/system/users',
    roles: '/system/roles',
    permissions: '/system/permissions',
    menus: '/system/menus',
    departments: '/system/departments',
    tenants: '/system/tenants',
  },
  logs: {
    access: '/access-logs',
    operation: '/operation-logs',
    exception: '/exception-logs',
    audit: '/audit-logs',
  },
} as const

export type ApiContract = typeof API_CONTRACT
