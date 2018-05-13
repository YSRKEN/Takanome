using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.OS;
using Xamarin.Forms;
using Android.Content;
using Takanome.Droid;

[assembly: Dependency(typeof(ClipBoardService))]
namespace Takanome.Droid
{
    [Activity(Label = "Takanome", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());
        }
	}
	public class ClipBoardService : IClipBoardService
	{
		public string Paste() {
			ClipboardManager clipboard = (ClipboardManager)Forms.Context.GetSystemService(Context.ClipboardService);
			var item = clipboard.PrimaryClip.GetItemAt(0);
			return item.Text;
		}

		public void Copy(string title, string target) {
			ClipboardManager clipboard = (ClipboardManager)Forms.Context.GetSystemService(Context.ClipboardService);
			ClipData clip = ClipData.NewPlainText(target, target);
			clipboard.PrimaryClip = clip;
		}
	}
}

