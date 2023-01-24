using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;

namespace Aspenlaub.Net.GitHub.CSharp.Duality;

public class DualityWorker : BackgroundWorker {
    private readonly TextBox _TextBox;
    private readonly DualityWork _DualityWork;
    private DualityFolder _LastProcessedFolder;
    private readonly string _WorkFileName;
    private string _ErrorMessage;
    private bool _AllDone;

    public DualityWorker(DualityWork work, string workFileName, TextBox textBox) {
        _TextBox = textBox;
        _DualityWork = work;
        _WorkFileName = workFileName;
        _ErrorMessage = "";
        _AllDone = false;
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

        for (var i = 0; i < _DualityWork.DualityFolders.Count; i++) {
            if (worker.CancellationPending) {
                e.Cancel = true;
                break;
            }
            _LastProcessedFolder = _DualityWork.DualityFolders[i];
            var needsProcessing = _LastProcessedFolder.NeedsProcessing();
            if (needsProcessing) {
                _ErrorMessage = _LastProcessedFolder.Process();
            }
            if (i + 1 == _DualityWork.DualityFolders.Count) {
                _AllDone = _ErrorMessage.Length == 0;
            }
            worker.ReportProgress((i + 1) * 100 / _DualityWork.DualityFolders.Count);
            if (_ErrorMessage.Length != 0) {
                break;
            }
            if (!needsProcessing) { continue; }
            File.Delete(_WorkFileName);
            _DualityWork.Save(_WorkFileName);
            Thread.Sleep(50);
        }
    }

    private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
        var worker = sender as BackgroundWorker;
        if (worker == null) {
            _TextBox.Text = "ERROR: background worker got lost";
            _TextBox.Foreground = Brushes.Red;
            return;
        }

        if (worker.CancellationPending) {
            _TextBox.Text = "Stopped";
        } else if (_ErrorMessage.Length != 0) {
            _TextBox.Text = "ERROR: " + _ErrorMessage;
            _TextBox.Foreground = Brushes.Red;
        } else if (_AllDone) {
            _TextBox.Text = "Everything is fine";
        } else {
            _TextBox.Text = e.ProgressPercentage + "% completed (Processed: " + _LastProcessedFolder.Folder + ")";
        }
    }

    public void ResetError() {
        _ErrorMessage = "";
        _TextBox.Foreground = Brushes.Black;
        _AllDone = false;
    }
}