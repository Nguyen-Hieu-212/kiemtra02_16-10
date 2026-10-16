using System;
using System.Drawing;
using System.Windows.Forms;

namespace bai2_16_10
{
    // Form đăng nhập đơn giản để có "tên người dùng đang đăng nhập" hiển thị trên StatusStrip
    public class frmLogin : Form
    {
        private const string DemoPassword = "123";

        private Label lblUser, lblPass, lblHint;
        private TextBox txtUser, txtPass;
        private Button btnOk, btnCancel;

        public string UserName => txtUser.Text.Trim();

        public frmLogin()
        {
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Đăng nhập hệ thống";
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(360, 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            lblUser = new Label { Text = "Tên đăng nhập:", Location = new Point(20, 25), AutoSize = true };
            txtUser = new TextBox { Location = new Point(140, 22), Width = 200, Text = "admin" };
            lblPass = new Label { Text = "Mật khẩu:", Location = new Point(20, 65), AutoSize = true };
            txtPass = new TextBox { Location = new Point(140, 62), Width = 200, UseSystemPasswordChar = true };
            lblHint = new Label { Text = "Demo: admin / " + DemoPassword, Location = new Point(20, 100), AutoSize = true, ForeColor = Color.Gray };

            btnOk = new Button { Text = "Đăng nhập", Location = new Point(140, 140), Size = new Size(95, 34) };
            btnCancel = new Button { Text = "Thoát", Location = new Point(245, 140), Size = new Size(95, 34), DialogResult = DialogResult.Cancel };
            btnOk.Click += btnOk_Click;

            Controls.AddRange(new Control[] { lblUser, txtUser, lblPass, txtPass, lblHint, btnOk, btnCancel });
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUser.Text) || txtPass.Text != DemoPassword)
            {
                MessageBox.Show("Tên đăng nhập hoặc mật khẩu không đúng!", "Lỗi đăng nhập",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPass.SelectAll();
                txtPass.Focus();
                return;
            }
            DialogResult = DialogResult.OK;
        }
    }
}