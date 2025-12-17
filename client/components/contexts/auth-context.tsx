"use client"

import { createContext, useContext, useEffect, useState, useCallback, type ReactNode } from "react"
import { useRouter } from "next/navigation"
import type { User, AuthResponse } from "@/lib/types"
import {
  setTokens,
  getAccessToken,
  clearTokens,
  setTokenRefreshFailedCallback,
  hasValidTokens,
  tryRefreshToken,
  setStoredUser,
  getStoredUser,
  getRefreshToken,
} from "@/lib/token-manager"
import { revokeToken } from "@/lib/api"
import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr"
import { useToast } from "@/hooks/use-toast" 

interface AuthContextType {
  user: User | null
  token: string | null
  isLoading: boolean
  isAuthenticated: boolean
  login: (authData: AuthResponse["data"]) => void
  logout: () => Promise<void>
  getValidToken: () => Promise<string | null>
  updateUser: (data: Partial<User>) => void
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [token, setToken] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const router = useRouter()
  const { toast } = useToast()
  const [hubConnection, setHubConnection] = useState<HubConnection | null>(null)

  const logout = useCallback(async () => {
    const refreshToken = getRefreshToken()
    if (refreshToken) {
      try {
        await revokeToken(refreshToken)
      } catch (error) {
        console.error("Failed to revoke token:", error)
      }
    }
    clearTokens()
    setToken(null)
    setUser(null)
    router.push("/login")
  }, [router])


  useEffect(() => {
    if (!user) return

    const connection = new HubConnectionBuilder()
      .withUrl("http://localhost:5298/hubs/user-event", {
        accessTokenFactory: () => getAccessToken() || "",
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build()

    connection.start().catch(console.error)


    connection.on("UserRoleChanged", (newRole: string) => {
      toast({ title: "Role Updated", description: `Your role is now ${newRole}. Logging out to refresh permissions.` })
      
      setTimeout(logout, 1500)
    })

    
    connection.on("AccountDeleted", () => {
      toast({ title: "Account Deleted", description: "Your account was deleted" })
      logout()
      router.push("/register")
    })


    setHubConnection(connection)

    return () => {
      connection.stop()
    }
  }, [user, logout, router, toast])

  useEffect(() => {
    setTokenRefreshFailedCallback(logout)
  }, [logout])

  useEffect(() => {
    const storedUser = getStoredUser()

    if (storedUser) {
      setUser(storedUser)
    }

    const initAuth = async () => {
      const currentToken = getAccessToken()
      if (currentToken && hasValidTokens()) {
        setToken(currentToken)
        setIsLoading(false)
        return
      }
      const refreshed = await tryRefreshToken()
      if (refreshed) setToken(getAccessToken())
      else {
      
        setUser(null)
      }
      setIsLoading(false)
    }

    void initAuth()
  }, [])

  const login = (authData: AuthResponse["data"]) => {
    const userData: User = {
      userId: authData.userId,
      userName: authData.userName,
      email: authData.email,
      role: authData.role,
      firstName: authData.firstName,
      lastName: authData.lastName,
      fullName: authData.fullName,
    }

    setTokens(authData.token, authData.refreshToken, authData.expiresAt)
    
    setStoredUser(userData) 
    
  
    

    setToken(authData.token)
    setUser(userData)

    if (authData.role === "Admin") router.push("/admin")
    else router.push("/dashboard")
  }

  const updateUser = (updated: Partial<User>) => {
    setUser((prev) => {
      if (!prev) return prev
      const newUser = { ...prev, ...updated }
      setStoredUser(newUser)
      return newUser
    })
  }

  const getValidToken = useCallback(async (): Promise<string | null> => {
    const currentToken = getAccessToken()
    if (currentToken && hasValidTokens()) return currentToken
    const refreshed = await tryRefreshToken()
    if (refreshed) {
      const newToken = getAccessToken()
      setToken(newToken)
      return newToken
    }
    return null
  }, [])

  const isAuthenticated = user !== null && token !== null

  const value: AuthContextType = { user, token, isLoading, isAuthenticated, login, logout, getValidToken, updateUser }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (context === undefined) throw new Error("useAuth must be used within an AuthProvider")
  return context
}