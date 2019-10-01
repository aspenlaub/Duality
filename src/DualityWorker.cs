using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;

namespace Aspenlaub.Net.GitHub.CSharp.Duality {
    public class DualityWorker : BackgroundWorker {
        private readonly TextBox vTextBox;
        private readonly DualityWork vDualityWork;
        private DualityFolder vLastProcessedFolder;
        private readonly string vWorkFileName;
        private string vErrorMessage;
        private bool vAllDone;

        public DualityWorker(DualityWork work, string workFileName, TextBox textBox) {
            vTextBox = textBox;
            vDualityWork = work;
            vWorkFileName = workFileName;
            vErrorMessage = "";
            vAllDone = false;
            WorkerReportsProgress = true;
            WorkerSupportsCancellation = true;
            DoWork += BackgroundWorker_DoWork;
            ProgressChanged += BackgroundWorker_ProgressChanged;
        }

        public void OnClosing() {
            DoWork -= BackgroundWorker_DoWork;
            ProgressChanged -= BackgroundWorker_ProgressChanged;
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var worker = sender as BackgroundWorker;
            if (worker == null) { return; }

            for (var i = 0; i < vDualityWork.DualityFolders.Count; i++) {
                if (worker.CancellationPending) {
                    e.Cancel = true;
                    break;
                }
                vLastProcessedFolder = vDualityWork.DualityFolders[i];
                var needsProcessing = vLastProcessedFolder.NeedsProcessing();
                if (needsProcessing) {
                    vErrorMessage = vLastProcessedFolder.Process();
                }
                if (i + 1 == vDualityWork.DualityFolders.Count) {
                    vAllDone = vErrorMessage.Length == 0;
                }
                worker.ReportProgress((i + 1) * 100 / vDualityWork.DualityFolders.Count);
                if (vErrorMessage.Length != 0) {
                    break;
                }
                if (!needsProcessing) { continue; }
                File.Delete(vWorkFileName);
                vDualityWork.Save(vWorkFileName);
                Thread.Sleep(50);
            }
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            var worker = sender as BackgroundWorker;
            if (worker == null) {
                vTextBox.Text = "ERROR: background worker got lost";
                vTextBox.Foreground = Brushes.Red;
                return;
            }

            if (worker.CancellationPending) {
                vTextBox.Text = "Stopped";
            } else if (vErrorMessage.Length != 0) {
                vTextBox.Text = "ERROR: " + vErrorMessage;
                vTextBox.Foreground = Brushes.Red;
            } else if (vAllDone) {
                vTextBox.Text = "Everything is fine";
            } else {
                vTextBox.Text = e.ProgressPercentage + "% completed (Processed: " + vLastProcessedFolder.Folder + ")";
            }
        }

        public void ResetError() {
            vErrorMessage = "";
            vTextBox.Foreground = Brushes.Black;
            vAllDone = false;
        }
    }
}
