// "use client";

// import Link from "next/link";
// import { usePathname } from "next/navigation";
// import { useEffect } from "react";
// import { useCurrentUser, useIsAuthenticated, useLogout } from "@/hooks/useAuth";

// type NavLink = { label: string; href: string };

// const ADMIN_LINKS: NavLink[] = [
//   { label: "Sự kiện",    href: "/" },
//   { label: "Bộ tiêu chí",  href: "/criteria" },
//   { label: "Chấm điểm", href: "/scoring" },
//   { label: "Xếp hạng",  href: "/leaderboard" },
//   { label: "Bài nộp",   href: "/submission" },
//   { label: "Phúc khảo", href: "/appeals" },
//   { label: "Phê duyệt", href: "/approval" },
// ];

// const MENTOR_LINKS: NavLink[] = [
//   { label: "Sự kiện",    href: "/" },
//   { label: "Chấm điểm", href: "/scoring" },
//   { label: "Xếp hạng",  href: "/leaderboard" },
//   { label: "Bài nộp",   href: "/submission" },
//   { label: "Phúc khảo", href: "/appeals" },
// ];

// const STUDENT_LINKS: NavLink[] = [
//   { label: "Sự kiện",    href: "/" },
//   { label: "Xếp hạng",  href: "/team-ranking" },
//   { label: "Nộp bài",   href: "/submit-project" },
//   { label: "Phúc khảo", href: "/team-appeal" },
//   { label: "Đăng ký",   href: "/dang-ky" },
// ];

// const GUEST_LINKS: NavLink[] = [
//   { label: "Sự kiện", href: "/" },
// ];

// export function Navbar() {
//   const pathname = usePathname();
//   const isAuthenticated = useIsAuthenticated();
//   const { data: user } = useCurrentUser();
//   const logout = useLogout();

//   // Focus main on route change for keyboard/a11y
//   useEffect(() => {
//     const main = document.querySelector<HTMLElement>("main");
//     if (main) main.focus();
//   }, [pathname]);

//   const links: NavLink[] =
//     !isAuthenticated          ? GUEST_LINKS   :
//     user?.role === "ADMIN"    ? ADMIN_LINKS   :
//     user?.role === "MENTOR"   ? MENTOR_LINKS  :
//     user?.role === "STUDENT"  ? STUDENT_LINKS :
//     GUEST_LINKS;

//   return (
//     <header>
//       <nav
//         className="primary-nav sticky-chrome"
//         style={{ position: "sticky", top: 0, zIndex: 30 }}
//       >
//         <Link
//           href="/"
//           className="primary-nav__brand"
//           style={{ fontSize: "var(--fs-heading-sm)", textDecoration: "none" }}
//         >
//           SWP<span style={{ color: "var(--color-primary)" }}>·</span>SE1907
//         </Link>

//         <ul className="primary-nav__links">
//           {links.map(({ label, href }) => (
//             <li key={href}>
//               <Link
//                 href={href}
//                 style={{
//                   color:
//                     pathname === href
//                       ? "var(--color-primary)"
//                       : "var(--color-on-dark)",
//                   fontWeight: pathname === href ? 700 : 400,
//                   fontSize: "var(--fs-body-md)",
//                   textDecoration: "none",
//                 }}
//               >
//                 {label}
//               </Link>
//             </li>
//           ))}
//         </ul>

//         <div className="primary-nav__cluster">
//           {isAuthenticated ? (
//             <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
//               {user?.fullName && (
//                 <span
//                   style={{
//                     fontSize: "var(--fs-body-sm)",
//                     color: "var(--color-on-dark-mute)",
//                   }}
//                 >
//                   {user.fullName}
//                 </span>
//               )}
//               <button
//                 type="button"
//                 onClick={() => logout.mutate()}
//                 disabled={logout.isPending}
//                 className="btn btn-outline-on-dark btn-sm"
//               >
//                 {logout.isPending ? "Đang đăng xuất…" : "Đăng xuất"}
//               </button>
//             </div>
//           ) : (
//             <Link href="/auth" className="btn btn-outline-on-dark btn-sm">
//               Đăng nhập
//             </Link>
//           )}
//         </div>
//       </nav>
//     </header>
//   );
// }

