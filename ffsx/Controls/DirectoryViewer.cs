using ffsx.Class;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections.Specialized;
using LinqToVisualTree;
using System.Windows.Interop;
using System.Windows.Controls.Primitives;

namespace ffsx.Controls
{
    public class DirectoryViewerTag : ViewModelBase
    {
        public object Tag { get; set; }
        public DirectoryInfo Info { get; set; }
        private bool _Populating;
        public bool Populating
        {
            get { return _Populating; }
            set
            {
                if (value != _Populating)
                {
                    _Populating = value;
                    RaisePropertyChanged("Populating");
                }
            }
        }
        private string _WpfIconKey;
        public string WpfIconKey
        {
            get { return _WpfIconKey; }
            set
            {
                if (value != _WpfIconKey)
                {
                    _WpfIconKey = value;
                    RaisePropertyChanged("WpfIconKey");
                }
            }
        }
        public bool IsFavorite { get; set; }
        

        private Environment.SpecialFolder? _SpecialFolder = null;
        private DriveInfo _DriveInfo = null;
        private string _Name = null;

        public DirectoryViewerTag(DriveInfo drive)
        {
            SetDirInfos(null, null, drive);
        }
        public DirectoryViewerTag(Environment.SpecialFolder SpecialFolder)
        {
            SetDirInfos(null, SpecialFolder, null);
        }
        public DirectoryViewerTag(string dir, bool isFavorite, string name)
        {
            IsFavorite = isFavorite;
            SetDirInfos(dir, null, null);
            _Name = name;
        }
        private void SetDirInfos(string dir, Environment.SpecialFolder? specialFolder, DriveInfo driveInfo)
        {
            string d = dir;
            _Name = null;
            _SpecialFolder = specialFolder;
            _DriveInfo = driveInfo;


            if (!string.IsNullOrEmpty(dir))
            {
                WpfIconKey = WpfFileIconCache.GetDirectoryIconKey(dir, IsFavorite);
            }
            if (specialFolder.HasValue)
            {
                WpfIconKey = WpfFileIconCache.GetSpecialFolderIconKey(specialFolder.Value);
                d = Environment.GetFolderPath(specialFolder.Value);
            }
            if (driveInfo != null)
            {
                d = null;
                WpfIconKey = WpfFileIconCache.GetDriveIconKey(driveInfo);
                Info = driveInfo.RootDirectory;
            }

            if (!string.IsNullOrEmpty(d))
                Info = new DirectoryInfo(d);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(_Name))
            {
                string nam = Info.Name;
                if (_SpecialFolder.HasValue)
                {
                    string spnam = Win32.GetDisplayName(Info.FullName);
                    if (!string.IsNullOrEmpty(spnam))
                        nam = spnam;
                }
                else if (_DriveInfo != null)
                {
                    if (false == _DriveInfo.IsReady)
                    {
                        nam = Info.Name;
                    }
                    else
                    {
                        nam = string.Format("{0} ({1})", _DriveInfo.VolumeLabel, Info.Name);
                    }
                }

                _Name = nam;
            }

