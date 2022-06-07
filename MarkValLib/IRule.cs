using System.Collections.Generic;
using Markdig.Syntax;
using SystemWrapper.IO;

namespace MarkValLib
{
    public interface IRule
    {

        IEnumerable<MarkdownProblem> GetProblems(MarkdownObject obj, MarkdownDocument document, IFileInfoWrap file, ValidationContext context);
    }
}
