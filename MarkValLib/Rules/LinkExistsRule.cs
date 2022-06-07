using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using HtmlAgilityPack;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using SystemWrapper.IO;

namespace MarkValLib.Rules
{
    public class LinkExistsRule : IRule
    {
        public IEnumerable<MarkdownProblem> GetProblems(MarkdownObject obj, MarkdownDocument document, IFileInfoWrap file, ValidationContext context)
        {
            if (obj is LinkReferenceDefinition reference)
            {
                MarkdownProblem problem = CheckLinkReference(reference, document, file, context);
                if (problem != null) yield return problem;
            }

            if (obj is LinkInline link)
            {
                MarkdownProblem problem = CheckLinkInline(link, document, file, context);
                if (problem != null) yield return problem;
            }
        }

        private MarkdownProblem CheckLinkInline(LinkInline link, MarkdownDocument document, IFileInfoWrap file, ValidationContext context)
        {
            string url = link.Url;
            return GetLinkProblem(url, new LinkInlineWrapper(link), document, file, context);
        }

        private MarkdownProblem GetLinkProblem(string url, ILinkWrapper link, MarkdownDocument document, IFileInfoWrap file, ValidationContext context)
        {
            if (string.IsNullOrEmpty(url))
            {
                return new MarkdownProblem(this, link.MarkdownObject, file, $"Empty link on [{link.Label}]");
            }

            if (url.StartsWith("#"))
            {
                return CheckAnchor(url, link, document, file, context);
            }

            if (url.StartsWith("mailto:")) return null;

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                {
                    return CheckHttpLink(uri, link, file);
                }

                if (uri.Scheme == Uri.UriSchemeMailto)
                {
                    return null;
                }
            }

            return CheckLocalLink(url, link, file, context);
        }

        private string NormalizeAnchor(string anchor)
        {
            if (string.IsNullOrEmpty(anchor)) return "";
            if (anchor.StartsWith("#")) anchor = anchor.Substring(1);
            return anchor.Replace(" ", "-").ToLowerInvariant();
        }

        private MarkdownProblem CheckAnchor(string url,
            ILinkWrapper link,
            MarkdownDocument document,
            IFileInfoWrap file,
            ValidationContext context,
            IFileInfoWrap foreignFile = null)
        {
            string anchor = url.Substring(1);
            anchor = WebUtility.UrlDecode(anchor);
            anchor = NormalizeAnchor(anchor);

            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(document.ToHtml());
            foreach (var node in html.DocumentNode.SelectNodes("//a"))
            {
                if (NormalizeAnchor(node.Name) == anchor)
                {
                    return null;
                }
            }
            foreach (var node in html.DocumentNode.Descendants())
            {
                if (NormalizeAnchor(node.Id) == anchor)
                {
                    return null;
                }
            }


            //TODO: do I need to do a tree search?
            foreach (Block block in document)
            {
                if (block is HeadingBlock heading)
                {
                    string headingText = "";
                    if (heading.Inline == null) continue;
                    foreach (Inline i in heading.Inline)
                    {
                        if (i is LiteralInline li)
                        {
                            headingText += li.ToString();
                        }
                    }

                    headingText = NormalizeAnchor(headingText);
                    if (headingText == anchor)
                    {
                        return null;
                    }
                }
            }

            string message = $"Cannot find anchor in document [{url}]";
            if (foreignFile != null)
            {
                message = $"Cannot find anchor [{url}] on page [{context.GetRepoPath(foreignFile)}]";
            }
            return new MarkdownProblem(this, link.MarkdownObject, file, message);
        }

        private static int MAX_CONNECTIONS = 50;
        private static Semaphore sem = new Semaphore(MAX_CONNECTIONS, MAX_CONNECTIONS);
        private static HttpClient client;

