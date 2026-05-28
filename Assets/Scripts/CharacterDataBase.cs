using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Five Stages/Sistema de Dialogo/Banco de Personagens")]
public class CharacterDatabase : ScriptableObject
{
    // A lista de personagens válidos no jogo (Baseado no Documento de Narrativa)
    public enum CharacterType
    {
        Noah,
        Emily,
        Claire,
        Atendente
    }

    [System.Serializable]
    public struct CharacterProfile
    {
        public CharacterType characterType;
        public string displayName; // O nome exato que vai aparecer na tela (ex: "Noah") 
        public Sprite defaultPortrait; // A foto padrão dele
    }

    [Header("Cadastro de Personagens do Jogo")]
    public CharacterProfile[] characters;

    // Função auxiliar para o Manager encontrar os dados rápido
    public CharacterProfile GetProfile(CharacterType type)
    {
        foreach (var profile in characters)
        {
            if (profile.characterType == type) return profile;
        }
        return default;
    }
}