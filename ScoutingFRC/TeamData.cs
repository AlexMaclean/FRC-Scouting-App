using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace ScoutingFRC
{
    [Serializable]
    class TeamData
    {
        public int teamNumber;
        public DateTime timeCollected;
        public string scoutName;
        public string notes;
    }
}