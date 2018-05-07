using CoreTweet;
using Reactive.Bindings;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Text;
using Xamarin.Forms;
using static CoreTweet.OAuth;

namespace Takanome
{
	class MainController : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public ReactiveProperty<string> SearchWord { get; } = new ReactiveProperty<string>("艦これ");
		public ReactiveProperty<string> SelectTweet { get; } = new ReactiveProperty<string>();
		public ReadOnlyReactiveCollection<string> SearchResult { get; }
		public ReactiveCommand SearchStartCommand { get; }

		private ObservableCollection<string> searchResult = new ObservableCollection<string>();
		private ReactiveProperty<bool> progressFlg = new ReactiveProperty<bool>(false);
		private OAuth2Token apponly;

		public MainController() {
			SearchStartCommand = SearchWord.Select(s => s.Length != 0).CombineLatest(progressFlg, (x, y) => x & !y).ToReactiveCommand();
			SearchResult = searchResult.ToReadOnlyReactiveCollection();
			//
			apponly = OAuth2.GetToken(Consumer.Key, Consumer.Secret);
			//
			SearchStartCommand.Subscribe(async _ => {
				progressFlg.Value = true;
				searchResult.Clear();
				try {
					foreach (var status in await apponly.Search.TweetsAsync(q => SearchWord.Value + " exclude:retweets lang:ja")) {
						if (!status.Text.Contains(SearchWord.Value))
							continue;
						searchResult.Add(status.Text);
					}
				}
				catch (TwitterException e) {
					Console.WriteLine(e.Message);
					Console.WriteLine(e.StackTrace);
					string message = e.Message;
					var response = await apponly.Application.RateLimitStatusAsync("resources");
					var reset = response["resources"]["/users/search"].Reset;
					message += "\n" + reset.ToString();
					MessagingCenter.Send(this, "DisplayAlert", new AlertParameter() {
						Title = "Takanome",
						Message = message
					});
				}
				catch(Exception e) {
					Console.WriteLine(e.Message);
					Console.WriteLine(e.StackTrace);
				}
				finally {
					progressFlg.Value = false;
				}
			});
		}
	}
}
