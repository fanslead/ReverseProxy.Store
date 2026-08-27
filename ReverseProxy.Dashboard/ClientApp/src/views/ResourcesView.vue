<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { Message } from '@arco-design/web-vue'
import { Pencil, Plus, RefreshCw, Route as RouteIcon, Search, Server } from '@lucide/vue'
import CreateDraftModal from '@/components/CreateDraftModal.vue'
import { localizeApiError } from '@/i18n/errors'
import { controlPlane } from '@/services/controlPlane'
import { useSessionStore } from '@/stores/session'
import type { ClusterConfig, RouteConfig, RuntimeConfiguration } from '@/types/controlPlane'

type ResourceKind = 'routes' | 'clusters'
type ChangeIntent = 'add' | 'edit'

const props = defineProps<{ resourceKind: ResourceKind }>()
const router = useRouter()
const session = useSessionStore()
const { locale, t } = useI18n()

const configuration = ref<RuntimeConfiguration | null>(null)
const loading = ref(true)
const search = ref('')
const createVisible = ref(false)
const changeIntent = ref<ChangeIntent>('add')
const selectedResourceId = ref<string | null>(null)

const isRoutes = computed(() => props.resourceKind === 'routes')
const publishedFormatter = computed(() => new Intl.DateTimeFormat(locale.value, {
  year: 'numeric',
  month: 'short',
  day: 'numeric',
  hour: '2-digit',
  minute: '2-digit'
}))
const normalizedSearch = computed(() => search.value.trim().toLocaleLowerCase(locale.value))
const routes = computed(() => configuration.value?.document.routes ?? [])
const clusters = computed(() => configuration.value?.document.clusters ?? [])
const filteredRoutes = computed(() => routes.value.filter(route => {
  const query = normalizedSearch.value
  return !query || [route.routeId, route.match.path, route.clusterId, ...(route.match.methods ?? [])]
    .some(value => value?.toLocaleLowerCase(locale.value).includes(query))
}))
const filteredClusters = computed(() => clusters.value.filter(cluster => {
  const query = normalizedSearch.value
  const destinations = Object.entries(cluster.destinations).flatMap(([id, destination]) => [id, destination.address])
  return !query || [cluster.clusterId, cluster.loadBalancingPolicy, ...destinations]
    .some(value => value?.toLocaleLowerCase(locale.value).includes(query))
}))
const resultCount = computed(() => isRoutes.value ? filteredRoutes.value.length : filteredClusters.value.length)
const totalCount = computed(() => isRoutes.value ? routes.value.length : clusters.value.length)
const suggestedChangeName = computed(() => {
  if (changeIntent.value === 'add') {
    return t(isRoutes.value ? 'resources.changeName.addRoute' : 'resources.changeName.addCluster')
  }
  return t(isRoutes.value ? 'resources.changeName.editRoute' : 'resources.changeName.editCluster', {
    id: selectedResourceId.value
  })
})

async function load() {
  loading.value = true
  try {
    configuration.value = (await controlPlane.runtimeConfiguration()).data
  } catch (reason) {
    Message.error(localizeApiError(reason, 'resources.loadFailed'))
  } finally {
    loading.value = false
  }
}

function formatMethods(route: RouteConfig) {
  return route.match.methods?.length ? route.match.methods.join(', ') : t('resources.allMethods')
}

function formatDestinations(cluster: ClusterConfig) {
  const destinations = Object.values(cluster.destinations)
  if (!destinations.length) return t('resources.noDestinations')
  const visible = destinations.slice(0, 2).map(destination => destination.address)
  const remaining = destinations.length - visible.length
  return `${visible.join(' · ')}${remaining > 0 ? ` +${remaining}` : ''}`
}

function routeUsage(clusterId: string) {
  const count = routes.value.filter(route => route.clusterId === clusterId).length
  return count === 0 ? t('resources.unused') : t('resources.usedByRoutes', { count }, count)
}

function resultLabel() {
  return t('resources.resultCount', { count: resultCount.value }, resultCount.value)
}

function beginChange(intent: ChangeIntent, resourceId?: string) {
  changeIntent.value = intent
  selectedResourceId.value = resourceId ?? null
  createVisible.value = true
}

