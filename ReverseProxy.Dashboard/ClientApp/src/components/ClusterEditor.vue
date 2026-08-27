<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { Plus, Trash2 } from '@lucide/vue'
import type { ClusterConfig, DestinationConfig } from '@/types/controlPlane'

const props = defineProps<{ cluster: ClusterConfig }>()
const { t } = useI18n()

function uniqueDestinationId() {
  let index = Object.keys(props.cluster.destinations).length + 1
  while (`destination-${index}` in props.cluster.destinations) index += 1
  return `destination-${index}`
}

function addDestination() {
  props.cluster.destinations[uniqueDestinationId()] = { address: 'https://' }
}

function removeDestination(id: string) {
  delete props.cluster.destinations[id]
}

function renameDestination(previousId: string, nextId: string, destination: DestinationConfig) {
  const normalized = nextId.trim()
  if (!normalized || normalized === previousId || normalized in props.cluster.destinations) return
  const entries = Object.entries(props.cluster.destinations)
  props.cluster.destinations = Object.fromEntries(entries.map(([id, value]) => id === previousId ? [normalized, destination] : [id, value]))
}
</script>

<template>
  <div class="resource-editor">
    <div class="editor-section">
      <div class="editor-section-title"><span>{{ t('editor.cluster.identity') }}</span><small>{{ t('editor.cluster.tag') }}</small></div>
      <div class="form-grid">
        <label class="field">
          <span>{{ t('editor.cluster.clusterId') }}</span>
          <a-input v-model="cluster.clusterId" placeholder="orders" />
        </label>
        <label class="field">
          <span>{{ t('editor.cluster.loadBalancing') }}</span>
          <a-select v-model="cluster.loadBalancingPolicy" allow-clear :placeholder="t('editor.cluster.defaultPolicy')">
            <a-option value="PowerOfTwoChoices">{{ t('editor.cluster.powerOfTwo') }}</a-option>
            <a-option value="RoundRobin">{{ t('editor.cluster.roundRobin') }}</a-option>
            <a-option value="LeastRequests">{{ t('editor.cluster.leastRequests') }}</a-option>
            <a-option value="Random">{{ t('editor.cluster.random') }}</a-option>
          </a-select>
        </label>
      </div>
    </div>

    <div class="editor-section">
      <div class="editor-section-title">
        <span>{{ t('editor.cluster.destinations') }}</span>
        <a-button size="mini" @click="addDestination"><Plus :size="13" /> {{ t('editor.cluster.addDestination') }}</a-button>
      </div>
      <div v-if="Object.keys(cluster.destinations).length === 0" class="destination-empty">{{ t('editor.cluster.emptyDestinations') }}</div>
      <div v-else class="destination-list">
        <div v-for="(destination, id) in cluster.destinations" :key="id" class="destination-row">
          <label class="field">
            <span>{{ t('editor.cluster.destinationId') }}</span>
            <a-input :model-value="id" @change="value => renameDestination(id, String(value), destination)" />
          </label>
          <label class="field address-field">
            <span>{{ t('editor.cluster.address') }}</span>
            <a-input v-model="destination.address" placeholder="https://service.internal/" />
          </label>
          <button class="remove-destination" type="button" :title="t('editor.cluster.removeDestination')" :aria-label="t('editor.cluster.removeNamed', { id })" @click="removeDestination(id)">
            <Trash2 :size="16" />
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.resource-editor { display: grid; gap: 28px; }
.editor-section { display: grid; gap: 15px; }
.editor-section-title { display: flex; padding-bottom: 9px; border-bottom: 1px solid var(--line); align-items: center; justify-content: space-between; }
.editor-section-title span { font: 620 14px/1 'Space Grotesk Variable', sans-serif; }
.editor-section-title small { color: var(--muted); font: 400 9px/1 'IBM Plex Mono', monospace; }
.editor-section-title .arco-btn { display: inline-flex; align-items: center; gap: 4px; }
.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.field { display: grid; min-width: 0; gap: 7px; color: var(--ink-soft); font-size: 11px; font-weight: 650; }
.destination-list { display: grid; gap: 10px; }
.destination-row { display: grid; padding: 14px; background: var(--surface-subtle); border: 1px solid var(--line); border-radius: 4px; grid-template-columns: minmax(130px, .55fr) minmax(220px, 1.45fr) 34px; align-items: end; gap: 10px; }
.remove-destination { display: grid; width: 32px; height: 32px; padding: 0; color: var(--muted); background: transparent; border: 1px solid var(--line); border-radius: 4px; cursor: pointer; place-items: center; }
.remove-destination:hover { color: var(--red); border-color: #e7b6b2; background: var(--red-soft); }
.destination-empty { padding: 28px; color: var(--muted); background: var(--surface-subtle); border: 1px dashed var(--line-strong); text-align: center; }
@media (max-width: 700px) { .form-grid { grid-template-columns: 1fr; } .destination-row { grid-template-columns: 1fr 34px; } .address-field { grid-column: 1 / -1; grid-row: 2; } }
</style>