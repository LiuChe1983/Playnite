using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Playnite.SDK;
using System.IO;

namespace Playnite
{
    public class GameSaveWatcher
    {
        private static ILogger logger = LogManager.GetLogger();

        public Game game;
        public string realPath;
        public bool IsChanged { get; set; } = false;
        public string lastError = "";
        private FileSystemWatcher watcher;

        public GameSaveWatcher(Game _game)
        {
            game = _game;
            realPath = SaveManager.GetRealPath(game.SavePath);
            watcher = new FileSystemWatcher(realPath);

            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 // | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            watcher.Changed += OnChanged;
            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;
            //watcher.Filter = "*.txt";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }
            IsChanged = true;
        }

        private void OnCreated(object sender, FileSystemEventArgs e) => IsChanged = true;

        private void OnDeleted(object sender, FileSystemEventArgs e) => IsChanged = true;

        private void OnRenamed(object sender, RenamedEventArgs e) => IsChanged = true;

        private void OnError(object sender, ErrorEventArgs e) =>
            PrintException(e.GetException());

        private void PrintException(Exception ex)
        {
            if (ex != null)
            {
                if(ex.Message != lastError) logger.Error("文件监控错误:" + ex.Message);
            }
        }

        public void Clear()
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            DirectoryInfo di = new DirectoryInfo(realPath); //删除存档文件夹
            if (di.Exists)
            {
                di.Delete(true);
            }
        }

    }
}
