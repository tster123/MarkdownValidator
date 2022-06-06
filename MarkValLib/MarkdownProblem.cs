using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using SystemWrapper.IO;

namespace MarkValLib
{
    public class MarkdownProblem
    {
        public readonly IRule Rule;
        public readonly MarkdownObject Object;
        public readonly IFileInfoWrap File;
        public readonly string Description;

        public MarkdownProblem(IRule rule, MarkdownObject obj, IFileInfoWrap file, string description)
        {
            Rule = rule;
            Object = obj;
            File = file;
            Description = description;
        }

        public override string ToString()
        {
            return $"{Rule.GetType().Name}: {File.FullName} @ Line {Line} : {Description}";
        }

        private int Line
        {
            get
            {
                var o = Object;
                while (o != null && o.Line == 0)
                {
                    if (o is ContainerInline ci) o = (MarkdownObject)ci.ParentBlock ?? ci.Parent;
                    else if (o is Inline i) o = i.Parent;
                    else if (o is IBlock b) o = b.Parent;
                    else return -1;
                }

                return (o?.Line ?? -2) + 1;
            }
        }
    }
}
