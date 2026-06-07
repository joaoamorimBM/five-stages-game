using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BecoSequenceManager : MonoBehaviour
{
    [Header("Referências de Personagem e Câmera")]
    [SerializeField] private Transform noahTransform;
    [SerializeField] private Camera noahCamera; 
    [SerializeField] private Transform[] caminhoCaminhadaNoah;
    [SerializeField] private Transform[] caminhoAteFinalBeco;
    [SerializeField] private Transform pontoInteracaoGato; 
    
    [Header("Referências do Gato e Lata")]
    [SerializeField] private GameObject gatoPlaceholder;
    [SerializeField] private CatBecoCutscene gatoScript;
    [SerializeField] private Rigidbody lataLixoRb;

    [Header("Dados de Diálogo")]
    [SerializeField] private DialogueData dialogoGato;          
    [SerializeField] private DialogueData memoriaClaireDialogue; 
    [SerializeField] private DialogueData noahReacaoDialogue;
    [SerializeField] private DialogueData dialogoAntesFlashback;
    [SerializeField] private DialogueData dialogoPosFlashback;   

    [Header("Configurações Físicas e Áudio")]
    [SerializeField] private float velocidadeCaminhadaNoah = 2f;
    [SerializeField] private float forcaChute = 18f;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip somChuteLata;

    [Header("Física da Tampa")]
    [SerializeField] private Rigidbody tampaLixoRb;

    // --- ADICIONE AS REFERÊNCIAS DOS SEUS SCRIPTS DE CONTROLE AQUI ---
    [Header("Scripts do Player para Desativar")]
    [SerializeField] private MonoBehaviour scriptMovimentacaoPlayer; // Arraste o script de andar do Noah
    [SerializeField] private MonoBehaviour scriptRotacaoCamera;      // Arraste o script de girar a câmera/mouse (se houver)

    [Header("Parte 2: Emily e Flashback")]
    [SerializeField] private GameObject emilyPrefab;
    [SerializeField] private Transform pontoOlharEmily;

    private bool _dialogoEmAndamento = false;

    private void OnEnable() => DialogueManager.OnDialogueEnded += NotificarFimDialogo;
    private void OnDisable() => DialogueManager.OnDialogueEnded -= NotificarFimDialogo;

    private void Start()
    {
        StartCoroutine(SequenciaBecoRoutine());
    }

    private void NotificarFimDialogo()
    {
        _dialogoEmAndamento = false;
    }

private IEnumerator SequenciaBecoRoutine()
    {
        // --- PARTE 1: TRAVA CONTROLES USANDO O SEU SCRIPT NATIVO ---
        PlayerMovement playerMovement = noahTransform.GetComponent<PlayerMovement>();
        CharacterController playerCC = noahTransform.GetComponent<CharacterController>();

        if (playerMovement != null)
        {
            // Usa a sua função que pausa animações e desativa o script de forma limpa!
            playerMovement.SetMovementLocked(true); 
        }

        Quaternion rotacaoOriginalCamera = noahCamera.transform.localRotation;

        // Percorre cada curva do beco de forma natural
        for (int i = 0; i < caminhoCaminhadaNoah.Length; i++)
        {
            Transform pontoAtual = caminhoCaminhadaNoah[i];
            if (pontoAtual == null) continue;

            while (Vector3.Distance(noahTransform.position, pontoAtual.position) > 0.3f)
            {
                // 1. Calcula a direção para onde ele deve andar (física)
                Vector3 direcaoPonto = (pontoAtual.position - noahTransform.position).normalized;
                
                if (playerCC != null)
                {
                    playerCC.Move(direcaoPonto * velocidadeCaminhadaNoah * Time.deltaTime);
                }
                else
                {
                    noahTransform.position = Vector3.MoveTowards(noahTransform.position, pontoAtual.position, velocidadeCaminhadaNoah * Time.deltaTime);
                }
                
                // 2. CORREÇÃO DO OMBRO: Força o CORPO do Noah a ficar sempre de frente para o gato (apenas rotação Y)
                Vector3 direcaoGatoHorizontal = (gatoPlaceholder.transform.position - noahTransform.position).normalized;
                direcaoGatoHorizontal.y = 0; // Mantém o corpo reto, sem inclinar para cima/baixo
                
                if (direcaoGatoHorizontal != Vector3.zero)
                {
                    noahTransform.rotation = Quaternion.Slerp(noahTransform.rotation, Quaternion.LookRotation(direcaoGatoHorizontal), 8f * Time.deltaTime);
                }

                // 3. FOCO DA CÂMERA: A câmera (filha) cuida apenas do ajuste fino de altura/olhar
                Vector3 direcaoCameraGato = (gatoPlaceholder.transform.position - noahCamera.transform.position).normalized;
                if (direcaoCameraGato != Vector3.zero)
                {
                    noahCamera.transform.rotation = Quaternion.LookRotation(direcaoCameraGato);
                }

                yield return null;
            }
        }

        // --- PARTE 2: DIÁLOGO COM O GATO ---
        _dialogoEmAndamento = true;
        DialogueManager.Instance.StartDialogue(dialogoGato);
        yield return new WaitUntil(() => !_dialogoEmAndamento); 

        // --- PARTE 3: GATO FUGIND0 E CÂMERA SEGUINDO ---
        if (gatoScript != null)
        {
            gatoScript.StartCatEscape(); 
        }

        while (gatoPlaceholder.activeSelf)
        {
            Vector3 direcaoOlharGato = (gatoPlaceholder.transform.position - noahCamera.transform.position).normalized;
            if (direcaoOlharGato != Vector3.zero)
            {
                // Foco absoluto e cravado no gato enquanto ele corre para o buraco
                noahCamera.transform.rotation = Quaternion.LookRotation(direcaoOlharGato);
            }
            yield return null;
        }

        // Retorna a câmera suavemente para a frente do Noah
        float tempoManeio = 0f;
        while (tempoManeio < 1f)
        {
            tempoManeio += Time.deltaTime * 3f;
            noahCamera.transform.localRotation = Quaternion.Slerp(noahCamera.transform.localRotation, Quaternion.identity, tempoManeio);
            yield return null;
        }

        // Sincroniza a variável interna do seu PlayerMovement para o mouse não dar tranco no final da cutscene
        if (playerMovement != null)
        {
            // Atualiza o rotX interno com base na rotação atual que a cutscene deixou a câmera
            float anguloXAtual = noahCamera.transform.localEulerAngles.x;
            // Converte ângulos de 0-360 para o padrão -180 a 180 que o Mathf.Clamp usa
            if (anguloXAtual > 180) anguloXAtual -= 360;
            
            playerMovement.ForceRotation(noahTransform.eulerAngles.y, anguloXAtual);
        }

        // --- PARTE 4: PROMPT DA PULSEIRA ---
        Debug.Log("Aperte E para olhar a pulseira...");
        bool apertouE = false;
        while (!apertouE)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                apertouE = true;
            }
            yield return null;
        }

        // Mostra a memória visual da Claire sorrindo na direita
        _dialogoEmAndamento = true;
        DialogueManager.Instance.StartDialogue(memoriaClaireDialogue);
        yield return new WaitUntil(() => !_dialogoEmAndamento);

