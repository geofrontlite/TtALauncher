using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Trails_To_Azure_Launcher.Utils
{
    class GUIUtils
    {
        private const double marginBetweenLines = 1.15D;

        public static Action<Window, String, bool> autoAdjustMargins = (Window window, String labelName, bool top) =>
        {
            window.Dispatcher.Invoke(() =>
            {
                Label label = (Label)window.FindName(labelName);
                double noLineHeight = label.FontSize - 6;
                double lineHeight = label.FontSize * marginBetweenLines;
                byte lines = (byte)Math.Round((label.ActualHeight - noLineHeight) / lineHeight);

                label.Margin = new Thickness(label.Margin.Left, (top == true) ? label.Margin.Top - ((lines - 1) * lineHeight) : 0, 0,
                    (top == false) ? label.Margin.Bottom - ((lines - 1) * lineHeight) : 0);
                Debug.WriteLine(label.Margin);
            });
        };

        
    }
}
