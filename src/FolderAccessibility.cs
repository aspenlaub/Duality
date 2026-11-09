using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aspenlaub.Net.GitHub.CSharp.Duality;

public class FolderAccessibility {
    private static readonly List<string> _accessibleFolders = [], _inaccessibleFolders = [];

    private const int _waitTimeInMilliseconds = 1000;

    public static bool FolderIsInaccessible(DualityFolder folder) {
        if (_accessibleFolders.Contains(folder.Folder)) {
            return false;
        }
        if (_inaccessibleFolders.Contains(folder.Folder)) {
            return true;
        }
        if (_inaccessibleFolders.Any(x => folder.Folder.StartsWith(x))) {
            return true;
        }

        Debug.WriteLine($"Checking accessibility of {folder.Folder}");
        try {
            using var cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            var existsTask = Task.Run(() => Directory.Exists(folder.Folder), token);
            if (Task.WaitAny([existsTask], TimeSpan.FromMilliseconds(_waitTimeInMilliseconds)) != 0) {
                cts.Cancel();
                return true;
            }

            bool exists = existsTask.Result;
            if (exists) {
                _accessibleFolders.Add(folder.Folder);
            } else {
                _inaccessibleFolders.Add(folder.Folder);
            }
            return !exists;

        } catch {
            return true;
        }
    }

    public static bool OtherFolderIsInaccessible(DualityFolder folder) {
        if (_accessibleFolders.Contains(folder.OtherFolder)) {
            return false;
        }
        if (_inaccessibleFolders.Contains(folder.OtherFolder)) {
            return true;
        }
        if (_inaccessibleFolders.Any(x => folder.OtherFolder.StartsWith(x))) {
            return true;
        }

        Debug.WriteLine($"Checking accessibility of {folder.OtherFolder}");
        try {
            using var cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            var existsTask = Task.Run(() => Directory.Exists(folder.OtherFolder), token);
            if (Task.WaitAny([existsTask], TimeSpan.FromMilliseconds(_waitTimeInMilliseconds)) != 0) {
                cts.Cancel();
                return true;
            }

            bool exists = existsTask.Result;
            if (exists) {
                _accessibleFolders.Add(folder.OtherFolder);
            } else {
                _inaccessibleFolders.Add(folder.OtherFolder);
            }
            return !exists;

        } catch {
            return true;
        }
    }
}
