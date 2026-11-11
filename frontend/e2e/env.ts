import process from 'node:process'

/**
 * 端到端用例的环境：自起的后端与前端各占一组端口，不碰本机 9710 / 9100 上的开发服务
 * 库是单独的 xihan_basicapp_e2e（Development 环境启动时建库、建表、播种），E2E_DATABASE 可换成别的连接串
 */
export const FRONTEND_PORT = 9130
export const BACKEND_PORT = 9730
export const BACKEND_GRPC_PORT = 9731

export const FRONTEND_URL = `http://localhost:${FRONTEND_PORT}`
export const BACKEND_URL = `http://localhost:${BACKEND_PORT}`

export const DATABASE = process.env.E2E_DATABASE
  ?? 'Host=127.0.0.1;Port=5432;Database=xihan_basicapp_e2e;Username=postgres;Password=postgres'

/** 种子写超管时的初始密码；库里已有超管时种子不改它 */
export const SUPER_ADMIN = { username: 'superadmin', password: 'E2e-SuperAdmin@2026' }

/** 用例里开通的租户管理员与成员的初始密码 */
export const INITIAL_PASSWORD = 'E2e-Initial@2026'
