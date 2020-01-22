using Autofac;
using Cmf.Tools.TestDetector.ChangeHandlers;
using Cmf.Tools.TestDetector.Repository;
using CommandLine;
using System;
using System.IO;

namespace Cmf.Tools.TestDetector.Console
{
    class Program
    {

        public class Options
        {
            [Option(HelpText = "Repository root folder path", Required = true)]
            public string RepositoryPath { get; set; }

            [Option(HelpText = "Path Test Solution. Can be either absolute or relative (to repository path)", Required = true)]
            public string TestSolutionPath { get; set; }

            [Option(HelpText = "Should the search be recursive?", Default = false, Required = false)]
            public bool RecursiveSearch { get; set; }

            [Option(HelpText = "Only analyze files that match the given glob", Default = null, Required = false)]
            public string Filter { get; set; }

            [Option(HelpText = "Name of the TestCategory to add", Required = true)]
            public string TestCategory { get; set; }

            [Option(
                HelpText = "Source Commit Identifier (commit from branch where we want to merge to)",
                Required = true
            )]
            public string SourceCommitId { get; set; }

            [Option(
                HelpText = "Target Commit Identifier (merge commit from the pull request branch and the branch we want to merge to)",
                Required = true
            )]
            public string TargetCommitId { get; set; }

            public void Validate()
            {
                if (!Path.IsPathRooted(TestSolutionPath))
                {
                    TestSolutionPath = Path.Join(RepositoryPath, TestSolutionPath);
                }
            }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed((o) =>
            {
                o.Validate();

                var builder = new ContainerBuilder();
                builder.RegisterModule<DetectorModule>();

                var container = builder.Build();

                using (var scope = container.BeginLifetimeScope())
                {
                    var repoFactory = scope.Resolve<IRepositoryFactory>();
                    using (var repo = repoFactory.Create(o.RepositoryPath))
                    {
                       var cSharpChangeHandler = scope.ResolveKeyed<IChangeHandler>(ChangeHandlerType.CSharpRoslynChangeHandler);

                       using (cSharpChangeHandler)
                       {
                            cSharpChangeHandler.Initialize(o.TestSolutionPath).Wait();
                            foreach (var change in repo.GetChanges(o.SourceCommitId, o.TargetCommitId, o.Filter))
                            {
                                foreach (var block in change.Blocks)
                                {
                                    cSharpChangeHandler.TryAddCategoryToTestMethod(change.AbsolutePath, block.Line, o.TestCategory, o.RecursiveSearch).Wait();
                                }
                            }
                            cSharpChangeHandler.Save();
                       }
                    }
                }
            });
        }
    }
}
