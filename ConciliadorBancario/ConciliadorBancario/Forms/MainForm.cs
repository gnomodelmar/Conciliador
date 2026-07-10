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
        private Button _btnVincular;
        private Button _btnEliminarBanco;
        private Button _btnEliminarSistema;

        private ComboBox _cmbFiltroBancoEst;
        private ComboBox _cmbFiltroBancoBco;
        private DateTimePicker _dtBancoDesde;
        private DateTimePicker _dtBancoHasta;

        private ComboBox _cmbFiltroSistEst;
        private ComboBox _cmbFiltroSistBco;
        private DateTimePicker _dtSistDesde;
        private DateTimePicker _dtSistHasta;

        private MovimientoRepository _movRepo;
        private ConciliacionEngine _engine;
        private ConfiguracionRepository _confRepo;

        private List<Movimiento> _allBanco;
        private List<Movimiento> _allSistema;
        private bool _isUpdatingSelection = false;

        public MainForm()
        {
            InitializeComponent();
            _movRepo = new MovimientoRepository();
            _engine = new ConciliacionEngine();
            _confRepo = new ConfiguracionRepository();
            DatabaseHelper.InitializeDatabase();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Sistema de Conciliación Bancaria";
            this.Size = new Size(1350, 800);

            var splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Orientation = Orientation.Vertical;
            splitContainer.SplitterDistance = 650;
            this.Controls.Add(splitContainer);

            var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            _btnAutoConciliar = new Button { Text = "Auto-Conciliar (Coincidencia Exacta)", Location = new Point(10, 15), Width = 200 };
            _btnAutoConciliar.Click += (s, e) => { _engine.AutoConciliar(); LoadData(); MessageBox.Show("Auto-conciliación finalizada."); };

            _btnExportarMasivos = new Button { Text = "Exportar Asientos Masivos", Location = new Point(220, 15), Width = 180 };
            _btnExportarMasivos.Click += BtnExportarMasivos_Click;

            _btnVincular = new Button { Text = "Vincular Seleccionados (Match Manual)", Location = new Point(410, 15), Width = 250 };
            _btnVincular.Click += BtnVincular_Click;

            _btnEliminarBanco = new Button { Text = "Eliminar Sel. Banco", Location = new Point(670, 15), Width = 150 };
            _btnEliminarBanco.Click += (s, e) => EliminarFilas(_gridBanco);

            _btnEliminarSistema = new Button { Text = "Eliminar Sel. Sistema", Location = new Point(830, 15), Width = 150 };
            _btnEliminarSistema.Click += (s, e) => EliminarFilas(_gridSistema);

            panelBottom.Controls.Add(_btnAutoConciliar);
            panelBottom.Controls.Add(_btnExportarMasivos);
            panelBottom.Controls.Add(_btnVincular);
            panelBottom.Controls.Add(_btnEliminarBanco);
            panelBottom.Controls.Add(_btnEliminarSistema);
            this.Controls.Add(panelBottom);

            _menuStrip = new MenuStrip();
            _menuStrip.Dock = DockStyle.Top;
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

            // --- Panel Banco ---
            var panelIzquierdo = new Panel { Dock = DockStyle.Fill };
            var headerIzquierdo = new Panel { Dock = DockStyle.Top, Height = 90 };
            headerIzquierdo.Controls.Add(new Label { Text = "Movimientos Bancarios", Location = new Point(10, 5), Font = new Font("Arial", 12, FontStyle.Bold), AutoSize = true });

            headerIzquierdo.Controls.Add(new Label { Text = "Estado:", Location = new Point(10, 35), AutoSize = true });
            _cmbFiltroBancoEst = new ComboBox { Location = new Point(60, 32), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbFiltroBancoEst.Items.AddRange(new[] { "Todos", "No encontrado", "Posible match manual", "Pendiente de corregir en sistema", "Pendiente de pasar", "Pend. Asiento Masivo", "Conciliado" });
            _cmbFiltroBancoEst.SelectedIndex = 0;
            _cmbFiltroBancoEst.SelectedIndexChanged += (s, e) => FilterData();

            headerIzquierdo.Controls.Add(new Label { Text = "Banco:", Location = new Point(220, 35), AutoSize = true });
            _cmbFiltroBancoBco = new ComboBox { Location = new Point(270, 32), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbFiltroBancoBco.SelectedIndexChanged += (s, e) => FilterData();

            headerIzquierdo.Controls.Add(new Label { Text = "Desde:", Location = new Point(10, 65), AutoSize = true });
            _dtBancoDesde = new DateTimePicker { Location = new Point(60, 62), Width = 100, Format = DateTimePickerFormat.Short, Value = new DateTime(2000, 1, 1) };
            _dtBancoDesde.ValueChanged += (s, e) => FilterData();

            headerIzquierdo.Controls.Add(new Label { Text = "Hasta:", Location = new Point(170, 65), AutoSize = true });
            _dtBancoHasta = new DateTimePicker { Location = new Point(220, 62), Width = 100, Format = DateTimePickerFormat.Short, Value = new DateTime(2100, 1, 1) };
            _dtBancoHasta.ValueChanged += (s, e) => FilterData();

            var btnResetBanco = new Button { Text = "Limpiar Filtros", Location = new Point(330, 60), Width = 100 };
            btnResetBanco.Click += (s, e) => { _cmbFiltroBancoEst.SelectedIndex=0; _cmbFiltroBancoBco.SelectedIndex=0; _dtBancoDesde.Value=new DateTime(2000,1,1); _dtBancoHasta.Value=new DateTime(2100,1,1); };

            headerIzquierdo.Controls.Add(_cmbFiltroBancoEst);
            headerIzquierdo.Controls.Add(_cmbFiltroBancoBco);
            headerIzquierdo.Controls.Add(_dtBancoDesde);
            headerIzquierdo.Controls.Add(_dtBancoHasta);
            headerIzquierdo.Controls.Add(btnResetBanco);

            _gridBanco = CrearGrid(true);
            panelIzquierdo.Controls.Add(_gridBanco);
            panelIzquierdo.Controls.Add(headerIzquierdo);

            // --- Panel Sistema ---
            var panelDerecho = new Panel { Dock = DockStyle.Fill };
            var headerDerecho = new Panel { Dock = DockStyle.Top, Height = 90 };
            headerDerecho.Controls.Add(new Label { Text = "Movimientos del Sistema", Location = new Point(10, 5), Font = new Font("Arial", 12, FontStyle.Bold), AutoSize = true });

            headerDerecho.Controls.Add(new Label { Text = "Estado:", Location = new Point(10, 35), AutoSize = true });
            _cmbFiltroSistEst = new ComboBox { Location = new Point(60, 32), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbFiltroSistEst.Items.AddRange(new[] { "Todos", "No encontrado", "Posible match manual", "Pendiente de corregir en sistema", "Pendiente de pasar", "Pend. Asiento Masivo", "Conciliado" });
            _cmbFiltroSistEst.SelectedIndex = 0;
            _cmbFiltroSistEst.SelectedIndexChanged += (s, e) => FilterData();

            headerDerecho.Controls.Add(new Label { Text = "Banco:", Location = new Point(220, 35), AutoSize = true });
            _cmbFiltroSistBco = new ComboBox { Location = new Point(270, 32), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbFiltroSistBco.SelectedIndexChanged += (s, e) => FilterData();

            headerDerecho.Controls.Add(new Label { Text = "Desde:", Location = new Point(10, 65), AutoSize = true });
            _dtSistDesde = new DateTimePicker { Location = new Point(60, 62), Width = 100, Format = DateTimePickerFormat.Short, Value = new DateTime(2000, 1, 1) };
            _dtSistDesde.ValueChanged += (s, e) => FilterData();

            headerDerecho.Controls.Add(new Label { Text = "Hasta:", Location = new Point(170, 65), AutoSize = true });
            _dtSistHasta = new DateTimePicker { Location = new Point(220, 62), Width = 100, Format = DateTimePickerFormat.Short, Value = new DateTime(2100, 1, 1) };
            _dtSistHasta.ValueChanged += (s, e) => FilterData();

            var btnResetSist = new Button { Text = "Limpiar Filtros", Location = new Point(330, 60), Width = 100 };
            btnResetSist.Click += (s, e) => { _cmbFiltroSistEst.SelectedIndex=0; _cmbFiltroSistBco.SelectedIndex=0; _dtSistDesde.Value=new DateTime(2000,1,1); _dtSistHasta.Value=new DateTime(2100,1,1); };

            headerDerecho.Controls.Add(_cmbFiltroSistEst);
            headerDerecho.Controls.Add(_cmbFiltroSistBco);
            headerDerecho.Controls.Add(_dtSistDesde);
            headerDerecho.Controls.Add(_dtSistHasta);
            headerDerecho.Controls.Add(btnResetSist);

            _gridSistema = CrearGrid(false);
            panelDerecho.Controls.Add(_gridSistema);
            panelDerecho.Controls.Add(headerDerecho);

            splitContainer.Panel1.Controls.Add(panelIzquierdo);
            splitContainer.Panel2.Controls.Add(panelDerecho);

            _gridBanco.CellClick += (s, e) => InteraccionGrilla(_gridBanco, _gridSistema, true, e);
            _gridSistema.CellClick += (s, e) => InteraccionGrilla(_gridSistema, _gridBanco, false, e);

            _gridBanco.CellValueChanged += Grid_CellValueChanged;
            _gridSistema.CellValueChanged += Grid_CellValueChanged;

            LoadBancosDropdown();
        }

        private void LoadBancosDropdown()
        {
            if (_confRepo == null) return;
            var bancos = _confRepo.GetAllBancos("Banco");

            _cmbFiltroBancoBco.Items.Clear();
            _cmbFiltroSistBco.Items.Clear();
            _cmbFiltroBancoBco.Items.Add("Todos");
            _cmbFiltroSistBco.Items.Add("Todos");

            foreach(var b in bancos)
            {
                _cmbFiltroBancoBco.Items.Add(b.NombreBanco);
                _cmbFiltroSistBco.Items.Add(b.NombreBanco);
            }
            _cmbFiltroBancoBco.SelectedIndex = 0;
            _cmbFiltroSistBco.SelectedIndex = 0;
        }

        private DataGridView CrearGrid(bool isBanco)
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
                MultiSelect = true
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", Visible = false });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha", HeaderText = "Fecha", DataPropertyName = "Fecha", ReadOnly = true, Width = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Monto", HeaderText = "Monto", DataPropertyName = "Monto", ReadOnly = true, Width = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Concepto", HeaderText = "Concepto", DataPropertyName = "Concepto", ReadOnly = true, Width = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CodOperacion", HeaderText = "Cod. Banco", DataPropertyName = "Referencia_CodOperacion", ReadOnly = true, Width = 100 });

            if (!isBanco)
            {
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CodOperacionSistema", HeaderText = "Cod. Sistema", DataPropertyName = "CodOperacionSistema", ReadOnly = true, Width = 100 });
            }

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Banco", HeaderText = "Banco", DataPropertyName = "Banco", ReadOnly = true, Width = 80 });

            var cmbEstado = new DataGridViewComboBoxColumn
            {
                Name = "Estado",
                HeaderText = "Estado",
                DataPropertyName = "Estado",
                Width = 150
            };
            cmbEstado.Items.AddRange(new[] { "No encontrado", "Posible match manual", "Pendiente de corregir en sistema", "Pendiente de pasar", "Pend. Asiento Masivo", "Conciliado" });
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
            if (_allBanco == null || _allSistema == null || _isUpdatingSelection) return;

            string estadoBco = _cmbFiltroBancoEst.SelectedItem?.ToString() ?? "Todos";
            string bancoBco = _cmbFiltroBancoBco.SelectedItem?.ToString() ?? "Todos";
            DateTime desdeBco = _dtBancoDesde.Value.Date;
            DateTime hastaBco = _dtBancoHasta.Value.Date;

            var bancoList = _allBanco.Where(m =>
                (estadoBco == "Todos" || m.Estado == estadoBco) &&
                (bancoBco == "Todos" || m.Banco == bancoBco) &&
                m.Fecha.Date >= desdeBco && m.Fecha.Date <= hastaBco
            ).ToList();

            string estadoSis = _cmbFiltroSistEst.SelectedItem?.ToString() ?? "Todos";
            string bancoSis = _cmbFiltroSistBco.SelectedItem?.ToString() ?? "Todos";
            DateTime desdeSis = _dtSistDesde.Value.Date;
            DateTime hastaSis = _dtSistHasta.Value.Date;

            var sistemaList = _allSistema.Where(m =>
                (estadoSis == "Todos" || m.Estado == estadoSis) &&
                (bancoSis == "Todos" || m.Banco == bancoSis) &&
                m.Fecha.Date >= desdeSis && m.Fecha.Date <= hastaSis
            ).ToList();

            _gridBanco.DataSource = new SortableBindingList<Movimiento>(bancoList);
            _gridSistema.DataSource = new SortableBindingList<Movimiento>(sistemaList);
        }

        private void InteraccionGrilla(DataGridView sourceGrid, DataGridView targetGrid, bool isSourceBanco, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var movSource = sourceGrid.Rows[e.RowIndex].DataBoundItem as Movimiento;
            if (movSource == null) return;

            try
            {
                if (movSource.Estado == EstadosMovimiento.Conciliado && movSource.MatchId.HasValue)
                {
                    ComboBox targetComboEst = isSourceBanco ? _cmbFiltroSistEst : _cmbFiltroBancoEst;
                    if (targetComboEst.SelectedIndex != 0 && targetComboEst.SelectedItem.ToString() != EstadosMovimiento.Conciliado)
                    {
                        // Temporarily bypass the FilterData update guard so the visual list actually refreshes
                        _isUpdatingSelection = false;
                        targetComboEst.SelectedIndex = 0;
                        FilterData();
                        _isUpdatingSelection = true; // Reinstate guard
                    }

                    targetGrid.ClearSelection();
                    foreach (DataGridViewRow row in targetGrid.Rows)
                    {
                        var m = row.DataBoundItem as Movimiento;
                        if (m != null && m.Id == movSource.MatchId.Value)
                        {
                            row.Selected = true;
                            targetGrid.FirstDisplayedScrollingRowIndex = row.Index;
                            break;
                        }
                    }
                }
                else if (isSourceBanco && (movSource.Estado == EstadosMovimiento.NoEncontrado || movSource.Estado == EstadosMovimiento.PosibleMatch))
                {
                    _isUpdatingSelection = true;
                    var candidatos = _engine.ObtenerCandidatosFuzzy(movSource, _allSistema.Where(s => s.Estado == EstadosMovimiento.NoEncontrado || s.Estado == EstadosMovimiento.PosibleMatch).ToList());
                    if (candidatos.Count > 0)
                    {
                        if (_cmbFiltroSistEst.SelectedIndex != 0)
                        {
                            _cmbFiltroSistEst.SelectedIndex = 0;
                        }
                        targetGrid.DataSource = new SortableBindingList<Movimiento>(candidatos);
                    }
                }
            }
            finally
            {
                _isUpdatingSelection = false;
            }
        }

        private void BtnVincular_Click(object sender, EventArgs e)
        {
            if (_gridBanco.SelectedRows.Count == 0 || _gridSistema.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione una fila en el Banco y una fila en el Sistema para vincularlos.");
                return;
            }

            var movBanco = _gridBanco.SelectedRows[0].DataBoundItem as Movimiento;
            var movSistema = _gridSistema.SelectedRows[0].DataBoundItem as Movimiento;

            if (movBanco != null && movSistema != null)
            {
                _engine.MarcarConciliado(movBanco, movSistema, "Match Manual");
                LoadData();
                MessageBox.Show("Movimientos vinculados correctamente.");
            }
        }

        private void EliminarFilas(DataGridView grid)
        {
            if (grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione al menos una fila para eliminar.");
                return;
            }

            var res = MessageBox.Show($"¿Está seguro que desea eliminar lógicamente {grid.SelectedRows.Count} movimiento(s)? Si estaban vinculados, su contraparte volverá a estado 'No encontrado'.", "Confirmar Eliminación", MessageBoxButtons.YesNo);
            if (res == DialogResult.Yes)
            {
                foreach(DataGridViewRow row in grid.SelectedRows)
                {
                    var mov = row.DataBoundItem as Movimiento;
                    if (mov != null)
                    {
                        _movRepo.DeleteMovimientoAndUnmatch(mov.Id);
                    }
                }
                LoadData();
            }
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = sender as DataGridView;
            var mov = grid.Rows[e.RowIndex].DataBoundItem as Movimiento;
            if (mov != null)
            {
                // Explicitly pass MatchId so we don't accidentally wipe it
                _movRepo.UpdateEstado(mov.Id, mov.Estado, mov.Observaciones, mov.MatchId);

                // If it was changed manually via the dropdown away from Conciliado, trigger UI refresh to unmatch
                if (mov.Estado != EstadosMovimiento.Conciliado && mov.MatchId.HasValue)
                {
                    LoadData();
                }
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
                 LoadBancosDropdown();
             }
        }

        private void BtnExportarMasivos_Click(object sender, EventArgs e)
        {
            using (var form = new ExportarAMForm())
            {
                form.ShowDialog();
            }
        }
    }
}
