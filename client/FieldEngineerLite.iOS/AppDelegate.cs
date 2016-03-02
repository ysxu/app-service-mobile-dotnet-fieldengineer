using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading.Tasks;

using Foundation;
using UIKit;

using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json.Linq;

using Xamarin.Forms;
using Xamarin.Forms.Maps;

using System.Text.RegularExpressions;

using System.Net.Http;

using Plugin.Geolocator;

namespace FieldEngineerLite.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate, IPusher
    {
        // class-level declarations
        //UIWindow window;

		private MobileServiceUser user;
		private IMobileServiceClient client;
		private NSData deviceToken; 

		// Return username of currently logged in user
		public async Task<string> GetUsername(IMobileServiceClient client)
		{
			//Get user name
			var url = "https://demofieldengineernxevufbcegvsy.azurewebsites.net/.auth/me";
			HttpClient httpclient = new HttpClient();
			httpclient.DefaultRequestHeaders.Add("X-ZUMO-AUTH", client.CurrentUser.MobileServiceAuthenticationToken);

			HttpResponseMessage response = await httpclient.GetAsync (url);
			string content = await response.Content.ReadAsStringAsync();

			// get rid of {,},[,]
			content = content.Replace("{","").Replace("}","").Replace("[","").Replace("]","").Replace("\"","");

			// split with , and :
			String [] splitContent = content.Split(new Char[] { ',', ':'},
				StringSplitOptions.RemoveEmptyEntries).ToArray();

			// find "name" key
			int nameIndex = Array.IndexOf(splitContent, "name");
			// get "name" value
			return splitContent[(nameIndex+2)];
		}
			
		// Return current location ({city} {state}) of device
		public async Task<string> GetLocation()
		{
			try {
				// Get Longitude and Latitude of current device location
				var locator = CrossGeolocator.Current;
				locator.DesiredAccuracy = 50;
				locator.AllowsBackgroundUpdates = true;

				var position = await locator.GetPositionAsync(timeoutMilliseconds: 10000);
				var geoCoderPosition = new Xamarin.Forms.Maps.Position(position.Latitude, position.Longitude);

				// Reverse geocoding
				var geocoder = new Geocoder();
				Xamarin.FormsMaps.Init();
				var addresses = await geocoder.GetAddressesForPositionAsync(geoCoderPosition);

				foreach (var address in addresses)
				{
					string addressString = address.ToString();
					List<string> addressParse = addressString.Split("\n".ToCharArray()).ToList<string>();
					string cityState = Regex.Replace(addressParse[1], @"[\d-]", string.Empty);
					return cityState.Trim();
				}
			}
			catch (Exception ex)
			{
				UIAlertView avAlert = new UIAlertView("postion failed", ex.Message, null, "OK", null);
				avAlert.Show();
			}
			return null;
		}

		// Register push for authenticated username and current device location
		public async Task<bool> RegisterPush()
		{
			var success = false;
			try
			{
				client = new MobileServiceClient("https://demofieldengineernxevufbcegvsy.azurewebsites.net/");

				// Sign in with AAD
				if (user == null)
				{
					user = await client.LoginAsync(UIApplication.SharedApplication.KeyWindow.RootViewController,
						MobileServiceAuthenticationProvider.WindowsAzureActiveDirectory);
				}

				// Register for push with your mobile app
				var push = client.GetPush ();
				await push.RegisterAsync (deviceToken);

				// update push tags
				var body = new JArray();

				// Get location
				string location = await this.GetLocation();
				string locationTag = location.Replace(" ","_").ToLower();

				// Get user name
				string userName = await GetUsername(client);
				string userTag = userName.Replace(" ","_").ToLower();

				body.Add(locationTag);
				body.Add(userTag);

				// Call the custom API '/api/register/<installationid>' with tags
				await client.InvokeApiAsync("register/" + client.InstallationId, body);

				UIAlertView avAlert = new UIAlertView("Push Notifications", "Push is enabled for " + userName + " in " + location.Split(' ').First() + "," + location.Split(' ').Last() + ".", null, "OK", null);
				avAlert.Show();

				success = true;
			}
			catch (Exception ex)
			{
				UIAlertView avAlert = new UIAlertView("Push could not be enabled", ex.Message, null, "OK", null);
				avAlert.Show();
			}
			return success;
		}

		// Unregister user for push
		public async Task<bool> UnregisterPush()
		{
			var success = true;
			try
			{
				if (client != null) {
					// Unregisters push for user id
					await client.GetPush().UnregisterAsync();

					/*
					// Sign out
					await client.LogoutAsync();

					user = null;

					// Clear cookies
					var store = NSHttpCookieStorage.SharedStorage;
					var cookies = store.Cookies;
					foreach (var c in cookies) {
						store.DeleteCookie (c);
					}
					*/
				}
			}
			catch (Exception ex)
			{
				success = false;
				UIAlertView avAlert = new UIAlertView("Log out failed", ex.Message, null, "OK", null);
				avAlert.Show();
			}
			if (success) 
			{
				user = null;
				UIAlertView avAlert = new UIAlertView("", "You have successfully unregistered for push notifications!", null, "OK", null);
				avAlert.Show();
			}
			return success;
		}

        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            Forms.Init();
            Microsoft.WindowsAzure.MobileServices.CurrentPlatform.Init();
            SQLitePCL.CurrentPlatform.Init();         

			App.Init(this);

            var myapp = new App();
            LoadApplication(myapp);

            var success = base.FinishedLaunching (app, options);
            App.UIContext = UIApplication.SharedApplication.KeyWindow.RootViewController;

			// registers for push for iOS8
			var settings = UIUserNotificationSettings.GetSettingsForTypes(
				UIUserNotificationType.Alert
				| UIUserNotificationType.Badge
				| UIUserNotificationType.Sound,
				new NSSet());

			UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);
			UIApplication.SharedApplication.RegisterForRemoteNotifications();
			Console.WriteLine("finished launching called register");

            return success;
        }
			

		public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
		{
			this.deviceToken = deviceToken;
		}

		public override void DidReceiveRemoteNotification (UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
		{
			NSDictionary aps = userInfo.ObjectForKey(new NSString("aps")) as NSDictionary;

			string alert = string.Empty;
			if (aps.ContainsKey(new NSString("alert")))
				alert = (aps [new NSString("alert")] as NSString).ToString();

			//show alert
			if (!string.IsNullOrEmpty(alert))
			{
				UIAlertView avAlert = new UIAlertView("Notification", alert, null, "OK", null);
				avAlert.Show();
			}
		}
			
			
    }
}
