using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Store_Snitch
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class MainPage : Page
  {
    public MainPage()
    {
      this.InitializeComponent();
    }

    System.Uri _appUri;

    public string Version
    {
      get {
        Package package = Package.Current;
        PackageId packageId = package.Id;
        PackageVersion version = packageId.Version;

        return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
      }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
      // It is recommended to only retrieve the ShareOperation object in the activation handler, return as
      // quickly as possible, and retrieve all data from the share target asynchronously.

      ShareOperation shareOperation;
      try {
        shareOperation = (ShareOperation)e.Parameter;
      } catch {
        return;
      }

      await Task.Factory.StartNew(async () => {
        // Retrieve the data package content.
        // The GetWebLinkAsync(), GetTextAsync(), GetStorageItemsAsync(), etc. APIs will throw if there was an error retrieving the data from the source app.
        // In this sample, we just display the error. It is recommended that a share target app handles these in a way appropriate for that particular app.
        if (shareOperation.Data.Contains(StandardDataFormats.WebLink)) {
          try {
            this._appUri = await shareOperation.Data.GetWebLinkAsync();
          } catch (Exception ex) {
            throw new Exception("Failed GetWebLinkAsync - " + ex.Message);
          }
        }

        // In this sample, we just display the shared data content.

        // Get back to the UI thread using the dispatcher.
        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
          if (this._appUri != null) {
            var url = this._appUri.AbsoluteUri;
            storeURLInput.Text = url;
            this.showDetails(url);
          }
        });
      });
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      var url = storeURLInput.Text;
      if (url.Length > 0) {
        this.showDetails(url);
      }
    }

    private async Task<AppDetails> GetDetailsFromURL(string url)
    {
      var storeQuery = new StoreQuery();
      var response = await storeQuery.makeRequest(url);
      JObject appEntryPayload = storeQuery.findPayload(response);
      return storeQuery.getUWPAppDetails(appEntryPayload);
    }

    private async void showDetails(string url)
    {
      var spinner = new ProgressRing();
      spinner.IsActive = true;
      var oldButtonContent = fetchButton.Content;
      fetchButton.Content = spinner;

      AppDetails appDetails;

      try {
        appDetails = await this.GetDetailsFromURL(url);
      } catch (Exception e) {
        var dialog = new MessageDialog(e.Message);
        await dialog.ShowAsync();
        fetchButton.Content = oldButtonContent;
        return;
      }

      fetchButton.Content = oldButtonContent;

      Frame rootFrame = Window.Current.Content as Frame;
      if (rootFrame == null) {
        // Create a Frame to act as the navigation context and navigate to the first page
        rootFrame = new Frame();

        // Place the frame in the current Window
        Window.Current.Content = rootFrame;
      }
      rootFrame.Navigate(typeof(AppDetailsPage), appDetails);
      Window.Current.Activate();
    }
  }
}
