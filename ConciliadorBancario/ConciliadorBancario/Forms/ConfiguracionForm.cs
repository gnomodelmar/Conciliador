using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using ConciliadorBancario.Data;
using ConciliadorBancario.Models;

namespace ConciliadorBancario.Forms
{
    public class ConfiguracionForm : Form
    {
        private ConfiguracionRepository _confRepo;
        private ReglasRepository _reglasRepo;

        private DataGridView _gridBancos;
        private DataGridView _gridReglas;
        private ComboBox _cmbBancoReglas;

        private int _currentReglaId = 0;
        private TextBox _txtNombreRegla;
        private TextBox _txtPalabraClave;
        private System.ComponentModel.BindingList<ReglaAsientoDetalle> _bindingListDetalles;

        public ConfiguracionForm()
        {
            _confRepo = new ConfiguracionRepository();
            _reglasRepo = new ReglasRepository();
            InitializeComponent();
            LoadBancos();
            LoadReglas();
        }

        private void InitializeComponent()
        {
            this.Text = "Configuración";
            this.Size = new Size(1250, 700);

            var tabControl = new TabControl { Dock = DockStyle.Fill };

            // --- TAB BANCOS ---
            var tabBancos = new TabPage("Mapeo Bancos");

            var lblAyuda = new Label {
                Text = "Nota: El mapeo del Sistema Contable está unificado y pre-configurado.\nAquí solo configure cómo leer los Excel provenientes de los Bancos.",
                Location = new Point(10, 10),
                AutoSize = true,
                ForeColor = Color.Blue
            };

            _gridBancos = new DataGridView
            {
                Location = new Point(10, 50),
                Size = new Size(1200, 210),
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true
            };

            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "NombreBanco", DataPropertyName = "NombreBanco", HeaderText = "Nombre del Banco", Width = 130 });

            // Only Bank mappings are editable now
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColumnaFecha", DataPropertyName = "ColumnaFecha", HeaderText = "Col. Fecha", Width = 100 });
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColumnaMonto", DataPropertyName = "ColumnaMonto", HeaderText = "Col. Monto (Unica)", Width = 100 });
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColumnaMontoEntrada", DataPropertyName = "ColumnaMontoEntrada", HeaderText = "Col. Entrada", Width = 100 });
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColumnaMontoSalida", DataPropertyName = "ColumnaMontoSalida", HeaderText = "Col. Salida", Width = 100 });
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColumnaConcepto", DataPropertyName = "ColumnaConcepto", HeaderText = "Col. Concepto", Width = 100 });
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColumnaReferencia", DataPropertyName = "ColumnaReferencia", HeaderText = "Col. Ref. Banco", Width = 100 });
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColumnaCodOperacionSistema", DataPropertyName = "ColumnaCodOperacionSistema", HeaderText = "Col. Ref. Sist.", Width = 100 });
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColumnaTipo", DataPropertyName = "ColumnaTipo", HeaderText = "Col. Tipo", Width = 80 });
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ValorTipoSalida", DataPropertyName = "ValorTipoSalida", HeaderText = "Valor que resta", Width = 100 });

            var btnSaveBancos = new Button { Text = "Guardar Cambios Mapeos", Location = new Point(10, 270), Width = 200 };
            btnSaveBancos.Click += BtnSaveBancos_Click;

            var btnDeleteBancos = new Button { Text = "Eliminar Mapeo Seleccionado", Location = new Point(220, 270), Width = 200 };
            btnDeleteBancos.Click += BtnDeleteBancos_Click;

            tabBancos.Controls.Add(lblAyuda);
            tabBancos.Controls.Add(_gridBancos);
            tabBancos.Controls.Add(btnSaveBancos);
            tabBancos.Controls.Add(btnDeleteBancos);

            // --- TAB REGLAS ---
            var tabReglas = new TabPage("Reglas Asientos Masivos");

