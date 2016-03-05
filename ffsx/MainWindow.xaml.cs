using ffsx.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;
using ffsx.Classes;
using LinqToVisualTree;
using System.Diagnostics;

namespace ffsx
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private bool EdPathChangedByCode = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnCommand(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
        }

        private void OnTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            BindingExpression binding = null;
            if (sender is WatermarkTextBox)
            {
                WatermarkTextBox tb = sender as WatermarkTextBox;
                binding = tb.GetBindingExpression(WatermarkTextBox.TextProperty);
            }
            else 
            if (sender is TextBox)
            {
                TextBox tb = sender as TextBox;
                binding = tb.GetBindingExpression(TextBox.TextProperty);
            }
            else
            if (sender is ComboBox)
            {
                ComboBox cb = sender as ComboBox;
                binding = cb.GetBindingExpression(ComboBox.TextProperty);
            }

            if (binding != null)
                binding.UpdateSource();

            if (sender == EdPath)
                OnSearchPathChanged();
        }

        private void OnSearchPathChanged()
        {
            if (!EdPathChangedByCode)
            {
                string path = EdPath.Text;
                if (Directory.Exists(path))
                {
                    DvDirtree.SafeSelected(path);
                }
            }
        }

        private void OnSearchClicked(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.Search();
        }

        private void OnDirectorySelected(object sender, Controls.DirectorySelectedEventArgs e)
        {
            EdPathChangedByCode = true;
            EdPath.Text = e.Tag.Info.FullName;
            EdPathChangedByCode = false;
        }

        private void OnResultItemRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
            ListBox lb = fe.GetParents<ListBox>().FirstOrDefault();
            if (lb != null)
            {
                ShellContextMenu menu = new ShellContextMenu();
                FileInfo[] arrFI = new FileInfo[lb.SelectedItems.Count];
                int idx = 0;
                foreach (SearchResultViewModel item in lb.SelectedItems)
                {
                    arrFI[idx++] = item.File;
                }

                menu.ShowContextMenu(arrFI, System.Windows.Forms.Control.MousePosition);
            }
        }

        private void OnCloseSearch(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
            SearchViewModel search = fe.DataContext as SearchViewModel;
            if (search != null)
            {
                MainViewModel.Instance.Remove(search);
            }
        }

        private void OnDirectoryRightClick(object sender, Controls.DirectorySelectedEventArgs e)
        {
            ShellContextMenu menu = new ShellContextMenu();
            DirectoryInfo[] arrFI = new DirectoryInfo[1] { e.Tag.Info };
            menu.ShowContextMenu(arrFI, System.Windows.Forms.Control.MousePosition);
        }

        private void OnResultDirectoryClicked(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
            SearchResultViewModel result = fe.DataContext as SearchResultViewModel;
            if (result != null)
            {
                Process process = new Process();
                process.StartInfo.FileName = "explorer.exe";
                process.StartInfo.Arguments = string.Format("/e,/select,\"{0}\"", result.File.FullName);
                process.Start();
            }
        }
    }
}
