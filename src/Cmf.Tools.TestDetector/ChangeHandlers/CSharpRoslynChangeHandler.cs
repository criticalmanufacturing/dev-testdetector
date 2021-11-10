using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using NLog;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CSharp;

namespace Cmf.Tools.TestDetector.ChangeHandlers
{
    /// <summary>
    /// Handle Changes in a file using C# (Roslyn) compiler.
    /// </summary>
    /// <seealso cref="Cmf.Tools.TestDetector.ChangeHandlers.IChangeHandler" />
    public sealed class CSharpRoslynChangeHandler : IChangeHandler
    {
        private struct RoslynNode
        {
            public Project Project;
            public Compilation ProjectCompilation;
            public SyntaxTree SyntaxTree;
        }

        private const string ATTRIBUTE_TEST_METHOD = "TestMethod";
        private const string ATTRIBUTE_TEST_CATEGORY = "TestCategory";

        private bool _isInitialized = false;
        
        private ILogger _logger;

        private MSBuildWorkspace _workspace;
        
        private Solution _solution;

        private Dictionary<string, RoslynNode> _fileSyntaxNodeMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpRoslynChangeHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public CSharpRoslynChangeHandler(ILogger logger)
        {
            _logger = logger;
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }
        }

        private bool IsInstanceInitialized()
        {
            return _isInitialized;
        }

        private bool HasAttribute(ISymbol symbol, string attributeName)
        {
            return symbol.GetAttributes().Any((att) => att.AttributeClass.Name == attributeName);

        }

        private async Task<ISymbol> FindMethod(RoslynNode roslynNode, string methodName)
        {
            if (!IsInstanceInitialized())
            {
                throw new Exception($"Class was not initialized. Make sure to call {nameof(Initialize)} first.");
            }

            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            var syntaxNode = await roslynNode.SyntaxTree.GetRootAsync().ConfigureAwait(false);

            var methodSyntax = syntaxNode.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(x => x.Identifier.ValueText.Equals(methodName, StringComparison.InvariantCultureIgnoreCase))
                .SingleOrDefault();

            if (methodSyntax == null)
            {
                return null;
            }

            var semanticModel = roslynNode.ProjectCompilation.GetSemanticModel(roslynNode.SyntaxTree, true);
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax);