            var panelReglasIzquierdo = new Panel { Dock = DockStyle.Left, Width = 300 };
            _gridReglas = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, ReadOnly = true, AllowUserToAddRows = false };
            _gridReglas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", Visible = false });
            _gridReglas.Columns.Add(new DataGridViewTextBoxColumn { Name = "NombreRegla", DataPropertyName = "NombreRegla", HeaderText = "Nombre", Width = 150 });
            _gridReglas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Banco", DataPropertyName = "Banco", HeaderText = "Banco", Width = 100 });

            _gridReglas.CellClick += GridReglas_CellClick;

            var btnEliminarRegla = new Button { Text = "Eliminar Regla Seleccionada", Dock = DockStyle.Bottom };
            btnEliminarRegla.Click += BtnEliminarRegla_Click;

            var btnNuevaRegla = new Button { Text = "Crear Nueva Regla (Limpiar)", Dock = DockStyle.Bottom };
            btnNuevaRegla.Click += (s, e) => {
                 _currentReglaId = 0;
                 _txtNombreRegla.Clear();
                 _txtPalabraClave.Clear();
                 _bindingListDetalles.Clear();
            };

            panelReglasIzquierdo.Controls.Add(_gridReglas);
            panelReglasIzquierdo.Controls.Add(btnNuevaRegla);
            panelReglasIzquierdo.Controls.Add(btnEliminarRegla);

            var panelReglasDerecho = new Panel { Dock = DockStyle.Fill };

            var lblTituloCreate = new Label { Text = "Editor de Regla", Location = new Point(10, 10), Font = new Font("Arial", 10, FontStyle.Bold), AutoSize = true };
            var lblNombreR = new Label { Text = "Nombre Regla:", Location = new Point(10, 40), Width = 100 };
            _txtNombreRegla = new TextBox { Location = new Point(120, 40), Width = 200 };

            var lblPalabra = new Label { Text = "Palabra Clave:", Location = new Point(10, 70), Width = 100 };
            _txtPalabraClave = new TextBox { Location = new Point(120, 70), Width = 200 };

            var lblBanco = new Label { Text = "Banco:", Location = new Point(10, 100), Width = 100 };
            _cmbBancoReglas = new ComboBox { Location = new Point(120, 100), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblAyudaR = new Label { Text = "Variables permitidas en Concepto: {MM/YYYY}, {BANCO}", Location = new Point(350, 40), AutoSize = true, ForeColor = Color.Gray };

            var gridDetalles = new DataGridView
            {
                Location = new Point(10, 140),
                Size = new Size(650, 250),
                AutoGenerateColumns = false
            };
            var cmbTipo = new DataGridViewComboBoxColumn { Name = "Tipo", DataPropertyName = "Tipo", HeaderText = "Tipo" };
            cmbTipo.Items.AddRange(new[] { "Entrada", "Salida" });
            gridDetalles.Columns.Add(cmbTipo);
            gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "IdCuenta", DataPropertyName = "IdCuenta", HeaderText = "ID Cuenta" });
            gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "ConceptoTemplate", DataPropertyName = "ConceptoTemplate", HeaderText = "Concepto Template", Width = 300 });

            _bindingListDetalles = new System.ComponentModel.BindingList<ReglaAsientoDetalle>();
            gridDetalles.DataSource = _bindingListDetalles;

            var btnGuardarRegla = new Button { Text = "Guardar Regla", Location = new Point(10, 400), Width = 150 };
            btnGuardarRegla.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(_txtNombreRegla.Text) || string.IsNullOrWhiteSpace(_txtPalabraClave.Text) || _cmbBancoReglas.SelectedItem == null)
                {
                    MessageBox.Show("Debe completar Nombre, Palabra Clave y Banco.");
                    return;
                }

                var regla = new ReglaAsiento
                {
                    Id = _currentReglaId,
                    NombreRegla = _txtNombreRegla.Text,
                    PalabraClave = _txtPalabraClave.Text,
                    Banco = _cmbBancoReglas.SelectedItem.ToString(),
                    Detalles = new List<ReglaAsientoDetalle>(_bindingListDetalles)
                };

                try
                {
                    _reglasRepo.SaveRegla(regla);
                    MessageBox.Show("Regla guardada correctamente.");
                    _currentReglaId = 0;
                    _bindingListDetalles.Clear();
                    _txtNombreRegla.Clear();
                    _txtPalabraClave.Clear();
                    LoadReglas();
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error guardando regla: " + ex.Message);
                }
            };

            panelReglasDerecho.Controls.Add(lblTituloCreate);
            panelReglasDerecho.Controls.Add(lblNombreR);
            panelReglasDerecho.Controls.Add(_txtNombreRegla);
            panelReglasDerecho.Controls.Add(lblPalabra);
            panelReglasDerecho.Controls.Add(_txtPalabraClave);
            panelReglasDerecho.Controls.Add(lblBanco);
            panelReglasDerecho.Controls.Add(_cmbBancoReglas);
            panelReglasDerecho.Controls.Add(lblAyudaR);
            panelReglasDerecho.Controls.Add(gridDetalles);
            panelReglasDerecho.Controls.Add(btnGuardarRegla);

            tabReglas.Controls.Add(panelReglasDerecho);
            tabReglas.Controls.Add(panelReglasIzquierdo);

            tabControl.TabPages.Add(tabBancos);
            tabControl.TabPages.Add(tabReglas);

            this.Controls.Add(tabControl);
        }

        private void LoadBancos()
        {
            var bancos = _confRepo.GetAllBancos("Banco"); // Only load Banks for editing
            _gridBancos.DataSource = new System.ComponentModel.BindingList<ConfiguracionBanco>(bancos);

            _cmbBancoReglas.Items.Clear();
            var uniqueBancos = new HashSet<string>();
            foreach(var b in bancos)
            {
                if (uniqueBancos.Add(b.NombreBanco))
                {
                    _cmbBancoReglas.Items.Add(b.NombreBanco);
                }
            }
        }

        private void LoadReglas()
        {
            var reglas = _reglasRepo.GetAllReglas();
            _gridReglas.DataSource = new System.ComponentModel.BindingList<ReglaAsiento>(reglas);
        }

        private void GridReglas_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var regla = _gridReglas.Rows[e.RowIndex].DataBoundItem as ReglaAsiento;
                if (regla != null)
                {
                    _currentReglaId = regla.Id;
                    _txtNombreRegla.Text = regla.NombreRegla;
                    _txtPalabraClave.Text = regla.PalabraClave;
                    _cmbBancoReglas.SelectedItem = regla.Banco;

                    _bindingListDetalles.Clear();
                    foreach (var det in regla.Detalles)
                    {
                        _bindingListDetalles.Add(det);
                    }
                }
            }
        }

        private void BtnSaveBancos_Click(object sender, EventArgs e)
        {
            try
            {
                var bindingList = _gridBancos.DataSource as System.ComponentModel.BindingList<ConfiguracionBanco>;
                if (bindingList != null)
                {
                    foreach (var conf in bindingList)
                    {
                        if (!string.IsNullOrWhiteSpace(conf.NombreBanco))
                        {
                            conf.TipoConfiguracion = "Banco"; // Enforce it
                            _confRepo.SaveConfiguracionBanco(conf);
                        }
                    }
                    MessageBox.Show("Bancos guardados correctamente.");
                    LoadBancos();
                }
            }
            catch (Exception ex)
            {
                 MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }

        private void BtnDeleteBancos_Click(object sender, EventArgs e)
        {
            if (_gridBancos.SelectedRows.Count > 0)
            {
                var conf = _gridBancos.SelectedRows[0].DataBoundItem as ConfiguracionBanco;
                if (conf != null && !string.IsNullOrWhiteSpace(conf.NombreBanco))
                {
                    var res = MessageBox.Show($"¿Eliminar configuración del banco {conf.NombreBanco}?", "Confirmar", MessageBoxButtons.YesNo);
                    if (res == DialogResult.Yes)
                    {
                        _confRepo.DeleteConfiguracionBanco(conf.NombreBanco, "Banco");
                        LoadBancos();
                    }
                }
            }
        }

        private void BtnEliminarRegla_Click(object sender, EventArgs e)
        {
            if (_gridReglas.SelectedRows.Count > 0)
            {
                var regla = _gridReglas.SelectedRows[0].DataBoundItem as ReglaAsiento;
                if (regla != null)
                {
                     var res = MessageBox.Show($"¿Eliminar regla {regla.NombreRegla}?", "Confirmar", MessageBoxButtons.YesNo);
                     if (res == DialogResult.Yes)
                     {
                         _reglasRepo.DeleteRegla(regla.Id);
                         LoadReglas();
                     }
                }
            }
        }
    }
}
