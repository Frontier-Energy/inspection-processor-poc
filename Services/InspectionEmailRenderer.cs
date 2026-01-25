using System.Net;
using System.Text;

namespace InspectionProcessor.Services;

public sealed class InspectionEmailRenderer
{
    public string RenderHtml(GetInspectionResponse inspection)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html>");
        builder.AppendLine("<body style=\"font-family: Arial, sans-serif; color: #1b1b1b;\">");
        builder.AppendLine("<h2 style=\"margin: 0 0 12px 0;\">Inspection Details</h2>");
        builder.AppendLine("<table style=\"border-collapse: collapse; width: 100%;\">");

        AppendRow(builder, "Session Id", inspection.SessionId);
        AppendRow(builder, "User Id", inspection.UserId);
        AppendRow(builder, "Name", inspection.Name);
        AppendQueryParams(builder, inspection.QueryParams);
        AppendFiles(builder, inspection.Files);

        builder.AppendLine("</table>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, string label, string? value)
    {
        builder.AppendLine("<tr>");
        builder.AppendLine($"<td style=\"padding: 6px 10px; border: 1px solid #ddd; font-weight: bold; width: 160px;\">{HtmlEncode(label)}</td>");
        builder.AppendLine($"<td style=\"padding: 6px 10px; border: 1px solid #ddd;\">{HtmlEncode(value)}</td>");
        builder.AppendLine("</tr>");
    }

    private static void AppendHtmlRow(StringBuilder builder, string label, string htmlValue)
    {
        builder.AppendLine("<tr>");
        builder.AppendLine($"<td style=\"padding: 6px 10px; border: 1px solid #ddd; font-weight: bold; width: 160px;\">{HtmlEncode(label)}</td>");
        builder.AppendLine($"<td style=\"padding: 6px 10px; border: 1px solid #ddd;\">{htmlValue}</td>");
        builder.AppendLine("</tr>");
    }

    private static void AppendQueryParams(StringBuilder builder, Dictionary<string, string>? queryParams)
    {
        if (queryParams is null || queryParams.Count == 0)
        {
            AppendRow(builder, "Query Params", null);
            return;
        }

        var inner = new StringBuilder();
        inner.AppendLine("<table style=\"border-collapse: collapse; width: 100%;\">");
        foreach (var pair in queryParams)
        {
            inner.AppendLine("<tr>");
            inner.AppendLine($"<td style=\"padding: 4px 8px; border: 1px solid #eee; font-weight: bold;\">{HtmlEncode(pair.Key)}</td>");
            inner.AppendLine($"<td style=\"padding: 4px 8px; border: 1px solid #eee;\">{HtmlEncode(pair.Value)}</td>");
            inner.AppendLine("</tr>");
        }
        inner.AppendLine("</table>");

        AppendHtmlRow(builder, "Query Params", inner.ToString());
    }

    private static void AppendFiles(StringBuilder builder, List<InspectionFileReference>? files)
    {
        if (files is null || files.Count == 0)
        {
            AppendRow(builder, "Files", null);
            return;
        }

        var inner = new StringBuilder();
        inner.AppendLine("<table style=\"border-collapse: collapse; width: 100%;\">");
        foreach (var file in files)
        {
            inner.AppendLine("<tr>");
            inner.AppendLine($"<td style=\"padding: 4px 8px; border: 1px solid #eee;\">{HtmlEncode(file.FileName)}</td>");
            inner.AppendLine($"<td style=\"padding: 4px 8px; border: 1px solid #eee;\">{HtmlEncode(file.FileType)}</td>");
            inner.AppendLine("</tr>");
        }
        inner.AppendLine("</table>");

        AppendHtmlRow(builder, "Files", inner.ToString());
    }

    private static string HtmlEncode(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "None" : WebUtility.HtmlEncode(value);
    }
}
