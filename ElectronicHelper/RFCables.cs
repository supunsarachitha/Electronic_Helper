﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace ElectronicHelper
{
    [Activity(Label = "RFCables")]
    public class RFCables : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ImageView);

            this.Title = "Coaxial cable Connectors";

            var Image = FindViewById<ElectronicHelper.Views.ScaleImageView>(Resource.Id.ScaleImg);
            Image.SetImageResource(Resource.Drawable.CoaxialCable);
            // Create your application here
        }
    }
}