"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import { User } from "lucide-react";
import { useCurrentUser, useIsAuthenticated, useLogout } from "@/hooks/useAuth";
import { NotificationBell } from "./NotificationBell";

type NavLink = { label: string; href: string; children?: { label: string; href: string }[] };

const ADMIN_LINKS: NavLink[] = [
  { label: "Trang chủ",     href: "/" },
  {
    label: "Bộ tiêu chí", href: "/criteria",
    children: [
      { label: "Bộ tiêu chí", href: "/criteria" },
      { label: "Tiêu chí",    href: "/criteria/pool" },
    ],
  },
  { label: "Người dùng", href: "/users" },
  { label: "Trường học", href: "/schools" },
];

const MENTOR_LINKS: NavLink[] = [
  { label: "Trang chủ",    href: "/" },
];

const STUDENT_LINKS: NavLink[] = [
  { label: "Trang chủ",    href: "/" },
];

const GUEST_LINKS: NavLink[] = [
  { label: "Trang chủ", href: "/" },
];

export function Navbar() {
  const pathname       = usePathname();
  const isAuthenticated = useIsAuthenticated();
  const { data: user } = useCurrentUser();
  const logout         = useLogout();
  const [openMenu, setOpenMenu] = useState<string | null>(null);

  useEffect(() => {
    const main = document.querySelector<HTMLElement>("main");
    if (main) main.focus({ preventScroll: true });
  }, [pathname]);

  const baseLinks: NavLink[] =
    !isAuthenticated         ? GUEST_LINKS   :
    user?.role === "ADMIN"   ? ADMIN_LINKS   :
    user?.role === "MENTOR"  ? MENTOR_LINKS  :
    user?.role === "STUDENT" ? STUDENT_LINKS :
    GUEST_LINKS;

  // Nếu đang ở /events/{id}/... → gắn eventId vào link Chấm điểm
  const eventIdFromPath = pathname?.match(/^\/events\/([^/]+)/)?.[1] ?? null;
  const links = eventIdFromPath
    ? baseLinks.map(l =>
        l.href === "/scoring"
          ? { ...l, href: `/scoring?eventId=${eventIdFromPath}` }
          : l
      )
    : baseLinks;

  const isActive = (href: string) =>
    pathname === href || (pathname?.startsWith(href + "/") ?? false)

  return (
    <header>
      <nav
        className="primary-nav sticky-chrome"
        style={{ position: "sticky", top: 0, zIndex: 30 }}
      >
        <Link
          href="/"
          className="primary-nav__brand"
          style={{ fontSize: "var(--fs-heading-sm)", textDecoration: "none" }}
        >
          <span style={{ color: "var(--color-primary)" }}>SEAL-HACKATHON</span>
        </Link>

        <ul className="primary-nav__links" style={{ position: "relative" }}>
          {links.map(({ label, href, children }) =>
            children ? (
              // ── Nav item có dropdown ─────────────────────────────────────
              <li
                key={href}
                style={{ position: "relative" }}
                onMouseEnter={() => setOpenMenu(href)}
                onMouseLeave={() => setOpenMenu(null)}
              >
                {/* Label — click vẫn đi đến href chính */}
                <Link
                  href={href}
                  style={{
                    color:      isActive(href) ? "var(--color-primary)" : "var(--color-on-dark)",
                    fontWeight: isActive(href) ? 700 : 400,
                    fontSize:   "var(--fs-body-md)",
                    textDecoration: "none",
                    display: "flex",
                    alignItems: "center",
                    gap: 4,
                  }}
                >
                  {label}
                  {/* mũi tên nhỏ */}
                  <span style={{
                    fontSize: 9,
                    display: "inline-block",
                    transition: "transform .2s",
                    transform: openMenu === href ? "rotate(180deg)" : "rotate(0deg)",
                    color: "var(--color-on-dark-mute)",
                  }}>▼</span>
                </Link>

                {/* Dropdown panel */}
                {openMenu === href && (
                  <>
                    {/* Bridge chống mất hover */}
                    <div style={{
                      position: "absolute",
                      top: "100%",
                      left: 0,
                      width: "100%",
                      height: "8px",
                      backgroundColor: "transparent",
                      zIndex: 99
                    }} />
                    <ul style={{
                      position:   "absolute",
                      top:        "calc(100% + 8px)",
                      left:       "50%",
                      transform:  "translateX(-50%)",
                      minWidth:   160,
                      background: "#1a1a1a",
                      border:     "1px solid rgba(255,255,255,.1)",
                      borderRadius: 4,
                      padding:    "6px 0",
                      listStyle:  "none",
                      margin:     0,
                      boxShadow:  "0 8px 24px rgba(0,0,0,.4)",
                      zIndex:     100,
                    }}>
                    {/* mũi tên nhỏ trỏ lên */}
                    <div style={{
                      position:    "absolute",
                      top:         -5,
                      left:        "50%",
                      transform:   "translateX(-50%)",
                      width:       10,
                      height:      10,
                      background:  "#1a1a1a",
                      border:      "1px solid rgba(255,255,255,.1)",
                      borderBottom: "none",
                      borderRight:  "none",
                      rotate:       "45deg",
                    }} />

                    {children.map(child => (
                      <li key={child.href}>
                        <Link
                          href={child.href}
                          onClick={() => setOpenMenu(null)}
                          style={{
                            display:    "block",
                            padding:    "9px 18px",
                            fontSize:   "var(--fs-body-sm)",
                            fontWeight: pathname === child.href ? 700 : 400,
                            color:      pathname === child.href
                              ? "var(--color-primary)"
                              : "var(--color-on-dark)",
                            textDecoration: "none",
                            whiteSpace: "nowrap",
                            transition: "background .15s",
                          }}
                          onMouseEnter={e => (e.currentTarget.style.background = "rgba(255,255,255,.07)")}
                          onMouseLeave={e => (e.currentTarget.style.background = "transparent")}
                        >
                          {child.label}
                        </Link>
                      </li>
                    ))}
                  </ul>
                  </>
                )}
              </li>
            ) : (
              // ── Nav item thường ──────────────────────────────────────────
              <li key={href}>
                <Link
                  href={href}
                  style={{
                    color:      isActive(href) ? "var(--color-primary)" : "var(--color-on-dark)",
                    fontWeight: isActive(href) ? 700 : 400,
                    fontSize:   "var(--fs-body-md)",
                    textDecoration: "none",
                  }}
                >
                  {label}
                </Link>
              </li>
            )
          )}
        </ul>

        <div className="primary-nav__cluster">
          {isAuthenticated ? (
            <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
              <NotificationBell />
              {user?.fullName && (
                <Link 
                  href="/profile"
                  style={{ display: "flex", alignItems: "center", transition: "opacity 0.2s" }}
                  onMouseEnter={(e) => (e.currentTarget.style.opacity = "0.8")}
                  onMouseLeave={(e) => (e.currentTarget.style.opacity = "1")}
                  title={`Hồ sơ của ${user.fullName}`}
                >
                  <User color="rgb(118, 185, 0)" size={20} />
                </Link>
              )}
              <button
                type="button"
                onClick={() => logout.mutate()}
                disabled={logout.isPending}
                className="btn btn-outline-on-dark btn-sm"
              >
                {logout.isPending ? "Đang đăng xuất…" : "Đăng xuất"}
              </button>
            </div>
          ) : (
            <Link href="/auth" className="btn btn-outline-on-dark btn-sm">
              Đăng nhập
            </Link>
          )}
        </div>
      </nav>
    </header>
  );
}
