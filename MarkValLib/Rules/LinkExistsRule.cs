using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using SystemWrapper.IO;

namespace MarkValLib.Rules
{
    public class LinkExistsRule : IRule
    {
        public IEnumerable<MarkdownProblem> GetProblems(MarkdownObject obj, MarkdownDocument document, IFileInfoWrap file, IDirectoryInfoWrap repo)
        {
            if (obj is LinkReferenceDefinition)
            {
                MarkdownProblem problem = CheckLink((LinkReferenceDefinition)obj, document, file, repo);
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
            if (url == null) return new MarkdownProblem(this, link, file, "null URL");
            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                Uri uri = new Uri(url);
                return null;
            }
            return GetLinkProblem(url, link, document, file, repo);
        }

        private MarkdownProblem GetLinkProblem(string url, LinkInline link, MarkdownDocument document, IFileInfoWrap file, IDirectoryInfoWrap repo)
        {
            if (url == "")
            {
                return new MarkdownProblem(this, link, file, $"Empty link on [{link.Label}]");
            }

            if (url.StartsWith("#"))
            {
                // TODO: support anchors in other pages
                return CheckAnchor(url, link, document, file, repo);
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                {
                    return CheckHttpLink(uri, link, file, repo);
                }

                if (uri.Scheme == Uri.UriSchemeMailto)
                {
                    return null;
                }
            }

            return CheckLocalLink(url, link, file, repo);
        }

        private MarkdownProblem CheckAnchor(string url, LinkInline link, MarkdownDocument document, IFileInfoWrap file, IDirectoryInfoWrap repo)
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
                            headingText += i.ToString();
                        }
                    }
                    headingText = headingText.Replace("-", " ");
                    if (headingText.ToLowerInvariant() == anchor.ToLowerInvariant())
                    {
                        return null;
                    }
                }
            }

            return new MarkdownProblem(this, link, file, $"Cannot find anchor in document [{url}]");
        }

        private MarkdownProblem CheckHttpLink(Uri url, LinkInline link, IFileInfoWrap file, IDirectoryInfoWrap repo)
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            try
            {
                HttpResponseMessage response = client.GetAsync(url).Result;
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new MarkdownProblem(this, link, file, $"Got 404 from [{url}]");
                }
            }
            catch (Exception e)
            {
                return new MarkdownProblem(this, link, file, $"Error getting [{url}]: {e.Message}");
            }
            return null;
        }

        private MarkdownProblem CheckLocalLink(string url, LinkInline link, IFileInfoWrap file, IDirectoryInfoWrap repo)
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
                        return new MarkdownProblem(this, link, file,
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
                        return new MarkdownProblem(this, link, file,
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
                    WebUtility.UrlDecode(f.Name).ToLowerInvariant() == filename.ToLowerInvariant());
            if (candidateFile != null) return null;

            var candidateDir = workDir.GetDirectories()
                .SingleOrDefault(d => d.Name.ToLowerInvariant() == filename.ToLowerInvariant());
            if (candidateDir != null)
            {
                return null;
            }

            return new MarkdownProblem(this, link, file, $"Cannot find file [{filename}] in [{workDirPath}]");
        }

        private MarkdownProblem CheckLink(LinkReferenceDefinition block, MarkdownDocument document, IFileInfoWrap file, IDirectoryInfoWrap repo)
        {
            return new MarkdownProblem(this, block, file, "Don't understand LinkReferenceDefinition");
        }
    }
}
