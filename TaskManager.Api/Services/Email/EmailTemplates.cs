using System.Globalization;
using System.Net;
using System.Text;

namespace TaskManager.Api.Services.Email
{
    public static class EmailTemplates
    {
        public static EmailMessage Verification(string to, string username, string link)
        {
            var safeName = WebUtility.HtmlEncode(username);
            var safeLink = WebUtility.HtmlEncode(link);

            var html = $"""
                <!DOCTYPE html>
                <html>
                <body style="margin:0;padding:0;background:#030b0f;">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#030b0f;padding:32px 16px;">
                    <tr><td align="center">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;background:#0a1620;border:1px solid #00e5ff;">
                        <tr><td style="padding:28px 28px 8px;font-family:'Courier New',monospace;font-size:11px;letter-spacing:2px;color:#0a8fa8;text-transform:uppercase;">
                          &#9679; Task Manager &middot; Identity Check
                        </td></tr>
                        <tr><td style="padding:0 28px;font-family:Arial,sans-serif;font-size:22px;letter-spacing:3px;color:#00e5ff;text-transform:uppercase;">
                          Verify your email
                        </td></tr>
                        <tr><td style="padding:20px 28px;font-family:'Courier New',monospace;font-size:14px;line-height:1.6;color:#eafcff;">
                          Operator {safeName}, confirm this address to receive task reminders.
                          The link is valid for 24 hours.
                        </td></tr>
                        <tr><td style="padding:4px 28px 28px;">
                          <a href="{safeLink}" style="display:inline-block;padding:12px 24px;border:1px solid #00e5ff;background:#0f202e;color:#00e5ff;font-family:Arial,sans-serif;font-size:13px;letter-spacing:2px;text-transform:uppercase;text-decoration:none;">
                            Verify email
                          </a>
                        </td></tr>
                        <tr><td style="padding:0 28px 28px;font-family:'Courier New',monospace;font-size:11px;line-height:1.5;color:#0a8fa8;">
                          If you didn't create an account, you can ignore this email.
                        </td></tr>
                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """;

            var text =
                $"Operator {username},\n\n" +
                $"Confirm this address to receive task reminders (valid for 24 hours):\n{link}\n\n" +
                "If you didn't create an account, you can ignore this email.";

            return new EmailMessage(to, "Verify your email · Task Manager", html, text);
        }

        public static EmailMessage DueReminder(string to, string username, ReminderDigest digest, string appUrl)
        {
            var safeName = WebUtility.HtmlEncode(username);
            var safeUrl = WebUtility.HtmlEncode(appUrl);

            var sections = new StringBuilder();
            var text = new StringBuilder($"Operator {username}, here's what needs attention:\n");

            void Section(string title, string color, IReadOnlyList<DigestItem> items)
            {
                if (items.Count == 0) return;

                sections.Append($"""
                    <tr><td style="padding:18px 28px 6px;font-family:'Courier New',monospace;font-size:11px;letter-spacing:2px;color:{color};text-transform:uppercase;">
                      {WebUtility.HtmlEncode(title)} ({items.Count})
                    </td></tr>
                    """);
                text.Append($"\n{title.ToUpperInvariant()} ({items.Count})\n");

                foreach (var item in items)
                {
                    var date = item.DueDate.ToString("dd MMM", CultureInfo.InvariantCulture);
                    sections.Append($"""
                        <tr><td style="padding:4px 28px;">
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-left:2px solid {color};">
                            <tr>
                              <td style="padding:6px 10px;font-family:'Courier New',monospace;font-size:14px;color:#eafcff;">
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

            text.Append($"\nOpen Task Manager: {appUrl}\nTurn off reminders on your Account page.");

            var headline = $"{digest.Total} {(digest.Total == 1 ? "task needs" : "tasks need")} attention";

            var html = $"""
                <!DOCTYPE html>
                <html>
                <body style="margin:0;padding:0;background:#030b0f;">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#030b0f;padding:32px 16px;">
                    <tr><td align="center">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;background:#0a1620;border:1px solid #00e5ff;">
                        <tr><td style="padding:28px 28px 8px;font-family:'Courier New',monospace;font-size:11px;letter-spacing:2px;color:#0a8fa8;text-transform:uppercase;">
                          &#9679; Task Manager &middot; Daily Briefing
                        </td></tr>
                        <tr><td style="padding:0 28px;font-family:Arial,sans-serif;font-size:22px;letter-spacing:3px;color:#00e5ff;text-transform:uppercase;">
                          {headline}
                        </td></tr>
                        <tr><td style="padding:12px 28px 0;font-family:'Courier New',monospace;font-size:13px;color:#eafcff;">
                          Operator {safeName}, here's your status report.
                        </td></tr>
                        {sections}
                        <tr><td style="padding:24px 28px 8px;">
                          <a href="{safeUrl}" style="display:inline-block;padding:12px 24px;border:1px solid #00e5ff;background:#0f202e;color:#00e5ff;font-family:Arial,sans-serif;font-size:13px;letter-spacing:2px;text-transform:uppercase;text-decoration:none;">
                            Open Task Manager
                          </a>
                        </td></tr>
                        <tr><td style="padding:12px 28px 28px;font-family:'Courier New',monospace;font-size:11px;line-height:1.5;color:#0a8fa8;">
                          You're receiving this because daily reminders are on. Turn them off on your Account page.
                        </td></tr>
                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """;

            var subject = digest.Overdue.Count > 0
                ? $"{digest.Overdue.Count} overdue · {digest.Total} tasks need attention"
                : $"{digest.Total} {(digest.Total == 1 ? "task" : "tasks")} due soon";

            return new EmailMessage(to, $"{subject} · Task Manager", html, text.ToString());
        }
    }
}