import type { RequestClient } from '~/request'
import { createRequestClient } from '~/request'

const baseURL = String(import.meta.env.VITE_API_BASE_URL ?? '')
const apiPrefix = String(import.meta.env.VITE_API_PREFIX ?? '/api')

export const requestClient: RequestClient = createRequestClient(baseURL, apiPrefix)

export type { RequestClient }
