using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Takanome
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();
			this.BindingContext = new MainController();
		}

		private void MainPage_Appearing(object sender, EventArgs e) {
			MessagingCenter.Subscribe<MainController, AlertParameter>(this, "DisplayAlert", DisplayAlert);
		}

		private void MainPage_Disappearing(object sender, EventArgs e) {
			MessagingCenter.Unsubscribe<MainController, AlertParameter>(this, "DisplayAlert");
		}

		private async void DisplayAlert<T>(T sender, AlertParameter arg) {
			await DisplayAlert(arg.Title, arg.Message, "OK");
		}
	}
}
