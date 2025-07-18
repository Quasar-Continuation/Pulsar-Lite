﻿using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Helper;
using Pulsar.Server.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmCertificate : Form
    {
        private X509Certificate2 _certificate;

        public FrmCertificate()
        {
            InitializeComponent();
            DarkModeManager.ApplyDarkMode(this);
			ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);
        }

        private void SetCertificate(X509Certificate2 certificate)
        {
            _certificate = certificate;
            txtDetails.Text = _certificate.ToString(false);
            btnSave.Enabled = true;
        }

        private string GenerateRandomStringPair()
        {
            const string letters = "abcdefghijklmnopqrstuvwxyz";
            Random random = new Random();
            string GenerateRandomString(int length) => new string(Enumerable.Repeat(letters, length).Select(s => s[random.Next(s.Length)]).ToArray());

            string randomString1 = GenerateRandomString(random.Next(4, 7));
            string randomString2 = GenerateRandomString(random.Next(4, 7));

            return $"{randomString1} {randomString2}";
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            SetCertificate(CertificateHelper.CreateCertificateAuthority(GenerateRandomStringPair(), 4096));
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.CheckFileExists = true;
                ofd.Filter = "*.p12|*.p12";
                ofd.Multiselect = false;
                ofd.InitialDirectory = Application.StartupPath;
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        SetCertificate(new X509Certificate2(ofd.FileName, "", X509KeyStorageFlags.Exportable));

                        //btnSave.Select();

                        btnSave.PerformClick();

                        string importedDir = Path.GetDirectoryName(ofd.FileName);
                        string sourcePulsarStuff = Path.Combine(importedDir, "PulsarStuff");
                        string destPulsarStuff = Path.Combine(Application.StartupPath, "PulsarStuff");
                        if (Directory.Exists(sourcePulsarStuff))
                        {
                            Directory.CreateDirectory(destPulsarStuff);

                            foreach (string file in Directory.GetFiles(sourcePulsarStuff, "*", SearchOption.AllDirectories))
                            {
                                string relativePath = file.Substring(sourcePulsarStuff.Length + 1);
                                string destFile = Path.Combine(destPulsarStuff, relativePath);
                                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                                File.Copy(file, destFile, true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Error importing the certificate:\n{ex.Message}", "Save error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (_certificate == null)
                    throw new ArgumentNullException();

                if (!_certificate.HasPrivateKey)
                    throw new ArgumentException();

                File.WriteAllBytes(Settings.CertificatePath, _certificate.Export(X509ContentType.Pkcs12));

                MessageBox.Show(this,
                    "Please backup the certificate now. Loss of the certificate results in loosing all clients!",
                    "Certificate backup", MessageBoxButtons.OK, MessageBoxIcon.Information);

                string argument = "/select, \"" + Settings.CertificatePath + "\"";
                Process.Start("explorer.exe", argument);

                this.DialogResult = DialogResult.OK;
            }
            catch (ArgumentNullException)
            {
                MessageBox.Show(this, "Please create or import a certificate first.", "Save error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (ArgumentException)
            {
                MessageBox.Show(this,
                    "The imported certificate has no associated private key. Please import a different certificate.",
                    "Save error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception)
            {
                MessageBox.Show(this,
                    "There was an error saving the certificate, please make sure you have write access to the Pulsar directory.",
                    "Save error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
