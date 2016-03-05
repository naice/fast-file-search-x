using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Xceed.Wpf.Toolkit;

namespace ffsx.Controls
{
    public class AutoCompleteTextBox : WatermarkTextBox
    {
        Popup Popup { get { return this.Template.FindName("PART_Popup", this) as Popup; } }
        ListBox ItemList { get { return this.Template.FindName("PART_ItemList", this) as ListBox; } }
        Grid Root { get { return this.Template.FindName("root", this) as Grid; } }
        ScrollViewer Host { get { return this.Template.FindName("PART_ContentHost", this) as ScrollViewer; } }
        UIElement TextBoxView { get { foreach (object o in LogicalTreeHelper.GetChildren(Host)) return o as UIElement; return null; } }

        private bool _loaded = false;
        private bool _prevState = false;
        private bool _handleOnTextChanged = true;
        private bool _isSuggestAppend = false;
        string _lastPath;

        public AutoCompleteTextBox()
        {
            string template =
                "<ControlTemplate TargetType=\"{x:Type t:WatermarkTextBox}\"                     " + Environment.NewLine +
                "  xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"           " + Environment.NewLine +
                "  xmlns:t=\"http://schemas.xceed.com/wpf/xaml/toolkit\"                         " + Environment.NewLine +
                "  xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">                     " + Environment.NewLine +                                                       
                "  <Grid x:Name=\"root\">                                                        " + Environment.NewLine +
                "      <Border x:Name=\"Border\" BorderThickness=\"{TemplateBinding BorderThickness}\" BorderBrush=\"{TemplateBinding BorderBrush}\" CornerRadius=\"1\" Background=\"{TemplateBinding Background}\" />          " + Environment.NewLine +
                "      <Border x:Name=\"MouseOverVisual\" Opacity=\"0\" BorderThickness=\"{TemplateBinding BorderThickness}\" CornerRadius=\"1\" />                                                                             " + Environment.NewLine +
                "      <Border x:Name=\"FocusVisual\" Opacity=\"0\" BorderThickness=\"{TemplateBinding BorderThickness}\" CornerRadius=\"1\" />                                                                                 " + Environment.NewLine +
                "      <ScrollViewer x:Name=\"PART_ContentHost\" SnapsToDevicePixels=\"{TemplateBinding SnapsToDevicePixels}\" />                                                                                               " + Environment.NewLine +
                "      <ContentPresenter x:Name=\"PART_WatermarkHost\"                                                                                                                                                          " + Environment.NewLine +
                "                      Content=\"{TemplateBinding Watermark}\"                                                                                                                                                  " + Environment.NewLine +
                "                      ContentTemplate=\"{TemplateBinding WatermarkTemplate}\"                                                                                                                                  " + Environment.NewLine +
                "                      VerticalAlignment=\"{TemplateBinding VerticalContentAlignment}\"                                                                                                                         " + Environment.NewLine +
                "                      HorizontalAlignment=\"{TemplateBinding HorizontalContentAlignment}\"                                                                                                                     " + Environment.NewLine +
                "                      IsHitTestVisible=\"False\"                                                                                                                                                               " + Environment.NewLine +
                "                      Margin=\"{TemplateBinding Padding}\"                                                                                                                                                     " + Environment.NewLine +
                "                      Visibility=\"Collapsed\" />                                                                                                                                                              " + Environment.NewLine +
                "      <Popup x:Name=\"PART_Popup\"                                              " + Environment.NewLine +
                "                  AllowsTransparency=\"true\"                                   " + Environment.NewLine +
                "                  Placement=\"Custom\"                                          " + Environment.NewLine +
                "                  IsOpen=\"False\"                                              " + Environment.NewLine +
                "                  PopupAnimation=\"{DynamicResource {x:Static SystemParameters.ComboBoxPopupAnimationKey}}\" " + Environment.NewLine +
                "                  VerticalOffset=\"{Binding Path=Top, RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type Window}}}\" " + Environment.NewLine +
                "                  HorizontalOffset=\"{Binding Path=Left, RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type Window}}}\" >     " + Environment.NewLine +
                "            <ListBox x:Name=\"PART_ItemList\"                                   " + Environment.NewLine +
                "                  SnapsToDevicePixels=\"{TemplateBinding SnapsToDevicePixels}\" " + Environment.NewLine +
                "                  VerticalContentAlignment=\"Stretch\"                          " + Environment.NewLine +
                "                  HorizontalContentAlignment=\"Stretch\"                        " + Environment.NewLine +
                "                  KeyboardNavigation.DirectionalNavigation=\"Contained\" />     " + Environment.NewLine +
                "      </Popup>                                                                  " + Environment.NewLine +
                "  </Grid>                                                                       " + Environment.NewLine +
                "</ControlTemplate>                                                              " ;

            Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(template);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _loaded = true;
            this.KeyDown += new KeyEventHandler(AutoCompleteTextBox_KeyDown);
            this.PreviewKeyDown += new KeyEventHandler(AutoCompleteTextBox_PreviewKeyDown);
            ItemList.PreviewMouseDown += new MouseButtonEventHandler(ItemList_PreviewMouseDown);
            ItemList.KeyDown += new KeyEventHandler(ItemList_KeyDown);
            Popup.CustomPopupPlacementCallback += new CustomPopupPlacementCallback(Repositioning);


            Window parentWindow = getParentWindow();
            if (parentWindow != null)
            {
                parentWindow.Deactivated += delegate { _prevState = Popup.IsOpen; Popup.IsOpen = false; };
                parentWindow.Activated += delegate { Popup.IsOpen = _prevState; };
            }
        }

