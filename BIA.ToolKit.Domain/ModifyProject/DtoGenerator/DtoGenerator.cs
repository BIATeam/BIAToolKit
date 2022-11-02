namespace BIA.ToolKit.Domain.DtoGenerator
{
    using System.Collections.Generic;

    public class DtoGenerator
    {

        #region Properties
        /// The directory of the project.
        public string? ProjectDir { get; set; }

        /// The path of the project.
        public string? ProjectPath { get; set; }

        public List<EntityInfo>? Entities { get; private set; }
        #endregion
    }
}
