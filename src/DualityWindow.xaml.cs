using System;
using System.Collections.Generic;
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

        var secret = new DualityFoldersSecret();
        var errorsAndInfos = new ErrorsAndInfos();
        var secretDualityFolders = await container.Resolve<ISecretRepository>().GetAsync(secret, errorsAndInfos);
        if (errorsAndInfos.AnyErrors()) {
            throw new Exception(errorsAndInfos.ErrorsToString());
        }

        secretDualityFolders = secretDualityFolders.ForThisMachine();
        if (!secretDualityFolders.Any()) {
            StartupInfoText.Text = "No duality folders have been configured for this machine";
            return;
        }

        var persistenceFolder = await container.Resolve<IFolderResolver>().ResolveAsync(@"$(GitHub)\DualityBin\Release\Persistence", errorsAndInfos);
        if (errorsAndInfos.AnyErrors()) {
            throw new Exception(errorsAndInfos.ErrorsToString());
        }

        persistenceFolder.CreateIfNecessary();

        var folderErrorsAndInfos = new ErrorsAndInfos();
        var inaccessibleFolders = InaccessibleFolders(secretDualityFolders, out var numberOfSimilarFolders);
        foreach (var inaccessibleFolder in inaccessibleFolders) {
            folderErrorsAndInfos.Errors.Add($"Folder/-s is/are inaccessible: {inaccessibleFolder}");
        }
        if (numberOfSimilarFolders > 0) {
            folderErrorsAndInfos.Errors.Add($"{numberOfSimilarFolders} folders with similar location/-s are also inaccessible");
        }
        StartupInfoText.Text = folderErrorsAndInfos.ErrorsToString();

        var workFileName = folderErrorsAndInfos.AnyErrors() ? $"DualityWorkPartial{folderErrorsAndInfos.Errors.Count}.xml" : "DualityWork.xml";
        var workFile = persistenceFolder.FullName + @"\" + workFileName;
        var work = File.Exists(workFile) ? new DualityWork(workFile, Environment.MachineName) : new DualityWork();
        work.UpdateFolders(secretDualityFolders.Where(x => !FolderIsInaccessible(x) && !OtherFolderIsInaccessible(x)).ToList());
        File.Delete(workFile);
        work.Save(workFile);
        _DualityWorker = new DualityWorker(work, workFile, InfoText);
    }

    private List<string> InaccessibleFolders(DualityFolders secretDualityFolders, out int numberOfSimilarFolders) {
        numberOfSimilarFolders = 0;
        const int numberOfSuffixCharacters = 24;
        var inaccessibleFolders = secretDualityFolders.Where(FolderIsInaccessible).Select(secretDualityFolder => secretDualityFolder.Folder).ToList();
        inaccessibleFolders.AddRange(secretDualityFolders.Where(OtherFolderIsInaccessible).Select(secretDualityFolder => secretDualityFolder.OtherFolder));
        inaccessibleFolders = inaccessibleFolders.Distinct().ToList();
        for (var i = 1; i < inaccessibleFolders.Count; i++) {
            if (inaccessibleFolders[i].Length < numberOfSuffixCharacters) {
                continue;
            }
            var pos = inaccessibleFolders[i].IndexOf("\\", numberOfSuffixCharacters, StringComparison.InvariantCulture);
            if (pos < 0) {
                continue;
            }
            for (var j = 0; j < i; j++) {
                if (inaccessibleFolders[j].Length < pos
                        ||  inaccessibleFolders[i][..numberOfSuffixCharacters] != inaccessibleFolders[j][..numberOfSuffixCharacters]) {
                    continue;
                }

                inaccessibleFolders[i] = "";
                numberOfSimilarFolders ++;
                break;
            }
        }
        return inaccessibleFolders.Where(x => !string.IsNullOrEmpty(x)).ToList();
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
            StartupInfoText.Text = $"Duality worker is not busy ({DateTime.Now.ToLongTimeString()})..";
            return;
        }

        if (_DualityWorker.WorkerSupportsCancellation) {
            _DualityWorker.CancelAsync();
        }
    }

    private async void RestartButton_OnClick(object sender, RoutedEventArgs e) {
        InfoText.Text = "Restarting, stand by..";
        StartupInfoText.Text = "Restarting, stand by..";
        await UpdateWorkAndRun();
    }

    private async Task UpdateWorkAndRun() {
        if (_DualityWorker?.IsBusy == true) {
            StartupInfoText.Text = $"Duality worker is busy, please press Stop and wait ({DateTime.Now.ToLongTimeString()})..";
            return;
        }

        await UpdateWork();
        _DualityWorker?.ResetError();
        _DualityWorker?.RunWorkerAsync();
    }

    private bool FolderIsInaccessible(DualityFolder folder) {
        try {
            return !Directory.Exists(folder.Folder);
        } catch {
            return true;
        }
    }

    private bool OtherFolderIsInaccessible(DualityFolder folder) {
        try {
            return !Directory.Exists(folder.OtherFolder);
        } catch {
            return true;
        }
    }
}