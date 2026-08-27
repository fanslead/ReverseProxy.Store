<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { Message } from '@arco-design/web-vue'
import { FilePlus2, Search } from '@lucide/vue'
import CreateDraftModal from '@/components/CreateDraftModal.vue'
import DraftTable from '@/components/DraftTable.vue'
import { localizeApiError } from '@/i18n/errors'
import { controlPlane } from '@/services/controlPlane'
import { useSessionStore } from '@/stores/session'
import type { DraftSummary } from '@/types/controlPlane'

const router = useRouter()
const session = useSessionStore()
const { t } = useI18n()
const drafts = ref<DraftSummary[]>([])
const search = ref('')
const page = ref(1)
const total = ref(0)
const loading = ref(true)
const createVisible = ref(false)

async function load() {
  loading.value = true
  try {
    const result = await controlPlane.listDrafts(search.value, page.value, 25)
    drafts.value = result.data.items
    total.value = result.data.total
  } catch (reason) {
    Message.error(localizeApiError(reason, 'drafts.loadFailed'))
  } finally {
    loading.value = false
  }
}

function searchDrafts() {
  page.value = 1
  load()
}

function openDraft(id: string) {
  router.push({ name: 'studio', params: { id } })
}

watch(page, load)
onMounted(load)
</script>

<template>
  <div class="page">
    <header class="page-header">
      <div>
        <p class="eyebrow">{{ t('drafts.eyebrow') }}</p>
        <h1 class="page-title">{{ t('drafts.title') }}</h1>
        <p class="page-summary">{{ t('drafts.summary') }}</p>
      </div>
      <a-button v-if="session.canEdit" type="primary" @click="createVisible = true">
        <FilePlus2 :size="16" /> {{ t('drafts.newDraft') }}
      </a-button>
    </header>

    <form class="draft-toolbar" role="search" @submit.prevent="searchDrafts">
      <a-input v-model="search" allow-clear :placeholder="t('drafts.searchPlaceholder')" :input-attrs="{ 'aria-label': t('drafts.searchPlaceholder') }" @clear="searchDrafts">
        <template #prefix><Search :size="15" /></template>
      </a-input>
      <a-button html-type="submit">{{ t('drafts.search') }}</a-button>
      <span class="draft-total mono">{{ t('drafts.total', { count: total }) }}</span>
    </form>

    <DraftTable :drafts="drafts" :loading="loading" />

    <div v-if="total > 25" class="pagination-wrap">
      <a-pagination v-model:current="page" :total="total" :page-size="25" :show-total="true" />
    </div>

    <CreateDraftModal v-model:visible="createVisible" @created="openDraft" />
  </div>
</template>

<style scoped>
.page-header .arco-btn { display: inline-flex; align-items: center; gap: 7px; }
.draft-toolbar { display: flex; align-items: center; gap: 8px; margin-bottom: 14px; }
.draft-toolbar .arco-input-wrapper { width: min(380px, 100%); }
.draft-total { margin-left: auto; color: var(--muted); font-size: 10px; }
.pagination-wrap { display: flex; padding-top: 18px; justify-content: flex-end; }
@media (max-width: 620px) {
  .draft-toolbar { flex-wrap: wrap; }
  .draft-toolbar .arco-input-wrapper { width: calc(100% - 76px); }
  .draft-total { width: 100%; margin-left: 0; }
}
</style>