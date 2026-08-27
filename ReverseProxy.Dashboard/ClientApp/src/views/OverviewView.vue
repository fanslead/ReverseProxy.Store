<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { Message } from '@arco-design/web-vue'
import { ArrowUpRight, FilePlus2, RefreshCw } from '@lucide/vue'
import CreateDraftModal from '@/components/CreateDraftModal.vue'
import DraftTable from '@/components/DraftTable.vue'
import SignalTrace from '@/components/SignalTrace.vue'
import { localizeApiError } from '@/i18n/errors'
import { controlPlane } from '@/services/controlPlane'
import { useSessionStore } from '@/stores/session'
import type { DraftSummary, RuntimeStatus } from '@/types/controlPlane'

const router = useRouter()
const session = useSessionStore()
const { locale, t, te } = useI18n()
const runtime = ref<RuntimeStatus | null>(null)
const drafts = ref<DraftSummary[]>([])
const loading = ref(true)
const createVisible = ref(false)
const runtimeStatusClass = computed(() => {
  const status = runtime.value?.status.toLowerCase()
  return status === 'healthy' || status === 'degraded' ? status : 'unhealthy'
})

const timeFormatter = computed(() => new Intl.DateTimeFormat(locale.value, {
  hour: '2-digit',
  minute: '2-digit',
  second: '2-digit'
}))

function formatRuntimeStatus() {
  if (loading.value) return t('overview.checking')
  if (!runtime.value) return t('overview.unavailable')
  const key = `runtime.${runtime.value.status.toLowerCase()}`
  return te(key) ? t(key) : runtime.value.status
}

function formatOpenDrafts() {
  const count = drafts.value.length
  return count === 0
    ? t('overview.noOpenDrafts')
    : t('overview.openDrafts', { count }, count)
}

async function load() {
  loading.value = true
  try {
    const [runtimeResult, draftsResult] = await Promise.all([
      controlPlane.runtimeStatus(),
      controlPlane.listDrafts('', 1, 5)
    ])
    runtime.value = runtimeResult.data
    drafts.value = draftsResult.data.items
  } catch (reason) {
    Message.error(localizeApiError(reason, 'overview.loadFailed'))
  } finally {
    loading.value = false
  }
}

function openDraft(id: string) {
  router.push({ name: 'studio', params: { id } })
}

onMounted(load)
</script>

<template>
  <div class="page overview-page">
    <header class="page-header">
      <div>
        <p class="eyebrow">{{ t('overview.eyebrow') }}</p>
        <h1 class="page-title">{{ t('overview.title') }}</h1>
        <p class="page-summary">{{ t('overview.summary') }}</p>
      </div>
      <div class="header-actions">
        <a-button :title="t('overview.refresh')" :aria-label="t('overview.refresh')" @click="load"><RefreshCw :size="16" /></a-button>
        <a-button v-if="session.canEdit" type="primary" @click="createVisible = true">
          <FilePlus2 :size="16" /> {{ t('overview.newDraft') }}
        </a-button>
      </div>
    </header>

    <section class="metric-strip panel" :aria-label="t('overview.runtimeMetrics')">
      <div>
        <span>{{ t('overview.runtimeStatus') }}</span>
        <strong :class="['runtime-status', runtimeStatusClass]"><i />{{ formatRuntimeStatus() }}</strong>
      </div>
      <div>
        <span>{{ t('overview.activeRevision') }}</span>
        <strong class="display-type">{{ loading ? '—' : `r${runtime?.revisionNumber ?? 0}` }}</strong>
      </div>
      <div>
        <span>{{ t('overview.routesClusters') }}</span>
        <strong class="display-type">{{ loading ? '—' : `${runtime?.routeCount ?? 0} / ${runtime?.clusterCount ?? 0}` }}</strong>
      </div>
      <div>
        <span>{{ t('overview.loadedAt') }}</span>
        <strong class="mono metric-time">{{ runtime ? timeFormatter.format(new Date(runtime.loadedAt)) : '—' }}</strong>
      </div>
    </section>

    <SignalTrace
      :revision="runtime?.revisionNumber"
      :route-count="runtime?.routeCount"
      :cluster-count="runtime?.clusterCount"
      :loading="loading"
    />

    <section class="recent-section">
      <div class="section-heading">
        <div>
          <h2>{{ t('overview.recentDrafts') }}</h2>
          <p>{{ formatOpenDrafts() }}</p>
        </div>
        <RouterLink :to="{ name: 'changes' }" class="view-all">{{ t('overview.viewAll') }} <ArrowUpRight :size="14" /></RouterLink>
      </div>
      <DraftTable :drafts="drafts" :loading="loading" />
    </section>

    <CreateDraftModal v-model:visible="createVisible" @created="openDraft" />
  </div>
</template>

<style scoped>
.header-actions { display: flex; gap: 8px; }
.header-actions .arco-btn { display: inline-flex; align-items: center; gap: 7px; }
.metric-strip { display: grid; margin-bottom: 18px; grid-template-columns: repeat(4, 1fr); }
.metric-strip > div { display: grid; min-height: 92px; padding: 19px 22px; border-right: 1px solid var(--line); align-content: center; gap: 8px; }
.metric-strip > div:last-child { border-right: 0; }
.metric-strip span { color: var(--muted); font-size: 10px; text-transform: uppercase; }
.metric-strip strong { font-size: 23px; font-weight: 620; }
.metric-strip .runtime-status { display: flex; align-items: center; gap: 9px; font-size: 14px; }
.runtime-status i { width: 8px; height: 8px; border-radius: 50%; }
.runtime-status.healthy { color: var(--green); }
.runtime-status.healthy i { background: var(--green); box-shadow: 0 0 0 5px var(--green-soft); }
.runtime-status.degraded { color: var(--amber); }
.runtime-status.degraded i { background: var(--amber); box-shadow: 0 0 0 5px var(--amber-soft); }
.runtime-status.unhealthy { color: var(--red); }
.runtime-status.unhealthy i { background: var(--red); box-shadow: 0 0 0 5px var(--red-soft); }
.metric-strip .metric-time { font-size: 14px; }
.recent-section { margin-top: 28px; }
.view-all { display: inline-flex; align-items: center; gap: 6px; color: var(--blue); font-size: 12px; font-weight: 650; }

@media (max-width: 980px) {
  .metric-strip { grid-template-columns: repeat(2, 1fr); }
  .metric-strip > div:nth-child(2) { border-right: 0; }
  .metric-strip > div:nth-child(-n+2) { border-bottom: 1px solid var(--line); }
}

@media (max-width: 620px) {
  .header-actions { width: 100%; }
  .header-actions .arco-btn:first-child { width: 42px; }
  .header-actions .arco-btn:last-child { flex: 1; }
  .metric-strip > div { min-height: 82px; padding: 15px; }
  .metric-strip strong { font-size: 18px; }
}
</style>