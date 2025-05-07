namespace BIA.ToolKit.Application.Templates._4_0_0.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Templates._4_0_0.Models;

    public class EntityDtoMock : EntityDtoModel
    {
        public EntityDtoMock()
        {
            CompanyName = "TheBIADevCompany";
            ProjectName = "BIADemo";
            EntityNameArticle = "a";
            DomainName = "Fleet";
            EntityName = "Plane";
            BaseKeyType = "int";
            AncestorTeam = "Site";
            Properties = new List<PropertyDtoModel>()
            {
                new PropertyDtoModel
                {
                    EntityCompositeName = "Id",
                    MappingType = "int",
                    MappingName = "Id",
                    IsRequired = true
                },
                new PropertyDtoModel
                {
                    EntityCompositeName = "Name",
                    MappingType = "string",
                    MappingName = "Name",
                    IsRequired = true,
                },
                new PropertyDtoModel
                {
                    EntityCompositeName = "Option",
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
