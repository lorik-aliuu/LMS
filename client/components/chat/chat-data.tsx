"use client"

import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Card } from "@/components/ui/card"
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from "recharts"
import { BookOpen } from "lucide-react"

interface ChatDataDisplayProps {
  data: Record<string, unknown>[]
  chartType?: string
}

export function ChatDataDisplay({ data, chartType }: ChatDataDisplayProps) {
  if (!data || data.length === 0) return null

  const columns = Object.keys(data[0])

  const formatValue = (value: unknown): string => {
    if (value === null || value === undefined) return "-"
    if (typeof value === "number") {
      return value % 1 === 0 ? value.toString() : value.toFixed(2)
    }
    return String(value)
  }

  
  const formatHeader = (key: string): string => {
    return key
      .replace(/([A-Z])/g, " $1")
      .replace(/^./, (str) => str.toUpperCase())
      .trim()
  }
  if (chartType === "single" && data.length === 1) {
    const item = data[0]
    const metric = (item["metric"] as string) || "Value"
    const value = item["value"]

    return (
      <Card className="mt-2 overflow-hidden">
        <div className="flex items-center gap-4 p-4">
          <div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
            <BookOpen className="h-6 w-6 text-primary" />
          </div>
          <div>
            <p className="text-sm text-muted-foreground">{metric}</p>
            <p className="text-3xl font-bold text-foreground">{formatValue(value)}</p>
          </div>
        </div>
      </Card>
    )
  }

  if (chartType === "bar" && data.length > 0) {
  
    const numericKey = columns.find((col) => typeof data[0][col] === "number")
    const labelKey = columns.find((col) => typeof data[0][col] === "string") || columns[0]

    if (numericKey) {
      return (
        <Card className="mt-2 overflow-hidden">
       <div
              className="w-full p-4 overflow-y-auto"
              style={{ height: Math.max(300, data.length * 40) }}
              >
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={data} layout="vertical">
                <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                <XAxis type="number" className="text-xs" />
                <YAxis
                  dataKey={labelKey}
                  type="category"
                  width={100}
                  className="text-xs"
                  tickFormatter={(value) =>
                    String(value).length > 15 ? `${String(value).slice(0, 15)}...` : String(value)
                  }
                />
                <Tooltip
                  contentStyle={{
                    backgroundColor: "#fff",
                    border: "1px solid hsl(var(--border))",
                    borderRadius: "8px",
                     color: "#000"
                  }}  
                />
                <Bar dataKey={numericKey} fill="hsl(var(--primary))" radius={[0, 4, 4, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </Card>
      )
    }
  }

 
  return (
    <Card className="mt-2 overflow-hidden">
      <div className="max-h-64 overflow-auto">
        <Table>
          <TableHeader>
            <TableRow>
              {columns.map((col) => (
                <TableHead key={col} className="whitespace-nowrap text-xs">
                  {formatHeader(col)}
                </TableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {data.map((row, rowIndex) => (
              <TableRow key={rowIndex}>
                {columns.map((col) => (
                  <TableCell key={col} className="whitespace-nowrap text-xs">
                    {formatValue(row[col])}
                  </TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </Card>
  )
}
