using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markdig.Parsers;
using Markdig.Syntax;
using SystemWrapper.IO;

namespace MarkValLib
{
    public class ValidationContext
    {
        public readonly IDirectoryInfoWrap Directory;
        private readonly ConcurrentDictionary<string, MarkdownDocument> parsedMarkdowns;

        public ValidationContext(IDirectoryInfoWrap directory)
        {
            Directory = directory;
            parsedMarkdowns = new ConcurrentDictionary<string, MarkdownDocument>();
        }

        public MarkdownDocument GetDocument(IFileInfoWrap file)
        {
            if (!parsedMarkdowns.ContainsKey(file.FullName))
            {
                var stream = file.OpenText();
                using (stream.StreamReaderInstance)
                {
                    string text = stream.ReadToEnd();
                    parsedMarkdowns[file.FullName] = MarkdownParser.Parse(text);
                }
            }

            return parsedMarkdowns[file.FullName];
        }
    }
}