        static LinkExistsRule()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.MaxConnectionsPerServer = MAX_CONNECTIONS;
            client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(20);
        }

        private MarkdownProblem CheckHttpLink(Uri url, ILinkWrapper link, IFileInfoWrap file)
        {
            sem.WaitOne();
            try
            {
                HttpResponseMessage response = client.GetAsync(url).Result;
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new MarkdownProblem(this, link.MarkdownObject, file, $"Got 404 from [{url}]");
                }
            }
            catch (Exception e)
            {
                while (e.InnerException != null) e = e.InnerException;
                return new MarkdownProblem(this, link.MarkdownObject, file, $"Error getting [{url}]: {e.Message}");
            }
            finally
            {
                sem.Release();
            }
            return null;
        }

        private MarkdownProblem CheckLocalLink(string url, ILinkWrapper link, IFileInfoWrap file, ValidationContext context)
        {
            bool absolute = url[0] == '/';
            if (absolute) url = url.Substring(1);
            IDirectoryInfoWrap repo = context.Directory;
            IDirectoryInfoWrap workDir = absolute ? repo : file.Directory;
            string[] parts = url.Split('/');
            string workDirPath = absolute ? "/" : "";
            for (int i = 0; i < parts.Length - 1; i++)
            {
                string part = parts[i];
                part = WebUtility.UrlDecode(part);
                if (part == ".")
                {
                    // no-op on working dir
                }
                else if (part == "..")
                {
                    if (workDir.FullName.ToLowerInvariant() == repo.FullName.ToLowerInvariant())
                    {
                        return new MarkdownProblem(this, link.MarkdownObject, file,
                            $"link for [{link.Label}] went above markdown base directory");
                    }

                    workDir = workDir.Parent;
                }
                else
                {
                    var candidate = workDir.GetDirectories()
                        .SingleOrDefault(d => WebUtility.UrlDecode(d.Name).ToLowerInvariant() == part.ToLowerInvariant());
                    if (candidate == null)
                    {
                        return new MarkdownProblem(this, link.MarkdownObject, file,
                            $"Cannot find dir [{part}] in [{workDirPath}]");
                    }

                    workDir = candidate;
                }

                if (workDirPath != "" && workDirPath != "/") workDirPath += "/";
                workDirPath += part;
            }

            string filename = WebUtility.UrlDecode(parts.Last());
            string[] filenameParts = filename.Split('#');
            if (filenameParts.Length > 2)
                return new MarkdownProblem(this, link.MarkdownObject, file, $"Ill-formed anchor specified in [{url}]");
            filename = filenameParts[0];

            IFileInfoWrap candidateFile =
                workDir.GetFiles().SingleOrDefault(f =>
                    WebUtility.UrlDecode(f.Name).ToLowerInvariant() == filename.ToLowerInvariant() ||
                    WebUtility.UrlDecode(f.Name).ToLowerInvariant() == filename.ToLowerInvariant() + ".md");
            if (candidateFile == null)
            {
                return new MarkdownProblem(this, link.MarkdownObject, file,
                    $"Cannot find file [{filename}] in [{workDirPath}]");
            }

            if (filenameParts.Length > 1)
            {
                string fAnchor = "#" + filenameParts[1];
                return CheckAnchor(fAnchor, link, context.GetDocument(candidateFile), file, context, candidateFile);
            }

            return null;

        }

        private MarkdownProblem CheckLinkReference(LinkReferenceDefinition block, MarkdownDocument document, IFileInfoWrap file, ValidationContext context)
        {
            return GetLinkProblem(block.Url, new LinkReferenceWrapper(block), document, file, context);
        }

        internal interface ILinkWrapper
        {
            string Url { get; }
            string Label { get; }
            MarkdownObject MarkdownObject { get; }
        }

        internal class LinkInlineWrapper : ILinkWrapper
        {
            private readonly LinkInline link;

            public LinkInlineWrapper(LinkInline link)
            {
                this.link = link;
            }

            public string Url => link.Url;
            public string Label => link.Label;
            public MarkdownObject MarkdownObject => link;
        }

        internal class LinkReferenceWrapper : ILinkWrapper
        {
            private readonly LinkReferenceDefinition link;

            public LinkReferenceWrapper(LinkReferenceDefinition link)
            {
                this.link = link;
            }

            public string Url => link.Url;
            public string Label => link.Label;
            public MarkdownObject MarkdownObject => link;
        }
    }
}
