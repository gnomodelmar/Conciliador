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
        private Button _btnDesvincular;
        private Button _btnEliminarBanco;
        private Button _btnEliminarSistema;

        private ComboBox _cmbFiltroBancoEst;
        private ComboBox _cmbFiltroBancoBco;
        private DateTimePicker _dtBancoDesde;
        private DateTimePicker _dtBancoHasta;
        private TextBox _txtSearchBanco;

        private ComboBox _cmbFiltroSistEst;
        private ComboBox _cmbFiltroSistBco;
        private DateTimePicker _dtSistDesde;
        private DateTimePicker _dtSistHasta;
        private TextBox _txtSearchSist;

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

            _btnAutoConciliar = new Button { Text = "Auto-Conciliar", Location = new Point(10, 15), Width = 100 };
            _btnAutoConciliar.Click += (s, e) => { _engine.AutoConciliar(); LoadData(); MessageBox.Show("Auto-conciliación finalizada."); };

            _btnExportarMasivos = new Button { Text = "Exportar Asientos Mas.", Location = new Point(120, 15), Width = 150 };
            _btnExportarMasivos.Click += BtnExportarMasivos_Click;

            _btnVincular = new Button { Text = "Vincular Sel.", Location = new Point(280, 15), Width = 120 };
            _btnVincular.Click += BtnVincular_Click;

            _btnDesvincular = new Button { Text = "Desvincular Sel.", Location = new Point(410, 15), Width = 120 };
            _btnDesvincular.Click += BtnDesvincular_Click;

            _btnEliminarBanco = new Button { Text = "Eliminar Sel. Banco", Location = new Point(540, 15), Width = 130 };
            _btnEliminarBanco.Click += (s, e) => EliminarFilas(_gridBanco);

            _btnEliminarSistema = new Button { Text = "Eliminar Sel. Sistema", Location = new Point(680, 15), Width = 130 };
            _btnEliminarSistema.Click += (s, e) => EliminarFilas(_gridSistema);

            panelBottom.Controls.Add(_btnAutoConciliar);
            panelBottom.Controls.Add(_btnExportarMasivos);
            panelBottom.Controls.Add(_btnVincular);
            panelBottom.Controls.Add(_btnDesvincular);
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
            var headerIzquierdo = new Panel { Dock = DockStyle.Top, Height = 120 };
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

            headerIzquierdo.Controls.Add(new Label { Text = "Buscar:", Location = new Point(10, 95), AutoSize = true });
            _txtSearchBanco = new TextBox { Location = new Point(60, 92), Width = 260 };
            _txtSearchBanco.TextChanged += (s, e) => FilterData();

            var btnResetBanco = new Button { Text = "Limpiar Filtros", Location = new Point(330, 90), Width = 100 };
            btnResetBanco.Click += (s, e) => { _txtSearchBanco.Clear(); _cmbFiltroBancoEst.SelectedIndex=0; _cmbFiltroBancoBco.SelectedIndex=0; _dtBancoDesde.Value=new DateTime(2000,1,1); _dtBancoHasta.Value=new DateTime(2100,1,1); };

            headerIzquierdo.Controls.Add(_cmbFiltroBancoEst);
            headerIzquierdo.Controls.Add(_cmbFiltroBancoBco);
            headerIzquierdo.Controls.Add(_dtBancoDesde);
            headerIzquierdo.Controls.Add(_dtBancoHasta);
            headerIzquierdo.Controls.Add(_txtSearchBanco);
            headerIzquierdo.Controls.Add(btnResetBanco);

            _gridBanco = CrearGrid(true);
            panelIzquierdo.Controls.Add(_gridBanco);
            panelIzquierdo.Controls.Add(headerIzquierdo);

            // --- Panel Sistema ---
            var panelDerecho = new Panel { Dock = DockStyle.Fill };
            var headerDerecho = new Panel { Dock = DockStyle.Top, Height = 120 };
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

            headerDerecho.Controls.Add(new Label { Text = "Buscar:", Location = new Point(10, 95), AutoSize = true });
            _txtSearchSist = new TextBox { Location = new Point(60, 92), Width = 260 };
            _txtSearchSist.TextChanged += (s, e) => FilterData();

            var btnResetSist = new Button { Text = "Limpiar Filtros", Location = new Point(330, 90), Width = 100 };
            btnResetSist.Click += (s, e) => { _txtSearchSist.Clear(); _cmbFiltroSistEst.SelectedIndex=0; _cmbFiltroSistBco.SelectedIndex=0; _dtSistDesde.Value=new DateTime(2000,1,1); _dtSistHasta.Value=new DateTime(2100,1,1); };

            headerDerecho.Controls.Add(_cmbFiltroSistEst);
            headerDerecho.Controls.Add(_cmbFiltroSistBco);
            headerDerecho.Controls.Add(_dtSistDesde);
            headerDerecho.Controls.Add(_dtSistHasta);
            headerDerecho.Controls.Add(_txtSearchSist);
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

            _gridBanco.CellFormatting += Grid_CellFormatting;
            _gridSistema.CellFormatting += Grid_CellFormatting;

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

            int scrollBco = _gridBanco.FirstDisplayedScrollingRowIndex;
            int scrollSis = _gridSistema.FirstDisplayedScrollingRowIndex;

            string estadoBco = _cmbFiltroBancoEst.SelectedItem?.ToString() ?? "Todos";
            string bancoBco = _cmbFiltroBancoBco.SelectedItem?.ToString() ?? "Todos";
            DateTime desdeBco = _dtBancoDesde.Value.Date;
            DateTime hastaBco = _dtBancoHasta.Value.Date;
            string searchBco = _txtSearchBanco.Text.Trim().ToLower();

            var bancoList = _allBanco.Where(m =>
                (estadoBco == "Todos" || m.Estado == estadoBco) &&
                (bancoBco == "Todos" || m.Banco == bancoBco) &&
                m.Fecha.Date >= desdeBco && m.Fecha.Date <= hastaBco &&
                (string.IsNullOrEmpty(searchBco) ||
                 (m.Concepto != null && m.Concepto.ToLower().Contains(searchBco)) ||
                 m.Monto.ToString().Contains(searchBco) ||
                 (m.Referencia_CodOperacion != null && m.Referencia_CodOperacion.ToLower().Contains(searchBco)))
            ).ToList();

            string estadoSis = _cmbFiltroSistEst.SelectedItem?.ToString() ?? "Todos";
            string bancoSis = _cmbFiltroSistBco.SelectedItem?.ToString() ?? "Todos";
            DateTime desdeSis = _dtSistDesde.Value.Date;
            DateTime hastaSis = _dtSistHasta.Value.Date;
            string searchSis = _txtSearchSist.Text.Trim().ToLower();

            var sistemaList = _allSistema.Where(m =>
                (estadoSis == "Todos" || m.Estado == estadoSis) &&
                (bancoSis == "Todos" || m.Banco == bancoSis) &&
                m.Fecha.Date >= desdeSis && m.Fecha.Date <= hastaSis &&
                (string.IsNullOrEmpty(searchSis) ||
                 (m.Concepto != null && m.Concepto.ToLower().Contains(searchSis)) ||
                 m.Monto.ToString().Contains(searchSis) ||
                 (m.Referencia_CodOperacion != null && m.Referencia_CodOperacion.ToLower().Contains(searchSis)) ||
                 (m.CodOperacionSistema != null && m.CodOperacionSistema.ToLower().Contains(searchSis)))
            ).ToList();

            _gridBanco.DataSource = new SortableBindingList<Movimiento>(bancoList);
            _gridSistema.DataSource = new SortableBindingList<Movimiento>(sistemaList);

            if (scrollBco >= 0 && scrollBco < _gridBanco.Rows.Count) _gridBanco.FirstDisplayedScrollingRowIndex = scrollBco;
            if (scrollSis >= 0 && scrollSis < _gridSistema.Rows.Count) _gridSistema.FirstDisplayedScrollingRowIndex = scrollSis;
        }

        private void InteraccionGrilla(DataGridView sourceGrid, DataGridView targetGrid, bool isSourceBanco, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var movSource = sourceGrid.Rows[e.RowIndex].DataBoundItem as Movimiento;
            if (movSource == null) return;

            try
            {
                if (movSource.Estado == EstadosMovimiento.Conciliado && (movSource.MatchId.HasValue || !string.IsNullOrEmpty(movSource.MatchGrupoId)))
                {
                    ComboBox targetComboEst = isSourceBanco ? _cmbFiltroSistEst : _cmbFiltroBancoEst;
                    if (targetComboEst.SelectedIndex != 0 && targetComboEst.SelectedItem.ToString() != EstadosMovimiento.Conciliado)
                    {
                        _isUpdatingSelection = false;
                        targetComboEst.SelectedIndex = 0;
                        FilterData();
                        _isUpdatingSelection = true;
                    }

                    targetGrid.ClearSelection();
                    int firstScrolled = -1;
                    foreach (DataGridViewRow row in targetGrid.Rows)
                    {
                        var m = row.DataBoundItem as Movimiento;
                        if (m != null)
                        {
                            if ((movSource.MatchId.HasValue && m.Id == movSource.MatchId.Value) ||
                                (!string.IsNullOrEmpty(movSource.MatchGrupoId) && m.MatchGrupoId == movSource.MatchGrupoId))
                            {
                                row.Selected = true;
                                if (firstScrolled == -1) firstScrolled = row.Index;
                            }
                        }
                    }
                    if (firstScrolled != -1)
                    {
                        targetGrid.FirstDisplayedScrollingRowIndex = firstScrolled;
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

                        int scrollSis = targetGrid.FirstDisplayedScrollingRowIndex;
                        targetGrid.DataSource = new SortableBindingList<Movimiento>(candidatos);
                        if (scrollSis >= 0 && scrollSis < targetGrid.Rows.Count) targetGrid.FirstDisplayedScrollingRowIndex = scrollSis;
                    }
                }
            }
            finally
            {
                _isUpdatingSelection = false;
            }
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < ((DataGridView)sender).Rows.Count)
            {
                var mov = ((DataGridView)sender).Rows[e.RowIndex].DataBoundItem as Movimiento;
                if (mov != null)
                {
                    switch (mov.Estado)
                    {
                        case EstadosMovimiento.Conciliado:
                            e.CellStyle.BackColor = Color.LightGreen;
                            break;
                        case EstadosMovimiento.PosibleMatch:
                            e.CellStyle.BackColor = Color.LightYellow;
                            break;
                        case EstadosMovimiento.PendienteAsientoMasivo:
                            e.CellStyle.BackColor = Color.LightCyan;
                            break;
                        case EstadosMovimiento.PendienteCorregir:
                        case EstadosMovimiento.PendientePasar:
                            e.CellStyle.BackColor = Color.LightCoral;
                            break;
                        default:
                            e.CellStyle.BackColor = Color.White;
                            break;
                    }
                }
            }
        }

        private void BtnVincular_Click(object sender, EventArgs e)
        {
            if (_gridBanco.SelectedRows.Count == 0 || _gridSistema.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione al menos una fila en el Banco y una fila en el Sistema para vincularlos.");
                return;
            }

            var movsBanco = _gridBanco.SelectedRows.Cast<DataGridViewRow>().Select(r => r.DataBoundItem as Movimiento).ToList();
            var movsSistema = _gridSistema.SelectedRows.Cast<DataGridViewRow>().Select(r => r.DataBoundItem as Movimiento).ToList();

            if (movsBanco.Count == 1 && movsSistema.Count == 1)
            {
                _engine.MarcarConciliado(movsBanco[0], movsSistema[0], "Match Manual 1a1");
            }
            else
            {
                _engine.MarcarConciliadoMulti(movsBanco, movsSistema, $"Match Manual Multi ({movsBanco.Count} Bco vs {movsSistema.Count} Sist)");
            }

            LoadData();
            MessageBox.Show("Movimientos vinculados correctamente.");
        }

        private void BtnDesvincular_Click(object sender, EventArgs e)
        {
            // Handle unlinking any selected rows across both grids
            var toUnlink = new List<Movimiento>();
            foreach(DataGridViewRow r in _gridBanco.SelectedRows)
            {
                var m = r.DataBoundItem as Movimiento;
                if (m != null && m.Estado == EstadosMovimiento.Conciliado) toUnlink.Add(m);
            }
            foreach(DataGridViewRow r in _gridSistema.SelectedRows)
            {
                var m = r.DataBoundItem as Movimiento;
                if (m != null && m.Estado == EstadosMovimiento.Conciliado) toUnlink.Add(m);
            }

            if (toUnlink.Count == 0)
            {
                MessageBox.Show("Seleccione al menos un movimiento 'Conciliado' para desvincular.");
                return;
            }

            var res = MessageBox.Show($"¿Desea desvincular {toUnlink.Count} movimiento(s)? Esto devolverá su estado a 'No encontrado'.", "Confirmar", MessageBoxButtons.YesNo);
            if (res == DialogResult.Yes)
            {
                foreach(var mov in toUnlink)
                {
                    // UpdateEstado inherently triggers the cascade unmatch logic if changing away from Conciliado
                    _movRepo.UpdateEstado(mov.Id, EstadosMovimiento.NoEncontrado, "Desvinculado manualmente", mov.MatchId);
                }
                LoadData();
                MessageBox.Show("Movimientos desvinculados correctamente.");
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
                _movRepo.UpdateEstado(mov.Id, mov.Estado, mov.Observaciones, mov.MatchId);

                if (mov.Estado != EstadosMovimiento.Conciliado && (mov.MatchId.HasValue || !string.IsNullOrEmpty(mov.MatchGrupoId)))
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
