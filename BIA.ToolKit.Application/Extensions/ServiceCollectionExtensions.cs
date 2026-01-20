namespace BIA.ToolKit.Application.Extensions
{
    using BIA.ToolKit.Application.Services.CRUD;
    using BIA.ToolKit.Application.Services.DTO;
    using BIA.ToolKit.Application.Services.Option;
    using BIA.ToolKit.Application.Services.ProjectMigration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Extension methods for IServiceCollection to register application services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds CRUD generation services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCRUDServices(this IServiceCollection services)
        {
            services.AddScoped<ICRUDGenerationService, CRUDGenerationService>();
            return services;
        }

        /// <summary>
        /// Adds DTO generation services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDtoServices(this IServiceCollection services)
        {
            services.AddScoped<IDtoGenerationService, DtoGenerationService>();
            return services;
        }

        /// <summary>
        /// Adds project migration services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddProjectServices(this IServiceCollection services)
        {
            services.AddScoped<IProjectMigrationService, ProjectMigrationService>();
            return services;
        }

        /// <summary>
        /// Adds option generation services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddOptionServices(this IServiceCollection services)
        {
            services.AddScoped<IOptionGenerationService, OptionGenerationService>();
            return services;
        }

        /// <summary>
        /// Adds all application business services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddCRUDServices();
            services.AddDtoServices();
            services.AddProjectServices();
            services.AddOptionServices();
            return services;
        }
    }
}
