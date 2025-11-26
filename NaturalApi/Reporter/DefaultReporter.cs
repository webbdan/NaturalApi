using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NaturalApi.Reporter
{
    public class DefaultReporter : INaturalReporter
    {
        public void OnRequestSent(ApiRequestSpec request)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[yellow]API Request[/]");

            table.AddColumn("Field");
            table.AddColumn("Value");

            table.AddRow("Method", $"[green]{request.Method}[/]");
            table.AddRow("Url", $"[blue]{request.Endpoint}[/]");

            if (request.Headers?.Any() == true)
            {
                table.AddEmptyRow();
                table.AddRow("[bold]Headers[/]", "");
                foreach (var h in request.Headers)
                {
                    var masked = MaskIfSensitive(h.Key, Escape(h.Value));
                    table.AddRow($"• {h.Key}", masked);
                }
            }

            if (!string.IsNullOrEmpty(request.Body?.ToString()))
            {
                table.AddEmptyRow();
                table.AddRow("[bold]Body[/]", "");
                table.AddRow("", FormatJson(request.Body.ToString()));
            }

            AnsiConsole.Write(table);
        }

        public void OnResponseReceived(IApiResultContext response)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[yellow]API Response[/]");

            table.AddColumn("Field");
            table.AddColumn("Value");

            table.AddRow("Status",
                response.StatusCode >= 200 && response.StatusCode < 300
                    ? $"[green]{response.StatusCode}[/]"
                    : $"[red]{response.StatusCode}[/]"
            );

            table.AddRow("Duration", $"{response.Duration} ms");

            if (response.Headers?.Any() == true)
            {
                table.AddEmptyRow();
                table.AddRow("[bold]Headers[/]", "");
                foreach (var h in response.Headers)
                {
                    var masked = MaskIfSensitive(h.Key, Escape(h.Value));
                    table.AddRow($"• {h.Key}", masked);
                }
            }

            if (!string.IsNullOrWhiteSpace(response.RawBody))
            {
                table.AddEmptyRow();
                table.AddRow("[bold]Body[/]", "");
                table.AddRow("", FormatJson(response.RawBody));
            }

            AnsiConsole.Write(table);
        }

        private string MaskIfSensitive(string key, string value)
        {
            var lower = key.ToLowerInvariant();

            if (lower.Contains("authorization")
                || lower.Contains("auth")
                || lower.Contains("token")
                || lower.Contains("password")
                || lower.Contains("secret")
                || lower.Contains("apikey")
                || lower.Contains("api-key")
                || lower.Contains("bearer"))
            {
                return "***MASKED***";
            }

            return value;
        }

        public void OnAssertionPassed(string message, ApiResultContext response)
        {
            AnsiConsole.MarkupLine(
                $"[bold green]✔ Assertion passed[/]: {Escape(message)}"
            );
        }

        public void OnAssertionFailed(string message, ApiResultContext response)
        {
            AnsiConsole.MarkupLine(
                $"[bold red]✘ Assertion failed[/]: {Escape(message)}"
            );

            // Optional: print full response body for debugging
            if (!string.IsNullOrWhiteSpace(response.BodyAs<string>()))
            {
                AnsiConsole.Write(
                    new Panel(FormatJson(response.BodyAs<string>()))
                        .Header("Response Body")
                        .BorderColor(Spectre.Console.Color.Red)
                );
            }
        }

        private static string Escape(string text) =>
            Markup.Escape(text ?? "");

        private static string MaskJsonSensitiveFields(string json)
        {
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (dict == null) return json;

                foreach (var key in dict.Keys.ToList())
                {
                    if (key.ToLowerInvariant().Contains("password")
                        || key.ToLowerInvariant().Contains("token")
                        || key.ToLowerInvariant().Contains("secret"))
                    {
                        dict[key] = "***MASKED***";
                    }
                }

                return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return json; // "If it breaks, log it anyway but don't crash"
            }
        }

        private static string FormatJson(string raw)
        {
            raw = MaskJsonSensitiveFields(raw);

            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(raw);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var pretty = System.Text.Json.JsonSerializer.Serialize(doc, options);
                return $"[grey]{Markup.Escape(pretty)}[/]";
            }
            catch
            {
                // Not JSON. Return as plain text.
                return $"[grey]{Markup.Escape(raw)}[/]";
            }
        }
    }
}
