using Markdig;
using System.IO;
using System.Text;

namespace MarkdownToPdf
{
    public static class MarkdownConverter
    {
        private static readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        public static string ConvertToHtml(string markdownContent)
        {
            if (string.IsNullOrWhiteSpace(markdownContent))
            {
                return string.Empty;
            }

            var htmlContent = Markdown.ToHtml(markdownContent, pipeline);
            return WrapInTemplate(htmlContent);
        }

        private static string WrapInTemplate(string htmlContent)
        {
            var template = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <script src=""file:///Assets/mermaid.min.js""></script>
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            mermaid.initialize({ 
                startOnLoad: true,
                theme: 'default',
                securityLevel: 'loose',
                fontFamily: 'Meiryo, Yu Gothic, sans-serif'
            });
            mermaid.run();
        });
    </script>
    <style>
        body { 
            font-family: 'Meiryo', 'Yu Gothic', sans-serif; 
            padding: 20px; 
            line-height: 1.6;
            color: #333;
        }
        h1, h2, h3, h4, h5, h6 {
            color: #2c3e50;
            margin-top: 1.5em;
            margin-bottom: 0.5em;
        }
        table {
            border-collapse: collapse;
            width: 100%;
            margin: 1em 0;
        }
        table th, table td {
            border: 1px solid #ddd;
            padding: 8px;
            text-align: left;
        }
        table th {
            background-color: #f2f2f2;
        }
        code {
            background-color: #f4f4f4;
            padding: 2px 4px;
            border-radius: 3px;
            font-family: 'Consolas', 'Monaco', monospace;
        }
        pre {
            background-color: #f4f4f4;
            padding: 10px;
            border-radius: 5px;
            overflow-x: auto;
        }
        blockquote {
            border-left: 4px solid #3498db;
            margin: 1em 0;
            padding-left: 1em;
            color: #666;
        }
    </style>
</head>
<body>
    {{BODY}}
</body>
</html>";
            return template.Replace("{{BODY}}", htmlContent);
        }

        public static bool ContainsMermaid(string markdownContent)
        {
            return markdownContent.Contains("```mermaid");
        }
    }
}