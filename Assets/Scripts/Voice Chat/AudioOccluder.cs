using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioOccluder : MonoBehaviour
{
	[Header("References")]
	private AudioLowPassFilter lowPassFilter;

	private AudioReverbFilter reverbFilter;

	private AudioSource audioSource;

	public LayerMask occluderMask;
	
	[Header("Values")]
	[SerializeField]
	private bool occluded;
	
	[SerializeField]
	private float updateOcclusionCooldown;
	
	[Header("Settings")]
	public float updateOcclusionCooldownSetting = 0.5f;

	public bool overridingLowPass;

	public float lowPassOverride = 20000f;

	private void Start()
	{
		lowPassFilter = GetComponent<AudioLowPassFilter>();
		lowPassFilter.cutoffFrequency = 20000f;
		
		reverbFilter = GetComponent<AudioReverbFilter>();
		reverbFilter.reverbPreset = AudioReverbPreset.User;
		reverbFilter.dryLevel = -1f;
		reverbFilter.decayTime = 0.8f;
		reverbFilter.room = -2300f;
		
		audioSource = GetComponent<AudioSource>();
	}

	private void Update()
	{
		if (audioSource.isVirtual)
		{
			return;
		}
		
		if (updateOcclusionCooldown > 0f)
		{
			updateOcclusionCooldown -= Time.deltaTime;
		}
		else
		{
			UpdateOcclussion();
			updateOcclusionCooldown = updateOcclusionCooldownSetting;
		}
		
		if (!overridingLowPass)
		{
			if (occluded)
			{
				lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, Mathf.Clamp(2500f / (Vector3.Distance(GameSessionManager.Instance.audioListener.transform.position, base.transform.position) / (audioSource.maxDistance / 2f)), 900f, 4000f), Time.deltaTime * 8f);
			}
			else
			{
				lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, 10000f, Time.deltaTime * 8f);
			}
		}
		else
		{
			lowPassFilter.cutoffFrequency = lowPassOverride;
		}
		
		if (GameSessionManager.Instance != null && GameSessionManager.Instance.localPlayerController != null)
		{
			// if (GameNetworkManager.Instance.localPlayerController.isInsideFactory || (GameNetworkManager.Instance.localPlayerController.isPlayerDead && GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null && GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript.isInsideFactory))
			// {
			// 	reverbFilter.dryLevel = Mathf.Lerp(reverbFilter.dryLevel, Mathf.Clamp(0f - 3.4f * (Vector3.Distance(GameSessionManager.Instance.audioListener.transform.position, base.transform.position) / (thisAudio.maxDistance / 5f)), -300f, -1f), Time.deltaTime * 8f);
			// 	reverbFilter.enabled = true;
			// }
			// else
			// {
			// 	reverbFilter.enabled = false;
			// }
		}
	}

	public void UpdateOcclussion()
	{
		if (Physics.Linecast(base.transform.position, GameSessionManager.Instance.audioListener.transform.position, out var _, occluderMask, QueryTriggerInteraction.Ignore))
		{
			occluded = true;
		}
		else
		{
			occluded = false;
		}
	}
}
