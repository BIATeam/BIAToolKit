﻿namespace BIA.ToolKit.Application.Helper
{
    public interface IConsoleWriter
    {
        public void AddMessageLine(string message, string color = null, bool refreshimediate = true);
    }
}