using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ViewModelBase : INotifyPropertyChanged, INotifyPropertyChanging
{
    public event PropertyChangingEventHandler PropertyChanging;
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void RaisePropertyChanged(string propertyName)
    {
        if (PropertyChanged != null)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    protected virtual void RaisePropertyChanging(string propertyName)
    {
        if (PropertyChanging != null)
        {
            PropertyChanging.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }
    }
}
