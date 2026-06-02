using UnityEngine;

public class HeartbeatZone : MonoBehaviour
{
    [Header("Referências")]
    public Transform    player;
    public AudioSource  heartbeatAudio;

    [Header("Configurações de Distância")]
    public float startDistance = 6f;
    public float minDistance   = 1f;

    [Header("Pitch e Volume")]
    public float pitchMin  = 0.6f;
    public float pitchMax  = 1.8f;
    public float volumeMin = 0f;
    public float volumeMax = 1f;

    bool playerInCorridor = false;

    void Start()
    {
        if (heartbeatAudio == null)
            heartbeatAudio = GetComponent<AudioSource>();

        heartbeatAudio.loop        = true;
        heartbeatAudio.playOnAwake = false;
        heartbeatAudio.volume      = 0f;
        heartbeatAudio.pitch       = pitchMin;
        heartbeatAudio.Play();

        if (player         == null) Debug.LogError("Player não atribuído!");
        if (heartbeatAudio == null) Debug.LogError("AudioSource não encontrado!");
    }

    void Update()
    {
            Debug.Log("playerInCorridor: " + playerInCorridor + 
              " | volume: " + heartbeatAudio.volume);
              
        if (player == null || heartbeatAudio == null) return;

        if (!playerInCorridor)
        {
            heartbeatAudio.volume = Mathf.Lerp(
                heartbeatAudio.volume, 0f, Time.deltaTime * 3f
            );
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= startDistance)
        {
            float t = 1f - Mathf.InverseLerp(minDistance, startDistance, distance);
            heartbeatAudio.volume = Mathf.Lerp(volumeMin, volumeMax, t);
            heartbeatAudio.pitch  = Mathf.Lerp(pitchMin,  pitchMax,  t);
        }
        else
        {
            heartbeatAudio.volume = Mathf.Lerp(
                heartbeatAudio.volume, 0f, Time.deltaTime * 2f
            );
        }
    }

    // O Collider Trigger está no PRÓPRIO HeartbeatZone agora
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Enter: " + other.name);
        if (other.CompareTag("Player"))
        {
            playerInCorridor = true;
            Debug.Log("Player entrou no corredor!");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInCorridor = false;
            Debug.Log("Player saiu do corredor!");
        }
    }
}