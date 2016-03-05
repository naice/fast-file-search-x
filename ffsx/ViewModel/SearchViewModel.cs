using ffsx.Class;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ffsx.ViewModel
{
    public enum SearchViewModelSubSearch { FileOnly, Text, }

    public class SearchViewModel : ViewModelBase
    {
        private static string FileSizeToString(long size)
        {
            string rval = "";

            if (size / 1024 < 1)
                rval = string.Format("{0:F} Bytes", size);
            else if (size / 1024 / 1024 < 1)
                rval = string.Format("{0:F} KB", (double)size / 1024);
            else if (size / 1024 / 1024 / 1024 < 1)
                rval = string.Format("{0:F} MB", (double)size / 1024 / 1024);
            else
                rval = string.Format("{0:F} GB", (double)size / 1024 / 1024 / 1024);

            return rval;
        }

        private List<SearchResultViewModel> _Result = new List<SearchResultViewModel>();
        public IEnumerable<SearchResultViewModel> Result
        {
            get
            {
                lock (_Result)
                {
                    foreach (var item in _Result)
                    {
                        // TODO: Filter?

                        yield return item;
                    }
                }
            }
        }
        public int ResultCount
        {
            get
            {
                int cnt = 0;
                lock (_Result)
                    cnt = _Result.Count;
                return cnt;
            }
        }
        
        private string _Directory;
        public string Directory
        {
            get { return _Directory; }
            set
            {
                if (value != _Directory)
                {
                    _Directory = value;
                    RaisePropertyChanged("Directory");
                }
            }
        }
        private string _SearchToken;
        public string SearchToken
        {
            get { return _SearchToken; }
            set
            {
                if (value != _SearchToken)
                {
                    _SearchToken = value;
                    RaisePropertyChanged("SearchToken");
                }
            }
        }
        private DateTime _Started;
        public DateTime Started
        {
            get { return _Started; }
            set
            {
                if (value != _Started)
                {
                    _Started = value;
                    RaisePropertyChanged("Started");
                }
            }
        }
        private TimeSpan _Duration;
        public TimeSpan Duration
        {
            get { return _Duration; }
            set
            {
                if (value != _Duration)
                {
                    _Duration = value;
                    RaisePropertyChanged("Duration");
                }
            }
        }
        private List<string> _FileMasks;
        public List<string> FileMasks
        {
            get { return _FileMasks; }
            set
            {
                if (value != _FileMasks)
                {
                    _FileMasks = value;
                    RaisePropertyChanged("FileMasks");
                }
            }
        }
        private bool _Searching;
        public bool Searching
        {
            get { return _Searching; }
            set
            {
                if (value != _Searching)
                {
                    _Searching = value;
                    RaisePropertyChanged("Searching");
                }
            }
        }
        private SearchViewModelSubSearch _SubSearch;
        public SearchViewModelSubSearch SubSearch
        {
            get { return _SubSearch; }
            set
            {
                if (value != _SubSearch)
                {
                    _SubSearch = value;
                    RaisePropertyChanged("SubSearch");
                }
            }
        }

        FileSearch _fileSearch = null;
        public void Search()
        {
            if (Searching) return;
            Searching = true;
            Duration = TimeSpan.Zero;
            Started = DateTime.Now;

            _fileSearch = new FileSearch();
            _fileSearch.End += fs_End;
            _fileSearch.Begin += fs_Begin;
            _fileSearch.FileFound += fs_FileFound;
            _fileSearch.FMasks = FileMasks;
            _fileSearch.SearchPaths = new List<string>() { Directory };
            _fileSearch.Token = SearchToken;

            switch (SubSearch)
            {
                case SearchViewModelSubSearch.Text:
                    _fileSearch.SubSearch = SubSearch_Text;
                    break;
                case  SearchViewModelSubSearch.FileOnly:
                default:
                    break;
            }

            _fileSearch.Start();
        }
        public void Stop()
        {
            if (_fileSearch != null)
            {
                _fileSearch.Stop();
            }
        }
        private void OnSearchingEnd()
        {
            Searching = false;
        }
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(SearchToken))
                return SearchToken.Length > 20 ? SearchToken.Substring(0, 20) + "..." : SearchToken;

            return "New ...";
        }

        public bool SameSearch(SearchViewModel search)
        {
            if (FileMasks.Count != search.FileMasks.Count)
                return false;
            foreach (var item in search.FileMasks)
            {
                if (!FileMasks.Contains(item))
                    return false;
            }
            return Directory.Equals(search.Directory) &&
                   SearchToken == (search.SearchToken) && 
                   SubSearch == search.SubSearch;
        }

        private void OnResultChanged()
        {
            _Duration = DateTime.Now - _Started;
            RaisePropertyChanged("Result");
            RaisePropertyChanged("ResultCount");
            RaisePropertyChanged("Duration");
        }

        #region SEARCH EVENTS
        DateTime lastResultChange = DateTime.MinValue;
        void fs_Begin(FileSearch fs)
        {
            lastResultChange = DateTime.MinValue;
        }
        void fs_FileFound(FileSearch fs, FileFoundData ffd)
        {
            SearchResultViewModel result = new SearchResultViewModel() 
            {
                Column = ffd.Col,
                File = ffd.File,
                Line = ffd.Line,
                Position = ffd.Pos,
            };

            lock (_Result)
            {
                _Result.Add(result);
            }


            if ((DateTime.Now - lastResultChange).TotalMilliseconds > 500)
            {
                lastResultChange = DateTime.Now;
                Application.Current.Dispatcher.Invoke(() => OnResultChanged());
            }
        }
        void fs_End(FileSearch fs)
        {
            Application.Current.Dispatcher.Invoke(() => OnResultChanged());
            Application.Current.Dispatcher.Invoke(() => OnSearchingEnd());
        }
        #endregion

        #region SUB SEARCHES

        private bool SubSearch_Text(FileSearch fs, FileFoundData ffd)
        {
            bool rval = false;

            try
            {
                using (TextReader tr = new StreamReader(ffd.File.OpenRead(), true))
                {
                    string line;
                    ffd.Line = 1;
                    while ((line = tr.ReadLine()) != null)
                    {
                        if ((ffd.Col = line.IndexOf(fs.Token)) != -1)
                        {
                            ffd.Snippet = line;
                            ffd.Pos += ffd.Col;
                            rval = true;
                            break;
                        }

                        ffd.Line++;
                        ffd.Pos += line.Length;
                    }
                    tr.Close();
                }
            }
            catch { }

            return rval;
        }
        #endregion
    }
}
