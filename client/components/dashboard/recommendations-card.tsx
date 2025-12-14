"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Sparkles, Loader2, RefreshCw, Plus, X, ChevronDown, ChevronUp } from "lucide-react"
import { getRecommendations, saveRecommendedBook, dismissRecommendedBook } from "@/lib/api"
import type { BookRecommendation } from "@/lib/types"
import { useToast } from "@/hooks/use-toast"

interface RecommendationsCardProps {
  onBookAdded?: () => void
}

export function RecommendationsCard({ onBookAdded }: RecommendationsCardProps) {
  const [recommendations, setRecommendations] = useState<BookRecommendation[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string>("")
  const [hasLoaded, setHasLoaded] = useState(false)
  const [savingIndex, setSavingIndex] = useState<number | null>(null)
  const [dismissingIndex, setDismissingIndex] = useState<number | null>(null)
  const [isCollapsed, setIsCollapsed] = useState(false)
  const { toast } = useToast()

  const fetchRecommendations = async () => {
    setIsLoading(true)
    setError(null)
    try {
      const response = await getRecommendations(3)
      setRecommendations(response.recommendations)
      setMessage(response.message)
      setHasLoaded(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load recommendations")
    } finally {
      setIsLoading(false)
    }
  }

  const handleSave = async (rec: BookRecommendation, index: number) => {
    setSavingIndex(index)
    try {
      await saveRecommendedBook({
        title: rec.title,
        author: rec.author,
        genre: rec.genre,
        price: rec.estimatedPrice,
      })
      toast({
        title: "Book added to library",
        description: `${rec.title} has been added to your collection`,
      })
      
      setRecommendations((prev) => prev.filter((_, i) => i !== index))
      
      onBookAdded?.()
    } catch (err) {
      toast({
        title: "Failed to save book",
        description: err instanceof Error ? err.message : "An error occurred",
        variant: "destructive",
      })
    } finally {
      setSavingIndex(null)
    }
  }

  const handleDismiss = async (rec: BookRecommendation, index: number) => {
    setDismissingIndex(index)
    try {
      await dismissRecommendedBook({
        title: rec.title,
        author: rec.author,
      })
      toast({
        title: "Recommendation dismissed",
        description: "We'll show you different recommendations next time",
      })
     
      setRecommendations((prev) => prev.filter((_, i) => i !== index))
    } catch (err) {
      toast({
        title: "Failed to dismiss",
        description: err instanceof Error ? err.message : "An error occurred",
        variant: "destructive",
      })
    } finally {
      setDismissingIndex(null)
    }
  }

  return (
    <Card className="mb-8">
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Sparkles className="h-5 w-5 text-primary" />
            <CardTitle>Recommended for You</CardTitle>
          </div>
          <div className="flex items-center gap-1">
            {hasLoaded && (
              <Button variant="ghost" size="sm" onClick={fetchRecommendations} disabled={isLoading}>
                <RefreshCw className={`h-4 w-4 ${isLoading ? "animate-spin" : ""}`} />
              </Button>
            )}
            {hasLoaded && (
              <Button variant="ghost" size="sm" onClick={() => setIsCollapsed(!isCollapsed)}>
                {isCollapsed ? <ChevronDown className="h-4 w-4" /> : <ChevronUp className="h-4 w-4" />}
              </Button>
            )}
          </div>
        </div>
        <CardDescription>
          {message || "Get AI-powered book recommendations based on your reading history"}
        </CardDescription>
      </CardHeader>
      {!isCollapsed && (
        <CardContent>
          {error && <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">{error}</div>}

          {!hasLoaded && !isLoading ? (
            <div className="flex items-center justify-center py-8">
              <Button onClick={fetchRecommendations} size="lg">
                <Sparkles className="mr-2 h-5 w-5" />
                Get Recommendations
              </Button>
            </div>
          ) : isLoading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-6 w-6 animate-spin text-primary" />
            </div>
          ) : (
            <div className="space-y-3">
              {recommendations.map((rec, index) => (
                <div
                  key={index}
                  className="rounded-lg border border-border bg-card p-4 transition-colors hover:bg-accent"
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex-1">
                      <h4 className="font-semibold text-foreground">{rec.title}</h4>
                      <p className="text-sm text-muted-foreground">by {rec.author}</p>
                      <div className="mt-2 flex items-center gap-3 text-xs text-muted-foreground">
                        <span className="rounded-full bg-primary/10 px-2 py-1 text-primary">{rec.genre}</span>
                        <span className="font-medium">${rec.estimatedPrice.toFixed(2)}</span>
                      </div>
                      <p className="mt-2 text-sm text-muted-foreground">{rec.reason}</p>
                    </div>
                    <div className="flex flex-col gap-2">
                      <Button
                        size="sm"
                        onClick={() => handleSave(rec, index)}
                        disabled={savingIndex === index || dismissingIndex === index}
                      >
                        {savingIndex === index ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <>
                            <Plus className="mr-1 h-4 w-4" />
                            Add
                          </>
                        )}
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => handleDismiss(rec, index)}
                        disabled={savingIndex === index || dismissingIndex === index}
                      >
                        {dismissingIndex === index ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <>
                            <X className="mr-1 h-4 w-4" />
                            Dismiss
                          </>
                        )}
                      </Button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      )}
    </Card>
  )
}
