using System.Net;

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
    }
}