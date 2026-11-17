import type { Metadata } from "next";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: "Dashboard · Humanitarian Aid Platform",
};

export default function DashboardLayout({
  children,
}: Readonly<{ children: ReactNode }>) {
  return children;
}