            return _Name;
        }
    }
    public class DirectorySelectedEventArgs : EventArgs
    {
        public DirectoryViewerTag Tag { get; set; }
    }

    public class DirectoryViewer : TreeView
    {
        public const double PROXIMITYRANGE = 5;
        public event EventHandler<DirectorySelectedEventArgs> DirectorySelected = null;
        public event EventHandler<DirectorySelectedEventArgs> DirectoryRightClick = null;

        private bool _FavoritesContainerShown = false;
        private Grid _FavoritesContainer = null;
        private Button _FavoritesAddButton = null;
        private Button _FavoritesRemoveButton = null;
        private TextBlock _FavoritesHint = null;
        private Popup _FavoritesPopup = null;
        private Rect _FavoritesContainerRect ;
        private Rect _FavoritesContainerProximityRect ;

        public Dictionary<string, string> Favorites
        {
            get { return (Dictionary<string, string>)GetValue(FavoritesProperty); }
            set { SetValue(FavoritesProperty, value); }
        }
        public static DependencyProperty FavoritesProperty = DependencyProperty.Register("Favorites", typeof(Dictionary<string, string>), typeof(DirectoryViewer),
            new PropertyMetadata(new Dictionary<string, string>(), new PropertyChangedCallback((A, B) => ((DirectoryViewer)A).OnFavoritesChanged())));

        public DirectoryViewer()
        {
            Loaded += (s,a) => {
                _FavoritesContainer = this.GetChildren<Grid>().FirstOrDefault((A) => A.Name == "FavoritesPanel");
                _FavoritesPopup = this.GetChildren<Popup>().FirstOrDefault((A) => A.Name == "FavoriteEditPopup"); 
                if (_FavoritesContainer != null)
                {
                    _FavoritesAddButton = _FavoritesContainer.GetChildren<Button>().FirstOrDefault((A) => A.Name == "BtnAddFavorite");
                    _FavoritesAddButton.Click += FavoritesAddButton_Click;
                    _FavoritesRemoveButton = _FavoritesContainer.GetChildren<Button>().FirstOrDefault((A) => A.Name == "BtnRemoveFavorite"); 
                    _FavoritesRemoveButton.Click += FavoritesRemoveButton_Click;
                    _FavoritesHint = _FavoritesContainer.GetChildren<TextBlock>().FirstOrDefault((A) => A.Name == "FavoriteHint"); 

                    Animations.ElementYFadeOut(_FavoritesContainer);
                    MouseHook.BeginHook();
                    MouseHook.Watching = true;
                    MouseHook.MouseActing += MouseHook_MouseActing;
                }
                ApplyStandartFolders(); 
            };
            PreviewKeyUp+=(s,a)=> {
                if (a.Key == System.Windows.Input.Key.F2)
                {
                    if (_FavoritesPopup != null)
                    {
                        _FavoritesPopup.IsOpen = true;
                    }
                }
            };

            SelectedItemChanged += DirectoryViewer_SelectedItemChanged;
        }

        private void FavoritesRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveFavorite();
        }

        private void FavoritesAddButton_Click(object sender, RoutedEventArgs e)
        {
            AddFavorite();
        }

        void MouseHook_MouseActing(MouseAktion aktion)
        {
            try
            {
                OnMyMouseMoveOutside(aktion.Position);
            }
            catch { }
        }
        
        protected virtual void OnMyMouseMoveOutside(Point mousePosition)
        {
            if (_FavoritesContainer != null)
            {
                _FavoritesContainerRect = new Rect(_FavoritesContainer.PointToScreen(new Point()), new Size(_FavoritesContainer.ActualWidth, _FavoritesContainer.ActualHeight));
                _FavoritesContainerProximityRect = _FavoritesContainerRect;
                _FavoritesContainerProximityRect.Inflate(PROXIMITYRANGE, PROXIMITYRANGE); 

                if (_FavoritesContainerShown)
                {
                    if (!_FavoritesContainerProximityRect.Contains(mousePosition))
                    {
                        _FavoritesContainerShown = false;
                        Animations.ElementYFadeOut(_FavoritesContainer);
                    }
                }
                else
                {
                    if (_FavoritesContainerProximityRect.Contains(mousePosition))
                    {
                        _FavoritesContainerShown = true;
                        Animations.ElementYFadeIn(_FavoritesContainer);
                    }
                }
            }
        }

        void DirectoryViewer_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender != e.OriginalSource) return;
            if (DirectorySelected != null && e.NewValue is TreeViewItem)
            {
                var item = e.NewValue as TreeViewItem;
                if (item.Header is DirectoryViewerTag)
                {
                    var tag = item.Header as DirectoryViewerTag;
                    DirectorySelected.Invoke(this, new DirectorySelectedEventArgs() { Tag = tag });

                    if (_FavoritesRemoveButton != null)
                    {
                        _FavoritesRemoveButton.Visibility = tag.IsFavorite ? Visibility.Visible : Visibility.Collapsed;
                        if (_FavoritesHint != null)
                        {
                            _FavoritesHint.Visibility = Visibility.Collapsed;
                        }
                    }
                    if (_FavoritesAddButton != null)
                    {
                        _FavoritesAddButton.Visibility = false == tag.IsFavorite ? Visibility.Visible : Visibility.Collapsed;
                        if (_FavoritesHint != null)
                        {
                            _FavoritesHint.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
        }

        public void SafeSelected(string path)
        {
            try 
            {
                if (!string.IsNullOrEmpty(path))
                {
                    string[] pathPieces = path.Split(new char []{Path.DirectorySeparatorChar}, StringSplitOptions.RemoveEmptyEntries);
                    TryExpand(Items, pathPieces, 0);
                }

            } catch { }
        }
        private void TryExpand(ItemCollection items, string[] paths, int index)
        {
            int i = index;
            if (i < paths.Length)
            {
                string piece = paths[i];
                if (piece[piece.Length-1] == Path.VolumeSeparatorChar)
                    piece += Path.DirectorySeparatorChar;
                TreeViewItem expanded = null;
                foreach (var obj in items)
                {
                    TreeViewItem item = obj as TreeViewItem;
                    if (item != null)
                    {
                        DirectoryViewerTag tag = item.Header as DirectoryViewerTag;
                        if (tag != null)
                        {
                            if (tag.Info.Name.Equals(piece))
                            {
                                item.ExpandSubtree();
                                expanded = item;
                                break;
                            }
                        }
                    }
                }
                if (expanded != null)
                    TryExpand(expanded.Items, paths, index++);
            }
        }

        private void OnFavoritesChanged()
        {
            bool hasFav = ContainsFavorite();
            ClearFavorites();

            if (Favorites != null)
            {
                if (Favorites.Count > 0)
                    Items.Insert(0, MakeSeperator("Favorites"));
                
                foreach (var item in Favorites)
                {
                    DirInfo difo = new DirInfo();
                    try 
                    {
                        difo.IsFavorite = true;
                        difo.Directory = item.Key;
                        difo.FavoriteName = item.Value;
                        difo.HasSubFolders = Directory.GetDirectories(item.Key).FirstOrDefault() != null;
                    }
                    catch { difo = null; }

                    if (difo != null)
                        Items.Insert(1, MakeNode(difo, null, null));
                }


                var binding = GetBindingExpression(FavoritesProperty);
                if (binding != null) binding.UpdateSource();
            }

        }
        public void AddFavorite()
        {
            var item = SelectedItem as TreeViewItem;
            if (item != null && item.Header is DirectoryViewerTag)
            {
                var tag = item.Header as DirectoryViewerTag;

                if (!Favorites.ContainsKey(tag.Info.FullName))
                {
                    Favorites.Add(tag.Info.FullName, tag.Info.Name);
                    OnFavoritesChanged();
                }
            }
        }
        public void RemoveFavorite()
        {
            var item = SelectedItem as TreeViewItem;
            if (item != null && item.Header is DirectoryViewerTag)
            {
                var tag = item.Header as DirectoryViewerTag;

                if (tag.IsFavorite)
                {
                    Favorites.Remove(tag.Info.FullName);
                    OnFavoritesChanged();
                }
            } 
        }

        private void ClearFavorites()
        {
            List<TreeViewItem> rem = new List<TreeViewItem>();
            foreach (TreeViewItem item in Items)
            {
                var root = item.Tag as DirectoryViewerTag;
                if (root != null && root.IsFavorite) rem.Add(item);
                if (root == null && item.Header is string && item.Header.Equals("Favorites")) rem.Add(item);
            }
            foreach (var item in rem)
            {
                Items.Remove(item);
            }
        }
        private bool ContainsFavorite()
        {
            foreach (TreeViewItem item in Items)
            {
                var root = item.Tag as DirectoryViewerTag;
                if (root != null && root.IsFavorite) return true;
            }
            return false;
        }

        public void ApplyStandartFolders()
        {
            var drives = SafeGetDrives();
            bool hasDrives = drives.Count > 0;

            Items.Add(MakeSeperator(Environment.UserName));
            Items.Add(MakeNode(null, null, Environment.SpecialFolder.DesktopDirectory));
            Items.Add(MakeNode(null, null, Environment.SpecialFolder.MyDocuments));
            Items.Add(MakeNode(null, null, Environment.SpecialFolder.MyPictures));
            Items.Add(MakeNode(null, null, Environment.SpecialFolder.MyMusic));
            Items.Add(MakeNode(null, null, Environment.SpecialFolder.MyVideos));

            if (hasDrives)
                Items.Add(MakeSeperator("Computer"));
            else
                Items.Add(MakeSeperator());
            
            foreach (var item in drives)
            {
                if (item.IsReady)
                {
                    hasDrives = true;
                    Items.Add(MakeNode(null, item, null));
                }
            }

            if (hasDrives)
                Items.Add(MakeSeperator());
        }

        private List<DriveInfo> SafeGetDrives()
        {
            var drives = new List<DriveInfo>();

            try
            {
                foreach (var item in DriveInfo.GetDrives())
                {
                    if (item.IsReady)
                    {
                        drives.Add(item);
                    }
                }
            }
            catch { }

            return drives;
        }
        private TreeViewItem MakeSeperator(string header = null)
        {
            TreeViewItem item = new TreeViewItem();
            item.Header = header;
            item.HeaderTemplate = FindResource("DirectoryViewerItemSeperatorTemplate") as DataTemplate;
            item.IsHitTestVisible = false;
            return item;
        }
        private TreeViewItem MakeNode(DirInfo path, DriveInfo info, Environment.SpecialFolder? spec)
        {
            TreeViewItem item = new TreeViewItem();
            DirectoryViewerTag tag = null;

            if (null != path)
            {
                tag = new DirectoryViewerTag(path.Directory, path.IsFavorite, path.FavoriteName);
            }
            else if (info != null)
                tag = new DirectoryViewerTag(info);
            else if (spec.HasValue)
                tag = new DirectoryViewerTag(spec.Value);

            if (tag == null) throw new InvalidOperationException();

            item.Tag = tag;
            item.Header = tag;
            item.HeaderTemplate = FindResource("DirectoryViewerItemTemplate") as DataTemplate;
            if ((path != null && path.HasSubFolders) || (Directory.GetDirectories(tag.Info.FullName).FirstOrDefault() != null))
                item.Items.Add(new TreeViewItem());
            item.Expanded += ExpandItem;
            item.MouseRightButtonUp += OnRightMouseUp;

            return item;
        }
        private void OnRightMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender != e.Source) return;
            var container = sender as TreeViewItem;
            var root = container.Tag as DirectoryViewerTag;

            if (DirectoryRightClick != null && root != null)
            {
                DirectoryRightClick.Invoke(this, new DirectorySelectedEventArgs() { Tag = root });
            }
        }
        private void ExpandItem(object sender, RoutedEventArgs e)
        {
            if (sender != e.OriginalSource) return;

            var container = sender as TreeViewItem;
            var root = container.Tag as DirectoryViewerTag;

            System.Diagnostics.Debugger.Log(0, "", root.ToString() + Environment.NewLine);
            Populate(sender as TreeViewItem);
        }
        private class DirInfo
        {
            public bool HasSubFolders { get; set; }
            public string Directory { get; set; }
            public bool IsFavorite { get; set; }
            public string FavoriteName { get; set; }
        }
        private async void Populate(TreeViewItem container)
        {
            var root = container.Tag as DirectoryViewerTag;
            if (root != null && root.Populating) 
                return;

            if (container.Items.Count == 1 &&
                container.Items[0] is TreeViewItem)
            {
                root.Populating = true;
                container.Items.Clear();

                List<DirInfo> paths = await Task.Run<List<DirInfo>>(() =>
                {
                    List<DirInfo> p = new List<DirInfo>();
                    try
                    {
                        foreach (var item in Directory.GetDirectories(root.Info.FullName))
                            try
                            {
                                p.Add(new DirInfo() { Directory = item, HasSubFolders = Directory.GetDirectories(item).FirstOrDefault() != null });
                            }
                            catch { }
                    }
                    catch { }
                    return p;
                });

                foreach (var path in paths)
                {
                    container.Items.Add(MakeNode(path, null, null));
                }

                if (root != null)
                    root.Populating = false;
            }
        }
    }
}