function openChange(id: string) {
  router.push({
    name: 'studio',
    params: { id },
    query: {
      kind: props.resourceKind,
      ...(changeIntent.value === 'add' ? { action: 'add' } : { resource: selectedResourceId.value ?? undefined })
    }
  })
}

watch(() => props.resourceKind, () => {
  search.value = ''
})
onMounted(load)
</script>

<template>
  <div class="page resource-page">
    <header class="page-header">
      <div>
        <p class="eyebrow">{{ t('resources.activeEyebrow', { revision: configuration?.revisionNumber ?? '—' }) }}</p>
        <h1 class="page-title">{{ t(isRoutes ? 'resources.routesTitle' : 'resources.clustersTitle') }}</h1>
        <p class="page-summary">{{ t(isRoutes ? 'resources.routesSummary' : 'resources.clustersSummary') }}</p>
      </div>
      <a-button v-if="session.canEdit" type="primary" @click="beginChange('add')">
        <Plus :size="16" /> {{ t(isRoutes ? 'resources.newRoute' : 'resources.newCluster') }}
      </a-button>
    </header>

    <section class="resource-context panel">
      <div>
        <span>{{ t('resources.activeRevision') }}</span>
        <strong class="display-type">{{ configuration ? `r${configuration.revisionNumber}` : '—' }}</strong>
      </div>
      <div>
        <span>{{ t('resources.resourceCount') }}</span>
        <strong class="display-type">{{ loading ? '—' : totalCount }}</strong>
      </div>
      <div>
        <span>{{ t('resources.publishedAt') }}</span>
        <strong>{{ configuration ? publishedFormatter.format(new Date(configuration.publishedAt)) : '—' }}</strong>
      </div>
      <div class="hash-context">
        <span>{{ t('resources.contentHash') }}</span>
        <strong class="mono">{{ configuration?.contentHash.slice(0, 12) ?? '—' }}</strong>
      </div>
      <a-button :title="t('resources.refresh')" :aria-label="t('resources.refresh')" :loading="loading" @click="load">
        <RefreshCw :size="15" />
      </a-button>
    </section>

    <form class="resource-toolbar" role="search" @submit.prevent>
      <a-input v-model="search" allow-clear :placeholder="t(isRoutes ? 'resources.searchRoutes' : 'resources.searchClusters')">
        <template #prefix><Search :size="15" /></template>
      </a-input>
      <span class="result-count mono">{{ resultLabel() }}</span>
    </form>

    <section class="resource-catalog panel" :aria-label="t(isRoutes ? 'resources.routesTitle' : 'resources.clustersTitle')">
      <a-spin :loading="loading" class="catalog-spin">
        <div v-if="!loading && resultCount === 0" class="empty-state">
          <div>
            <component :is="isRoutes ? RouteIcon : Server" :size="30" :stroke-width="1.5" />
            <p>{{ search ? t('resources.noMatches') : t(isRoutes ? 'resources.emptyRoutes' : 'resources.emptyClusters') }}</p>
          </div>
        </div>

        <template v-else>
          <div class="table-scroll desktop-catalog">
            <table v-if="isRoutes" class="resource-table">
            <thead><tr>
              <th>{{ t('resources.table.routeId') }}</th>
              <th>{{ t('resources.table.match') }}</th>
              <th>{{ t('resources.table.cluster') }}</th>
              <th>{{ t('resources.table.methods') }}</th>
              <th>{{ t('resources.table.order') }}</th>
              <th :aria-label="t('resources.table.actions')" />
            </tr></thead>
            <tbody><tr v-for="routeItem in filteredRoutes" :key="routeItem.routeId">
              <td><strong>{{ routeItem.routeId }}</strong></td>
              <td><code>{{ routeItem.match.path ?? '—' }}</code></td>
              <td>{{ routeItem.clusterId ?? '—' }}</td>
              <td><span class="method-list">{{ formatMethods(routeItem) }}</span></td>
              <td class="mono">{{ routeItem.order ?? t('resources.defaultOrder') }}</td>
              <td><a-button v-if="session.canEdit" size="small" @click="beginChange('edit', routeItem.routeId)"><Pencil :size="13" /> {{ t('resources.editRoute') }}</a-button></td>
            </tr></tbody>
            </table>

            <table v-else class="resource-table cluster-table">
            <thead><tr>
              <th>{{ t('resources.table.clusterId') }}</th>
              <th>{{ t('resources.table.destinations') }}</th>
              <th>{{ t('resources.table.policy') }}</th>
              <th>{{ t('resources.table.usage') }}</th>
              <th :aria-label="t('resources.table.actions')" />
            </tr></thead>
            <tbody><tr v-for="clusterItem in filteredClusters" :key="clusterItem.clusterId">
              <td><strong>{{ clusterItem.clusterId }}</strong></td>
              <td><span class="destination-list">{{ formatDestinations(clusterItem) }}</span></td>
              <td>{{ clusterItem.loadBalancingPolicy ?? t('resources.defaultPolicy') }}</td>
              <td>{{ routeUsage(clusterItem.clusterId) }}</td>
              <td><a-button v-if="session.canEdit" size="small" @click="beginChange('edit', clusterItem.clusterId)"><Pencil :size="13" /> {{ t('resources.editCluster') }}</a-button></td>
            </tr></tbody>
            </table>
          </div>

          <div class="mobile-resource-list">
            <article v-for="routeItem in filteredRoutes" v-if="isRoutes" :key="routeItem.routeId" class="mobile-resource-item">
              <div class="mobile-resource-heading">
                <div><span>{{ t('resources.table.routeId') }}</span><strong>{{ routeItem.routeId }}</strong></div>
                <a-button v-if="session.canEdit" size="small" @click="beginChange('edit', routeItem.routeId)"><Pencil :size="13" /> {{ t('resources.editRoute') }}</a-button>
              </div>
              <dl>
                <div><dt>{{ t('resources.table.match') }}</dt><dd><code>{{ routeItem.match.path ?? '—' }}</code></dd></div>
                <div><dt>{{ t('resources.table.cluster') }}</dt><dd>{{ routeItem.clusterId ?? '—' }}</dd></div>
                <div><dt>{{ t('resources.table.methods') }}</dt><dd>{{ formatMethods(routeItem) }}</dd></div>
                <div><dt>{{ t('resources.table.order') }}</dt><dd>{{ routeItem.order ?? t('resources.defaultOrder') }}</dd></div>
              </dl>
            </article>

            <article v-for="clusterItem in filteredClusters" v-else :key="clusterItem.clusterId" class="mobile-resource-item">
              <div class="mobile-resource-heading">
                <div><span>{{ t('resources.table.clusterId') }}</span><strong>{{ clusterItem.clusterId }}</strong></div>
                <a-button v-if="session.canEdit" size="small" @click="beginChange('edit', clusterItem.clusterId)"><Pencil :size="13" /> {{ t('resources.editCluster') }}</a-button>
              </div>
              <dl>
                <div class="mobile-wide"><dt>{{ t('resources.table.destinations') }}</dt><dd>{{ formatDestinations(clusterItem) }}</dd></div>
                <div><dt>{{ t('resources.table.policy') }}</dt><dd>{{ clusterItem.loadBalancingPolicy ?? t('resources.defaultPolicy') }}</dd></div>
                <div><dt>{{ t('resources.table.usage') }}</dt><dd>{{ routeUsage(clusterItem.clusterId) }}</dd></div>
              </dl>
            </article>
          </div>
        </template>
      </a-spin>
    </section>

    <CreateDraftModal v-model:visible="createVisible" :suggested-name="suggestedChangeName" @created="openChange" />
  </div>
