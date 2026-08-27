<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ArrowRight, CheckCircle2, LockKeyhole } from '@lucide/vue'
import BrandMark from '@/components/BrandMark.vue'
import LanguageSwitcher from '@/components/LanguageSwitcher.vue'
import { localizeApiError } from '@/i18n/errors'
import { useSessionStore } from '@/stores/session'
import { ApiError } from '@/services/http'

const route = useRoute()
const router = useRouter()
const session = useSessionStore()
const { t } = useI18n()
const form = reactive({ userName: '', password: '', rememberMe: false })
const error = ref('')

async function submit() {
  error.value = ''
  try {
    await session.login(form.userName, form.password, form.rememberMe)
    const destination = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    await router.replace(destination)
  } catch (reason) {
    error.value = reason instanceof ApiError && reason.status === 401
      ? t('auth.invalidCredentials')
      : reason instanceof ApiError && reason.status === 429
        ? t('auth.locked')
        : localizeApiError(reason, 'auth.signInFailed')
  }
}
</script>

<template>
  <main class="login-page">
    <header><BrandMark /><LanguageSwitcher /></header>

    <section class="login-workspace">
      <div class="login-intro">
        <p class="eyebrow">{{ t('auth.operatorAccess') }}</p>
        <h1 class="display-type">{{ t('auth.headlinePrimary') }}<br><span>{{ t('auth.headlineAccent') }}</span></h1>
        <p>{{ t('auth.summary') }}</p>

        <div class="login-trace" aria-hidden="true">
          <div class="trace-caption mono">{{ t('auth.traceCaption') }}</div>
          <div class="trace-line"><i /><i /><i /><span /></div>
          <div class="trace-labels mono"><span>{{ t('auth.request') }}</span><span>{{ t('auth.policy') }}</span><span>{{ t('auth.upstream') }}</span></div>
        </div>
      </div>

      <div class="login-form-wrap">
        <div class="security-note"><LockKeyhole :size="16" /> {{ t('auth.secureSession') }}</div>
        <form class="login-form" @submit.prevent="submit">
          <div>
            <label for="username">{{ t('auth.username') }}</label>
            <a-input
              v-model="form.userName"
              size="large"
              :placeholder="t('auth.usernamePlaceholder')"
              autofocus
              :input-attrs="{ id: 'username', 'aria-label': t('auth.username'), autocomplete: 'username' }"
            />
          </div>
          <div>
            <label for="password">{{ t('auth.password') }}</label>
            <a-input-password
              v-model="form.password"
              size="large"
              :placeholder="t('auth.passwordPlaceholder')"
              :input-attrs="{ id: 'password', 'aria-label': t('auth.password'), autocomplete: 'current-password' }"
            />
          </div>
          <a-checkbox v-model="form.rememberMe">{{ t('auth.rememberMe') }}</a-checkbox>
          <a-alert v-if="error" type="error" :show-icon="false">{{ error }}</a-alert>
          <a-button type="primary" size="large" html-type="submit" long :loading="session.loading">
            {{ t('auth.signIn') }} <ArrowRight :size="17" />
          </a-button>
        </form>
        <div class="session-policy"><CheckCircle2 :size="14" /> {{ t('auth.sessionPolicy') }}</div>
      </div>
    </section>

    <footer class="mono"><span>{{ t('brand.name') }}</span><span>{{ t('auth.footerVersion') }}</span></footer>
  </main>
</template>

<style scoped>
.login-page { display: flex; min-height: 100vh; padding: 28px 42px 20px; overflow: hidden; flex-direction: column; }
.login-page > header { position: relative; z-index: 2; display: flex; align-items: center; justify-content: space-between; }
.login-workspace { display: grid; width: min(1180px, 100%); margin: auto; align-items: center; grid-template-columns: minmax(0, 1.2fr) minmax(340px, 440px); gap: clamp(60px, 10vw, 150px); }
.login-intro h1 { margin: 0; color: var(--ink); font-size: clamp(40px, 6vw, 72px); line-height: .98; letter-spacing: 0; }
.login-intro h1 span { color: var(--blue); }
.login-intro > p:not(.eyebrow) { max-width: 560px; margin: 24px 0 0; color: var(--muted); font-size: 15px; line-height: 1.7; }
.login-trace { margin-top: 58px; padding-top: 16px; border-top: 1px solid var(--line-strong); }
.trace-caption, .trace-labels { color: var(--muted); font-size: 9px; }
.trace-line { position: relative; display: flex; height: 34px; margin: 8px 0 2px; align-items: center; justify-content: space-between; }
.trace-line::before { position: absolute; right: 5px; left: 5px; height: 1px; background: var(--line-strong); content: ''; }
.trace-line i { position: relative; z-index: 1; width: 9px; height: 9px; background: var(--surface); border: 2px solid var(--ink); border-radius: 50%; }
.trace-line i:nth-child(2) { border-color: var(--blue); }
.trace-line i:nth-child(3) { border-color: var(--green); }
.trace-line span { position: absolute; z-index: 2; left: 0; width: 36px; height: 3px; background: var(--blue); animation: login-signal 3.2s ease-in-out infinite; }
.trace-labels { display: flex; justify-content: space-between; }
.login-form-wrap { padding: 34px; background: rgb(255 255 255 / 88%); border: 1px solid var(--line); border-radius: 6px; box-shadow: var(--shadow); backdrop-filter: blur(12px); }
.security-note { display: flex; align-items: center; gap: 8px; margin-bottom: 28px; color: var(--ink-soft); font: 500 11px/1 'IBM Plex Mono', monospace; }
.login-form { display: grid; gap: 18px; }
.login-form > div { display: grid; gap: 7px; }
.login-form label { color: var(--ink-soft); font-size: 12px; font-weight: 650; }
.login-form .arco-btn { height: 46px; margin-top: 4px; }
.login-form .arco-btn :deep(.arco-btn-content) { display: inline-flex; align-items: center; gap: 9px; }
.session-policy { display: flex; align-items: center; gap: 7px; margin-top: 22px; padding-top: 17px; color: var(--muted); border-top: 1px solid var(--line); font-size: 10px; }
.login-page > footer { display: flex; color: var(--muted); font-size: 9px; justify-content: space-between; }
@keyframes login-signal { 0% { transform: translateX(0); opacity: 0; } 12%, 84% { opacity: 1; } 100% { transform: translateX(min(42vw, 520px)); opacity: 0; } }

@media (max-width: 800px) {
  .login-page { padding: 22px 18px 18px; overflow: auto; }
  .login-workspace { margin: 46px auto 34px; grid-template-columns: 1fr; gap: 34px; }
  .login-intro h1 { font-size: 42px; }
  .login-intro > p:not(.eyebrow) { font-size: 13px; }
  .login-trace { margin-top: 30px; }
  .login-form-wrap { padding: 24px 20px; }
}
</style>