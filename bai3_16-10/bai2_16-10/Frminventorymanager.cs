using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace bai2_16_10
{
    public class frmProductCatalog : Form
    {
        // ====== Thanh điều khiển ======
        private MenuStrip menuStrip;
        private ToolStripMenuItem mnuSystem, mnuOpenFile, mnuSaveReport, mnuExit;
        private ToolStripMenuItem mnuTools, mnuImportCsv, mnuExportCsv;

        private ToolStrip toolStrip;
        private ToolStripButton tsbOpen, tsbSaveReport, tsbImport, tsbExport, tsbExit;

        private StatusStrip statusStrip;
        private ToolStripStatusLabel tslCount, tslQuantity, tslValue, tslSpacer, tslTime;

        // ====== Khu vực nhập liệu ======
        private Panel pnlMain;
        private GroupBox grpInput;
        private Label lblId, lblName, lblCategory, lblPrice, lblQuantity;
        private TextBox txtProductId, txtProductName, txtPrice;
        private ComboBox cboCategory;
        private NumericUpDown numQuantity;
        private PictureBox picProductImage;
        private Button btnChooseImage;
        private Button btnAdd, btnUpdate, btnDelete, btnClear;
        private DataGridView dgvProducts;
        private ErrorProvider errorProvider;
        private System.Windows.Forms.Timer _clock;

        // ====== Dữ liệu ======
        private readonly BindingList<Product> _products = new BindingList<Product>();
        private readonly BindingSource _bs = new BindingSource();
        private readonly CultureInfo _vn = new CultureInfo("vi-VN");
        private bool _loading = false;
        private string _imagePath = null;   // đường dẫn ảnh đang chọn trên form

        public frmProductCatalog()
        {
            InitializeUI();
            InitializeData();
        }

        // =========================================================
        // 1. GIAO DIỆN (toàn bộ bằng code)
        // =========================================================
        private void InitializeUI()
        {
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;   // tự co giãn theo độ phóng đại màn hình
            Text = "Quản lý kho và xuất báo cáo tổng hợp";
            Size = new Size(1000, 700);
            MinimumSize = new Size(1000, 640);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10F);

            errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

            BuildMenuStrip();
            BuildToolStrip();
            BuildStatusStrip();
            BuildMainPanel();

            // Thứ tự Add quan trọng để Dock hiển thị đúng
            Controls.Add(pnlMain);
            Controls.Add(statusStrip);
            Controls.Add(toolStrip);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;

            FormClosing += frmInventoryManager_FormClosing;

            // Đồng hồ thời gian thực
            _clock = new System.Windows.Forms.Timer { Interval = 1000 };
            _clock.Tick += (s, e) => tslTime.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            _clock.Start();
            tslTime.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        // Tạo icon đơn giản bằng code (hình tròn màu + ký hiệu)
        private static Bitmap MakeIcon(string glyph, Color color)
        {
            var bmp = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                using (var br = new SolidBrush(color))
                    g.FillEllipse(br, 1, 1, 22, 22);
                using (var f = new Font("Segoe UI Symbol", 11F, FontStyle.Bold))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString(glyph, f, Brushes.White, new RectangleF(0, 0, 24, 25), sf);
            }
            return bmp;
        }

        private void BuildMenuStrip()
        {
            Image icOpen = MakeIcon("▤", Color.SteelBlue);
            Image icReport = MakeIcon("≡", Color.SeaGreen);
            Image icImport = MakeIcon("↓", Color.DarkOrange);
            Image icExport = MakeIcon("↑", Color.MediumPurple);
            Image icExit = MakeIcon("✕", Color.Firebrick);

            menuStrip = new MenuStrip();

            mnuSystem = new ToolStripMenuItem("System");
            mnuOpenFile = new ToolStripMenuItem("Open File", icOpen, mnuOpenFile_Click) { ShortcutKeys = Keys.Control | Keys.O };
            mnuSaveReport = new ToolStripMenuItem("Save Report", icReport, mnuSaveReport_Click) { ShortcutKeys = Keys.Control | Keys.S };
            mnuExit = new ToolStripMenuItem("Exit", icExit, mnuExit_Click);
            mnuSystem.DropDownItems.Add(mnuOpenFile);
            mnuSystem.DropDownItems.Add(mnuSaveReport);
            mnuSystem.DropDownItems.Add(new ToolStripSeparator());
            mnuSystem.DropDownItems.Add(mnuExit);

            mnuTools = new ToolStripMenuItem("Tools");
            mnuImportCsv = new ToolStripMenuItem("Import CSV", icImport, mnuImportCsv_Click) { ShortcutKeys = Keys.Control | Keys.I };
            mnuExportCsv = new ToolStripMenuItem("Export CSV", icExport, mnuExportCsv_Click) { ShortcutKeys = Keys.Control | Keys.E };
            mnuTools.DropDownItems.Add(mnuImportCsv);
            mnuTools.DropDownItems.Add(mnuExportCsv);

            menuStrip.Items.Add(mnuSystem);
            menuStrip.Items.Add(mnuTools);
        }

        private void BuildToolStrip()
        {
            toolStrip = new ToolStrip { ImageScalingSize = new Size(24, 24) };

            tsbOpen = MakeToolButton("Open", "Mở file dữ liệu", MakeIcon("▤", Color.SteelBlue), mnuOpenFile_Click);
            tsbSaveReport = MakeToolButton("Report", "Lưu báo cáo", MakeIcon("≡", Color.SeaGreen), mnuSaveReport_Click);
            tsbImport = MakeToolButton("Import", "Nhập từ CSV", MakeIcon("↓", Color.DarkOrange), mnuImportCsv_Click);
            tsbExport = MakeToolButton("Export", "Xuất ra CSV", MakeIcon("↑", Color.MediumPurple), mnuExportCsv_Click);
            tsbExit = MakeToolButton("Exit", "Thoát chương trình", MakeIcon("✕", Color.Firebrick), mnuExit_Click);

            toolStrip.Items.Add(tsbOpen);
            toolStrip.Items.Add(tsbSaveReport);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(tsbImport);
            toolStrip.Items.Add(tsbExport);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(tsbExit);
        }

        private ToolStripButton MakeToolButton(string text, string tip, Image icon, EventHandler onClick)
        {
            var b = new ToolStripButton(text, icon, onClick)
            {
                ToolTipText = tip,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                TextImageRelation = TextImageRelation.ImageAboveText
            };
            return b;
        }

        private void BuildStatusStrip()
        {
            statusStrip = new StatusStrip();
            tslCount = new ToolStripStatusLabel("Số sản phẩm: 0") { BorderSides = ToolStripStatusLabelBorderSides.Right };
            tslQuantity = new ToolStripStatusLabel("Tổng số lượng: 0") { BorderSides = ToolStripStatusLabelBorderSides.Right };
            tslValue = new ToolStripStatusLabel("Tổng giá trị kho: 0 ₫");
            tslSpacer = new ToolStripStatusLabel { Spring = true };
            tslTime = new ToolStripStatusLabel();
            statusStrip.Items.AddRange(new ToolStripItem[] { tslCount, tslQuantity, tslValue, tslSpacer, tslTime });
        }

        private void BuildMainPanel()
        {
            pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 8) };
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 250 };   // vùng nhập liệu + nút

            grpInput = new GroupBox
            {
                Text = "Thông tin sản phẩm",
                Location = new Point(0, 8),
                Size = new Size(950, 190),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblId = new Label { Text = "Mã sản phẩm:", Location = new Point(20, 35), AutoSize = true };
            txtProductId = new TextBox { Name = "txtProductId", Location = new Point(135, 32), Width = 210, ReadOnly = true, BackColor = SystemColors.Control };

            lblName = new Label { Text = "Tên sản phẩm:", Location = new Point(20, 75), AutoSize = true };
            txtProductName = new TextBox { Name = "txtProductName", Location = new Point(135, 72), Width = 210 };

            lblCategory = new Label { Text = "Loại sản phẩm:", Location = new Point(20, 115), AutoSize = true };
            cboCategory = new ComboBox { Name = "cboCategory", Location = new Point(135, 112), Width = 210, DropDownStyle = ComboBoxStyle.DropDownList };
            cboCategory.Items.AddRange(new object[] { "Laptop", "Điện thoại", "Máy tính bảng", "Màn hình", "Phụ kiện" });

            lblPrice = new Label { Text = "Đơn giá (VNĐ):", Location = new Point(380, 35), AutoSize = true };
            txtPrice = new TextBox { Name = "txtPrice", Location = new Point(500, 32), Width = 190 };

            lblQuantity = new Label { Text = "Số lượng tồn:", Location = new Point(380, 75), AutoSize = true };
            numQuantity = new NumericUpDown { Name = "numQuantity", Location = new Point(500, 72), Width = 190, Minimum = 0, Maximum = 1000000 };

            picProductImage = new PictureBox
            {
                Name = "picProductImage",
                Location = new Point(730, 22),
                Size = new Size(200, 120),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };
            btnChooseImage = new Button { Name = "btnChooseImage", Text = "Chọn ảnh", Location = new Point(730, 148), Size = new Size(200, 32) };
            btnChooseImage.Click += btnChooseImage_Click;

            grpInput.Controls.AddRange(new Control[]
            {
                lblId, txtProductId, lblName, txtProductName, lblCategory, cboCategory,
                lblPrice, txtPrice, lblQuantity, numQuantity, picProductImage, btnChooseImage
            });

            btnAdd = MakeButton("btnAdd", "Thêm mới", 12, btnAdd_Click);
            btnUpdate = MakeButton("btnUpdate", "Cập nhật", 132, btnUpdate_Click);
            btnDelete = MakeButton("btnDelete", "Xóa", 252, btnDelete_Click);
            btnClear = MakeButton("btnClear", "Làm mới / Hủy", 372, btnClear_Click);
            btnClear.Width = 140;

            dgvProducts = new DataGridView
            {
                Name = "dgvProducts",
                Dock = DockStyle.Fill,   // bảng tự lấp đầy phần còn lại khi thu nhỏ / phóng to
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            ConfigureGrid();

            // Validation khi rời ô
            txtProductName.Validating += (s, e) => ValidateName();
            cboCategory.Validating += (s, e) => ValidateCategory();
            cboCategory.SelectedIndexChanged += (s, e) => { if (!_loading) ValidateCategory(); };
            txtPrice.Validating += (s, e) => ValidatePrice(out _);

            pnlTop.Controls.AddRange(new Control[] { grpInput, btnAdd, btnUpdate, btnDelete, btnClear });
            pnlMain.Controls.Add(dgvProducts);   // Fill add trước
            pnlMain.Controls.Add(pnlTop);
        }

        private Button MakeButton(string name, string text, int x, EventHandler onClick)
        {
            var b = new Button { Name = name, Text = text, Location = new Point(x - 12, 205), Size = new Size(110, 36) };
            b.Click += onClick;
            return b;
        }

        // Cấu hình cột thủ công bằng code. Cột ảnh chỉ hiển thị ĐƯỜNG DẪN (không nạp ảnh vào dòng)
        private void ConfigureGrid()
        {
            dgvProducts.AutoGenerateColumns = false;
            dgvProducts.ReadOnly = true;
            dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProducts.MultiSelect = false;
            dgvProducts.AllowUserToAddRows = false;
            dgvProducts.AllowUserToDeleteRows = false;
            dgvProducts.RowHeadersVisible = false;
            dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProducts.EnableHeadersVisualStyles = false;
            dgvProducts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(230, 236, 245);
            dgvProducts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvProducts.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "Mã SP", DataPropertyName = nameof(Product.Id), FillWeight = 10 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Tên sản phẩm", DataPropertyName = nameof(Product.Name), FillWeight = 24 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategory", HeaderText = "Loại", DataPropertyName = nameof(Product.Category), FillWeight = 13 });

            var colPrice = new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "Đơn giá", DataPropertyName = nameof(Product.Price), FillWeight = 15 };
            colPrice.DefaultCellStyle.Format = "C0";
            colPrice.DefaultCellStyle.FormatProvider = _vn;
            colPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvProducts.Columns.Add(colPrice);

            var colQty = new DataGridViewTextBoxColumn { Name = "colQuantity", HeaderText = "Tồn kho", DataPropertyName = nameof(Product.Quantity), FillWeight = 9 };
            colQty.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvProducts.Columns.Add(colQty);

            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colImagePath", HeaderText = "Đường dẫn ảnh", DataPropertyName = nameof(Product.ImagePath), FillWeight = 29 });
        }

        // =========================================================
        // 2. DỮ LIỆU BAN ĐẦU
        // =========================================================
        private void InitializeData()
        {
            // Dữ liệu mẫu (có thể xóa 3 dòng này nếu muốn danh sách trống lúc mở)

            _bs.DataSource = _products;
            dgvProducts.DataSource = _bs;

            // Mỗi khi danh sách thay đổi (Thêm/Sửa/Xóa/Import) -> cập nhật StatusStrip
            _products.ListChanged += (s, e) => UpdateStatus();

            dgvProducts.SelectionChanged += (s, e) => LoadCurrentToForm();
            dgvProducts.CellClick += (s, e) => LoadCurrentToForm();

            Shown += (s, e) => ClearForm();   // tránh Grid tự chọn dòng đầu khi mở form

            UpdateStatus();
            ClearForm();
        }

        private void UpdateStatus()
        {
            int totalQty = 0;
            decimal totalValue = 0;
            foreach (var p in _products)
            {
                totalQty += p.Quantity;
                totalValue += p.Price * p.Quantity;
            }
            tslCount.Text = "Số sản phẩm: " + _products.Count;
            tslQuantity.Text = "Tổng số lượng: " + totalQty.ToString("N0", _vn);
            tslValue.Text = "Tổng giá trị kho: " + totalValue.ToString("N0", _vn) + " ₫";
        }

        // =========================================================
        // 3. ĐỒNG BỘ GRID -> FORM
        // =========================================================
        private void LoadCurrentToForm()
        {
            if (_loading) return;
            if (dgvProducts.SelectedRows.Count == 0) return;

            var p = _bs.Current as Product;
            if (p == null) return;

            _loading = true;
            txtProductId.Text = p.Id;
            txtProductName.Text = p.Name;
            cboCategory.SelectedItem = p.Category;
            txtPrice.Text = p.Price.ToString("N0", _vn);
            numQuantity.Value = Math.Min(numQuantity.Maximum, Math.Max(numQuantity.Minimum, p.Quantity));
            _imagePath = p.ImagePath;
            SetPicture(LoadImageSafe(_imagePath));
            _loading = false;

            errorProvider.Clear();
        }

        // =========================================================
        // 4. XỬ LÝ HÌNH ẢNH (OpenFileDialog)
        // =========================================================
        private void btnChooseImage_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Chọn ảnh sản phẩm";
                ofd.Filter = "Ảnh (*.jpg;*.png;*.bmp)|*.jpg;*.png;*.bmp";
                ofd.Multiselect = false;

                if (ofd.ShowDialog() != DialogResult.OK) return;

                Image img = LoadImageSafe(ofd.FileName);
                if (img == null)
                {
                    MessageBox.Show("Không thể mở file ảnh đã chọn!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                _imagePath = ofd.FileName;   // lưu đường dẫn (sẽ gán vào Product khi Thêm/Cập nhật)
                SetPicture(img);
            }
        }

        // Đọc ảnh qua stream để không khóa file trên đĩa
        private static Image LoadImageSafe(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var tmp = Image.FromStream(fs))
                    return new Bitmap(tmp);
            }
            catch
            {
                return null;
            }
        }

        private void SetPicture(Image img)
        {
            var old = picProductImage.Image;
            picProductImage.Image = img;
            if (old != null) old.Dispose();
        }

        // =========================================================
        // 5. VALIDATION
        // =========================================================
        private bool ValidateName()
        {
            if (string.IsNullOrWhiteSpace(txtProductName.Text))
            {
                errorProvider.SetError(txtProductName, "Tên sản phẩm không được để trống!");
                return false;
            }
            errorProvider.SetError(txtProductName, "");
            return true;
        }

        private bool ValidateCategory()
        {
            if (cboCategory.SelectedIndex < 0)
            {
                errorProvider.SetError(cboCategory, "Vui lòng chọn loại sản phẩm!");
                return false;
            }
            errorProvider.SetError(cboCategory, "");
            return true;
        }

        private static bool TryParsePrice(string text, out decimal price)
        {
            string raw = (text ?? "").Replace(".", "").Replace(",", "").Replace(" ", "").Replace("₫", "").Trim();
            bool ok = decimal.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out price) && price > 0;
            if (!ok) price = 0;
            return ok;
        }

        private bool ValidatePrice(out decimal price)
        {
            if (!TryParsePrice(txtPrice.Text, out price))
            {
                errorProvider.SetError(txtPrice, "Đơn giá phải là số hợp lệ và lớn hơn 0!");
                return false;
            }
            errorProvider.SetError(txtPrice, "");
            return true;
        }

        private bool ValidateAll(out decimal price)
        {
            bool okName = ValidateName();
            bool okCat = ValidateCategory();
            bool okPrice = ValidatePrice(out price);
            return okName && okCat && okPrice;
        }

        // =========================================================
        // 6. THÊM / SỬA / XÓA / LÀM MỚI
        // =========================================================
        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateAll(out decimal price)) return;

            _products.Add(new Product
            {
                Id = GenerateId(),
                Name = txtProductName.Text.Trim(),
                Category = cboCategory.SelectedItem.ToString(),
                Price = price,
                Quantity = (int)numQuantity.Value,
                ImagePath = _imagePath
            });

            MessageBox.Show("Thêm sản phẩm thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearForm();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            var p = _bs.Current as Product;
            if (p == null || dgvProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một sản phẩm trên danh sách để cập nhật!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!ValidateAll(out decimal price)) return;

            p.Name = txtProductName.Text.Trim();
            p.Category = cboCategory.SelectedItem.ToString();
            p.Price = price;
            p.Quantity = (int)numQuantity.Value;
            p.ImagePath = _imagePath;

            MessageBox.Show("Cập nhật thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var p = _bs.Current as Product;
            if (p == null || dgvProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một sản phẩm để xóa!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var rs = MessageBox.Show($"Bạn có chắc chắn muốn xóa sản phẩm \"{p.Name}\" không?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (rs == DialogResult.Yes)
            {
                _products.Remove(p);
                ClearForm();
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            _loading = true;
            txtProductId.Text = GenerateId();
            txtProductName.Clear();
            cboCategory.SelectedIndex = -1;
            txtPrice.Clear();
            numQuantity.Value = 0;
            _imagePath = null;
            SetPicture(null);
            dgvProducts.ClearSelection();
            _loading = false;

            errorProvider.Clear();
            txtProductName.Focus();
        }

        private string GenerateId()
        {
            int max = 0;
            foreach (var p in _products)
            {
                if (p.Id != null && p.Id.StartsWith("SP") && int.TryParse(p.Id.Substring(2), out int n))
                    max = Math.Max(max, n);
            }
            return "SP" + (max + 1).ToString("D3");
        }

        // =========================================================
        // 7. XUẤT / NHẬP CSV, BÁO CÁO (SaveFileDialog & OpenFileDialog)
        // =========================================================
        private const string CsvHeader = "Ma;Ten;Loai;DonGia;SoLuong;DuongDanAnh";

        // Export CSV: phân cách bằng dấu chấm phẩy (;)
        private void mnuExportCsv_Click(object sender, EventArgs e)
        {
            if (_products.Count == 0)
            {
                MessageBox.Show("Kho đang trống, không có dữ liệu để xuất!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "Xuất báo cáo CSV";
                sfd.Filter = "CSV (*.csv)|*.csv";
                sfd.FileName = "BaoCaoKho.csv";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine(CsvHeader);
                    foreach (var p in _products)
                    {
                        sb.AppendLine(string.Join(";",
                            CsvEscape(p.Id),
                            CsvEscape(p.Name),
                            CsvEscape(p.Category),
                            p.Price.ToString("0", CultureInfo.InvariantCulture),
                            p.Quantity.ToString(CultureInfo.InvariantCulture),
                            CsvEscape(p.ImagePath)));
                    }
                    File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true)); // UTF-8 BOM để Excel đọc đúng tiếng Việt
                    MessageBox.Show("Xuất CSV thành công!\n" + sfd.FileName, "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể ghi file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void mnuImportCsv_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Nhập dữ liệu từ CSV";
                ofd.Filter = "CSV (*.csv)|*.csv|Tất cả file (*.*)|*.*";
                if (ofd.ShowDialog() != DialogResult.OK) return;
                ImportFromFile(ofd.FileName);
            }
        }

        // System > Open File: mở file dữ liệu (CSV/TXT) và nạp vào danh sách
        private void mnuOpenFile_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Mở file dữ liệu kho";
                ofd.Filter = "File dữ liệu (*.csv;*.txt)|*.csv;*.txt|Tất cả file (*.*)|*.*";
                if (ofd.ShowDialog() != DialogResult.OK) return;
                ImportFromFile(ofd.FileName);
            }
        }

        private void ImportFromFile(string path)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể đọc file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Xác định dấu phân cách: ; hoặc ,
            char sep = ';';
            foreach (var l in lines)
            {
                if (string.IsNullOrWhiteSpace(l)) continue;
                sep = l.Contains(";") ? ';' : ',';
                break;
            }

            var imported = new List<Product>();
            int skipped = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var f = SplitCsv(line, sep);
                // Dòng tiêu đề (cột giá không phải số) -> bỏ qua
                if (f.Count >= 5 && !TryParsePrice(f[3], out _) && imported.Count == 0 && i == 0) continue;

                if (f.Count < 5 ||
                    string.IsNullOrWhiteSpace(f[1]) ||
                    !TryParsePrice(f[3], out decimal price) ||
                    !int.TryParse(f[4].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int qty))
                {
                    skipped++;
                    continue;
                }

                imported.Add(new Product
                {
                    Id = f[0].Trim(),
                    Name = f[1].Trim(),
                    Category = f[2].Trim(),
                    Price = price,
                    Quantity = qty,
                    ImagePath = f.Count > 5 && !string.IsNullOrWhiteSpace(f[5]) ? f[5].Trim() : null
                });
            }

            if (imported.Count == 0)
            {
                MessageBox.Show("Không có dòng dữ liệu hợp lệ trong file!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Nạp lại vào BindingList<T>
            _products.RaiseListChangedEvents = false;
            _products.Clear();
            int seq = 0;
            foreach (var p in imported)
            {
                seq++;
                if (string.IsNullOrWhiteSpace(p.Id)) p.Id = "SP" + seq.ToString("D3");
                _products.Add(p);
            }
            _products.RaiseListChangedEvents = true;
            _products.ResetBindings();

            ClearForm();
            MessageBox.Show($"Đã nhập {imported.Count} sản phẩm" + (skipped > 0 ? $", bỏ qua {skipped} dòng lỗi." : "."),
                "Kết quả nhập", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // System > Save Report: lưu báo cáo tổng hợp dạng văn bản
        private void mnuSaveReport_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "Lưu báo cáo tổng hợp";
                sfd.Filter = "Text (*.txt)|*.txt";
                sfd.FileName = "BaoCaoTongHop.txt";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    int totalQty = 0;
                    decimal totalValue = 0;
                    var sb = new StringBuilder();
                    sb.AppendLine("BAO CAO TONG HOP KHO HANG");
                    sb.AppendLine("Thoi gian: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                    sb.AppendLine(new string('-', 70));
                    sb.AppendLine(string.Format("{0,-8}{1,-28}{2,-14}{3,14}{4,8}", "Ma", "Ten", "Loai", "Don gia", "SL"));
                    foreach (var p in _products)
                    {
                        sb.AppendLine(string.Format("{0,-8}{1,-28}{2,-14}{3,14}{4,8}",
                            p.Id, Cut(p.Name, 26), Cut(p.Category, 12), p.Price.ToString("N0", _vn), p.Quantity));
                        totalQty += p.Quantity;
                        totalValue += p.Price * p.Quantity;
                    }
                    sb.AppendLine(new string('-', 70));
                    sb.AppendLine("So san pham   : " + _products.Count);
                    sb.AppendLine("Tong so luong : " + totalQty.ToString("N0", _vn));
                    sb.AppendLine("Tong gia tri  : " + totalValue.ToString("N0", _vn) + " VND");

                    File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));
                    MessageBox.Show("Lưu báo cáo thành công!\n" + sfd.FileName, "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể ghi file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static string Cut(string s, int len)
        {
            s = s ?? "";
            return s.Length <= len ? s : s.Substring(0, len);
        }

        private static string CsvEscape(string s)
        {
            s = s ?? "";
            if (s.Contains(";") || s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        private static List<string> SplitCsv(string line, char sep)
        {
            var res = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else sb.Append(c);
                }
                else
                {
                    if (c == '"') inQuotes = true;
                    else if (c == sep) { res.Add(sb.ToString()); sb.Clear(); }
                    else sb.Append(c);
                }
            }
            res.Add(sb.ToString());
            return res;
        }

        // =========================================================
        // 8. THOÁT CHƯƠNG TRÌNH (MessageBox Yes/No)
        // =========================================================
        private void mnuExit_Click(object sender, EventArgs e)
        {
            Close();   // việc hỏi xác nhận được xử lý trong FormClosing
        }

        private void frmInventoryManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Là form con MDI thì không hỏi thoát chương trình (frmMain sẽ hỏi)
            if (MdiParent == null && e.CloseReason == CloseReason.UserClosing)
            {
                var rs = MessageBox.Show("Bạn có chắc chắn muốn thoát chương trình không?",
                    "Xác nhận thoát", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (rs == DialogResult.No) { e.Cancel = true; return; }
            }
            _clock.Stop();
        }
    }
}