using System;
using System.Windows.Forms;

namespace bai2_16_10
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new frmInventoryManager());
        }
    }
}