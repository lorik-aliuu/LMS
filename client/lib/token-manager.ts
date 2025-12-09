

import type { RefreshTokenResponse } from "./types"

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "https://localhost:7112/api"
const TOKEN_STORAGE_KEY = "authTokens"

interface TokenData {
  accessToken: string
  refreshToken: string
  expiresAt: Date
}

let tokenData: TokenData | null = null
let refreshTimeoutId: ReturnType<typeof setTimeout> | null = null
let onTokenRefreshFailed: (() => void) | null = null

function isBrowser() {
  return typeof window !== "undefined"
}

function readStoredTokens(): TokenData | null {
  if (!isBrowser()) return null
  const raw = window.localStorage.getItem(TOKEN_STORAGE_KEY)
  if (!raw) return null
  try {
    const parsed = JSON.parse(raw) as { accessToken: string; refreshToken: string; expiresAt: string }
    const expiresAt = new Date(parsed.expiresAt)
    if (Number.isNaN(expiresAt.getTime())) {
      window.localStorage.removeItem(TOKEN_STORAGE_KEY)
      return null
    }
    return {
      accessToken: parsed.accessToken,
      refreshToken: parsed.refreshToken,
      expiresAt,
    }
  } catch {
    window.localStorage.removeItem(TOKEN_STORAGE_KEY)
    return null
  }
}

function writeStoredTokens(data: TokenData) {
  if (!isBrowser()) return
  window.localStorage.setItem(
    TOKEN_STORAGE_KEY,
    JSON.stringify({
      accessToken: data.accessToken,
      refreshToken: data.refreshToken,
      expiresAt: data.expiresAt.toISOString(),
    }),
  )
}

export function setTokenRefreshFailedCallback(callback: () => void) {
  onTokenRefreshFailed = callback
}

export function setTokens(accessToken: string, refreshToken: string, expiresAt: string) {
  tokenData = {
    accessToken,
    refreshToken,
    expiresAt: new Date(expiresAt),
  }

  writeStoredTokens(tokenData)
  scheduleTokenRefresh()
}

export function getAccessToken(): string | null {
  if (!tokenData) {
    const stored = readStoredTokens()
    if (stored) {
      tokenData = stored
      scheduleTokenRefresh()
    }
  }

  return tokenData?.accessToken || null
}

export function getRefreshToken(): string | null {
  return tokenData?.refreshToken || null
}

export function clearTokens() {
  tokenData = null
  if (refreshTimeoutId) {
    clearTimeout(refreshTimeoutId)
    refreshTimeoutId = null
  }
  if (isBrowser()) {
    window.localStorage.removeItem(TOKEN_STORAGE_KEY)
  }
}

export function hasValidTokens(): boolean {
  if (!tokenData) {
    const stored = readStoredTokens()
    if (stored) {
      tokenData = stored
      scheduleTokenRefresh()
    }
  }

  if (!tokenData) return false

  const expiry = tokenData.expiresAt?.getTime()
  if (!expiry || Number.isNaN(expiry)) return false

  return new Date(expiry - 30000) > new Date()
}

function scheduleTokenRefresh() {
  if (refreshTimeoutId) {
    clearTimeout(refreshTimeoutId)
  }

  if (!tokenData) return

  
  const now = new Date()
  const expiresAt = tokenData.expiresAt
  const refreshTime = expiresAt.getTime() - now.getTime() - 60000 

  if (refreshTime <= 0) {
  
    refreshAccessToken()
    return
  }

  refreshTimeoutId = setTimeout(() => {
    refreshAccessToken()
  }, refreshTime)
}

async function refreshAccessToken(): Promise<boolean> {
  if (!tokenData) return false

  try {
    const response = await fetch(`${API_BASE_URL}/Auth/refresh`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        accessToken: tokenData.accessToken,
        refreshToken: tokenData.refreshToken,
      }),
    })

    const data: RefreshTokenResponse = await response.json()

    if (data.success && data.data) {
      setTokens(data.data.token, data.data.refreshToken, data.data.expiresAt)
      return true
    } else {
      clearTokens()
      onTokenRefreshFailed?.()
      return false
    }
  } catch (error) {
    console.error("Token refresh failed:", error)
    clearTokens()
    onTokenRefreshFailed?.()
    return false
  }
}


export async function tryRefreshToken(): Promise<boolean> {
  if (!tokenData) {
    const stored = readStoredTokens()
    if (stored) {
      tokenData = stored
      scheduleTokenRefresh()
    }
  }

  return refreshAccessToken()
}
