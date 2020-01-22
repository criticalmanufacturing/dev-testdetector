using Cmf.Tools.TestDetector.Changes;
using LibGit2Sharp;
using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Cmf.Tools.TestDetector.Repository
{
    /// <summary>
    /// Repository implementation for Git respositories
    /// </summary>
    /// <seealso cref="Cmf.Tools.TestDetector.Repository.IRepository" />
    public sealed class GitRepository : IRepository
    {
        private static readonly Regex _gitDiffBlockSeparator = new Regex(@"^@@ \-(?<oldRow>\d+),(\d+) \+(?<newRow>\d+),(\d+) @@ (?<method>.*)$", RegexOptions.Multiline);

        private string _folderPath;
        private LibGit2Sharp.IRepository _repository;
        private IFileChangesFactory _fileChangesFactory;
        private IBlockChangeFactory _blockChangesFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitRepository"/> class.
        /// </summary>
        /// <param name="folderPath">The folder path.</param>
        /// <param name="fileChangesFactory">The file changes factory.</param>
        /// <param name="blockChangesFactory">The block changes factory.</param>
        /// <exception cref="ArgumentOutOfRangeException">If folderPath is not a directory.</exception>
        /// <exception cref="ArgumentNullException">
        /// fileChangesFactory
        /// or
        /// blockChangesFactory
        /// </exception>
        public GitRepository(string folderPath, IFileChangesFactory fileChangesFactory, IBlockChangeFactory blockChangesFactory)
        {
            // Check if folder exists
            if (!Directory.Exists(folderPath))
            {
                throw new ArgumentOutOfRangeException($"{nameof(folderPath)} with value '{folderPath}' is valid. Directory doesn't exist.");
            }

            if (fileChangesFactory == null)
            {
                throw new ArgumentNullException(nameof(fileChangesFactory));
            }

            if (blockChangesFactory == null)
            {
                throw new ArgumentNullException(nameof(blockChangesFactory));
            }

            _folderPath = folderPath;
            _fileChangesFactory = fileChangesFactory;
            _blockChangesFactory = blockChangesFactory;
            _repository = new LibGit2Sharp.Repository(folderPath);
        }

        public IEnumerable<string> GetChangedFiles(string commitId, string glob = null)
        {
            Matcher m = new Matcher(StringComparison.InvariantCultureIgnoreCase);
            m.AddInclude(glob);

            var mergeCommit = _repository.Lookup<Commit>(commitId);

            foreach (var entry in mergeCommit.Tree)
            {
                var matchingResult = m.Match(entry.Path);

                if (matchingResult.HasMatches)
                {
                    yield return entry.Path;
                }
            }
        }

        /// <summary>
        /// Gets the paths of files changed in the given commit.
        /// </summary>
        /// <param name="commit">The commit to analyze</param>
        /// <returns>A set of paths of changed files</returns>
        private ISet<string> GetChangedFilesPath(Commit commit, Matcher matcher = null)
        {
            var changedFiles = new HashSet<string>();
            foreach (var parent in commit.Parents)
            {
                foreach (TreeEntryChanges change in _repository.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree))
                {
                    if (matcher != null && !matcher.Match(change.Path).HasMatches)
                    {
                        continue;
                    }

                    changedFiles.Add(change.Path);
                }
            }

            return changedFiles;
        }

        /// <summary>
        /// Analyzes a patch entry change
        /// </summary>
        /// <param name="change">The change.</param>
        /// <returns>A file changes descriptor.</returns>
        private IFileChanges AnalyzeChange(PatchEntryChanges change)
        {
            var fileChange = _fileChangesFactory.Create();
            fileChange.RelativePath = change.Path;
            fileChange.AbsolutePath = Path.GetFullPath(Path.Join(_folderPath, change.Path));

            foreach (Match blockHeader in _gitDiffBlockSeparator.Matches(change.Patch))
            {
                var methodNameGroup = blockHeader.Groups["method"];
                var newRowGroup = blockHeader.Groups["newRow"];

                var blockChange = _blockChangesFactory.Create();
                if (methodNameGroup.Success && newRowGroup.Success)
                {
                    blockChange.Line = int.Parse(newRowGroup.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    blockChange.StartText = methodNameGroup.Value;
                }

                fileChange.Blocks.Add(blockChange);
            }

            return fileChange;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceCommitId">The commit of the source branch</param>
        /// <param name="targetCommitId">The commit of the target branch</param>
        /// <param name="glob"></param>
        /// <returns>An enumerable of file changes</returns>
        public IEnumerable<IFileChanges> GetChanges(string sourceCommitId, string targetCommitId, string glob = null)
        {
            #region Arguments validation

            if (string.IsNullOrWhiteSpace(sourceCommitId))
            {
                throw new ArgumentNullException(nameof(sourceCommitId));
            }

            if (string.IsNullOrWhiteSpace(targetCommitId))
            {
                throw new ArgumentNullException(nameof(targetCommitId));
            }

            #endregion

            Matcher fileMatcher = null;
            if (!string.IsNullOrWhiteSpace(glob))
            {
                fileMatcher = new Matcher(StringComparison.InvariantCultureIgnoreCase);
                fileMatcher.AddInclude(glob);
            }

            // Change detection algorithm

            // 1. Retreive two commits (A and B)
            // 2. Get all commits between A and B
            // 3. Create a patch between the two extremes of the commit list.

            // 1. Get source and target commits
            var sourceCommit = _repository.Lookup<Commit>(sourceCommitId);
            var targetCommit = _repository.Lookup<Commit>(targetCommitId);

            if (sourceCommit == null)
            {
                throw new Exception($"Source commit '{sourceCommitId}' was not found.");
            }
            
            if (targetCommit == null)
            {
                throw new Exception($"Target commit '{targetCommitId}' was not found.");
            }

            // 2. Get All commits between A and B

            var filter = new CommitFilter
            {
                SortBy = CommitSortStrategies.Time,
                IncludeReachableFrom = sourceCommit,
                ExcludeReachableFrom = targetCommit
            };

            var allCommitsBetweenSourceAndTarget = _repository.Commits.QueryBy(filter).ToList();

            // If there are no commits, stop
            if (allCommitsBetweenSourceAndTarget.Count == 0)
            {
                yield break;
            }

            var deltaBetweenCommits = _repository.Diff.Compare<Patch>(targetCommit.Tree, allCommitsBetweenSourceAndTarget.First().Tree);

            // 3. Get all changed files between all the commits retrieved in 2.
            foreach (var change in deltaBetweenCommits)
            {
                // Skip if a glob was passed and the path change doesn't match
                if (fileMatcher != null && !fileMatcher.Match(change.Path).HasMatches)
                {
                    continue;
                }

                yield return AnalyzeChange(change);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this._repository != null)
            {
                this._repository.Dispose();
            }
        }
    }
}
