"use client"

import { useEffect, useState } from "react"

import type { Book } from "@/lib/types"
import { ReadingStatus } from "@/lib/types"
import { Card, CardContent, CardFooter } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Edit, Trash2, Star } from "lucide-react"

interface BookCardProps {
  book: Book
  onEdit: (book: Book) => void
  onDelete: (book: Book) => void
}

const statusLabels: Record<ReadingStatus, string> = {
  [ReadingStatus.NotStarted]: "Not Started",
  [ReadingStatus.Reading]: "Reading",
  [ReadingStatus.Completed]: "Completed",
}

const statusColors: Record<ReadingStatus, string> = {
  [ReadingStatus.NotStarted]: "bg-muted text-muted-foreground",
  [ReadingStatus.Reading]: "bg-primary/10 text-primary",
  [ReadingStatus.Completed]: "bg-green-100 text-green-700",
}

export function BookCard({ book, onEdit, onDelete }: BookCardProps) {
  const coverImageUrl = book.coverImageUrl?.trim()
  const [coverImage, setCoverImage] = useState<string>(coverImageUrl || "/no_cover_available.png")

  useEffect(() => {
    setCoverImage(coverImageUrl || "/no_cover_available.png")
  }, [coverImageUrl])

  return (
    <Card className="group overflow-hidden transition-all hover:shadow-md text-sm">
      <div className="relative h-48 w-full overflow-hidden bg-muted">
        {coverImageUrl ? (
          <img
            src={coverImage}
            alt={book.title}
            className="h-full w-full object-cover"
            onError={() => setCoverImage("/no_cover_available.png")}
          />
        ) : (
          <img
            src={coverImage}
            alt={book.title}
            className="h-full w-full object-contain p-6"
            onError={() => setCoverImage("/no_cover_available.png")}
          />
        )}
        <Badge className={`absolute top-2 right-2 ${statusColors[book.readingStatus]}`}>
          {statusLabels[book.readingStatus]}
        </Badge>
      </div>
      <CardContent className="p-3">
        <h3 className="font-semibold text-foreground line-clamp-1">{book.title}</h3>
        <p className="text-sm text-muted-foreground line-clamp-1">{book.author}</p>
        <div className="mt-2 flex items-center justify-between">
          <Badge variant="outline" className="text-xs">
            {book.genre}
          </Badge>
          <span className="text-sm font-medium text-primary">${book.price.toFixed(2)}</span>
        </div>
        {book.rating && (
          <div className="mt-2 flex items-center gap-1">
            {Array.from({ length: 5 }).map((_, i) => (
              <Star
                key={i}
                className={`h-3 w-3 ${i < book.rating! ? "fill-yellow-400 text-yellow-400" : "text-muted"}`}
              />
            ))}
          </div>
        )}
      </CardContent>
      <CardFooter className="gap-2 border-t p-3">
        <Button variant="outline" size="sm" className="flex-1 bg-transparent" onClick={() => onEdit(book)}>
          <Edit className="mr-1 h-3 w-3" />
          Edit
        </Button>
        <Button
          variant="outline"
          size="sm"
          className="flex-1 text-destructive hover:bg-destructive hover:text-destructive-foreground bg-transparent"
          onClick={() => onDelete(book)}
        >
          <Trash2 className="mr-1 h-3 w-3" />
          Delete
        </Button>
      </CardFooter>
    </Card>
  )
}
