// <copyright file="PlaneMapper.cs" company="TheBIADevCompany">
//     Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Domain.Fleet.Mappers
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Security.Principal;
    using BIA.Net.Core.Common.Extensions;
    using BIA.Net.Core.Domain;
    using BIA.Net.Core.Domain.Dto.Base;
    using BIA.Net.Core.Domain.Dto.Option;
    using TheBIADevCompany.BIADemo.Domain.Fleet.Entities;
    using TheBIADevCompany.BIADemo.Domain.Dto.Fleet;
    using TheBIADevCompany.BIADemo.Domain.User.Mappers;

    /// <summary>
    /// The mapper used for Plane.
    /// </summary>
    public class PlaneMapper : TTeamMapper<PlaneDto, Plane>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlaneMapper"/> class.
        /// </summary>
        /// <param name="principal">The principal.</param>
        public PlaneMapper(IPrincipal principal)
            : base(principal)
        {
        }

        /// <inheritdoc/>
        public override int TeamType => base.TeamType;
        /// <inheritdoc/>
        public override ExpressionCollection<Plane> ExpressionCollection
        {
            // It is not necessary to implement this function if you do not use the mapper for filtered list.
            // In BIADemo it is used only for Calc SpreadSheet.
            get
            {
                return new ExpressionCollection<Plane>
                {
                    { HeaderName.Id, x => x.Id },
                    { HeaderName.Name, x => x.Name },
                    { HeaderName.Option, x => x.Option != null ? x.Option.Name : null },
                };
            }
        }

        /// <inheritdoc/>
        public override void DtoToEntity(PlaneDto dto, Plane entity)
        {
            entity ??= new Plane();

            base.DtoToEntity(dto, entity);
            entity.Id = dto.Id;
            entity.Name = dto.Name;
            entity.OptionId = dto.Option.Id;
        }

        /// <inheritdoc/>
        public override Expression<Func<Plane, PlaneDto>> EntityToDto()
        {
            return base.EntityToDto().CombineMapping(entity => new PlaneDto
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
        public override Func<PlaneDto, object[]> DtoToRecord(List<string> headerNames = null)
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
