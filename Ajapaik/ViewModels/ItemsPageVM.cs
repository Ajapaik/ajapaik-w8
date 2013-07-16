using Ajapaik.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ajapaik.ViewModels
{
    class ItemsPageVM
    {
        private ObservableCollection<Pin> _pinlist;

        internal ObservableCollection<Pin> Pinlist
        {
            get { return _pinlist; }
        }

        public ItemsPageVM(ObservableCollection<Pin> pinlist)
        {
            this._pinlist = pinlist;
        }

    }
}
