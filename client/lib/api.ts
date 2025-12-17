import type { Book, CreateBookDTO, UpdateBookDTO, UserProfile, UpdateProfileDTO, ChangePasswordDTO, DeleteAccountDTO, AdminUser, AdminBook, UpdateRoleDTO, RecommendationResponse, DismissRecommendationDTO, SaveRecommendationDTO, UserReadingHabits, LibraryInsights } from "./types"
import { getAccessToken, tryRefreshToken } from "./token-manager"

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL

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


export async function getProfile(): Promise<UserProfile> {
  const response = await authenticatedFetch(`${API_BASE_URL}/User/profile`, {
    method: "GET",
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to fetch profile")
  }

  const data = await response.json()
  return data.data || data
}

export async function updateProfile(data: UpdateProfileDTO): Promise<void> {
  const response = await authenticatedFetch(`${API_BASE_URL}/User/profile`, {
    method: "PUT",
    body: JSON.stringify({
      FirstName: data.firstName,
      LastName: data.lastName,
      Email: data.email,
      PhoneNumber: data.phoneNumber,
      DateOfBirth: data.dateOfBirth,
    }),
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to update profile")
  }
}

export async function changePassword(data: ChangePasswordDTO): Promise<void> {
  const response = await authenticatedFetch(`${API_BASE_URL}/User/change-password`, {
    method: "POST",
    body: JSON.stringify({
      CurrentPassword: data.currentPassword,
      NewPassword: data.newPassword,
      ConfirmNewPassword: data.confirmNewPassword,
    }),
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to change password")
  }
}

export async function deleteAccount(data: DeleteAccountDTO): Promise<void> {
  const response = await authenticatedFetch(`${API_BASE_URL}/User/account`, {
    method: "DELETE",
    body: JSON.stringify({
      Password: data.password,
    }),
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to delete account")
  }
}

export async function getAllUsers(): Promise<AdminUser[]> {
  const response = await authenticatedFetch(`${API_BASE_URL}/User`, {
    method: "GET",
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to fetch users")
  }

  const data = await response.json()
  return data.data || data
}

export async function getUserById(userId: string): Promise<AdminUser> {
  const response = await authenticatedFetch(`${API_BASE_URL}/User/${userId}`, {
    method: "GET",
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to fetch user")
  }

  const data = await response.json()
  return data.data || data
}

export async function deleteUser(userId: string): Promise<void> {
  const response = await authenticatedFetch(`${API_BASE_URL}/User/${userId}`, {
    method: "DELETE",
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to delete user")
  }
}

export async function updateUserRole(userId: string, role: UpdateRoleDTO): Promise<void> {
  const response = await authenticatedFetch(`${API_BASE_URL}/User/${userId}/role`, {
    method: "PUT",
    body: JSON.stringify({ Role: role.role }),
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    const errorMessage = error.message || `Failed to update user role (${response.status})`
    console.error(" Role update failed:", errorMessage)
    throw new Error(errorMessage)
  }
}


export async function getAllBooksAdmin(): Promise<AdminBook[]> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Book/admin/all`, {
    method: "GET",
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to fetch all books")
  }

  const data = await response.json()
  return data.data || data
}

export async function getBookByIdAdmin(id: number): Promise<AdminBook> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Book/admin/${id}`, {
    method: "GET",
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to fetch book")
  }

  const data = await response.json()
  return data.data || data
}

export async function deleteBookAdmin(id: number): Promise<void> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Book/admin/${id}`, {
    method: "DELETE",
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to delete book")
  }
}

export async function getTotalBooksCount(): Promise<number> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Book/admin/count`, {
    method: "GET",
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to fetch books count")
  }

  const data = await response.json()
  return data.totalBooks || 0
}


export async function sendAiQuery(query: string): Promise<{
  success: boolean
  answer: string
  interpretedQuery?: string
  data?: Record<string, unknown>[]
  chartType?: string
  errorMessage?: string
}> {
  const response = await authenticatedFetch(`${API_BASE_URL}/AiQuery/query`, {
    method: "POST",
    body: JSON.stringify({ Query: query }),
  })

  const result = await response.json()

  if (!response.ok || !result.success) {
    return {
      success: false,
      answer: result.errorMessage || result.message || "Failed to process query",
      errorMessage: result.errorMessage || result.message,
    }
  }

  return {
    success: true,
    answer: result.answer,
    interpretedQuery: result.interpretedQuery,
    data: result.data,
    chartType: result.chartType,
  }
}

export async function getAiExamples(): Promise<string[]> {
  const response = await authenticatedFetch(`${API_BASE_URL}/AiQuery/examples`, {
    method: "GET",
  })

  if (!response.ok) {
    return []
  }

  const result = await response.json()
  return result.data || []
}

export async function getRecommendations(count = 5): Promise<RecommendationResponse> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Recommendation`, {
    method: "POST",
    body: JSON.stringify({ Count: count }),
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to get recommendations")
  }

  const data = await response.json()


  const mappedRecommendations = (data.recommendations || []).map((rec: Record<string, unknown>) => ({
    title: rec.title || rec.Title || "",
    author: rec.author || rec.Author || "",
    genre: rec.genre || rec.Genre || "",
    estimatedPrice: rec.estimatedPrice ?? rec.EstimatedPrice ?? 0,
    reason: rec.reason || rec.Reason || "",
  }))

  return {
    ...data,
    recommendations: mappedRecommendations,
  }
}

export async function saveRecommendedBook(data: SaveRecommendationDTO): Promise<void> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Recommendation/save`, {
    method: "POST",
    body: JSON.stringify({
      Title: data.title,
      Author: data.author,
      Genre: data.genre,
      EstimatedPrice: data.price,
    }),
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to save book")
  }
}

export async function dismissRecommendedBook(data: DismissRecommendationDTO): Promise<void> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Recommendation/dismiss`, {
    method: "POST",
    body: JSON.stringify({
      Title: data.title,
      Author: data.author,
    }),
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to dismiss recommendation")
  }
}

export async function getMyInsights(): Promise<LibraryInsights> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Insights/my-insights`, {
    method: "GET",
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to fetch insights")
  }

  return await response.json()
}


export async function getMyHabits(): Promise<UserReadingHabits> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Insights/my-habits`, {
    method: "GET",
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to fetch reading habits")
  }

  return await response.json()
}

export async function getLibraryInsights(): Promise<LibraryInsights> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Insights/library`, {
    method: "GET",
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to fetch library insights")
  }

  return await response.json()
}

export async function getUserHabits(userId: string): Promise<UserReadingHabits> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Insights/user/${userId}/habits`, {
    method: "GET",
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to fetch user habits")
  }

  return await response.json()
}

export async function revokeToken(refreshToken: string): Promise<void> {
  const response = await authenticatedFetch(`${API_BASE_URL}/Auth/revoke`, {
    method: "POST",
    body: JSON.stringify({ refreshToken }),
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || "Failed to revoke token")
  }
}

