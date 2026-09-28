using Microsoft.AspNetCore.Html;

namespace FaresPortfolio.Pages;

public static class TextHtml
{
    // Encodes the text and keeps words containing a slash (e.g. "CI/CD-deployed") on one line,
    // so a line break can never split them.
    public static IHtmlContent KeepSlashedWordsTogether(string text)
    {
        var html = new HtmlContentBuilder();
        var words = text.Split(' ');

        for (var i = 0; i < words.Length; i++)
        {
            if (i > 0) html.Append(" ");

            if (words[i].Contains('/'))
            {
                html.AppendHtml("<span class=\"nowrap\">");
                html.Append(words[i]);
                html.AppendHtml("</span>");
            }
            else
            {
                html.Append(words[i]);
            }
        }

        return html;
    }
}
