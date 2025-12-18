"use client"

import { useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { Search, Trash2, Star } from "lucide-react"
import type { AdminBook } from "@/lib/types"
import { ReadingStatus } from "@/lib/types"
import { deleteBookAdmin } from "@/lib/api"
import { useToast } from "@/hooks/use-toast"

interface BooksTabProps {
  books: AdminBook[]
  onRefresh: () => void
}

const statusLabels: Record<ReadingStatus, string> = {
  [ReadingStatus.NotStarted]: "Not Started",
  [ReadingStatus.Reading]: "Reading",
  [ReadingStatus.Completed]: "Completed",
}

const statusColors: Record<ReadingStatus, string> = {
  [ReadingStatus.NotStarted]: "bg-muted text-muted-foreground",
  [ReadingStatus.Reading]: "bg-blue-100 text-blue-700",
  [ReadingStatus.Completed]: "bg-green-100 text-green-700",
}

export function BooksTab({ books, onRefresh }: BooksTabProps) {
  const [search, setSearch] = useState("")
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [selectedBook, setSelectedBook] = useState<AdminBook | null>(null)
  const [loading, setLoading] = useState(false)
  const { toast } = useToast()

  const filteredBooks = books.filter(
    (book) =>
      book.title.toLowerCase().includes(search.toLowerCase()) ||
      book.author.toLowerCase().includes(search.toLowerCase()) ||
      book.genre.toLowerCase().includes(search.toLowerCase()),
  )

  const handleDeleteBook = async () => {
    if (!selectedBook) return
    setLoading(true)
    try {
      await deleteBookAdmin(selectedBook.id)
      toast({
        title: "Book deleted",
        description: `"${selectedBook.title}" has been removed.`,
      })
      onRefresh()
    } catch (error) {
      toast({
        title: "Error",
        description: error instanceof Error ? error.message : "Failed to delete book",
        variant: "destructive",
      })
    } finally {
      setLoading(false)
      setDeleteDialogOpen(false)
      setSelectedBook(null)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-4">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search books by title, author, or genre..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-10"
          />
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>All Books ({filteredBooks.length})</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Title</TableHead>
                <TableHead>Author</TableHead>
                <TableHead>Genre</TableHead>
                <TableHead>Owner</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Rating</TableHead>
                <TableHead className="w-[50px]"></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredBooks.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} className="text-center text-muted-foreground">
                    No books found.
                  </TableCell>
                </TableRow>
              ) : (
                filteredBooks.map((book) => (
                  <TableRow key={book.id}>
                    <TableCell>
                      <div className="flex items-center gap-3">
                        {book.coverImageUrl ? (
                          <img
                            src={book.coverImageUrl || "/placeholder.svg"}
                            alt={book.title}
                            className="h-10 w-8 rounded object-cover"
                          />
                        ) : (
                          <div className="flex h-10 w-8 items-center justify-center rounded bg-muted text-xs text-muted-foreground">
                            No img
                          </div>
                        )}
                        <span className="font-medium">{book.title}</span>
                      </div>
                    </TableCell>
                    <TableCell className="text-muted-foreground">{book.author}</TableCell>
                    <TableCell className="text-muted-foreground">{book.genre}</TableCell>
                    <TableCell className="text-muted-foreground">@{book.userName || "Unknown"}</TableCell>
                    <TableCell>
                      <span
                        className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${
                          statusColors[book.readingStatus]
                        }`}
                      >
                        {statusLabels[book.readingStatus]}
                      </span>
                    </TableCell>
                    <TableCell>
                      {book.rating ? (
                        <div className="flex items-center gap-1">
                          <Star className="h-3 w-3 fill-yellow-400 text-yellow-400" />
                          <span className="text-sm">{book.rating}</span>
                        </div>
                      ) : (
                        <span className="text-muted-foreground">-</span>
                      )}
                    </TableCell>
                    <TableCell>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-destructive hover:bg-destructive/10 hover:text-destructive"
                        onClick={() => {
                          setSelectedBook(book)
                          setDeleteDialogOpen(true)
                        }}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Book</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete <strong>{selectedBook?.title}</strong>? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={loading}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDeleteBook}
              disabled={loading}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {loading ? "Deleting..." : "Delete"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