        private Window getParentWindow()
        {
            DependencyObject d = this;
            while (d != null && !(d is Window))
                d = LogicalTreeHelper.GetParent(d);
            return d as Window;
        }

        //09-04-09 Based on SilverLaw's approach 
        private CustomPopupPlacement[] Repositioning(Size popupSize, Size targetSize, Point offset)
        {
            return new CustomPopupPlacement[] {
                new CustomPopupPlacement(new Point((0.01 - offset.X), (Root.ActualHeight - offset.Y)), PopupPrimaryAxis.None) };
        }

        void AutoCompleteTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (ItemList.Items.Count > 0 && !(e.OriginalSource is ListBoxItem))
                switch (e.Key)
                {
                    case Key.Up:
                    case Key.Down:
                    case Key.Prior:
                    case Key.Next:
                        ItemList.Focus();
                        ItemList.SelectedIndex = 0;
                        ListBoxItem lbi = ItemList.ItemContainerGenerator.ContainerFromIndex(ItemList.SelectedIndex) as ListBoxItem;
                        lbi.Focus();
                        e.Handled = true;
                        break;
                }                

            if (e.Key == Key.Back)
            {
                int length = SelectionLength;
                int pos = SelectionStart;

                _handleOnTextChanged = false;
                if (_isSuggestAppend && !string.IsNullOrEmpty(SelectedText) && !string.IsNullOrEmpty(Text))
                {
                    SelectedText = "";
                    Text = Text.Remove(Text.Length - 1);
                    Select(Text.Length, 0);
                    e.Handled = true;
                }
                else
                {
                    
                }
                _handleOnTextChanged = true;
            }


            _isSuggestAppend = false;
        }
        
        void ItemList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is ListBoxItem)
            {

                ListBoxItem tb = e.OriginalSource as ListBoxItem;

                e.Handled = true;
                switch (e.Key)
                {
                    case Key.Enter:
                        Text = (tb.Content as string); updateSource(); break;
                    //12-25-08 - added "\" support when picking in list view
                    case Key.Oem5:
                        Text = (tb.Content as string) + "\\";
                        break;
                    //12-25-08 - roll back if escape is pressed
                    case Key.Escape:
                        Text = _lastPath.TrimEnd('\\') + "\\";
                        break;
                    default: e.Handled = false; break;
                }
                //12-25-08 - Force focus back the control after selected.
                if (e.Handled)
                {
                    Keyboard.Focus(this);
                    Popup.IsOpen = false;
                    this.Select(Text.Length, 0); //Select last char
                }
            }
        }
        
        void AutoCompleteTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Popup.IsOpen = false;
                updateSource();
            }
        }

        void updateSource()
        {
            var binding = this.GetBindingExpression(TextBox.TextProperty);
            if (binding != null)
                binding.UpdateSource();
        }

        void ItemList_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                TextBlock tb = e.OriginalSource as TextBlock;
                if (tb != null)
                {
                    Text = tb.Text;
                    updateSource();
                    Popup.IsOpen = false;
                    e.Handled = true;
                }
            }
        }
        
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (_loaded && _handleOnTextChanged)
            {
                try
                {
                    _isSuggestAppend = false;
                    string text = this.Text;
                    string[] pt = GetPathAndToken(text);
                    string path = pt[0], token = pt[1], suggest = "", suggestAppend = "";

                    if (string.IsNullOrEmpty(path))
                    {
                        if (token.Length > 0)
                        {
                            path = Directory.GetDirectoryRoot(token);
                            suggest = path;
                            token = "";
                        }
                    }

                    List<string> paths = new List<string>(Lookup(path));

                    //_lastPath = Path.GetDirectoryName(text);
                    ItemList.Items.Clear();
                    bool getBestMatch  = false == string.IsNullOrEmpty(token);
                    foreach (string p in Lookup(path))
                    {
                        if (getBestMatch && p.StartsWith(text))
                        {
                            suggest = p;
                            getBestMatch = false;
                        }

                        ItemList.Items.Add(p);
                    }
                    if (false == string.IsNullOrEmpty(suggest))
                    {
                        suggestAppend = suggest.Substring(text.Length);
                    }

                    if (false == string.IsNullOrEmpty(suggestAppend))
                    {
                        _handleOnTextChanged = false;
                        _isSuggestAppend = true;
                        SelectedText = suggestAppend;
                        _handleOnTextChanged = true;
                    }


                    Popup.IsOpen = ItemList.Items.Count > 0;
                }
                catch
                {

                }
            }
        }

        private static string GetBestMatch(IEnumerable<string> paths, string path)
        {
            foreach (string p in paths)
            {
                if (p.StartsWith(path, StringComparison.InvariantCultureIgnoreCase))
                    return p;
            }

            return "";
        }

        private static string[] GetPathAndToken(string path)
        {
            string[] arr = new string[] {"", ""};

            int idx = path.LastIndexOf('\\');
            if (idx > 0)
                arr[0] = path.Substring(0, idx+1);
            if (idx < path.Length-1)
                arr[1] = path.Substring(idx + 1);

            return arr;
        }

        private static IEnumerable<string> Lookup(string path)
        {
            string[] dirs = new string[0];
            try
            {
                if (Directory.Exists(path))
                {
                    dirs = Directory.GetDirectories(path);
                }
            }
            catch (Exception ex)
            {
            }

            foreach (string item in dirs)
            {
                yield return item;
            }
        }
    }
}
