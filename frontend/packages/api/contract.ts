export const API_CONTRACT = {
  auth: {
    loginConfig: '/auth/loginConfig',
    login: '/auth/login',
    refreshToken: '/auth/refreshtoken',
    currentUser: '/auth/currentUser',
    permissions: '/auth/permissions',
    logout: '/auth/logout',
    codes: '/auth/permissioncodes',
    userMenus: '/menu/usermenus',
  },
  system: {
    users: '/system/users',
    roles: '/system/roles',
    permissions: '/system/permissions',
    menus: '/system/menus',
  },
  logs: {
    access: '/accesslogs',
    operation: '/operationlogs',
    exception: '/exceptionlogs',
    audit: '/auditlogs',
  },
} as const

export type ApiContract = typeof API_CONTRACT
