<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { JudgeResult } from '../api'
import { verdictKey } from '../api'

const props = defineProps<{ result: JudgeResult | null; total: number }>()
const emit = defineEmits<{ (e: 'close'): void }>()
const { t } = useI18n()

type Phase = 'waiting' | 'counting' | 'failed' | 'accepted' | 'compileError'
const phase = ref<Phase>('waiting')
const shown = ref(0) // cases revealed so far
const failedAt = ref<number | null>(null) // index of the first failing case
const canvas = ref<HTMLCanvasElement | null>(null)

const STEP_MS = 90
let timer: number | null = null
let raf: number | null = null

const total = computed(() => props.result?.results.length || props.total)
const percent = computed(() => (total.value === 0 ? 0 : (shown.value / total.value) * 100))
const title = computed(() => {
  switch (phase.value) {
    case 'waiting': return t('progress.compiling')
    case 'counting': return t('progress.running')
    case 'accepted': return t('progress.accepted')
    case 'compileError': return t(verdictKey('CompileError'))
    case 'failed': return props.result ? t(verdictKey(props.result.verdict)) : ''
  }
})

function start(result: JudgeResult) {
  stop()
  shown.value = 0
  failedAt.value = null
  if (result.verdict === 'CompileError' || result.verdict === 'JudgeError' || result.results.length === 0) {
    phase.value = 'compileError'
    return
  }
  phase.value = 'counting'
  let i = 0
  timer = window.setInterval(() => {
    const r = result.results[i]
    if (!r) {
      stop()
      phase.value = 'accepted'
      void launchConfetti()
      return
    }
    if (r.passed) {
      shown.value = i + 1
      i++
      return
    }
    failedAt.value = i
    shown.value = i + 1
    stop()
    phase.value = 'failed'
  }, STEP_MS)
}

function stop() {
  if (timer !== null) window.clearInterval(timer)
  timer = null
}

function dotState(i: number): 'pending' | 'passed' | 'failed' {
  if (failedAt.value === i) return 'failed'
  return i < shown.value ? 'passed' : 'pending'
}

function close() {
  emit('close')
}

function onKey(e: KeyboardEvent) {
  if (e.key === 'Escape') close()
}

/** Lightweight confetti: a few hundred coloured particles falling from the top, no dependencies. */
async function launchConfetti() {
  const el = canvas.value
  if (!el) return
  const ctx = el.getContext('2d')
  if (!ctx) return
  const dpr = window.devicePixelRatio || 1
  const w = (el.width = el.clientWidth * dpr)
  const h = (el.height = el.clientHeight * dpr)
  const colors = ['#3ddc84', '#7c9cff', '#ffc857', '#ff6b6b', '#c084fc', '#ffffff']
  const particles = Array.from({ length: 220 }, () => ({
    x: Math.random() * w,
    y: -Math.random() * h * 0.3,
    vx: (Math.random() - 0.5) * 3 * dpr,
    vy: (2 + Math.random() * 4) * dpr,
    size: (4 + Math.random() * 6) * dpr,
    rot: Math.random() * Math.PI,
    vr: (Math.random() - 0.5) * 0.3,
    color: colors[Math.floor(Math.random() * colors.length)]!,
  }))
  const startAt = performance.now()
  const frame = (now: number) => {
    const elapsed = now - startAt
    ctx.clearRect(0, 0, w, h)
    for (const p of particles) {
      p.x += p.vx
      p.y += p.vy
      p.vy += 0.03 * dpr
      p.rot += p.vr
      ctx.save()
      ctx.translate(p.x, p.y)
      ctx.rotate(p.rot)
      ctx.fillStyle = p.color
      ctx.globalAlpha = elapsed > 2600 ? Math.max(0, 1 - (elapsed - 2600) / 600) : 1
      ctx.fillRect(-p.size / 2, -p.size / 4, p.size, p.size / 2)
      ctx.restore()
    }
    if (elapsed < 3200) raf = requestAnimationFrame(frame)
    else ctx.clearRect(0, 0, w, h)
  }
  raf = requestAnimationFrame(frame)
}

watch(
  () => props.result,
  (r) => {
    if (r) start(r)
  },
  { immediate: true },
)

onMounted(() => window.addEventListener('keydown', onKey))
onBeforeUnmount(() => {
  stop()
  if (raf !== null) cancelAnimationFrame(raf)
  window.removeEventListener('keydown', onKey)
})
</script>

