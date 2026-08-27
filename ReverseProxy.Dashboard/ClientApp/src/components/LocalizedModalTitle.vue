<script setup lang="ts">
import { X } from '@lucide/vue'

defineProps<{
  title: string
  closeLabel: string
  disabled?: boolean
}>()

defineEmits<{ close: [] }>()
</script>

<template>
  <div class="localized-modal-title">
    <span>{{ title }}</span>
    <button
      type="button"
      :title="closeLabel"
      :aria-label="closeLabel"
      :disabled="disabled"
      @click.stop="$emit('close')"
    >
      <X :size="15" />
    </button>
  </div>
</template>

<style scoped>
.localized-modal-title { display: flex; width: 100%; align-items: center; justify-content: space-between; gap: 16px; }
.localized-modal-title button { display: grid; width: 30px; height: 30px; margin-right: -8px; padding: 0; color: var(--muted); background: transparent; border: 0; border-radius: 3px; cursor: pointer; place-items: center; }
.localized-modal-title button:hover { color: var(--ink); background: var(--surface-subtle); }
.localized-modal-title button:disabled { cursor: not-allowed; opacity: .45; }
</style>