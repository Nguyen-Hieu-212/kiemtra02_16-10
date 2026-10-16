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

            // Vòng lặp: Đăng nhập -> frmMain. Nếu chọn "Đăng xuất" thì quay lại đăng nhập, "Thoát" thì dừng hẳn.
            while (true)
            {
                string user;
                using (var login = new frmLogin())
                {
                    if (login.ShowDialog() != DialogResult.OK) return;
                    user = login.UserName;
                }

                var main = new frmMain(user);
                Application.Run(main);
                bool logout = main.LogoutRequested;
                main.Dispose();

                if (!logout) break;
            }
        }
    }
}