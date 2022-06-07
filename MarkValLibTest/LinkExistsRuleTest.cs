using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        public void TestAnchors()
        {
            IDirectoryInfoWrap directory = new DirectoryInfoWrap(repo);
            IFileInfoWrap file = directory.GetFiles("PrepSP.md", SearchOption.AllDirectories).Single();
            LinkExistsRule rule = new LinkExistsRule();
            Validator v = new Validator(directory, new[] { rule });
            var problems = v.CheckDocument(file);
            foreach (var p in problems)
            {
                Console.WriteLine(p);
            }
        }

        [TestMethod]
        public void TestDecoding()
        {
            IDirectoryInfoWrap directory = new DirectoryInfoWrap(repo);
            IFileInfoWrap file = directory.GetFiles("Upgrading-.NET-Runtime-on-SPO-Servers.md", SearchOption.AllDirectories).Single();
            LinkExistsRule rule = new LinkExistsRule();
            Validator v = new Validator(directory, new[] { rule });
            var problems = v.CheckDocument(file);
            foreach (var p in problems)
            {
                Console.WriteLine(p);
            }
        }

        [TestMethod]
        public void TestWebsites()
        {
            IDirectoryInfoWrap directory = new DirectoryInfoWrap(repo);
            IFileInfoWrap file = directory.GetFiles("Security-training.md", SearchOption.AllDirectories).Single();
            LinkExistsRule rule = new LinkExistsRule();
            Validator v = new Validator(directory, new[] { rule });
            var problems = v.CheckDocument(file);
            foreach (var p in problems)
            {
                Console.WriteLine(p);
            }
        }

        [TestMethod]
        public void TestLinkMissingMdExtension()
        {
            IDirectoryInfoWrap directory = new DirectoryInfoWrap(repo);
            IFileInfoWrap file = directory.GetFiles("Onboarding.md", SearchOption.TopDirectoryOnly).Single();
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
            object lockObj = new object();
            Parallel.ForEach(directory.GetFiles("*.md", SearchOption.AllDirectories), file =>
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

        [TestMethod]
        public void GetAllBroken2()
        {
            IDirectoryInfoWrap directory = new DirectoryInfoWrap(@"D:\repos\dr-oce-wiki");
            object lockObj = new object();
            Parallel.ForEach(directory.GetFiles("*.md", SearchOption.AllDirectories), file =>
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
