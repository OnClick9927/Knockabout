/*********************************************************************************
 *Author:         anonymous
 *Version:        1.0
 *UnityVersion:   2021.3.33f1c1
 *Date:           2024-08-13
*********************************************************************************/
using System.Collections.Generic;
using UnityEngine;

namespace IFramework
{
    public interface IAudioHelper
    {
        AudioPref Read();
        void Write(AudioPref pref);


        bool IsDone(string path);
        void Load(string path);
        AudioClip GetClip(string path);
        void Release(string path);
    }
    public enum SoundCoverType
    {
        None = 0,
        All,
        Other
    }
    public interface IAudioConfig
    {
        bool GetSoundLoop(int sound_id);
        SoundCoverType GetSoundCover(int sound_id);
        string GetSoundPath(int sound_id);
        int GetSoundChannel(int sound_id);
        //IAudioAssetContext CreateContext();
        //void SetContext(IAudioAssetContext context);
    }
    //public interface IAudioAssetContext
    //{
    //    bool isDone { get; }

    //    void Load(string path);
    //    AudioClip GetClip();
    //    void Release(string path);
    //}

    [System.Serializable]
    public class AudioPref
    {
        [UnityEngine.SerializeField]
        private Dictionary<int, float> pairs = new Dictionary<int, float>();
        [UnityEngine.SerializeField]

        private float MainVolume = 1;
        internal float GetVolume(int channel)
        {
            float vol = -1;
            if (!pairs.TryGetValue(channel, out vol))
            {
                vol = -1;
            }
            return vol;
        }
        internal float GetMainVolume() => MainVolume;
        internal void SetMainVolume(float volume)
        {
            MainVolume = volume;
        }
        internal void SetVolume(int channel, float volume)
        {
            pairs[channel] = volume;
        }
    }
    class AudioPlayer
    {
        private AudioSource _source;
        private AudioService service;




        private float volume = 0f;
        private bool _loading;
        public int sound_id { get; private set; }

        public bool lifeEnd { get; private set; }

        public AudioAsset asset { get; private set; }



        public AudioPlayer(AudioService service, AudioSource source)
        {
            this.service = service;
            this._source = source;
        }

        //private float GetTargetVolume(float percent) => volume * (1 + percent);
        public void SetVolume(float volume)
        {
            this.volume = volume;
            if (!lifeEnd && sound_id != 0)
                _source.volume = volume;
        }


        private void PlayAudio()
        {
            if (lifeEnd) return;
            service.OnSoundBeginPlay(this.sound_id);

            AudioClip clip = asset.GetClip();
            _source.clip = clip;
            _source.volume = this.volume;
            _source.loop = service.config.GetSoundLoop(sound_id);
            _source.Play(0);
        }
        public void Play(int sound_id)
        {
            this.sound_id = sound_id;
            asset = service.Prepare(sound_id);
            if (asset.isDone)
                PlayAudio();
            else
                _loading = true;
        }
        public void Update()
        {
            if (lifeEnd) return;
            if (_loading)
            {
                if (!asset.isDone) return;
                if (asset.isDone)
                {
                    _loading = false;
                    PlayAudio();
                }
            }
            else
            {
                if (Application.runInBackground)
                {
                    if (!_source.isPlaying)
                        EndLife();
                }
                else
                {
                    if (!_source.isPlaying && Application.isFocused)
                        EndLife();
                }
            }
        }

        public void EndLife()
        {
            if (lifeEnd)
                return;

            lifeEnd = true;
            _loading = false;
            _source.Stop();
            _source.clip = null;
            service.OnSoundEnd(this.sound_id);
            service.ReleaseAsset(asset);
            asset = null;
            this.sound_id = 0;
        }

        public void BeginLife()
        {
            _loading = false;
            lifeEnd = false;
        }
    }

    class AudioChannel
    {
        private Queue<AudioPlayer> sleeps = new Queue<AudioPlayer>();
        private List<AudioPlayer> players = new List<AudioPlayer>();
        private int index;
        private AudioService service;

