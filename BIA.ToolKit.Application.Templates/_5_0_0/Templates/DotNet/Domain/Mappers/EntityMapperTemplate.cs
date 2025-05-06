// <copyright file="PlaneMapper.cs" company="TheBIADevCompany">
// Copyright (c) TheBIADevCompany. All rights reserved.
// </copyright>

namespace TheBIADevCompany.BIADemo.Domain.Fleet.Mappers
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using BIA.Net.Core.Domain;
    using BIA.Net.Core.Domain.Dto.Option;
    using TheBIADevCompany.BIADemo.Domain.Dto.Fleet;
    using TheBIADevCompany.BIADemo.Domain.Fleet.Entities;

    /// <summary>
    /// The mapper used for Plane.
    /// </summary>
    public class PlaneMapper : BaseMapper<PlaneDto, Plane, int>
    {
        /// <inheritdoc cref="BaseMapper{TDto,TEntity}.ExpressionCollection"/>
        public override ExpressionCollection<Plane> ExpressionCollection
        {
            get
            {
                return new ExpressionCollection<Plane>
                {
                    { HeaderName.Id, plane => plane.Id },
                    { HeaderName.Name, plane => plane.Name },
                    { HeaderName.Option, plane => plane.Option != null ? plane.Option.Name : null },
                };
            }
        }

        /// <inheritdoc cref="BaseMapper{TDto,TEntity}.DtoToEntity"/>
        public override void DtoToEntity(PlaneDto dto, Plane entity)
        {
            entity ??= new Plane();
            entity.Id = dto.Id;
            entity.Name = dto.Name;

            // Map relationship 0..1-* : Option
            entity.OptionId = dto.Option?.Id;
        }

        /// <inheritdoc cref="BaseMapper{TDto,TEntity}.EntityToDto"/>
        public override Expression<Func<Plane, PlaneDto>> EntityToDto()
        {
            return entity => new PlaneDto
            {
                Id = entity.Id,
                Name = entity.Name,

                // Map relationship 0..1-* : Option
                Option = entity.Option != null ? new OptionDto
                {
                    Id = entity.Option.Id,
                    Display = entity.Option.Name,
                }
                : null,
                RowVersion = Convert.ToBase64String(entity.RowVersion),
            };
        }

        /// <inheritdoc cref="BaseMapper{TDto,TEntity}.DtoToRecord"/>
        public override Func<PlaneDto, object[]> DtoToRecord(List<string> headerNames = null)
        {
            return x =>
            {
                List<object> records = new List<object>();

                if (headerNames?.Any() == true)
                {
                    foreach (string headerName in headerNames)
                    {
                        if (string.Equals(headerName, HeaderName.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            records.Add(CSVNumber(x.Id));
                        }

                        if (string.Equals(headerName, HeaderName.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            records.Add(CSVString(x.Name));
                        }

                        if (string.Equals(headerName, HeaderName.Option, StringComparison.OrdinalIgnoreCase))
                        {
                            records.Add(CSVString(x.Option?.Display));
                        }
                    }
                }

                return records.ToArray();
            };
        }

        /// <summary>
        /// Header names.
        /// </summary>
        public struct HeaderName
        {
            /// <summary>
            /// Header name for Id.
            /// </summary>
            public const string Id = "id";

            /// <summary>
            /// Header name for Name.
            /// </summary>
            public const string Name = "name";

            /// <summary>
            /// Header name for Option.
            /// </summary>
            public const string Option = "option";
        }
    }
}
