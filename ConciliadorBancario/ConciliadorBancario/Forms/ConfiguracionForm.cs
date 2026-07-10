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
            this.Size = new Size(1000, 700);

            var tabControl = new TabControl { Dock = DockStyle.Fill };

            // --- TAB BANCOS ---
            var tabBancos = new TabPage("Mapeo Bancos");

            _gridBancos = new DataGridView
            {
                Location = new Point(10, 10),
                Size = new Size(950, 250),
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true
            };

            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "NombreBanco", DataPropertyName = "NombreBanco", HeaderText = "Nombre del Banco (Obligatorio)", Width = 150 });
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColumnaFecha", DataPropertyName = "ColumnaFecha", HeaderText = "Col. Fecha", Width = 100 });
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColumnaMonto", DataPropertyName = "ColumnaMonto", HeaderText = "Col. Monto (Unica)", Width = 100 });
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColumnaMontoEntrada", DataPropertyName = "ColumnaMontoEntrada", HeaderText = "Col. Entrada", Width = 100 });
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColumnaMontoSalida", DataPropertyName = "ColumnaMontoSalida", HeaderText = "Col. Salida", Width = 100 });
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColumnaConcepto", DataPropertyName = "ColumnaConcepto", HeaderText = "Col. Concepto", Width = 100 });
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColumnaReferencia", DataPropertyName = "ColumnaReferencia", HeaderText = "Col. Referencia", Width = 100 });
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColumnaTipo", DataPropertyName = "ColumnaTipo", HeaderText = "Col. Tipo", Width = 80 });
            _gridBancos.Columns.Add(new DataGridViewTextBoxColumn { Name = "ValorTipoSalida", DataPropertyName = "ValorTipoSalida", HeaderText = "Valor que resta", Width = 100 });

            var btnSaveBancos = new Button { Text = "Guardar Cambios Bancos", Location = new Point(10, 270), Width = 200 };
            btnSaveBancos.Click += BtnSaveBancos_Click;

            var btnDeleteBancos = new Button { Text = "Eliminar Banco Seleccionado", Location = new Point(220, 270), Width = 200 };
            btnDeleteBancos.Click += BtnDeleteBancos_Click;

            tabBancos.Controls.Add(_gridBancos);
            tabBancos.Controls.Add(btnSaveBancos);
            tabBancos.Controls.Add(btnDeleteBancos);

            // --- TAB REGLAS ---
            var tabReglas = new TabPage("Reglas Asientos Masivos");

            // Left Panel (List rules)
            var panelReglasIzquierdo = new Panel { Dock = DockStyle.Left, Width = 300 };
            _gridReglas = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, ReadOnly = true, AllowUserToAddRows = false };
            _gridReglas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", Visible = false });
            _gridReglas.Columns.Add(new DataGridViewTextBoxColumn { Name = "NombreRegla", DataPropertyName = "NombreRegla", HeaderText = "Nombre", Width = 150 });
            _gridReglas.Columns.Add(new DataGridViewTextBoxColumn { Name = "Banco", DataPropertyName = "Banco", HeaderText = "Banco", Width = 100 });

            var btnEliminarRegla = new Button { Text = "Eliminar Regla Seleccionada", Dock = DockStyle.Bottom };
            btnEliminarRegla.Click += BtnEliminarRegla_Click;
            panelReglasIzquierdo.Controls.Add(_gridReglas);
            panelReglasIzquierdo.Controls.Add(btnEliminarRegla);

            // Right Panel (Create rule)
            var panelReglasDerecho = new Panel { Dock = DockStyle.Fill };

            var lblTituloCreate = new Label { Text = "Crear Nueva Regla", Location = new Point(10, 10), Font = new Font("Arial", 10, FontStyle.Bold), AutoSize = true };
            var lblNombre = new Label { Text = "Nombre Regla:", Location = new Point(10, 40), Width = 100 };
            var txtNombre = new TextBox { Location = new Point(120, 40), Width = 200 };

            var lblPalabra = new Label { Text = "Palabra Clave:", Location = new Point(10, 70), Width = 100 };
            var txtPalabra = new TextBox { Location = new Point(120, 70), Width = 200 };

            var lblBanco = new Label { Text = "Banco:", Location = new Point(10, 100), Width = 100 };
            _cmbBancoReglas = new ComboBox { Location = new Point(120, 100), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblAyuda = new Label { Text = "Variables permitidas en Concepto: {MM/YYYY}, {BANCO}", Location = new Point(350, 40), AutoSize = true, ForeColor = Color.Gray };

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

            var bindingListDetalles = new System.ComponentModel.BindingList<ReglaAsientoDetalle>();
            gridDetalles.DataSource = bindingListDetalles;

            var btnGuardarRegla = new Button { Text = "Guardar Regla", Location = new Point(10, 400), Width = 150 };
            btnGuardarRegla.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtPalabra.Text) || _cmbBancoReglas.SelectedItem == null)
                {
                    MessageBox.Show("Debe completar Nombre, Palabra Clave y Banco.");
                    return;
                }

                var regla = new ReglaAsiento
                {
                    NombreRegla = txtNombre.Text,
                    PalabraClave = txtPalabra.Text,
                    Banco = _cmbBancoReglas.SelectedItem.ToString(),
                    Detalles = new List<ReglaAsientoDetalle>(bindingListDetalles)
                };

                try
                {
                    _reglasRepo.SaveRegla(regla);
                    MessageBox.Show("Regla guardada correctamente.");
                    bindingListDetalles.Clear();
                    txtNombre.Clear();
                    txtPalabra.Clear();
                    LoadReglas();
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error guardando regla: " + ex.Message);
                }
            };

            panelReglasDerecho.Controls.Add(lblTituloCreate);
            panelReglasDerecho.Controls.Add(lblNombre);
            panelReglasDerecho.Controls.Add(txtNombre);
            panelReglasDerecho.Controls.Add(lblPalabra);
            panelReglasDerecho.Controls.Add(txtPalabra);
            panelReglasDerecho.Controls.Add(lblBanco);
            panelReglasDerecho.Controls.Add(_cmbBancoReglas);
            panelReglasDerecho.Controls.Add(lblAyuda);
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
            var bancos = _confRepo.GetAllBancos();
            _gridBancos.DataSource = new System.ComponentModel.BindingList<ConfiguracionBanco>(bancos);

            _cmbBancoReglas.Items.Clear();
            foreach(var b in bancos)
            {
                _cmbBancoReglas.Items.Add(b.NombreBanco);
            }
        }

        private void LoadReglas()
        {
            var reglas = _reglasRepo.GetAllReglas();
            _gridReglas.DataSource = new System.ComponentModel.BindingList<ReglaAsiento>(reglas);
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
                            _confRepo.SaveConfiguracionBanco(conf);
                        }
                    }
                    MessageBox.Show("Bancos guardados correctamente.");
                    LoadBancos(); // Refresh dropdowns
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
                    var res = MessageBox.Show($"¿Eliminar configuración de {conf.NombreBanco}?", "Confirmar", MessageBoxButtons.YesNo);
                    if (res == DialogResult.Yes)
                    {
                        _confRepo.DeleteConfiguracionBanco(conf.NombreBanco);
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
