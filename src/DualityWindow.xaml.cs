using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.Duality;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
// ReSharper disable once UnusedMember.Global
public partial class DualityWindow {
    private DualityWorker _DualityWorker;

    public DualityWindow() {
        InitializeComponent();
    }

    private async void DualityWindow_OnLoaded(object sender, RoutedEventArgs e) {
        await UpdateWorkAndRun();
    }

    private async Task UpdateWork() {
        var container = new ContainerBuilder().UsePegh("Duality", new DummyCsArgumentPrompter()).Build();
        if (Environment.MachineName.ToUpper() != "DELTAFLYER") {
            StartupInfoText.Text = "Sorry, you should not run this program on this machine";
            return;
        }

        var secret = new DualityFoldersSecret();
        var errorsAndInfos = new ErrorsAndInfos();
        var secretDualityFolders = await container.Resolve<ISecretRepository>().GetAsync(secret, errorsAndInfos);
        if (errorsAndInfos.AnyErrors()) {
            throw new Exception(errorsAndInfos.ErrorsToString());
        }

        var persistenceFolder = await container.Resolve<IFolderResolver>().ResolveAsync(@"$(GitHub)\DualityBin\Release\Persistence", errorsAndInfos);
        if (errorsAndInfos.AnyErrors()) {
            throw new Exception(errorsAndInfos.ErrorsToString());
        }

        persistenceFolder.CreateIfNecessary();

        var folderErrorsAndInfos = new ErrorsAndInfos();
        foreach (var secretDualityFolder in secretDualityFolders.Where(IsInaccessible)) {
            folderErrorsAndInfos.Errors.Add($"Folder/-s is/are inaccessible: {secretDualityFolder}");
        }
        StartupInfoText.Text = folderErrorsAndInfos.ErrorsToString();

        var workFileName = folderErrorsAndInfos.AnyErrors() ? $"DualityWorkPartial{folderErrorsAndInfos.Errors.Count}.xml" : "DualityWork.xml";
        var workFile = persistenceFolder.FullName + @"\" + workFileName;
        var work = File.Exists(workFile) ? new DualityWork(workFile, Environment.MachineName) : new DualityWork();
        work.UpdateFolders(secretDualityFolders.Where(x => !IsInaccessible(x)).ToList());
        File.Delete(workFile);
        work.Save(workFile);
        _DualityWorker = new DualityWorker(work, workFile, InfoText);
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) {
        _DualityWorker?.OnClosing();
        Close();
    }

    private void DualityWindow_OnClosing(object sender, CancelEventArgs e) {
        _DualityWorker?.OnClosing();
    }

    private void StopButton_OnClick(object sender, RoutedEventArgs e) {
        if (_DualityWorker?.IsBusy != true) {
            return;
        }

        if (_DualityWorker.WorkerSupportsCancellation) {
            _DualityWorker.CancelAsync();
        }
    }

    private async void RestartButton_OnClick(object sender, RoutedEventArgs e) {
        await UpdateWorkAndRun();
    }

    private async Task UpdateWorkAndRun() {
        if (_DualityWorker?.IsBusy == true) {
            return;
        }

        await UpdateWork();
        _DualityWorker.ResetError();
        _DualityWorker.RunWorkerAsync();
    }

    private bool IsInaccessible(DualityFolder folder) {
        try {
            return !Directory.Exists(folder.Folder) || !Directory.Exists(folder.OtherFolder);
        } catch {
            return true;
        }
    }
}