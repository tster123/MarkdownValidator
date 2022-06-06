using System.Collections;
using System.Collections.Generic;
using Markdig.Parsers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using SystemWrapper.IO;

namespace MarkValLib
{
    public class Validator
    {
        private readonly IDirectoryInfoWrap Repository;
        private readonly IEnumerable<IRule> Rules;

        public Validator(IDirectoryInfoWrap directory, IEnumerable<IRule> rules)
        {
            Repository = directory;
            Rules = rules;
        }

        public IEnumerable<MarkdownProblem> CheckDocument(IFileInfoWrap file)
        {
            string text;
            var stream = file.OpenText();
            using (stream.StreamReaderInstance)
            {
                text = stream.ReadToEnd();
            }

            MarkdownDocument document = MarkdownParser.Parse(text);
            Queue<ContainerBlock> toProcess = new Queue<ContainerBlock>();
            toProcess.Enqueue(document);
            while (toProcess.Count > 0)
            {
                ContainerBlock container = toProcess.Dequeue();
                foreach (Block block in container)
                {
                    foreach (IRule rule in Rules)
                    {
                        foreach (var p in rule.GetProblems(block, document, file, Repository)) yield return p;
                    }

                    if (block is ContainerBlock newContainer) toProcess.Enqueue(newContainer);
                    if (block is LeafBlock leaf)
                    {
                        if (leaf.Inline == null) continue;
                        foreach (Inline inline in leaf.Inline)
                        {
                            foreach (IRule rule in Rules)
                            {
                                foreach (var p in rule.GetProblems(inline, document, file, Repository)) yield return p;
                            }
                        }
                    }
                }
            }
        }
    }
}