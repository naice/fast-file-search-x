using ffsx.Class;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ffsx.ViewModel
{
    public class SearchResultViewModel : ViewModelBase
    {
        private string _ImageKey;
        public BitmapSource Image
        {
            get 
            {
                BitmapSource src = null;

                if (_ImageKey == null)
                    _ImageKey = WpfFileIconCache.GetFileIconKey(File.FullName);

                if (WpfFileIconCache.ICONS.ContainsKey(_ImageKey))
                    src = WpfFileIconCache.ICONS[_ImageKey];

                return src;
            }
        }

        private FileInfo _File;
        public FileInfo File
        {
            get { return _File; }
            set
            {
                if (value != _File)
                {
                    _File = value;
                    RaisePropertyChanged("File");
                }
            }
        }

        private int _Position;
        public int Position
        {
            get { return _Position; }
            set
            {
                if (value != _Position)
                {
                    _Position = value;
                    RaisePropertyChanged("Position");
                }
            }
        }

        private int _Line;
        public int Line
        {
            get { return _Line; }
            set
            {
                if (value != _Line)
                {
                    _Line = value;
                    RaisePropertyChanged("Line");
                }
            }
        }
        private int _Column;
        public int Column
        {
            get { return _Column; }
            set
            {
                if (value != _Column)
                {
                    _Column = value;
                    RaisePropertyChanged("Column");
                }
            }
        }
    }
}
