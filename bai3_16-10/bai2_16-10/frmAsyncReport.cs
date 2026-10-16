using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace bai2_16_10
{
    // Một dòng của báo cáo doanh thu
    public class RevenueRow
    {
        public string Month { get; set; }
        public string Category { get; set; }
        public int Orders { get; set; }
        public decimal Revenue { get; set; }
    }

    // Form con 2: Báo cáo Thống kê & Đồng bộ
    //  - Scenario B: tải báo cáo bất đồng bộ (async/await + Task.Run), UI không bị đơ, ProgressBar ở Form cha
    //  - Scenario C: luồng nền đồng bộ dữ liệu mỗi 2 giây, cập nhật UI an toàn bằng Invoke
    public class frmAsyncReport : Form
    {
        private readonly CultureInfo _vn = new CultureInfo("vi-VN");

        // ====== Phần báo cáo ======
        private SplitContainer split;
        private GroupBox grpReport, grpSync;
        private Panel pnlReportTop, pnlSyncTop;
        private Button btnLoadReport, btnToggleSync;
        private Label lblReportInfo, lblSyncState, lblRate, lblOrders;
        private DataGridView dgvReport, dgvSync;

        private CancellationTokenSource _reportCts = new CancellationTokenSource();
        private CancellationTokenSource _syncCts;   // != null nghĩa là đang đồng bộ

        public frmAsyncReport()
        {
            InitializeUI();
        }

        // =========================================================
        // GIAO DIỆN
        // =========================================================
        private void InitializeUI()
        {
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Báo cáo Thống kê & Đồng bộ";
            Size = new Size(900, 620);
            Font = new Font("Segoe UI", 10F);

            split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };

            // ---- Nhóm báo cáo ----
            grpReport = new GroupBox { Text = "Báo cáo doanh thu (tải bất đồng bộ)", Dock = DockStyle.Fill };
            dgvReport = NewGrid();
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tháng", DataPropertyName = nameof(RevenueRow.Month), FillWeight = 20 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Danh mục", DataPropertyName = nameof(RevenueRow.Category), FillWeight = 30 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Số đơn",
                DataPropertyName = nameof(RevenueRow.Orders),
                FillWeight = 15,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            var colRev = new DataGridViewTextBoxColumn { HeaderText = "Doanh thu", DataPropertyName = nameof(RevenueRow.Revenue), FillWeight = 35 };
            colRev.DefaultCellStyle.Format = "C0";
            colRev.DefaultCellStyle.FormatProvider = _vn;
            colRev.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvReport.Columns.Add(colRev);

            pnlReportTop = new Panel { Dock = DockStyle.Top, Height = 48 };
            btnLoadReport = new Button { Text = "Tải Báo Cáo Doanh Thu", Location = new Point(10, 8), Size = new Size(210, 34) };
            btnLoadReport.Click += btnLoadReport_Click;
            lblReportInfo = new Label { Text = "Chưa có dữ liệu.", Location = new Point(235, 15), AutoSize = true };
            pnlReportTop.Controls.AddRange(new Control[] { btnLoadReport, lblReportInfo });

            grpReport.Controls.Add(dgvReport);       // Fill add trước
            grpReport.Controls.Add(pnlReportTop);
            split.Panel1.Controls.Add(grpReport);

            // ---- Nhóm đồng bộ nền ----
            grpSync = new GroupBox { Text = "Đồng bộ dữ liệu thời gian thực (luồng nền, 2 giây/lần)", Dock = DockStyle.Fill };
            dgvSync = NewGrid();
            dgvSync.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thời gian", FillWeight = 20 });
            dgvSync.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tỷ giá USD/VND", FillWeight = 25 });
            dgvSync.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Đơn mới", FillWeight = 15 });
            dgvSync.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tổng đơn", FillWeight = 15 });
            dgvSync.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thread ID (nền)", FillWeight = 25 });

            pnlSyncTop = new Panel { Dock = DockStyle.Top, Height = 80 };
            btnToggleSync = new Button { Text = "Bật đồng bộ nền", Location = new Point(10, 8), Size = new Size(210, 34) };
            btnToggleSync.Click += btnToggleSync_Click;
            lblSyncState = new Label { Text = "Trạng thái: đã dừng", Location = new Point(235, 15), AutoSize = true };
            lblRate = new Label { Text = "Tỷ giá USD/VND: --", Location = new Point(10, 52), AutoSize = true };
            lblOrders = new Label { Text = "Tổng đơn hàng mới: --", Location = new Point(300, 52), AutoSize = true };
            pnlSyncTop.Controls.AddRange(new Control[] { btnToggleSync, lblSyncState, lblRate, lblOrders });

            grpSync.Controls.Add(dgvSync);
            grpSync.Controls.Add(pnlSyncTop);
            split.Panel2.Controls.Add(grpSync);

            Controls.Add(split);

            Load += (s, e) => split.SplitterDistance = split.Height / 2;
            FormClosing += frmAsyncReport_FormClosing;
        }

        private static DataGridView NewGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                EnableHeadersVisualStyles = false
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(230, 236, 245);
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            g.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            return g;
        }

        // =========================================================
        // SCENARIO B: TẢI BÁO CÁO BẤT ĐỒNG BỘ (async / await + Task.Run)
        // =========================================================
        private async void btnLoadReport_Click(object sender, EventArgs e)
        {
            var main = MdiParent as frmMain;
            var token = _reportCts.Token;

            btnLoadReport.Enabled = false;
            lblReportInfo.Text = "Đang tải dữ liệu...";
            main?.BeginBusy("Đang tải báo cáo doanh thu...");

            // Progress<T> được tạo trên UI Thread nên callback tự chạy lại trên UI Thread
            var progress = new Progress<int>(p => main?.SetProgress(p));

            try
            {
                // Đẩy tác vụ nặng xuống Thread phụ; UI Thread được giải phóng trong lúc await
                List<RevenueRow> data = await Task.Run(() => BuildRevenueReport(progress, token), token);

                // Sau await, code tiếp tục chạy trên UI Thread -> được phép đụng vào Control
                dgvReport.DataSource = new BindingList<RevenueRow>(data);

                decimal total = 0;
                foreach (var r in data) total += r.Revenue;
                lblReportInfo.Text = $"Đã tải {data.Count} dòng - Tổng doanh thu: {total.ToString("N0", _vn)} ₫";
            }
            catch (OperationCanceledException)
            {
                return;   // form đã bị đóng khi đang tải -> không đụng vào giao diện nữa
            }
            catch (Exception ex)
            {
                lblReportInfo.Text = "Tải báo cáo thất bại.";
                MessageBox.Show("Lỗi khi tải báo cáo: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                main?.EndBusy("Sẵn sàng");
                if (!IsDisposed) btnLoadReport.Enabled = true;
            }
        }

        // Chạy trên Thread phụ: tuyệt đối KHÔNG truy cập Control ở đây
        private static List<RevenueRow> BuildRevenueReport(IProgress<int> progress, CancellationToken token)
        {
            string[] categories = { "Laptop", "Điện thoại", "Máy tính bảng", "Màn hình", "Phụ kiện" };
            var rnd = new Random();
            var list = new List<RevenueRow>();

            int total = 12 * categories.Length;
            int done = 0;

            for (int month = 1; month <= 12; month++)
            {
                foreach (string cat in categories)
                {
                    token.ThrowIfCancellationRequested();

                    Thread.Sleep(60);   // giả lập truy xuất CSDL / tính toán nặng (~3,6 giây tổng cộng)

                    int orders = rnd.Next(20, 200);
                    decimal avgPrice = rnd.Next(2, 40) * 1000000m;
                    list.Add(new RevenueRow
                    {
                        Month = "Tháng " + month.ToString("D2"),
                        Category = cat,
                        Orders = orders,
                        Revenue = orders * avgPrice
                    });

                    done++;
                    progress.Report(done * 100 / total);
                }
            }
            return list;
        }

        // =========================================================
        // SCENARIO C: LUỒNG NỀN ĐỒNG BỘ + CẬP NHẬT UI AN TOÀN (Invoke)
        // =========================================================
        private void btnToggleSync_Click(object sender, EventArgs e)
        {
            if (_syncCts == null) StartSync();
            else StopSync();
        }

        private void StartSync()
        {
            _syncCts = new CancellationTokenSource();
            CancellationToken token = _syncCts.Token;

            btnToggleSync.Text = "Dừng đồng bộ nền";
            lblSyncState.Text = "Trạng thái: đang đồng bộ...";

            Task.Run(() => SyncLoop(token));   // vòng lặp định kỳ chạy trên Thread phụ
        }

        private void StopSync()
        {
            _syncCts?.Cancel();
            _syncCts = null;

            btnToggleSync.Text = "Bật đồng bộ nền";
            lblSyncState.Text = "Trạng thái: đã dừng";
        }

        // Chạy trên Thread phụ
        private void SyncLoop(CancellationToken token)
        {
            var rnd = new Random();
            decimal rate = 25400m;   // tỷ giá giả lập
            int totalOrders = 0;

            while (!token.IsCancellationRequested)
            {
                rate += rnd.Next(-30, 31);
                int newOrders = rnd.Next(0, 6);
                totalOrders += newOrders;

                try
                {
                    // Chỉ GỬI dữ liệu sang UI; việc đụng vào Control nằm trong ApplySyncResult (đã có Invoke)
                    ApplySyncResult(DateTime.Now, rate, newOrders, totalOrders, Thread.CurrentThread.ManagedThreadId);
                }
                catch (ObjectDisposedException) { break; }      // form đã đóng
                catch (InvalidOperationException) { break; }    // handle chưa có / đã hủy

                if (token.WaitHandle.WaitOne(2000)) break;       // ngủ 2 giây, thoát ngay nếu bị hủy
            }
        }

        // Quy tắc Thread Safety: nếu đang ở Thread phụ thì chuyển ngữ cảnh về UI Thread trước khi đụng Control
        private void ApplySyncResult(DateTime time, decimal rate, int newOrders, int totalOrders, int threadId)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(() => ApplySyncResult(time, rate, newOrders, totalOrders, threadId)));
                return;
            }

            // ---- Từ đây chắc chắn đang ở UI Thread ----
            dgvSync.Rows.Insert(0,
                time.ToString("HH:mm:ss"),
                rate.ToString("N0", _vn),
                "+" + newOrders,
                totalOrders,
                threadId);
            while (dgvSync.Rows.Count > 100)
                dgvSync.Rows.RemoveAt(dgvSync.Rows.Count - 1);

            lblRate.Text = "Tỷ giá USD/VND: " + rate.ToString("N0", _vn);
            lblOrders.Text = "Tổng đơn hàng mới: " + totalOrders;

            (MdiParent as frmMain)?.SetStatus("Đồng bộ lúc " + time.ToString("HH:mm:ss"));   // cập nhật StatusStrip của Form cha
        }

        // =========================================================
        // ĐÓNG FORM: dừng mọi luồng nền
        // =========================================================
        private void frmAsyncReport_FormClosing(object sender, FormClosingEventArgs e)
        {
            _reportCts.Cancel();
            _syncCts?.Cancel();
            _syncCts = null;
        }
    }
}