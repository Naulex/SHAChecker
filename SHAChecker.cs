using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Linq;
using System.Drawing;

namespace SHACheckerN
{
    public partial class Form1 : Form
    {
        public static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        public static CancellationToken token = cancelTokenSource.Token;
        private SHA512 SHA512Hash = SHA512.Create();
        private MD5 MD5Hash = MD5.Create();
        private byte[] GetHashSha512(string filename)
        {
            using (FileStream stream = File.OpenRead(filename))
            {
                return SHA512Hash.ComputeHash(stream);
            }
        }

        public static string BytesToString(byte[] bytes)
        {
            string result = "";
            foreach (byte b in bytes) result += b.ToString("x2");
            return result;
        }

        public Form1()
        {
            InitializeComponent();
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            KeyPreview = true;
            openFoldertoHash.SelectedPath = Environment.CurrentDirectory;
            if (File.Exists("SHAChecksum"))
            {
                CheckHash();
            }
            else
            {
                CountHash();
            }
        }
        private async void CheckHash()
        {
            FileStream file = new FileStream("SHAChecksum", FileMode.Open);
            StreamReader readFile = new StreamReader(file);
            var Splitteddata = (Unzip(Convert.FromBase64String(readFile.ReadToEnd()))).Split(new[] { "\r\n" }, StringSplitOptions.None);
            readFile.Close();
            Show();
            int OKcount = 0, ERRcount = 0, AccessERRcount = 0;
            string[] Readeddata;
            SHAprogressBar.Minimum = 0;
            SHAprogressBar.Maximum = Splitteddata.Length - 1;
            foreach (var line in Splitteddata)
            {
                Readeddata = line.Split('*');
                if (File.Exists(Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1) + Readeddata[1]))
                {
                    await Task.Run(() =>
                    {
                        var data = Encoding.UTF8.GetBytes(Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1) + Readeddata[1]);
                        using (SHA512 shaM = new SHA512Managed())
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;

                            }
                            string SHA512 = BytesToString(GetHashSha512(Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1) + Readeddata[1]));
                            if (SHA512 == Readeddata[0])
                            {
                                InfoTextBox.Invoke(new Action(() => InfoTextBox.Text += "[" + Convert.ToInt32(Array.IndexOf(Splitteddata, line) + 1) + "/" + Convert.ToString(Convert.ToInt32(Splitteddata.Length)) + "]: " + " Контрольная сумма файла совпадает!\r\n" + GetRelativePath(Environment.CurrentDirectory, Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1) + Readeddata[1]) + " " + SHA512 + "\r\n\r\n"));
                                SHAprogressBar.Invoke(new Action(() => SHAprogressBar.PerformStep()));
                                OKcount++;
                            }

