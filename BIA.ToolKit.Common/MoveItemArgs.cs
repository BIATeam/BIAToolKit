namespace BIA.ToolKit.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class MoveItemArgs(int oldIndex, int newIndex)
    {
        public int OldIndex { get; } = oldIndex;
        public int NewIndex { get; } = newIndex;
    }
}
