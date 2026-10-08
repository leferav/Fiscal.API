
using System;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using Fiscal.Agent.Services.Configuracao;

namespace Fiscal.Certificado.Manager
{
    public partial class FrmCertificado : Form
    {
        private readonly TextBox txtArquivo = new();
        private readonly TextBox txtSenha = new();
        private readonly Label lblTitular = new();
        private readonly Label lblCnpj = new();
        private readonly Label lblValidade = new();

        private Button? _btnSalvar;
        private bool _certificadoValidado = false;

        public FrmCertificado()
        {
            InitializeComponent();
            MontarInterface();
        }

        private void MontarInterface()
        {
            Text = "Fiscal - Gerenciamento de Certificado Digital";
            Size = new Size(650, 410);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            var lblArquivo = new Label
            {
                Text = "Certificado digital A1 (.pfx)",
                Location = new Point(25, 25),
                AutoSize = true
            };

            txtArquivo.Location = new Point(25, 50);
            txtArquivo.Size = new Size(460, 25);
            txtArquivo.ReadOnly = true;

            var btnProcurar = new Button
            {
                Text = "Procurar...",
                Location = new Point(495, 48),
                Size = new Size(110, 30)
            };
            btnProcurar.Click += BtnProcurar_Click;

            var lblSenha = new Label
            {
                Text = "Senha do certificado",
                Location = new Point(25, 100),
                AutoSize = true
            };

            txtSenha.Location = new Point(25, 125);
            txtSenha.Size = new Size(300, 25);
            txtSenha.UseSystemPasswordChar = true;

            var btnValidar = new Button
            {
                Text = "Validar certificado",
                Location = new Point(25, 175),
                Size = new Size(160, 35)
            };
            btnValidar.Click += BtnValidar_Click;

            _btnSalvar = new Button
            {
                Text = "Salvar certificado",
                Location = new Point(200, 175),
                Size = new Size(160, 35),
                Enabled = false
            };
            _btnSalvar.Click += BtnSalvar_Click;

            lblTitular.Location = new Point(25, 235);
            lblTitular.Size = new Size(580, 25);
            lblTitular.Text = "Titular: -";

            lblCnpj.Location = new Point(25, 270);
            lblCnpj.Size = new Size(580, 25);
            lblCnpj.Text = "CNPJ: -";

            lblValidade.Location = new Point(25, 305);
            lblValidade.Size = new Size(580, 25);
            lblValidade.Text = "Validade: -";

            Controls.AddRange(new Control[]
            {
                lblArquivo, txtArquivo, btnProcurar,
                lblSenha, txtSenha, btnValidar, _btnSalvar,
                lblTitular, lblCnpj, lblValidade
            });

            txtSenha.TextChanged += (s, e) =>
            {
                _certificadoValidado = false;
                _btnSalvar.Enabled = false;
            };
        }

        private void BtnProcurar_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Selecionar certificado digital A1",
                Filter = "Certificado digital (*.pfx)|*.pfx",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtArquivo.Text = dialog.FileName;
                txtSenha.Clear();
                LimparInformacoes();
            }
        }

        private void BtnValidar_Click(object? sender, EventArgs e)
        {
            _certificadoValidado = false;
            _btnSalvar!.Enabled = false;

            if (string.IsNullOrWhiteSpace(txtArquivo.Text) ||
                string.IsNullOrWhiteSpace(txtSenha.Text))
            {
                MessageBox.Show(
                    "Selecione o certificado e informe a senha.",
                    "Atenção",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var certificado = new X509Certificate2(
                    txtArquivo.Text,
                    txtSenha.Text,
                    X509KeyStorageFlags.EphemeralKeySet);

                if (!certificado.HasPrivateKey)
                    throw new Exception(
                        "O certificado não possui chave privada.");

                if (DateTime.Now < certificado.NotBefore ||
                    DateTime.Now > certificado.NotAfter)
                    throw new Exception(
                        "O certificado está fora do prazo de validade.");

                lblTitular.Text =
                    $"Titular: {certificado.GetNameInfo(X509NameType.SimpleName, false)}";

                // A identificação do CNPJ pelo padrão ICP-Brasil
                // será implementada posteriormente.
                lblCnpj.Text = "CNPJ: a identificar";

                lblValidade.Text =
                    $"Validade: {certificado.NotAfter:dd/MM/yyyy}";

                _certificadoValidado = true;
                _btnSalvar.Enabled = true;

                MessageBox.Show(
                    "Certificado validado com sucesso!",
                    "Fiscal",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception)
            {
                LimparInformacoes();

                MessageBox.Show(
                    "Não foi possível validar o certificado. " +
                    "Verifique o arquivo, a senha, a chave privada e a validade.",
                    "Erro na validação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnSalvar_Click(object? sender, EventArgs e)
        {
            if (!_certificadoValidado)
            {
                MessageBox.Show(
                    "Valide o certificado antes de salvar.",
                    "Atenção",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var confirmacao = MessageBox.Show(
                "Deseja salvar este certificado no computador?\n\n" +
                "Se já existir um certificado configurado, ele será substituído.\n" +
                "Será necessário reiniciar o Fiscal.AgentCertificado " +
                "para utilizar o certificado atualizado.",
                "Confirmar certificado",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmacao != DialogResult.Yes)
                return;

            try
            {
                var configuracaoService = new ConfiguracaoLocalService();

                configuracaoService.SalvarCertificado(
                    txtArquivo.Text,
                    txtSenha.Text);

                MessageBox.Show(
                    "Certificado salvo com sucesso!\n\n" +
                    "Reinicie o Fiscal.AgentCertificado para " +
                    "carregar o certificado atualizado.",
                    "Fiscal",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                txtSenha.Clear();
                _certificadoValidado = false;
                _btnSalvar!.Enabled = false;
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Não foi possível salvar o certificado. " +
                    "Verifique as permissões da pasta e " +
                    "se o arquivo PFX está acessível.",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LimparInformacoes()
        {
            lblTitular.Text = "Titular: -";
            lblCnpj.Text = "CNPJ: -";
            lblValidade.Text = "Validade: -";

            _certificadoValidado = false;

            if (_btnSalvar != null)
                _btnSalvar.Enabled = false;
        }
    }
}
