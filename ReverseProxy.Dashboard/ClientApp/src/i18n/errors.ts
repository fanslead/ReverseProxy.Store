import { i18n } from './index'
import { ApiError } from '@/services/http'
import type { ValidationError } from '@/types/controlPlane'

export function localizeApiError(reason: unknown, fallbackKey: string): string {
  const { t, te } = i18n.global
  if (reason instanceof ApiError) {
    const codeKey = reason.code ? `apiError.${reason.code}` : ''
    if (codeKey && te(codeKey)) return t(codeKey)

    const statusKey = `apiError.status${reason.status}`
    if (te(statusKey)) return t(statusKey)
  }

  return t(fallbackKey)
}

export function localizeValidationError(error: ValidationError): string {
  const { t } = i18n.global
  if (error.code === 'unsupported_schema_version') return t('validation.unsupportedSchemaVersion')
  if (error.code === 'duplicate_cluster_id') return t('validation.duplicateClusterId')
  if (error.code === 'duplicate_route_id') return t('validation.duplicateRouteId')
  if (error.code === 'cluster_not_found') return t('validation.clusterNotFound')
  if (error.code === 'invalid_destination_address') return t('validation.invalidDestinationAddress')
  if (error.code === 'required') {
    if (error.path.endsWith('.clusterId')) return t('validation.clusterIdRequired')
    if (error.path.endsWith('.routeId')) return t('validation.routeIdRequired')
    if (error.path.endsWith('.destinations')) return t('validation.destinationNameRequired')
    if (error.path.endsWith('.address')) return t('validation.destinationAddressRequired')
  }

  return t('validation.unknown')
}