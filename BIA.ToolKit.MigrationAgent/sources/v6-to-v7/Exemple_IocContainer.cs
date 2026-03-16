// <copyright file="IocContainer.cs" company="Safran">
// Copyright (c) Safran. All rights reserved.
// </copyright>

namespace Safran.SCardNG.Crosscutting.Ioc
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Audit.Core;
    using Audit.EntityFramework;
    using BIA.Net.Core.Application.Permission;
    using BIA.Net.Core.Application.User;
    using BIA.Net.Core.Common;
    using BIA.Net.Core.Common.Configuration;
    using BIA.Net.Core.Common.Configuration.ApiFeature;
    using BIA.Net.Core.Common.Configuration.CommonFeature;
    using BIA.Net.Core.Common.Configuration.WorkerFeature;
    using BIA.Net.Core.Common.Enum;
    using BIA.Net.Core.Domain.Announcement.Mappers;
    using BIA.Net.Core.Domain.Mapper;
    using BIA.Net.Core.Domain.RepoContract;
    using BIA.Net.Core.Domain.User.Mappers;
    using BIA.Net.Core.Domain.User.Services;
    using BIA.Net.Core.Infrastructure.Data;
    using BIA.Net.Core.Infrastructure.Data.Helpers;
    using BIA.Net.Core.Infrastructure.Data.Repositories;
    using BIA.Net.Core.Infrastructure.Data.Repositories.HistoryRepositories;
    using BIA.Net.Core.Infrastructure.Service.Repositories;
    using BIA.Net.Core.Ioc;
    using BIA.Net.Core.Presentation.Common.Features.HubForClients;
    using Hangfire;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Migrations;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Safran.SCardNG.Application.User;
    using Safran.SCardNG.Crosscutting.Common;
    using Safran.SCardNG.Crosscutting.Common.Enum;
    using Safran.SCardNG.Crosscutting.Common.Error;
    using Safran.SCardNG.Domain.Dto.User;
    using Safran.SCardNG.Domain.RepoContract;
    using Safran.SCardNG.Domain.User.Entities;
    using Safran.SCardNG.Domain.User.Mappers;
    using Safran.SCardNG.Domain.User.Models;
    using Safran.SCardNG.Infrastructure.Data;
    using Safran.SCardNG.Infrastructure.Data.Features;
    using Safran.SCardNG.Infrastructure.Data.Repositories;
    using Safran.SCardNG.Infrastructure.Service.Repositories;

    /// <summary>
    /// The IoC Container.
    /// </summary>
    public static class IocContainer
    {
        /// <summary>
        /// The method used to register all instance.
        /// </summary>
        /// <param name="collection">The collection of service.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="isApi">true if it's an API, false if it's a Worker.</param>
        /// <param name="isUnitTest">Are we configuring IoC for unit tests? If so, some IoC shall not be performed here but replaced by
        /// specific ones in IocContainerTest.</param>
        public static void ConfigureContainer(IServiceCollection collection, IConfiguration configuration, bool isApi, bool isUnitTest = false)
        {
            if (configuration == null && !isUnitTest)
            {
                throw Exception("Configuration cannot be null");
            }

            BiaNetSection biaNetSection = new BiaNetSection();
            configuration?.GetSection("BiaNet").Bind(biaNetSection);

            ConfigureInfrastructureServiceContainer(collection, biaNetSection, isUnitTest);
            ConfigureDomainContainer(collection);
            ConfigureApplicationContainer(collection, isApi);

            BiaIocContainer.ConfigureContainer(collection, configuration, isUnitTest);

            ConfigureInfrastructureDataContainer(collection, configuration, isUnitTest);
            if (!isUnitTest)
            {
                ConfigureCommonContainer(collection, configuration);
                collection.Configure<CommonFeatures>(configuration.GetSection("BiaNet:CommonFeatures"));
                collection.Configure<WorkerFeatures>(configuration.GetSection("BiaNet:WorkerFeatures"));
                collection.Configure<ApiFeatures>(configuration.GetSection("BiaNet:ApiFeatures"));
            }

            ErrorMessage.FillErrorTranslations();
        }

        private static Exception Exception(string v)
        {
            throw new NotImplementedException();
        }

        private static void ConfigureApplicationContainer(IServiceCollection collection, bool isApi)
        {
            // Permissions
            collection.AddSingleton<IPermissionProvider, PermissionProvider<BiaPermissionId>>();
            collection.AddSingleton<IPermissionProvider, PermissionProvider<PermissionId>>();
            collection.AddSingleton<IPermissionService, PermissionService>();
            collection.AddTransient(typeof(IBaseUserSynchronizeDomainService<User, UserFromDirectory>), typeof(UserSynchronizeDomainService));
            collection.AddTransient(typeof(IBaseUserAppService<UserDto, User, UserFromDirectoryDto, UserFromDirectory>), typeof(UserAppService));
            collection.AddTransient(typeof(IBaseTeamAppService<TeamTypeId>), typeof(TeamAppService));

            // IT'S NOT NECESSARY TO DECLARE Services (They are automatically managed by the method BiaIocContainer.RegisterServicesFromAssembly)
            BiaIocContainer.RegisterServicesFromAssembly(
                collection: collection,
                assemblyName: "BIA.Net.Core.Application",
                serviceLifetime: ServiceLifetime.Transient);

            // IT'S NOT NECESSARY TO DECLARE Services (They are automatically managed by the method BiaIocContainer.RegisterServicesFromAssembly)
            BiaIocContainer.RegisterServicesFromAssembly(
                collection: collection,
                assemblyName: "Safran.SCardNG.Application",
                excludedServiceNames: new List<string>() { nameof(AuthAppService) },
                serviceLifetime: ServiceLifetime.Transient);

            if (isApi)
            {
                collection.AddTransient(typeof(IAuthAppService), typeof(AuthAppService));
            }

            collection.AddTransient<IBackgroundJobClient, BackgroundJobClient>();
        }

        private static void ConfigureDomainContainer(IServiceCollection collection)
        {
            collection.AddTransient(typeof(IUserFromDirectoryMapper<UserFromDirectoryDto, UserFromDirectory>), typeof(UserFromDirectoryMapper));

            // IT'S NOT NECESSARY TO DECLARE Services (They are automatically managed by the method BiaIocContainer.RegisterServicesFromAssembly)
            BiaIocContainer.RegisterServicesFromAssembly(
                collection: collection,
                assemblyName: "Safran.SCardNG.Domain",
                serviceLifetime: ServiceLifetime.Transient);

            Type templateType = typeof(BiaBaseMapper<,,>);
            Assembly assembly = Assembly.Load("Safran.SCardNG.Domain");
            List<Type> derivedTypes = ReflectiveEnumerator.GetDerivedTypes(assembly, templateType);
            foreach (var type in derivedTypes)
            {
                collection.AddScoped(type);
            }

            collection.AddSingleton<IAuditMapper, AnnouncementAuditMapper>();

            Type auditMapperType = typeof(IAuditMapper);
            List<Type> auditMapperDerivedTypes = ReflectiveEnumerator.GetDerivedTypes(assembly, auditMapperType);
            foreach (var auditMapperDerivedType in auditMapperDerivedTypes)
            {
                collection.AddSingleton(auditMapperType, auditMapperDerivedType);
            }
        }

        private static void ConfigureCommonContainer(IServiceCollection collection, IConfiguration configuration)
        {
            // Common Layer
        }

        private static void ConfigureInfrastructureDataContainer(IServiceCollection collection, IConfiguration configuration, bool isUnitTest)
        {
            if (!isUnitTest)
            {
                collection.Configure<BiaHistoryRepositoryOptions>(options =>
                {
                    options.AppVersion = Constants.Application.BackEndVersion;
                });

                string connectionString = configuration.GetDatabaseConnectionString(BiaConstants.DatabaseConfiguration.DefaultKey);

                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    DbProvider dbEngine = configuration.GetProvider(BiaConstants.DatabaseConfiguration.DefaultKey);

                    // Infrastructure Data Layer
                    collection.AddDbContext<IQueryableUnitOfWork, DataContext>(
                        options =>
                        {
                            if (dbEngine == DbProvider.PostGreSql)
                            {
                                options.UseNpgsql(connectionString, options =>
                                {
                                    options.EnableRetryOnFailure();
                                });
                                options.ReplaceService<IHistoryRepository, BiaNpgsqlHistoryRepository>();
                            }
                            else
                            {
                                options.UseSqlServer(connectionString, options =>
                                {
                                    options.EnableRetryOnFailure();
                                });
                                options.ReplaceService<IHistoryRepository, BiaSqlServerHistoryRepository>();
                            }

                            options.EnableSensitiveDataLogging();
                            options.AddInterceptors(new AuditSaveChangesInterceptor());
                        });

                    collection.AddDbContext<IQueryableUnitOfWorkNoTracking, DataContextNoTracking>(
                        options =>
                        {
                            if (dbEngine == DbProvider.PostGreSql)
                            {
                                options.UseNpgsql(connectionString, options =>
                                {
                                    options.EnableRetryOnFailure();
                                });
                                options.ReplaceService<IHistoryRepository, BiaNpgsqlHistoryRepository>();
                            }
                            else
                            {
                                options.UseSqlServer(connectionString, options =>
                                {
                                    options.EnableRetryOnFailure();
                                });
                                options.ReplaceService<IHistoryRepository, BiaSqlServerHistoryRepository>();
                            }

                            options.EnableSensitiveDataLogging();
                        },
                        contextLifetime: ServiceLifetime.Transient);
                }

                collection.AddScoped<DataContextFactory>();
                collection.AddSingleton<BIA.Net.Core.Application.Services.IAuditFeatureService, BIA.Net.Core.Application.Services.AuditFeatureService>();
            }

            collection.AddSingleton<IAuditFeature, AuditFeature>();
            collection.AddScoped<IBiaHybridCache, BiaHybridCache>();

            // IT'S NOT NECESSARY TO DECLARE QueryCustomizer/Repository (They are automatically managed by the method BiaIocContainer.RegisterServicesFromAssembly)
            BiaIocContainer.RegisterServicesFromAssembly(
                collection: collection,
                assemblyName: "Safran.SCardNG.Infrastructure.Data",
                interfaceAssemblyName: "Safran.SCardNG.Domain",
                serviceLifetime: ServiceLifetime.Transient);

            // Must specify the User type explicitly
            collection.AddScoped<ICoreUserRepository, CoreUserRepository<User>>();
        }

