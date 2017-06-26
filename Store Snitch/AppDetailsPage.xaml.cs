using System;
using Humanizer;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Store_Snitch
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class AppDetailsPage : Page
  {
    public AppDetailsPage()
    {
      this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
      // It is recommended to only retrieve the ShareOperation object in the activation handler, return as
      // quickly as possible, and retrieve all data from the share target asynchronously.

      var appDetails = (AppDetails)e.Parameter;
      Color bgColor;
      try {
        bgColor = (Color)XamlBindingHelper.ConvertValue(typeof(Color), appDetails.backgroundColor);
      } catch {
        // handle unspecified background colour - happens for some tiles such as Whatsapp.
        bgColor = Colors.Transparent;
      }

      // Some app tiles can also define 'transparent' colour - which will be
      // transformed to the system accent colour.
      if (bgColor.Equals(Colors.Transparent)) {
        bgColor = (Color)this.Resources["SystemAccentColor"];
      }

      // Get back to the UI thread using the dispatcher.
      await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
        var fill = new SolidColorBrush(bgColor);
        appImage.Source = new BitmapImage(new Uri(appDetails.image));
        appImageBg.Fill = fill;
        appImageHeroBg.Fill = fill;
        appName.Text = appDetails.name;
        appReleaseDate.Text = appDetails.releaseDate.ToLocalTime().ToString();
        appReleaseDateRelative.Text = appDetails.releaseDate.Humanize();
        appLastUpdatedDate.Text = appDetails.lastUpdatedDate.ToLocalTime().ToString();
        appLastUpdatedDateRelative.Text = appDetails.lastUpdatedDate.Humanize();
      });
    }

    private void ToMainPage_Click(object sender, RoutedEventArgs e)
    {
      Frame rootFrame = Window.Current.Content as Frame;
      rootFrame.Navigate(typeof(MainPage));
      Window.Current.Activate();
    }
  }
}