        public int channel { get; private set; }
        private float volume = 0.5f;
        private AudioPlayer Get()
        {
            AudioPlayer player = null;
            if (sleeps.Count > 0)
                player = sleeps.Dequeue();
            else
            {
                var source = new GameObject($"{channel}_{index++}").AddComponent<AudioSource>();
                source.transform.SetParent(Game.Current.transform);
                player = new AudioPlayer(this.service, source);
            }
            player.BeginLife();
            player.SetVolume(volume);
            players.Add(player);
            return player;
        }

        public AudioChannel(AudioService service, int channel)
        {
            this.service = service;
            this.channel = channel;
        }
        public void Play(int sound_id, SoundCoverType cover)
        {

            switch (cover)
            {
                case SoundCoverType.None:
                    Get().Play(sound_id);

                    break;
                case SoundCoverType.All:
                    StopChannel();
                    Get().Play(sound_id);
                    break;
                case SoundCoverType.Other:
                    bool play = IsPlaying(sound_id);
                    StopChannelWithout(sound_id);
                    if (!play)
                        Get().Play(sound_id);
                    break;
                default:
                    break;
            }

        }

        public void SetVolume(float volume)
        {
            this.volume = volume;
            for (int i = 0; i < players.Count; i++)
                players[i].SetVolume(volume);
        }
        private void BackToPool(AudioPlayer player)
        {
            //player.Stop();
            players.Remove(player);
            sleeps.Enqueue(player);
        }
        public void Update()
        {
            for (int i = players.Count - 1; i >= 0; i--)
            {
                players[i].Update();
                if (players[i].lifeEnd)
                    BackToPool(players[i]);
            }
        }

        public bool IsPlaying(int sound_id)
        {
            for (int i = players.Count - 1; i >= 0; i--)
            {
                if (players[i].sound_id == sound_id)
                    return true;
            }
            return false;
        }
        public void StopChannelWithout(int sound_id)
        {
            for (int i = players.Count - 1; i >= 0; i--)
            {
                if (players[i].sound_id != sound_id)
                    players[i].EndLife();
            }
        }
        public void StopChannel()
        {
            for (int i = players.Count - 1; i >= 0; i--)
                players[i].EndLife();
        }

        public void Stop(int sound_id, bool all)
        {
            if (all)
            {
                var _players = players.FindAll(x => x.sound_id == sound_id);
                if (_players != null)
                    foreach (var player in _players)
                        player.EndLife();
                //ShutDown(player);
            }
            else
            {

                var player = players.Find(x => x.sound_id == sound_id);
                if (player != null)
                    player.EndLife();

                //ShutDown(player);
            }
        }
    }

    class AudioAsset
    {
        private int refCount;
        private readonly float time = 10;
        private float _time;
        public string path {  get; private set; }
        public IAudioHelper context { get; private set; }

        public void InitAsset(IAudioHelper context, string path)
        {
            this.path = path;
            this.context = context;
            context.Load(path);
            _time = refCount = 0;
        }

        public bool isDone => context.IsDone(this.path);
        public AudioClip GetClip() => context.GetClip(this.path);
        public void Retain()
        {
            _time = time;
            refCount++;
        }
        public float MinusTime(float deltaTime)
        {
            if (refCount != 0)
                return time;
            _time -= deltaTime;
            return _time;
        }
        public void Release()
        {
            refCount--;
        }

        public void ReleaseAsset()
        {
            context.Release(path);
        }
    }

    public static class AudioServiceEx
    {
        public static Game UseAudio(this Game game, IAudioHelper helper, IAudioConfig config)
        {
            AudioService service = new AudioService(helper, config);
            game.Use(service);
            return game;
        }
        public static AudioService Audio(this Game game)
        {
            return game.GetRequiredService<AudioService>();
        }
        public static Game EnterAudio(this Game game)
        {
            game.EnterService<AudioService>();
            return game;
        }
    }

    public class AudioService : IFramework.ServiceBase
    {
        internal IAudioConfig config;
        internal IAudioHelper helper;
        AudioPref pref;

