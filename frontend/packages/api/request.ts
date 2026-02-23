import { createRequestClient } from '~/request'

const baseURL = import.meta.env.VITE_API_BASE_URL || ''
const apiPrefix = import.meta.env.VITE_API_PREFIX || '/api'

export const requestClient = createRequestClient(baseURL, apiPrefix)

export default requestClient
