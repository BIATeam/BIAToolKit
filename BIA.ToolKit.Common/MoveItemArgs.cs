namespace BIA.ToolKit.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class MoveItemArgs
    {
        public int OldIndex { get; }
        public int NewIndex { get; }

        public MoveItemArgs(int oldIndex, int newIndex)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }
    }
}
