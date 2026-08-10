namespace SEAL_Application.Commons
{
    /// <summary>
    /// Template email SEAL — phong cách sáng, thoáng (nền kem, logo, nút bo tròn dạng "viên thuốc",
    /// dải hình trang trí ở chân). Dùng chung cho mọi email hệ thống để đồng bộ giao diện.
    /// </summary>
    public static class EmailTemplate
    {
        public enum Callout { Info, Success, Danger, Warning }

        private const string Primary = "#76b900";      // xanh lá thương hiệu
        private const string PrimaryDark = "#5c9200";
        private const string Ink = "#2b2b2b";
        private const string Mute = "#6b6b6b";
        private const string Canvas = "#fbf7ee";       // nền kem sáng
        private const string Card = "#ffffff";

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
                Callout.Danger => ("#fdecec", "#e52020", "#a32d2d"),
                Callout.Warning => ("#fef6e0", "#df6500", "#854f0b"),
                Callout.Info => ("#e8f1fb", "#378add", "#0c447c"),
                _ => ("#f0f7e2", Primary, "#3b6d11"),
            };

            var b = new System.Text.StringBuilder();

            // ── Bọc ngoài (nền kem) ──────────────────────────────────────────────
            b.Append($@"<div style=""margin:0;padding:32px 12px;background:{Canvas};font-family:Arial,Helvetica,sans-serif;"">");
            b.Append($@"<div style=""max-width:560px;margin:0 auto;background:{Card};border:1px solid #efe7d6;border-radius:14px;overflow:hidden;"">");
            b.Append(@"<div style=""padding:36px 40px 8px;"">");

            // ── Logo ─────────────────────────────────────────────────────────────
            b.Append(@"<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:26px;""><tr>");
            b.Append($@"<td style=""width:44px;vertical-align:middle;""><div style=""width:44px;height:44px;background:{Primary};border-radius:12px;text-align:center;line-height:44px;color:#fff;font-weight:bold;font-size:20px;"">S</div></td>");
            b.Append($@"<td style=""padding-left:12px;vertical-align:middle;""><div style=""font-size:16px;font-weight:bold;color:#111;letter-spacing:.5px;line-height:1.1;"">SEAL</div><div style=""font-size:11px;color:{Mute};letter-spacing:.14em;text-transform:uppercase;"">Event Platform</div></td>");
            b.Append(@"</tr></table>");

            // ── Tiêu đề + chào ───────────────────────────────────────────────────
            b.Append($@"<h1 style=""font-size:23px;line-height:1.3;color:#111;margin:0 0 16px;font-weight:bold;"">{heading}</h1>");
            if (!string.IsNullOrEmpty(greetingName))
                b.Append($@"<p style=""font-size:15px;line-height:1.65;margin:0 0 10px;color:{Ink};"">Chào <b>{greetingName}</b>,</p>");
            b.Append($@"<p style=""font-size:15px;line-height:1.65;margin:0 0 4px;color:{Ink};"">{introHtml}</p>");

            // ── Hộp thông tin ────────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(calloutLabel) || !string.IsNullOrEmpty(calloutHtml))
            {
                b.Append($@"<div style=""background:{cBg};border-left:3px solid {cBar};border-radius:8px;padding:14px 18px;margin:20px 0;"">");
                if (!string.IsNullOrEmpty(calloutLabel))
                    b.Append($@"<div style=""font-size:11px;font-weight:bold;letter-spacing:.06em;text-transform:uppercase;color:{cLabel};margin-bottom:6px;"">{calloutLabel}</div>");
                if (!string.IsNullOrEmpty(calloutHtml))
                    b.Append($@"<div style=""font-size:14px;line-height:1.8;color:{Ink};"">{calloutHtml}</div>");
                b.Append("</div>");
            }

            // ── Nút CTA (viên thuốc, canh giữa) ─────────────────────────────────
            if (!string.IsNullOrEmpty(ctaText) && !string.IsNullOrEmpty(ctaUrl))
            {
                b.Append(@"<div style=""text-align:center;margin:28px 0 6px;"">");
                b.Append($@"<a href=""{ctaUrl}"" style=""display:inline-block;background:{Primary};color:#ffffff;text-decoration:none;font-weight:bold;font-size:14px;padding:13px 30px;border-radius:40px;margin:4px 6px;"">{ctaText}</a>");
                if (!string.IsNullOrEmpty(ctaText2) && !string.IsNullOrEmpty(ctaUrl2))
                    b.Append($@"<a href=""{ctaUrl2}"" style=""display:inline-block;background:#ffffff;color:#a32d2d;text-decoration:none;font-weight:bold;font-size:14px;padding:12px 30px;border:2px solid #e6b3b3;border-radius:40px;margin:4px 6px;"">{ctaText2}</a>");
                b.Append("</div>");
                if (showLoginHint)
                {
                    var fallback = string.IsNullOrEmpty(ctaFallbackUrl) ? ctaUrl : ctaFallbackUrl;
                    b.Append($@"<p style=""font-size:12px;color:#9a9a9a;text-align:center;margin:6px 0 0;"">Hoặc <a href=""{fallback}"" style=""color:{PrimaryDark};font-weight:bold;text-decoration:underline;"">đăng nhập hệ thống</a> để xem và phản hồi lời mời trong phần thông báo.</p>");
                }
            }

            // ── Ghi chú ──────────────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(noteHtml))
                b.Append($@"<p style=""font-size:13px;line-height:1.6;color:{Mute};margin:22px 0 0;text-align:center;"">⏳ {noteHtml}</p>");

            b.Append("</div>"); // hết padding nội dung

            // ── Dải hình trang trí ──────────────────────────────────────────────
            b.Append(@"<div style=""text-align:center;padding:26px 0 22px;"">");
            b.Append(@"<span style=""display:inline-block;width:12px;height:12px;background:#f0c419;border-radius:50%;margin:0 6px;""></span>");
            b.Append(@"<span style=""display:inline-block;width:12px;height:12px;background:#e5533c;border-radius:3px;margin:0 6px;""></span>");
            b.Append($@"<span style=""display:inline-block;width:16px;height:16px;background:{Primary};border-radius:50%;margin:0 6px;""></span>");
            b.Append(@"<span style=""display:inline-block;width:12px;height:12px;background:#3a86ff;border-radius:3px;margin:0 6px;""></span>");
            b.Append(@"<span style=""display:inline-block;width:12px;height:12px;background:#8e44ad;border-radius:50%;margin:0 6px;""></span>");
            b.Append("</div>");

            // ── Footer ───────────────────────────────────────────────────────────
            b.Append($@"<div style=""background:#faf4e9;padding:16px 40px;font-size:11px;line-height:1.6;color:#a89f8c;text-align:center;border-top:1px solid #efe7d6;"">Email tự động từ hệ thống <b style=""color:{Mute};"">SEAL</b> — vui lòng không trả lời email này.</div>");

            b.Append("</div>"); // hết card
            b.Append("</div>"); // hết wrapper

            return b.ToString();
        }
    }
}
