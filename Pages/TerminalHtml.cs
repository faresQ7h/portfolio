using FaresPortfolio.Models;
using Microsoft.AspNetCore.Html;

namespace FaresPortfolio.Pages;

// Renders a terminal session as the inner HTML of a <pre>. Built in code rather than Razor markup
// because whitespace inside <pre> is significant; every piece of text is HTML-encoded.
public static class TerminalHtml
{
    public static IHtmlContent Render(TerminalSession session)
    {
        var html = new HtmlContentBuilder();

        for (var i = 0; i < session.Lines.Count; i++)
        {
            if (i > 0) html.AppendHtml("\n");
            var line = session.Lines[i];

            switch (line.Kind)
            {
                case TerminalLine.Command:
                    Span(html, "term-prompt", session.Prompt);
                    html.Append(" " + line.Text);
                    break;
                case TerminalLine.Error:
                    Span(html, "term-err", line.Text);
                    break;
                case TerminalLine.Input:
                    Span(html, "term-out", line.Text);
                    Span(html, "term-typed", line.Value);
                    break;
                case TerminalLine.Note:
                    Span(html, "term-note", line.Text);
                    break;
                default:
                    Span(html, "term-out", line.Text);
                    break;
            }
        }

        return html;
    }

    private static void Span(HtmlContentBuilder html, string cssClass, string text)
    {
        html.AppendHtml($"<span class=\"{cssClass}\">");
        html.Append(text);
        html.AppendHtml("</span>");
    }
}
