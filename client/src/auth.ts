import { computed, ref } from 'vue'
import { api, ApiError, type User } from './api'

const TOKEN_KEY = 'sharpcode:session'

/** Minimal typing for Google Identity Services (loaded from accounts.google.com on demand). */
interface GoogleCredentialResponse {
  credential: string
}
interface GoogleAccountsId {
  initialize(config: { client_id: string; callback: (r: GoogleCredentialResponse) => void; ux_mode?: 'popup' | 'redirect' }): void
  renderButton(
    parent: HTMLElement,
    options: { theme?: 'outline' | 'filled_blue' | 'filled_black'; size?: 'large' | 'medium' | 'small'; text?: string; width?: number; locale?: string; shape?: string },
  ): void
  prompt(): void
  disableAutoSelect(): void
}
declare global {
  interface Window {
    google?: { accounts: { id: GoogleAccountsId } }
  }
}

function readToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_KEY)
  } catch {
    return null
  }
}
function writeToken(token: string | null) {
  try {
    if (token) localStorage.setItem(TOKEN_KEY, token)
    else localStorage.removeItem(TOKEN_KEY)
  } catch {
    /* storage unavailable */
  }
}

/** Bearer token for the API. Null when signed out. */
export const token = ref<string | null>(readToken())
export const user = ref<User | null>(null)
export const isSignedIn = computed(() => user.value !== null)
/** True until the stored session has been checked against the server (so guards do not redirect too early). */
export const sessionReady = ref(false)
/** Null until `/api/config` answers; empty string when the server has no Google client configured. */
export const googleClientId = ref<string | null>(null)

function setSession(newToken: string | null, newUser: User | null) {
  token.value = newToken
  user.value = newUser
  writeToken(newToken)
}

export function signOut() {
  setSession(null, null)
  window.google?.accounts.id.disableAutoSelect()
}

/** Called by the API layer when the server rejects the bearer token (expired or revoked). */
export function onUnauthorized() {
  if (token.value) setSession(null, null)
}

/** Validates the stored token on startup and loads the sign-in configuration. Never throws. */
export async function bootstrapSession() {
  try {
    const cfg = await api.getConfig()
    googleClientId.value = cfg.googleClientId ?? ''
  } catch {
    googleClientId.value = ''
  }
  if (token.value) {
    try {
      const s = await api.me()
      setSession(s.token ?? token.value, s.user)
    } catch (e) {
      if (e instanceof ApiError && e.status === 401) setSession(null, null)
      // Other failures (offline, server down): keep the token and retry on the next load.
    }
  }
  sessionReady.value = true
}

let gisLoading: Promise<GoogleAccountsId> | null = null

/** Loads the Google Identity Services script once and returns its `google.accounts.id` API. */
export function loadGoogle(): Promise<GoogleAccountsId> {
  if (window.google?.accounts?.id) return Promise.resolve(window.google.accounts.id)
  gisLoading ??= new Promise((resolve, reject) => {
    const script = document.createElement('script')
    script.src = 'https://accounts.google.com/gsi/client'
    script.async = true
    script.defer = true
    script.onload = () => {
      const g = window.google?.accounts?.id
      if (g) resolve(g)
      else reject(new Error('Google Identity Services did not load.'))
    }
    script.onerror = () => {
      gisLoading = null
      reject(new Error('Could not load Google sign-in.'))
    }
    document.head.appendChild(script)
  })
  return gisLoading
}

/**
 * Renders the "Sign in with Google" button into `el`. When the user completes the Google popup, the ID token is
 * exchanged for an app session. `onDone` reports success or the error message.
 */
export async function renderGoogleButton(el: HTMLElement, locale: string, onDone: (error: string | null) => void) {
  const clientId = googleClientId.value
  if (!clientId) throw new Error('Google sign-in is not configured.')
  const g = await loadGoogle()
  g.initialize({
    client_id: clientId,
    callback: async (r) => {
      try {
        const s = await api.signInWithGoogle(r.credential)
        setSession(s.token ?? null, s.user)
        onDone(null)
      } catch (e) {
        onDone((e as Error).message)
      }
    },
  })
  el.innerHTML = ''
  g.renderButton(el, { theme: 'filled_blue', size: 'large', text: 'signin_with', shape: 'pill', locale, width: 280 })
}
