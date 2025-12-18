"use client"


import { useState, useEffect } from "react"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Separator } from "@/components/ui/separator"
import { Input } from "@/components/ui/input"
import { getLibraryInsights, getUserHabits, getAllUsers } from "@/lib/api"
import type { LibraryInsights, UserReadingHabits, AdminUser } from "@/lib/types"
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell } from "recharts"
import { Lightbulb, TrendingUp, Loader2, BookOpen, Users, Search } from "lucide-react"
import { toast } from "sonner"

const COLORS = ["#0088FE", "#00C49F", "#FFBB28", "#FF8042", "#8884d8", "#82ca9d"]

export function InsightsTab() {
  const [libraryInsights, setLibraryInsights] = useState<LibraryInsights | null>(null)
  const [selectedUser, setSelectedUser] = useState<string | null>(null)
  const [userHabits, setUserHabits] = useState<UserReadingHabits | null>(null)
  const [users, setUsers] = useState<AdminUser[]>([])
  const [searchQuery, setSearchQuery] = useState("")
  const [loading, setLoading] = useState(false)
  const [loadingHabits, setLoadingHabits] = useState(false)

  useEffect(() => {
    const fetchUsers = async () => {
      try {
        const usersData = await getAllUsers()
        setUsers(usersData)
      } catch (error) {
        console.error("Failed to fetch users:", error)
      }
    }
    fetchUsers()
  }, [])

  const handleLoadLibraryInsights = async () => {
    setLoading(true)
    try {
      const data = await getLibraryInsights()
      setLibraryInsights(data)
      toast.success("Library insights loaded")
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Failed to load insights")
    } finally {
      setLoading(false)
    }
  }

  const handleLoadUserHabits = async (userId: string, userName: string) => {
    setLoadingHabits(true)
    setSelectedUser(userId)
    try {
      const data = await getUserHabits(userId)
      setUserHabits(data)
      toast.success(`Loaded habits for ${userName}`)
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Failed to load user habits")
      setSelectedUser(null)
    } finally {
      setLoadingHabits(false)
    }
  }

  const filteredUsers = users.filter(
    (user) =>
      user.role !== "Admin" &&
      (user.userName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      user.fullName.toLowerCase().includes(searchQuery.toLowerCase())),
  )

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold tracking-tight text-foreground">Library Insights & Analytics</h2>
        <p className="text-muted-foreground">AI-powered insights across your entire library</p>
      </div>

      
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <TrendingUp className="h-5 w-5 text-primary" />
                Library Overview
              </CardTitle>
              <CardDescription>Comprehensive insights across all users and books</CardDescription>
            </div>
            <Button onClick={handleLoadLibraryInsights} disabled={loading}>
              {loading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Loading...
                </>
              ) : (
                "Load Insights"
              )}
            </Button>
          </div>
        </CardHeader>
        {libraryInsights && (
          <CardContent className="space-y-6">
           
            <div className="rounded-lg bg-primary/5 p-4">
              <p className="text-sm leading-relaxed text-foreground">{libraryInsights.summary}</p>
            </div>

          
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <div className="rounded-lg border border-border bg-card p-4 text-center">
                <p className="text-3xl font-bold text-primary">{libraryInsights.statistics.totalBooks}</p>
                <p className="text-sm text-muted-foreground">Total Books</p>
              </div>
              <div className="rounded-lg border border-border bg-card p-4 text-center">
                <p className="text-3xl font-bold text-blue-600">{libraryInsights.statistics.totalUsers || 0}</p>
                <p className="text-sm text-muted-foreground">Total Users</p>
              </div>
              <div className="rounded-lg border border-border bg-card p-4 text-center">
                <p className="text-3xl font-bold text-green-600">{libraryInsights.statistics.completedBooksCount}</p>
                <p className="text-sm text-muted-foreground">Completed</p>
              </div>
              <div className="rounded-lg border border-border bg-card p-4 text-center">
                <p className="text-3xl font-bold text-orange-600">{libraryInsights.statistics.inProgressBooksCount}</p>
                <p className="text-sm text-muted-foreground">In Progress</p>
              </div>
            </div>

           
            <div className="grid gap-6 lg:grid-cols-2">
             
              <div className="rounded-lg border border-border bg-card p-4">
                <h4 className="mb-4 text-sm font-semibold text-foreground">Genre Distribution</h4>
                <ResponsiveContainer width="100%" height={250}>
                  <PieChart>
                    <Pie
                      data={Object.entries(libraryInsights.statistics.genreDistribution).map(([name, value]) => ({
                        name,
                        value,
                      }))}
                      cx="50%"
                      cy="50%"
                      labelLine={false}
                      label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
                      outerRadius={80}
                      fill="#8884d8"
                      dataKey="value"
                    >
                      {Object.keys(libraryInsights.statistics.genreDistribution).map((_, index) => (
                        <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                </ResponsiveContainer>
              </div>

            
              <div className="rounded-lg border border-border bg-card p-4">
                <h4 className="mb-4 text-sm font-semibold text-foreground">Reading Status</h4>
                <ResponsiveContainer width="100%" height={250}>
                  <BarChart
                    data={Object.entries(libraryInsights.statistics.statusDistribution).map(([name, value]) => ({
                      name,
                      value,
                    }))}
                  >
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="name" />
                    <YAxis />
                    <Tooltip />
                    <Bar dataKey="value" fill="hsl(var(--primary))" />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            </div>

            <Separator />

          
            {libraryInsights.insights.length > 0 && (
              <div className="space-y-3">
                <h4 className="flex items-center gap-2 text-sm font-semibold text-foreground">
                  <Lightbulb className="h-4 w-4" />
                  Key Insights
                </h4>
                <div className="grid gap-3 sm:grid-cols-2">
                  {libraryInsights.insights.map((insight, idx) => (
                    <div key={idx} className="rounded-lg border border-border bg-card p-3">
                      <div className="flex items-start gap-3">
                        <BookOpen className="mt-0.5 h-4 w-4 shrink-0 text-primary" />
                        <div>
                          <p className="text-sm font-medium text-foreground">{insight.title}</p>
                          <p className="mt-1 text-xs text-muted-foreground">{insight.description}</p>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </CardContent>
        )}
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="h-5 w-5 text-primary" />
            User Reading Habits
          </CardTitle>
          <CardDescription>View detailed reading patterns for individual users</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
        
          <div className="relative">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder="Search users..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-9"
            />
          </div>

         
          <div className="max-h-60 space-y-2 overflow-y-auto">
            {filteredUsers.map((user) => (
              <div
                key={user.userId}
                className={`flex cursor-pointer items-center justify-between rounded-lg border border-border p-3 transition-colors hover:bg-accent ${
                  selectedUser === user.userId ? "bg-accent" : "bg-card"
                }`}
                onClick={() => handleLoadUserHabits(user.userId, user.userName)}
              >
                <div>
                  <p className="text-sm font-medium text-foreground">{user.fullName}</p>
                  <p className="text-xs text-muted-foreground">@{user.userName}</p>
                </div>
                <Badge variant="secondary">{user.totalBooks} books</Badge>
              </div>
            ))}
          </div>

          
          {loadingHabits && (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-6 w-6 animate-spin text-primary" />
            </div>
          )}

          {userHabits && !loadingHabits && (
            <div className="space-y-4 rounded-lg border border-border bg-card p-4">
              <div>
                <h4 className="text-lg font-semibold text-foreground">{userHabits.userName}'s Reading Profile</h4>
                <p className="mt-2 text-sm leading-relaxed text-muted-foreground">{userHabits.summary}</p>
              </div>

              <div className="grid gap-3 sm:grid-cols-3">
                <div className="rounded-lg border border-border bg-background p-3 text-center">
                  <p className="text-2xl font-bold text-primary">{userHabits.totalBooks}</p>
                  <p className="text-xs text-muted-foreground">Total Books</p>
                </div>
                <div className="rounded-lg border border-border bg-background p-3 text-center">
                  <p className="text-2xl font-bold text-green-600">{userHabits.completedBooks}</p>
                  <p className="text-xs text-muted-foreground">Completed</p>
                </div>
                <div className="rounded-lg border border-border bg-background p-3 text-center">
                  <p className="text-2xl font-bold text-blue-600">{userHabits.booksInProgress}</p>
                  <p className="text-xs text-muted-foreground">In Progress</p>
                </div>
              </div>

              {userHabits.preferredGenres.length > 0 && (
                <div>
                  <p className="mb-2 text-xs font-medium text-muted-foreground">Preferred Genres</p>
                  <div className="flex flex-wrap gap-2">
                    {userHabits.preferredGenres.map((genre) => (
                      <Badge key={genre} variant="secondary">
                        {genre}
                      </Badge>
                    ))}
                  </div>
                </div>
              )}

              {userHabits.characteristics.length > 0 && (
                <div>
                  <p className="mb-2 text-xs font-medium text-muted-foreground">Reading Characteristics</p>
                  <div className="flex flex-wrap gap-2">
                    {userHabits.characteristics.map((char, idx) => (
                      <Badge key={idx} variant="outline">
                        {char}
                      </Badge>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