<template>
  <Teleport to="body">
    <div class="backdrop" @click.self="close">
      <canvas ref="canvas" class="confetti" aria-hidden="true"></canvas>
      <div class="card" :class="phase" role="dialog" aria-modal="true" :aria-label="title">
        <div class="icon" aria-hidden="true">
          <span v-if="phase === 'waiting'" class="spinner"></span>
          <span v-else-if="phase === 'accepted'" class="check">✓</span>
          <span v-else-if="phase === 'failed' || phase === 'compileError'" class="cross">✕</span>
          <span v-else class="spinner"></span>
        </div>

        <h2 class="title">{{ title }}</h2>

        <template v-if="phase !== 'compileError'">
          <div class="count" aria-live="polite">{{ shown }}/{{ total }}</div>
          <div class="track" :class="{ indeterminate: phase === 'waiting' }">
            <div class="fill" :style="{ width: percent + '%' }"></div>
          </div>
          <div class="dots">
            <span v-for="i in total" :key="i" class="dot" :class="dotState(i - 1)"></span>
          </div>
        </template>

        <p class="message">
          <template v-if="phase === 'accepted'">{{ t('progress.allPassed', { total }) }}</template>
          <template v-else-if="phase === 'failed' && failedAt !== null">{{ t('progress.failedAt', { n: failedAt + 1, total }) }}</template>
          <template v-else-if="phase === 'compileError' && result">{{ result.message }}</template>
          <template v-else-if="phase === 'waiting'">{{ t('progress.waitingHint') }}</template>
          <template v-else>&nbsp;</template>
        </p>

        <button v-if="phase !== 'waiting' && phase !== 'counting'" class="primary" @click="close">
          {{ phase === 'accepted' ? t('progress.close') : t('progress.details') }}
        </button>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.backdrop {
  position: fixed; inset: 0; z-index: 50;
  display: grid; place-items: center;
  background: rgba(8, 10, 14, 0.72);
  backdrop-filter: blur(3px);
  animation: fade-in 0.15s ease-out;
}
.confetti { position: absolute; inset: 0; width: 100%; height: 100%; pointer-events: none; }
.card {
  position: relative;
  width: min(520px, calc(100vw - 32px));
  padding: 1.75rem 1.5rem 1.5rem;
  background: var(--panel); border: 1px solid var(--border); border-radius: 14px;
  text-align: center;
  display: flex; flex-direction: column; align-items: center; gap: 0.6rem;
  animation: pop-in 0.2s ease-out;
}
.card.failed, .card.compileError { animation: pop-in 0.2s ease-out, shake 0.45s ease-in-out 0.05s; border-color: rgba(255, 107, 107, 0.5); }
.card.accepted { border-color: rgba(61, 220, 132, 0.6); box-shadow: 0 0 40px rgba(61, 220, 132, 0.18); }
.icon { height: 56px; display: grid; place-items: center; }
.spinner {
  width: 36px; height: 36px; border-radius: 50%;
  border: 3px solid var(--border); border-top-color: var(--accent);
  animation: spin 0.8s linear infinite;
}
.check, .cross {
  display: inline-grid; place-items: center; width: 56px; height: 56px; border-radius: 50%;
  font-size: 1.8rem; font-weight: 800; color: #fff;
}
.check { background: var(--green); animation: pop-check 0.45s cubic-bezier(0.2, 1.4, 0.4, 1); }
.cross { background: var(--red); }
.title { margin: 0; font-size: 1.3rem; }
.accepted .title { color: var(--green); }
.failed .title, .compileError .title { color: var(--red); }
.count { font-family: var(--mono); font-size: 2rem; font-weight: 700; letter-spacing: 0.02em; }
.track { width: 100%; height: 12px; border-radius: 999px; background: var(--panel-2); border: 1px solid var(--border); overflow: hidden; }
.fill { height: 100%; background: var(--green); border-radius: 999px; transition: width 0.08s linear, background 0.2s; }
.failed .fill { background: var(--red); }
.track.indeterminate .fill { width: 35% !important; animation: slide 1.1s ease-in-out infinite; background: var(--accent); }
.dots { display: flex; flex-wrap: wrap; justify-content: center; gap: 5px; max-width: 100%; }
.dot { width: 12px; height: 12px; border-radius: 3px; background: var(--panel-2); border: 1px solid var(--border); transition: background 0.15s, transform 0.15s; }
.dot.passed { background: var(--green); border-color: var(--green); }
.dot.failed { background: var(--red); border-color: var(--red); transform: scale(1.35); box-shadow: 0 0 10px rgba(255, 107, 107, 0.8); }
.message { margin: 0.2rem 0 0; color: var(--muted); min-height: 1.4em; }
.card button { margin-top: 0.5rem; }

@keyframes fade-in { from { opacity: 0; } to { opacity: 1; } }
@keyframes pop-in { from { transform: scale(0.92); opacity: 0; } to { transform: scale(1); opacity: 1; } }
@keyframes pop-check { from { transform: scale(0); } to { transform: scale(1); } }
@keyframes spin { to { transform: rotate(360deg); } }
@keyframes slide { 0% { margin-left: -35%; } 100% { margin-left: 100%; } }
@keyframes shake {
  0%, 100% { transform: translateX(0); }
  20% { transform: translateX(-8px); }
  40% { transform: translateX(8px); }
  60% { transform: translateX(-5px); }
  80% { transform: translateX(5px); }
}
</style>
