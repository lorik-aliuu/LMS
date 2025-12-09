import type { Book, CreateBookDTO, UpdateBookDTO } from "./types"
import { getAccessToken, tryRefreshToken } from "./token-manager"

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "https://localhost:7112/api"

function getAuthHeaders(): HeadersInit {
  const token = getAccessToken()
  return {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  }
}

async function authenticatedFetch(url: string, options: RequestInit = {}): Promise<Response> {
  const response = await fetch(url, {
    ...options,
    headers: {
      ...getAuthHeaders(),
      ...options.headers,
    },
  })

  if (response.status === 401) {
    const refreshed = await tryRefreshToken()
    if (refreshed) {
      return fetch(url, {
        ...options,
        headers: {
          ...getAuthHeaders(),
          ...options.headers,
        },
      })
    }
  }

  return response
}

async function parseError(response: Response) {
  try {
    const data = await response.json()
    const validation =
      (data?.errors &&
        Object.values(data.errors as Record<string, string[]>)
          .flat()
          .join(" ")) ||
      data?.message ||
      data?.title ||
      data?.error
    return validation || `Request failed (${response.status} ${response.statusText})`
  } catch {
    return `Request failed (${response.status} ${response.statusText})`
  }
}


export async function getBooks(): Promise<Book[]> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Book/my-books`, {
    method: "GET",
  })

  if (!response.ok) {
    throw new Error("Failed to fetch books")
  }

  const data = await response.json()
  return data.data || data
}

export async function getBook(id: number): Promise<Book> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Book/${id}`, {
    method: "GET",
  })

  if (!response.ok) {
    throw new Error("Failed to fetch book")
  }

  const data = await response.json()
  return data.data || data
}

export async function createBook(book: CreateBookDTO): Promise<Book> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Book`, {
    method: "POST",
    body: JSON.stringify({
      Title: book.title,
      Author: book.author,
      Genre: book.genre,
      Price: book.price,
      PublicationYear: book.publicationYear,
      ReadingStatus: book.readingStatus,
      Rating: book.rating,
      CoverImageUrl: book.coverImageUrl,
    }),
  })

  if (!response.ok) {
    throw new Error(await parseError(response))
  }

  const data = await response.json()
  return data.data || data
}

export async function updateBook(id: number, book: UpdateBookDTO): Promise<Book> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Book/${id}`, {
    method: "PUT",
    body: JSON.stringify({
      Id: id,
      Title: book.title,
      Author: book.author,
      Genre: book.genre,
      Price: typeof book.price === "number" ? book.price : Number(book.price ?? 0),
      PublicationYear: book.publicationYear,
      ReadingStatus: book.readingStatus,
      Rating: book.rating,
      CoverImageUrl: book.coverImageUrl,
    }),
  })

  if (!response.ok) {
    throw new Error(await parseError(response))
  }

  const data = await response.json()
  return data.data || data
}

export async function deleteBook(id: number): Promise<void> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Book/${id}`, {
    method: "DELETE",
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to delete book")
  }
}


