namespace BIA.ToolKit.Application.TemplateGenerator._4_0_0.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.TemplateGenerator._4_0_0.Models;

    public class EntityMock : EntityModel
    {
        public EntityMock()
        {
            CompanyName = "Company";
            ProjectName = "Project";
            NameArticle = "an";
            DomainName = "Domain";
            DtoName = "Entity" + "Dto";
            EntityName = "Entity";
            BaseKeyType = "int";
            EntityNamespace = "Company.Project.Domain";
            MapperName = "EntityMapper";
            IsTeamType = false;
            Properties = new List<PropertyModel>()
            {
                new PropertyModel
                {
                    EntityCompositeName = "Id",
                    MappingType = "int",
                    MappingName = "Id",
                    IsRequired = true
                },
                new PropertyModel
                {
                    EntityCompositeName = "Name",
                    MappingType = "string",
                    MappingName = "Name",
                    IsRequired = true,
                },
                new PropertyModel
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
