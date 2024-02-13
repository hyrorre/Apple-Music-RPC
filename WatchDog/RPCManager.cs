using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordRPC;

namespace AppleMusicRPC
{
    internal class RPCManager
    {

        private static DiscordRpcClient rpcClient { get; set; }

        private static DiscordRpcClient GetClient() {
            if (rpcClient == null)
            {
                rpcClient = new DiscordRpcClient("773825528921849856");
                rpcClient.Initialize();
            }

            return rpcClient;
        }

        private static Assets BuildAssetsFromPayload(Payload payload, TrackExtras extras)
        {
            return new Assets
            {
                LargeImageKey = extras.ArtworkUrl ?? "appicon",
                LargeImageText = payload.album
            };
        }

        private static Button[] BuildButtonsFromPayload(Payload payload, TrackExtras extras)
        {
            var Buttons = new List<Button>();


            // Apple Music
            if (extras.ItunesUrl != null)
            {
                Buttons.Add(new Button
                {
                    Label = "Play on Apple Music",
                    Url = extras.ItunesUrl,
                });
            }

            Uri uri;
            string query;

            ////Spotify
            // query = $"{payload.artist} {payload.title}";
            // uri = new Uri($"https://open.spotify.com/search/{query}?si");
            // if (uri.AbsolutePath.Length <= 512)
            // {
            //     Buttons.Add(new Button
            //     {
            //         Label = "Search on Spotify",
            //         Url = uri.AbsoluteUri,
            //     });
            // }

            //Youtube
            query = $"{payload.artist.Replace("#", "%23").Replace("&", "%26")} {payload.title.Replace("#", "%23").Replace("&","%26")}";
            uri = new Uri($"https://music.youtube.com/search?q={query}");
            if (uri.AbsolutePath.Length <= 512)
            {
                Buttons.Add(new Button
                {
                    Label = "Search on Youtube",
                    Url = uri.AbsoluteUri,
                });
            }

            return Buttons.ToArray();
        }

        private static RichPresence BuildPlayingPresenceFromPayload(Payload payload, TrackExtras extras)
        {
            var assets = BuildAssetsFromPayload(payload, extras);
            var buttons = BuildButtonsFromPayload(payload, extras);

            return new RichPresence
            {
                Buttons = buttons,
                Assets = assets,
                State = $"{payload.artist}",
                Details = $"{payload.title}",
                Timestamps = new Timestamps {EndUnixMilliseconds = (ulong?)payload.endTime }
            };
        }

        private static RichPresence BuildPausedPresenceFromPayload(Payload payload, TrackExtras extras)
        {
            var assets = BuildAssetsFromPayload(payload, extras);
            var buttons = BuildButtonsFromPayload(payload, extras);

            return new RichPresence
            {
                Buttons = buttons,
                Assets = assets,
                State = $"{payload.artist} - {payload.title}",
                Details = $"Paused"            
            };
        }

        public static async Task SetActivity(Payload payload)
        {
            TrackExtras extras;
            switch (payload.playerState)
            {
                case "playing":
                    extras = await TrackExtras.GetTrackExtras(payload.title, payload.artist, payload.album);

                    GetClient().SetPresence(BuildPlayingPresenceFromPayload(payload, extras));
                    break;
                case "paused":
                    extras = await TrackExtras.GetTrackExtras(payload.title, payload.artist, payload.album);
                    GetClient().SetPresence(BuildPausedPresenceFromPayload(payload, extras));
                    break;
                default:
                    GetClient().ClearPresence();
                    break;
            }
            
        }
    }
}
