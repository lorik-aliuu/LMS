"use client"

import type React from "react"

import { useState, useRef, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from "@/components/ui/sheet"
import { sendAiQuery, getAiExamples } from "@/lib/api"
import type { ChatMessage } from "@/lib/types"
import { MessageSquare, Send, Loader2, Sparkles, Bot, User } from "lucide-react"
import { ChatDataDisplay } from "./chat-data"

export function ChatAssistant() {
  const [isOpen, setIsOpen] = useState(false)
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [input, setInput] = useState("")
  const [isLoading, setIsLoading] = useState(false)
  const [examples, setExamples] = useState<string[]>([])
  const scrollRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (isOpen && examples.length === 0) {
      getAiExamples().then(setExamples)
    }
  }, [isOpen, examples.length])

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight
    }
  }, [messages])

  const handleSend = async (query?: string) => {
    const messageText = query || input.trim()
    if (!messageText || isLoading) return

    const userMessage: ChatMessage = {
      id: Date.now().toString(),
      role: "user",
      content: messageText,
      timestamp: new Date(),
    }

    setMessages((prev) => [...prev, userMessage])
    setInput("")
    setIsLoading(true)

    try {
      const response = await sendAiQuery(messageText)

      const assistantMessage: ChatMessage = {
        id: (Date.now() + 1).toString(),
        role: "assistant",
        content: response.answer,
        data: response.data,
        chartType: response.chartType,
        timestamp: new Date(),
      }

      setMessages((prev) => [...prev, assistantMessage])
    } catch (error) {
      const errorMessage: ChatMessage = {
        id: (Date.now() + 1).toString(),
        role: "assistant",
        content: "Sorry, I had an error processing your request. Please try again.",
        timestamp: new Date(),
      }
      setMessages((prev) => [...prev, errorMessage])
    } finally {
      setIsLoading(false)
    }
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  const clearChat = () => {
    setMessages([])
  }

  return (
    <Sheet open={isOpen} onOpenChange={setIsOpen}>
      <SheetTrigger asChild>
        <Button size="lg" className="fixed bottom-6 right-6 z-50 h-14 w-14 rounded-full shadow-lg">
          <MessageSquare className="h-6 w-6" />
          <span className="sr-only">Open chat assistant</span>
        </Button>
      </SheetTrigger>
      <SheetContent className="flex h-full w-full flex-col p-0 sm:max-w-lg">
        <SheetHeader className="border-b border-border px-4 py-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary">
                <Sparkles className="h-4 w-4 text-primary-foreground" />
              </div>
              <SheetTitle>Library Assistant</SheetTitle>
            </div>
            {messages.length > 0 && (
              <Button variant="ghost" size="sm" onClick={clearChat}>
                Clear
              </Button>
            )}
          </div>
        </SheetHeader>

        <div ref={scrollRef} className="flex-1 overflow-y-auto px-4">
          <div className="py-4">
            {messages.length === 0 ? (
              <div className="space-y-4">
                <div className="text-center">
                  <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
                    <Bot className="h-6 w-6 text-primary" />
                  </div>
                  <h3 className="font-medium text-foreground">How can I help you?</h3>
                  <p className="mt-1 text-sm text-muted-foreground">Ask me anything about your library</p>
                </div>

                {examples.length > 0 && (
                  <div className="space-y-2">
                    <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground">Try asking</p>
                    <div className="grid gap-2">
                      {examples.slice(0, 6).map((example, index) => (
                        <button
                          key={index}
                          onClick={() => handleSend(example)}
                          className="rounded-lg border border-border bg-card p-3 text-left text-sm transition-colors hover:bg-accent"
                        >
                          {example}
                        </button>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            ) : (
              <div className="space-y-4">
                {messages.map((message) => (
                  <div
                    key={message.id}
                    className={`flex gap-3 ${message.role === "user" ? "justify-end" : "justify-start"}`}
                  >
                    {message.role === "assistant" && (
                      <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary">
                        <Bot className="h-4 w-4 text-primary-foreground" />
                      </div>
                    )}
                    <div
                      className={`max-w-[85%] space-y-2 ${
                        message.role === "user"
                          ? "rounded-2xl rounded-br-sm bg-primary px-4 py-2 text-primary-foreground"
                          : ""
                      }`}
                    >
                      <p className={`text-sm ${message.role === "assistant" ? "text-foreground" : ""}`}>
                        {message.content}
                      </p>
                      {message.data && message.data.length > 0 && (
                        <ChatDataDisplay data={message.data} chartType={message.chartType} />
                      )}
                    </div>
                    {message.role === "user" && (
                      <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-muted">
                        <User className="h-4 w-4 text-muted-foreground" />
                      </div>
                    )}
                  </div>
                ))}
                {isLoading && (
                  <div className="flex gap-3">
                    <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary">
                      <Bot className="h-4 w-4 text-primary-foreground" />
                    </div>
                    <div className="flex items-center gap-1 text-muted-foreground">
                      <Loader2 className="h-4 w-4 animate-spin" />
                      <span className="text-sm">Thinking...</span>
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>

        <div className="border-t border-border p-4">
          <div className="flex gap-2">
            <Input
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Ask about your books..."
              disabled={isLoading}
              className="flex-1"
            />
            <Button onClick={() => handleSend()} disabled={!input.trim() || isLoading}>
              {isLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
            </Button>
          </div>
        </div>
      </SheetContent>
    </Sheet>
  )
}
