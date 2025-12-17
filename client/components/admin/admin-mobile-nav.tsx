"use client"

import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { LayoutDashboard, Users, BookOpen, TrendingUp } from "lucide-react"

type Tab = "overview" | "users" | "books" | "insights"

interface AdminMobileNavProps {
  activeTab: Tab
  onTabChange: (tab: Tab) => void
}

export function AdminMobileNav({ activeTab, onTabChange }: AdminMobileNavProps) {
  const navItems = [
    { id: "overview" as Tab, label: "Overview", icon: LayoutDashboard },
    { id: "users" as Tab, label: "Users", icon: Users },
    { id: "books" as Tab, label: "Books", icon: BookOpen },
    { id: "insights" as Tab, label: "Insights", icon: TrendingUp },
  ]

  return (
    <div className="flex border-b border-border bg-card p-2 md:hidden">
      {navItems.map((item) => (
        <Button
          key={item.id}
          variant="ghost"
          size="sm"
          className={cn("flex-1 gap-2", activeTab === item.id && "bg-primary/10 text-primary")}
          onClick={() => onTabChange(item.id)}
        >
          <item.icon className="h-4 w-4" />
          <span className="text-xs">{item.label}</span>
        </Button>
      ))}
    </div>
  )
}
