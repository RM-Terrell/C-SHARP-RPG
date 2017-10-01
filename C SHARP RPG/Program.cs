using System;
using System.Windows.Forms;
using SA = SuperAdventure.SuperAdventure;

namespace C_SHARP_RPG
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SA());
        }
    }
}
