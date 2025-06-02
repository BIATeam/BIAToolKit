namespace BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings
{
    using System;
    using System.Collections.Generic;

    public class CRUDGeneration
    {
        public List<CRUDGenerationHistory> CRUDGenerationHistory { get; }

        public CRUDGeneration()
        {
            CRUDGenerationHistory = new();
        }
    }

    public class CRUDGenerationHistory : GenerationHistory
    {
        public string DisplayItem { get; set; }
        public List<string> OptionItems { get; set; }
        public DtoMapping Mapping { get; set; }
        public string Feature { get; set; }
        public bool HasParent { get; set; }
        public string ParentName { get; set; }
        public string ParentNamePlural { get; set; }
        public string Domain { get; set; }
        public string BiaFront { get; set; }
        public bool IsTeam { get; set; }
    }

    public class DtoMapping
    {
        public string Dto { get; set; }
        public string Type { get; set; }
    }
}
