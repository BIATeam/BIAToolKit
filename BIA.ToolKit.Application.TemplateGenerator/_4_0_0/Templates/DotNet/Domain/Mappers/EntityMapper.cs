// <copyright file="EntityMapper.cs" company="Company">
//     Copyright (c) Company. All rights reserved.
// </copyright>

namespace Company.Project.Domain.Domain.Mappers
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using BIA.Net.Core.Domain;
    using BIA.Net.Core.Domain.Dto.Base;
    using BIA.Net.Core.Domain.Dto.Option;
    using Company.Project.Domain;
    using Company.Project.Domain.Dto.Domain;

    /// <summary>
    /// The mapper used for Entity.
    /// </summary>
    public class EntityMapper : BaseMapper<EntityDto, Entity, int>
    {
        /// <inheritdoc/>
        public override ExpressionCollection<Entity> ExpressionCollection
        {
            // It is not necessary to implement this function if you do not use the mapper for filtered list.
            // In BIADemo it is used only for Calc SpreadSheet.
            get
            {
                return new ExpressionCollection<Entity>
                {
                    { HeaderName.Id, x => x.Id },
                    { HeaderName.Name, x => x.Name },
                    { HeaderName.Option, x => x.Option != null ? x.Option.Name : null },
                };
            }
        }

        /// <inheritdoc/>
        public override void DtoToEntity(EntityDto dto, Entity entity)
        {
            entity ??= new Entity();

            entity.Id = dto.Id;
            entity.Name = dto.Name;
            entity.OptionId = dto.Option.Id;
        }

        /// <inheritdoc/>
        public override Expression<Func<Entity, EntityDto>> EntityToDto()
        {
            return entity => new EntityDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Option = entity.Option != null ? 
                  new OptionDto { Id = entity.Option.Id, Display = entity.Option.Name } :
                  null,
            ;
        }

        /// <inheritdoc/>
        public override Func<EntityDto, object[]> DtoToRecord(List<string> headerNames = null)
        {
            return x => (new object[]
            {
                CSVNumber(x.Id),
                CSVString(x.Name),
            });
        }

        /// <summary>
        /// Header Names.
        /// </summary>
        private struct HeaderName
        {
            /// <summary>
            /// Header Name Id.
            /// </summary>
            public const string Id = "id";
            /// <summary>
            /// Header Name Name.
            /// </summary>
            public const string Name = "name";
            /// <summary>
            /// Header Name Option.
            /// </summary>
            public const string Option = "option";
        }
    }
}
