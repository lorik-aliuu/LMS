"use client"

import type React from "react"

import { useState, useEffect } from "react"
import type { Book, CreateBookDTO, UpdateBookDTO } from "@/lib/types"
import { ReadingStatus } from "@/lib/types"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Loader2 } from "lucide-react"
import { useToast } from "@/hooks/use-toast"

interface BookFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  book?: Book | null
  onSubmit: (data: CreateBookDTO | UpdateBookDTO) => Promise<void>
}

type FormState = Omit<CreateBookDTO, "price"> & { price: number | "" }

export function BookFormDialog({ open, onOpenChange, book, onSubmit }: BookFormDialogProps) {
  const [isLoading, setIsLoading] = useState(false)
  const [formData, setFormData] = useState<FormState>({
    title: "",
    author: "",
    genre: "",
    price: "",
    publicationYear: new Date().getFullYear(),
    readingStatus: ReadingStatus.NotStarted,
    rating: undefined,
    coverImageUrl: "",
  })
  const { toast } = useToast()

  useEffect(() => {
    if (book) {
      setFormData({
        title: book.title,
        author: book.author,
        genre: book.genre,
        price: book.price ?? "",
        publicationYear: book.publicationYear ?? new Date().getFullYear(),
        readingStatus: book.readingStatus,
        rating: book.rating && book.rating >= 1 ? book.rating : undefined,
        coverImageUrl: book.coverImageUrl || "",
      })
    } else {
      setFormData({
        title: "",
        author: "",
        genre: "",
        price: "",
        publicationYear: new Date().getFullYear(),
        readingStatus: ReadingStatus.NotStarted,
        rating: undefined,
        coverImageUrl: "",
      })
    }
  }, [book, open])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsLoading(true)
    try {
      const payload: CreateBookDTO | UpdateBookDTO = {
        ...formData,
        price: formData.price === "" ? 0 : formData.price,
        rating: formData.rating && formData.rating >= 1 ? formData.rating : undefined,
      }
      await onSubmit(payload)
      onOpenChange(false)
    } catch (error) {
      const message =
        error instanceof Error
          ? error.message
          : typeof error === "string"
            ? error
            : "Unable to save the book."

      toast({
        title: "Save failed",
        description: message,
        variant: "destructive",
      })
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>{book ? "Edit Book" : "Add New Book"}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="title">Title</Label>
              <Input
                id="title"
                value={formData.title}
                onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="author">Author</Label>
              <Input
                id="author"
                value={formData.author}
                onChange={(e) => setFormData({ ...formData, author: e.target.value })}
                required
              />
            </div>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="genre">Genre</Label>
              <Input
                id="genre"
                value={formData.genre}
                onChange={(e) => setFormData({ ...formData, genre: e.target.value })}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="price">Price</Label>
              <Input
                id="price"
                type="number"
                step="0.01"
                min="0"
                value={formData.price === "" ? "" : formData.price}
                onChange={(e) =>
                  setFormData({
                    ...formData,
                    price: e.target.value === "" ? "" : Number.parseFloat(e.target.value) || 0,
                  })
                }
                required
              />
            </div>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="publicationYear">Publication Year</Label>
              <Input
                id="publicationYear"
                type="number"
                min="0"
                value={Number.isFinite(formData.publicationYear) ? formData.publicationYear : ""}
                onChange={(e) =>
                  setFormData({
                    ...formData,
                    publicationYear:
                      e.target.value === ""
                        ? new Date().getFullYear()
                        : Number.parseInt(e.target.value) || new Date().getFullYear(),
                  })
                }
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="rating">Rating (1-5)</Label>
              <Select
                value={formData.rating ? formData.rating.toString() : "0"}
                onValueChange={(value) =>
                  setFormData({
                    ...formData,
                    rating: value === "0" ? undefined : Number.parseInt(value),
                  })
                }
              >
                <SelectTrigger>
                  <SelectValue placeholder="No rating" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="0">No rating</SelectItem>
                  <SelectItem value="1">1 Star</SelectItem>
                  <SelectItem value="2">2 Stars</SelectItem>
                  <SelectItem value="3">3 Stars</SelectItem>
                  <SelectItem value="4">4 Stars</SelectItem>
                  <SelectItem value="5">5 Stars</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="readingStatus">Reading Status</Label>
              <Select
                value={formData.readingStatus.toString()}
                onValueChange={(value) =>
                  setFormData({ ...formData, readingStatus: Number.parseInt(value) as ReadingStatus })
                }
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="0">Not Started</SelectItem>
                  <SelectItem value="1">Reading</SelectItem>
                  <SelectItem value="2">Completed</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="coverImageUrl">Cover Image URL (optional)</Label>
              <Input
                id="coverImageUrl"
                type="url"
                value={formData.coverImageUrl}
                onChange={(e) => setFormData({ ...formData, coverImageUrl: e.target.value })}
                placeholder=""
              />
            </div>
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isLoading}>
              {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {book ? "Save Changes" : "Add Book"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
