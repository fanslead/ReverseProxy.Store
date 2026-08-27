<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { ChevronRight, FileCode2 } from '@lucide/vue'
import type { DraftSummary } from '@/types/controlPlane'

defineProps<{ drafts: DraftSummary[]; loading?: boolean }>()

const { locale, t } = useI18n()

const formatter = computed(() => new Intl.DateTimeFormat(locale.value, {
  month: 'short',
  day: 'numeric',
  hour: '2-digit',
  minute: '2-digit'
}))

function formatDate(value: string) {
  return formatter.value.format(new Date(value))
}
</script>

<template>
  <div class="draft-table-wrap panel">
    <a-spin :loading="loading" class="table-spin">
      <div v-if="!loading && drafts.length === 0" class="empty-state">
        <div>
          <FileCode2 :size="30" :stroke-width="1.5" />
          <p>{{ t('drafts.empty') }}</p>
        </div>
      </div>
      <div v-else class="table-scroll">
        <table class="draft-table">
          <thead>
            <tr>
              <th>{{ t('drafts.table.name') }}</th>
              <th>{{ t('drafts.table.base') }}</th>
              <th>{{ t('drafts.table.resources') }}</th>
              <th>{{ t('drafts.table.updated') }}</th>
              <th :aria-label="t('drafts.table.open')" />
            </tr>
          </thead>
          <tbody>
            <tr v-for="draft in drafts" :key="draft.id">
              <td>
                <RouterLink :to="{ name: 'studio', params: { id: draft.id } }" class="draft-name">
                  <strong>{{ draft.name }}</strong>
                  <span class="hash">{{ draft.contentHash.slice(0, 12) }}</span>
                </RouterLink>
              </td>
              <td><span class="revision-chip mono">r{{ draft.baseRevisionNumber }}</span></td>
              <td><span class="resource-count mono">{{ t('drafts.table.resourceCount', { routes: draft.routeCount, clusters: draft.clusterCount }) }}</span></td>
              <td><span class="updated">{{ formatDate(draft.updatedAt) }}</span></td>
              <td>
                <RouterLink :to="{ name: 'studio', params: { id: draft.id } }" class="open-link" :aria-label="t('drafts.table.openNamed', { name: draft.name })">
                  <ChevronRight :size="17" />
                </RouterLink>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </a-spin>
  </div>
</template>

<style scoped>
.draft-table-wrap { width: 100%; max-width: 100%; min-width: 0; overflow: hidden; }
.table-spin { display: block; width: 100%; min-width: 0; min-height: 120px; }
.table-scroll { overflow-x: auto; }
.draft-table { width: 100%; border-collapse: collapse; table-layout: fixed; }
.draft-table th { height: 42px; padding: 0 18px; color: var(--muted); background: var(--surface-subtle); border-bottom: 1px solid var(--line); font: 400 10px/1 'IBM Plex Mono', monospace; text-align: left; text-transform: uppercase; }
.draft-table td { height: 64px; padding: 0 18px; border-bottom: 1px solid var(--line); font-size: 12px; }
.draft-table tbody tr:last-child td { border-bottom: 0; }
.draft-table tbody tr:hover { background: #f8faf9; }
.draft-table th:first-child { width: 42%; }
.draft-table th:nth-child(2) { width: 13%; }
.draft-table th:nth-child(3) { width: 17%; }
.draft-table th:nth-child(4) { width: 23%; }
.draft-table th:last-child { width: 5%; }
.draft-name { display: grid; gap: 4px; }
.draft-name strong { overflow: hidden; font-size: 13px; text-overflow: ellipsis; white-space: nowrap; }
.revision-chip { display: inline-block; padding: 4px 6px; color: var(--blue-dark); background: #e8edff; border-radius: 3px; font-size: 10px; }
.resource-count, .updated { color: var(--muted); font-size: 11px; }
.open-link { display: grid; width: 30px; height: 30px; color: var(--muted); border-radius: 3px; place-items: center; }
.open-link:hover { color: var(--blue); background: #eaf0ff; }
@media (max-width: 620px) {
  .draft-table { min-width: 640px; }
}
</style>