</template>

<style scoped>
.resource-page { width: 100%; max-width: 1560px; }
.page-header .arco-btn, .resource-table .arco-btn { display: inline-flex; align-items: center; gap: 6px; }
.resource-context { display: grid; min-height: 78px; margin-bottom: 14px; grid-template-columns: 140px 130px minmax(190px, 1fr) minmax(150px, 1fr) 52px; align-items: stretch; }
.resource-context > div { display: grid; padding: 15px 20px; border-right: 1px solid var(--line); align-content: center; gap: 6px; }
.resource-context span { color: var(--muted); font-size: 9px; text-transform: uppercase; }
.resource-context strong { overflow: hidden; color: var(--ink); font-size: 12px; text-overflow: ellipsis; white-space: nowrap; }
.resource-context .display-type { font-size: 18px; }
.resource-context > .arco-btn { width: 34px; height: 34px; margin: auto; padding: 0; }
.resource-toolbar { display: flex; min-height: 46px; margin-bottom: 10px; align-items: center; gap: 12px; }
.resource-toolbar .arco-input-wrapper { width: min(480px, 100%); }
.result-count { margin-left: auto; color: var(--muted); font-size: 10px; }
.resource-catalog { min-height: 300px; overflow: hidden; }
.catalog-spin { display: block; min-height: 300px; }
.table-scroll { overflow-x: auto; }
.mobile-resource-list { display: none; }
.resource-table { width: 100%; min-width: 860px; border-collapse: collapse; table-layout: fixed; }
.resource-table th { height: 42px; padding: 0 16px; color: var(--muted); background: var(--surface-subtle); border-bottom: 1px solid var(--line); font: 400 9px/1 'IBM Plex Mono', monospace; text-align: left; text-transform: uppercase; }
.resource-table td { height: 68px; padding: 10px 16px; color: var(--ink-soft); border-bottom: 1px solid var(--line); font-size: 11px; vertical-align: middle; }
.resource-table tbody tr:last-child td { border-bottom: 0; }
.resource-table tbody tr:hover { background: #f8faf9; }
.resource-table th:first-child { width: 20%; }
.resource-table th:nth-child(2) { width: 25%; }
.resource-table th:nth-child(3) { width: 17%; }
.resource-table th:nth-child(4) { width: 18%; }
.resource-table th:nth-child(5) { width: 10%; }
.resource-table th:last-child { width: 10%; }
.cluster-table th:first-child { width: 20%; }
.cluster-table th:nth-child(2) { width: 38%; }
.cluster-table th:nth-child(3) { width: 17%; }
.cluster-table th:nth-child(4) { width: 15%; }
.cluster-table th:last-child { width: 10%; }
.resource-table strong { color: var(--ink); font-size: 12px; }
.resource-table code { color: var(--blue-dark); font: 400 10px/1.5 'IBM Plex Mono', monospace; }
.method-list, .destination-list { display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.empty-state { min-height: 300px; }

@media (max-width: 760px) {
  .resource-context { grid-template-columns: repeat(2, 1fr) 48px; }
  .resource-context > div { padding: 13px 15px; }
  .resource-context > div:nth-child(2) { border-right: 0; }
  .resource-context > div:nth-child(-n+2) { border-bottom: 1px solid var(--line); }
  .resource-context > div:nth-child(4) { border-right: 0; }
  .resource-context > .arco-btn { grid-column: 3; grid-row: 1 / 3; }
  .resource-toolbar { align-items: stretch; flex-direction: column; }
  .resource-toolbar .arco-input-wrapper { width: 100%; }
  .result-count { margin-left: 0; }
  .resource-catalog { min-height: 0; overflow: visible; background: transparent; border: 0; box-shadow: none; }
  .catalog-spin { min-height: 0; }
  .desktop-catalog { display: none; }
  .mobile-resource-list { display: grid; gap: 10px; }
  .mobile-resource-item { padding: 15px; background: var(--surface); border: 1px solid var(--line); border-radius: 4px; box-shadow: var(--shadow-sm); }
  .mobile-resource-heading { display: flex; padding-bottom: 13px; border-bottom: 1px solid var(--line); align-items: center; justify-content: space-between; gap: 12px; }
  .mobile-resource-heading > div { display: grid; min-width: 0; gap: 4px; }
  .mobile-resource-heading span, .mobile-resource-item dt { color: var(--muted); font-size: 8px; text-transform: uppercase; }
  .mobile-resource-heading strong { overflow: hidden; color: var(--ink); font-size: 13px; text-overflow: ellipsis; white-space: nowrap; }
  .mobile-resource-heading .arco-btn { flex-shrink: 0; }
  .mobile-resource-item dl { display: grid; margin: 0; padding-top: 13px; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 13px 16px; }
  .mobile-resource-item dl > div { display: grid; min-width: 0; gap: 5px; }
  .mobile-resource-item dd { overflow: hidden; margin: 0; color: var(--ink-soft); font-size: 10px; text-overflow: ellipsis; white-space: nowrap; }
  .mobile-resource-item code { color: var(--blue-dark); font: 400 10px/1.4 'IBM Plex Mono', monospace; }
  .mobile-resource-item .mobile-wide { grid-column: 1 / -1; }
}
</style>