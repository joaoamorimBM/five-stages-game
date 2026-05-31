using UnityEngine;

public class HeartbeatZone : MonoBehaviour
{
    [Header("Referências")]
    public Transform player;
    public AudioSource heartbeatAudio;

    [Header("Configurações de Distância")]
    public float startDistance  = 6f;   // distância que começa a tocar
    public float minDistance    = 1f;   // distância mínima (na porta)

    [Header("Configurações de Pitch")]
    public float pitchMin = 0.6f;   // pitch longe (batida lenta)
    public float pitchMax = 1.8f;   // pitch perto (batida acelerada)

    [Header("Configurações de Volume")]
    public float volumeMin = 0f;    // volume longe
    public float volumeMax = 1f;    // volume perto

    void Start()
    {
        if (heartbeatAudio == null)
            heartbeatAudio = GetComponent<AudioSource>();

        heartbeatAudio.loop        = true;
        heartbeatAudio.playOnAwake = false;
        heartbeatAudio.volume      = 0f;
        heartbeatAudio.pitch       = pitchMin;
        heartbeatAudio.Play();

        if (player == null) Debug.LogError("Player não atribuído!");
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= startDistance)
        {
            // Calcula o progresso entre longe e perto (0 a 1)
            float t = 1f - Mathf.InverseLerp(minDistance, startDistance, distance);

            // Aplica volume e pitch proporcionais à distância
            heartbeatAudio.volume = Mathf.Lerp(volumeMin, volumeMax, t);
            heartbeatAudio.pitch  = Mathf.Lerp(pitchMin,  pitchMax,  t);
        }
        else
        {
            // Fora do range — silencia gradualmente
            heartbeatAudio.volume = Mathf.Lerp(
                heartbeatAudio.volume, 0f, Time.deltaTime * 2f
            );
        }
    }
}