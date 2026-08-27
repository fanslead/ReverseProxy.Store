import { apiRequest } from './http'
import type {
  ConfigDiff,
  ConfigOperation,
  CurrentUser,
  Draft,
  DraftSummary,
  PagedResponse,
  ProxyConfigDocument,
  RuntimeConfiguration,
  RuntimeStatus,
  ValidationResult
} from '@/types/controlPlane'

export const controlPlane = {
  me: () => apiRequest<CurrentUser>('/api/v1/auth/me'),
  login: (userName: string, password: string, rememberMe: boolean) =>
    apiRequest<CurrentUser>('/api/v1/auth/login', {
      method: 'POST',
      csrf: true,
      body: { userName, password, rememberMe }
    }),
  logout: () => apiRequest<void>('/api/v1/auth/logout', { method: 'POST', csrf: true }),
  runtimeStatus: () => apiRequest<RuntimeStatus>('/api/v1/runtime/status'),
  runtimeConfiguration: () => apiRequest<RuntimeConfiguration>('/api/v1/runtime/configuration'),
  listDrafts: (search = '', page = 1, pageSize = 25) => {
    const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (search.trim()) query.set('search', search.trim())
    return apiRequest<PagedResponse<DraftSummary>>(`/api/v1/drafts?${query}`)
  },
  createDraft: (name: string) => apiRequest<Draft>('/api/v1/drafts', {
    method: 'POST',
    csrf: true,
    body: { name }
  }),
  getDraft: (id: string) => apiRequest<Draft>(`/api/v1/drafts/${id}`),
  updateDraft: (id: string, name: string, document: ProxyConfigDocument, etag: string) =>
    apiRequest<Draft>(`/api/v1/drafts/${id}`, {
      method: 'PUT',
      csrf: true,
      headers: { 'If-Match': etag },
      body: { name, document }
    }),
  deleteDraft: (id: string, etag: string) => apiRequest<void>(`/api/v1/drafts/${id}`, {
    method: 'DELETE',
    csrf: true,
    headers: { 'If-Match': etag }
  }),
  validateDraft: (id: string) => apiRequest<ValidationResult>(`/api/v1/drafts/${id}/validate`, {
    method: 'POST',
    csrf: true
  }),
  diffDraft: (id: string) => apiRequest<ConfigDiff>(`/api/v1/drafts/${id}/diff`),
  publishDraft: (id: string, etag: string) => apiRequest<ConfigOperation>(`/api/v1/drafts/${id}/publish`, {
    method: 'POST',
    csrf: true,
    headers: { 'If-Match': etag }
  }),
  operation: (id: string) => apiRequest<ConfigOperation>(`/api/v1/operations/${id}`)
}