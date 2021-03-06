﻿#if __ANDROID__
using System;
using Android.Text.Method;
using Android.Text.Util;
using Android.Widget;
using Xamarin.Forms.CommonCore;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Util = Android.Util;
using Graphics = Android.Graphics;

[assembly: ExportRenderer(typeof(CoreTextArea), typeof(CoreTextAreaRenderer))]
namespace Xamarin.Forms.CommonCore
{
    public class CoreTextAreaRenderer : ViewRenderer<CoreTextArea, TextView>
    {
        private TextView txtView;
        private CoreTextArea parent;

        protected override void OnElementChanged(ElementChangedEventArgs<CoreTextArea> e)
        {
            base.OnElementChanged(e);
            if (e.NewElement != null)
                parent = e.NewElement;

            if (txtView == null)
            {
                txtView = new TextView(Forms.Context);
                txtView.Text = e.NewElement.Text;

                var textColor = Graphics.Color.Black;
                if (((int)parent.TextColor.R) != -1)
                    textColor = e.NewElement.TextColor.ToAndroid();

                if (parent.LinksEnabled)
                    Linkify.AddLinks(txtView, MatchOptions.All);

                txtView.SetTextSize(Util.ComplexUnitType.Sp, (float)parent.FontSize);
                txtView.SetTextColor(textColor);
                SetNativeControl(txtView);
            }
        }
    }
}
#endif

