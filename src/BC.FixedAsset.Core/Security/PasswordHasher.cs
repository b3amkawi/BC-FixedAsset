using System;
using System.Security.Cryptography;

namespace BC.FixedAsset.Core.Security
{
    public sealed class PasswordHashResult
    {
        public byte[] Hash { get; set; }
        public byte[] Salt { get; set; }
        public int Iterations { get; set; }
        public string Algorithm { get; set; }
    }

    public static class PasswordHasher
    {
        public const int DefaultIterations = 120000;
        private const int SaltSize = 32;
        private const int HashSize = 32;

        public static PasswordHashResult Hash(string password, int iterations = DefaultIterations)
        {
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password is required.", nameof(password));
            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            return new PasswordHashResult
            {
                Hash = Derive(password, salt, iterations),
                Salt = salt,
                Iterations = iterations,
                Algorithm = "PBKDF2-HMAC-SHA256"
            };
        }

        public static bool Verify(string password, byte[] expectedHash, byte[] salt, int iterations)
        {
            if (string.IsNullOrEmpty(password) || expectedHash == null || salt == null) return false;
            var actual = Derive(password, salt, iterations);
            return FixedTimeEquals(actual, expectedHash);
        }

        private static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using (var derive = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                return derive.GetBytes(HashSize);
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left.Length != right.Length) return false;
            var difference = 0;
            for (var i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }
    }
}
