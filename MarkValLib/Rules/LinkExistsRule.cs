using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkValLib;
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
            bool absolute = url[0] == '/';
            if (absolute) url = url.Substring(1);
            IDirectoryInfoWrap workDir = absolute ? repo : file.Directory;
            string[] parts = url.Split('/');
            string workDirPath = absolute ? "/" : "";
            for (int i = 0; i < parts.Length - 1; i++)
            {
                string part = parts[i];
                if (part == ".")
                {
                    // no-op on working dir
                }
                else if (part == "..")
                {
                    if (workDir.FullName.ToLowerInvariant() == repo.FullName.ToLowerInvariant())
                    {
                        return new MarkdownProblem(this, link, file, "link went above markdown base directory");
                    }

                    workDir = workDir.Parent;
                }
                else
                {
                    var candidate = workDir.GetDirectories().SingleOrDefault(d => d.Name.ToLowerInvariant() == part.ToLowerInvariant());
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

            string filename = parts.Last();
            var candidateFile = workDir.GetFiles().SingleOrDefault(f => f.Name.ToLowerInvariant() == filename.ToLowerInvariant());
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
