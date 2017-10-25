using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using Plugin.MediaManager.Abstractions;
using Plugin.MediaManager.Abstractions.Enums;

namespace Plugin.MediaManager.Audio
{
    public class AudioPlayerImplementation : Java.Lang.Object, IAudioPlayer
    {
        private MediaManagerImplementation _mediaManagerImplementation;
        public MediaBrowserCompat mediaBrowser { get; private set; }
        public MediaControllerCompat mediaController { get; private set; }
        public MediaBrowserConnectionCallback mediaBrowserConnectionCallback { get; private set; }
        public MediaControllerCallback mediaControllerCallback { get; private set; }

        public AudioPlayerImplementation(MediaManagerImplementation mediaManagerImplementation)
        {
            _mediaManagerImplementation = mediaManagerImplementation;
        }

        public async Task<bool> ConnectService()
        {
            if (mediaController != null)
                return true;

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            mediaBrowserConnectionCallback = new MediaBrowserConnectionCallback(_mediaManagerImplementation);
            mediaBrowserConnectionCallback.OnConnectedImpl = () =>
            {
                mediaController = new MediaControllerCompat(_mediaManagerImplementation.Context, mediaBrowser.SessionToken);
                mediaControllerCallback = new MediaControllerCallback(_mediaManagerImplementation);
                mediaController.RegisterCallback(mediaControllerCallback);
                tcs.TrySetResult(true);
            };

            mediaBrowser = new MediaBrowserCompat(_mediaManagerImplementation.Context, new ComponentName(_mediaManagerImplementation.Context, Java.Lang.Class.FromType(typeof(AudioPlayerService))), mediaBrowserConnectionCallback, null);
            mediaBrowser.Connect();

            return await tcs.Task;
        }

        public PlaybackState State => PlaybackState.Loading;
        public TimeSpan Position => new TimeSpan();
        public TimeSpan Duration => new TimeSpan();
        public TimeSpan Buffered => new TimeSpan();
        public Dictionary<string, string> RequestHeaders { get; set; } = new Dictionary<string, string>();

        public event StatusChangedEventHandler Status;
        public event PlayingChangedEventHandler Playing;
        public event BufferingChangedEventHandler Buffering;
        public event MediaFinishedEventHandler Finished;
        public event MediaFailedEventHandler Failed;

        public Task Pause()
        {
            mediaController?.GetTransportControls().Pause();
            return Task.CompletedTask;
        }

        public async Task Play(string url)
        {
            if (await ConnectService())
                mediaController?.GetTransportControls().PlayFromUri(Android.Net.Uri.Parse(url), null);
        }

        public async Task Play(IMediaItem item)
        {
            var FLAG_BROWSABLE = 1;
            var FLAG_PLAYABLE = 2;

            if (await ConnectService())
            {
                var _builder = new MediaDescriptionCompat.Builder();

                _builder.SetMediaId(new Guid().ToString())
                    .SetTitle(item.Metadata.DisplayTitle)
                    .SetDescription(item.Metadata.DisplayDescription)
                    .SetSubtitle(item.Metadata.DisplaySubtitle)
                    .SetIconBitmap(item.Metadata.DisplayIcon as Bitmap);
                if (!string.IsNullOrEmpty(item.Metadata.DisplayIconUri))
                    _builder.SetIconUri(Android.Net.Uri.Parse(item.Metadata.DisplayIconUri));

                mediaController?.GetTransportControls().PlayFromMediaId(_builder.Build().MediaId, Bundle.Empty);
            }
        }

        public async Task Seek(TimeSpan position)
        {
            if (await ConnectService())
                if (long.TryParse(position.TotalSeconds.ToString(), out var pos))
                    mediaController?.GetTransportControls().SeekTo(pos);
        }

        public async Task Stop()
        {
            if (await ConnectService())
                mediaController?.GetTransportControls().Stop();
        }
    }
}
