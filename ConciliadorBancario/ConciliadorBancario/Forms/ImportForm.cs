using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConciliadorBancario.Models;
using ConciliadorBancario.Services;
using ConciliadorBancario.Data;

namespace ConciliadorBancario.Forms
{
    public class ImportForm : Form
    {
        private string _tipoFuente; // "Banco" o "Sistema"
        private ComboBox _cmbBancos;
        private TextBox _txtFilePath;
        private DataGridView _gridPreview;
        private Button _btnCargar;
        private Button _btnGuardar;

        private ImportadorService _importadorService;
        private ConfiguracionRepository _confRepo;

        private List<Movimiento> _movimientosPrevia;

        public ImportForm(string tipoFuente)
        {
            _tipoFuente = tipoFuente;
            _importadorService = new ImportadorService();
            _confRepo = new ConfiguracionRepository();
            _movimientosPrevia = new List<Movimiento>();

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = $"Importar {_tipoFuente}";
            this.Size = new Size(800, 600);

            var panelTop = new Panel { Dock = DockStyle.Top, Height = 60 };

            _txtFilePath = new TextBox { Location = new Point(10, 10), Width = 300, ReadOnly = true };
            var btnSelectFile = new Button { Text = "Seleccionar Archivo", Location = new Point(320, 8), Width = 150 };
            btnSelectFile.Click += BtnSelectFile_Click;

            panelTop.Controls.Add(_txtFilePath);
            panelTop.Controls.Add(btnSelectFile);

            if (_tipoFuente == "Banco")
            {
                var lblBanco = new Label { Text = "Banco:", Location = new Point(10, 35), Width = 50 };
                _cmbBancos = new ComboBox { Location = new Point(70, 35), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
                // Fake static list for now, should come from DB config
                _cmbBancos.Items.AddRange(new[] { "Galicia", "Santander", "BNA", "BBVA" });
                if (_cmbBancos.Items.Count > 0) _cmbBancos.SelectedIndex = 0;

                panelTop.Controls.Add(lblBanco);
                panelTop.Controls.Add(_cmbBancos);
            }

            _btnCargar = new Button { Text = "Cargar Vista Previa", Location = new Point(480, 8), Width = 150 };
            _btnCargar.Click += BtnCargar_Click;
            panelTop.Controls.Add(_btnCargar);

            _gridPreview = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false
            };

            _gridPreview.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Incluir", HeaderText = "Incluir", DataPropertyName = "Activo", Width = 50 });
            _gridPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha", HeaderText = "Fecha", DataPropertyName = "Fecha", Width = 80 });
            _gridPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Monto", HeaderText = "Monto", DataPropertyName = "Monto", Width = 80 });
            _gridPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Concepto", HeaderText = "Concepto", DataPropertyName = "Concepto", Width = 200 });
            _gridPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Obs", HeaderText = "Observaciones", DataPropertyName = "Observaciones", Width = 200 });

            var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            _btnGuardar = new Button { Text = "Confirmar Importación", Location = new Point(10, 10), Width = 200, Enabled = false };
            _btnGuardar.Click += BtnGuardar_Click;
            panelBottom.Controls.Add(_btnGuardar);

            this.Controls.Add(_gridPreview);
            this.Controls.Add(panelTop);
            this.Controls.Add(panelBottom);
        }

        private void BtnSelectFile_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = "Excel/CSV|*.xlsx;*.xls;*.csv" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _txtFilePath.Text = ofd.FileName;
                }
            }
        }

        private void BtnCargar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_txtFilePath.Text))
            {
                MessageBox.Show("Seleccione un archivo.");
                return;
            }

            try
            {
                if (_tipoFuente == "Banco")
                {
                    string banco = _cmbBancos.SelectedItem.ToString();
                    // Setup default mock config if it doesn't exist
                    var conf = _confRepo.GetConfiguracionBanco(banco);
                    if (conf == null)
                    {
                         _confRepo.SaveConfiguracionBanco(new ConfiguracionBanco {
                             NombreBanco = banco, ColumnaFecha = "Fecha", ColumnaMonto = "Monto", ColumnaConcepto = "Concepto"
                         });
                    }

                    _movimientosPrevia = _importadorService.PrepararImportacionBanco(_txtFilePath.Text, banco);
                }
                else
                {
                    var conf = new ConfiguracionBanco { ColumnaFecha = "Fecha", ColumnaMonto = "Importe", ColumnaConcepto = "Concepto", ColumnaReferencia = "ID" };
                    _movimientosPrevia = _importadorService.PrepararImportacionSistema(_txtFilePath.Text, conf);
                }

                // If duplicates found, disable "Activo" (Incluir) by default
                foreach (var m in _movimientosPrevia)
                {
                    if (!string.IsNullOrEmpty(m.Observaciones) && m.Observaciones.Contains("Duplicado"))
                    {
                        m.Activo = false; // User can re-check it in UI
                    }
                }

                _gridPreview.DataSource = new System.ComponentModel.BindingList<Movimiento>(_movimientosPrevia);
                _btnGuardar.Enabled = _movimientosPrevia.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando archivo: {ex.Message}");
            }
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            // Only import those that have Activo == true in the preview
            var seleccionados = _movimientosPrevia.Where(m => m.Activo).ToList();
            if (seleccionados.Count == 0)
            {
                MessageBox.Show("No hay registros seleccionados para importar.");
                return;
            }

            try
            {
                _importadorService.GuardarLote(seleccionados, _txtFilePath.Text, _tipoFuente);
                MessageBox.Show("Importación exitosa.");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error guardando importación: {ex.Message}");
            }
        }
    }
}
