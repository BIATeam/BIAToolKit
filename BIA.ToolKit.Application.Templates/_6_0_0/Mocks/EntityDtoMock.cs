namespace BIA.ToolKit.Application.Templates._6_0_0.Mocks
{
    using System.Collections.Generic;
    using BIA.ToolKit.Application.Templates._6_0_0.Models;

    public class EntityDtoMock : EntityDtoModel<PropertyDtoModel>
    {
        public EntityDtoMock()
        {
            CompanyName = "TheBIADevCompany";
            ProjectName = "BIADemo";
            EntityNameArticle = "a";
            DomainName = "Fleet";
            EntityName = "Plane";
            BaseKeyType = "int";
            Properties = new List<PropertyDtoModel>()
            {
                new PropertyDtoModel
                {
                    EntityCompositeName = "Id",
                    EntityType = "int",
                    MappingType = "int",
                    MappingName = "Id",
                    IsRequired = true
                },
                new PropertyDtoModel
                {
                    EntityCompositeName = "Name",
                    EntityType = "string",
                    MappingType = "string",
                    MappingName = "Name",
                    IsRequired = true,
                },
                new PropertyDtoModel
                {
                    EntityCompositeName = "Option",
                    EntityType = "PlaneType",
                    MappingType = "OptionDto",
                    MappingName = "Option",
                    IsOption = true,
                    OptionType = "Option",
                    OptionDisplayProperty = "Name",
                    OptionIdProperty = "Id",
                    OptionEntityIdPropertyComposite = "OptionId"
                }
            };
        }
    }
}
