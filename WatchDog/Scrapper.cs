using System;
using System.Collections.Generic;
using System.Diagnostics;
using FlaUI.UIA3;
using FlaUI.Core.Conditions;
using System.Text.RegularExpressions;
using FlaUI.Core.AutomationElements;

namespace AppleMusicRPC
{

    internal class SongInfos
    {
        public string SongName { get; private set; }
        public string SongArtist { get; private set; }
        public string SongAlbum { get; private set; }
        public string SongPerformer { get; private set; }

        public SongInfos(string SongName, string SongArtist, string SongAlbum, string SongPerformer) {
            this.SongName = SongName;
            this.SongArtist = SongArtist;
            this.SongAlbum = SongAlbum;
            this.SongPerformer = SongPerformer;
        }
    }

    internal class ClientScrapper
    {
        private static readonly Regex ComposerPerformerRegex = new Regex(@"By\s.*?\s\u2014", RegexOptions.Compiled);

        public static SongInfos GetInfos()
        {
            var amProcesses = Process.GetProcessesByName("AppleMusic");
            if (amProcesses.Length == 0)
            {
                return null;
            }

            var app = FlaUI.Core.Application.Attach(amProcesses[0].Id);
            using (var automation = new UIA3Automation())
            {
                var window = app.GetMainWindow(automation);
                var amWinTransportBar = FindFirstDescendantWithAutomationId(window, "TransportBar");
                if (amWinTransportBar == null)
                {
                    return null;
                }
                var amWinLCD = amWinTransportBar.FindFirstChild("LCD");

                // song panel not initialised
                if (amWinLCD == null)
                {
                    return null;
                }

                var songFields = amWinLCD.FindAllChildren(new ConditionFactory(new UIA3PropertyLibrary()).ByAutomationId("myScrollViewer"));

                if (songFields.Length != 2)
                {
                    return null;
                }

                var songNameElement = songFields[0];
                var songAlbumArtistElement = songFields[1];

                if (songNameElement.BoundingRectangle.Bottom > songAlbumArtistElement.BoundingRectangle.Bottom)
                {
                    songNameElement = songFields[1];
                    songAlbumArtistElement = songFields[0];
                }

                string songName;
                string songAlbumArtist;

                try
                {
                   songName = songNameElement.Name;
                   songAlbumArtist = songAlbumArtistElement.Name;
                } catch
                {
                    return null;
                }
                

                string songArtist = "";
                string songAlbum = "";
                string songPerformer = null;

                try
                {
                    var songInfo = ParseSongAlbumArtist(songAlbumArtist, false);
                    songArtist = songInfo.Item1;
                    songAlbum = songInfo.Item2;
                    songPerformer = songInfo.Item3;
                }
                catch (Exception ex)
                {
                }
                return new SongInfos(songName, songArtist, songAlbum, songPerformer);
            }
        }


        private static Tuple<string, string, string> ParseSongAlbumArtist(string songAlbumArtist, bool composerAsArtist)
        {
            string songArtist;
            string songAlbum;
            string songPerformer = null;

            // some classical songs add "By " before the composer's name
            var songComposerPerformer = ComposerPerformerRegex.Matches(songAlbumArtist);
            if (songComposerPerformer.Count > 0)
            {
                var splitted = Regex.Split(songAlbumArtist, @" \u2014 ");
                var songComposer = splitted[0].Remove(0, 3);
                songPerformer = splitted[1];
                songArtist = composerAsArtist ? songComposer : songPerformer;
                songAlbum = splitted[2];
            }
            else
            {
                // U+2014 is the emdash used by the Apple Music app, not the standard "-" character on the keyboard!
                var songSplit = Regex.Split(songAlbumArtist, @" \u2014 ");
                if (songSplit.Length > 1)
                {
                    songArtist = songSplit[0];
                    songAlbum = songSplit[1];
                }
                else
                { // no emdash, probably custom music
                    // TODO find a better way to handle this?
                    songArtist = songSplit[0];
                    songAlbum = songSplit[0];
                }
            }
            return new Tuple<string, string, string>(songArtist, songAlbum, songPerformer);
        }

        private static AutomationElement FindFirstDescendantWithAutomationId(AutomationElement baseElement, string id)
        {
            List<AutomationElement> nodes = new List<AutomationElement>() { baseElement };
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.Properties.AutomationId.IsSupported && node.AutomationId == id)
                {
                    return node;
                }
                nodes.AddRange(node.FindAllChildren());
            }
            return null;
        }
    }
       
}