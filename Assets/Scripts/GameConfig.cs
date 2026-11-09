using System;
using UnityEngine;

// Configuração lida de Assets/Resources/five_stages_config.json.
// functionsUrl = https://SEU_PROJECT_REF.supabase.co/functions/v1
// Vazio = jogo 100% offline (grava o relatório, mas não envia nem confere o código).
// Não vai nenhuma chave aqui: as funções do jogo não pedem login.
public static class GameConfig
{
    [Serializable]
    private class Data
    {
        public string functionsUrl = "";
    }

    private static Data data;

    public static string FunctionsUrl
    {
        get
        {
            if (data == null) Load();
            return data.functionsUrl;
        }
    }

    public static bool Online => !string.IsNullOrEmpty(FunctionsUrl);

    private static void Load()
    {
        data = new Data();
        TextAsset file = Resources.Load<TextAsset>("five_stages_config");
        if (file == null) return;

        try
        {
            data = JsonUtility.FromJson<Data>(file.text) ?? new Data();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[GameConfig] five_stages_config.json inválido: " + e.Message);
            data = new Data();
        }

        data.functionsUrl = (data.functionsUrl ?? "").Trim().TrimEnd('/');
    }
}
