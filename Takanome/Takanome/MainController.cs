using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Text;
using Xamarin.Forms;

namespace Takanome
{
	class MainController : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public ReactiveProperty<string> SearchWord { get; } = new ReactiveProperty<string>("艦これ");
		public ReadOnlyReactiveCollection<string> SearchResult { get; }
		public ReactiveCommand SearchStartCommand { get; }

		public MainController() {
			SearchStartCommand = SearchWord.Select(s => s.Length != 0).ToReactiveCommand();
			SearchStartCommand.Subscribe(_ => {
				MessagingCenter.Send(this, "DisplayAlert", new AlertParameter() { Title = "Takanome", Message = "検索ワード：" + SearchWord.Value });
			});
		}
	}
}
