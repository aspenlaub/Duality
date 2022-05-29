using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;

namespace Aspenlaub.Net.GitHub.CSharp.Duality;

public class DualityWorker : BackgroundWorker {
    private readonly TextBox TextBox;
    private readonly DualityWork DualityWork;
    private DualityFolder LastProcessedFolder;
    private readonly string WorkFileName;
    private string ErrorMessage;
    private bool AllDone;

    public DualityWorker(DualityWork work, string workFileName, TextBox textBox) {
        TextBox = textBox;
        DualityWork = work;
        WorkFileName = workFileName;
        ErrorMessage = "";
        AllDone = false;
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

        for (var i = 0; i < DualityWork.DualityFolders.Count; i++) {
            if (worker.CancellationPending) {
                e.Cancel = true;
                break;
            }
            LastProcessedFolder = DualityWork.DualityFolders[i];
            var needsProcessing = LastProcessedFolder.NeedsProcessing();
            if (needsProcessing) {
                ErrorMessage = LastProcessedFolder.Process();
            }
            if (i + 1 == DualityWork.DualityFolders.Count) {
                AllDone = ErrorMessage.Length == 0;
            }
            worker.ReportProgress((i + 1) * 100 / DualityWork.DualityFolders.Count);
            if (ErrorMessage.Length != 0) {
                break;
            }
            if (!needsProcessing) { continue; }
            File.Delete(WorkFileName);
            DualityWork.Save(WorkFileName);
            Thread.Sleep(50);
        }
    }

    private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
        var worker = sender as BackgroundWorker;
        if (worker == null) {
            TextBox.Text = "ERROR: background worker got lost";
            TextBox.Foreground = Brushes.Red;
            return;
        }

        if (worker.CancellationPending) {
            TextBox.Text = "Stopped";
        } else if (ErrorMessage.Length != 0) {
            TextBox.Text = "ERROR: " + ErrorMessage;
            TextBox.Foreground = Brushes.Red;
        } else if (AllDone) {
            TextBox.Text = "Everything is fine";
        } else {
            TextBox.Text = e.ProgressPercentage + "% completed (Processed: " + LastProcessedFolder.Folder + ")";
        }
    }

    public void ResetError() {
        ErrorMessage = "";
        TextBox.Foreground = Brushes.Black;
        AllDone = false;
    }
}