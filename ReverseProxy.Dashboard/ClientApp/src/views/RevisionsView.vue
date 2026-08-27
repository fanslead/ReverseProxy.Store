<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Message } from '@arco-design/web-vue'
import { CheckCircle2, CircleAlert, Eye, FileClock, RefreshCw, RotateCcw } from '@lucide/vue'
import LocalizedModalTitle from '@/components/LocalizedModalTitle.vue'
import { localizeApiError } from '@/i18n/errors'
import { controlPlane } from '@/services/controlPlane'
import { ApiError } from '@/services/http'
import { useSessionStore } from '@/stores/session'
import type { ConfigOperation, Revision, RevisionSummary, RuntimeConfiguration } from '@/types/controlPlane'

const pageSize = 25
const session = useSessionStore()
const { locale, t, te } = useI18n()
const revisions = ref<RevisionSummary[]>([])
const runtime = ref<RuntimeConfiguration | null>(null)
const page = ref(1)
const total = ref(0)
const loading = ref(true)
const loadFailed = ref(false)
const detailVisible = ref(false)
const detailLoading = ref(false)
const restoring = ref(false)
const selectedRevision = ref<Revision | null>(null)
const trackedOperation = ref<ConfigOperation | null>(null)
let loadGeneration = 0
let detailGeneration = 0
let operationGeneration = 0
let isMounted = false

const dateFormatter = computed(() => new Intl.DateTimeFormat(locale.value, {
  year: 'numeric',
  month: 'short',
  day: 'numeric',
  hour: '2-digit',
  minute: '2-digit'
}))
const formattedDocument = computed(() => selectedRevision.value
  ? JSON.stringify(selectedRevision.value.document, null, 2)
  : '')

async function load() {
  const generation = ++loadGeneration
  const requestedPage = page.value
  loading.value = true
  try {
    const [historyResult, runtimeResult] = await Promise.all([
      controlPlane.listRevisions(requestedPage, pageSize),
      controlPlane.runtimeConfiguration()
    ])
    if (generation !== loadGeneration || requestedPage !== page.value) return
    revisions.value = historyResult.data.items
    total.value = historyResult.data.total
    runtime.value = runtimeResult.data
    loadFailed.value = false
  } catch (reason) {
    if (generation === loadGeneration) {
      loadFailed.value = true
      Message.error(localizeApiError(reason, 'revisions.loadFailed'))
    }
  } finally {
    if (generation === loadGeneration) loading.value = false
  }
}

async function inspectRevision(revision: RevisionSummary) {
  const generation = ++detailGeneration
  detailVisible.value = true
  detailLoading.value = true
  selectedRevision.value = null
  try {
    const result = await controlPlane.getRevision(revision.id)
    if (generation !== detailGeneration || !detailVisible.value) return
    selectedRevision.value = result.data
  } catch (reason) {
    if (generation === detailGeneration) {
      detailVisible.value = false
      Message.error(localizeApiError(reason, 'revisions.detailFailed'))
    }
  } finally {
    if (generation === detailGeneration) detailLoading.value = false
  }
}

async function restoreRevision() {
  if (!selectedRevision.value || selectedRevision.value.isActive || !session.canPublish || trackedOperation.value) return
  const revisionId = selectedRevision.value.id
  restoring.value = true
  try {
    const active = (await controlPlane.runtimeConfiguration()).data
    if (!isMounted) return
    const operation = (await controlPlane.rollbackRevision(revisionId, active.revisionId)).data
    if (!isMounted) return
    detailVisible.value = false
    selectedRevision.value = null
    if (page.value === 1) await load()
    else page.value = 1
    if (!isMounted) return
    notifyOperation(operation)
    if (isPendingOperation(operation)) void trackOperation(operation)
  } catch (reason) {
    if (!isMounted) return
    Message.error(localizeApiError(reason, 'revisions.rollbackFailed'))
    if (isRevisionConflict(reason)) {
      detailVisible.value = false
      selectedRevision.value = null
      if (page.value === 1) await load()
      else page.value = 1
    }
  } finally {
    if (isMounted) restoring.value = false
  }
}

