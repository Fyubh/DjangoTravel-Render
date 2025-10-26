using System.Text.RegularExpressions;

namespace Jango_Travel.Utils
{
    public static class HtmlSafe
    {
        private static readonly Regex RemoveScriptStyle = new(@"</?(script|style|iframe|object|embed|form|input|button|textarea|link|meta)[^>]*>", RegexOptions.IgnoreCase|RegexOptions.Multiline);
        private static readonly Regex RemoveOnHandlers = new(@"\son\w+\s*=\s*(['""]).*?\1", RegexOptions.IgnoreCase|RegexOptions.Multiline);
        private static readonly Regex RemoveJsHrefSrc = new(@"\s(href|src)\s*=\s*(['""])\s*javascript:[^'""]*\2", RegexOptions.IgnoreCase|RegexOptions.Multiline);
        private static readonly Regex RemoveDataScript = new(@"\s(href|src)\s*=\s*(['""])\s*data:text/html[^'""]*\2", RegexOptions.IgnoreCase|RegexOptions.Multiline);
        private static readonly Regex StripAllTagsExceptWhitelist =
            new(@"</?(?!p\b|br\b|strong\b|em\b|h1\b|h2\b|h3\b|ul\b|ol\b|li\b|blockquote\b|img\b|a\b)[a-z0-9]+[^>]*>", RegexOptions.IgnoreCase|RegexOptions.Multiline);

        private static readonly Regex AllowOnlySafeImgAttrs = new(@"<img\b([^>]*)>", RegexOptions.IgnoreCase);
        private static readonly Regex AllowOnlySafeAAttrs   = new(@"<a\b([^>]*)>", RegexOptions.IgnoreCase);

        public static string Clean(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            html = RemoveScriptStyle.Replace(html, string.Empty);
            html = RemoveOnHandlers.Replace(html, string.Empty);
            html = RemoveJsHrefSrc.Replace(html, string.Empty);
            html = RemoveDataScript.Replace(html, string.Empty);
            html = StripAllTagsExceptWhitelist.Replace(html, string.Empty);

            html = AllowOnlySafeImgAttrs.Replace(html, m =>
            {
                var attrs = m.Groups[1].Value;
                var src  = Regex.Match(attrs, @"\ssrc\s*=\s*(['""])[^'""]+\1", RegexOptions.IgnoreCase).Value;
                var alt  = Regex.Match(attrs, @"\salt\s*=\s*(['""])[^'""]*\1", RegexOptions.IgnoreCase).Value;
                var title= Regex.Match(attrs, @"\stitle\s*=\s*(['""])[^'""]*\1", RegexOptions.IgnoreCase).Value;
                return $"<img{src}{alt}{title}>";
            });

            html = AllowOnlySafeAAttrs.Replace(html, m =>
            {
                var attrs = m.Groups[1].Value;
                var href  = Regex.Match(attrs, @"\shref\s*=\s*(['""])[^'""]+\1", RegexOptions.IgnoreCase).Value;
                var title = Regex.Match(attrs, @"\stitle\s*=\s*(['""])[^'""]*\1", RegexOptions.IgnoreCase).Value;
                return $"<a{href}{title} target=\"_blank\" rel=\"noopener noreferrer\">";
            });

            return html;
        }
    }
}
