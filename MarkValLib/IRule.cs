using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markdig.Syntax;
using SystemWrapper.IO;

namespace MarkValLib
{
    public interface IRule
    {

        IEnumerable<MarkdownProblem> GetProblems(MarkdownObject obj, MarkdownDocument document, IFileInfoWrap file, IDirectoryInfoWrap repo);
    }
}
