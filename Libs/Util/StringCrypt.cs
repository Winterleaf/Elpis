/*
 * Copyright 2012 - Adam Haile
 * http://adamhaile.net
 *
 * This file is part of Elpis.
 * Elpis is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation, either version 3 of the License, or 
 * (at your option) any later version.
 * 
 * Elpis is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License 
 * along with Elpis. If not, see http://www.gnu.org/licenses/.
*/

namespace Elpis.Util
{
    //from http://http://stackoverflow.com/questions/202011/encrypt-decrypt-string-in-net
    public class StringCrypt
    {
        private static readonly byte[] _salt = System.Text.Encoding.ASCII.GetBytes("j3adb345MYlj22");

        /// <summary>
        ///     Encrypt the given string using AES.  The string can be decrypted using
        ///     DecryptStringAES().  The sharedSecret parameters must match.
        /// </summary>
        /// <param name="plainText">The text to encrypt.</param>
        /// <param name="sharedSecret">A password used to generate a key for encryption.</param>
        public static string EncryptString(string plainText, string sharedSecret)
        {
            if (string.IsNullOrEmpty(plainText))
                return "";
            //throw new ArgumentNullException("plainText");
            if (string.IsNullOrEmpty(sharedSecret))
                throw new System.ArgumentNullException(nameof(sharedSecret));

            string outStr; // Encrypted string to return
            System.Security.Cryptography.RijndaelManaged aesAlg = null;
                // RijndaelManaged object used to encrypt the data.

            try
            {
                // generate the key from the shared secret and the salt
                System.Security.Cryptography.Rfc2898DeriveBytes key =
                    new System.Security.Cryptography.Rfc2898DeriveBytes(sharedSecret, _salt);

                // Create a RijndaelManaged object
                // with the specified key and IV.
                aesAlg = new System.Security.Cryptography.RijndaelManaged();
                aesAlg.Key = key.GetBytes(aesAlg.KeySize/8);
                aesAlg.IV = key.GetBytes(aesAlg.BlockSize/8);

                // Create a decrytor to perform the stream transform.
                System.Security.Cryptography.ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (System.IO.MemoryStream msEncrypt = new System.IO.MemoryStream())
                {
                    using (
                        System.Security.Cryptography.CryptoStream csEncrypt =
                            new System.Security.Cryptography.CryptoStream(msEncrypt, encryptor,
                                System.Security.Cryptography.CryptoStreamMode.Write))
                    {
                        using (System.IO.StreamWriter swEncrypt = new System.IO.StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                    }
                    outStr = System.Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
            finally
            {
                // Clear the RijndaelManaged object.
                aesAlg?.Clear();
            }

            // Return the encrypted bytes from the memory stream.
            return outStr;
        }

        /// <summary>
        ///     Decrypt the given string.  Assumes the string was encrypted using
        ///     EncryptStringAES(), using an identical sharedSecret.
        /// </summary>
        /// <param name="cipherText">The text to decrypt.</param>
        /// <param name="sharedSecret">A password used to generate a key for decryption.</param>
        public static string DecryptString(string cipherText, string sharedSecret)
        {
            if (string.IsNullOrEmpty(cipherText))
                return "";
            //throw new ArgumentNullException("cipherText");
            if (string.IsNullOrEmpty(sharedSecret))
                throw new System.ArgumentNullException(nameof(sharedSecret));

            // Declare the RijndaelManaged object
            // used to decrypt the data.
            System.Security.Cryptography.RijndaelManaged aesAlg = null;

            // Declare the string used to hold
            // the decrypted text.
            string plaintext;

            try
            {
                // generate the key from the shared secret and the salt
                System.Security.Cryptography.Rfc2898DeriveBytes key =
                    new System.Security.Cryptography.Rfc2898DeriveBytes(sharedSecret, _salt);

                // Create a RijndaelManaged object
                // with the specified key and IV.
                aesAlg = new System.Security.Cryptography.RijndaelManaged();
                aesAlg.Key = key.GetBytes(aesAlg.KeySize/8);
                aesAlg.IV = key.GetBytes(aesAlg.BlockSize/8);

                // Create a decrytor to perform the stream transform.
                System.Security.Cryptography.ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                // Create the streams used for decryption.                
                byte[] bytes = System.Convert.FromBase64String(cipherText);
                using (System.IO.MemoryStream msDecrypt = new System.IO.MemoryStream(bytes))
                {
                    using (
                        System.Security.Cryptography.CryptoStream csDecrypt =
                            new System.Security.Cryptography.CryptoStream(msDecrypt, decryptor,
                                System.Security.Cryptography.CryptoStreamMode.Read))
                    {
                        using (System.IO.StreamReader srDecrypt = new System.IO.StreamReader(csDecrypt))

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }
            finally
            {
                // Clear the RijndaelManaged object.
                aesAlg?.Clear();
            }

            return plaintext;
        }
    }
}