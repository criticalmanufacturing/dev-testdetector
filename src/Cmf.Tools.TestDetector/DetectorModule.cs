using Autofac;
using Autofac.Extras.NLog;
using Cmf.Tools.TestDetector.ChangeHandlers;
using Cmf.Tools.TestDetector.Changes;
using Cmf.Tools.TestDetector.Repository;
using System;

namespace Cmf.Tools.TestDetector
{
    /// <summary>
    /// Test Detector AutoFac Module.
    /// </summary>
    /// <seealso cref="Autofac.Module" />
    public class DetectorModule : Module
    {
        /// <summary>
        /// Override to add registrations to the container.
        /// </summary>
        /// <param name="builder">The builder through which components can be
        /// registered.</param>
        /// <remarks>
        /// Note that the ContainerBuilder parameter is unique to this module.
        /// </remarks>
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.RegisterModule<NLogModule>();

            builder.RegisterType<FileChangesFactory>()
                .As<IFileChangesFactory>();

            builder.RegisterType<BlockChangeFactory>()
                .As<IBlockChangeFactory>();

            builder.RegisterType<RepositoryFactory>()
                .As<IRepositoryFactory>();

            builder.Register<IRepository>((c, p) =>
            {
                return new GitRepository(
                    p.Named<string>("path"),
                    c.Resolve<IFileChangesFactory>(),
                    c.Resolve<IBlockChangeFactory>()
                );
            });

            builder.Register<IFileChanges>((c, p) =>
            {
                return new DefaultFileChanges();
            });

            builder.Register<IBlockChange>((c, p) =>
            {
                return new DefaultBlockChange();
            });

            #region Change Handlers Register

            builder.RegisterType<CSharpRoslynChangeHandler>()
                .Keyed<IChangeHandler>(ChangeHandlerType.CSharpRoslynChangeHandler);

            #endregion
        }
    }
}
