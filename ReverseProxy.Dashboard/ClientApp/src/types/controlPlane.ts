export interface CurrentUser {
  id: string
  userName: string
  displayName: string
  roles: string[]
}

export interface RuntimeStatus {
  revisionId: string
  revisionNumber: number
  contentHash: string
  publishedAt: string
  loadedAt: string
  routeCount: number
  clusterCount: number
  status: string
}

export interface RuntimeConfiguration {
  revisionId: string
  revisionNumber: number
  contentHash: string
  publishedAt: string
  document: ProxyConfigDocument
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
}

export interface DraftSummary {
  id: string
  name: string
  baseRevisionNumber: number
  rowVersion: number
  contentHash: string
  routeCount: number
  clusterCount: number
  updatedBy: string
  updatedAt: string
}

export interface RouteMatch {
  path?: string
  hosts?: string[]
  methods?: string[]
  [key: string]: unknown
}

export interface RouteConfig {
  routeId: string
  clusterId?: string
  order?: number
  match: RouteMatch
  transforms?: Array<Record<string, string>>
  metadata?: Record<string, string>
  [key: string]: unknown
}

export interface DestinationConfig {
  address: string
  health?: string
  metadata?: Record<string, string>
  [key: string]: unknown
}

export interface ClusterConfig {
  clusterId: string
  loadBalancingPolicy?: string
  destinations: Record<string, DestinationConfig>
  metadata?: Record<string, string>
  [key: string]: unknown
}

export interface ProxyConfigDocument {
  schemaVersion: number
  routes: RouteConfig[]
  clusters: ClusterConfig[]
}

export interface Draft extends Omit<DraftSummary, 'routeCount' | 'clusterCount'> {
  document: ProxyConfigDocument
}

export interface ValidationError {
  code: string
  path: string
  message: string
}

export interface ValidationResult {
  isValid: boolean
  contentHash: string
  errors: ValidationError[]
}

export interface ResourceDiff {
  added: string[]
  removed: string[]
  modified: string[]
}

export interface ConfigDiff {
  baseHash: string
  candidateHash: string
  hasChanges: boolean
  routes: ResourceDiff
  clusters: ResourceDiff
}

export interface ConfigOperation {
  id: string
  type: string
  status: string
  revisionId: string
  revisionNumber: number
  createdAt: string
  completedAt?: string
  errorCode?: string
  errorMessage?: string
}