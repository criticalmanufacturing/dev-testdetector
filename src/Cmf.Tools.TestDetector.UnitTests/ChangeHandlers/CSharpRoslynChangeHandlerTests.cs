using Cmf.Tools.TestDetector.ChangeHandlers;
using Microsoft.Build.Construction;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cmf.Tools.TestDetector.UnitTests.ChangeHandlers
{
    [TestClass]
    public class CSharpRoslynChangeHandlerTests
    {
        private const string TESTMETHOD1_NAME = "MyTestMethod1";
        private const string TESTMETHOD2_NAME = "MyTestMethod2";
        private const string SCENARIOMETHOD1_NAME = "MyScenarioMethod1";

        private const string DOCUMENT_TEST = "TestClass.cs";
        private const string DOCUMENT_SCENARIO = "ScenarioClass.cs";

        private Workspace _workspace;
        private Solution _solution;
        private DocumentId _documentId;

        private CSharpRoslynChangeHandler _handler;

        [TestInitialize]
        public async Task CreateScenario()
        {
            var logger = new Mock<ILogger>().Object;

            var workspace = new AdhocWorkspace();

            var solutionScenario = SolutionInfo.Create(
                SolutionId.CreateNewId(),
                VersionStamp.Create()
            );

            var solution = workspace.AddSolution(solutionScenario);


            ProjectInfo projectScenario = ProjectInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Create(),
                "CSharpRoslynChangeHandlerTests_Project",
                "...",
                LanguageNames.CSharp
            );

            solution = solution.AddProject(projectScenario);

            #region Test Class

            SourceText sourceText = SourceText.From($@"
                using Microsoft.VisualStudio.TestTools.UnitTesting;
                public class TestClass {{
                    [TestMethod]
                    public void {TESTMETHOD1_NAME}() {{

                    }}

                    [TestMethod]
                    public void {TESTMETHOD2_NAME}() {{
                        ScenarioClass.{SCENARIOMETHOD1_NAME}();
                    }}
                }}
            ");

            _documentId = DocumentId.CreateNewId(solution.ProjectIds[0]);
            solution = solution.AddDocument(_documentId, DOCUMENT_TEST, sourceText);

            #endregion

            #region Other Class

            sourceText = SourceText.From($@"
                public static class ScenarioClass {{
                    public static void {SCENARIOMETHOD1_NAME}() {{

                    }}
                }}
            ");

            _documentId = DocumentId.CreateNewId(solution.ProjectIds[0]);
            _solution = solution.AddDocument(_documentId, DOCUMENT_SCENARIO, sourceText);

            #endregion


            workspace.TryApplyChanges(_solution);

            _workspace = workspace;

            _handler = new CSharpRoslynChangeHandler(logger);
            await _handler.Initialize(_workspace.CurrentSolution);
        }

        [TestMethod]
        public async Task AddCategoryToTestMethod_TestMethod()
        {
            var result = await _handler.TryAddCategoryToTestMethod(DOCUMENT_TEST, TESTMETHOD1_NAME, "PullRequest");

            Assert.IsTrue(result, "TryAddCategoryToTestMethod should be able to add a category to the test");
        }

        [TestMethod]
        public async Task AddCategoryToTestMethod_TestMethod_ByLine()
        {
            var result = await _handler.TryAddCategoryToTestMethod(DOCUMENT_TEST, 5, "PullRequest");

            Assert.IsTrue(result, "TryAddCategoryToTestMethod should be able to add a category to the test");
        }

        [TestMethod]
        public async Task AddCategoryToTestMethod_NonTestMethod()
        {
            var result = await _handler.TryAddCategoryToTestMethod(DOCUMENT_TEST, "C", "PullRequest");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task AddCategoryToTestMethod_NonTestMethod_ByLine()
        {
            var result = await _handler.TryAddCategoryToTestMethod(DOCUMENT_TEST, 1, "PullRequest");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task AddCategoryToTestMethod_TestMethodRecursive()
        {
            var result = await _handler.TryAddCategoryToTestMethod(DOCUMENT_SCENARIO, SCENARIOMETHOD1_NAME, "PullRequest", false);

            Assert.IsFalse(result);

            result = await _handler.TryAddCategoryToTestMethod(DOCUMENT_SCENARIO, SCENARIOMETHOD1_NAME, "PullRequest", true);

            Assert.IsTrue(result);
        }

        [TestCleanup]
        public void DestroyScenario()
        {
            if (_handler != null)
            {
                _handler.Dispose();
            }

            if (_workspace != null)
            {
                _workspace.Dispose();
            }
        }
    }
}
