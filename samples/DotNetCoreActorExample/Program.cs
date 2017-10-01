﻿using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Unity;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Serilog;
using SInnovations.ServiceFabric.Unity;
using SInnovations.Unity.AspNetCore;

namespace DependencyInjectionActorSample
{

    public interface IMyScopedDependency : IDisposable {

    }

    public class MyScopedDependency : IMyScopedDependency
    {
        public void Dispose()
        {
            
        }
    }
    public interface IMyTestActor : IActor
    {
        Task StartAsync();
    }

    public interface IMySecondTestActor : IActor
    {
        Task DoWorkAsync();
    }

    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IDependencyInjectionActorSample : IActor
    {
        /// <summary>
        /// TODO: Replace with your own actor method.
        /// </summary>
        /// <returns></returns>
        Task<int> GetCountAsync();

        /// <summary>
        /// TODO: Replace with your own actor method.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        Task SetCountAsync(int count);
    }


    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IDependencyInjectionActorSample1 : IActor
    {
        /// <summary>
        /// TODO: Replace with your own actor method.
        /// </summary>
        /// <returns></returns>
        Task<int> GetCountAsync();

        /// <summary>
        /// TODO: Replace with your own actor method.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        Task SetCountAsync(int count);
    }


    [StatePersistence(StatePersistence.Persisted)]
    public class MyTestActor : Actor, IMyTestActor, IRemindable
    {
        private readonly ILogger<MyTestActor> _logger;
        private readonly IServiceProvider _services;
        private readonly IServiceScopeFactory _scopes;
        public MyTestActor(
            ActorService actorService, ActorId actorId,
            ILoggerFactory logger,
            IServiceProvider services,
            IServiceScopeFactory scopes
            ) : base(actorService, actorId)
        {
            this._logger = logger.CreateLogger<MyTestActor>();
            this._services = services;
            this._scopes = scopes;
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            // The StateManager is this actor's private state store.
            // Data stored in the StateManager will be replicated for high-availability for actors that use volatile or persisted state storage.
            // Any serializable object can be saved in the StateManager.
            // For more information, see https://aka.ms/servicefabricactorsstateserialization

         
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            _logger.LogInformation("Reminder {reminderName} of {ActorName} is triggered", reminderName, nameof(MyTestActor));


            await ActorProxy.Create<IMySecondTestActor>(this.GetActorId()).DoWorkAsync();

            // dependency1 will 
            var dependency1 = _services.GetService<IMyScopedDependency>();
            var dependency2 = _services.GetService<IMyScopedDependency>();

            if(dependency1.GetHashCode() != dependency2.GetHashCode())
            {
                throw new Exception("Should be the same dependencies");
            }
            using (var scope = _scopes.CreateScope())
            {

                var dependency3 = scope.ServiceProvider.GetService<IMyScopedDependency>();
                if (dependency1.GetHashCode() == dependency3.GetHashCode())
                {
                    throw new Exception("Should be different dependencies");
                }


            }


        }

        public async Task StartAsync()
        {
            _logger.LogInformation("{ActorName} {Method} triggered", nameof(MyTestActor), nameof(StartAsync));

            await this.RegisterReminderAsync("myreminder", null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(3));
        }
    }

    [StatePersistence(StatePersistence.Persisted)]
    public class MySecondTestActor : Actor, IMySecondTestActor, IRemindable
    {
        private readonly ILogger<MySecondTestActor> _logger;
        public MySecondTestActor(ActorService actorService, ActorId actorId, ILoggerFactory loggerFactory) : base(actorService, actorId)
        {
            this._logger = loggerFactory.CreateLogger<MySecondTestActor>();
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            // The StateManager is this actor's private state store.
            // Data stored in the StateManager will be replicated for high-availability for actors that use volatile or persisted state storage.
            // Any serializable object can be saved in the StateManager.
            // For more information, see https://aka.ms/servicefabricactorsstateserialization


        }

        protected override Task OnDeactivateAsync()
        {
            return base.OnDeactivateAsync();
        }

        public async Task DoWorkAsync()
        {
            _logger.LogInformation("{ActorName} {Method} triggered", nameof(MySecondTestActor), nameof(DoWorkAsync));

            var isWorking = await this.StateManager.GetOrAddStateAsync("isWorking", false);
            if (!isWorking)
            {
                await this.RegisterReminderAsync("mysecoondreminder", null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));
                await this.StateManager.AddOrUpdateStateAsync("isWorking", true, (s, o) => true);
            }
           
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            _logger.LogInformation("Reminder {reminderName} of {ActorName} is triggered", reminderName, nameof(MySecondTestActor));

            var count = await ActorProxy.Create<IDependencyInjectionActorSample>(this.GetActorId()).GetCountAsync();
            await ActorProxy.Create<IDependencyInjectionActorSample>(this.GetActorId()).SetCountAsync(count + 1);

        }
    }

    //public class FabricContainer : UnityContainer, IServiceScopeInitializer
    //{

    //    public FabricContainer()
    //    {
    //        this.RegisterInstance<IServiceScopeInitializer>(this);
    //        this.AsFabricContainer();
    //    }
    //    public IUnityContainer InitializeScope(IUnityContainer container)
    //    {
    //        return container.WithExtension();
    //    }
    //}

    public class FabricContainer : UnityContainer, IServiceScopeInitializer
    {

        public FabricContainer()
        {
            this.RegisterInstance<IServiceScopeInitializer>(this);
            this.AsFabricContainer();
        }
        public IUnityContainer InitializeScope(IUnityContainer container)
        {
            return container.WithExtension();
        }
    }

    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // This line registers an Actor Service to host your actor class with the Service Fabric runtime.
                // The contents of your ServiceManifest.xml and ApplicationManifest.xml files
                // are automatically populated when you build this project.
                // For more information, see https://aka.ms/servicefabricactorsplatform

                var log = new LoggerConfiguration()
                 .MinimumLevel.Debug()
                 .WriteTo.Trace()
                 .CreateLogger();


                using (var container = new FabricContainer())
                {
                    var loggerfac = new LoggerFactory() as ILoggerFactory;
                    loggerfac.AddSerilog();
                    container.RegisterInstance(loggerfac);

                    container.RegisterType<IMyScopedDependency, MyScopedDependency>(new HierarchicalLifetimeManager());
                    
                    container.WithActor<MyTestActor>(new ActorServiceSettings()
                    {
                        ActorGarbageCollectionSettings = new ActorGarbageCollectionSettings(120, 60)
                    });

                    container.WithActor<MySecondTestActor>(new ActorServiceSettings()
                    {
                        ActorGarbageCollectionSettings = new ActorGarbageCollectionSettings(30, 10)
                    });

                    container.WithActor<DependencyInjectionActorSample>(new ActorServiceSettings()
                    {
                        ActorGarbageCollectionSettings = new ActorGarbageCollectionSettings(300, 100)
                    });

                    Task.Delay(15000).ContinueWith(async (task) =>
                    {
                        await ActorProxy.Create<IMyTestActor>(new ActorId("MyCoolActor")).StartAsync();

                        

                    }).Wait();

                    Thread.Sleep(Timeout.Infinite);

                }
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
