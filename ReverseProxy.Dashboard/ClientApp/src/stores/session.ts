import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { controlPlane } from '@/services/controlPlane'
import { ApiError, resetAntiforgeryToken } from '@/services/http'
import type { CurrentUser } from '@/types/controlPlane'

export const useSessionStore = defineStore('session', () => {
  const user = ref<CurrentUser | null>(null)
  const initialized = ref(false)
  const loading = ref(false)

  const canEdit = computed(() => user.value?.roles.some(role => role === 'Administrator' || role === 'Editor') ?? false)
  const canPublish = computed(() => user.value?.roles.some(role => role === 'Administrator' || role === 'Publisher') ?? false)

  async function restore() {
    if (initialized.value) return
    loading.value = true
    try {
      user.value = (await controlPlane.me()).data
    } catch (error) {
      if (!(error instanceof ApiError) || error.status !== 401) throw error
      user.value = null
    } finally {
      initialized.value = true
      loading.value = false
    }
  }

  async function login(userName: string, password: string, rememberMe: boolean) {
    loading.value = true
    resetAntiforgeryToken()
    try {
      user.value = (await controlPlane.login(userName, password, rememberMe)).data
      resetAntiforgeryToken()
      initialized.value = true
    } finally {
      loading.value = false
    }
  }

  async function logout() {
    try {
      await controlPlane.logout()
    } catch (error) {
      if (!(error instanceof ApiError) || error.status !== 401) throw error
    } finally {
      user.value = null
      resetAntiforgeryToken()
    }
  }

  return { user, initialized, loading, canEdit, canPublish, restore, login, logout }
})