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
using System.Text.RegularExpressions;

namespace Takanome
{
	class MainController : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public ReactiveProperty<string> SearchWord { get; } = new ReactiveProperty<string>("YSR");
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
					foreach (var status in await apponly.Search.TweetsAsync(count => 100, q => SearchWord.Value + " exclude:retweets lang:ja")) {
						string tweet = status.Text;
						if(tweet == null)
							continue;
						Regex rgx = new Regex("@[A-Za-z_]+");
						tweet = rgx.Replace(tweet, "");
						if (!tweet.Contains(SearchWord.Value))
							continue;
						searchResult.Add(status.Text);
					}
				}
				catch (TwitterException e) {
					Console.WriteLine(e.Message);
					Console.WriteLine(e.StackTrace);
					var reset = e.RateLimit.Reset;
					var fixedReset = reset.ToUniversalTime().AddHours(9);
					string message = e.Message + "\n" + fixedReset.ToString("yyyy/MMM/dd HH:mm:ss zzz") + "に規制解除";
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
					if(searchResult.Count == 0) {
						searchResult.Add("<なし>");
					}
					progressFlg.Value = false;
				}
			});
		}
	}
}
