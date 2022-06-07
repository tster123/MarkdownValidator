using System;
using System.Collections.Generic;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using SystemWrapper.IO;

namespace MarkValLib
{
    public class IOErrorRule : IRule
    {
        public IEnumerable<MarkdownProblem> GetProblems(MarkdownObject obj, MarkdownDocument document, IFileInfoWrap file, ValidationContext context)
        {
            return Array.Empty<MarkdownProblem>();
        }
    }

    public class Validator
    {
        private readonly ValidationContext context;

        private readonly IEnumerable<IRule> rules;

        public Validator(IDirectoryInfoWrap directory, IEnumerable<IRule> rules)
        {
            context = new ValidationContext(directory);
            this.rules = rules;
        }

        public IEnumerable<MarkdownProblem> CheckDocument(IFileInfoWrap file)
        {
            try
            {
                MarkdownDocument document = context.GetDocument(file);
                return TraverseDocument(document, file);
            }
            catch (Exception e)
            {
                return new[]
                {
                    new MarkdownProblem(new IOErrorRule(), null, file, "Unable to open file: " + e.Message)
                };
            }
        }

        private IEnumerable<MarkdownProblem> TraverseDocument(MarkdownDocument document, IFileInfoWrap file)
        {
            Queue<ContainerBlock> toProcess = new Queue<ContainerBlock>();
            toProcess.Enqueue(document);
            while (toProcess.Count > 0)
            {
                ContainerBlock container = toProcess.Dequeue();
                foreach (Block block in container)
                {
                    foreach (IRule rule in rules)
                    {
                        foreach (var p in rule.GetProblems(block, document, file, context)) yield return p;
                    }

                    if (block is ContainerBlock newContainer) toProcess.Enqueue(newContainer);
                    if (block is LeafBlock leaf)
                    {
                        if (leaf.Inline == null) continue;
                        foreach (Inline inline in leaf.Inline)
                        {
                            foreach (IRule rule in rules)
                            {
                                foreach (var p in rule.GetProblems(inline, document, file, context)) yield return p;
                            }
                        }
                    }
                }
            }
        }
    }
}