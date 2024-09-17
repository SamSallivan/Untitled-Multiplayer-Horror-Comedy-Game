using Dissonance;
using Dissonance.Audio.Playback;
using UnityEngine;

public class PlayerVoicePlaybackObject : MonoBehaviour
{
	public AudioReverbFilter filter;

	public AudioSource voiceAudio;

	public VoicePlayback _playbackComponent;

	public DissonanceComms _dissonanceComms;

	public VoicePlayerState _playerState;

	public bool set2D;

	private void Awake()
	{
		_playbackComponent = GetComponent<VoicePlayback>();
		_dissonanceComms = Object.FindObjectOfType<DissonanceComms>();
		filter = GetComponent<AudioReverbFilter>();
		voiceAudio = GetComponent<AudioSource>();
	}

	private void OnEnable()
	{
		_playerState = _dissonanceComms.FindPlayer(_playbackComponent.PlayerName);
	}

	private void LateUpdate()
	{
		if (set2D)
		{
			voiceAudio.spatialBlend = 0f;
		}
		else
		{
			voiceAudio.spatialBlend = 1f;
		}
	}

	public void FindPlayerIfNull()
	{
		if (string.IsNullOrEmpty(_playbackComponent.PlayerName))
		{
			return;
		}
		
		_playerState = _dissonanceComms.FindPlayer(_playbackComponent.PlayerName);
	}
}
