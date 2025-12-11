"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "@/components/contexts/auth-context"
import { AdminHeader } from "@/components/admin/admin-header"
import { AdminSidebar } from "@/components/admin/admin-sidebar"
import { AdminMobileNav } from "@/components/admin/admin-mobile-nav"
import { OverviewTab } from "@/components/admin/overview-tab"
import { UsersTab } from "@/components/admin/users-tab"
import { BooksTab } from "@/components/admin/books-tab"
import { getAllUsers, getAllBooksAdmin, getTotalBooksCount } from "@/lib/api"
import { ChatAssistant } from "@/components/chat/chat-assistant"

import type { AdminUser, AdminBook } from "@/lib/types"
import { Loader2 } from "lucide-react"

type Tab = "overview" | "users" | "books"

export default function AdminDashboard() {
  const { user, isAuthenticated, isLoading: authLoading, logout } = useAuth() 
  const router = useRouter()
  const [activeTab, setActiveTab] = useState<Tab>("overview")
  const [users, setUsers] = useState<AdminUser[]>([])
  const [books, setBooks] = useState<AdminBook[]>([])
  const [totalBooksCount, setTotalBooksCount] = useState(0)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push("/login")
      return
    }
    
    if (!authLoading && user?.role !== "Admin") {
      
      router.push("/dashboard")
      return
    }
    
    

  }, [isAuthenticated, authLoading, user, router]) 

  const fetchData = async () => {
    setLoading(true)
    try {
      const [usersData, booksData, countData] = await Promise.all([
        getAllUsers(),
        getAllBooksAdmin(),
        getTotalBooksCount(),
      ])
      setUsers(usersData)
      setBooks(booksData)
      setTotalBooksCount(countData)
    } catch (error) {
      console.error("Failed to fetch  data:", error)
      
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    if (isAuthenticated && user?.role === "Admin") {
      fetchData()
    }
  }, [isAuthenticated, user])

  if (authLoading || !isAuthenticated || user?.role !== "Admin") {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    )
  }

  return (
    <div className="flex min-h-screen flex-col bg-background">
      <AdminHeader />
      <AdminMobileNav activeTab={activeTab} onTabChange={setActiveTab} />
      <div className="flex flex-1">
        <AdminSidebar activeTab={activeTab} onTabChange={setActiveTab} />
        <main className="flex-1 p-6">
          {loading ? (
            <div className="flex h-64 items-center justify-center">
              <Loader2 className="h-8 w-8 animate-spin text-primary" />
            </div>
          ) : (
            <>
              {activeTab === "overview" && (
                <OverviewTab users={users} books={books} totalBooksCount={totalBooksCount} />
              )}
              {activeTab === "users" && <UsersTab users={users} onRefresh={fetchData} currentUserId={user.userId} />}
              {activeTab === "books" && <BooksTab books={books} onRefresh={fetchData} />}
            </>
          )}
        </main>
      </div>
          <ChatAssistant />
    </div>
  )
}