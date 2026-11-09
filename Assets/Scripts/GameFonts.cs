using TMPro;
using UnityEngine;

// Fontes do jogo criadas a partir dos .ttf em Assets/Resources/Fonts (Alegreya Sans, licença OFL).
// O TextMeshPro gera o atlas na hora (fonte dinâmica), então não precisa do Font Asset Creator.
// Se o arquivo não carregar, devolve null e quem chamou mantém a fonte que já tinha.
public static class GameFonts
{
    private static TMP_FontAsset body;
    private static TMP_FontAsset bodyMedium;
    private static bool loaded;

    public static TMP_FontAsset Body { get { Load(); return body; } }
    public static TMP_FontAsset BodyMedium { get { Load(); return bodyMedium ?? body; } }

    private static void Load()
    {
        if (loaded) return;
        loaded = true;
        body = Create("Fonts/AlegreyaSans-Regular");
        bodyMedium = Create("Fonts/AlegreyaSans-Medium");
    }

    private static TMP_FontAsset Create(string resourcePath)
    {
        Font font = Resources.Load<Font>(resourcePath);
        if (font == null)
        {
            Debug.LogWarning("[GameFonts] Não achei " + resourcePath + ".ttf em Resources.");
            return null;
        }

        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font);
        if (asset == null) return null;

        // Se faltar algum caractere, cai na fonte padrão do TextMeshPro
        if (TMP_Settings.defaultFontAsset != null)
            asset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };
        return asset;
    }
}
