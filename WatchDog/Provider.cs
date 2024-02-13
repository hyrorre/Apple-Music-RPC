using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace WatchDog
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
    }
}
