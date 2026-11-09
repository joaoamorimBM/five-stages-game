using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Envia os relatórios da pasta Relatorios pra Edge Function submit-report.
// - Deu certo: o arquivo vai pra Relatorios/Enviados (menos o da sessão em andamento,
//   que continua sendo reenviado a cada troca de cena).
// - Servidor recusou (código inexistente, formato errado...): vai pra Relatorios/Recusados.
// - Sem internet ou servidor fora do ar: o arquivo fica e é tentado de novo depois.
// Roda em coroutine pra nunca travar o jogo.
public static class ReportUploader
{
    private static bool running;
    private static bool runAgain;

    [Serializable]
    private class Header
    {
        public string patientCode;
    }

    public static void UploadPending(MonoBehaviour host)
    {
        if (!GameConfig.Online || host == null) return;
        if (running)
        {
            runAgain = true; // termina a rodada atual e passa de novo
            return;
        }
        host.StartCoroutine(Run());
    }

    private static IEnumerator Run()
    {
        running = true;

        do
        {
            runAgain = false;
            string dir = ReportLogger.ReportsFolder;
            if (!Directory.Exists(dir)) break;

            bool offline = false;
            foreach (string path in Directory.GetFiles(dir, "*.json"))
            {
                string json;
                try { json = File.ReadAllText(path); }
                catch { continue; } // arquivo sendo gravado agora: pega na próxima

                Header header = null;
                try { header = JsonUtility.FromJson<Header>(json); }
                catch { /* trata abaixo */ }

                if (header == null)
                {
                    Move(path, "Recusados");
                    continue;
                }

                // Teste no editor sem código: fica só no computador
                if (string.IsNullOrEmpty(header.patientCode)) continue;

                using (UnityWebRequest req = new UnityWebRequest(GameConfig.FunctionsUrl + "/submit-report", "POST"))
                {
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.timeout = 15;

                    yield return req.SendWebRequest();

                    long status = req.responseCode;
                    if (req.result == UnityWebRequest.Result.Success && status == 200)
                    {
                        bool active = ReportLogger.Instance != null && Path.GetFileName(path) == ReportLogger.Instance.ActiveFileName;
                        if (!active) Move(path, "Enviados");
                    }
                    else if (status >= 400 && status < 500)
                    {
                        Debug.LogWarning("[ReportUploader] Relatório recusado (" + status + "): " + req.downloadHandler.text);
                        Move(path, "Recusados");
                    }
                    else
                    {
                        offline = true; // sem internet ou servidor fora: para e tenta depois
                    }
                }

                if (offline) break;
            }

            if (offline) break;
        } while (runAgain);

        running = false;
    }

    // Move o .json e o .txt da mesma sessão pra uma subpasta
    private static void Move(string jsonPath, string subfolder)
    {
        try
        {
            string dir = Path.Combine(Path.GetDirectoryName(jsonPath), subfolder);
            Directory.CreateDirectory(dir);

            foreach (string ext in new[] { ".json", ".txt" })
            {
                string from = Path.ChangeExtension(jsonPath, ext);
                if (!File.Exists(from)) continue;
                string to = Path.Combine(dir, Path.GetFileName(from));
                if (File.Exists(to)) File.Delete(to);
                File.Move(from, to);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ReportUploader] Não deu pra mover " + jsonPath + ": " + e.Message);
        }
    }
}
