using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BIA.ToolKit.Controls
{
    public class NumericTextBox : TextBox
    {
        private static readonly Regex _digitRegex = new Regex(@"^\d+$");

        public NumericTextBox()
        {
            PreviewTextInput += (s, e) => e.Handled = !_digitRegex.IsMatch(e.Text);
            DataObject.AddPastingHandler(this, OnPaste);
        }
        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text)) { e.CancelCommand(); return; }
            if (!_digitRegex.IsMatch((string)e.DataObject.GetData(DataFormats.Text))) e.CancelCommand();
        }
    }

}
