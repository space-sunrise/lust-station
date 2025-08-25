using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Content.Server.Station.Systems;
using Content.Server._Sunrise.TTS;
using Content.Server.Power.Components;
using Content.Shared._Sunrise.AnnouncementSpeaker.Components;
using Content.Shared._Sunrise.AnnouncementSpeaker.Events;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Station.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.AnnouncementSpeaker;

/// <summary>
/// Represents a queued announcement waiting to be played.
/// </summary>
public sealed class QueuedAnnouncement
{
    public EntityUid Station { get; set; }
    public string Message { get; set; } = "";
    public ResolvedSoundSpecifier? AnnouncementSound { get; set; }
    public string? AnnounceVoice { get; set; }
    public byte[]? TtsData { get; set; }
    public TimeSpan QueuedAt { get; set; }

    public QueuedAnnouncement(EntityUid station, string message, ResolvedSoundSpecifier? announcementSound, string? announceVoice, byte[]? ttsData)
    {
        Station = station;
        Message = message;
        AnnouncementSound = announcementSound;
        AnnounceVoice = announceVoice;
        TtsData = ttsData;
    }
}

/// <summary>
/// System that manages announcement speakers distributed across stations.
/// Replaces global announcements with spatial audio from speaker networks.
/// </summary>
public sealed class AnnouncementSpeakerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private bool _isEnabled;
    private string _defaultAnnounceVoice = "Hanson";

    // Queue system for preventing overlapping announcements
    private readonly Queue<QueuedAnnouncement> _announcementQueue = new();
    private bool _isPlayingAnnouncement = false;
    private TimeSpan _currentAnnouncementEndTime;
    private const float AnnouncementDurationEstimate = 5.0f; // Estimate 5 seconds per announcement

    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(SunriseCCVars.TTSEnabled, v => _isEnabled = v, true);
        SubscribeLocalEvent<AnnouncementSpeakerEvent>(OnAnnouncementSpeaker);
        // Note: SpeakerPlayAnnouncementEvent is handled by TTSSystem for the component
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        ProcessAnnouncementQueue();
    }

    /// <summary>
    /// Processes the announcement queue and plays the next announcement if none is currently playing.
    /// </summary>
    private void ProcessAnnouncementQueue()
    {
        // Check if current announcement has finished
        if (_isPlayingAnnouncement && _timing.CurTime >= _currentAnnouncementEndTime)
        {
            _isPlayingAnnouncement = false;
        }

        // If not playing and have queued announcements, play next one
        if (!_isPlayingAnnouncement && _announcementQueue.Count > 0)
        {
            var announcement = _announcementQueue.Dequeue();
            PlayAnnouncementNow(announcement);
        }
    }

    /// <summary>
    /// Immediately plays an announcement without queuing.
    /// </summary>
    private void PlayAnnouncementNow(QueuedAnnouncement announcement)
    {
        var duration = TimeSpan.FromSeconds(AnnouncementDurationEstimate);
        if (announcement.TtsData != null)
            duration = GetAudioDurationFromBytes(announcement.TtsData);
        _isPlayingAnnouncement = true;
        _currentAnnouncementEndTime = _timing.CurTime + duration;

        var ev = new AnnouncementSpeakerEvent(
            announcement.Station,
            announcement.Message,
            announcement.AnnouncementSound,
            announcement.AnnounceVoice,
            announcement.TtsData
        );

        RaiseLocalEvent(ref ev);
    }

    /// <summary>
    /// Handles station-wide announcements by finding all speakers on the station and playing the announcement through them.
    /// </summary>
    private void OnAnnouncementSpeaker(ref AnnouncementSpeakerEvent ev)
    {
        // Find all speakers on the station
        var speakers = GetStationSpeakers(ev.Station);

        if (speakers.Count == 0)
        {
            // Fallback: If no speakers are found, log a warning
            // In the future, this could send to a single communications console or similar
            Logger.Warning($"No announcement speakers found on station {ToPrettyString(ev.Station)}. Announcement not played: {ev.Message}");
            return;
        }

        // Play announcement sound via PVS for each speaker on server side
        if (ev.AnnouncementSound != null)
        {
            foreach (var speaker in speakers)
            {
                if (!TryComp<AnnouncementSpeakerComponent>(speaker, out var speakerComp))
                    continue;

                // Check if speaker is enabled and has power
                if (!speakerComp.Enabled)
                    continue;

                if (speakerComp.RequiresPower)
                {
                    if (!TryComp<ApcPowerReceiverComponent>(speaker, out var powerReceiver) || !powerReceiver.Powered)
                        continue;
                }

                // Play announcement sound via PVS from this speaker
                var audioParams = AudioParams.Default.WithVolume(-2f * speakerComp.VolumeModifier).WithMaxDistance(speakerComp.Range);
                _audioSystem.PlayPvs(ev.AnnouncementSound, speaker, audioParams);
            }
        }

        // Передаём TTS сразу всем динамикам
        var speakerEvent = new SpeakerPlayAnnouncementEvent(ev.Message, ev.AnnouncementSound, ev.AnnounceVoice, ev.TtsData);
        foreach (var speaker in speakers)
        {
            RaiseLocalEvent(speaker, ref speakerEvent);
        }
    }

    /// <summary>
    /// Gets all functional announcement speakers on a station.
    /// </summary>
    private List<EntityUid> GetStationSpeakers(EntityUid station)
    {
        var speakers = new List<EntityUid>();

        if (!TryComp<StationDataComponent>(station, out var stationData))
            return speakers;

        // Look through all grids on the station for speakers
        foreach (var grid in stationData.Grids)
        {
            var query = EntityQueryEnumerator<AnnouncementSpeakerComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var speakerComp, out var xform))
            {
                // Check if the speaker is on this grid
                if (xform.GridUid == grid)
                {
                    speakers.Add(uid);
                }
            }
        }

        return speakers;
    }

    /// <summary>
    /// Dispatches an announcement to all speakers on a station.
    /// This is the main entry point for the announcement speaker system.
    /// Uses the queue system to prevent overlapping announcements.
    /// </summary>
    public async void DispatchAnnouncementToSpeakers(EntityUid station, string message, SoundSpecifier? announcementSound = null, string? announceVoice = null)
    {
        var resolvedSound = announcementSound != null ? _audioSystem.ResolveSound(announcementSound) : null;
        if (!_isEnabled)
            return;
        if (!GetVoicePrototype(announceVoice ?? _defaultAnnounceVoice, out var protoVoice))
            return;
        var generatedTts = await GenerateTtsForAnnouncement(message, protoVoice);

        var queuedAnnouncement = new QueuedAnnouncement(station, message, resolvedSound, announceVoice, generatedTts)
        {
            QueuedAt = _timing.CurTime
        };

        if (!_isPlayingAnnouncement)
        {
            // If no announcement is playing, play immediately
            PlayAnnouncementNow(queuedAnnouncement);
        }
        else
        {
            // Queue the announcement to play after current one finishes
            _announcementQueue.Enqueue(queuedAnnouncement);
        }
    }

    /// <summary>
    /// Dispatches an announcement to speakers on all stations.
    /// Used for server-wide announcements like round start/end.
    /// Uses the queue system to prevent overlapping announcements.
    /// </summary>
    public async void DispatchAnnouncementToAllStations(string message, SoundSpecifier? announcementSound = null, string? announceVoice = null)
    {
        var resolvedSound = announcementSound != null ? _audioSystem.ResolveSound(announcementSound) : null;
        if (!_isEnabled)
            return;
        if (!GetVoicePrototype(announceVoice ?? _defaultAnnounceVoice, out var protoVoice))
            return;
        var generatedTts = await GenerateTtsForAnnouncement(message, protoVoice);

        var stationQuery = EntityQueryEnumerator<StationDataComponent>();
        while (stationQuery.MoveNext(out var stationUid, out var stationData))
        {
            var queuedAnnouncement = new QueuedAnnouncement(stationUid, message, resolvedSound, announceVoice, generatedTts)
            {
                QueuedAt = _timing.CurTime
            };

            if (!_isPlayingAnnouncement)
            {
                // If no announcement is playing, play immediately
                PlayAnnouncementNow(queuedAnnouncement);
            }
            else
            {
                // Queue the announcement to play after current one finishes
                _announcementQueue.Enqueue(queuedAnnouncement);
            }
        }
    }

    /// <summary>
    /// Gets a voice prototype by ID, with fallback to default voice.
    /// </summary>
    private bool GetVoicePrototype(string voiceId, [NotNullWhen(true)] out TTSVoicePrototype? voicePrototype)
    {
        if (!_prototypeManager.TryIndex(voiceId, out voicePrototype))
        {
            return _prototypeManager.TryIndex("father_grigori", out voicePrototype);
        }
        return true;
    }

    /// <summary>
    /// Generates TTS audio for an announcement with the megaphone effect.
    /// </summary>
    private async Task<byte[]?> GenerateTtsForAnnouncement(string text, TTSVoicePrototype voicePrototype)
    {
        try
        {
            var textSanitized = Sanitize(text);
            if (textSanitized == "") return null;
            if (char.IsLetter(textSanitized[^1]))
                textSanitized += ".";

            // Use TTS manager directly to generate with megaphone effect
            var ttsManager = IoCManager.Resolve<TTSManager>();
            return await ttsManager.ConvertTextToSpeechAnnounce(voicePrototype, textSanitized);
        }
        catch (Exception e)
        {
            Logger.Error($"TTS System error in announcement generation: {e.Message}");
        }
        return null;
    }

    /// <summary>
    /// Sanitizes text for TTS generation.
    /// </summary>
    private string Sanitize(string text)
    {
        return text.Trim();
    }

    /// <summary>
    /// Checks if a player has any working announcement speakers within range.
    /// Used to determine if they should receive announcement messages in chat.
    /// </summary>
    public bool HasWorkingSpeakersNearby(EntityUid playerEntity)
    {
        if (!TryComp<TransformComponent>(playerEntity, out var playerTransform))
            return false;

        var playerPos = playerTransform.Coordinates;

        // Find all speakers and check if any are in range and working
        var query = EntityQueryEnumerator<AnnouncementSpeakerComponent, TransformComponent>();
        while (query.MoveNext(out var speakerUid, out var speakerComp, out var speakerTransform))
        {
            // Check if speaker is enabled
            if (!speakerComp.Enabled)
                continue;

            // Check if speaker has power (if required)
            if (speakerComp.RequiresPower)
            {
                if (!TryComp<ApcPowerReceiverComponent>(speakerUid, out var powerReceiver) || !powerReceiver.Powered)
                    continue;
            }

            // Check if player is within range of this speaker
            if (playerPos.TryDistance(EntityManager, speakerTransform.Coordinates, out var distance) &&
                distance <= speakerComp.Range)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets all speakers on all stations that are working.
    /// Used to determine if any announcements can be played at all.
    /// </summary>
    public bool HasAnyWorkingSpeakers()
    {
        var query = EntityQueryEnumerator<AnnouncementSpeakerComponent>();
        while (query.MoveNext(out var speakerUid, out var speakerComp))
        {
            // Check if speaker is enabled
            if (!speakerComp.Enabled)
                continue;

            // Check if speaker has power (if required)
            if (speakerComp.RequiresPower)
            {
                if (!TryComp<ApcPowerReceiverComponent>(speakerUid, out var powerReceiver) || !powerReceiver.Powered)
                    continue;
            }

            return true; // Found at least one working speaker
        }

        return false;
    }

    private static TimeSpan GetWavDurationFromBytes(byte[] wavData)
    {
        // WAV header is 44 bytes for PCM
        if (wavData.Length < 44)
            return TimeSpan.Zero;
        int sampleRate = BitConverter.ToInt32(wavData, 24);
        short channels = BitConverter.ToInt16(wavData, 22);
        short bitsPerSample = BitConverter.ToInt16(wavData, 34);
        int dataSize = BitConverter.ToInt32(wavData, 40);
        int bytesPerSample = bitsPerSample / 8;
        int totalSamples = dataSize / (bytesPerSample * channels);
        if (sampleRate <= 0 || channels <= 0 || bitsPerSample <= 0)
            return TimeSpan.Zero;
        double durationSeconds = (double)totalSamples / sampleRate;
        return TimeSpan.FromSeconds(durationSeconds);
    }

    private static TimeSpan GetAudioDurationFromBytes(byte[] audioData, string? fileType = null)
    {
        if (fileType == "wav" || (audioData.Length > 12 && audioData[0] == 'R' && audioData[1] == 'I' && audioData[2] == 'F' && audioData[8] == 'W' && audioData[9] == 'A' && audioData[10] == 'V'))
        {
            return GetWavDurationFromBytes(audioData);
        }
        if (fileType == "ogg" || (audioData.Length > 4 && audioData[0] == 'O' && audioData[1] == 'g' && audioData[2] == 'g' && audioData[3] == 'S'))
        {
            try
            {
                var nvorbisType = Type.GetType("NVorbis.VorbisReader, NVorbis");
                if (nvorbisType != null)
                {
                    using var stream = new MemoryStream(audioData);
                    using var reader = (IDisposable)Activator.CreateInstance(nvorbisType, stream, false)!;
                    var totalTimeProp = nvorbisType.GetProperty("TotalTime");
                    if (totalTimeProp != null)
                    {
                        var totalTime = totalTimeProp.GetValue(reader);
                        if (totalTime is TimeSpan ts)
                            return ts;
                    }
                }
            }
            catch
            {
                // NVorbis не доступен или ошибка — fallback
            }
            double fallbackBitrate = 128000.0; // 128 кбит/с
            double duration = audioData.Length * 8.0 / fallbackBitrate;
            return TimeSpan.FromSeconds(duration);
        }
        return TimeSpan.Zero;
    }
}
