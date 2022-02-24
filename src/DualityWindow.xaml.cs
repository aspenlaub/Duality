using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.Duality {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public partial class DualityWindow {
        private DualityWorker DualityWorker;

        public DualityWindow() {
            InitializeComponent();
        }

        private async void DualityWindow_OnLoaded(object sender, RoutedEventArgs e) {
            var container = new ContainerBuilder().UsePegh(new DummyCsArgumentPrompter()).Build();
            if (Environment.MachineName.ToUpper() != "DELTAFLYER") {
                InfoText.Text = "Sorry, you should not run this program on this machine";
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
            var workFile = persistenceFolder.FullName + @"\DualityWork.xml";
            var work = File.Exists(workFile) ? new DualityWork(workFile, Environment.MachineName) : new DualityWork();
            work.UpdateFolders(secretDualityFolders);
            File.Delete(workFile);
            work.Save(workFile);
            CreateWorker(work, workFile);
        }

        private void CreateWorker(DualityWork work, string workFile) {
            DualityWorker = new DualityWorker(work, workFile, InfoText);
            RestartButton_OnClick(DualityWorker, null);
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e) {
            DualityWorker?.OnClosing();
            Close();
        }

        private void DualityWindow_OnClosing(object sender, CancelEventArgs e) {
            DualityWorker?.OnClosing();
        }

        private void StopButton_OnClick(object sender, RoutedEventArgs e) {
            if (DualityWorker.WorkerSupportsCancellation) {
                DualityWorker.CancelAsync();
            }
        }

        private void RestartButton_OnClick(object sender, RoutedEventArgs e) {
            if (DualityWorker.IsBusy) {
                return;
            }

            DualityWorker.ResetError();
            DualityWorker.RunWorkerAsync();
        }
    }
}