        private Dictionary<int, AudioChannel> channels;
        private Dictionary<string, AudioAsset> assets;
        public AudioService(IAudioHelper helper, IAudioConfig config)
        {
            this.helper = helper;
            this.config = config;
            assets = new Dictionary<string, AudioAsset>();
            channels = new Dictionary<int, AudioChannel>();
        }
        public delegate void AudioEvent(int id);
        public event AudioEvent onSoundEnd, onSoundBeginPlay;
        internal void OnSoundEnd(int id) => onSoundEnd?.Invoke(id);
        internal void OnSoundBeginPlay(int id) => onSoundBeginPlay?.Invoke(id);
        protected override void OnQuit(IServiceCollection services)
        {
            IFramework.Game.UnBindUpdate(Update);
            foreach (var channel in channels.Values)
                channel.StopChannel();
            foreach (var item in assets.Values)
                AddToRelease(item);
            ReleaseAssets();
        }
        protected override void OnUse(IServiceCollection services)
        {
            IFramework.Game.BindUpdate(Update);
        }
        protected override void OnEnter(IServiceCollection services)
        {
            pref = helper.Read() ?? new AudioPref();

        }
        private void Update()
        {

            foreach (var channel in channels.Values)
                channel.Update();
            float delta = Time.deltaTime;
            foreach (var item in assets.Values)
            {
                if (item.MinusTime(delta) <= 0)
                    AddToRelease(item);
            }
            ReleaseAssets();
        }
        public void SetDefaultVolume(int channel, float vol)
        {
            if (GetVolume(channel) != -1) return;
            SetVolume(channel, vol);
        }
        public void SetVolume(int channel, float volume)
        {
            pref.SetVolume(channel, volume);
            helper.Write(pref);

            AudioChannel chan = GetChannel(channel);
            chan.SetVolume(GetPlayVolume(channel));
        }
        public void SetMainVolume(float volume)
        {
            pref.SetMainVolume(volume);
            helper.Write(pref);
            foreach (var item in channels)
            {
                item.Value.SetVolume(GetPlayVolume(item.Key));
            }
        }

        public float GetMainVolume() => pref.GetMainVolume();
        public float GetPlayVolume(int channel) => pref.GetVolume(channel) * pref.GetMainVolume();

        public float GetVolume(int channel) => pref.GetVolume(channel);
        public void Play(int sound_id)
        {
            AudioChannel chan = GetChannel(config.GetSoundChannel(sound_id));
            chan.Play(sound_id, config.GetSoundCover(sound_id));
        }
        public bool IsSoundPlaying(int sound_id)
        {
            AudioChannel chan = GetChannel(config.GetSoundChannel(sound_id));
            return chan.IsPlaying(sound_id);
        }
        public void StopChannelWithout(int sound_id)
        {
            AudioChannel chan = GetChannel(config.GetSoundChannel(sound_id));
            chan.StopChannelWithout(sound_id);
        }
        public void Stop(int sound_id, bool all = false)
        {
            AudioChannel chan = GetChannel(config.GetSoundChannel(sound_id));
            chan.Stop(sound_id, all);
        }
        public void StopAllChannel()
        {
            foreach (var item in channels.Values)
            {
                item.StopChannel();
            }
        }

        public void StopChannel(int channel)
        {
            AudioChannel chan = GetChannel(channel);
            chan.StopChannel();
        }


        private AudioChannel GetChannel(int channel)
        {
            AudioChannel chan;
            if (!channels.TryGetValue(channel, out chan))
            {
                chan = new AudioChannel(this, channel);
                chan.SetVolume(GetPlayVolume(channel));
                channels.Add(channel, chan);
            }
            return chan;
        }
        private Queue<AudioAsset> release = new Queue<AudioAsset>();
        private void ReleaseAssets()
        {
            while (release.Count > 0)
            {
                var item = release.Dequeue();
                assets.Remove(item.path);
                item.ReleaseAsset();
                StaticPool.Set(item);
                this.helper.Release(item.path);
            }
        }
        private void AddToRelease(AudioAsset asset) => release.Enqueue(asset);
        internal AudioAsset Prepare(int sound_id)
        {
            string path = config.GetSoundPath(sound_id);
            AudioAsset asset;
            if (!assets.TryGetValue(path, out asset))
            {
                asset = StaticPool.Get<AudioAsset>();
                asset.InitAsset(this.helper, path);
                assets.Add(path, asset);
            }
            asset.Retain();
            return asset;
        }
        internal void ReleaseAsset(AudioAsset asset) => asset.Release();
    }
}
