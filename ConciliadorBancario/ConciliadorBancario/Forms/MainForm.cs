using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using ConciliadorBancario.Data;
using ConciliadorBancario.Models;
using ConciliadorBancario.Services;

namespace ConciliadorBancario.Forms
{
    public class MainForm : Form
    {
        private MenuStrip _menuStrip;
        private DataGridView _gridBanco;
        private DataGridView _gridSistema;
        private Button _btnAutoConciliar;
        private Button _btnExportarMasivos;
        private ComboBox _cmbFiltroBanco;
        private ComboBox _cmbFiltroSistema;

        private MovimientoRepository _movRepo;
        private ConciliacionEngine _engine;

        private List<Movimiento> _allBanco;
        private List<Movimiento> _allSistema;

        public MainForm()
        {
            InitializeComponent();
            _movRepo = new MovimientoRepository();
            _engine = new ConciliacionEngine();
            DatabaseHelper.InitializeDatabase();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Sistema de Conciliación Bancaria";
            this.Size = new Size(1200, 800);

            // MenuStrip setup
            _menuStrip = new MenuStrip();
            var menuArchivo = new ToolStripMenuItem("Archivo");
            var menuImportarBanco = new ToolStripMenuItem("Importar Banco", null, (s,e) => ImportarBanco());
            var menuImportarSistema = new ToolStripMenuItem("Importar Sistema", null, (s,e) => ImportarSistema());
            var menuConfiguracion = new ToolStripMenuItem("Configuración", null, (s,e) => AbrirConfiguracion());

            menuArchivo.DropDownItems.Add(menuImportarBanco);
            menuArchivo.DropDownItems.Add(menuImportarSistema);
            menuArchivo.DropDownItems.Add(new ToolStripSeparator());
            menuArchivo.DropDownItems.Add(menuConfiguracion);
            _menuStrip.Items.Add(menuArchivo);
            this.MainMenuStrip = _menuStrip;
            this.Controls.Add(_menuStrip);

            var splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Orientation = Orientation.Vertical;
            splitContainer.SplitterDistance = 600;

            var panelIzquierdo = new Panel { Dock = DockStyle.Fill };
            var headerIzquierdo = new Panel { Dock = DockStyle.Top, Height = 60 };
            headerIzquierdo.Controls.Add(new Label { Text = "Movimientos Bancarios", Location = new Point(10, 10), Font = new Font("Arial", 12, FontStyle.Bold), AutoSize = true });

            _cmbFiltroBanco = new ComboBox { Location = new Point(10, 35), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbFiltroBanco.Items.AddRange(new[] { "Todos", "No encontrado", "Pendiente de corregir en sistema", "Pendiente de pasar", "Pend. Asiento Masivo", "Conciliado" });
            _cmbFiltroBanco.SelectedIndex = 0;
            _cmbFiltroBanco.SelectedIndexChanged += (s, e) => FilterData();
            headerIzquierdo.Controls.Add(_cmbFiltroBanco);

            _gridBanco = CrearGrid(true);
            panelIzquierdo.Controls.Add(_gridBanco);
            panelIzquierdo.Controls.Add(headerIzquierdo);

            var panelDerecho = new Panel { Dock = DockStyle.Fill };
            var headerDerecho = new Panel { Dock = DockStyle.Top, Height = 60 };
            headerDerecho.Controls.Add(new Label { Text = "Movimientos del Sistema", Location = new Point(10, 10), Font = new Font("Arial", 12, FontStyle.Bold), AutoSize = true });

            _cmbFiltroSistema = new ComboBox { Location = new Point(10, 35), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbFiltroSistema.Items.AddRange(new[] { "Todos", "No encontrado", "Pendiente de corregir en sistema", "Pendiente de pasar", "Pend. Asiento Masivo", "Conciliado" });
            _cmbFiltroSistema.SelectedIndex = 0;
            _cmbFiltroSistema.SelectedIndexChanged += (s, e) => FilterData();
            headerDerecho.Controls.Add(_cmbFiltroSistema);

            _gridSistema = CrearGrid(false);
            panelDerecho.Controls.Add(_gridSistema);
            panelDerecho.Controls.Add(headerDerecho);

            splitContainer.Panel1.Controls.Add(panelIzquierdo);
            splitContainer.Panel2.Controls.Add(panelDerecho);

            var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            _btnAutoConciliar = new Button { Text = "Auto-Conciliar", Location = new Point(10, 10), Width = 120 };
            _btnAutoConciliar.Click += (s, e) => { _engine.AutoConciliar(); LoadData(); MessageBox.Show("Auto-conciliación finalizada."); };

            _btnExportarMasivos = new Button { Text = "Exportar Asientos Masivos", Location = new Point(150, 10), Width = 200 };
            _btnExportarMasivos.Click += BtnExportarMasivos_Click;

            panelBottom.Controls.Add(_btnAutoConciliar);
            panelBottom.Controls.Add(_btnExportarMasivos);

            this.Controls.Add(splitContainer);
            this.Controls.Add(panelBottom);
            _menuStrip.BringToFront();

            _gridBanco.CellClick += (s, e) => BuscarSimilares(e);
            _gridBanco.CellValueChanged += Grid_CellValueChanged;
            _gridSistema.CellValueChanged += Grid_CellValueChanged;
        }

        private DataGridView CrearGrid(bool isBanco)
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", Visible = false });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha", HeaderText = "Fecha", DataPropertyName = "FechaFormateada", ReadOnly = true, Width = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Monto", HeaderText = "Monto", DataPropertyName = "MontoFormateado", ReadOnly = true, Width = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Concepto", HeaderText = "Concepto", DataPropertyName = "Concepto", ReadOnly = true, Width = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CodOperacion", HeaderText = "Cod. Banco", DataPropertyName = "Referencia_CodOperacion", ReadOnly = true, Width = 100 });

            if (!isBanco)
            {
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CodOperacionSistema", HeaderText = "Cod. Sistema", DataPropertyName = "CodOperacionSistema", ReadOnly = true, Width = 100 });
            }

            if (isBanco)
            {
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Banco", HeaderText = "Banco", DataPropertyName = "Banco", ReadOnly = true, Width = 80 });
            }

            var cmbEstado = new DataGridViewComboBoxColumn
            {
                Name = "Estado",
                HeaderText = "Estado",
                DataPropertyName = "Estado",
                Width = 150
            };
            cmbEstado.Items.AddRange(new[] { "No encontrado", "Pendiente de corregir en sistema", "Pendiente de pasar", "Pend. Asiento Masivo", "Conciliado" });
            grid.Columns.Add(cmbEstado);

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Observaciones", HeaderText = "Observaciones", DataPropertyName = "Observaciones", Width = 150 });

            return grid;
        }

        private void LoadData()
        {
            _allBanco = _movRepo.GetMovimientosActivos("Banco");
            _allSistema = _movRepo.GetMovimientosActivos("Sistema");
            FilterData();
        }

        private void FilterData()
        {
            if (_allBanco == null || _allSistema == null) return;

            string filtroBanco = _cmbFiltroBanco.SelectedItem?.ToString() ?? "Todos";
            string filtroSistema = _cmbFiltroSistema.SelectedItem?.ToString() ?? "Todos";

            var bancoList = filtroBanco == "Todos" ? _allBanco : _allBanco.Where(m => m.Estado == filtroBanco).ToList();
            var sistemaList = filtroSistema == "Todos" ? _allSistema : _allSistema.Where(m => m.Estado == filtroSistema).ToList();

            _gridBanco.DataSource = new System.ComponentModel.BindingList<Movimiento>(bancoList);
            _gridSistema.DataSource = new System.ComponentModel.BindingList<Movimiento>(sistemaList);
        }

        private void BuscarSimilares(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var movBanco = _gridBanco.Rows[e.RowIndex].DataBoundItem as Movimiento;
            if (movBanco == null || movBanco.Estado != "No encontrado") return;

            var candidatos = _engine.ObtenerCandidatosFuzzy(movBanco, _allSistema.Where(s => s.Estado == "No encontrado").ToList());

            if (candidatos.Count > 0)
            {
                var bindingList = new System.ComponentModel.BindingList<Movimiento>(candidatos);
                _gridSistema.DataSource = bindingList;
                _cmbFiltroSistema.SelectedIndex = 1; // "No encontrado"
            }
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = sender as DataGridView;
            var mov = grid.Rows[e.RowIndex].DataBoundItem as Movimiento;
            if (mov != null)
            {
                _movRepo.UpdateEstado(mov.Id, mov.Estado, mov.Observaciones);
            }
        }

        private void ImportarBanco()
        {
            using (var form = new ImportForm("Banco"))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    LoadData();
                }
            }
        }

        private void ImportarSistema()
        {
             using (var form = new ImportForm("Sistema"))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    LoadData();
                }
            }
        }

        private void AbrirConfiguracion()
        {
             using (var form = new ConfiguracionForm())
             {
                 form.ShowDialog();
             }
        }

        private void BtnExportarMasivos_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog() { Filter = "CSV|*.csv|Excel|*.xlsx", FileName = "AsientosMasivos.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var exportService = new AsientoMasivoExportService();
                    exportService.ExportarAsientosMasivos(sfd.FileName);
                    MessageBox.Show("Exportación exitosa.");
                }
            }
        }
    }
}
