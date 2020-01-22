using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cmf.Tools.TestDetector.ChangeHandlers
{
    public interface IChangeHandler : IDisposable
    {
        /// <summary>
        /// Initializes the change handler
        /// </summary>
        /// <param name="rootPath">The root path.</param>
        /// <returns></returns>
        Task Initialize(string rootPath);
        Task<bool> TryAddCategoryToTestMethod(string filePath, string methodName, string category, bool recursive = false);

        /// <summary>
        /// Tries the add category to test method.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <param name="category">The category.</param>
        /// <param name="recursive">If set to <c>true</c> it will look for the test method recursively.</param>
        /// <returns>Returns <c>true</c> if it added the category to at least on method. <c>False</c> otherwise.</returns>
        Task<bool> TryAddCategoryToTestMethod(string filePath, int lineNumber, string category, bool recursive = false);

        /// <summary>
        /// Make changes permanently.
        /// </summary>
        /// <returns></returns>
        Task<bool> Save();

    }
}
