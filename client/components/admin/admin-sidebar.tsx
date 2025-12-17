"use client"

import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { LayoutDashboard, Users, BookOpen, TrendingUp } from "lucide-react"

type Tab = "overview" | "users" | "books" | "insights"

interface AdminSidebarProps {
  activeTab: Tab
  onTabChange: (tab: Tab) => void
}

export function AdminSidebar({ activeTab, onTabChange }: AdminSidebarProps) {
  const navItems = [
    { id: "overview" as Tab, label: "Overview", icon: LayoutDashboard },
    { id: "users" as Tab, label: "Users", icon: Users },
    { id: "books" as Tab, label: "All Books", icon: BookOpen },
    { id: "insights" as Tab, label: "Insights", icon: TrendingUp },
  ]

  return (
    <aside className="hidden w-64 shrink-0 border-r border-border bg-card md:block">
      <nav className="flex flex-col gap-1 p-4">
        {navItems.map((item) => (
          <Button
            key={item.id}
            variant="ghost"
            className={cn(
              "justify-start gap-3",
              activeTab === item.id && "bg-primary/10 text-primary hover:bg-primary/15",
            )}
            onClick={() => onTabChange(item.id)}
          >
            <item.icon className="h-4 w-4" />
            {item.label}
          </Button>
        ))}
      </nav>
    </aside>
  )
}
