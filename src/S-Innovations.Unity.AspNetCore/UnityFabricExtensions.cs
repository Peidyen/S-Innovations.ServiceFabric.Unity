﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Lifetime;
using Unity.Injection;

namespace SInnovations.Unity.AspNetCore
{
    public static class UnityFabricExtensions
    {
        /// <summary>
        /// Configure logging in the container by registering the logger factory
        /// </summary>
        /// <param name="container"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IUnityContainer ConfigureLogging(this IUnityContainer container, ILoggerFactory logger)
        {
            return container.RegisterInstance(logger);
        }

        /// <summary>
        /// Configure the T for Options<typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="container"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IUnityContainer Configure<T>(this IUnityContainer container, IConfigurationSection configuration) where T : class
        {
            container.RegisterInstance<IOptionsChangeTokenSource<T>>(typeof(T).AssemblyQualifiedName, new ConfigurationChangeTokenSource<T>(configuration));
            container.RegisterInstance<IConfigureOptions<T>>(typeof(T).AssemblyQualifiedName, new ConfigureFromConfigurationOptions<T>(configuration));
            return container;
        }

        /// <summary>
        /// Configure using a subsection name for T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="container"></param>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        public static IUnityContainer Configure<T>(this IUnityContainer container, string sectionName) where T : class
        {
            container.RegisterType<IOptionsChangeTokenSource<T>>(typeof(T).AssemblyQualifiedName, new ContainerControlledLifetimeManager(),
             new InjectionFactory((c) => new ConfigurationChangeTokenSource<T>(c.Resolve<IConfigurationRoot>().GetSection(sectionName))));
            container.RegisterType<IConfigureOptions<T>>(typeof(T).AssemblyQualifiedName, new ContainerControlledLifetimeManager(),
              new InjectionFactory((c) => new ConfigureFromConfigurationOptions<T>(c.Resolve<IConfigurationRoot>().GetSection(sectionName))));
            return container;

        }

        public static IUnityContainer UseConfiguration(this IUnityContainer container, IConfigurationBuilder builder)
        {
            container.RegisterInstance(builder);
            container.RegisterType<IConfigurationRoot>(new ContainerControlledLifetimeManager(),
                new InjectionFactory((c) => c.Resolve<IConfigurationBuilder>().Build()));
            container.RegisterType<IConfiguration>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => c.Resolve<IConfigurationRoot>()));


            return container;

            //return container.UseConfiguration(builder.Build());
        }

        /// <summary>
        /// Add AspNet Core Options support to the container
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        public static IUnityContainer AddOptions(this IUnityContainer container)
        {
#if ASPNETCORE1
            return container.RegisterType(typeof(IOptions<>), typeof(OptionsManager<>))
                            .RegisterType(typeof(IOptionsSnapshot<>), typeof(OptionsManager<>), new HierarchicalLifetimeManager())
                            .RegisterType(typeof(IOptionsMonitor<>), typeof(OptionsMonitor<>), new ContainerControlledLifetimeManager());
#endif

#if NETSTANDARD2_0
            return container.RegisterType(typeof(IOptions<>), typeof(OptionsManager<>), new ContainerControlledLifetimeManager())
                .RegisterType(typeof(IOptionsSnapshot<>), typeof(OptionsManager<>), new HierarchicalLifetimeManager())
           .RegisterType(typeof(IOptionsMonitor<>), typeof(OptionsMonitor<>), new ContainerControlledLifetimeManager())
            .RegisterType(typeof(IOptionsFactory<>), typeof(OptionsFactory<>), new TransientLifetimeManager())
           .RegisterType(typeof(IOptionsMonitorCache<>), typeof(OptionsCache<>), new ContainerControlledLifetimeManager());
#endif

        }


        /// <summary>
        /// Add the extensions needed to make everything works. Including EnumerableExtensions.
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        public static IUnityContainer WithExtension(this IUnityContainer container)
        {
            container.RegisterType<IServiceProvider, UnityServiceProvider>();
            container.RegisterType<IServiceScopeFactory, UnityServiceScopeFactory>();

            return container.AddExtension(new EnumerableExtension()).AddExtension(new CustomBuildExtension());
        }
    }
}
