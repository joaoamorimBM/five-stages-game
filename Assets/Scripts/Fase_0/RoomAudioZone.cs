using UnityEngine;

public class RoomAudioZone : MonoBehaviour
{
    [Header("Referências")]
    public AudioSource audioSource;

    [Header("Configurações")]
    public float fadeSpeed = 3f;
    public float targetVolume = 0.5f;

    bool playerInRoom = false;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.volume      = 0f;
        audioSource.loop        = true;
        audioSource.playOnAwake = false;
        audioSource.Play();
    }

    void Update()
    {
        if (audioSource == null) return;

        float target = playerInRoom ? targetVolume : 0f;
        audioSource.volume = Mathf.Lerp(
            audioSource.volume, target, Time.deltaTime * fadeSpeed
        );
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRoom = true;
            Debug.Log("Player entrou na sala — TV ligando!");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRoom = false;
            Debug.Log("Player saiu da sala — TV apagando!");
        }
    }
}