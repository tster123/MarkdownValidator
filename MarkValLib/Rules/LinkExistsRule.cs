using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using SystemWrapper.IO;

namespace MarkValLib.Rules
{
    public class LinkExistsRule : IRule
    {
        public IEnumerable<MarkdownProblem> GetProblems(MarkdownObject obj, MarkdownDocument document, IFileInfoWrap file, IDirectoryInfoWrap repo)
        {
            if (obj is LinkReferenceDefinition reference)
            {
                MarkdownProblem problem = CheckLinkReference(reference, document, file, repo);
                if (problem != null) yield return problem;
            }

            if (obj is LinkInline link)
            {
                MarkdownProblem problem = CheckLinkInline(link, document, file, repo);
                if (problem != null) yield return problem;
            }
        }

        private MarkdownProblem CheckLinkInline(LinkInline link, MarkdownDocument document, IFileInfoWrap file, IDirectoryInfoWrap repo)
        {
            string url = link.Url;
            return GetLinkProblem(url, new LinkInlineWrapper(link), document, file, repo);
        }

        private MarkdownProblem GetLinkProblem(string url, ILinkWrapper link, MarkdownDocument document, IFileInfoWrap file, IDirectoryInfoWrap repo)
        {
            if (string.IsNullOrEmpty(url))
            {
                return new MarkdownProblem(this, link.MarkdownObject, file, $"Empty link on [{link.Label}]");
            }

            if (url.StartsWith("#"))
            {
                // TODO: support anchors in other pages
                return CheckAnchor(url, link, document, file);
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

            return CheckLocalLink(url, link, file, repo);
        }

        private MarkdownProblem CheckAnchor(string url, ILinkWrapper link, MarkdownDocument document, IFileInfoWrap file)
        {
            string anchor = url.Substring(1);
            anchor = WebUtility.UrlDecode(anchor);
            anchor = anchor.Replace("-", " ");
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
                            headingText += li.Content.Text;
                        }
                    }
                    headingText = headingText.Replace("-", " ");
                    if (headingText.ToLowerInvariant() == anchor.ToLowerInvariant())
                    {
                        return null;
                    }
                }
            }

            return new MarkdownProblem(this, link.MarkdownObject, file, $"Cannot find anchor in document [{url}]");
        }

        private static Semaphore sem = new Semaphore(8, 8);
        private MarkdownProblem CheckHttpLink(Uri url, ILinkWrapper link, IFileInfoWrap file)
        {
            sem.WaitOne();
            try
            {
                HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(20);
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

                return null;
            }
            finally
            {
                sem.Release();
            }
        }

        private MarkdownProblem CheckLocalLink(string url, ILinkWrapper link, IFileInfoWrap file, IDirectoryInfoWrap repo)
        {
            bool absolute = url[0] == '/';
            if (absolute) url = url.Substring(1);
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
            var candidateFile =
                workDir.GetFiles().SingleOrDefault(f =>
                    WebUtility.UrlDecode(f.Name).ToLowerInvariant() == filename.ToLowerInvariant() ||
                    WebUtility.UrlDecode(f.Name).ToLowerInvariant() == filename.ToLowerInvariant() + ".md");
            if (candidateFile != null) return null;

            return new MarkdownProblem(this, link.MarkdownObject, file, $"Cannot find file [{filename}] in [{workDirPath}]");
        }

        private MarkdownProblem CheckLinkReference(LinkReferenceDefinition block, MarkdownDocument document, IFileInfoWrap file, IDirectoryInfoWrap repo)
        {
            return GetLinkProblem(block.Url, new LinkReferenceWrapper(block), document, file, repo);
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
