import type { Metadata } from "next";
import { Inter } from "next/font/google";
import { QueryProvider } from "@/providers/QueryProvider";
import { AuthProvider } from "@/context/AuthContext";
import { NotificationProvider } from "@/components/NotificationProvider";
import { ConfirmDialogProvider } from "@/components/ConfirmDialogProvider";
import { ForcePasswordChangeGate } from "@/components/ForcePasswordChangeGate";
import "./globals.css";

const inter = Inter({
  variable: "--font-inter",
  subsets: ["latin"],
  weight: ["400", "700"],
  display: "swap",
});

export const metadata: Metadata = {
  title: " SEAL-HACKATHON",
  description: "Software Project Platform",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="vi" className={`${inter.variable} h-full`} suppressHydrationWarning>
      <body className="min-h-full flex flex-col antialiased" suppressHydrationWarning>
        <AuthProvider>
          <QueryProvider>
            <NotificationProvider>
              <ConfirmDialogProvider>
                {children}
                <ForcePasswordChangeGate />
              </ConfirmDialogProvider>
            </NotificationProvider>
          </QueryProvider>
        </AuthProvider>
      </body>
    </html>
  );
}
