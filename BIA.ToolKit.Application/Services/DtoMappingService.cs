namespace BIA.ToolKit.Application.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator;

    /// <summary>
    /// Encapsulates the logic for building domain entity property trees and DTO mapping property
    /// lists. Mirrors the methodology used in <c>DtoGeneratorViewModel</c> for interactive generation
    /// so that the same accurate property data is available when regenerating from history (e.g.
    /// <c>FeatureMigrationGeneratorService</c>).
    /// </summary>
    public partial class DtoMappingService(IConsoleWriter consoleWriter)
    {
        private readonly IConsoleWriter consoleWriter = consoleWriter;

        internal static readonly List<string> OptionCollectionsMappingTypes =
        [
            "icollection",
            "list",
        ];

        internal static readonly List<string> StandardMappingTypes =
        [
            "bool",
            "byte",
            "sbyte",
            "char",
            "decimal",
            "double",
            "float",
            "int",
            "uint",
            "long",
            "ulong",
            "short",
            "ushort",
            "string",
            "DateTime",
            "DateOnly",
            "TimeOnly",
            "byte[]",
            "Guid",
        ];

        internal static readonly Dictionary<string, string> SpecialTypeToRemap = new()
        {
            { "TimeSpan", "string" },
            { "TimeSpan?", "string" },
        };

        internal static readonly Dictionary<string, List<string>> DateTypesByPropertyType = new()
        {
            { "TimeSpan", new List<string> { "time" } },
            { "DateTime", new List<string> { "datetime", "date", "time" } },
        };

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Builds the entity property tree for a given domain entity.
        /// Mirrors <c>DtoGeneratorViewModel.RefreshEntityPropertiesTreeView</c> and
        /// <c>DtoGeneratorViewModel.FillEntityProperties</c>.
        /// </summary>
        public List<EntityProperty> BuildEntityPropertyTree(EntityInfo entityInfo, IEnumerable<EntityInfo> allDomainEntities)
        {
            var allEntities = allDomainEntities.ToList();
            var result = new List<EntityProperty>();

            foreach (PropertyInfo property in entityInfo.Properties.OrderBy(x => x.Name))
            {
                var entityProperty = new EntityProperty
                {
                    Name = property.Name,
                    Type = property.Type,
                    CompositeName = property.Name,
                    IsSelected = true,
                    ParentType = entityInfo.Name,
                };
                FillEntityPropertyChildren(entityProperty, entityInfo.Name, allEntities);
                result.Add(entityProperty);
            }

            return result;
        }

        /// <summary>
        /// Builds the full <see cref="MappingEntityProperty"/> list from an entity property tree,
        /// populating all fields that are used during code generation (EntityType, MappingType,
        /// OptionType, OptionRelationType, etc.).
        /// Only properties where <see cref="EntityProperty.IsSelected"/> is <c>true</c> are included.
        /// <para>
        /// Mirrors <c>DtoGeneratorViewModel.RefreshMappingProperties</c> and
        /// <c>DtoGeneratorViewModel.AddMappingProperties</c> but without the UI-only list fields
        /// (<c>OptionIdProperties</c>, <c>OptionDisplayProperties</c>, <c>MappingDateTypes</c>, …).
        /// </para>
        /// </summary>
        public List<MappingEntityProperty> BuildMappingProperties(
            IEnumerable<EntityProperty> entityPropertyTree,
            IEnumerable<EntityInfo> allDomainEntities)
        {
            var allEntities = allDomainEntities.ToList();
            var result = new List<MappingEntityProperty>();
            var allPropertiesFlat = GetAllPropertiesRecursively(entityPropertyTree).ToList();

            BuildMappingPropertiesRecursive(entityPropertyTree, result, allPropertiesFlat, allEntities);

            // Compute OptionRelationPropertyComposite for collection options, same as
            // DtoGeneratorViewModel.RefreshMappingProperties does after AddMappingProperties.
            foreach (MappingEntityProperty mp in result.Where(x => x.IsOptionCollection))
            {
                mp.OptionRelationPropertyComposite = allPropertiesFlat
                    .SingleOrDefault(x =>
                        x.ParentType == mp.ParentEntityType
                        && x.Type.Equals($"ICollection<{mp.OptionRelationType}>", StringComparison.OrdinalIgnoreCase))
                    ?.CompositeName;

                if (string.IsNullOrWhiteSpace(mp.OptionRelationPropertyComposite))
                {
                    consoleWriter.AddMessageLine(
                        $"ERROR: unable to find matching property of type ICollection<{mp.OptionRelationType}> in type {mp.ParentEntityType} to map {mp.EntityCompositeName}",
                        "red");
                }
            }

            return result;
        }

        // ── Static helpers (shared with DtoGeneratorViewModel) ────────────────

        /// <summary>
        /// Computes the mapping type for a given entity property.
        /// Mirrors <c>DtoGeneratorViewModel.ComputeMappingType</c>.
        /// </summary>
        public static string ComputeMappingType(EntityProperty entityProperty)
        {
            if (OptionCollectionsMappingTypes.Any(x => entityProperty.Type.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
                return Constants.BiaClassName.CollectionOptionDto;

            if (StandardMappingTypes.Any(x => entityProperty.Type.Replace("?", string.Empty).Equals(x, StringComparison.InvariantCultureIgnoreCase)))
                return entityProperty.Type;

            if (SpecialTypeToRemap.TryGetValue(entityProperty.Type, out string remapped))
                return remapped;

            return Constants.BiaClassName.OptionDto;
        }

        /// <summary>
        /// Extracts the inner type name from an option or collection type expression.
        /// E.g. "ICollection&lt;Status&gt;" → "Status", "Status" → "Status".
        /// Mirrors <c>DtoGeneratorViewModel.ExtractOptionType</c>.
        /// </summary>
        public static string ExtractOptionType(string optionType)
        {
            if (!optionType.Contains('<'))
                return optionType;

            return InnerTypeRegex().Match(optionType).Groups[1].Value;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Fills child properties of <paramref name="property"/> by looking up the property's type
        /// in the domain entity list (mirrors <c>DtoGeneratorViewModel.FillEntityProperties</c>).
        /// </summary>
        private static void FillEntityPropertyChildren(EntityProperty property, string rootPropertyType, List<EntityInfo> allEntities)
        {
            EntityInfo propertyInfo = allEntities.FirstOrDefault(e => e.Name == property.Type);
            if (propertyInfo == null)
                return;

            IEnumerable<EntityProperty> childProperties = propertyInfo.Properties
                .Where(p => p.Type != property.ParentType && p.Type != rootPropertyType)
                .Select(p => new EntityProperty
                {
                    Name = p.Name,
                    Type = p.Type,
                    CompositeName = $"{property.CompositeName}.{p.Name}",
                    ParentType = property.Type,
                });

            property.Properties.AddRange(childProperties);
            property.Properties.ForEach(p => FillEntityPropertyChildren(p, rootPropertyType, allEntities));
        }

        private void BuildMappingPropertiesRecursive(
            IEnumerable<EntityProperty> entityProperties,
            List<MappingEntityProperty> result,
            List<EntityProperty> allPropertiesFlat,
            List<EntityInfo> allEntities)
        {
            foreach (EntityProperty entityProperty in entityProperties)
            {
                if (entityProperty.IsSelected && !result.Any(x => x.EntityCompositeName == entityProperty.CompositeName))
                {
                    string mappingType = ComputeMappingType(entityProperty);

                    var mp = new MappingEntityProperty
                    {
                        EntityCompositeName = entityProperty.CompositeName,
                        EntityType = entityProperty.Type,
                        ParentEntityType = entityProperty.ParentType,
                        MappingType = mappingType,
                    };

                    // Set default MappingDateType (first available type for the property's type)
                    if (DateTypesByPropertyType.TryGetValue(mappingType.Replace("?", string.Empty), out List<string> dateTypes))
                    {
                        mp.MappingDateType = dateTypes.FirstOrDefault();
                    }

                    if (mp.IsOption || mp.IsOptionCollection)
                    {
                        mp.OptionType = ExtractOptionType(entityProperty.Type);
                        EntityInfo optionEntity = allEntities.FirstOrDefault(x => x.Name == mp.OptionType);

                        if (optionEntity != null)
                        {
                            mp.OptionDisplayProperty = optionEntity.Properties
                                .Select(x => x.Name)
                                .Where(x => !x.Equals("id", StringComparison.OrdinalIgnoreCase))
                                .FirstOrDefault();

                            if (mp.IsOption)
                            {
                                string optionBaseKeyType = optionEntity.BaseKeyType;
                                if (string.IsNullOrEmpty(optionBaseKeyType))
                                {
                                    consoleWriter.AddMessageLine(
                                        $"Unable to find base key type of related entity {optionEntity.Name}, the mapping for this property has been ignored.",
                                        "orange");
                                    continue;
                                }

                                mp.OptionIdProperty = optionEntity.Properties
                                    .Where(x => x.Type.Equals(optionBaseKeyType, StringComparison.OrdinalIgnoreCase))
                                    .Select(x => x.Name)
                                    .FirstOrDefault(x => x.Equals("id", StringComparison.OrdinalIgnoreCase));

                                // Find the entity's own ID property that references the option entity.
                                string optionEntityIdPropertyName = $"{entityProperty.CompositeName.Split('.').Last()}Id";
                                var candidateEntityIdProps = allPropertiesFlat
                                    .Where(x =>
                                        x.Type.Replace("?", string.Empty).Equals(optionBaseKeyType, StringComparison.OrdinalIgnoreCase)
                                        && !x.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                                    .Select(x => x.Name)
                                    .ToList();

                                if (candidateEntityIdProps.Count == 0)
                                {
                                    consoleWriter.AddMessageLine(
                                        $"Unable to find {optionBaseKeyType} ID property related to {mp.EntityCompositeName}, the mapping for this property has been ignored.",
                                        "orange");
                                    entityProperty.IsSelected = false;
                                    continue;
                                }

                                // Prefer the property matching the expected naming convention; otherwise take first.
                                mp.OptionEntityIdProperty =
                                    candidateEntityIdProps.FirstOrDefault(x => x.Equals(optionEntityIdPropertyName, StringComparison.OrdinalIgnoreCase))
                                    ?? candidateEntityIdProps.First();
                            }

                            if (mp.IsOptionCollection)
                            {
                                string optionRelationFirstType = entityProperty.ParentType;
                                string optionRelationSecondType = mp.OptionType;

                                var relationTypeClassNames = new List<string> { optionRelationFirstType, optionRelationSecondType };
                                EntityInfo relEntityInfo = allEntities.SingleOrDefault(x =>
                                    string.IsNullOrEmpty(x.BaseKeyType)
                                    && relationTypeClassNames.All(y => x.Properties.Select(p => p.Type).Contains(y)));

                                if (relEntityInfo is null)
                                {
                                    consoleWriter.AddMessageLine(
                                        $"Unable to find relation's entity between types {optionRelationFirstType} and {optionRelationSecondType} to map {mp.EntityCompositeName}, the mapping for this property has been ignored.",
                                        "orange");
                                    entityProperty.IsSelected = false;
                                    continue;
                                }

                                mp.OptionRelationType = relEntityInfo.Name;

                                string firstIdProp = optionRelationFirstType + "Id";
                                mp.OptionRelationFirstIdProperty = relEntityInfo.Properties
                                    .SingleOrDefault(x => x.Name.Equals(firstIdProp))?.Name;
                                if (string.IsNullOrWhiteSpace(mp.OptionRelationFirstIdProperty))
                                {
                                    consoleWriter.AddMessageLine(
                                        $"Unable to find matching relation property {firstIdProp} in the entity {relEntityInfo.Name} to map {mp.EntityCompositeName}, the mapping for this property has been ignored.",
                                        "orange");
                                    entityProperty.IsSelected = false;
                                    continue;
                                }

                                string secondIdProp = optionRelationSecondType + "Id";
                                mp.OptionRelationSecondIdProperty = relEntityInfo.Properties
                                    .SingleOrDefault(x => x.Name.Equals(secondIdProp))?.Name;
                                if (string.IsNullOrWhiteSpace(mp.OptionRelationSecondIdProperty))
                                {
                                    consoleWriter.AddMessageLine(
                                        $"Unable to find matching relation property {secondIdProp} in the entity {relEntityInfo.Name} to map {mp.EntityCompositeName}, the mapping for this property has been ignored.",
                                        "orange");
                                    entityProperty.IsSelected = false;
                                    continue;
                                }

                                mp.OptionIdProperty = optionEntity.Properties
                                    .Select(x => x.Name)
                                    .FirstOrDefault(x => x.Equals("id", StringComparison.OrdinalIgnoreCase));
                            }
                        }
                    }

                    result.Add(mp);
                }

                BuildMappingPropertiesRecursive(entityProperty.Properties, result, allPropertiesFlat, allEntities);
            }
        }

        private static IEnumerable<EntityProperty> GetAllPropertiesRecursively(IEnumerable<EntityProperty> properties)
        {
            foreach (EntityProperty p in properties)
            {
                yield return p;
                foreach (EntityProperty child in GetAllPropertiesRecursively(p.Properties))
                    yield return child;
            }
        }

        [GeneratedRegex(@"<\s*(\w+)\s*>")]
        private static partial Regex InnerTypeRegex();
    }
}
