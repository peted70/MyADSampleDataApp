using Microsoft.Azure.ActiveDirectory.GraphClient;
using MyADSampleDataApp.GenUsers;
using Newtonsoft.Json;
using PCLCommandBase;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MyADSampleDataApp
{
    public class ViewModel : BindableBase
    {
        private bool _isBusy;
        ObservableCollection<IUser> _users;

        public bool IsBusy
        {
            get { return _isBusy; }
            set { SetProperty(ref _isBusy, value); }
        }

        public ObservableCollection<IUser> Users
        {
            get { return _users; }
            set { SetProperty(ref _users, value); }
        }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public ViewModel ViewModel { get; set; } = new ViewModel();

        public MainPage()
        {
            this.InitializeComponent();
            Loaded += OnLoaded;
        }

        ActiveDirectoryClient _client = null;

        private static string tenant = "<TENANT>";
        private static readonly string ClientId = "<CLIENT ID>";
        private static readonly Uri redirectUri = new Uri("<REDIRECT URI>");
        private static readonly string Resource = "https://graph.windows.net";
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
#if ADAL
                // Ok, to use async () here as ctor takes a Func<Task<string>>
                _client =
                    new ActiveDirectoryClient(new Uri($"{Resource}/{tenant}"),
                        async () =>
                        {
                            var auth = await GetAuthAsync();
                            return auth.AccessToken;
                        });
#else
                ViewModel.IsBusy = true;

                _client =
                    new ActiveDirectoryClient(new Uri($"{Resource}/{tenant}"),
                        () =>
                        {
                            var tcs = new TaskCompletionSource<string>();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                            {
                                try
                                {                                 // replace 'organizations' with 'consumers' to use MSA
                                    string authority = $"https://login.microsoftonline.com/{tenant}";
                                    //string authority = "organizations";
                                    var wap = await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com",
                                        authority);

                                    WebTokenRequest wtr = new WebTokenRequest(wap, string.Empty, ClientId);
                                    wtr.Properties.Add("resource", Resource);

                                    WebTokenRequestResult wtrr = await WebAuthenticationCoreManager.RequestTokenAsync(wtr);

                                    if (wtrr.ResponseStatus == WebTokenRequestStatus.Success)
                                        tcs.SetResult(wtrr.ResponseData[0].Token);
                                }
                                catch (Exception ex)
                                {
                                    tcs.SetException(ex);
                                }
                                finally
                                {
                                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                                        () =>
                                        {
                                            ViewModel.IsBusy = false;
                                        });
                                }
                            });

                            return tcs.Task;
                        });

#endif
                // put first page into obs collection for now...
                var users = await _client.Users.ExecuteAsync();
                ViewModel.Users = new ObservableCollection<IUser>(users.CurrentPage);
            }
            catch (Exception ex)
            {
                MessageDialog mg = new MessageDialog(ex.Message);
                await mg.ShowAsync();
            }
        }

        public async Task AddRandomUsersAsync()
        {
            ViewModel.IsBusy = true;

            try
            {
                var http = new HttpClient();
                int results = 200;
                var res = await http.GetAsync(new Uri($"http://api.randomuser.me/?results={results}"));
                var resStr = await res.Content.ReadAsStringAsync();
                
                // Deserialise the response and convert over to a collection of IUser for adding to AD...
                var root = JsonConvert.DeserializeObject<RootObject>(resStr);

                var users = root.results.Select((u) =>
                {
                    return new Microsoft.Azure.ActiveDirectory.GraphClient.User
                    {
                        AccountEnabled = true,
                        City = u.user.location.city,
                        DisplayName = $"{u.user.name.first} {u.user.name.last}",
                        GivenName = $"{ u.user.name.first}",
                        ImmutableId = u.user.sha1,
                        Mobile = u.user.phone,
                        MailNickname = u.user.username,
                        UserPrincipalName = $"{u.user.name.first}.{u.user.name.last}@{tenant}",
                        PasswordProfile = new PasswordProfile { Password = "TempPa55w0rd", ForceChangePasswordNextLogin = true },
                        State = u.user.location.state,
                        StreetAddress = u.user.location.street,
                        PostalCode = u.user.location.zip,
                        Surname = u.user.name.last,
                        TelephoneNumber = u.user.phone,
                    };
                });

                foreach (var usr in users)
                {
                    await _client.Users.AddUserAsync(usr);
                }

                var md = new MessageDialog("Users Added");
                var strs = users.Select(u => u.DisplayName);
                md.Content = string.Join("\n", strs);
                await md.ShowAsync();
            }
            finally
            {
                ViewModel.IsBusy = false;
            }
        }

#if ADAL
        private Task<AuthenticationResult> GetAuthAsync()
        {
            var tcs = new TaskCompletionSource<AuthenticationResult>();
            AuthenticationResult res = null;
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    AuthenticationContext ac = new AuthenticationContext("https://login.windows.net/common");
                    try
                    {
                        // Idea here is to always call AcquireToken and it will deal with caching and refresh, etc..
                        res = await ac.AcquireTokenAsync(Resource, ClientId, redirectUri);
                        tcs.SetResult(res);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
            return tcs.Task;
        }
#endif

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            await AddRandomUsersAsync();

        }
    }
}