                            if (SHA512 != Readeddata[0])
                            {
                                BadTextBox.Invoke(new Action(() => BadTextBox.Text += "[" + Convert.ToInt32(Array.IndexOf(Splitteddata, line) + 1) + "/" + Convert.ToString(Convert.ToInt32(Splitteddata.Length)) + "]: " + " Контрольная сумма файла не совпадает!\r\n" + GetRelativePath(Environment.CurrentDirectory, Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1) + Readeddata[1]) + "\r\nРассчитанная сумма: " + SHA512 + "\r\nСохранённая сумма: " + Readeddata[0] + "\r\n\r\n"));
                                SHAprogressBar.Invoke(new Action(() => SHAprogressBar.PerformStep()));
                                ERRcount++;
                            }
                        }
                    });
                }
                else
                {
                    BadTextBox.Invoke(new Action(() => BadTextBox.Text += "[" + Convert.ToInt32(Array.IndexOf(Splitteddata, line) + 1) + "/" + Convert.ToString(Convert.ToInt32(Splitteddata.Length)) + "]: " + " Файл не обнаружен!\r\n" + GetRelativePath(Environment.CurrentDirectory, Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1) + Readeddata[1]) + "\r\n\r\n"));
                    SHAprogressBar.Invoke(new Action(() => SHAprogressBar.PerformStep()));
                    AccessERRcount++;
                }

            }
            отменаToolStripMenuItem.Enabled = false;
            пересчитатьToolStripMenuItem.Enabled = true;
            InfoTextBox.Invoke(new Action(() => InfoTextBox.Text += "====================\r\nОК: " + OKcount + "\r\nОшибок контрольной суммы: " + ERRcount + "\r\nОшибок доступа к файлу: " + AccessERRcount + "\r\n===================="));
            if (ERRcount != 0 || AccessERRcount != 0)
            {
                Console.Beep(300, 300);
                Console.Beep(300, 300);
                Console.Beep(300, 300);
            }
            else
            {
                Console.Beep(200, 100);
                Console.Beep(300, 100);
                Console.Beep(400, 100);
            }

        }

        private async void CountHash()
        {
            DialogResult dialogResult = MessageBox.Show("Добро пожаловать в SHACheckerN v.3.1.\n\nПри запуске программа ищет текстовый файл с хеш-суммой файлов и если находит такой, то сразу сравнивает полученные суммы с сохранёнными в файле значениями, а затем выводит результат.\r\n\r\nВычислить хеш для содержимого папки?", "Добро пожаловать | SHACheckerN", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (dialogResult == DialogResult.Yes)
            {
               
                if (openFoldertoHash.ShowDialog() == DialogResult.Cancel)
                    Environment.Exit(0);
                if ((openFoldertoHash.SelectedPath) != (Environment.CurrentDirectory))
                {
                    MessageBox.Show("Файл, для которого вычисляется хеш-сумма, должен находиться в одной папке с программой.", "Ошибка | SHACheckerN", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(0);
                }
                Show();

                List<string> files = (Directory.GetFiles(openFoldertoHash.SelectedPath, "*.*", SearchOption.AllDirectories)).OfType<string>().ToList(); ;
                files.Remove(Application.ExecutablePath);
                SHAprogressBar.Minimum = 0;
                SHAprogressBar.Maximum = files.Count;

                string dataToZIP = "";

                foreach (var file in files)
                {
                    await Task.Run(() =>
                {
                    var data = Encoding.UTF8.GetBytes(file);
                    using (SHA512 shaM = new SHA512Managed())
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                        string SHA512 = BytesToString(GetHashSha512(file));
                        SHAprogressBar.Invoke(new Action(() => SHAprogressBar.PerformStep()));
                        string Info = SHA512 + "*" + GetRelativePath(Environment.CurrentDirectory, file);
                        InfoTextBox.Invoke(new Action(() => InfoTextBox.Text += "[" + Convert.ToInt32(files.IndexOf(file) + 1) + "/" + Convert.ToString(Convert.ToInt32(files.Count)) + "]: " + GetRelativePath(Environment.CurrentDirectory, file) + " : " + SHA512 + "\r\n\r\n"));

                        if (files.IndexOf(file) + 1 == files.Count)
                        { dataToZIP += Info; }
                        else
                        {
                            dataToZIP += Info + "\r\n";
                        }

                    }
                });
                }

                using (StreamWriter sw = new StreamWriter("SHAChecksum", true))
                {
                    sw.Write(Convert.ToBase64String(Zip(dataToZIP)));
                }
                отменаToolStripMenuItem.Enabled = false;
                пересчитатьToolStripMenuItem.Enabled = true;
                BadTextBox.Invoke(new Action(() => BadTextBox.Text += "Операция завершена."));
                Console.Beep(400, 100);
                Console.Beep(300, 100);
                Console.Beep(200, 100);

            }
            else if (dialogResult == DialogResult.No)
            {
                Environment.Exit(0);
            }
        }
        public static byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }

                return mso.ToArray();
            }
        }

        public static string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        public string GetRelativePath(string relativeTo, string path)
        {
            var uri = new Uri(relativeTo);
            var rel = Uri.UnescapeDataString(uri.MakeRelativeUri(new Uri(path)).ToString()).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (rel.Contains(Path.DirectorySeparatorChar.ToString()) == false)
            {
                rel = $".{ Path.DirectorySeparatorChar }{ rel }";
            }
            return rel;
        }

        private void InfoTextBox_TextChanged_1(object sender, EventArgs e)
        {
            InfoTextBox.SelectionStart = InfoTextBox.Text.Length;
            InfoTextBox.ScrollToCaret();
            InfoTextBox.Refresh();
        }

        private void пересчитатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InfoTextBox.Clear();
            BadTextBox.Clear();
            пересчитатьToolStripMenuItem.Enabled = false;
            отменаToolStripMenuItem.Enabled = true;
            CheckHash();
        }

        private void отменаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cancelTokenSource.Cancel();
        }

        private void оПрограммеToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("SHACheckerN - программа, позволяющая создавать и проверять контрольные суммы файлов по стандарту SHA-512.\r\n\r\nИспользуется сжатие файлов контрольных сумм по стандарту Zip.\r\n\r\n\r\nSHACheckerN v.3.1\r\n(c)Naulex, 2023\r\n073797@gmail.com", "О программе | SHACheckerN", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void поверхToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (поверхToolStripMenuItem.Checked == true)
            { TopMost = true; }
            else
            { TopMost = false; }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F && e.Control)
            {
                if (TopMost == false)
                {
                    TopMost = true;
                    поверхToolStripMenuItem.Checked = true;
                    BadTextBox.Text += "Отображение поверх всех окон включено.\r\n";
                }
                else
                {
                    TopMost = false;
                    поверхToolStripMenuItem.Checked = false;
                    BadTextBox.Text += "Отображение поверх всех окон отключено.\r\n";
                }
            }
        }

        private void BadTextBox_TextChanged(object sender, EventArgs e)
        {
            BadTextBox.SelectionStart = BadTextBox.Text.Length;
            BadTextBox.ScrollToCaret();
            BadTextBox.Refresh();
        }
    }
}
