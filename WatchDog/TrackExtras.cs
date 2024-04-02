using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AppleMusicRPC
{
    internal class TrackExtras
    {

        protected class iTunesSearchResponse {
            [JsonProperty("resultCount")]
            public int ResultCount { get; set; }

            [JsonProperty("results")]
            public ItunesSearchResult[] Results { get; set; }
        }

        protected class ItunesSearchResult
        {
            [JsonProperty("trackName")]
            public string TrackName { get; set; }

            [JsonProperty("collectionName")]
            public string CollectionName { get; set; }

            [JsonProperty("artworkUrl100")]
            public string ArtworkUrl { get; set; }

            [JsonProperty("trackViewUrl")]
            public string TrackViewUrl { get; set; }
        }


        public string ArtworkUrl { get; private set; }
        public string ItunesUrl { get; private set; }

        public static async Task<TrackExtras> GetTrackExtras(string song, string artist, string album)
        {
            // GET JSON
            string[] queries = { $"{song} {artist} {album}", $"{song} {album}", $"{song} {artist}", $"{album}" };
            foreach(string query in queries)
            {
                WebClient webClient = new WebClient();
                webClient.QueryString.Add("media", "music");
                webClient.QueryString.Add("entity", "song");
                webClient.QueryString.Add("term", query.Replace("*", "").Replace("#", "%23").Replace("&", "%26"));
                var data = webClient.DownloadString(new Uri("https://itunes.apple.com/search"));
                var response = JsonConvert.DeserializeObject<iTunesSearchResponse>(data);

                if (response?.ResultCount > 0)
                {
                    Console.WriteLine("Hit music with the query : " + query);
                    // Get Track
                    ItunesSearchResult result = null;
                    if (response.ResultCount == 1)
                    {
                        result = response.Results[0];
                    }
                    else if (response.ResultCount > 1)
                    {
                        result = response.Results.FirstOrDefault(x =>
                                x.CollectionName.ToLower().Contains(album.ToLower())
                            && x.TrackName.ToLower().Contains(song.ToLower())
                            ) ?? response.Results[0];
                    }
                    else if (Regex.Match(album, @"\(.*\)").Success)
                    {
                        return await GetTrackExtras(song, artist, Regex.Replace(album, @"\(.*\)", ""));
                    }

                    return new TrackExtras { ArtworkUrl = result.ArtworkUrl ?? null, ItunesUrl = result.TrackViewUrl ?? null };
                }
            }
            Console.WriteLine("No music hitted");
            return null;
        }
    }
}
