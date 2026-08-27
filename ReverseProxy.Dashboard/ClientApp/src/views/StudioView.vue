<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { onBeforeRouteLeave, onBeforeRouteUpdate, useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { Message } from '@arco-design/web-vue'
import {
  ArrowLeft,
  Braces,
  CheckCircle2,
  CircleAlert,
  GitCompareArrows,
  Plus,
  Route as RouteIcon,
  Save,
  Send,
  Server,
  Trash2
} from '@lucide/vue'
import ClusterEditor from '@/components/ClusterEditor.vue'
import LocalizedModalTitle from '@/components/LocalizedModalTitle.vue'
import RouteEditor from '@/components/RouteEditor.vue'
import { localizeApiError, localizeValidationError } from '@/i18n/errors'
import { controlPlane } from '@/services/controlPlane'
import { useSessionStore } from '@/stores/session'
import type {
  ClusterConfig,
  ConfigDiff,
  ConfigOperation,
  Draft,
  ProxyConfigDocument,
  RouteConfig,
  ValidationResult
} from '@/types/controlPlane'

type ResourceKind = 'routes' | 'clusters'
type EditorMode = 'structured' | 'json'

const route = useRoute()
const router = useRouter()
const session = useSessionStore()
const { t, te } = useI18n()
const draftId = computed(() => String(route.params.id))

const draft = ref<Draft | null>(null)
const name = ref('')
const etag = ref('')
const baseline = ref('')
const loading = ref(true)
const saving = ref(false)
const validating = ref(false)
const reviewing = ref(false)
const publishing = ref(false)
const mode = ref<EditorMode>('structured')
const resourceKind = ref<ResourceKind>('routes')
const selectedIndex = ref(0)
const rawJson = ref('')
const jsonError = ref('')
const validation = ref<ValidationResult | null>(null)
const diff = ref<ConfigDiff | null>(null)
const reviewVisible = ref(false)
const publishedOperation = ref<ConfigOperation | null>(null)

const document = computed(() => draft.value?.document)
const resources = computed(() => resourceKind.value === 'routes' ? document.value?.routes ?? [] : document.value?.clusters ?? [])
const selectedRoute = computed(() => resourceKind.value === 'routes' ? document.value?.routes[selectedIndex.value] : undefined)
const selectedCluster = computed(() => resourceKind.value === 'clusters' ? document.value?.clusters[selectedIndex.value] : undefined)
const selectedTitle = computed(() => selectedRoute.value?.routeId ?? selectedCluster.value?.clusterId ?? t('studio.noResourceSelected'))
const dirty = computed(() => {
  if (!draft.value) return false
  return JSON.stringify({ name: name.value, document: draft.value.document }) !== baseline.value
})

function setBaseline() {
  if (!draft.value) return
  baseline.value = JSON.stringify({ name: name.value, document: draft.value.document })
}

function normalizeDocument(value: ProxyConfigDocument): ProxyConfigDocument {
  return {
    ...value,
    schemaVersion: value.schemaVersion ?? 1,
    routes: Array.isArray(value.routes) ? value.routes.map(item => ({ ...item, match: item.match ?? {} })) : [],
    clusters: Array.isArray(value.clusters)
      ? value.clusters.map(item => ({ ...item, destinations: item.destinations ?? {} }))
      : []
  }
}

async function load() {
  const requestedDraftId = draftId.value
  loading.value = true
  try {
    const result = await controlPlane.getDraft(requestedDraftId)
    if (requestedDraftId !== draftId.value) return
    result.data.document = normalizeDocument(result.data.document)
    draft.value = result.data
    name.value = result.data.name
    etag.value = result.response.headers.get('etag') ?? `"${result.data.rowVersion}"`
    rawJson.value = JSON.stringify(result.data.document, null, 2)
    setBaseline()
    applyEntryIntent()
  } catch (reason) {
    Message.error(localizeApiError(reason, 'studio.loadFailed'))
  } finally {
    loading.value = false
  }
}

function applyEntryIntent() {
  if (!document.value) return
  const requestedKind = route.query.kind
  if (requestedKind === 'routes' || requestedKind === 'clusters') {
    resourceKind.value = requestedKind
  } else if (!document.value.routes.length && document.value.clusters.length) {
    resourceKind.value = 'clusters'
  }

  const resourceId = typeof route.query.resource === 'string' ? route.query.resource : null
  if (resourceId) {
    const index = resources.value.findIndex(resource =>
      'routeId' in resource ? resource.routeId === resourceId : resource.clusterId === resourceId)
    selectedIndex.value = Math.max(0, index)
  } else if (route.query.action === 'add' && session.canEdit) {
    if (resourceKind.value === 'routes') addRoute()
    else addCluster()
  }

  if (requestedKind || resourceId || route.query.action) {
    void router.replace({ query: {} })
  }
}

function selectKind(kind: ResourceKind) {
  resourceKind.value = kind
  selectedIndex.value = 0
  mode.value = 'structured'
}

function selectResource(index: number) {
  selectedIndex.value = index
  mode.value = 'structured'
}

function addRoute() {
  if (!document.value || !session.canEdit) return
  const routeConfig: RouteConfig = {
    routeId: `route-${document.value.routes.length + 1}`,
    clusterId: document.value.clusters[0]?.clusterId,
    match: { path: '/{**catch-all}' }
  }
  document.value.routes.push(routeConfig)
  resourceKind.value = 'routes'
  selectedIndex.value = document.value.routes.length - 1
  mode.value = 'structured'
}

function addCluster() {
  if (!document.value || !session.canEdit) return
  const cluster: ClusterConfig = {
    clusterId: `cluster-${document.value.clusters.length + 1}`,
    destinations: { primary: { address: 'https://' } }
  }
  document.value.clusters.push(cluster)
  resourceKind.value = 'clusters'
  selectedIndex.value = document.value.clusters.length - 1
  mode.value = 'structured'
}

function removeSelected() {
  if (!document.value || !session.canEdit) return
  const collection = resourceKind.value === 'routes' ? document.value.routes : document.value.clusters
  collection.splice(selectedIndex.value, 1)
  selectedIndex.value = Math.max(0, Math.min(selectedIndex.value, collection.length - 1))
}

function switchMode(nextMode: EditorMode) {
  if (nextMode === 'json' && document.value) {
    rawJson.value = JSON.stringify(document.value, null, 2)
    jsonError.value = ''
  }
  mode.value = nextMode
}

function applyJson(showMessage = true) {
  try {
    const parsed = JSON.parse(rawJson.value) as ProxyConfigDocument
    if (parsed.schemaVersion !== 1 || !Array.isArray(parsed.routes) || !Array.isArray(parsed.clusters)) {
      throw new Error(t('studio.schemaExpected'))
    }
    if (!draft.value) return false
    draft.value.document = normalizeDocument(parsed)
    rawJson.value = JSON.stringify(draft.value.document, null, 2)
    jsonError.value = ''
    selectedIndex.value = 0
    if (showMessage) Message.success(t('studio.jsonApplied'))
    return true
  } catch (reason) {
    jsonError.value = reason instanceof SyntaxError
      ? t('studio.invalidJson')
      : reason instanceof Error ? reason.message : t('studio.invalidJson')
    return false
  }
}

async function save() {
  if (!draft.value || !session.canEdit) return false
  if (mode.value === 'json' && !applyJson(false)) return false
  if (!dirty.value) return true

  saving.value = true
  try {
    const result = await controlPlane.updateDraft(draftId.value, name.value.trim(), draft.value.document, etag.value)
    result.data.document = normalizeDocument(result.data.document)
    draft.value = result.data
    name.value = result.data.name
    etag.value = result.response.headers.get('etag') ?? `"${result.data.rowVersion}"`
    rawJson.value = JSON.stringify(result.data.document, null, 2)
    setBaseline()
    Message.success(t('studio.draftSaved'))
    return true
  } catch (reason) {
    Message.error(localizeApiError(reason, 'studio.saveFailed'))
    return false
  } finally {
    saving.value = false
  }
}

async function validate() {
  if (dirty.value && !await save()) return null
  validating.value = true
  try {
    validation.value = (await controlPlane.validateDraft(draftId.value)).data
    if (validation.value.isValid) Message.success(t('studio.configurationValid'))
    else Message.warning(formatValidationIssues(validation.value.errors.length))
    return validation.value
  } catch (reason) {
    Message.error(localizeApiError(reason, 'studio.validationFailed'))
    return null
  } finally {
    validating.value = false
  }
}

async function openReview() {
  if (dirty.value && !await save()) return
  reviewing.value = true
  try {
    const [validationResult, diffResult] = await Promise.all([
      controlPlane.validateDraft(draftId.value),
      controlPlane.diffDraft(draftId.value)
    ])
    validation.value = validationResult.data
    diff.value = diffResult.data
    if (!validation.value.isValid) {
      Message.warning(t('studio.resolveValidation'))
      return
    }
    reviewVisible.value = true
  } catch (reason) {
    Message.error(localizeApiError(reason, 'studio.reviewFailed'))
  } finally {
    reviewing.value = false
  }
}

async function publish() {
  if (!session.canPublish || !diff.value?.hasChanges) return
  publishing.value = true
  try {
    publishedOperation.value = (await controlPlane.publishDraft(draftId.value, etag.value)).data
    const refreshed = await controlPlane.getDraft(draftId.value)
    refreshed.data.document = normalizeDocument(refreshed.data.document)
    draft.value = refreshed.data
    name.value = refreshed.data.name
    etag.value = refreshed.response.headers.get('etag') ?? `"${refreshed.data.rowVersion}"`
    rawJson.value = JSON.stringify(refreshed.data.document, null, 2)
    setBaseline()
    diff.value = null
    reviewVisible.value = false
    Message.success(t('studio.revisionPublished', { revision: publishedOperation.value.revisionNumber }))
  } catch (reason) {
    Message.error(localizeApiError(reason, 'studio.publishFailed'))
  } finally {
    publishing.value = false
  }
}

function beforeUnload(event: BeforeUnloadEvent) {
  if (!dirty.value) return
  event.preventDefault()
  event.returnValue = ''
}

function formatValidationIssues(count: number) {
  return t('studio.validationIssues', { count }, count)
}

function formatIssuesFound(count: number) {
  return t('studio.issuesFound', { count }, count)
}

function formatOperationStatus(status: string) {
  const key = `studio.operation.${status.toLowerCase()}`
  return te(key) ? t(key) : status
}

watch(dirty, isDirty => {
  if (isDirty) validation.value = null
})

onBeforeRouteLeave(() => !dirty.value || window.confirm(t('studio.discardConfirm')))
onBeforeRouteUpdate(to => {
  if (String(to.params.id) === draftId.value) return true
  return !dirty.value || window.confirm(t('studio.discardConfirm'))
})
watch(() => route.params.id, () => {
  draft.value = null
  validation.value = null
  diff.value = null
  reviewVisible.value = false
  publishedOperation.value = null
  selectedIndex.value = 0
  void load()
})
onMounted(() => {
  window.addEventListener('beforeunload', beforeUnload)
  load()
})
onBeforeUnmount(() => window.removeEventListener('beforeunload', beforeUnload))
</script>

<template>
  <div class="page studio-page">
    <div v-if="loading" class="studio-loading"><a-spin :size="32" /></div>

    <template v-else-if="draft">
      <header class="studio-header">
        <div class="studio-title-row">
          <button class="back-button" type="button" :title="t('studio.backToDrafts')" :aria-label="t('studio.backToDrafts')" @click="router.push({ name: 'changes' })">
            <ArrowLeft :size="18" />
          </button>
          <div class="title-copy">
            <p class="eyebrow">{{ t('studio.draftEyebrow', { revision: draft.baseRevisionNumber }) }}</p>
            <a-input
              v-model="name"
              class="draft-title-input"
              :disabled="!session.canEdit"
              :max-length="120"
              :input-attrs="{ 'aria-label': t('studio.draftName') }"
            />
          </div>
        </div>
        <div class="draft-state">
          <span :class="['save-state', { dirty }]">{{ dirty ? t('studio.unsavedChanges') : t('studio.allChangesSaved') }}</span>
          <span class="etag mono">ETAG {{ etag.replaceAll('"', '') }}</span>
        </div>
      </header>

      <a-alert v-if="publishedOperation" class="published-banner" type="success">
        {{ t('studio.publishedState', { revision: publishedOperation.revisionNumber, status: formatOperationStatus(publishedOperation.status) }) }}
      </a-alert>

      <div class="studio-grid">
        <aside class="resource-rail panel">
          <div class="resource-tabs">
            <button :class="{ active: resourceKind === 'routes' }" type="button" @click="selectKind('routes')">
              <RouteIcon :size="15" /> {{ t('studio.routes') }} <span>{{ document?.routes.length }}</span>
            </button>
            <button :class="{ active: resourceKind === 'clusters' }" type="button" @click="selectKind('clusters')">
              <Server :size="15" /> {{ t('studio.clusters') }} <span>{{ document?.clusters.length }}</span>
            </button>
          </div>

          <div class="resource-list">
            <button
              v-for="(resource, index) in resources"
              :key="index"
              :class="{ active: selectedIndex === index }"
              type="button"
              @click="selectResource(index)"
            >
              <span>{{ 'routeId' in resource ? resource.routeId : resource.clusterId }}</span>
              <small class="mono">{{ String(index + 1).padStart(2, '0') }}</small>
            </button>
          </div>

          <button v-if="session.canEdit" class="add-resource" type="button" @click="resourceKind === 'routes' ? addRoute() : addCluster()">
            <Plus :size="15" /> {{ resourceKind === 'routes' ? t('studio.addRoute') : t('studio.addCluster') }}
          </button>
        </aside>

        <section class="editor-panel panel">
          <div class="editor-toolbar">
            <div>
              <span class="resource-type mono">{{ resourceKind === 'routes' ? t('editor.route.tag') : t('editor.cluster.tag') }}</span>
              <h2>{{ mode === 'json' ? t('studio.configurationDocument') : selectedTitle }}</h2>
            </div>
            <div class="editor-tools">
              <div class="mode-switch" :aria-label="t('studio.editorMode')">
                <button :class="{ active: mode === 'structured' }" type="button" @click="switchMode('structured')">{{ t('studio.form') }}</button>
                <button :class="{ active: mode === 'json' }" type="button" @click="switchMode('json')"><Braces :size="13" /> JSON</button>
              </div>
              <a-popconfirm v-if="mode === 'structured' && resources.length && session.canEdit" :content="t('studio.removeResourceConfirm')" @ok="removeSelected">
                <button class="remove-resource" type="button" :title="t('studio.removeResource')" :aria-label="t('studio.removeResource')"><Trash2 :size="16" /></button>
              </a-popconfirm>
            </div>
          </div>

          <div class="editor-body" :class="{ readonly: !session.canEdit }">
            <template v-if="mode === 'structured'">
              <RouteEditor v-if="selectedRoute" :route="selectedRoute" :clusters="document?.clusters ?? []" />
              <ClusterEditor v-else-if="selectedCluster" :cluster="selectedCluster" />
              <div v-else class="empty-state">
                <div>
                  <component :is="resourceKind === 'routes' ? RouteIcon : Server" :size="30" :stroke-width="1.5" />
                  <p>{{ resourceKind === 'routes' ? t('studio.noRoutes') : t('studio.noClusters') }}</p>
                </div>
              </div>
            </template>
            <template v-else>
              <textarea v-model="rawJson" class="json-editor mono" spellcheck="false" :readonly="!session.canEdit" :aria-label="t('studio.configurationJson')" />
              <div class="json-footer">
                <span :class="['json-message', { error: jsonError }]">{{ jsonError || t('studio.schemaHint') }}</span>
                <a-button v-if="session.canEdit" size="small" @click="applyJson(true)">{{ t('studio.applyJson') }}</a-button>
              </div>
            </template>
          </div>
        </section>

        <aside class="change-panel panel">
          <div class="change-header">
            <span class="resource-type mono">{{ t('studio.changeControl') }}</span>
            <h2>{{ t('studio.draftReadiness') }}</h2>
          </div>
          <dl class="draft-facts">
            <div><dt>{{ t('studio.baseRevision') }}</dt><dd class="mono">r{{ draft.baseRevisionNumber }}</dd></div>
            <div><dt>{{ t('studio.routes') }}</dt><dd class="mono">{{ document?.routes.length }}</dd></div>
            <div><dt>{{ t('studio.clusters') }}</dt><dd class="mono">{{ document?.clusters.length }}</dd></div>
            <div><dt>{{ t('studio.hash') }}</dt><dd class="hash">{{ draft.contentHash.slice(0, 12) }}</dd></div>
          </dl>

          <div class="validation-state">
            <template v-if="validation?.isValid">
              <CheckCircle2 :size="18" /><div><strong>{{ t('studio.validConfiguration') }}</strong><span>{{ t('studio.readyForReview') }}</span></div>
            </template>
            <template v-else-if="validation">
              <CircleAlert :size="18" /><div><strong>{{ formatIssuesFound(validation.errors.length) }}</strong><span>{{ t('studio.publishingBlocked') }}</span></div>
            </template>
            <template v-else>
              <CircleAlert :size="18" /><div><strong>{{ t('studio.notValidated') }}</strong><span>{{ t('studio.runServerValidation') }}</span></div>
            </template>
          </div>

          <div v-if="validation && !validation.isValid" class="validation-errors">
            <div v-for="issue in validation.errors.slice(0, 4)" :key="`${issue.path}-${issue.code}`">
              <span class="mono">{{ issue.path }}</span>
              <p>{{ localizeValidationError(issue) }}</p>
            </div>
          </div>
        </aside>
      </div>

      <footer class="action-bar">
        <div class="action-status">
          <i :class="{ dirty }" />
          <span>{{ dirty ? t('studio.draftHasLocalChanges') : t('studio.draftMatchesServer') }}</span>
        </div>
        <div class="action-buttons">
          <a-button :loading="validating" @click="validate"><CheckCircle2 :size="15" /> {{ t('studio.validate') }}</a-button>
          <a-button v-if="session.canEdit" :disabled="!dirty" :loading="saving" @click="save"><Save :size="15" /> {{ t('studio.saveChanges') }}</a-button>
          <a-button v-if="session.canPublish" type="primary" :loading="reviewing" @click="openReview"><GitCompareArrows :size="15" /> {{ t('studio.reviewPublish') }}</a-button>
        </div>
      </footer>

      <a-modal v-model:visible="reviewVisible" :footer="false" :closable="false" title-align="start" width="680px">
        <template #title>
          <LocalizedModalTitle :title="t('studio.reviewTitle')" :close-label="t('common.close')" :disabled="publishing" @close="reviewVisible = false" />
        </template>
        <div v-if="diff" class="review-content">
          <div class="hash-change">
            <div><span>{{ t('studio.base') }}</span><code>{{ diff.baseHash.slice(0, 12) }}</code></div>
            <ArrowLeft class="hash-arrow" :size="17" />
            <div><span>{{ t('studio.candidate') }}</span><code>{{ diff.candidateHash.slice(0, 12) }}</code></div>
          </div>

          <div v-if="diff.hasChanges" class="diff-grid">
            <section v-for="group in ['routes', 'clusters'] as const" :key="group">
              <h3>{{ t(`studio.${group}`) }}</h3>
              <div class="diff-row added"><span>{{ t('studio.added') }}</span><strong>{{ diff[group].added.length }}</strong></div>
              <div class="diff-row modified"><span>{{ t('studio.modified') }}</span><strong>{{ diff[group].modified.length }}</strong></div>
              <div class="diff-row removed"><span>{{ t('studio.removed') }}</span><strong>{{ diff[group].removed.length }}</strong></div>
              <p class="diff-names mono">{{ [...diff[group].added, ...diff[group].modified, ...diff[group].removed].join(', ') || t('studio.noChanges') }}</p>
            </section>
          </div>
          <a-alert v-else type="info">{{ t('studio.noChangesFromBase') }}</a-alert>

          <div class="publish-note">
            <CheckCircle2 :size="17" />
            <p><strong>{{ t('studio.validationPassed') }}</strong><span>{{ t('studio.publishImmutable') }}</span></p>
          </div>
          <div class="review-actions">
            <a-button @click="reviewVisible = false">{{ t('common.cancel') }}</a-button>
            <a-button type="primary" :disabled="!diff.hasChanges" :loading="publishing" @click="publish"><Send :size="15" /> {{ t('studio.publishRevision') }}</a-button>
          </div>
        </div>
      </a-modal>
    </template>
  </div>
</template>

<style scoped>
.studio-page { width: 100%; max-width: 1700px; padding-bottom: 105px; }
.studio-loading { display: grid; min-height: 65vh; place-items: center; }
.studio-header { display: flex; margin-bottom: 20px; align-items: center; justify-content: space-between; gap: 20px; }
.studio-title-row { display: flex; min-width: 0; align-items: center; gap: 14px; }
.back-button { display: grid; width: 38px; height: 38px; padding: 0; color: var(--ink); background: var(--surface); border: 1px solid var(--line); border-radius: 4px; cursor: pointer; place-items: center; }
.title-copy { min-width: 0; }
.title-copy .eyebrow { margin-bottom: 3px; }
.draft-title-input { width: min(520px, 50vw); padding: 0; background: transparent; border: 0; box-shadow: none; }
.draft-title-input :deep(input) { height: 38px; padding: 0; color: var(--ink); font: 650 27px/1 'Space Grotesk Variable', sans-serif; }
.draft-state { display: grid; justify-items: end; gap: 5px; }
.save-state { color: var(--green); font-size: 11px; font-weight: 650; }
.save-state.dirty { color: var(--amber); }
.etag { color: var(--muted); font-size: 9px; }
.published-banner { margin-bottom: 14px; }
.studio-grid { display: grid; min-height: 590px; grid-template-columns: 218px minmax(430px, 1fr) 246px; gap: 12px; }
.resource-rail { display: flex; min-width: 0; overflow: hidden; flex-direction: column; }
.resource-tabs { display: grid; padding: 8px; background: var(--surface-subtle); border-bottom: 1px solid var(--line); grid-template-columns: 1fr 1fr; gap: 4px; }
.resource-tabs button { display: flex; min-width: 0; height: 34px; padding: 0 8px; color: var(--muted); background: transparent; border: 0; border-radius: 3px; cursor: pointer; align-items: center; gap: 6px; font-size: 10px; font-weight: 650; }
.resource-tabs button.active { color: var(--ink); background: var(--surface); box-shadow: 0 1px 3px rgb(23 35 31 / 10%); }
.resource-tabs button span { margin-left: auto; font: 400 9px/1 'IBM Plex Mono', monospace; }
.resource-list { display: grid; padding: 8px; overflow-y: auto; align-content: start; gap: 3px; flex: 1; }
.resource-list button { display: flex; min-width: 0; min-height: 38px; padding: 0 9px; color: var(--ink-soft); background: transparent; border: 1px solid transparent; border-radius: 3px; cursor: pointer; align-items: center; gap: 8px; text-align: left; }
.resource-list button:hover { background: var(--surface-subtle); }
.resource-list button.active { color: var(--blue-dark); background: #edf1ff; border-color: #d6dfff; }
.resource-list button span { overflow: hidden; font-size: 11px; font-weight: 620; text-overflow: ellipsis; white-space: nowrap; }
.resource-list button small { margin-left: auto; color: var(--muted); font-size: 8px; }
.add-resource { display: flex; height: 42px; color: var(--blue); background: transparent; border: 0; border-top: 1px solid var(--line); cursor: pointer; align-items: center; justify-content: center; gap: 7px; font-size: 11px; font-weight: 650; }
.editor-panel { min-width: 0; overflow: hidden; }
.editor-toolbar { display: flex; min-height: 72px; padding: 14px 18px; background: var(--surface-subtle); border-bottom: 1px solid var(--line); align-items: center; justify-content: space-between; gap: 16px; }
.resource-type { color: var(--blue); font-size: 8px; }
.editor-toolbar h2, .change-header h2 { overflow: hidden; margin: 4px 0 0; font: 620 17px/1.2 'Space Grotesk Variable', sans-serif; text-overflow: ellipsis; white-space: nowrap; }
.editor-tools { display: flex; align-items: center; gap: 7px; }
.mode-switch { display: flex; padding: 3px; background: #e7ece9; border-radius: 4px; }
.mode-switch button { display: inline-flex; height: 28px; padding: 0 9px; color: var(--muted); background: transparent; border: 0; border-radius: 3px; cursor: pointer; align-items: center; gap: 5px; font-size: 10px; font-weight: 650; }
.mode-switch button.active { color: var(--ink); background: var(--surface); box-shadow: 0 1px 3px rgb(23 35 31 / 12%); }
.remove-resource { display: grid; width: 34px; height: 34px; padding: 0; color: var(--muted); background: transparent; border: 1px solid var(--line); border-radius: 4px; cursor: pointer; place-items: center; }
.remove-resource:hover { color: var(--red); background: var(--red-soft); border-color: #e7b6b2; }
.editor-body { min-height: 516px; padding: 24px; }
.editor-body.readonly { opacity: .86; }
.json-editor { width: 100%; height: 430px; padding: 16px; resize: vertical; color: #dce6e2; background: #17231f; border: 1px solid #2f413a; border-radius: 4px; outline: 0; font-size: 11px; line-height: 1.7; tab-size: 2; }
.json-editor:focus { border-color: #67bca7; box-shadow: 0 0 0 3px rgb(10 143 120 / 16%); }
.json-footer { display: flex; min-height: 44px; align-items: center; justify-content: space-between; gap: 12px; }
.json-message { color: var(--muted); font-size: 10px; }
.json-message.error { color: var(--red); }
.change-panel { align-self: start; overflow: hidden; }
.change-header { padding: 18px; background: var(--surface-subtle); border-bottom: 1px solid var(--line); }
.draft-facts { margin: 0; padding: 8px 18px; }
.draft-facts div { display: flex; min-height: 42px; border-bottom: 1px solid var(--line); align-items: center; justify-content: space-between; gap: 10px; }
.draft-facts dt { color: var(--muted); font-size: 10px; }
.draft-facts dd { margin: 0; font-size: 11px; }
.draft-facts .hash { max-width: 95px; }
.validation-state { display: flex; margin: 10px 12px 12px; padding: 12px; color: var(--amber); background: var(--amber-soft); border-radius: 4px; gap: 9px; }
.validation-state:has(.lucide-circle-check-big) { color: var(--green); background: var(--green-soft); }
.validation-state div { display: grid; gap: 2px; }
.validation-state strong { font-size: 10px; }
.validation-state span { font-size: 9px; opacity: .78; }
.validation-errors { display: grid; padding: 0 12px 12px; gap: 7px; }
.validation-errors div { padding: 9px; background: var(--red-soft); border-left: 2px solid var(--red); }
.validation-errors span { color: var(--red); font-size: 8px; }
.validation-errors p { margin: 4px 0 0; color: var(--ink-soft); font-size: 9px; line-height: 1.4; }
.action-bar { position: fixed; right: 0; bottom: 0; left: 246px; z-index: 15; display: flex; min-height: 66px; padding: 12px 38px; background: rgb(255 255 255 / 94%); border-top: 1px solid var(--line); box-shadow: 0 -8px 28px rgb(23 35 31 / 7%); backdrop-filter: blur(12px); align-items: center; justify-content: space-between; gap: 18px; }
.action-status { display: flex; color: var(--muted); align-items: center; gap: 9px; font-size: 10px; }
.action-status i { width: 7px; height: 7px; background: var(--green); border-radius: 50%; }
.action-status i.dirty { background: var(--amber); }
.action-buttons { display: flex; gap: 8px; }
.action-buttons .arco-btn, .review-actions .arco-btn { display: inline-flex; align-items: center; gap: 6px; }
.review-content { display: grid; gap: 22px; }
.hash-change { display: grid; padding: 14px; background: var(--surface-subtle); border: 1px solid var(--line); border-radius: 4px; grid-template-columns: 1fr 24px 1fr; align-items: center; gap: 12px; }
.hash-change div { display: grid; gap: 5px; }
.hash-change div:last-child { text-align: right; }
.hash-change span { color: var(--muted); font-size: 9px; text-transform: uppercase; }
.hash-change code { color: var(--ink); font: 400 11px/1 'IBM Plex Mono', monospace; }
.hash-arrow { color: var(--muted); transform: rotate(180deg); }
.diff-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.diff-grid section { padding: 16px; border: 1px solid var(--line); border-radius: 4px; }
.diff-grid h3 { margin: 0 0 12px; font: 620 14px/1 'Space Grotesk Variable', sans-serif; text-transform: capitalize; }
.diff-row { display: flex; min-height: 28px; border-bottom: 1px solid var(--line); align-items: center; justify-content: space-between; font-size: 10px; }
.diff-row.added strong { color: var(--green); }
.diff-row.modified strong { color: var(--amber); }
.diff-row.removed strong { color: var(--red); }
.diff-names { min-height: 25px; margin: 10px 0 0; color: var(--muted); font-size: 8px; line-height: 1.5; }
.publish-note { display: flex; padding: 13px; color: var(--green); background: var(--green-soft); border-radius: 4px; gap: 10px; }
.publish-note p { display: grid; margin: 0; gap: 2px; }
.publish-note strong { font-size: 10px; }
.publish-note span { color: var(--ink-soft); font-size: 9px; }
.review-actions { display: flex; justify-content: flex-end; gap: 8px; }

@media (max-width: 1220px) {
  .studio-grid { grid-template-columns: 210px minmax(430px, 1fr); }
  .change-panel { grid-column: 1 / -1; }
  .draft-facts { display: grid; grid-template-columns: repeat(4, 1fr); }
  .draft-facts div { padding: 0 10px; border-right: 1px solid var(--line); border-bottom: 0; }
}

@media (max-width: 900px) {
  .studio-page { padding-bottom: 158px; }
  .action-bar { left: 0; bottom: 68px; padding: 10px 18px; }
}

@media (max-width: 760px) {
  .studio-header { align-items: flex-start; }
  .draft-title-input { width: calc(100vw - 120px); }
  .draft-state { display: none; }
  .studio-grid { grid-template-columns: 1fr; }
  .resource-rail { max-height: 184px; }
  .resource-list { display: flex; overflow-x: auto; }
  .resource-list button { min-width: 145px; }
  .editor-toolbar { align-items: flex-start; }
  .editor-tools { flex-shrink: 0; }
  .editor-body { min-height: 460px; padding: 18px; }
  .draft-facts { grid-template-columns: repeat(2, 1fr); }
  .draft-facts div { border-bottom: 1px solid var(--line); }
  .action-bar { align-items: stretch; flex-direction: column; }
  .action-status { display: none; }
  .action-buttons { display: grid; grid-template-columns: repeat(3, 1fr); }
  .action-buttons .arco-btn { width: 100%; padding: 0 7px; font-size: 10px; justify-content: center; }
  .diff-grid { grid-template-columns: 1fr; }
}

@media (max-width: 460px) {
  .editor-toolbar { flex-direction: column; }
  .editor-tools { width: 100%; justify-content: space-between; }
  .action-buttons .arco-btn :deep(svg) { display: none; }
}
</style>