
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SupermarketMDI
{
    public class Product
    {
        public string ProductId { get; set; } = "";
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new frmMain());
        }
    }

    public class frmMain : Form
    {
        private readonly ToolStripStatusLabel lblUser = new();
        private readonly ToolStripStatusLabel lblStatus = new();
        private readonly ToolStripProgressBar progressBar = new();
        private readonly BindingList<Product> products = new();

        public BindingList<Product> Products => products;

        public frmMain()
        {
            Text = "HỆ THỐNG QUẢN LÝ SIÊU THỊ - MDI";
            IsMdiContainer = true;
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;

            BuildMenu();
            BuildStatusStrip();

            products.Add(new Product
            {
                ProductId = "SP001",
                ProductName = "Sữa tươi",
                Price = 32000,
                Quantity = 100
            });

            products.Add(new Product
            {
                ProductId = "SP002",
                ProductName = "Bánh quy",
                Price = 25000,
                Quantity = 80
            });

            OpenChild<frmProductCatalog>();
        }

        private void BuildMenu()
        {
            var menu = new MenuStrip();

            var system = new ToolStripMenuItem("Hệ thống");
            system.DropDownItems.Add("Đăng xuất", null,
                (s, e) => Logout());
            system.DropDownItems.Add("Thoát", null,
                (s, e) => Close());

            var category = new ToolStripMenuItem("Danh mục");
            category.DropDownItems.Add("Quản lý sản phẩm", null,
                (s, e) => OpenChild<frmProductCatalog>());
            category.DropDownItems.Add("Quản lý nhập xuất", null,
                (s, e) => OpenChild<frmStockMovement>());

            var report = new ToolStripMenuItem("Báo cáo");
            report.DropDownItems.Add("Thống kê doanh thu", null,
                (s, e) => OpenChild<frmAsyncReport>());

            var windows = new ToolStripMenuItem("Cửa sổ");
            windows.DropDownItems.Add("Cascade", null,
                (s, e) => LayoutMdi(MdiLayout.Cascade));
            windows.DropDownItems.Add("Tile Horizontal", null,
                (s, e) => LayoutMdi(MdiLayout.TileHorizontal));
            windows.DropDownItems.Add("Tile Vertical", null,
                (s, e) => LayoutMdi(MdiLayout.TileVertical));

            menu.Items.AddRange(new ToolStripItem[]
            {
                system, category, report, windows
            });

            MainMenuStrip = menu;
            Controls.Add(menu);
        }

        private void BuildStatusStrip()
        {
            var status = new StatusStrip();

            lblUser.Text = "Người dùng: Admin";
            lblStatus.Text = "Sẵn sàng";

            progressBar.Visible = false;
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Width = 180;

            status.Items.Add(lblUser);
            status.Items.Add(new ToolStripStatusLabel { Spring = true });
            status.Items.Add(lblStatus);
            status.Items.Add(progressBar);

            Controls.Add(status);
        }

        public void SetProgress(int value, string message)
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                Invoke(new Action(() => SetProgress(value, message)));
                return;
            }

            progressBar.Visible = value >= 0;
            if (value >= 0)
                progressBar.Value = Math.Clamp(value, 0, 100);

            lblStatus.Text = message;
        }

        public void OpenChild<T>() where T : Form
        {
            foreach (Form child in MdiChildren)
            {
                if (child is T)
                {
                    child.Activate();
                    return;
                }
            }

            Form form;

            if (typeof(T) == typeof(frmProductCatalog))
                form = new frmProductCatalog(this);
            else if (typeof(T) == typeof(frmAsyncReport))
                form = new frmAsyncReport(this);
            else
                form = new frmStockMovement(this);

            form.MdiParent = this;
            form.Show();
        }

        private void Logout()
        {
            var result = MessageBox.Show(
                "Bạn có muốn đăng xuất khỏi hệ thống?",
                "Đăng xuất",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                foreach (Form child in MdiChildren)
                    child.Close();

                lblUser.Text = "Người dùng: Chưa đăng nhập";
                lblStatus.Text = "Đã đăng xuất";
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn thoát chương trình?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.No)
                e.Cancel = true;

            base.OnFormClosing(e);
        }
    }

    public class frmProductCatalog : Form
    {
        private readonly frmMain main;
        private readonly TextBox txtId = new();
        private readonly TextBox txtName = new();
        private readonly TextBox txtPrice = new();
        private readonly NumericUpDown numQuantity = new();
        private readonly DataGridView grid = new();
        private Product? selected;

        public frmProductCatalog(frmMain parent)
        {
            main = parent;
            Text = "Quản lý sản phẩm";
            Size = new Size(850, 500);

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 150,
                ColumnCount = 4,
                RowCount = 3,
                Padding = new Padding(8)
            };

            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            AddField(panel, "Mã sản phẩm:", txtId, 0, 0);
            AddField(panel, "Tên sản phẩm:", txtName, 2, 0);
            AddField(panel, "Đơn giá:", txtPrice, 0, 1);
            AddField(panel, "Số lượng:", numQuantity, 2, 1);

            txtId.ReadOnly = true;
            numQuantity.Maximum = 1000000;

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill
            };

            buttons.Controls.Add(MakeButton("Thêm", AddProduct));
            buttons.Controls.Add(MakeButton("Cập nhật", UpdateProduct));
            buttons.Controls.Add(MakeButton("Xóa", DeleteProduct));
            buttons.Controls.Add(MakeButton("Làm mới", ClearInput));

            panel.Controls.Add(buttons, 0, 2);
            panel.SetColumnSpan(buttons, 4);

            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoGenerateColumns = true;
            grid.DataSource = main.Products;

            grid.SelectionChanged += (s, e) =>
            {
                if (grid.CurrentRow?.DataBoundItem is Product p)
                {
                    selected = p;
                    txtId.Text = p.ProductId;
                    txtName.Text = p.ProductName;
                    txtPrice.Text = p.Price.ToString();
                    numQuantity.Value = Math.Min(
                        numQuantity.Maximum,
                        Math.Max(numQuantity.Minimum, p.Quantity));
                }
            };

            Controls.Add(grid);
            Controls.Add(panel);
        }

        private void AddField(
            TableLayoutPanel panel, string label,
            Control control, int labelColumn, int row)
        {
            panel.Controls.Add(new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, labelColumn, row);

            control.Dock = DockStyle.Fill;
            panel.Controls.Add(control, labelColumn + 1, row);
        }

        private Button MakeButton(string text, Action action)
        {
            var button = new Button
            {
                Text = text,
                Width = 100,
                Height = 32
            };
            button.Click += (s, e) => action();
            return button;
        }

        private bool ValidateInput(out decimal price)
        {
            price = 0;

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Vui lòng nhập tên sản phẩm.");
                return false;
            }

            if (!decimal.TryParse(txtPrice.Text, out price) || price < 0)
            {
                MessageBox.Show("Đơn giá không hợp lệ.");
                return false;
            }

            return true;
        }

        private void AddProduct()
        {
            if (!ValidateInput(out decimal price)) return;

            int id = 1;
            while (main.Products.Any(p => p.ProductId == $"SP{id:000}"))
                id++;

            main.Products.Add(new Product
            {
                ProductId = $"SP{id:000}",
                ProductName = txtName.Text.Trim(),
                Price = price,
                Quantity = (int)numQuantity.Value
            });

            ClearInput();
        }

        private void UpdateProduct()
        {
            if (selected == null ||
                !ValidateInput(out decimal price))
            {
                MessageBox.Show("Hãy chọn sản phẩm và nhập dữ liệu hợp lệ.");
                return;
            }

            selected.ProductName = txtName.Text.Trim();
            selected.Price = price;
            selected.Quantity = (int)numQuantity.Value;

            grid.Refresh();
            main.SetProgress(-1, "Đã cập nhật sản phẩm");
        }

        private void DeleteProduct()
        {
            if (selected == null) return;

            if (MessageBox.Show("Xóa sản phẩm đã chọn?",
                "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                main.Products.Remove(selected);
                ClearInput();
            }
        }

        private void ClearInput()
        {
            selected = null;
            txtId.Clear();
            txtName.Clear();
            txtPrice.Clear();
            numQuantity.Value = 0;
            grid.ClearSelection();
        }
    }

    public class frmStockMovement : Form
    {
        public frmStockMovement(frmMain parent)
        {
            Text = "Quản lý nhập xuất";
            Size = new Size(600, 350);

            Controls.Add(new Label
            {
                Text = "Khu vực quản lý nhập - xuất hàng hóa",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 16)
            });
        }
    }

    public class frmAsyncReport : Form
    {
        private readonly frmMain main;
        private readonly Label lblResult = new();
        private readonly DataGridView grid = new();
        private readonly Button btnLoad = new();
        private readonly Button btnSync = new();
        private readonly System.Windows.Forms.Timer syncTimer = new();
        private readonly BindingList<string> updates = new();
        private CancellationTokenSource? syncCts;
        private bool loading;

        public frmAsyncReport(frmMain parent)
        {
            main = parent;
            Text = "Báo cáo doanh thu & đồng bộ dữ liệu";
            Size = new Size(850, 500);

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 55,
                Padding = new Padding(8)
            };

            btnLoad.Text = "Tải Báo Cáo Doanh Thu";
            btnLoad.Width = 200;
            btnLoad.Height = 32;
            btnLoad.Click += async (s, e) => await LoadReportAsync();

            btnSync.Text = "Bắt đầu đồng bộ";
            btnSync.Width = 150;
            btnSync.Height = 32;
            btnSync.Click += (s, e) => ToggleSync();

            panel.Controls.Add(btnLoad);
            panel.Controls.Add(btnSync);

            lblResult.Dock = DockStyle.Top;
            lblResult.Height = 55;
            lblResult.TextAlign = ContentAlignment.MiddleLeft;
            lblResult.Text = "Chưa tải báo cáo";

            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AutoGenerateColumns = true;
            grid.DataSource = updates;

            Controls.Add(grid);
            Controls.Add(lblResult);
            Controls.Add(panel);
        }

        private async Task LoadReportAsync()
        {
            if (loading) return;

            loading = true;
            btnLoad.Enabled = false;
            lblResult.Text = "Đang tải báo cáo...";
            main.SetProgress(0, "Đang tải báo cáo doanh thu");

            var progress = new Progress<int>(value =>
            {
                main.SetProgress(value, $"Đang xử lý báo cáo: {value}%");
            });

            try
            {
                var snapshot = main.Products
                    .Select(p => new Product
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Price = p.Price,
                        Quantity = p.Quantity
                    })
                    .ToList();

                decimal revenue = await Task.Run(async () =>
                {
                    for (int i = 1; i <= 5; i++)
                    {
                        await Task.Delay(700);
                        ((IProgress<int>)progress).Report(i * 20);
                    }

                    return snapshot.Sum(p => p.Price * p.Quantity);
                });

                lblResult.Text =
                    $"Báo cáo hoàn tất. Tổng giá trị hàng tồn: {revenue:N0} VNĐ";

                updates.Add(
                    $"{DateTime.Now:HH:mm:ss} - Báo cáo hoàn tất: {revenue:N0} VNĐ");

                main.SetProgress(-1, "Tải báo cáo hoàn tất");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải báo cáo: " + ex.Message);
                main.SetProgress(-1, "Tải báo cáo thất bại");
            }
            finally
            {
                loading = false;
                btnLoad.Enabled = true;
            }
        }

        private void ToggleSync()
        {
            if (syncCts != null)
            {
                syncCts.Cancel();
                syncCts.Dispose();
                syncCts = null;
                btnSync.Text = "Bắt đầu đồng bộ";
                main.SetProgress(-1, "Đã dừng đồng bộ");
                return;
            }

            syncCts = new CancellationTokenSource();
            btnSync.Text = "Dừng đồng bộ";
            _ = RunSyncAsync(syncCts.Token);
        }

        private async Task RunSyncAsync(CancellationToken token)
        {
            try
            {
                await Task.Run(async () =>
                {
                    int count = 0;

                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(2000, token);
                        count++;

                        string message =
                            $"{DateTime.Now:HH:mm:ss} - Đồng bộ dữ liệu lần {count}";

                        // Chuyển về UI Thread trước khi cập nhật giao diện.
                        if (IsDisposed || !IsHandleCreated) return;

                        Invoke(new Action(() =>
                        {
                            if (IsDisposed) return;

                            lblResult.Text = message;
                            updates.Add(message);
                            main.SetProgress(-1, "Đồng bộ dữ liệu đang chạy");
                        }));
                    }
                }, token);
            }
            catch (OperationCanceledException)
            {
                // Người dùng đã dừng đồng bộ.
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    BeginInvoke(new Action(() =>
                        MessageBox.Show("Lỗi đồng bộ: " + ex.Message)));
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            syncCts?.Cancel();
            syncCts?.Dispose();
            syncCts = null;
            base.OnFormClosed(e);
        }
    }
}