            return methodSymbol;
        }

        private async Task<ISymbol> FindMethod(RoslynNode roslynNode, int lineNumber)
        {
            if (!IsInstanceInitialized())
            {
                throw new Exception($"Class was not initialized. Make sure to call {nameof(Initialize)} first.");
            }

            var text = await roslynNode.SyntaxTree.GetTextAsync().ConfigureAwait(false);
            if (lineNumber < 0 || lineNumber > text.Lines.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(lineNumber));
            }

            var textSpan = text.Lines[lineNumber].Span;
            var root = await roslynNode.SyntaxTree.GetRootAsync().ConfigureAwait(false);

            var node = root.FindNode(textSpan);
            var methodSyntax = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();

            if (methodSyntax == null)
            {
                return null;
            }

            var semanticModel = roslynNode.ProjectCompilation.GetSemanticModel(roslynNode.SyntaxTree, true);
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax);

            return methodSymbol;
        }

        private async Task<IEnumerable<ISymbol>> FindReferencedMethod(ISymbol startSymbol, Func<ISymbol, bool> filter = null)
        {
            var references = await SymbolFinder.FindReferencesAsync(startSymbol, _solution).ConfigureAwait(false);
            var tasks = references
                .SelectMany(r =>
                {
                    return r.Locations.Select(async x =>
                    {
                        var compilation = await x.Document.Project.GetCompilationAsync().ConfigureAwait(false);
                        var node = new RoslynNode()
                        {
                            Project = x.Document.Project,
                            ProjectCompilation = compilation,
                            SyntaxTree = x.Location.SourceTree
                        };

                        return await FindMethod(node, x.Location.GetLineSpan().Span.Start.Line).ConfigureAwait(false);
                    });
                });

            var symbols = await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);

            if (filter != null)
            {
                return symbols.Where(filter);
            } else
            {
                return symbols;
            }
        }

        private async Task<IEnumerable<ISymbol>> FindReferencedTestMethods(ISymbol symbol, List<ISymbol> gatheredSymbols = null, HashSet<ISymbol> visitedSymbols = null)
        {
            if (gatheredSymbols == null)
            {
                gatheredSymbols = new List<ISymbol>();
            }

            if (visitedSymbols == null)
            {
                visitedSymbols = new HashSet<ISymbol>();
            }

            visitedSymbols.Add(symbol);

            bool isTestMethod = HasAttribute(symbol, ATTRIBUTE_TEST_METHOD);

            if (isTestMethod == true) {
                gatheredSymbols.Add(symbol);
                return gatheredSymbols;
            }

            // If it's not a test method, get all references
            var allReferences = await FindReferencedMethod(symbol).ConfigureAwait(false);
            if (allReferences.Any())
            {
                foreach (var reference in allReferences)
                {
                    if (visitedSymbols.Contains(reference))
                    {
                        continue;
                    }

                    visitedSymbols.Add(reference);

                    if (reference != null)
                    {
                        await FindReferencedTestMethods(reference, gatheredSymbols, visitedSymbols).ConfigureAwait(false);
                    }

                }
            }

            return gatheredSymbols;
        }

        private async Task<bool> TryAddCategoryToTestMethod(RoslynNode roslynNode, ISymbol method, string category, bool recursive)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            IEnumerable<ISymbol> testMethods;
            if (!HasAttribute(method, ATTRIBUTE_TEST_METHOD))
            {
                if (recursive == true)
                {
                    testMethods = await FindReferencedTestMethods(method).ConfigureAwait(false);
                } else
                {
                    testMethods = Array.Empty<ISymbol>();
                }
            }
            else
            {
                testMethods = new ISymbol[] { method };
            }

            foreach (var testMethod in testMethods)
            {
                var attrArgument = SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(category)));
                var attrList = SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(
                            SyntaxFactory.IdentifierName(ATTRIBUTE_TEST_CATEGORY),
                            SyntaxFactory.AttributeArgumentList(
                                SyntaxFactory.SeparatedList(new[] { attrArgument })
                            )
                        )
                    )
                );

                // We do know that methds only can be declared once, but for the sake of
                // completeness, let's assume that can have more than one declaration (because partial classes can)
                foreach (var declaration in testMethod.DeclaringSyntaxReferences)
                {
                    var node = declaration.GetSyntax();
                    var methodDeclaration = node as MethodDeclarationSyntax;
                    var newAttributesList = methodDeclaration.AttributeLists.Add(attrList);

                    var newNode = declaration.SyntaxTree.GetRoot().ReplaceNode(
                        methodDeclaration,
                        methodDeclaration.WithAttributeLists(newAttributesList)
                    );

                    var documentId = roslynNode.Project.GetDocumentId(declaration.SyntaxTree);
                    if (documentId != null)
                    {
                        _solution = _solution.WithDocumentSyntaxRoot(documentId, newNode);
                    } else
                    {
                        _logger.Debug($"Document not found for '{declaration.SyntaxTree.FilePath}' for project '{roslynNode.Project.Name}'. Skiping declaration.");
                    }
                }
            }

            return testMethods.Any();
        }

        /// <summary>
        /// Tries to add a category to test method.
        /// If the method on the given line is not a test method, returns <c>False</c>.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <param name="category">The category.</param>
        /// <param name="recursive">If set to <c>true</c> it will look for the test method recursively.</param>
        /// <returns>
        /// Returns <c>true</c> if it added the category the method. <c>False</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">filePath</exception>
        /// <exception cref="ArgumentOutOfRangeException">filePath</exception>
        public async Task<bool> TryAddCategoryToTestMethod(string filePath, int lineNumber, string category, bool recursive = false)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!_fileSyntaxNodeMap.TryGetValue(filePath, out RoslynNode roslynNode))
            {
                throw new ArgumentOutOfRangeException(nameof(filePath));
            }

            _logger.Info($"Adding category {category} to file {filePath}");

            var method = await FindMethod(roslynNode, lineNumber).ConfigureAwait(false);

            if (method == null)
            {
                return false;
            }

            return await TryAddCategoryToTestMethod(roslynNode, method, category, recursive).ConfigureAwait(false);
        }

        /// <summary>
        /// Tries the add a category to test method.
        /// If the method on the file is not a test method, returns <c>False</c>.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="category">The category.</param>
        /// <param name="recursive">if set to <c>true</c> It will recursively look for a test method.</param>
        /// <returns><c>True</c> if category </returns>
        /// <exception cref="System.ArgumentNullException">filePath</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">filePath</exception>
        public async Task<bool> TryAddCategoryToTestMethod(string filePath, string methodName, string category, bool recursive = false)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!_fileSyntaxNodeMap.TryGetValue(filePath, out RoslynNode roslynNode))
            {
                throw new ArgumentOutOfRangeException(nameof(filePath));
            }

            var method = await FindMethod(roslynNode, methodName).ConfigureAwait(false);

            if (method == null)
            {
                return false;
            }

            return await TryAddCategoryToTestMethod(roslynNode, method, category, recursive).ConfigureAwait(false);
        }

        /// <summary>
        /// Initializes the ChangeHandler with the specified solution path.
        /// </summary>
        /// <param name="solutionPath">The solution path.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">solutionPath</exception>
        public async Task Initialize(string solutionPath)
        {
            if (!File.Exists(solutionPath))
            {
                throw new ArgumentOutOfRangeException(nameof(solutionPath));
            }

            _workspace = MSBuildWorkspace.Create();

            var solution = await _workspace.OpenSolutionAsync(solutionPath, new Progress<ProjectLoadProgress>((p) =>
            {
                _logger.Debug<string>($"Processing file '{p.FilePath}' ({p.Operation.ToString("D")}");
            })).ConfigureAwait(false);

            await this.Initialize(solution).ConfigureAwait(false);
        }

        /// <summary>
        /// Initializes the ChangeHandler with the given solution.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <exception cref="ArgumentNullException">solution</exception>
        public async Task Initialize(Solution solution)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            _logger.Debug($"Initializing solution {solution.FilePath}");

            _solution = solution;
            ProjectDependencyGraph projectGraph = solution.GetProjectDependencyGraph();

            _fileSyntaxNodeMap = new Dictionary<string, RoslynNode>();

            foreach (var projectId in projectGraph.GetTopologicallySortedProjects())
            {
                var project = solution.GetProject(projectId);
                var projectCompilation = await project.GetCompilationAsync().ConfigureAwait(false);

                var diagnostics = projectCompilation.GetDiagnostics();

                foreach (var tree in projectCompilation.SyntaxTrees)
                {
                    _fileSyntaxNodeMap.TryAdd(tree.FilePath, new RoslynNode()
                    {
                        Project = project,
                        ProjectCompilation = projectCompilation,
                        SyntaxTree = tree
                    });
                }
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Saves this instance.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Save()
        {
            return _workspace.TryApplyChanges(_solution);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            if (_workspace != null)
            {
                _workspace.Dispose();
            }
        }
    }
}
