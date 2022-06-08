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
        private string drOce = @"D:\repos\dr-oce-wiki\DrOce\TSGs";

        [TestMethod]
        public void TestBasicLinks()
        {
            TestSingleFile(repo, "ziyiw-SPIron.md");
        }

        [TestMethod]
        public void TestAnchors()
        {
            //TestSingleFile(repo, "PrepSP.md");
            TestSingleFile(repo, "Build-Accessor.md");
        }

        [TestMethod]
        public void TestHtmlAnchors()
        {
            TestSingleFile(drOce, "AutoClumpsFailoverDRAlerts.md");
            TestSingleFile(drOce, "FailoverRollback.md");
        }
        
        [TestMethod]
        public void TestForeignAnchors()
        {
            TestSingleFile(repo, "6-Writing-Your-First-Recurring-Job.md");
        }


        [TestMethod]
        public void TestDecoding()
        {
            TestSingleFile(repo, "Upgrading-.NET-Runtime-on-SPO-Servers.md");
        }

        [TestMethod]
        public void TestWebsites()
        {
            TestSingleFile(repo, "Security-training.md");
        }

        [TestMethod]
        public void TestLinkMissingMdExtension()
        {
            TestSingleFile(repo, "Onboarding.md", SearchOption.TopDirectoryOnly);
        }

        private void TestSingleFile(string repo, string filename, SearchOption option = SearchOption.AllDirectories)
        {
            IDirectoryInfoWrap directory = new DirectoryInfoWrap(repo);
            IFileInfoWrap file = directory.GetFiles(filename, option).Single();
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
