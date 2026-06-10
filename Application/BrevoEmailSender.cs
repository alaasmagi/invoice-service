using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json.Serialization;
using Contracts.Application;

namespace Application;

public sealed class BrevoEmailSender(HttpClient httpClient, BrevoEmailOptions options) : IEmailSender
{
    public async Task SendMonthlyStatementEmailAsync(MonthlyStatementEmail email)
    {
        var request = new BrevoTransactionalEmailRequest(
            new BrevoSender(options.SenderEmail, options.SenderName),
            [new BrevoRecipient(email.ToEmail, email.ContactName)],
            $"Kuu koondarve {email.Period}",
            BuildTextContent(email),
            BuildHtmlContent(email));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "smtp/email")
        {
            Content = JsonContent.Create(request, mediaType: new MediaTypeHeaderValue(MediaTypeNames.Application.Json))
        };
        httpRequest.Headers.Add("api-key", options.ApiKey);

        using var response = await httpClient.SendAsync(httpRequest);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"Brevo email send failed with HTTP {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {body}");
    }

    private string BuildTextContent(MonthlyStatementEmail email)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Tere, {email.ContactName}!");
        builder.AppendLine();
        builder.AppendLine($"Sinu {email.Period} kuu koondarve summa on {Money(email.TotalAmount)}.");
        builder.AppendLine();
        builder.AppendLine("Arve read:");

        foreach (var line in email.Lines)
        {
            builder.AppendLine($"- {line.AddressName} / {line.ServiceName} / {line.InvoiceDate:yyyy-MM-dd}: Sinu osa {Money(line.ContactAmount)} arvest {Money(line.InvoiceTotal)}, jagatud {line.ResidentCount} inimese vahel");
        }

        builder.AppendLine();
        builder.AppendLine("Makse andmed:");
        builder.AppendLine($"Saaja: {email.SenderBankAccountName}");
        builder.AppendLine($"IBAN: {email.SenderBankIban}");
        builder.AppendLine($"Selgitus: {email.ContactName} {email.Period}");
        builder.AppendLine();
        builder.AppendLine("Arve");
        return builder.ToString();
    }

    private string BuildHtmlContent(MonthlyStatementEmail email)
    {
        var rows = email.Lines.Count == 0
            ? "<tr><td colspan=\"6\" style=\"padding:18px;color:#64748b;text-align:center;\">Selle koondarve jaoks arveridu ei leitud.</td></tr>"
            : string.Concat(email.Lines.Select(line => $"""
                <tr>
                  <td style="padding:14px 16px;border-bottom:1px solid #e2e8f0;color:#0f172a;font-weight:700;">{Html(line.AddressName)}</td>
                  <td style="padding:14px 16px;border-bottom:1px solid #e2e8f0;color:#475569;">{Html(line.ServiceName)}</td>
                  <td style="padding:14px 16px;border-bottom:1px solid #e2e8f0;color:#475569;">{line.InvoiceDate:dd MMM yyyy}<br><span style="font-size:12px;color:#94a3b8;">{PeriodText(line)}</span></td>
                  <td style="padding:14px 16px;border-bottom:1px solid #e2e8f0;color:#475569;text-align:right;">{Html(Money(line.InvoiceTotal))}</td>
                  <td style="padding:14px 16px;border-bottom:1px solid #e2e8f0;color:#475569;text-align:center;">{line.ResidentCount}</td>
                  <td style="padding:14px 16px;border-bottom:1px solid #e2e8f0;color:#0f172a;text-align:right;font-weight:800;">{Html(Money(line.ContactAmount))}</td>
                </tr>
                """));

        return $$"""
        <!doctype html>
        <html lang="et">
        <body style="margin:0;padding:0;background:#f2efe7;font-family:Georgia,'Times New Roman',serif;color:#172033;">
          <div style="display:none;max-height:0;overflow:hidden;opacity:0;">Sinu {{Html(email.Period)}} kuu koondarve summa on {{Html(Money(email.TotalAmount))}}.</div>
          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f2efe7;padding:32px 12px;">
            <tr>
              <td align="center">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:720px;background:#fffaf0;border-radius:28px;overflow:hidden;box-shadow:0 24px 70px rgba(38,32,21,.18);">
                  <tr>
                    <td style="background:#263a2f;padding:34px 36px 30px;color:#fffaf0;">
                      <div style="font-size:13px;letter-spacing:.18em;text-transform:uppercase;color:#d8c7a2;font-family:Arial,sans-serif;">Kuu koondarve</div>
                      <h1 style="margin:12px 0 0;font-size:38px;line-height:1.05;font-weight:400;">{{Html(email.Period)}}</h1>
                      <p style="margin:14px 0 0;color:#dbe7d7;font-size:17px;line-height:1.5;">Tere, {{Html(email.ContactName)}}! Siin on Sinu kuu koondarve.</p>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:0 36px;">
                      <div style="margin-top:-22px;background:#c86b35;border-radius:22px;padding:26px 28px;color:#fff7ed;box-shadow:0 14px 30px rgba(200,107,53,.28);">
                        <div style="font-size:13px;letter-spacing:.16em;text-transform:uppercase;font-family:Arial,sans-serif;opacity:.86;">Tasumisele kuulub</div>
                        <div style="font-size:48px;line-height:1;margin-top:8px;font-weight:800;font-family:Arial,sans-serif;">{{Html(Money(email.TotalAmount))}}</div>
                      </div>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:34px 36px 12px;">
                      <p style="margin:0;color:#475569;font-size:16px;line-height:1.65;">See kiri koondab kõik {{Html(email.Period)}} kuu arved ja Sinu osa nendest ühte ülevaatesse.</p>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:12px 36px 36px;">
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:separate;border-spacing:0;background:#ffffff;border:1px solid #e2e8f0;border-radius:18px;overflow:hidden;font-family:Arial,sans-serif;">
                        <thead>
                          <tr style="background:#f8f1df;">
                            <th align="left" style="padding:14px 16px;color:#334155;font-size:12px;text-transform:uppercase;letter-spacing:.08em;">Aadress</th>
                            <th align="left" style="padding:14px 16px;color:#334155;font-size:12px;text-transform:uppercase;letter-spacing:.08em;">Teenus</th>
                            <th align="left" style="padding:14px 16px;color:#334155;font-size:12px;text-transform:uppercase;letter-spacing:.08em;">Arve</th>
                            <th align="right" style="padding:14px 16px;color:#334155;font-size:12px;text-transform:uppercase;letter-spacing:.08em;">Arve summa</th>
                            <th align="center" style="padding:14px 16px;color:#334155;font-size:12px;text-transform:uppercase;letter-spacing:.08em;">Jagatud</th>
                            <th align="right" style="padding:14px 16px;color:#334155;font-size:12px;text-transform:uppercase;letter-spacing:.08em;">Sinu osa</th>
                          </tr>
                        </thead>
                        <tbody>
                          {{rows}}
                        </tbody>
                      </table>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:0 36px 36px;">
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#263a2f;border-radius:20px;color:#fffaf0;font-family:Arial,sans-serif;">
                        <tr>
                          <td style="padding:24px 26px;">
                            <div style="font-size:12px;letter-spacing:.14em;text-transform:uppercase;color:#d8c7a2;">Makse andmed</div>
                            <div style="margin-top:14px;font-size:15px;line-height:1.8;">
                              <div><strong>Saaja:</strong> {{Html(email.SenderBankAccountName)}}</div>
                              <div><strong>IBAN:</strong> {{Html(email.SenderBankIban)}}</div>
                              <div><strong>Selgitus:</strong> {{Html(email.ContactName)}} {{Html(email.Period)}}</div>
                            </div>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:0 36px 38px;">
                      <div style="border-top:1px solid #e6dcc6;padding-top:22px;color:#64748b;font-size:13px;line-height:1.6;font-family:Arial,sans-serif;">
                        Saadetud alaasmagi arveteenuse kaudu. Kui midagi tundub vale, võta ühendust haldajaga aadressil <a href="mailto:aleksander.laasmagi@gmail.com">aleksander.laasmagi@gmail.com</a>.
                      </div>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
    }

    private static string PeriodText(MonthlyStatementEmailLine line)
    {
        return line.PeriodStart.HasValue && line.PeriodEnd.HasValue
            ? $"{line.PeriodStart.Value:dd MMM yyyy} - {line.PeriodEnd.Value:dd MMM yyyy}"
            : "Periood puudub";
    }

    private static string Money(decimal amount)
    {
        return amount.ToString("C", CultureInfo.GetCultureInfo("et-EE"));
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private sealed record BrevoTransactionalEmailRequest(
        [property: JsonPropertyName("sender")] BrevoSender Sender,
        [property: JsonPropertyName("to")] IReadOnlyCollection<BrevoRecipient> To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("textContent")] string TextContent,
        [property: JsonPropertyName("htmlContent")] string HtmlContent);

    private sealed record BrevoSender(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("name")] string Name);

    private sealed record BrevoRecipient(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("name")] string Name);
}
