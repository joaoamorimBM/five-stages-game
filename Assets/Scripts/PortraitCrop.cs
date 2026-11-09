using System.Collections.Generic;
using UnityEngine;

// Enquadramento de busto dos retratos, igual pra todos os personagens.
// Os PNGs originais têm muito espaço vazio e enquadramentos diferentes; aqui só
// recortamos um pedaço (cabeça e ombros) em tempo de execução, sem mexer nos arquivos.
// Recortes calculados pelo contorno de cada imagem (normalizados, origem embaixo à esquerda).
// Retrato novo sem entrada aqui aparece inteiro.
public static class PortraitCrop
{
    private static readonly Dictionary<string, Rect> Busts = new Dictionary<string, Rect>
    {
        { "Noah_sprite", new Rect(0.3362f, 0.4147f, 0.3192f, 0.5853f) },
        { "Emily_sprite", new Rect(0.3481f, 0.4058f, 0.3188f, 0.5718f) },
        { "Claire_sprite", new Rect(0.3730f, 0.4605f, 0.2586f, 0.4638f) },
        { "Atendente_sprite", new Rect(0.3498f, 0.3977f, 0.3134f, 0.5605f) },
        { "emily_acidente", new Rect(0.3321f, 0.3950f, 0.3373f, 0.6050f) },
        { "Noah_acidente", new Rect(0.3490f, 0.4129f, 0.3244f, 0.5818f) },
        { "pai_de_familia", new Rect(0.3216f, 0.3950f, 0.3373f, 0.6050f) },
        { "crianca_padaria", new Rect(0.3447f, 0.3999f, 0.3346f, 0.6001f) },
        { "Noa_raiva_sprite", new Rect(0.3599f, 0.4116f, 0.3281f, 0.5884f) },
        { "Noah_chorando", new Rect(0.3294f, 0.3800f, 0.3457f, 0.6200f) },
    };

    private static readonly Dictionary<Sprite, Sprite> Cache = new Dictionary<Sprite, Sprite>();

    public static Sprite Bust(Sprite original)
    {
        if (original == null) return null;
        if (Cache.TryGetValue(original, out Sprite cached)) return cached;

        Sprite result = original;
        if (Busts.TryGetValue(original.name, out Rect n))
        {
            Rect r = original.rect;
            Rect crop = new Rect(r.x + n.x * r.width, r.y + n.y * r.height, n.width * r.width, n.height * r.height);
            result = Sprite.Create(original.texture, crop, new Vector2(0.5f, 0.5f), original.pixelsPerUnit);
            result.name = original.name + "_busto";
        }

        Cache[original] = result;
        return result;
    }
}
