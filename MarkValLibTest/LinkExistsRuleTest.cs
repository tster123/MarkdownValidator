using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using MarkValLib;
using MarkValLib.Rules;
using SystemWrapper.IO;

namespace MarkValLibTest
{
    [TestClass]
    public class LinkExistsRuleTest
    {
        private string repo = @"D:\Repos\wiki";

        [TestMethod]
        public void TestBasicLinks()
        {
            IDirectoryInfoWrap directory = new DirectoryInfoWrap(repo);
            IFileInfoWrap file = directory.GetFiles("ziyiw-SPIron.md", SearchOption.AllDirectories).Single();
            LinkExistsRule rule = new LinkExistsRule();
            Validator v = new Validator(directory, new[] { rule });
            var problems = v.CheckDocument(file);
            foreach (var p in problems)
            {
                Console.WriteLine(p);
            }
        }

        [TestMethod]
        public void GetAllBroken()
        {
            IDirectoryInfoWrap directory = new DirectoryInfoWrap(repo);
            foreach (var file in directory.GetFiles("*.md", SearchOption.AllDirectories))
            {
                LinkExistsRule rule = new LinkExistsRule();
                Validator v = new Validator(directory, new[] { rule });
                var problems = v.CheckDocument(file);
                foreach (var p in problems)
                {
                    Console.WriteLine(p);
                }
            }
        }
    }
}
