using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocketSharp;
using Windows.Media.Control;
using Windows.Web.Http;

namespace AppleMusicRPC
{
    internal class Provider
    {
        private GlobalSystemMediaTransportControlsSessionManager _sessionManager;
        private GlobalSystemMediaTransportControlsSession _ampSession;

        private const string AMPModelId = "AppleInc.AppleMusicWin";

        private readonly Payload _payload;
        private SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public Provider()
        {
            _payload = new Payload();

            UpdateSessionManager();
            UpdateAMPSession();
        }

        private async void UpdateSessionManager()
        {
            if (_sessionManager != null) return;

            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _sessionManager.SessionsChanged += OnSessionsChanged;
            GetAMPSession();
        }

        private void UpdateAMPSession()
        {
            UpdateSessionManager();
            GetAMPSession();
        }

        private void GetAMPSession()
        {
            if (_sessionManager != null)
            {
                GlobalSystemMediaTransportControlsSession newSession = FindAMPSession();
                SetAMPSession(newSession);
            }

            if (_ampSession != null)
            {
                _ampSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                _ampSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
                OnMediaPropertiesChanged(_ampSession, null);
            }
            else
            {
                _payload.ResetToInitialState();
            }
        }

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            Thread.Sleep(1000);
            try
            {
                if (_ampSession == null)
                {
                    _payload.ResetToInitialState();
                    RPCManager.SetActivity(_payload);
                    return;
                };

                var playbackInfo = _ampSession.GetPlaybackInfo();
                var timelineProperties = _ampSession.GetTimelineProperties();
                _payload.playerState = playbackInfo.PlaybackStatus.ToString().ToLower() == Payload.PlayingStatuses.Playing
                        ? Payload.PlayingStatuses.Playing : Payload.PlayingStatuses.Paused;

                _payload.endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() +
                                    (timelineProperties.EndTime.TotalMilliseconds - timelineProperties.Position.TotalMilliseconds);
                _payload.duration = timelineProperties.EndTime.TotalSeconds - timelineProperties.StartTime.TotalSeconds;

                RPCManager.SetActivity(_payload);
            } catch (Exception _)
            {
                _payload.ResetToInitialState();
                RPCManager.SetActivity(_payload);
            }
        }

        private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            await semaphore.WaitAsync();
            try
            {
                await sender.TryGetMediaPropertiesAsync();
            } catch (Exception _)
            {

            } 
            
            var songInfos = ClientScrapper.GetInfos();
            if (songInfos != null)
            {
                _payload.artist = songInfos.SongArtist;
                _payload.album = songInfos.SongAlbum;
                _payload.title = songInfos.SongName;
                OnPlaybackInfoChanged(null, null);
            }
            else
            {
                _payload.ResetToInitialState();
                RPCManager.SetActivity(_payload);
            }
             semaphore.Release();
        }

        private void SetAMPSession(GlobalSystemMediaTransportControlsSession newSession)
        {
            if (newSession == null && _ampSession != null)
            {
                _ampSession = null;
            }
            else if (_ampSession == null)
            {
                _ampSession = newSession;
            }
        }

        private GlobalSystemMediaTransportControlsSession FindAMPSession()
        {
            IReadOnlyList<GlobalSystemMediaTransportControlsSession> sessions = _sessionManager.GetSessions();
            foreach (GlobalSystemMediaTransportControlsSession session in sessions)
            {
                if (session.SourceAppUserModelId.Contains(AMPModelId))
                {
                    return session;
                }
            }

            return null;
        }

        private void OnSessionsChanged(object sender, SessionsChangedEventArgs e)
        {
            UpdateAMPSession();
        }

        private static async Task<GlobalSystemMediaTransportControlsSessionMediaProperties> GetMediaProperties(GlobalSystemMediaTransportControlsSession AMPSession) =>
            AMPSession == null ? null : await AMPSession.TryGetMediaPropertiesAsync();

        public async void NowPlaying()
        {
            string format = "#NowPlaying \"{title}\" by {artist}";
            string text = format;
            text = text.Replace("{title}", _payload.title);
            text = text.Replace("{artist}", _payload.artist);

            switch (_payload.playerState)
            {
                case "playing":
                case "paused":
                    try {
                        var extras = await TrackExtras.GetTrackExtras(
                            _payload.title,
                            _payload.artist,
                            _payload.album
                        );

                        var artworkUrl = extras.ArtworkUrl;
                        if (!artworkUrl.IsNullOrEmpty())
                        {
                            artworkUrl = Regex.Replace(artworkUrl, "/\\w+\\.jpg", "/1000x1000.jpg");

                            var webClient = new WebClient();
                            using (var stream = webClient.OpenRead(artworkUrl))
                            {
                                using (var bitmap = new Bitmap(stream))
                                {
                                    Clipboard.SetImage(bitmap);
                                }
                            }
                        }
                        else
                        {
                            Clipboard.Clear();
                        }
                    } catch (Exception e) {
                        Clipboard.Clear();
                    }

                    break;
                default:
                    break;
            }

            Process.Start("https://twitter.com/intent/tweet?text=" + WebUtility.UrlEncode(text));
        }
    }
}