// --- PARTE 5: ENCARAR A LATA COM O CORPO INTEIRO (CORREÇÃO DO OMBRO) ---
        float tempoOlharLata = 0f;
        while (tempoOlharLata < 0.8f)
        {
            tempoOlharLata += Time.deltaTime;
            
            Vector3 direcaoLataHorizontal = (lataLixoRb.transform.position - noahTransform.position).normalized;
            direcaoLataHorizontal.y = 0;
            if (direcaoLataHorizontal != Vector3.zero)
            {
                noahTransform.rotation = Quaternion.Slerp(noahTransform.rotation, Quaternion.LookRotation(direcaoLataHorizontal), 5f * Time.deltaTime);
            }

            Vector3 direcaoCameraLata = (lataLixoRb.transform.position - noahCamera.transform.position).normalized;
            if (direcaoCameraLata != Vector3.zero)
            {
                noahCamera.transform.rotation = Quaternion.Slerp(noahCamera.transform.rotation, Quaternion.LookRotation(direcaoCameraLata), 5f * Time.deltaTime);
            }
            yield return null;
        }

        // Noah grita "QUE MERDA!"
        _dialogoEmAndamento = true;
        DialogueManager.Instance.StartDialogue(noahReacaoDialogue);
        yield return new WaitUntil(() => !_dialogoEmAndamento);

