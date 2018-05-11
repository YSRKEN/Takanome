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
using CoreTweet.Core;

namespace Takanome
{
	class MainController : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public ReactiveProperty<string> SearchWord { get; } = new ReactiveProperty<string>("");
		public ReactiveProperty<Status> SelectTweet { get; } = new ReactiveProperty<Status>();
		public ReadOnlyReactiveProperty<string> LabelText { get; }
		public ReadOnlyReactiveProperty<string> ButtonText { get; }
		public ReadOnlyReactiveCollection<Status> SearchResult { get; }
		private ReadOnlyReactiveProperty<bool> showListViewFlg { get; }
		public ReactiveCommand SearchStartCommand { get; }

		private ObservableCollection<Status> searchResult = new ObservableCollection<Status>();
		private ReactiveProperty<bool> progressFlg = new ReactiveProperty<bool>(false);
		private ReactiveProperty<bool> getTokenFlg = new ReactiveProperty<bool>(false);
		private OAuthSession session;
		private TokensBase token;

		public MainController() {
			LabelText = getTokenFlg.Select(flg => flg ? "PINコード" : "検索ワード").ToReadOnlyReactiveProperty();
			ButtonText = getTokenFlg.Select(flg => flg ? "入力" : "検索").ToReadOnlyReactiveProperty();
			showListViewFlg = getTokenFlg.Select(flg => !flg).ToReadOnlyReactiveProperty();
			SearchStartCommand = SearchWord.Select(s => s.Length != 0).CombineLatest(progressFlg, (x, y) => x & !y).ToReactiveCommand();
			SearchResult = searchResult.ToReadOnlyReactiveCollection();
			//
			SearchStartCommand.Subscribe(async _ => {
				if (getTokenFlg.Value) {
					// トークン入力処理
					token = session.GetTokens(SearchWord.Value);
					getTokenFlg.Value = false;
					SearchWord.Value = "";
				}
				else {
					// 検索処理
					progressFlg.Value = true;
					searchResult.Clear();
					var botList = new List<string> { "twittbot.net", "IFTTT", "rakubo2" };
					try {
						foreach (var status in await token.Search.TweetsAsync(count => 100, q => SearchWord.Value + " exclude:retweets lang:ja")) {
							// ツイート本文が存在し、
							string tweet = status.Text;
							if (tweet == null)
								continue;
							// botツイートではなく
							if (botList.Any(str => status.Source.Contains(str)))
								continue;
							if (status.User.Name.Contains("bot"))
								continue;
							// スクリーンネーム以外の部分でヒットした場合、
							Regex rgx = new Regex("@[A-Za-z_]+");
							tweet = rgx.Replace(tweet, "");
							if (!tweet.Contains(SearchWord.Value))
								continue;
							// リストに加える
							searchResult.Add(status);
						}
						progressFlg.Value = false;
					}
					catch (TwitterException e) {
						// API呼び出し回数で規制を食らった場合の処理
						Console.WriteLine(e.Message);
						Console.WriteLine(e.StackTrace);
						//規制解除時刻を算出する(日本時間に決め打ち)
						var reset = e.RateLimit.Reset;
						var fixedReset = reset.ToUniversalTime().AddHours(9);
						string message = e.Message + "\n" + fixedReset.ToString("yyyy/MMM/dd HH:mm:ss zzz") + "に規制解除";
						//算出した結果を表示
						MessagingCenter.Send(this, "DisplayAlert", new AlertParameter() {
							Title = "Takanome",
							Message = message
						});
						progressFlg.Value = false;
					}
					catch (Exception e) {
						// 
						Console.WriteLine(e.Message);
						Console.WriteLine(e.StackTrace);
						progressFlg.Value = false;
					}
				}
			});
			SelectTweet.Subscribe(s => {
				if (s != null) {
					string url = "https://twitter.com/" + s.User.ScreenName + "/status/" + SelectTweet.Value.Id;
					Device.OpenUri(new Uri(url));
				}
			});
			//
			if (true) {
				token = Tokens.Create(TwiDev.CK, TwiDev.CS, TwiDev.AT, TwiDev.ATS);
			}
			else if (true) {
				session = Authorize(Consumer.Key, Consumer.Secret);
				Device.OpenUri(session.AuthorizeUri);
				getTokenFlg.Value = true;
			}
			else {
				//token = OAuth2.GetToken(Consumer.Key, Consumer.Secret);
			}
		}
	}
}
