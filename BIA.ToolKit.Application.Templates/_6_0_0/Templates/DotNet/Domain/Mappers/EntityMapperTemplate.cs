// <copyright file="PlaneMapper.cs" company="TheBIADevCompany">
// Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Domain.Fleet.Mappers
{
    using System;
    using System.Linq.Expressions;
    using BIA.Net.Core.Common.Extensions;
    using BIA.Net.Core.Domain;
    using BIA.Net.Core.Domain.Dto.Option;
    using BIA.Net.Core.Domain.Mapper;
    using TheBIADevCompany.BIADemo.Domain.Dto.Fleet;
    using TheBIADevCompany.BIADemo.Domain.Fleet.Entities;

    /// <summary>
    /// The mapper used for Plane.
    /// </summary>
    public class PlaneMapper : BaseMapper<PlaneDto, Plane, int>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlaneMapper"/> class.
        /// </summary>
        /// <param name="auditMappers">The injected collection of <see cref="IAuditMapper"/>.</param>
        public PlaneMapper(IEnumerable<IAuditMapper> auditMappers)
        {
            this.AuditMapper = auditMappers.FirstOrDefault(x => x.EntityType == typeof(Plane));
        }

        /// <inheritdoc />
        public override ExpressionCollection<Plane> ExpressionCollection
        {
            get
            {
                return new ExpressionCollection<Plane>(base.ExpressionCollection)
                {
                    { HeaderName.Name, plane => plane.Name },
                    { HeaderName.Option, plane => plane.Option != null ? plane.Option.Name : null },
                };
            }
        }

        /// <inheritdoc />
        public override ExpressionCollection<Plane> ExpressionCollectionFilterIn
        {
            get
            {
                return new ExpressionCollection<Plane>(
                    base.ExpressionCollectionFilterIn,
                    new ExpressionCollection<Plane>()
                    {
                        { HeaderName.Option, plane => plane.Option.Id },
                    });
            }
        }

        /// <inheritdoc />
        public override void DtoToEntity(PlaneDto dto, ref Plane entity)
        {
            base.DtoToEntity(dto, ref entity);
            entity.Name = dto.Name;

            // Map relationship 0..1-* : Option
            entity.OptionId = dto.Option?.Id;
        }

        /// <inheritdoc />
        public override Expression<Func<Plane, PlaneDto>> EntityToDto()
        {
            return base.EntityToDto().CombineMapping(entity => new PlaneDto
            {
                Name = entity.Name,

                // Map relationship 0..1-* : Option
                Option = entity.Option != null ? new OptionDto
                {
                    Id = entity.Option.Id,
                    Display = entity.Option.Name,
                }
                : null,
            });
        }

        /// <inheritdoc />
        public override Dictionary<string, Func<string>> DtoToCellMapping(PlaneDto dto)
        {
            return new Dictionary<string, Func<string>>(base.DtoToCellMapping(dto))
            {
                { HeaderName.Name, () => CSVString(dto.Name) },
                { HeaderName.Option, () => CSVString(dto.Option?.Display) },
            };
        }

        /// <summary>
        /// Header names.
        /// </summary>
        public struct HeaderName
        {
            /// <summary>
            /// Header name for name.
            /// </summary>
            public const string Name = "name";

            /// <summary>
            /// Header name for option.
            /// </summary>
            public const string Option = "option";
        }
    }
}
