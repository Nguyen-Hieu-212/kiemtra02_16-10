using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace DeviceManager
{
    public class frmDeviceManager : Form
    {
        // ====== Controls ======
        private GroupBox grpInput;
        private Label lblId, lblName, lblCategory, lblPrice, lblQuantity;
        private TextBox txtDeviceId, txtDeviceName, txtPrice;
        private ComboBox cboCategory;
        private NumericUpDown numQuantity;
        private Button btnAdd, btnUpdate, btnDelete, btnClear;
        private DataGridView dgvDevices;
        private ErrorProvider errorProvider;

        // ====== Dữ liệu ======
        private readonly BindingList<Device> _devices = new BindingList<Device>();
        private readonly BindingSource _bs = new BindingSource();
        private readonly CultureInfo _vn = new CultureInfo("vi-VN");
        private bool _loading = false; // chặn sự kiện khi đang nạp dữ liệu lên form

        public frmDeviceManager()
        {
            InitializeUI();
            InitializeData();
        }

        // =========================================================
        // 1. KHỞI TẠO GIAO DIỆN (toàn bộ bằng code)
        // =========================================================
        private void InitializeUI()
        {
            Text = "Quản lý danh mục thiết bị điện tử";
            Size = new Size(900, 620);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(800, 560);
            Font = new Font("Segoe UI", 10F);

            errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

            // ---- Group nhập liệu ----
            grpInput = new GroupBox
            {
                Text = "Thông tin thiết bị",
                Location = new Point(12, 10),
                Size = new Size(860, 150),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblId = new Label { Text = "Mã thiết bị:", Location = new Point(20, 33), AutoSize = true };
            txtDeviceId = new TextBox { Name = "txtDeviceId", Location = new Point(120, 30), Width = 200, ReadOnly = true, BackColor = SystemColors.Control };

            lblName = new Label { Text = "Tên thiết bị:", Location = new Point(20, 68), AutoSize = true };
            txtDeviceName = new TextBox { Name = "txtDeviceName", Location = new Point(120, 65), Width = 200 };

            lblCategory = new Label { Text = "Loại thiết bị:", Location = new Point(20, 103), AutoSize = true };
            cboCategory = new ComboBox
            {
                Name = "cboCategory",
                Location = new Point(120, 100),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboCategory.Items.AddRange(new object[] { "Laptop", "Điện thoại", "Máy tính bảng", "Màn hình", "Phụ kiện" });

            lblPrice = new Label { Text = "Đơn giá (VNĐ):", Location = new Point(400, 33), AutoSize = true };
            txtPrice = new TextBox { Name = "txtPrice", Location = new Point(520, 30), Width = 200 };

            lblQuantity = new Label { Text = "Số lượng tồn:", Location = new Point(400, 68), AutoSize = true };
            numQuantity = new NumericUpDown
            {
                Name = "numQuantity",
                Location = new Point(520, 65),
                Width = 200,
                Minimum = 0,
                Maximum = 100000,
                Value = 0
            };

            grpInput.Controls.AddRange(new Control[]
            {
                lblId, txtDeviceId, lblName, txtDeviceName, lblCategory, cboCategory,
                lblPrice, txtPrice, lblQuantity, numQuantity
            });

            // ---- Các nút chức năng ----
            btnAdd = MakeButton("btnAdd", "Thêm mới", 12);
            btnUpdate = MakeButton("btnUpdate", "Cập nhật", 132);
            btnDelete = MakeButton("btnDelete", "Xóa", 252);
            btnClear = MakeButton("btnClear", "Làm mới / Hủy", 372);
            btnClear.Width = 140;

            btnAdd.Click += btnAdd_Click;
            btnUpdate.Click += btnUpdate_Click;
            btnDelete.Click += btnDelete_Click;
            btnClear.Click += btnClear_Click;

            // ---- DataGridView ----
            dgvDevices = new DataGridView
            {
                Name = "dgvDevices",
                Location = new Point(12, 215),
                Size = new Size(860, 355),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            ConfigureGrid();

            // ---- Validation trực tiếp khi rời ô ----
            txtDeviceName.Validating += (s, e) => ValidateName();
            cboCategory.Validating += (s, e) => ValidateCategory();
            txtPrice.Validating += (s, e) => ValidatePrice(out _);
            cboCategory.SelectedIndexChanged += (s, e) => { if (!_loading) ValidateCategory(); };

            Controls.AddRange(new Control[] { grpInput, btnAdd, btnUpdate, btnDelete, btnClear, dgvDevices });
        }

        private Button MakeButton(string name, string text, int x)
        {
            return new Button
            {
                Name = name,
                Text = text,
                Location = new Point(x, 170),
                Size = new Size(110, 36)
            };
        }

        // Cấu hình cột thủ công hoàn toàn bằng code
        private void ConfigureGrid()
        {
            dgvDevices.AutoGenerateColumns = false;
            dgvDevices.ReadOnly = true;
            dgvDevices.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDevices.MultiSelect = false;
            dgvDevices.AllowUserToAddRows = false;
            dgvDevices.AllowUserToDeleteRows = false;
            dgvDevices.RowHeadersVisible = false;
            dgvDevices.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDevices.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvDevices.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvDevices.EnableHeadersVisualStyles = false;
            dgvDevices.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(230, 236, 245);

            dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colId",
                HeaderText = "Mã thiết bị",
                DataPropertyName = nameof(Device.Id),
                FillWeight = 15
            });
            dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colName",
                HeaderText = "Tên thiết bị",
                DataPropertyName = nameof(Device.Name),
                FillWeight = 33
            });
            dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCategory",
                HeaderText = "Loại thiết bị",
                DataPropertyName = nameof(Device.Category),
                FillWeight = 18
            });

            var colPrice = new DataGridViewTextBoxColumn
            {
                Name = "colPrice",
                HeaderText = "Đơn giá",
                DataPropertyName = nameof(Device.Price),
                FillWeight = 20
            };
            colPrice.DefaultCellStyle.Format = "C0";                 // định dạng tiền tệ
            colPrice.DefaultCellStyle.FormatProvider = _vn;           // 1.000.000 ₫
            colPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvDevices.Columns.Add(colPrice);

            var colQty = new DataGridViewTextBoxColumn
            {
                Name = "colQuantity",
                HeaderText = "Tồn kho",
                DataPropertyName = nameof(Device.Quantity),
                FillWeight = 14
            };
            colQty.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvDevices.Columns.Add(colQty);
        }

        // =========================================================
        // 2. KHỞI TẠO DỮ LIỆU + RÀNG BUỘC BINDINGSOURCE
        // =========================================================
        private void InitializeData()
        {


            _bs.DataSource = _devices;          // ràng buộc BindingList<Device> qua BindingSource
            dgvDevices.DataSource = _bs;

            // Click chọn dòng -> đổ ngược lên các control
            dgvDevices.SelectionChanged += dgvDevices_SelectionChanged;
            dgvDevices.CellClick += (s, e) => LoadCurrentToForm();

            ClearForm();
        }

        // =========================================================
        // 3. ĐỒNG BỘ 2 CHIỀU: GRID -> FORM
        // =========================================================
        private void dgvDevices_SelectionChanged(object sender, EventArgs e)
        {
            LoadCurrentToForm();
        }

        private void LoadCurrentToForm()
        {
            if (_loading) return;
            if (dgvDevices.SelectedRows.Count == 0) return;

            var d = _bs.Current as Device;
            if (d == null) return;

            _loading = true;
            txtDeviceId.Text = d.Id;
            txtDeviceName.Text = d.Name;
            cboCategory.SelectedItem = d.Category;
            txtPrice.Text = d.Price.ToString("N0", _vn);
            numQuantity.Value = Math.Min(numQuantity.Maximum, Math.Max(numQuantity.Minimum, d.Quantity));
            _loading = false;

            errorProvider.Clear();
        }

        // =========================================================
        // 4. VALIDATION (ErrorProvider)
        // =========================================================
        private bool ValidateName()
        {
            if (string.IsNullOrWhiteSpace(txtDeviceName.Text))
            {
                errorProvider.SetError(txtDeviceName, "Tên thiết bị không được để trống!");
                return false;
            }
            errorProvider.SetError(txtDeviceName, "");
            return true;
        }

        private bool ValidateCategory()
        {
            if (cboCategory.SelectedIndex < 0)
            {
                errorProvider.SetError(cboCategory, "Vui lòng chọn loại thiết bị!");
                return false;
            }
            errorProvider.SetError(cboCategory, "");
            return true;
        }

        private bool ValidatePrice(out decimal price)
        {
            price = 0;
            // bỏ dấu phân cách nghìn, khoảng trắng, ký hiệu tiền tệ
            string raw = (txtPrice.Text ?? "")
                .Replace(".", "").Replace(",", "").Replace(" ", "").Replace("₫", "").Trim();

            if (!decimal.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out price) || price <= 0)
            {
                // Lưu ý: NumberStyles.None không cho dấu âm, nên số âm cũng bị báo lỗi
                price = 0;
                errorProvider.SetError(txtPrice, "Đơn giá phải là số hợp lệ và lớn hơn 0!");
                return false;
            }
            errorProvider.SetError(txtPrice, "");
            return true;
        }

        private bool ValidateAll(out decimal price)
        {
            // Gọi đủ 3 hàm (không dùng && để mọi lỗi đều hiện cùng lúc)
            bool okName = ValidateName();
            bool okCat = ValidateCategory();
            bool okPrice = ValidatePrice(out price);
            return okName && okCat && okPrice;
        }

        // =========================================================
        // 5. CÁC NÚT CHỨC NĂNG
        // =========================================================
        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateAll(out decimal price)) return;

            var d = new Device
            {
                Id = GenerateId(),
                Name = txtDeviceName.Text.Trim(),
                Category = cboCategory.SelectedItem.ToString(),
                Price = price,
                Quantity = (int)numQuantity.Value
            };

            _devices.Add(d);   // BindingList tự báo cho Grid hiển thị ngay, không cần nạp lại

            // Chọn dòng vừa thêm
            _bs.Position = _devices.Count - 1;
            if (dgvDevices.Rows.Count > 0)
                dgvDevices.FirstDisplayedScrollingRowIndex = dgvDevices.Rows.Count - 1;

            MessageBox.Show("Thêm thiết bị thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearForm();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            var d = _bs.Current as Device;
            if (d == null || dgvDevices.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một thiết bị trên danh sách để cập nhật!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateAll(out decimal price)) return;

            // Đồng bộ từ Form xuống đối tượng trong danh sách -> Grid tự cập nhật
            d.Name = txtDeviceName.Text.Trim();
            d.Category = cboCategory.SelectedItem.ToString();
            d.Price = price;
            d.Quantity = (int)numQuantity.Value;

            MessageBox.Show("Cập nhật thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var d = _bs.Current as Device;
            if (d == null || dgvDevices.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một thiết bị để xóa!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa thiết bị \"{d.Name}\" không?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _devices.Remove(d);
                ClearForm();
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        // =========================================================
        // 6. HÀM HỖ TRỢ
        // =========================================================
        private void ClearForm()
        {
            _loading = true;
            txtDeviceId.Text = GenerateId();   // mã tự động cho thiết bị kế tiếp
            txtDeviceName.Clear();
            cboCategory.SelectedIndex = -1;
            txtPrice.Clear();
            numQuantity.Value = 0;
            dgvDevices.ClearSelection();
            _loading = false;

            errorProvider.Clear();
            txtDeviceName.Focus();
        }

        // Sinh mã dạng TB001, TB002, ... dựa trên số lớn nhất hiện có
        private string GenerateId()
        {
            int max = 0;
            foreach (var d in _devices)
            {
                if (d.Id != null && d.Id.StartsWith("TB") && int.TryParse(d.Id.Substring(2), out int n))
                    max = Math.Max(max, n);
            }
            return "TB" + (max + 1).ToString("D3");
        }
    }
}