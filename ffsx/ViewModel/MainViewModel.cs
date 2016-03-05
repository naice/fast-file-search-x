using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace ffsx.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public static MainViewModel Instance { get; private set; }

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
        private string _SearchPath;
        public string SearchPath
        {
            get { return _SearchPath; }
            set
            {
                if (value != _SearchPath)
                {
                    _SearchPath = value;
                    RaisePropertyChanged("SearchPath");
                }
            }
        }
        private ObservableCollection<string> _FileMasks;
        public ObservableCollection<string> FileMasks
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
        private string _SelectedFileMask = "*.*";
        public string SelectedFileMask
        {
            get { return _SelectedFileMask; }
            set
            {
                if (value != _SelectedFileMask)
                {
                    _SelectedFileMask = value;
                    RaisePropertyChanged("SelectedFileMask");
                }
            }
        }
        private ObservableCollection<SearchViewModel> _Searches = new ObservableCollection<SearchViewModel>();
        public ObservableCollection<SearchViewModel> Searches
        {
            get { return _Searches; }
            set
            {
                if (value != _Searches)
                {
                    _Searches = value;
                    RaisePropertyChanged("Searches");
                }
            }
        }
        private SearchViewModel _SelectedSearch;
        public SearchViewModel SelectedSearch
        {
            get { return _SelectedSearch; }
            set
            {
                if (value != _SelectedSearch)
                {
                    _SelectedSearch = value;
                    RaisePropertyChanged("SelectedSearch");
                }
            }
        }
        private Dictionary<string, string> _Favorites = new Dictionary<string, string>();
        public Dictionary<string, string> Favorites
        {
            get { return _Favorites; }
            set
            {
                if (value != _Favorites)
                {
                    _Favorites = value;
                    RaisePropertyChanged("Favorites");
                }
            }
        }

        public MainViewModel()
        {
            Instance = this;

            if (App.Settings != null)
            {
                FileMasks = new ObservableCollection<string>(App.Settings.FileMasks);
                SearchPath = App.Settings.SearchPath;
                SearchToken = App.Settings.SearchToken;
                SelectedFileMask = App.Settings.SelectedFileMask;
                Favorites = App.Settings.Favorites;
            }
        }

        public void Search()
        {
            if (string.IsNullOrEmpty(SearchPath)) return;

            // Create Search
            SearchViewModel search = new SearchViewModel() { 
                Directory = SearchPath,
                SearchToken = SearchToken,
                FileMasks = new List<string>(BindingConverter.FileMaskConverter.ToMask(SelectedFileMask)),
                SubSearch = SearchViewModelSubSearch.Text,
            };

            // after creation check if we got a similar search
            var sameSearch = Searches.FirstOrDefault((A) => A.SameSearch(search));
            if (sameSearch != null)
            {
                // Select same search
                SelectedSearch = sameSearch;
            }
            else
            {
                // Save last search
                App.Settings.SearchPath = SearchPath;
                App.Settings.SearchToken = SearchToken;
                // To avoid any formatting based issues we take the recently converted 
                // mask of the search to convert it back and then add and update if needed.
                string clearedMask = BindingConverter.FileMaskConverter.ToString(new ObservableCollection<string>(search.FileMasks));
                App.Settings.SelectedFileMask = clearedMask;
                SelectedFileMask = clearedMask;
                // for the vmodel..
                if (!FileMasks.Contains(clearedMask))
                    FileMasks.Add(clearedMask);
                // for the settings..
                if (!App.Settings.FileMasks.Contains(clearedMask))
                    App.Settings.FileMasks.Add(clearedMask);


                App.Settings.Save();

                // Append / Select / Start 
                Searches.Add(search);
                SelectedSearch = search;
                search.Search();
            }
        }

        public void Remove(SearchViewModel search)
        {
            Searches.Remove(search);
            search.Stop();
        }
    }
}
