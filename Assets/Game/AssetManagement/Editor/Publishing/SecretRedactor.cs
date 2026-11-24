using System;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Removes secret values from text before it reaches the Console or reports.</summary>
    public static class SecretRedactor
    {
        public const string Mask = "***";

        /// <summary>Replaces every occurrence of each non-empty secret with a mask. Null text yields an empty string.</summary>
        public static string Redact(string text, params string[] secretsArray)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            if (secretsArray == null)
            {
                return text;
            }
            foreach (var secret in secretsArray)
            {
                if (!string.IsNullOrEmpty(secret))
                {
                    text = text.Replace(secret, Mask);
                }
            }
            return text;
        }
    }
}
