using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BecoSequenceManager : MonoBehaviour
{
    [Header("Referências de Personagem e Câmera")]
    [SerializeField] private Transform noahTransform;
    [SerializeField] private Camera noahCamera; 
    [SerializeField] private Transform[] caminhoCaminhadaNoah;
    [SerializeField] private Transform pontoInteracaoGato; 
    
    [Header("Referências do Gato e Lata")]
    [SerializeField] private GameObject gatoPlaceholder;
    [SerializeField] private CatBecoCutscene gatoScript;
    [SerializeField] private Rigidbody lataLixoRb;

    [Header("Dados de Diálogo")]
    [SerializeField] private DialogueData dialogoGato;          
    [SerializeField] private DialogueData memoriaClaireDialogue; 
    [SerializeField] private DialogueData noahReacaoDialogue;   

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
            
            // 1. Gira o CORPO do Noah para ficar de frente para a lata (eixo Y)
            Vector3 direcaoLataHorizontal = (lataLixoRb.transform.position - noahTransform.position).normalized;
            direcaoLataHorizontal.y = 0; // Evita inclinar o corpo para cima ou para baixo
            
            if (direcaoLataHorizontal != Vector3.zero)
            {
                noahTransform.rotation = Quaternion.Slerp(noahTransform.rotation, Quaternion.LookRotation(direcaoLataHorizontal), 5f * Time.deltaTime);
            }

            // 2. A câmera faz apenas o ajuste vertical de olhar um pouco para baixo em direção ao objeto
            Vector3 direcaoCameraLata = (lataLixoRb.transform.position - noahCamera.transform.position).normalized;
            if (direcaoCameraLata != Vector3.zero)
            {
                noahCamera.transform.rotation = Quaternion.Slerp(noahCamera.transform.rotation, Quaternion.LookRotation(direcaoCameraLata), 5f * Time.deltaTime);
            }
            yield return null;
        }

        // Noah grita "QUE MERDA!" com o peito e olhos cravados na lata (ombro sumiu!)
        _dialogoEmAndamento = true;
        DialogueManager.Instance.StartDialogue(noahReacaoDialogue);
        yield return new WaitUntil(() => !_dialogoEmAndamento);

// --- PARTE 6: O CHUTE FÍSICO (LATA E TAMPA) ---
        if (lataLixoRb != null)
        {
            if (sfxSource != null && somChuteLata != null)
                sfxSource.PlayOneShot(somChuteLata);

            // 1. Descongela a física da tampa para ela poder voar separadamente
            if (tampaLixoRb != null)
            {
                tampaLixoRb.isKinematic = false; // Libera a gravidade e colisões dela
                tampaLixoRb.transform.SetParent(null); // Desvincula da lata para não herdar movimentos travados
                
                // Dá um impulso leve para cima e para frente na tampa para fazê-la saltar longe da lata
                Vector3 direcaoSaltoTampa = (lataLixoRb.transform.position - noahTransform.position).normalized + (Vector3.up * 1.5f);
                tampaLixoRb.AddForce(direcaoSaltoTampa * (forcaChute * 0.7f), ForceMode.Impulse);
            }

            // 2. Chuta o corpo da lata de lixo
            Vector3 direcaoChuteLata = (lataLixoRb.transform.position - noahTransform.position).normalized + (Vector3.up * 0.3f);
            lataLixoRb.AddForce(direcaoChuteLata * forcaChute, ForceMode.Impulse);
        }

        // Segura a câmera estática por 0.5 segundos assistindo o caos físico
        yield return new WaitForSeconds(0.5f);

        // --- SOLUÇÃO DO TRANCO: SINCRONIZAÇÃO FINAL DO CORPO E CÂMERA NA LATA ---
        if (playerMovement != null)
        {
            // 1. Descobre para onde a câmera ficou apontada olhando para a lata
            Vector3 direcaoOlharFinal = noahCamera.transform.forward;
            direcaoOlharFinal.y = 0; // foca a rotação do corpo apenas no plano horizontal
            
            if (direcaoOlharFinal != Vector3.zero)
            {
                // Gira o corpo do Noah para a direção da lixeira de forma definitiva
                noahTransform.rotation = Quaternion.LookRotation(direcaoOlharFinal);
            }

            // 2. Calcula o ângulo vertical (Pitch) atual da câmera para não dar tranco para cima/baixo
            float anguloXAtual = noahCamera.transform.localEulerAngles.x;
            if (anguloXAtual > 180) anguloXAtual -= 360;

            // 3. Injeta as posições exatas na sua função do PlayerMovement para atualizar a variável rotX
            playerMovement.ForceRotation(noahTransform.eulerAngles.y, anguloXAtual);

            // 4. DEVOLVE O CONTROLE DE MOVIMENTO: Agora o jogador é liberado olhando para a lixeira!
            playerMovement.SetMovementLocked(false);
        }

        Debug.Log("Sequência do beco concluída com sucesso e controles liberados na direção da lata!");
    }
}