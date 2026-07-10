using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConciliadorBancario.Data;
using ConciliadorBancario.Models;
using ConciliadorBancario.Services;

namespace ConciliadorBancario.Forms
{
    public class ExportarAMForm : Form
    {
        private ComboBox _cmbReglas;
        private Button _btnExportar;
        private ReglasRepository _reglasRepo;

        public ExportarAMForm()
        {
            _reglasRepo = new ReglasRepository();
            InitializeComponent();
            LoadReglas();
        }

        private void InitializeComponent()
        {
            this.Text = "Exportar Asientos Masivos";
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterParent;

            var lblInstruccion = new Label { Text = "Seleccione la regla a exportar:", Location = new Point(20, 20), AutoSize = true };
            this.Controls.Add(lblInstruccion);

            _cmbReglas = new ComboBox { Location = new Point(20, 50), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            this.Controls.Add(_cmbReglas);

            _btnExportar = new Button { Text = "Exportar Seleccionada", Location = new Point(120, 100), Width = 150 };
            _btnExportar.Click += BtnExportar_Click;
            this.Controls.Add(_btnExportar);
        }

        private void LoadReglas()
        {
            var reglas = _reglasRepo.GetAllReglas();
            _cmbReglas.Items.Add(new ReglaItem { Id = null, Nombre = "--- TODAS LAS REGLAS ---" });
            foreach (var r in reglas)
            {
                _cmbReglas.Items.Add(new ReglaItem { Id = r.Id, Nombre = r.NombreRegla });
            }
            _cmbReglas.SelectedIndex = 0;
        }

        private void BtnExportar_Click(object sender, EventArgs e)
        {
            var selected = _cmbReglas.SelectedItem as ReglaItem;
            if (selected == null) return;

            using (var sfd = new SaveFileDialog() { Filter = "CSV|*.csv|Excel|*.xlsx", FileName = "AsientosMasivos.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var exportService = new AsientoMasivoExportService();
                        exportService.ExportarAsientosMasivos(sfd.FileName, selected.Id);
                        MessageBox.Show("Exportación exitosa.");
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error en exportación: " + ex.Message);
                    }
                }
            }
        }

        private class ReglaItem
        {
            public int? Id { get; set; }
            public string Nombre { get; set; }
            public override string ToString() => Nombre;
        }
    }
}
