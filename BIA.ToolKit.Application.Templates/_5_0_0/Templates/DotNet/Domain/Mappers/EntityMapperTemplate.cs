// <copyright file="MyEntityMapper.cs" company="MyCompany">
//     Copyright (c) MyCompany. All rights reserved.
// </copyright>

namespace MyCompany.MyProject.Domain.MyDomain.Mappers
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Security.Principal;
    using BIA.Net.Core.Common.Extensions;
    using BIA.Net.Core.Domain;
    using BIA.Net.Core.Domain.Dto.Base;
    using BIA.Net.Core.Domain.Dto.Option;
    using MyCompany.MyProject.Domain.MyDomain.Entities;
    using MyCompany.MyProject.Domain.Dto.MyDomain;
    using MyCompany.MyProject.Domain.User.Mappers;

    /// <summary>
    /// The mapper used for MyEntity.
    /// </summary>
    public class MyEntityMapper : TTeamMapper<MyEntityDto, MyEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MyEntityMapper"/> class.
        /// </summary>
        /// <param name="principal">The principal.</param>
        public MyEntityMapper(IPrincipal principal)
            : base(principal)
        {
        }

        /// <inheritdoc/>
        public override int TeamType => base.TeamType;
        /// <inheritdoc/>
        public override ExpressionCollection<MyEntity> ExpressionCollection
        {
            // It is not necessary to implement this function if you do not use the mapper for filtered list.
            // In BIADemo it is used only for Calc SpreadSheet.
            get
            {
                return new ExpressionCollection<MyEntity>
                {
                    { HeaderName.Id, x => x.Id },
                    { HeaderName.Name, x => x.Name },
                    { HeaderName.Option, x => x.Option != null ? x.Option.Name : null },
                };
            }
        }

        /// <inheritdoc/>
        public override void DtoToEntity(MyEntityDto dto, MyEntity entity)
        {
            entity ??= new MyEntity();

            base.DtoToEntity(dto, entity);
            entity.Id = dto.Id;
            entity.Name = dto.Name;
            entity.OptionId = dto.Option.Id;
        }

        /// <inheritdoc/>
        public override Expression<Func<MyEntity, MyEntityDto>> EntityToDto()
        {
            return base.EntityToDto().CombineMapping(entity => new MyEntityDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Option = entity.Option != null ? 
                  new OptionDto { Id = entity.Option.Id, Display = entity.Option.Name } :
                  null,
                TeamTypeId = this.TeamType,
            });
        }

        /// <inheritdoc/>
        public override Func<MyEntityDto, object[]> DtoToRecord(List<string> headerNames = null)
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
