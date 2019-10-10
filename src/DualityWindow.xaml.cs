using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using IContainer = Autofac.IContainer;

namespace Aspenlaub.Net.GitHub.CSharp.Duality {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public partial class DualityWindow {
        private DualityWorker vDualityWorker;
        private readonly IContainer vContainer;

        public DualityWindow() {
            InitializeComponent();
            vContainer = new ContainerBuilder().RegisterForPegh(new DummyCsArgumentPrompter()).Build();
            if (Environment.MachineName.ToUpper() != "DELTAFLYER") {
                InfoText.Text = "Sorry, you should not run this program on this machine";
                return;
            }
            var secret = new DualityFoldersSecret();
            var errorsAndInfos = new ErrorsAndInfos();
            var secretDualityFolders = vContainer.Resolve<ISecretRepository>().GetAsync(secret, errorsAndInfos).Result;
            if (errorsAndInfos.AnyErrors()) {
                throw new Exception(errorsAndInfos.ErrorsToString());
            }
            var persistenceFolder = vContainer.Resolve<IFolderResolver>().Resolve(@"$(GitHub)\DualityBin\Release\Persistence", errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) {
                throw new Exception(errorsAndInfos.ErrorsToString());
            }
            var workFile = persistenceFolder + @"\DualityWork.xml";
            var work = File.Exists(workFile) ? new DualityWork(workFile, Environment.MachineName) : new DualityWork();
            work.UpdateFolders(secretDualityFolders);
            File.Delete(workFile);
            work.Save(workFile);
            CreateWorker(work, workFile);
        }

        private void CreateWorker(DualityWork work, string workFile) {
            vDualityWorker = new DualityWorker(work, workFile, InfoText);
            RestartButton_OnClick(vDualityWorker, null);
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e) {
            vDualityWorker?.OnClosing();
            Close();
        }

        private void DualityWindow_OnClosing(object sender, CancelEventArgs e) {
            vDualityWorker?.OnClosing();
        }

        private void StopButton_OnClick(object sender, RoutedEventArgs e) {
            if (vDualityWorker.WorkerSupportsCancellation) {
                vDualityWorker.CancelAsync();
            }
        }

        private void RestartButton_OnClick(object sender, RoutedEventArgs e) {
            if (vDualityWorker.IsBusy) {
                return;
            }

            vDualityWorker.ResetError();
            vDualityWorker.RunWorkerAsync();
        }
    }
}
