"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "@/components/contexts/auth-context"
import { DashboardHeader } from "@/components/dashboard/dashboard-header"
import { BookCard } from "@/components/dashboard/book-card"
import { BookFormDialog } from "@/components/dashboard/book-form-dialog"
import { DeleteConfirmDialog } from "@/components/dashboard/delete-confirm-dialog"
 import { SettingsDialog } from "@/components/dashboard/settings-dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { getBooks, createBook, updateBook, deleteBook } from "@/lib/api"
import type { Book, CreateBookDTO, UpdateBookDTO } from "@/lib/types"
import { ReadingStatus } from "@/lib/types"
import { Plus, Search, Loader2, BookOpen } from "lucide-react"
import { ChatAssistant } from "@/components/chat/chat-assistant"

export default function DashboardPage() {
  const { user, isLoading: authLoading, isAuthenticated } = useAuth()
  const router = useRouter()

  const [books, setBooks] = useState<Book[]>([])
  const [filteredBooks, setFilteredBooks] = useState<Book[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  
  const [searchQuery, setSearchQuery] = useState("")
  const [statusFilter, setStatusFilter] = useState<string>("all")


  const [isBookFormOpen, setIsBookFormOpen] = useState(false)
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const [isSettingsOpen, setIsSettingsOpen] = useState(false)
  const [selectedBook, setSelectedBook] = useState<Book | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

  
  useEffect(() => {
    if (!authLoading) {
      if (!isAuthenticated) {
        router.push("/login")
      } else if (user?.role === "Admin") {
        router.push("/admin")
      }
    }
  }, [authLoading, isAuthenticated, user, router])


  useEffect(() => {
    async function fetchBooks() {
      try {
        const data = await getBooks()
        setBooks(data)
        setFilteredBooks(data)
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to fetch books")
      } finally {
        setIsLoading(false)
      }
    }

    if (isAuthenticated) {
      fetchBooks()
    }
  }, [isAuthenticated])

  
  useEffect(() => {
    let result = books

    if (searchQuery) {
      const query = searchQuery.toLowerCase()
      result = result.filter(
        (book) =>
          book.title.toLowerCase().includes(query) ||
          book.author.toLowerCase().includes(query) ||
          book.genre.toLowerCase().includes(query),
      )
    }

    if (statusFilter !== "all") {
      result = result.filter((book) => book.readingStatus.toString() === statusFilter)
    }

    setFilteredBooks(result)
  }, [books, searchQuery, statusFilter])

  const handleAddBook = () => {
    setSelectedBook(null)
    setIsBookFormOpen(true)
  }

  const handleEditBook = (book: Book) => {
    setSelectedBook(book)
    setIsBookFormOpen(true)
  }

  const handleDeleteClick = (book: Book) => {
    setSelectedBook(book)
    setIsDeleteDialogOpen(true)
  }

  const handleBookSubmit = async (data: CreateBookDTO | UpdateBookDTO) => {
    if (selectedBook) {
      const updated = await updateBook(selectedBook.id, data as UpdateBookDTO)
      setBooks((prev) => prev.map((b) => (b.id === selectedBook.id ? updated : b)))
    } else {
      const created = await createBook(data as CreateBookDTO)
      setBooks((prev) => [...prev, created])
    }
  }

  const handleDeleteConfirm = async () => {
    if (!selectedBook) return

    setIsDeleting(true)
    try {
      await deleteBook(selectedBook.id)
      setBooks((prev) => prev.filter((b) => b.id !== selectedBook.id))
      setIsDeleteDialogOpen(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete book")
    } finally {
      setIsDeleting(false)
    }
  }

  if (authLoading || (!isAuthenticated && !authLoading)) {
    return (
      <div className="flex h-screen items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <DashboardHeader onSettingsClick={() => setIsSettingsOpen(true)} />

      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
       
        <div className="mb-8 grid gap-4 sm:grid-cols-3">
          <div className="rounded-lg border border-border bg-card p-4">
            <p className="text-sm text-muted-foreground">Total Books</p>
            <p className="text-2xl font-bold text-foreground">{books.length}</p>
          </div>
          <div className="rounded-lg border border-border bg-card p-4">
            <p className="text-sm text-muted-foreground">Currently Reading</p>
            <p className="text-2xl font-bold text-primary">
              {books.filter((b) => b.readingStatus === ReadingStatus.Reading).length}
            </p>
          </div>
          <div className="rounded-lg border border-border bg-card p-4">
            <p className="text-sm text-muted-foreground">Completed</p>
            <p className="text-2xl font-bold text-green-600">
              {books.filter((b) => b.readingStatus === ReadingStatus.Completed).length}
            </p>
          </div>
        </div>
 
    
        <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex flex-1 gap-3">
            <div className="relative flex-1 sm:max-w-xs">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Search books..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-9"
              />
            </div>
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger className="w-[140px]">
                <SelectValue placeholder="All Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Status</SelectItem>
                <SelectItem value="0">Not Started</SelectItem>
                <SelectItem value="1">Reading</SelectItem>
                <SelectItem value="2">Completed</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <Button onClick={handleAddBook}>
            <Plus className="mr-2 h-4 w-4" />
            Add Book
          </Button>
        </div>

      
        {error && <div className="mb-6 rounded-md bg-destructive/10 p-4 text-destructive">{error}</div>}

       
        {isLoading ? (
          <div className="flex h-64 items-center justify-center">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
          </div>
        ) : filteredBooks.length === 0 ? (
          <div className="flex h-64 flex-col items-center justify-center text-center">
            <BookOpen className="mb-4 h-12 w-12 text-muted-foreground" />
            <h3 className="text-lg font-medium text-foreground">No books found</h3>
            <p className="text-muted-foreground">
              {books.length === 0 ? "Start by adding your first book!" : "Try adjusting your search or filter."}
            </p>
            {books.length === 0 && (
              <Button onClick={handleAddBook} className="mt-4">
                <Plus className="mr-2 h-4 w-4" />
                Add Your First Book
              </Button>
            )}
          </div>
        ) : (
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {filteredBooks.map((book) => (
              <BookCard key={book.id} book={book} onEdit={handleEditBook} onDelete={handleDeleteClick} />
            ))}
          </div>
        )}
      </main>

  
      <BookFormDialog
        open={isBookFormOpen}
        onOpenChange={setIsBookFormOpen}
        book={selectedBook}
        onSubmit={handleBookSubmit}
      />

      <DeleteConfirmDialog
        open={isDeleteDialogOpen}
        onOpenChange={setIsDeleteDialogOpen}
        book={selectedBook}
        onConfirm={handleDeleteConfirm}
        isLoading={isDeleting}
      />

      <SettingsDialog open={isSettingsOpen} onOpenChange={setIsSettingsOpen} />
  
    <ChatAssistant />
    </div>
  )
}
