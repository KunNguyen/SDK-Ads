#if UNITY_IAP_ACTIVE
using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace JisSDKAds.IAP
{
    /// <summary>
    /// Lightweight XOR + HMAC wrapper around locally persisted IAP entitlement data.
    /// PlayerPrefs is plain text on disk (XML on most platforms, registry on Windows), so
    /// without this a player can hand-edit an entry and grant themselves RemoveAds / any
    /// non-consumable without ever buying it. This does not replace server-side receipt
    /// validation — a determined attacker can still extract the key from the compiled
    /// assembly — but it turns "edit a text file" into "reverse engineer the app", which is
    /// the same trade-off already accepted by Unity IAP's own tangle-obfuscated receipt
    /// validator used elsewhere in this SDK.
    /// </summary>
    internal static class IapLocalDataProtector
    {
        const string KeySalt = "jis-sdk-iap-entitlements-v1";

        public static string Protect(string plainText)
        {
            if (plainText == null)
                plainText = string.Empty;

            var key = DeriveKey();
            var cipherBytes = Xor(Encoding.UTF8.GetBytes(plainText), key);
            var mac = ComputeMac(cipherBytes, key);
            return Convert.ToBase64String(cipherBytes) + "." + Convert.ToBase64String(mac);
        }

        /// <summary>
        /// Attempts to decrypt+verify a payload previously produced by <see cref="Protect"/>.
        /// Returns false (and leaves <paramref name="plainText"/> null) if the payload is
        /// missing, malformed, or fails the integrity check — treat that as "no data" rather
        /// than trusting it.
        /// </summary>
        public static bool TryUnprotect(string payload, out string plainText)
        {
            plainText = null;
            if (string.IsNullOrEmpty(payload))
                return false;

            var separatorIndex = payload.IndexOf('.');
            if (separatorIndex <= 0 || separatorIndex == payload.Length - 1)
                return false;

            try
            {
                var cipherBytes = Convert.FromBase64String(payload.Substring(0, separatorIndex));
                var mac = Convert.FromBase64String(payload.Substring(separatorIndex + 1));
                var key = DeriveKey();
                var expectedMac = ComputeMac(cipherBytes, key);
                if (!FixedTimeEquals(mac, expectedMac))
                    return false;

                plainText = Encoding.UTF8.GetString(Xor(cipherBytes, key));
                return true;
            }
            catch
            {
                return false;
            }
        }

        static byte[] DeriveKey()
        {
            var seed = (Application.identifier ?? "jis-sdk") + KeySalt;
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
        }

        static byte[] Xor(byte[] data, byte[] key)
        {
            var result = new byte[data.Length];
            for (var i = 0; i < data.Length; i++)
                result[i] = (byte)(data[i] ^ key[i % key.Length]);
            return result;
        }

        static byte[] ComputeMac(byte[] data, byte[] key)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(data);
        }

        static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
#endif
