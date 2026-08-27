<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Message } from '@arco-design/web-vue'
import LocalizedModalTitle from '@/components/LocalizedModalTitle.vue'
import { localizeApiError } from '@/i18n/errors'
import { controlPlane } from '@/services/controlPlane'

const props = defineProps<{ visible: boolean; suggestedName?: string }>()
const emit = defineEmits<{
  'update:visible': [value: boolean]
  created: [id: string]
}>()
const { t } = useI18n()

const name = ref('')
const loading = ref(false)
const error = ref('')

watch(() => props.visible, visible => {
  if (visible) {
    name.value = props.suggestedName ?? ''
    error.value = ''
  }
})

function close() {
  if (!loading.value) emit('update:visible', false)
}

async function create() {
  if (!name.value.trim()) {
    error.value = t('drafts.create.required')
    return
  }
  loading.value = true
  error.value = ''
  try {
    const result = await controlPlane.createDraft(name.value.trim())
    Message.success(t('drafts.create.success'))
    emit('update:visible', false)
    emit('created', result.data.id)
  } catch (reason) {
    error.value = localizeApiError(reason, 'drafts.create.failed')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <a-modal :visible="visible" :footer="false" :closable="false" title-align="start" unmount-on-close @cancel="close">
    <template #title>
      <LocalizedModalTitle :title="t('drafts.create.title')" :close-label="t('common.close')" :disabled="loading" @close="close" />
    </template>
    <form class="draft-form" @submit.prevent="create">
      <label for="draft-name">{{ t('drafts.create.name') }}</label>
      <a-input
        v-model="name"
        :max-length="120"
        :placeholder="t('drafts.create.placeholder')"
        allow-clear
        autofocus
        :input-attrs="{ id: 'draft-name', 'aria-label': t('drafts.create.name') }"
      />
      <p v-if="error" class="form-error">{{ error }}</p>
      <p class="form-note">{{ t('drafts.create.hint') }}</p>
      <div class="modal-actions">
        <a-button type="text" :disabled="loading" @click="close">{{ t('drafts.create.cancel') }}</a-button>
        <a-button type="primary" html-type="submit" :loading="loading">{{ t('drafts.create.submit') }}</a-button>
      </div>
    </form>
  </a-modal>
</template>

<style scoped>
.draft-form { display: grid; gap: 10px; padding-top: 6px; }
.draft-form label { color: var(--ink-soft); font-size: 12px; font-weight: 650; }
.form-note { margin: 2px 0 10px; color: var(--muted); font-size: 11px; }
.form-error { margin: 0; color: var(--red); font-size: 11px; }
.modal-actions { display: flex; justify-content: flex-end; gap: 8px; }
</style>