using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.Web.Http;

namespace Store_Snitch
{
  public class AppDetails
  {
    public string name { get; set; }
    public string image { get; set; }
    public string backgroundColor { get; set; }
    public DateTime releaseDate { get; set; }
    public DateTime lastUpdatedDate { get; set; }

    public AppDetails(string name, DateTime releaseDate)
    {
      this.name = name;
      this.releaseDate = releaseDate;
    }

    public override string ToString()
    {
      var releaseDate = this.releaseDate.ToLocalTime().ToString();
      string lastUpdatedDate;
      try {
        lastUpdatedDate = this.lastUpdatedDate.ToLocalTime().ToString();
      } catch {
        lastUpdatedDate = releaseDate + "(solo release)";
      }

      return "App Name: " + this.name + "\nReleased on: " + releaseDate + "\nLast Updated: " + lastUpdatedDate;
    }
  }

  class StoreQuery
  {
    private string URL;

    public StoreQuery()
    {
      this.URL = "https://storeedgefd.dsx.mp.microsoft.com/v8.0/pages/pdp?productId={0}&market=US&locale=en-US&appversion=11703.1001.45.0";
    }

    public async Task<string> makeRequest(string fullURL)
    {
      string id;
      id = fullURL.Split('/').Last();

      HttpClient httpClient = new HttpClient();
      var headers = httpClient.DefaultRequestHeaders;
      Uri requestUri = new Uri(String.Format(this.URL, id));

      //Send the GET request asynchronously and retrieve the response as a string.
      HttpResponseMessage httpResponse = new HttpResponseMessage();
      string httpResponseBody = "";

      try {
        //Send the GET request
        httpResponse = await httpClient.GetAsync(requestUri);
        httpResponse.EnsureSuccessStatusCode();
        httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
      } catch (Exception ex) {
        httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
      }
      return httpResponseBody;
    }

    /**
     * the json response we get from the store is a list of dicts with
     * several payloads containing different things, like a list of
     * related apps, etc. we need to find which one is the one with
     * the app details. it usually is the last one, but I don't think
     * it is a good idea to hardcode that.
    */
    public JObject findPayload(string response)
    {
      var res = this.parseResponse(response);
      Regex regex = new Regex(@"Microsoft.Marketplace.Storefront.Contracts.V3.ProductDetails");
      foreach (JObject entry in res) {
        var payload = entry.GetValue("Payload");
        if (payload == null) {
          continue; // try the next entry
        }
        try {
          Match match = regex.Match((string)payload["$type"]);
          if (match.Success) {
            return payload.Value<JObject>();
          }
        } catch (KeyNotFoundException) {
          continue;  // try the next entry
        } catch (Exception e) {
          throw StoreInsiderException(e.Message);
        }
      }
      return null;
    }

    public AppDetails getUWPAppDetails(JObject payload)
    {
      if (payload == null) {
        return new AppDetails("Not found", new DateTime());
      }
      var title = payload.Value<string>("Title");
      DateTime releaseDate;
      DateTime lastUpdatedDate;

      try {
        releaseDate = payload.Value<DateTime>("ReleaseDateUtc");
      } catch {
        releaseDate = DateTime.Parse(payload.Value<string>("ReleaseDateUtc"));
      }
      var details = new AppDetails(title, releaseDate);
      JEnumerable<JObject> images = payload.GetValue("Images").Children<JObject>();
      var logo = images.FirstOrDefault(img => (img["ImageType"].ToString() == "tile" || img["ImageType"].ToString() == "logo") && int.Parse(img["Width"].ToString()) >= 150);
      // if a bigger image is not found, use whatever is available
      if (logo == null) {
        logo = images.First(img => (img["ImageType"].ToString() == "tile" || img["ImageType"].ToString() == "logo"));
      }

      details.image = logo["Url"].ToString();
      details.backgroundColor = logo["BackgroundColor"].ToString();
      try {
        lastUpdatedDate = payload.Value<DateTime>("LastUpdateDateUtc");
      } catch {
        lastUpdatedDate = DateTime.Parse(payload.Value<string>("LastUpdateDateUtc"));
      }
      details.lastUpdatedDate = lastUpdatedDate;
      return details;
    }

    public JArray parseResponse(string data)
    {
      try {
        var json = JArray.Parse(data);
        return json;
      } catch (JsonReaderException) {
        throw StoreInsiderException("Couldn't parse response.");
      } catch (Exception err) {
        throw StoreInsiderException(err.Message);
      }
    }

    private Exception StoreInsiderException(string v)
    {
      throw new Exception(v);
    }
  }
}