// --- PARTE 6: O CHUTE FÍSICO (LATA E TAMPA) ---
        if (lataLixoRb != null)
        {
            if (sfxSource != null && somChuteLata != null)
                sfxSource.PlayOneShot(somChuteLata);

            if (tampaLixoRb != null)
            {
                tampaLixoRb.isKinematic = false;
                tampaLixoRb.transform.SetParent(null);
                Vector3 direcaoSaltoTampa = (lataLixoRb.transform.position - noahTransform.position).normalized + (Vector3.up * 1.5f);
                tampaLixoRb.AddForce(direcaoSaltoTampa * (forcaChute * 0.7f), ForceMode.Impulse);
            }

            Vector3 direcaoChuteLata = (lataLixoRb.transform.position - noahTransform.position).normalized + (Vector3.up * 0.3f);
            lataLixoRb.AddForce(direcaoChuteLata * forcaChute, ForceMode.Impulse);
        }

        // Segura a câmera na lata voando por 1.5 segundos (pausa dramática da fúria)
        yield return new WaitForSeconds(1.5f); 

        // --- PARTE 6.5: NOAH CAMINHA ATÉ O FINAL DO BECO SOZINHO ---
        Debug.Log("Noah começando a caminhar sozinho até o final do beco...");

        for (int i = 0; i < caminhoAteFinalBeco.Length; i++)
        {
            Transform pontoAtual = caminhoAteFinalBeco[i];
            if (pontoAtual == null) continue;

            while (Vector3.Distance(noahTransform.position, pontoAtual.position) > 0.3f)
            {
                Vector3 direcaoPonto = (pontoAtual.position - noahTransform.position).normalized;
                
                if (playerCC != null)
                {
                    playerCC.Move(direcaoPonto * velocidadeCaminhadaNoah * Time.deltaTime);
                }
                else
                {
                    noahTransform.position = Vector3.MoveTowards(noahTransform.position, pontoAtual.position, velocidadeCaminhadaNoah * Time.deltaTime);
                }
                
                // O corpo e a cabeça/câmera focam na direção para onde ele está andando agora (olhando para frente normalmente)
                if (direcaoPonto != Vector3.zero)
                {
                    direcaoPonto.y = 0; 
                    noahTransform.rotation = Quaternion.Slerp(noahTransform.rotation, Quaternion.LookRotation(direcaoPonto), 6f * Time.deltaTime);
                    noahCamera.transform.rotation = Quaternion.Slerp(noahCamera.transform.rotation, Quaternion.LookRotation(direcaoPonto), 6f * Time.deltaTime);
                }

                yield return null;
            }
        }

        // --- PARTE 7: APARIÇÃO DA EMILY ATRÁS DO NOAH ---
        // Noah chegou ao final do beco e para. A Emily surge nas costas dele.
        if (emilyPrefab != null)
        {
            emilyPrefab.SetActive(true); // Ativa a Emily silenciosamente
        }

        yield return new WaitForSeconds(1.0f); // Tempo até ela chamar "Noah..."

        // --- PARTE 8: NOAH SE VIRA PARA A EMILY (CORPO E CÂMERA ALINHADOS) ---
        float tempoViradaEmily = 0f;
        while (tempoViradaEmily < 1.2f)
        {
            tempoViradaEmily += Time.deltaTime;
            
            Vector3 direcaoEmilyHorizontal = (emilyPrefab.transform.position - noahTransform.position).normalized;
            direcaoEmilyHorizontal.y = 0;
            if (direcaoEmilyHorizontal != Vector3.zero)
            {
                noahTransform.rotation = Quaternion.Slerp(noahTransform.rotation, Quaternion.LookRotation(direcaoEmilyHorizontal), 4f * Time.deltaTime);
            }

            if (pontoOlharEmily != null)
            {
                Vector3 direcaoCamEmily = (pontoOlharEmily.position - noahCamera.transform.position).normalized;
                noahCamera.transform.rotation = Quaternion.Slerp(noahCamera.transform.rotation, Quaternion.LookRotation(direcaoCamEmily), 4f * Time.deltaTime);
            }

            yield return null;
        }

        // --- PARTE 9: DIÁLOGO ANTES DO FLASHBACK ---
        _dialogoEmAndamento = true;
        DialogueManager.Instance.StartDialogue(dialogoAntesFlashback);
        yield return new WaitUntil(() => !_dialogoEmAndamento); // Espera ler até "Era pra ter sido eu..."

        // --- PARTE 10: ESPAÇO DA TRANSIÇÃO (PRO OUTRO MEMBRO DO GRUPO) ---
        Debug.Log("Fim do Bloco 1. Transição do Flashback liberada aqui!");
        DispararTransiciaoFlashback();
    }

    /// <summary>
    /// ESPAÇO DO GRUPO: O membro responsável pela transição vai colocar o código dele aqui dentro!
    /// Pode ser um SceneManager.LoadScene, ativação de animação de Fade, etc.
    /// </summary>
    private void DispararTransiciaoFlashback()
    {
        // EX: Seu amigo vai colocar algo como:
        // FadeManager.Instance.FadeOutToScene("Cena_Estrada");
    }

    /// <summary>
    /// MÉTODO PÚBLICO: Quando o flashback do acidente terminar lá na outra cena (ou cenário),
    /// o script daquela cena só precisa chamar esta função para o Beco continuar de onde parou.
    /// </summary>
    public void RetornarDoFlashback()
    {
        StartCoroutine(SequenciaPosFlashbackRoutine());
    }

    private IEnumerator SequenciaPosFlashbackRoutine()
    {
        Debug.Log("Noah voltou do flashback. Rodando o diálogo final de aceitação.");

        // --- PARTE 11: DIÁLOGO DE ENCERRAMENTO DO BECO ---
        _dialogoEmAndamento = true;
        DialogueManager.Instance.StartDialogue(dialogoPosFlashback);
        yield return new WaitUntil(() => !_dialogoEmAndamento); // Espera o "Vamos pra casa."

        // --- PARTE 12: DEVOLVE O CONTROLE DE MOVIMENTAÇÃO SE QUISER ---
        PlayerMovement playerMovement = noahTransform.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            // Sincroniza o mouse no ângulo atual da Emily para não dar tranco
            float anguloXAtual = noahCamera.transform.localEulerAngles.x;
            if (anguloXAtual > 180) anguloXAtual -= 360;
            playerMovement.ForceRotation(noahTransform.eulerAngles.y, anguloXAtual);

            playerMovement.SetMovementLocked(false); // Libera o jogador na cena
        }

        Debug.Log("Fase do Beco Finalizada com Sucesso!");
    }
}