async function trackOperation(initialOperation: ConfigOperation) {
  if (!isMounted) return
  const generation = ++operationGeneration
  let operation = initialOperation
  let delay = 1000
  trackedOperation.value = operation
  try {
    while (isPendingOperation(operation)) {
      await wait(delay)
      if (generation !== operationGeneration) return
      operation = (await controlPlane.operation(operation.id)).data
      if (generation !== operationGeneration) return
      trackedOperation.value = operation
      delay = Math.min(delay * 2, 5000)
    }
    notifyOperation(operation)
    if (page.value === 1) await load()
    else page.value = 1
  } catch (reason) {
    if (generation === operationGeneration) {
      Message.error(localizeApiError(reason, 'revisions.operationFailed'))
    }
  } finally {
    if (generation === operationGeneration) trackedOperation.value = null
  }
}

function notifyOperation(operation: ConfigOperation) {
    </style>
    revision: operation.revisionNumber,
    status: operationStatus(operation.status)
  })
  if (operation.status === 'Applied') Message.success(message)
  else if (operation.status === 'Failed') Message.error(message)
  else Message.warning(message)
}

function isPendingOperation(operation: ConfigOperation) {
  return operation.status === 'Queued' || operation.status === 'Applying'
}

function isRevisionConflict(reason: unknown) {
  return reason instanceof ApiError
    && (reason.code === 'active_revision_conflict' || reason.code === 'revision_already_active')
}

function wait(milliseconds: number) {
  return new Promise(resolve => setTimeout(resolve, milliseconds))
}

function formatDate(value: string) {
  return dateFormatter.value.format(new Date(value))
}

function operationStatus(status: string) {
  const key = `studio.operation.${status.toLowerCase()}`
  return te(key) ? t(key) : status
}

watch(page, load)
watch(detailVisible, visible => {
  if (!visible) {
    detailGeneration++
    detailLoading.value = false
  }
})
onMounted(() => {
  isMounted = true
  void load()
})
onBeforeUnmount(() => {
  isMounted = false
  loadGeneration++
  detailGeneration++
  operationGeneration++
})
</script>

