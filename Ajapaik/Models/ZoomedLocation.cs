using Bing.Maps;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ajapaik.Models
{
    public class ZoomedLocation
    {
        private Location loc;
        private double zoom;
        private ObservableCollection<Pin> pins = new ObservableCollection<Pin>();
        private MapType mapLookType;
        private Pin lastSelectedOldPic;

        public Pin LastSelectedOldPic
        {
            get { return lastSelectedOldPic; }
            set { lastSelectedOldPic = value; }
        }

        public ObservableCollection<Pin> Pins
        {
            get { return pins; }
            set { pins = value; }
        }

        public Location Loc
        {
            get { return loc; }
            set { loc = value; }
        }
        public double Zoom
        {
            get { return zoom; }
            set { zoom = value; }
        }

        public MapType MapLookType
        {
            get { return mapLookType; }
            set { mapLookType = value; }
        }
    }
}
