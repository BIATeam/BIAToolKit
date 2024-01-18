namespace BIA.ToolKit.Application.Settings
{
    using System;
    using System.Collections.Generic;

    public class CRUDGeneration
    {
        public List<CRUDGenerationHistory> CRUDGenerationHistory { get; set; }

        public CRUDGeneration()
        {
            CRUDGenerationHistory = new();
        }
    }

    public class CRUDGenerationHistory
    {
        public string EntityNameSingular { get; set; }
        public string EntityNamePlurial { get; set; }
        public DateTime Date { get; set; }
        public Mapping Mapping { get; set; }
        public List<Generation> Generation { get; set; }

        public CRUDGenerationHistory()
        {
            Generation = new();
        }
    }

    public class Generation
    {
        public string Type { get; set; }
        public string Folder { get; set; }
        public string Template { get; set; }
    }

    public class Mapping
    {
        public string Type { get; set; }
        public string Dto { get; set; }
        public string Template { get; set; }
    }
}