<template>
  <div class="page revisions-page">
    <header class="page-header">
      <div>
        <p class="eyebrow">{{ t('revisions.eyebrow') }}</p>
        <h1 class="page-title">{{ t('revisions.title') }}</h1>
        <p class="page-summary">{{ t('revisions.summary') }}</p>
      </div>
      <a-button :title="t('revisions.refresh')" :aria-label="t('revisions.refresh')" :loading="loading" @click="load">
        <RefreshCw :size="16" />
      </a-button>
    </header>

    <section class="revision-context panel">
      <div>
        <span>{{ t('revisions.activeRevision') }}</span>
        <strong class="display-type">{{ runtime ? `r${runtime.revisionNumber}` : '—' }}</strong>
      </div>
      <div>
        <span>{{ t('revisions.revisionCount') }}</span>
        <strong class="display-type">{{ loading ? '—' : total }}</strong>
      </div>
      <div class="restore-rule">
        <span>{{ t('revisions.invariant') }}</span>
        <strong><RotateCcw :size="14" /> {{ t('revisions.forwardOnly') }}</strong>
      </div>
      <span class="revision-total mono">{{ t('revisions.total', { count: total }, total) }}</span>
    </section>

    <a-alert v-if="trackedOperation" class="operation-alert" type="warning">
      {{ t('revisions.rollbackSuccess', {
        revision: trackedOperation.revisionNumber,
        status: operationStatus(trackedOperation.status)
      }) }}
    </a-alert>

    <section class="revision-ledger panel" :aria-label="t('revisions.title')">
      <a-spin :loading="loading" class="ledger-spin">
        <div v-if="!loading && loadFailed" class="empty-state load-error">
          <div>
            <CircleAlert :size="30" :stroke-width="1.5" />
            <p>{{ t('revisions.loadFailed') }}</p>
            <a-button @click="load"><RefreshCw :size="15" /> {{ t('revisions.refresh') }}</a-button>
          </div>
        </div>

        <div v-else-if="!loading && revisions.length === 0" class="empty-state">
          <div><FileClock :size="30" :stroke-width="1.5" /><p>{{ t('revisions.empty') }}</p></div>
        </div>

        <div v-else class="table-scroll desktop-ledger">
          <table class="revision-table">
            <thead><tr>
              <th>{{ t('revisions.table.revision') }}</th>
              <th>{{ t('revisions.table.state') }}</th>
              <th>{{ t('revisions.table.hash') }}</th>
              <th>{{ t('revisions.table.publisher') }}</th>
              <th>{{ t('revisions.table.published') }}</th>
              <th :aria-label="t('revisions.table.actions')" />
            </tr></thead>
            <tbody><tr v-for="revision in revisions" :key="revision.id">
              <td>
                <button class="revision-link" type="button" @click="inspectRevision(revision)">
                  <i :class="{ active: revision.isActive }" />
                  <span><strong>r{{ revision.revisionNumber }}</strong><small class="mono">schema {{ revision.schemaVersion }}</small></span>
                </button>
              </td>
              <td><span :class="['state-chip', { active: revision.isActive }]">{{ t(revision.isActive ? 'revisions.active' : 'revisions.historical') }}</span></td>
              <td><code>{{ revision.contentHash.slice(0, 12) }}</code></td>
              <td><span class="publisher">{{ revision.publishedBy }}</span></td>
              <td><span class="published-at">{{ formatDate(revision.publishedAt) }}</span></td>
              <td>
                <button class="inspect-button" type="button" :title="t('revisions.inspect')" :aria-label="t('revisions.inspectNamed', { revision: revision.revisionNumber })" @click="inspectRevision(revision)">
                  <Eye :size="16" />
                </button>
              </td>
            </tr></tbody>
          </table>
        </div>

        <div v-if="!loadFailed && revisions.length > 0" class="mobile-ledger">
          <button v-for="revision in revisions" :key="revision.id" class="mobile-revision" type="button" @click="inspectRevision(revision)">
            <span class="mobile-revision-title"><i :class="{ active: revision.isActive }" /><strong>r{{ revision.revisionNumber }}</strong><small>{{ t(revision.isActive ? 'revisions.active' : 'revisions.historical') }}</small></span>
            <code>{{ revision.contentHash.slice(0, 12) }}</code>
            <span>{{ formatDate(revision.publishedAt) }}</span>
            <Eye :size="16" />
          </button>
        </div>
      </a-spin>
    </section>

    <div v-if="!loadFailed && total > pageSize" class="pagination-wrap">
      <a-pagination v-model:current="page" class="desktop-pagination" :total="total" :page-size="pageSize" :show-total="true" />
      <a-pagination v-model:current="page" class="mobile-pagination" :total="total" :page-size="pageSize" simple />
    </div>

    <a-modal
      v-model:visible="detailVisible"
      :footer="false"
      :closable="false"
      :mask-closable="!restoring"
      :esc-to-close="!restoring"
      title-align="start"
      width="min(760px, calc(100vw - 32px))"
    >
      <template #title>
        <LocalizedModalTitle
          :title="t('revisions.detailTitle', { revision: selectedRevision?.revisionNumber ?? '—' })"
          :close-label="t('common.close')"
          :disabled="restoring"
          @close="detailVisible = false"
        />
      </template>
      <a-spin :loading="detailLoading" class="detail-spin">
        <div v-if="selectedRevision" class="revision-detail">
          <dl class="detail-facts">
            <div><dt>{{ t('revisions.publishedAt') }}</dt><dd>{{ formatDate(selectedRevision.publishedAt) }}</dd></div>
            <div><dt>{{ t('revisions.publishedBy') }}</dt><dd>{{ selectedRevision.publishedBy }}</dd></div>
            <div><dt>{{ t('revisions.schema') }}</dt><dd class="mono">v{{ selectedRevision.schemaVersion }}</dd></div>
            <div><dt>{{ t('revisions.hash') }}</dt><dd class="mono hash-value">{{ selectedRevision.contentHash }}</dd></div>
          </dl>

          <section class="document-section">
            <header>
              <div><span class="eyebrow">{{ t('revisions.document') }}</span><strong>{{ t('revisions.resourceCount', { routes: selectedRevision.document.routes.length, clusters: selectedRevision.document.clusters.length }) }}</strong></div>
              <span :class="['state-chip', { active: selectedRevision.isActive }]">{{ t(selectedRevision.isActive ? 'revisions.active' : 'revisions.historical') }}</span>
            </header>
            <pre class="document-json" tabindex="0" :aria-label="t('revisions.document')">{{ formattedDocument }}</pre>
          </section>

          <a-alert v-if="selectedRevision.isActive" type="success">
            <template #icon><CheckCircle2 :size="17" /></template>
            {{ t('revisions.currentNotice') }}
          </a-alert>
          <a-alert v-else-if="session.canPublish" type="warning">{{ t('revisions.rollbackNote') }}</a-alert>

          <div class="detail-actions">
            <a-button :disabled="restoring" @click="detailVisible = false">{{ t('common.close') }}</a-button>
            <a-popconfirm
              v-if="session.canPublish && !selectedRevision.isActive"
              :content="t('revisions.rollbackConfirm', { revision: selectedRevision.revisionNumber })"
              @ok="restoreRevision"
            >
              <a-button type="primary" status="warning" :disabled="Boolean(trackedOperation)" :loading="restoring"><RotateCcw :size="15" /> {{ t('revisions.rollback') }}</a-button>
            </a-popconfirm>
          </div>
        </div>
      </a-spin>
    </a-modal>
  </div>
