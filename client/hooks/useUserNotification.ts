"use client"

import { useEffect, useRef } from "react"
import * as signalR from "@microsoft/signalr"
import { useAuth } from "@/components/contexts/auth-context"
import { toast } from "sonner"


const LOGOUT = 5000; 

export function useUserNotification() {
  const { user, getValidToken, logout } = useAuth()
  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const isConnectingRef = useRef(false)
  const userRef = useRef(user)
  const hubUrl = process.env.NEXT_PUBLIC_SIGNALR_URL!

  useEffect(() => {
    userRef.current = user
  }, [user])

  useEffect(() => {
    const cleanup = async () => {
      if (connectionRef.current) {
        connectionRef.current.off("UserRoleChanged")
        connectionRef.current.off("AccountDeleted")
        await connectionRef.current.stop()
        connectionRef.current = null
      }
      isConnectingRef.current = false
    }

    if (!user) {
      cleanup()
      return
    }

    if (connectionRef.current || isConnectingRef.current) {
      cleanup()
    }

    const setupConnection = async () => {
      if (isConnectingRef.current) return
      isConnectingRef.current = true

      try {
        const token = await getValidToken()
        if (!token) {
          isConnectingRef.current = false
          return
        }

        const connection = new signalR.HubConnectionBuilder()
          .withUrl(hubUrl, { accessTokenFactory: async () => (await getValidToken()) ?? "" })
          .withAutomaticReconnect()
          .build()

      
        connection.on("UserRoleChanged", async (newRole: string) => {
          const currentUser = userRef.current
          if (!currentUser) return
          
          if (newRole === "Admin") {
            toast.success("Congratulations You are now an Admin Please log in again to activate your new permissions.", { duration: LOGOUT })
          } else {
            toast.info("Your role has been changed to User. Please log in again.", { duration: LOGOUT })
          }

          
          await new Promise((resolve) => setTimeout(resolve, LOGOUT))
          logout()
        })

      
        connection.on("AccountDeleted", async () => {
          toast.error("Your account has been deleted by an administrator", { duration: LOGOUT })
          await new Promise((resolve) => setTimeout(resolve, LOGOUT)) 
          logout()
        })

        

        connection.onclose(() => {
          connectionRef.current = null
          isConnectingRef.current = false
        })

        connection.onreconnected(() => {
        
        })

        await connection.start()
        connectionRef.current = connection
      } catch (error) {
        console.error("SignalR connection error:", error)
      } finally {
        isConnectingRef.current = false
      }
    }

    setupConnection()

    return () => {
      cleanup()
    }
  }, [user, getValidToken, logout, hubUrl])

  return connectionRef.current
}