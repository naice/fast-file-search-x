using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ffsx.Class
{
    public class FileFoundData
    {
        public System.Text.RegularExpressions.Match TextMatch { get; set; }

        public FileInfo File { get; set; }
        public int Line { get; set; }
        public int Pos { get; set; }
        public int Col { get; set; }
        public string Snippet { get; set; }

        public FileFoundData(string file) : this(new FileInfo(file))
        {
        }
        public FileFoundData(FileInfo file)
        {
            Line = -1;
            Pos = -1;
            Col = - 1;
            Snippet = null;

            File = file;
        }
    }
    public delegate void FileSearchFFDataHandler(FileSearch fs, FileFoundData ffd);
    public delegate bool FileSearchSubSearchHandler(FileSearch fs, FileFoundData ffd);
    public delegate void FileSearchHandler(FileSearch fs);
    public class FileSearch : IDisposable
    {
        private BackgroundWorker bwSearch;

        public bool Busy { get { return bwSearch.IsBusy; } }
        public bool CancellationPending { get { return bwSearch.CancellationPending; } }
        public List<string> SearchPaths { get; set; }
        public List<string> FMasks { get; set; }
        public string Token { get; set; }
        public event FileSearchFFDataHandler FileFound;
        public event FileSearchHandler Begin;
        public event FileSearchHandler End;
        public FileSearchSubSearchHandler SubSearch { get; set; }

        public FileSearch()
        {
            SubSearch = null;
            Begin = null;
            End = null;
            SearchPaths = new List<string>();
            FMasks = new List<string>();
            bwSearch = new BackgroundWorker();
            bwSearch.WorkerSupportsCancellation = true;
            bwSearch.DoWork += new DoWorkEventHandler(bwSearch_DoWork);
        }

        public void Start()
        {
            bwSearch.RunWorkerAsync();
        }
        public void Stop()
        {
            bwSearch.CancelAsync();
        }

        private void Search(string path)
        {
            List<FileInfo> files = new List<FileInfo>();
            DirectoryInfo[] nextDirs = new DirectoryInfo[0];
            DirectoryInfo currentDir = new DirectoryInfo(path);

            // für alle masken dateien sammeln..
            foreach (string fmask in FMasks)
            {
                try
                {
                    files.AddRange(currentDir.GetFiles(fmask, SearchOption.TopDirectoryOnly));
                }
                catch { }
            }

            // dateien verarbeiten..
            foreach (FileInfo file in files)
            {
                if (bwSearch.CancellationPending) break;

                bool filefound = true;
                FileFoundData ffd = new FileFoundData(file);

                if (SubSearch != null)
                {
                    filefound = OnSubSearch(ffd);
                }

                if (bwSearch.CancellationPending) break;

                if (filefound)
                {
                    OnFileFound(ffd);
                }
            }

            // die nächsten ordner auflisten und verarbeiten.
            try
            {
                nextDirs = currentDir.GetDirectories();
            }
            catch { }

            foreach (DirectoryInfo difo in nextDirs)
            {
                if (bwSearch.CancellationPending) break;

                Search(difo.FullName);
            }
        }
        private void bwSearch_DoWork(object sender, DoWorkEventArgs e)
        {
            OnSearchBegin();

            foreach (string spath in SearchPaths)
            {
                if (bwSearch.CancellationPending) break;

                Search(spath);
            }

            OnSearchEnd();
        }

        protected virtual bool OnSubSearch(FileFoundData dat)
        {
            bool ok = false;

            if (SubSearch != null)
            {
                ok = SubSearch(this, dat);
            }

            return ok;
        }
        protected virtual void OnSearchBegin()
        {
            if (Begin != null)
            {
                    Begin(this);
            }
        }
        protected virtual void OnSearchEnd()
        {
            if (End != null)
            {
                    End(this);
            }
        }
        protected virtual void OnFileFound(FileFoundData dat)
        {
            if (bwSearch.CancellationPending) return;

            if (FileFound != null)
            {
                    FileFound(this, dat);
            }
        }

        #region IDisposable Member

        public void Dispose()
        {
            if (bwSearch.IsBusy)
                bwSearch.CancelAsync();
            bwSearch.Dispose();
            bwSearch = null;
        }

        #endregion
    }
}
