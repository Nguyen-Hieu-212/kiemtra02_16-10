using System;
using System.Drawing;
using System.Windows.Forms;

namespace bai2_16_10
{
    // Form cha MDI (frmMain): menu điều hướng + StatusStrip (người dùng, trạng thái, ProgressBar dùng chung)
    public class frmMain : Form
    {
        // ====== Menu ======
        private MenuStrip menuStrip;
        private ToolStripMenuItem mnuSystem, mnuLogout, mnuExit;
        private ToolStripMenuItem mnuCatalog, mnuProducts, mnuStockMovement;
        private ToolStripMenuItem mnuReport, mnuRevenue;
        private ToolStripMenuItem mnuWindow, mnuCascade, mnuTileH, mnuTileV;

        // ====== StatusStrip ======
        private StatusStrip statusStrip;
        private ToolStripStatusLabel tslUser, tslStatus, tslSpacer;
        private ToolStripProgressBar tspProgress;

        private readonly string _userName;
        private int _busyCount = 0;            // nhiều form con có thể cùng dùng ProgressBar
        private bool _logoutRequested = false;

        // Program.cs dùng cờ này để biết là Đăng xuất (quay lại màn hình đăng nhập) hay Thoát hẳn
        public bool LogoutRequested => _logoutRequested;

        public frmMain(string userName)
        {
            _userName = userName;
            InitializeUI();
        }

        private void InitializeUI()
        {
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Hệ thống quản lý siêu thị (MDI)";
            Size = new Size(1200, 780);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            Font = new Font("Segoe UI", 10F);

            IsMdiContainer = true;               // <-- bắt buộc: Form cha MDI

            BuildMenu();
            BuildStatusStrip();

            Controls.Add(statusStrip);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;

            FormClosing += frmMain_FormClosing;
        }

        private void BuildMenu()
        {
            menuStrip = new MenuStrip();

            // --- Hệ thống ---
            mnuSystem = new ToolStripMenuItem("Hệ thống");
            mnuLogout = new ToolStripMenuItem("Đăng xuất", null, mnuLogout_Click);
            mnuExit = new ToolStripMenuItem("Thoát", null, mnuExit_Click) { ShortcutKeys = Keys.Alt | Keys.F4 };
            mnuSystem.DropDownItems.Add(mnuLogout);
            mnuSystem.DropDownItems.Add(new ToolStripSeparator());
            mnuSystem.DropDownItems.Add(mnuExit);

            // --- Danh mục ---
            mnuCatalog = new ToolStripMenuItem("Danh mục");
            mnuProducts = new ToolStripMenuItem("Quản lý Sản phẩm", null, (s, e) => ShowChild<frmProductCatalog>());
            mnuStockMovement = new ToolStripMenuItem("Quản lý Nhập xuất", null, (s, e) =>
                MessageBox.Show("Chức năng Quản lý Nhập xuất chưa được xây dựng trong bài này.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information));
            mnuCatalog.DropDownItems.Add(mnuProducts);
            mnuCatalog.DropDownItems.Add(mnuStockMovement);

            // --- Báo cáo ---
            mnuReport = new ToolStripMenuItem("Báo cáo");
            mnuRevenue = new ToolStripMenuItem("Thống kê Doanh thu", null, (s, e) => ShowChild<frmAsyncReport>());
            mnuReport.DropDownItems.Add(mnuRevenue);

            // --- Cửa sổ ---
            mnuWindow = new ToolStripMenuItem("Cửa sổ");
            mnuCascade = new ToolStripMenuItem("Cascade", null, (s, e) => LayoutMdi(MdiLayout.Cascade));
            mnuTileH = new ToolStripMenuItem("Tile Horizontal", null, (s, e) => LayoutMdi(MdiLayout.TileHorizontal));
            mnuTileV = new ToolStripMenuItem("Tile Vertical", null, (s, e) => LayoutMdi(MdiLayout.TileVertical));
            mnuWindow.DropDownItems.Add(mnuCascade);
            mnuWindow.DropDownItems.Add(mnuTileH);
            mnuWindow.DropDownItems.Add(mnuTileV);

            menuStrip.Items.AddRange(new ToolStripItem[] { mnuSystem, mnuCatalog, mnuReport, mnuWindow });
            menuStrip.MdiWindowListItem = mnuWindow;   // menu Cửa sổ tự liệt kê các form con đang mở
        }

        private void BuildStatusStrip()
        {
            statusStrip = new StatusStrip();
            tslUser = new ToolStripStatusLabel("Người dùng: " + _userName) { BorderSides = ToolStripStatusLabelBorderSides.Right };
            tslStatus = new ToolStripStatusLabel("Sẵn sàng");
            tslSpacer = new ToolStripStatusLabel { Spring = true };
            tspProgress = new ToolStripProgressBar { Width = 220, Visible = false, Minimum = 0, Maximum = 100 };
            statusStrip.Items.AddRange(new ToolStripItem[] { tslUser, tslStatus, tslSpacer, tspProgress });
        }

        // =========================================================
        // Scenario A: Điều hướng MDI - đã mở thì Activate(), chưa mở thì tạo mới
        // =========================================================
        private void ShowChild<T>() where T : Form, new()
        {
            foreach (Form f in MdiChildren)
            {
                if (f is T)
                {
                    if (f.WindowState == FormWindowState.Minimized)
                        f.WindowState = FormWindowState.Normal;
                    f.Activate();
                    return;
                }
            }

            var child = new T();
            child.MdiParent = this;
            child.Show();
        }

        // =========================================================
        // API cho form con dùng ProgressBar / thanh trạng thái (chỉ gọi từ UI Thread)
        // =========================================================
        public void BeginBusy(string text)
        {
            _busyCount++;
            tslStatus.Text = text;
            tspProgress.Value = 0;
            tspProgress.Visible = true;
        }

        public void SetProgress(int percent)
        {
            if (_busyCount == 0) return;   // bỏ qua báo cáo tiến độ đến muộn
            tspProgress.Value = Math.Max(0, Math.Min(100, percent));
        }

        public void EndBusy(string text)
        {
            if (_busyCount > 0) _busyCount--;
            if (_busyCount == 0)
            {
                tspProgress.Visible = false;
                tspProgress.Value = 0;
                tslStatus.Text = text;
            }
        }

        // Cập nhật dòng trạng thái khi không có tác vụ nặng nào đang chạy
        public void SetStatus(string text)
        {
            if (_busyCount == 0) tslStatus.Text = text;
        }

        // =========================================================
        // Hệ thống: Đăng xuất / Thoát
        // =========================================================
        private void mnuLogout_Click(object sender, EventArgs e)
        {
            var rs = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất không?", "Xác nhận đăng xuất",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (rs != DialogResult.Yes) return;
            _logoutRequested = true;
            Close();
        }

        private void mnuExit_Click(object sender, EventArgs e)
        {
            Close();   // việc hỏi xác nhận nằm trong FormClosing
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_logoutRequested || e.CloseReason != CloseReason.UserClosing) return;

            var rs = MessageBox.Show("Bạn có chắc chắn muốn thoát chương trình không?", "Xác nhận thoát",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (rs == DialogResult.No) e.Cancel = true;
        }
    }
}