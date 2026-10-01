using System.Globalization;
using System.Net;
using System.Text;

namespace TaskManager.Api.Services.Email
{
    public static class EmailTemplates
    {
        // Must match the join colour baked into frame-top.png / frame-bottom.png
        private const string FrameSide = "#0198ab";
        private const string Panel = "#0a1620";
        private const string Void = "#030b0f";

        // Clipped-corner HUD frame. Email clients ignore clip-path, so the corners are
        // two hosted cap images; the sides are plain borders, so the frame fits any height.
        private static string Frame(string rows, string assetBaseUrl)
        {
            var assets = WebUtility.HtmlEncode(assetBaseUrl.TrimEnd('/'));

            return $"""
                <!DOCTYPE html>
                <html>
                <body style="margin:0;padding:0;background:{Void};">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" bgcolor="{Void}" style="background:{Void};padding:32px 16px;">
                    <tr><td align="center">
                      <table role="presentation" width="520" cellpadding="0" cellspacing="0" style="width:100%;max-width:520px;">
                        <tr><td style="line-height:0;font-size:0;">
                          <img src="{assets}/email/frame-top.png" width="520" alt="" style="display:block;width:100%;max-width:520px;height:auto;border:0;">
                        </td></tr>
                        <tr><td bgcolor="{Panel}" style="background:{Panel};border-left:2px solid {FrameSide};border-right:2px solid {FrameSide};">
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                            {rows}
                          </table>
                        </td></tr>
                        <tr><td style="line-height:0;font-size:0;">
                          <img src="{assets}/email/frame-bottom.png" width="520" alt="" style="display:block;width:100%;max-width:520px;height:auto;border:0;">
                        </td></tr>
                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """;
        }

        private static string Button(string href, string label) => $"""
            <a href="{WebUtility.HtmlEncode(href)}" style="display:inline-block;padding:12px 24px;border:1px solid #00e5ff;background:#0f202e;color:#00e5ff;font-family:Arial,sans-serif;font-size:13px;letter-spacing:2px;text-transform:uppercase;text-decoration:none;">
              {label}
            </a>
            """;

        public static EmailMessage Verification(string to, string username, string link, string assetBaseUrl)
        {
            var safeName = WebUtility.HtmlEncode(username);

            var rows = $"""
                <tr><td style="padding:4px 28px 8px;font-family:'Courier New',monospace;font-size:11px;letter-spacing:2px;color:#0a8fa8;text-transform:uppercase;">
                  &#9679; Task Manager &middot; Identity Check
                </td></tr>
                <tr><td style="padding:0 28px;font-family:Arial,sans-serif;font-size:22px;letter-spacing:3px;color:#00e5ff;text-transform:uppercase;">
                  Verify your email
                </td></tr>
                <tr><td style="padding:20px 28px;font-family:'Courier New',monospace;font-size:14px;line-height:1.6;color:#eafcff;">
                  Operator {safeName}, confirm this address to receive task reminders.
                  The link is valid for 24 hours.
                </td></tr>
                <tr><td style="padding:4px 28px 20px;">
                  {Button(link, "Verify email")}
                </td></tr>
                <tr><td style="padding:0 28px 8px;font-family:'Courier New',monospace;font-size:11px;line-height:1.5;color:#0a8fa8;">
                  If you didn't create an account, you can ignore this email.
                </td></tr>
                """;

            var text =
                $"Operator {username},\n\n" +
                $"Confirm this address to receive task reminders (valid for 24 hours):\n{link}\n\n" +
                "If you didn't create an account, you can ignore this email.";

            return new EmailMessage(to, "Verify your email · Task Manager", Frame(rows, assetBaseUrl), text);
        }

        public static EmailMessage DueReminder(string to, string username, ReminderDigest digest, string appUrl)
        {
            var safeName = WebUtility.HtmlEncode(username);
            var headline = $"{digest.Total} {(digest.Total == 1 ? "task needs" : "tasks need")} attention";

            var rows = new StringBuilder($"""
                <tr><td style="padding:4px 28px 8px;font-family:'Courier New',monospace;font-size:11px;letter-spacing:2px;color:#0a8fa8;text-transform:uppercase;">
                  &#9679; Task Manager &middot; Daily Briefing
                </td></tr>
                <tr><td style="padding:0 28px;font-family:Arial,sans-serif;font-size:22px;letter-spacing:3px;color:#00e5ff;text-transform:uppercase;">
                  {headline}
                </td></tr>
                <tr><td style="padding:12px 28px 0;font-family:'Courier New',monospace;font-size:13px;color:#eafcff;">
                  Operator {safeName}, here's your status report.
                </td></tr>
                """);
            var text = new StringBuilder($"Operator {username}, here's what needs attention:\n");

            void Section(string title, string color, IReadOnlyList<DigestItem> items)
            {
                if (items.Count == 0) return;

                rows.Append($"""
                    <tr><td style="padding:18px 28px 6px;font-family:'Courier New',monospace;font-size:11px;letter-spacing:2px;color:{color};text-transform:uppercase;">
                      {WebUtility.HtmlEncode(title)} ({items.Count})
                    </td></tr>
                    """);
                text.Append($"\n{title.ToUpperInvariant()} ({items.Count})\n");

                foreach (var item in items)
                {
                    var date = item.DueDate.ToString("dd MMM", CultureInfo.InvariantCulture);
                    // Border sits on the <td>, not the table: Gmail drops borders on nested tables
                    rows.Append($"""
                        <tr><td style="padding:4px 28px;">
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                            <tr>
                              <td style="border-left:2px solid {color};padding:6px 10px;font-family:'Courier New',monospace;font-size:14px;color:#eafcff;">
                                {WebUtility.HtmlEncode(item.Title)}
                                <div style="font-size:11px;color:#0a8fa8;padding-top:2px;">{WebUtility.HtmlEncode(item.ProjectName)} &middot; {item.Priority}</div>
                              </td>
                              <td align="right" style="padding:6px 10px;font-family:'Courier New',monospace;font-size:12px;color:{color};white-space:nowrap;">{date}</td>
                            </tr>
                          </table>
                        </td></tr>
                        """);
                    text.Append($"- {item.Title} [{item.ProjectName}, {item.Priority}] due {date}\n");
                }
            }

            Section("Overdue", "#ffb020", digest.Overdue);
            Section("Due today", "#00e5ff", digest.DueToday);
            Section("Due tomorrow", "#39ff88", digest.DueTomorrow);

            rows.Append($"""
                <tr><td style="padding:24px 28px 8px;">
                  {Button(appUrl, "Open Task Manager")}
                </td></tr>
                <tr><td style="padding:12px 28px 8px;font-family:'Courier New',monospace;font-size:11px;line-height:1.5;color:#0a8fa8;">
                  You're receiving this because daily reminders are on. Turn them off on your Account page.
                </td></tr>
                """);

            text.Append($"\nOpen Task Manager: {appUrl}\nTurn off reminders on your Account page.");

            var subject = digest.Overdue.Count == digest.Total
                ? $"{digest.Total} overdue {(digest.Total == 1 ? "task" : "tasks")}"
                : digest.Overdue.Count > 0
                    ? $"{digest.Overdue.Count} overdue · {digest.Total} tasks need attention"
                    : $"{digest.Total} {(digest.Total == 1 ? "task" : "tasks")} due soon";

            return new EmailMessage(to, $"{subject} · Task Manager", Frame(rows.ToString(), appUrl), text.ToString());
        }
    }
}