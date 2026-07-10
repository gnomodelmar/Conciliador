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

        public ConfiguracionForm()
        {
            _confRepo = new ConfiguracionRepository();
            _reglasRepo = new ReglasRepository();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Configuración";
            this.Size = new Size(800, 600);

            var tabControl = new TabControl { Dock = DockStyle.Fill };

            var tabBancos = new TabPage("Mapeo Bancos");
            var btnSaveMock = new Button { Text = "Guardar Mock Banco BNA", Location = new Point(10, 20), Width = 200 };
            btnSaveMock.Click += (s, e) => {
                _confRepo.SaveConfiguracionBanco(new ConfiguracionBanco {
                    NombreBanco = "BNA",
                    ColumnaFecha = "FECHA",
                    ColumnaMonto = "IMPORTE",
                    ColumnaConcepto = "DETALLE"
                });
                MessageBox.Show("Configuración de banco guardada.");
            };
            tabBancos.Controls.Add(btnSaveMock);

            var tabReglas = new TabPage("Reglas Asientos Masivos");

            var lblNombre = new Label { Text = "Nombre Regla:", Location = new Point(10, 20), Width = 100 };
            var txtNombre = new TextBox { Location = new Point(120, 20), Width = 200 };

            var lblPalabra = new Label { Text = "Palabra Clave:", Location = new Point(10, 50), Width = 100 };
            var txtPalabra = new TextBox { Location = new Point(120, 50), Width = 200 };

            var lblBanco = new Label { Text = "Banco:", Location = new Point(10, 80), Width = 100 };
            var txtBanco = new TextBox { Location = new Point(120, 80), Width = 200 };

            var lblAyuda = new Label { Text = "Variables permitidas en Concepto: {MM/YYYY}, {BANCO}", Location = new Point(350, 20), AutoSize = true, ForeColor = Color.Gray };

            var gridDetalles = new DataGridView
            {
                Location = new Point(10, 120),
                Size = new Size(700, 200),
                AutoGenerateColumns = false
            };
            var cmbTipo = new DataGridViewComboBoxColumn { Name = "Tipo", DataPropertyName = "Tipo", HeaderText = "Tipo" };
            cmbTipo.Items.AddRange(new[] { "Entrada", "Salida" });
            gridDetalles.Columns.Add(cmbTipo);
            gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "IdCuenta", DataPropertyName = "IdCuenta", HeaderText = "ID Cuenta" });
            gridDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "ConceptoTemplate", DataPropertyName = "ConceptoTemplate", HeaderText = "Concepto Template", Width = 300 });

            var bindingList = new System.ComponentModel.BindingList<ReglaAsientoDetalle>();
            gridDetalles.DataSource = bindingList;

            var btnGuardarRegla = new Button { Text = "Guardar Regla", Location = new Point(10, 340), Width = 150 };
            btnGuardarRegla.Click += (s, e) => {
                var regla = new ReglaAsiento
                {
                    NombreRegla = txtNombre.Text,
                    PalabraClave = txtPalabra.Text,
                    Banco = txtBanco.Text,
                    Detalles = new List<ReglaAsientoDetalle>(bindingList)
                };

                try
                {
                    _reglasRepo.SaveRegla(regla);
                    MessageBox.Show("Regla guardada correctamente.");
                    bindingList.Clear();
                    txtNombre.Clear();
                    txtPalabra.Clear();
                    txtBanco.Clear();
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error guardando regla: " + ex.Message);
                }
            };

            tabReglas.Controls.Add(lblNombre);
            tabReglas.Controls.Add(txtNombre);
            tabReglas.Controls.Add(lblPalabra);
            tabReglas.Controls.Add(txtPalabra);
            tabReglas.Controls.Add(lblBanco);
            tabReglas.Controls.Add(txtBanco);
            tabReglas.Controls.Add(lblAyuda);
            tabReglas.Controls.Add(gridDetalles);
            tabReglas.Controls.Add(btnGuardarRegla);

            tabControl.TabPages.Add(tabBancos);
            tabControl.TabPages.Add(tabReglas);

            this.Controls.Add(tabControl);
        }
    }
}
