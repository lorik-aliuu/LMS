"use client"

import { useState } from "react"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Separator } from "@/components/ui/separator"
import { getMyInsights, getMyHabits } from "@/lib/api"
import type { LibraryInsights, UserReadingHabits } from "@/lib/types"
import { Lightbulb, TrendingUp, Loader2, BookOpen, Target, Sparkles } from "lucide-react"
import { toast } from "sonner"

export function InsightsCard() {
  const [insights, setInsights] = useState<LibraryInsights | null>(null)
  const [habits, setHabits] = useState<UserReadingHabits | null>(null)
  const [loading, setLoading] = useState(false)
  const [showInsights, setShowInsights] = useState(false)

  const handleLoadInsights = async () => {
    setLoading(true)
    try {
      const [insightsData, habitsData] = await Promise.all([getMyInsights(), getMyHabits()])
      setInsights(insightsData)
      setHabits(habitsData)
      setShowInsights(true)
      toast.success("Insights loaded successfully")
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Failed to load insights")
    } finally {
      setLoading(false)
    }
  }

  if (!showInsights) {
    return (
      <Card className="mb-6">
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Sparkles className="h-5 w-5 text-primary" />
                My Reading Insights
              </CardTitle>
              <CardDescription>Discover patterns in your reading journey</CardDescription>
            </div>
            <Button onClick={handleLoadInsights} disabled={loading}>
              {loading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Loading...
                </>
              ) : (
                <>
                  <TrendingUp className="mr-2 h-4 w-4" />
                  View Insights
                </>
              )}
            </Button>
          </div>
        </CardHeader>
      </Card>
    )
  }

  return (
    <Card className="mb-6">
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <Sparkles className="h-5 w-5 text-primary" />
              My Reading Insights
            </CardTitle>
            <CardDescription>AI-powered analysis of your reading habits</CardDescription>
          </div>
          <Button variant="outline" size="sm" onClick={handleLoadInsights} disabled={loading}>
            {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : "Refresh"}
          </Button>
        </div>
      </CardHeader>
      <CardContent className="space-y-6">
       
        {insights && (
          <div className="rounded-lg bg-primary/5 p-4">
            <p className="text-sm leading-relaxed text-foreground">{insights.summary}</p>
          </div>
        )}

       
        {habits && (
          <div className="space-y-4">
            <h4 className="flex items-center gap-2 text-sm font-semibold text-foreground">
              <Target className="h-4 w-4" />
              Reading Profile
            </h4>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="rounded-lg border border-border bg-card p-3">
                <p className="text-xs text-muted-foreground">Reading Pattern</p>
                <p className="mt-1 text-sm font-medium text-foreground">{habits.readingPattern}</p>
              </div>
              <div className="rounded-lg border border-border bg-card p-3">
                <p className="text-xs text-muted-foreground">Preferred Genres</p>
                <div className="mt-1 flex flex-wrap gap-1">
                  {habits.preferredGenres.map((genre) => (
                    <Badge key={genre} variant="secondary" className="text-xs">
                      {genre}
                    </Badge>
                  ))}
                </div>
              </div>
            </div>
            {habits.characteristics.length > 0 && (
              <div className="space-y-2">
                <p className="text-xs font-medium text-muted-foreground">Characteristics</p>
                <div className="flex flex-wrap gap-2">
                  {habits.characteristics.map((char, idx) => (
                    <Badge key={idx} variant="outline" className="text-xs">
                      {char}
                    </Badge>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}

        <Separator />

       
        {insights && insights.insights.length > 0 && (
          <div className="space-y-3">
            <h4 className="flex items-center gap-2 text-sm font-semibold text-foreground">
              <Lightbulb className="h-4 w-4" />
              Key Insights
            </h4>
            <div className="space-y-2">
              {insights.insights.map((insight, idx) => (
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

     
        {insights && (
          <div className="grid gap-3 sm:grid-cols-3">
            <div className="rounded-lg border border-border bg-card p-3 text-center">
              <p className="text-2xl font-bold text-primary">{insights.statistics.totalBooks}</p>
              <p className="text-xs text-muted-foreground">Total Books</p>
            </div>
            <div className="rounded-lg border border-border bg-card p-3 text-center">
              <p className="text-2xl font-bold text-green-600">{insights.statistics.completedBooksCount}</p>
              <p className="text-xs text-muted-foreground">Completed</p>
            </div>
            <div className="rounded-lg border border-border bg-card p-3 text-center">
              <p className="text-2xl font-bold text-blue-600">{insights.statistics.inProgressBooksCount}</p>
              <p className="text-xs text-muted-foreground">In Progress</p>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
