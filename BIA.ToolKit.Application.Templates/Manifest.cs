namespace BIA.ToolKit.Application.Templates
{
    using System;
    using System.Collections.Generic;

    public class Manifest
    {
        public class Feature
        {
            public class Template
            {
                public string InputPath { get; set; }
                public string OutputPath { get; set; }
                public bool IsPartial { get; set; }
                public string PartialInsertionMarkup { get; set; }
            }

            public string Name { get; set; }
            public List<Template> DotNetTemplates { get; set; } = new List<Template>();
            public List<Template> AngularTemplates { get; set; } = new List<Template>();
        }

        public Version Version { get; set; }
        public List<Feature> Features { get; set; } = new List<Feature>();
    }
}
