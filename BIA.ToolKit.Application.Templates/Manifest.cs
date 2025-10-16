namespace BIA.ToolKit.Application.Templates
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class Manifest
    {
        public class Feature
        {
            public enum FeatureType
            {
                Dto,
                Option,
                Crud
            }

            public class Template : IEquatable<Template>
            {
                public string InputPath { get; set; }
                public string OutputPath { get; set; }
                public bool IsPartial { get; set; }
                public string PartialInsertionMarkup { get; set; }
                public bool UseDomainPartialInsertionMarkup { get; set; }

                public bool Equals(Template other)
                {
                    return 
                        InputPath == other.InputPath 
                        && OutputPath == other.OutputPath 
                        && IsPartial == other.IsPartial 
                        && other.PartialInsertionMarkup == PartialInsertionMarkup;
                }
            }

            [JsonConverter(typeof(StringEnumConverter))]
            public FeatureType Type { get; set; }
            public List<Template> DotNetTemplates { get; set; } = new List<Template>();
            public List<Template> AngularTemplates { get; set; } = new List<Template>();
        }

        public Version Version { get; set; }
        public List<Feature> Features { get; set; } = new List<Feature>();
    }
}
