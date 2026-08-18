namespace SEAL_Application.Commons
{
    /// <summary>
    /// Template email SEAL — phong cách "Command Deck" (HUD tối màu), đồng bộ đúng bảng màu
    /// thương hiệu của Frontend (design tokens tại src/styles/tokens.css): nền Deep Space Navy,
    /// accent Electric Mint + Cyber Violet, nút bo góc vát dạng HUD. Dùng chung cho mọi email
    /// hệ thống để đồng bộ giao diện.
    /// </summary>
    public static class EmailTemplate
    {
        public enum Callout { Info, Success, Danger, Warning }

        // Bảng màu đồng bộ 1:1 với --bg-base/--bg-panel/--accent-*/--text-*/--border-muted
        // của FE (src/styles/tokens.css) — không tự chế màu riêng cho email.
        private const string BgBase = "#0a0f1d";        // Deep Space Navy — nền ngoài
        private const string BgPanel = "#111a2e";       // Panel — nền thẻ email
        private const string BgInput = "#17243e";       // Input/footer bề mặt phụ
        private const string AccentPrimary = "#2dd4bf"; // Electric Mint
        private const string AccentSecondary = "#a78bfa"; // Cyber Violet
        private const string ColorDanger = "#f87171";
        private const string TextPrimary = "#f1f5f9";
        private const string TextMuted = "#94a3b8";
        private const string BorderMuted = "#24344d";

        /// <summary>
        /// Dựng email hoàn chỉnh.
        /// </summary>
        /// <param name="heading">Tiêu đề lớn (vd: "Lời mời chấm thi").</param>
        /// <param name="greetingName">Tên người nhận để chào (null = bỏ dòng chào).</param>
        /// <param name="introHtml">Đoạn mở đầu (cho phép thẻ inline như &lt;b&gt;).</param>
        /// <param name="calloutLabel">Nhãn hộp callout (null = không có hộp).</param>
        /// <param name="calloutHtml">Nội dung hộp callout.</param>
        /// <param name="calloutKind">Màu hộp callout.</param>
        /// <param name="ctaText">Chữ nút chính (null = không có nút).</param>
        /// <param name="ctaUrl">Link nút chính (cũng hiển thị làm link dự phòng).</param>
        /// <param name="ctaText2">Chữ nút phụ (vd "Từ chối"). null = không có.</param>
        /// <param name="ctaUrl2">Link nút phụ.</param>
        /// <param name="noteHtml">Ghi chú dưới. null = không có.</param>
        public static string Render(
            string heading,
            string? greetingName,
            string introHtml,
            string? calloutLabel = null,
            string? calloutHtml = null,
            Callout calloutKind = Callout.Success,
            string? ctaText = null,
            string? ctaUrl = null,
            string? ctaText2 = null,
            string? ctaUrl2 = null,
            string? ctaFallbackUrl = null,
            string? noteHtml = null,
            bool showLoginHint = true)
        {
            var (cBg, cBar, cLabel) = calloutKind switch
            {
                Callout.Danger => ("#3d1f24", ColorDanger, "#fca5a5"),
                Callout.Warning => ("#3a2c14", "#f59e0b", "#fcd34d"),
                Callout.Info => ("#231b3d", AccentSecondary, "#c4b5fd"),
                _ => ("#0f2e29", AccentPrimary, "#5eead4"),
            };

            var b = new System.Text.StringBuilder();

            // ── Bọc ngoài (nền navy sẫm) ─────────────────────────────────────────
            b.Append($@"<div style=""margin:0;padding:32px 12px;background:{BgBase};font-family:'IBM Plex Sans',Arial,Helvetica,sans-serif;"">");
            b.Append($@"<div style=""max-width:560px;margin:0 auto;background:{BgPanel};border:1px solid {BorderMuted};border-radius:14px;overflow:hidden;"">");
            // ── Dải accent trên cùng (mint → violet) ─────────────────────────────
            b.Append($@"<div style=""height:4px;line-height:4px;font-size:0;background:linear-gradient(90deg,{AccentPrimary} 0%,{AccentSecondary} 100%);"">&nbsp;</div>");
            b.Append(@"<div style=""padding:36px 40px 8px;"">");

            // ── Logo ─────────────────────────────────────────────────────────────
            b.Append(@"<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:26px;""><tr>");
            b.Append($@"<td style=""width:44px;vertical-align:middle;""><div style=""width:44px;height:44px;background:{AccentPrimary};border-radius:10px;text-align:center;line-height:44px;color:{BgBase};font-weight:bold;font-size:20px;font-family:'Chakra Petch',Arial,sans-serif;"">S</div></td>");
            b.Append($@"<td style=""padding-left:12px;vertical-align:middle;""><div style=""font-size:16px;font-weight:bold;color:{TextPrimary};letter-spacing:.5px;line-height:1.1;font-family:'Chakra Petch',Arial,sans-serif;"">SEAL</div><div style=""font-size:11px;color:{TextMuted};letter-spacing:.14em;text-transform:uppercase;"">Event Platform</div></td>");
            b.Append(@"</tr></table>");

            // ── Tiêu đề + chào ───────────────────────────────────────────────────
            b.Append($@"<h1 style=""font-size:23px;line-height:1.3;color:{TextPrimary};margin:0 0 16px;font-weight:bold;font-family:'Chakra Petch',Arial,sans-serif;"">{heading}</h1>");
            if (!string.IsNullOrEmpty(greetingName))
                b.Append($@"<p style=""font-size:15px;line-height:1.65;margin:0 0 10px;color:{TextPrimary};"">Chào <b>{greetingName}</b>,</p>");
            b.Append($@"<p style=""font-size:15px;line-height:1.65;margin:0 0 4px;color:{TextPrimary};"">{introHtml}</p>");

            // ── Hộp thông tin ────────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(calloutLabel) || !string.IsNullOrEmpty(calloutHtml))
            {
                b.Append($@"<div style=""background:{cBg};border-left:3px solid {cBar};border-radius:8px;padding:14px 18px;margin:20px 0;"">");
                if (!string.IsNullOrEmpty(calloutLabel))
                    b.Append($@"<div style=""font-size:11px;font-weight:bold;letter-spacing:.06em;text-transform:uppercase;color:{cLabel};margin-bottom:6px;"">{calloutLabel}</div>");
                if (!string.IsNullOrEmpty(calloutHtml))
                    b.Append($@"<div style=""font-size:14px;line-height:1.8;color:{TextPrimary};"">{calloutHtml}</div>");
                b.Append("</div>");
            }

            // ── Nút CTA (HUD, bo góc vừa, canh giữa) ─────────────────────────────
            if (!string.IsNullOrEmpty(ctaText) && !string.IsNullOrEmpty(ctaUrl))
            {
                b.Append(@"<div style=""text-align:center;margin:28px 0 6px;"">");
                b.Append($@"<a href=""{ctaUrl}"" style=""display:inline-block;background:{AccentPrimary};color:{BgBase};text-decoration:none;font-weight:bold;font-size:14px;padding:13px 30px;border-radius:8px;margin:4px 6px;"">{ctaText}</a>");
                if (!string.IsNullOrEmpty(ctaText2) && !string.IsNullOrEmpty(ctaUrl2))
                    b.Append($@"<a href=""{ctaUrl2}"" style=""display:inline-block;background:transparent;color:{ColorDanger};text-decoration:none;font-weight:bold;font-size:14px;padding:11px 30px;border:2px solid #6b3a3a;border-radius:8px;margin:4px 6px;"">{ctaText2}</a>");
                b.Append("</div>");
                if (showLoginHint)
                {
                    var fallback = string.IsNullOrEmpty(ctaFallbackUrl) ? ctaUrl : ctaFallbackUrl;
                    b.Append($@"<p style=""font-size:12px;color:{TextMuted};text-align:center;margin:6px 0 0;"">Hoặc <a href=""{fallback}"" style=""color:{AccentPrimary};font-weight:bold;text-decoration:underline;"">đăng nhập hệ thống</a> để xem và phản hồi lời mời trong phần thông báo.</p>");
                }
            }

            // ── Ghi chú ──────────────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(noteHtml))
                b.Append($@"<p style=""font-size:13px;line-height:1.6;color:{TextMuted};margin:22px 0 0;text-align:center;"">⏳ {noteHtml}</p>");

            b.Append("</div>"); // hết padding nội dung

            // ── Dải chấm trang trí (đúng bộ màu phân vai của FE) ─────────────────
            b.Append(@"<div style=""text-align:center;padding:26px 0 22px;"">");
            b.Append(@"<span style=""display:inline-block;width:10px;height:10px;background:#38bdf8;border-radius:50%;margin:0 5px;""></span>"); // Team
            b.Append(@"<span style=""display:inline-block;width:10px;height:10px;background:#34d399;border-radius:3px;margin:0 5px;""></span>"); // Mentor
            b.Append($@"<span style=""display:inline-block;width:14px;height:14px;background:{AccentPrimary};border-radius:50%;margin:0 5px;""></span>"); // Primary
            b.Append(@"<span style=""display:inline-block;width:10px;height:10px;background:#fbbf24;border-radius:3px;margin:0 5px;""></span>"); // Judge
            b.Append($@"<span style=""display:inline-block;width:10px;height:10px;background:{AccentSecondary};border-radius:50%;margin:0 5px;""></span>"); // Coordinator
            b.Append("</div>");

            // ── Footer ───────────────────────────────────────────────────────────
            b.Append($@"<div style=""background:{BgInput};padding:16px 40px;font-size:11px;line-height:1.6;color:{TextMuted};text-align:center;border-top:1px solid {BorderMuted};"">Email tự động từ hệ thống <b style=""color:{TextPrimary};"">SEAL</b> — vui lòng không trả lời email này.</div>");

            b.Append("</div>"); // hết card
            b.Append("</div>"); // hết wrapper

            return b.ToString();
        }
    }
}
