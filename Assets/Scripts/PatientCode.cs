using System.Text;

// Código do paciente: XXXX-XXXX no alfabeto Crockford base32, último caractere = dígito verificador.
// MESMA regra de supabase/functions/_shared/codigo.ts (site e Edge Functions). Mudou lá, muda aqui.
// Verificador: soma de (valor do caractere × posição, começando em 1) mod 31.
// Teste de referência: "K7M29QX" → verificador 'P' (código K7M2-9QXP).
public static class PatientCode
{
    public const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"; // sem I, L, O, U

    public static char CheckDigit(string seven)
    {
        int sum = 0;
        for (int i = 0; i < seven.Length; i++)
            sum += Alphabet.IndexOf(seven[i]) * (i + 1);
        return Alphabet[sum % 31];
    }

    // Aceita minúsculas, espaços, hífen e as confusões comuns (O→0, I/L→1).
    // Devolve "XXXX-XXXX" ou null se o formato ou o verificador não baterem.
    public static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input)) return null;

        StringBuilder sb = new StringBuilder(8);
        foreach (char raw in input.ToUpperInvariant())
        {
            char c = raw;
            if (c == ' ' || c == '-') continue;
            if (c == 'O') c = '0';
            else if (c == 'I' || c == 'L') c = '1';
            if (Alphabet.IndexOf(c) < 0) return null;
            sb.Append(c);
        }

        if (sb.Length != 8) return null;
        string clean = sb.ToString();
        if (CheckDigit(clean.Substring(0, 7)) != clean[7]) return null;

        return clean.Substring(0, 4) + "-" + clean.Substring(4);
    }
}
