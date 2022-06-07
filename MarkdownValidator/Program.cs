using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarkValLib;
using MarkValLib.Rules;
using SystemWrapper.IO;

namespace MarkdownValidator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string repo = args[0];
            string file = null;
            if (args.Length > 1)
            {
                file = args[1];
            }
            else
            {
                file = "*.md";
            }

            new Program().FindProblems(repo, file);
        }
        
        public void FindProblems(string repo, string fileMatch)
        {
            IDirectoryInfoWrap directory = new DirectoryInfoWrap(repo);
            object lockObj = new object();
            Parallel.ForEach(directory.GetFiles(fileMatch, SearchOption.AllDirectories), file =>
            {
                LinkExistsRule rule = new LinkExistsRule();
                Validator v = new Validator(directory, new[] { rule });
                var problems = v.CheckDocument(file).ToList();
                if (problems.Count > 0)
                {
                    lock (lockObj)
                    {
                        foreach (var p in problems)
                        {
                            Console.WriteLine(p);
                        }
                    }
                }
            });
        }
    }
}