#pragma warning disable S1172 // Unused method parameters should be removed

        private static void ConfigureInfrastructureServiceContainer(IServiceCollection collection, BiaNetSection biaNetSection, bool isUnitTest = false)
#pragma warning restore S1172 // Unused method parameters should be removed
        {
            collection.AddSingleton<IUserDirectoryRepository<UserFromDirectoryDto, UserFromDirectory>, LdapRepository>();
            collection.AddSingleton<IUserIdentityKeyDomainService, UserIdentityKeyDomainService>();
            collection.AddTransient<IMailRepository, MailRepository>();
            collection.AddHttpClient<IIdentityProviderRepository<UserFromDirectory>, IdentityProviderRepository>().ConfigurePrimaryHttpMessageHandler(() => BiaIocContainer.CreateHttpClientHandler(biaNetSection, false));

            collection.AddHttpClient<ISeeEedRepository, SeeEedRepository>().ConfigurePrimaryHttpMessageHandler(() => BiaIocContainer.CreateHttpClientHandler(biaNetSection));
            collection.AddHttpClient<ISharpRepository, SharpRepository>().ConfigurePrimaryHttpMessageHandler(() => BiaIocContainer.CreateHttpClientHandler(biaNetSection));

            if (biaNetSection.CommonFeatures?.ClientForHub?.IsActive == true)
            {
                if (isUnitTest || !string.IsNullOrEmpty(biaNetSection.CommonFeatures?.ClientForHub.SignalRUrl))
                {
                    collection.AddTransient<IClientForHubRepository, ExternalClientForSignalRRepository>();
                }
                else
                {
                    collection.AddTransient<IClientForHubRepository, InternalClientForSignalRRepository<HubForClients>>();
                }
            }
        }
    }
}