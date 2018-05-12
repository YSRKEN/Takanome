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
using System.Threading.Tasks;

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
		public ReactiveCommand LoginCommand { get; } = new ReactiveCommand();

		private ObservableCollection<Status> searchResult = new ObservableCollection<Status>();
		private ReactiveProperty<bool> progressFlg = new ReactiveProperty<bool>(false);
		private ReactiveProperty<bool> getTokenFlg = new ReactiveProperty<bool>(false);
		private OAuthSession session;
		private Tokens token;
		private List<string> botList = new List<string> {
			"twittbot.net", "IFTTT", "rakubo2", "makebot.sh", "BotMaker",
			"twiroboJP", "Easybotter"
		};

		public MainController() {
			LabelText = getTokenFlg.Select(flg => flg ? "PINコード" : "検索ワード").ToReadOnlyReactiveProperty();
			ButtonText = getTokenFlg.Select(flg => flg ? "入力" : "検索").ToReadOnlyReactiveProperty();
			showListViewFlg = getTokenFlg.Select(flg => !flg).ToReadOnlyReactiveProperty();
			SearchStartCommand = SearchWord.Select(s => s.Length != 0).CombineLatest(progressFlg, (x, y) => x & !y).ToReactiveCommand();
			SearchResult = searchResult.ToReadOnlyReactiveCollection();
			//
			LoginCommand.Subscribe(_ => Login());
			SearchStartCommand.Subscribe(async _ => {
				if (getTokenFlg.Value) {
					// トークン入力処理
					token = session.GetTokens(SearchWord.Value);
					Application.Current.Properties["AccessToken"] = token.AccessToken;
					Application.Current.Properties["AccessTokenSecret"] = token.AccessTokenSecret;
					getTokenFlg.Value = false;
					SearchWord.Value = "";
				}
				else {
					// 検索処理
					await SearchTweet();
				}
			});
			SelectTweet.Subscribe(s => {
				if (s != null) {
					string url = "https://twitter.com/" + s.User.ScreenName + "/status/" + SelectTweet.Value.Id;
					Device.OpenUri(new Uri(url));
				}
			});
			//トークンが保存されているかを確かめ、されてない場合に限りログイン処理を行う
			if (Application.Current.Properties.ContainsKey("AccessToken")
				&& Application.Current.Properties.ContainsKey("AccessTokenSecret")) {
				token = Tokens.Create(Consumer.Key, Consumer.Secret,
					Application.Current.Properties["AccessToken"] as string,
					Application.Current.Properties["AccessTokenSecret"] as string);
			}
			else {
				Login();
			}
		}

		private async Task SearchTweet() {
			// 検索処理
			progressFlg.Value = true;
			searchResult.Clear();
			try {
				foreach (var status in await token.Search.TweetsAsync(count => 100, q => SearchWord.Value + " exclude:retweets")) {
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
					if (!Utility.IsMatch(tweet, SearchWord.Value))
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

		private void Login() {
			getTokenFlg.Value = true;
			session = Authorize(Consumer.Key, Consumer.Secret);
			Device.OpenUri(session.AuthorizeUri);
		}
	}
	static class Utility
	{
		public static bool IsMatch(string tweet, string query) {
			// 検索クエリをNFKC正規化する
			string query2 = query.Normalize(NormalizationForm.FormKC);
			// 検索クエリのスペース周りを正規化
			var rgx1 = new Regex(" {2,}");
			query2 = rgx1.Replace(query2, " ");
			var rgx2 = new Regex("^ ");
			query2 = rgx2.Replace(query2, "");
			var rgx3 = new Regex(" $");
			query2 = rgx3.Replace(query2, "");
			// スペースで区切り、「AND要素」と「OR要素」を抽出する
			string[] temp1 = query2.Split(' ');
			var temp2 = new List<string>();
			foreach (string keyword in temp1) {
				// 各種検索コマンドは無視する
				if (Regex.IsMatch(keyword, "(since|until):\\d+-\\d+-\\d+"))
					continue;
				if (Regex.IsMatch(keyword, "(-|)(@|from|to):[A-Za-z_]+"))
					continue;
				if (Regex.IsMatch(keyword, "(-|)filter:(images|videos|links|verified)"))
					continue;
				if (Regex.IsMatch(keyword, "(-|)source:[A-Za-z0-9_]+"))
					continue;
				// どんどん追加していく
				temp2.Add(keyword);
			}
			var temp3 = temp2.Select(str => new KeyValuePair<string, bool>(str, false)).ToList();
			for (int i = 1; i < temp2.Count - 1; ++i) {
				if (temp2[i] == "OR") {
					temp3[i - 1] = new KeyValuePair<string, bool>(temp3[i - 1].Key, true);
					temp3[i] = new KeyValuePair<string, bool>(temp3[i].Key, true);
					temp3[i + 1] = new KeyValuePair<string, bool>(temp3[i + 1].Key, true);
				}
			}
			var andKeyword = new List<string>();
			var orKeyword = new List<string>();
			for (int i = 0; i < temp3.Count; ++i) {
				if (temp3[i].Key == "OR")
					continue;
				if (temp3[i].Value) {
					orKeyword.Add(temp3[i].Key);
				}
				else {
					andKeyword.Add(temp3[i].Key);
				}
			}
			// マッチングを行う
			string tweet2 = tweet.Normalize(NormalizationForm.FormKC);
			if(andKeyword.Count != 0) {
				if(!andKeyword.All(str => tweet2.Contains(str))) {
					return false;
				}
			}
			if (orKeyword.Count != 0) {
				if (!orKeyword.Any(str => tweet2.Contains(str))) {
					return false;
				}
			}
			return true;
		}
	}
}