</template>

<style scoped>
.revisions-page { width: 100%; max-width: 1500px; }
.page-header .arco-btn { display: grid; width: 38px; height: 38px; padding: 0; place-items: center; }
.revision-context { display: grid; min-height: 86px; margin-bottom: 14px; grid-template-columns: 180px 160px minmax(220px, 1fr) auto; align-items: stretch; }
.revision-context > div { display: grid; padding: 17px 20px; border-right: 1px solid var(--line); align-content: center; gap: 6px; }
.revision-context span, .detail-facts dt { color: var(--muted); font-size: 9px; text-transform: uppercase; }
.revision-context strong { color: var(--ink); font-size: 12px; }
.revision-context .display-type { font-size: 22px; }
.restore-rule strong { display: inline-flex; color: var(--green); align-items: center; gap: 7px; }
.revision-total { display: flex; padding: 0 20px; color: var(--muted); align-items: center; font-size: 10px; }
.operation-alert { margin-bottom: 14px; }
.revision-ledger { overflow: hidden; }
.ledger-spin { display: block; min-height: 130px; }
.load-error > div { display: grid; justify-items: center; gap: 10px; }
.load-error p { margin: 0; }
.load-error .arco-btn { display: inline-flex; align-items: center; gap: 6px; }
.table-scroll { overflow-x: auto; }
.revision-table { width: 100%; border-collapse: collapse; table-layout: fixed; }
.revision-table th { height: 42px; padding: 0 18px; color: var(--muted); background: var(--surface-subtle); border-bottom: 1px solid var(--line); font: 400 9px/1 'IBM Plex Mono', monospace; text-align: left; text-transform: uppercase; }
.revision-table td { height: 68px; padding: 0 18px; border-bottom: 1px solid var(--line); font-size: 11px; }
.revision-table tbody tr:last-child td { border-bottom: 0; }
.revision-table tbody tr:hover { background: #f8faf9; }
.revision-table th:first-child { width: 19%; }
.revision-table th:nth-child(2) { width: 12%; }
.revision-table th:nth-child(3) { width: 16%; }
.revision-table th:nth-child(4) { width: 22%; }
.revision-table th:nth-child(5) { width: 25%; }
.revision-table th:last-child { width: 6%; }
.revision-link { display: flex; width: 100%; padding: 0; color: var(--ink); background: transparent; border: 0; cursor: pointer; align-items: center; gap: 12px; text-align: left; }
.revision-link > i, .mobile-revision-title > i { width: 9px; height: 9px; background: #c7d0cc; border: 2px solid var(--surface); border-radius: 50%; box-shadow: 0 0 0 1px #c7d0cc; flex: 0 0 auto; }
.revision-link > i.active, .mobile-revision-title > i.active { background: var(--green); box-shadow: 0 0 0 1px var(--green), 0 0 0 5px rgb(10 143 120 / 10%); }
.revision-link span { display: grid; gap: 4px; }
.revision-link strong { font: 650 14px/1 'Space Grotesk Variable', sans-serif; }
.revision-link small { color: var(--muted); font-size: 8px; }
.state-chip { display: inline-flex; min-height: 24px; padding: 0 8px; color: var(--muted); background: #eef1ef; border-radius: 3px; align-items: center; font-size: 9px; font-weight: 650; }
.state-chip.active { color: var(--green); background: var(--green-soft); }
.revision-table code { color: var(--ink-soft); font: 400 10px/1 'IBM Plex Mono', monospace; }
.publisher { display: block; overflow: hidden; color: var(--ink-soft); text-overflow: ellipsis; white-space: nowrap; }
.published-at { color: var(--muted); }
.inspect-button { display: grid; width: 30px; height: 30px; padding: 0; color: var(--muted); background: transparent; border: 0; border-radius: 3px; cursor: pointer; place-items: center; }
.inspect-button:hover { color: var(--blue); background: #eaf0ff; }
.mobile-ledger { display: none; }
.pagination-wrap { display: flex; padding-top: 18px; justify-content: flex-end; }
.mobile-pagination { display: none; }
.detail-spin { display: block; min-height: 180px; }
.revision-detail { display: grid; gap: 18px; }
.detail-facts { display: grid; margin: 0; grid-template-columns: 1fr 1fr; border: 1px solid var(--line); }
.detail-facts div { display: grid; min-width: 0; padding: 12px 14px; border-right: 1px solid var(--line); border-bottom: 1px solid var(--line); gap: 5px; }
.detail-facts div:nth-child(2n) { border-right: 0; }
.detail-facts div:nth-last-child(-n + 2) { border-bottom: 0; }
.detail-facts dd { min-width: 0; margin: 0; overflow: hidden; color: var(--ink-soft); font-size: 11px; text-overflow: ellipsis; white-space: nowrap; }
.hash-value { font-size: 9px !important; }
.document-section { overflow: hidden; border: 1px solid var(--line); border-radius: 4px; }
.document-section header { display: flex; min-height: 58px; padding: 10px 14px; background: var(--surface-subtle); border-bottom: 1px solid var(--line); align-items: center; justify-content: space-between; gap: 16px; }
.document-section header div { display: grid; gap: 4px; }
.document-section header strong { color: var(--ink-soft); font-size: 10px; }
.document-json { height: 260px; margin: 0; padding: 15px; overflow: auto; color: #dce6e2; background: #17231f; font: 400 10px/1.65 'IBM Plex Mono', monospace; white-space: pre; }
.detail-actions { display: flex; justify-content: flex-end; gap: 8px; }
.detail-actions .arco-btn { display: inline-flex; align-items: center; gap: 6px; }

@media (max-width: 760px) {
  .revision-context { grid-template-columns: 1fr 1fr; }
  .revision-context > div:nth-child(2) { border-right: 0; }
  .restore-rule { border-top: 1px solid var(--line); }
  .revision-total { border-top: 1px solid var(--line); }
  .desktop-ledger { display: none; }
  .mobile-ledger { display: grid; }
  .mobile-revision { display: grid; min-height: 76px; padding: 13px 15px; color: var(--ink-soft); background: transparent; border: 0; border-bottom: 1px solid var(--line); cursor: pointer; grid-template-columns: minmax(110px, 1fr) minmax(92px, .7fr) minmax(130px, 1fr) 24px; align-items: center; gap: 10px; text-align: left; }
  .mobile-revision:last-child { border-bottom: 0; }
  .mobile-revision-title { display: flex; align-items: center; gap: 9px; }
  .mobile-revision-title small { padding: 3px 5px; color: var(--muted); background: #eef1ef; border-radius: 3px; font-size: 8px; }
  .mobile-revision code { color: var(--muted); font: 400 9px/1 'IBM Plex Mono', monospace; }
  .mobile-revision > span:nth-child(3) { color: var(--muted); font-size: 9px; }
}

@media (max-width: 520px) {
  .revision-context { grid-template-columns: 1fr; }
  .revision-context > div { border-right: 0; border-bottom: 1px solid var(--line); }
  .revision-total { min-height: 48px; border-top: 0; }
  .mobile-revision { grid-template-columns: 1fr 24px; }
  .mobile-revision code, .mobile-revision > span:nth-child(3) { grid-column: 1; }
  .mobile-revision > svg { grid-column: 2; grid-row: 1 / 4; }
  .detail-facts { grid-template-columns: 1fr; }
  .detail-facts div, .detail-facts div:nth-child(2n), .detail-facts div:nth-last-child(-n + 2) { border-right: 0; border-bottom: 1px solid var(--line); }
  .detail-facts div:last-child { border-bottom: 0; }
  .document-json { height: 220px; }
  .desktop-pagination { display: none; }
  .mobile-pagination { display: flex; }
}
</style>
