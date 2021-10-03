/* Copyright (c) 2019 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;

namespace BL3Tools.Decryption {

    internal static class ProfileBogoCrypt {

        public static Dictionary<Platform, Dictionary<string, byte[]>> EncryptionKeys = new Dictionary<Platform, Dictionary<string, byte[]>>() {
            { Platform.PC, new Dictionary<string, byte[]>() {
                    { "PrefixMagic", new byte[] {
                        0xD8, 0x04, 0xB9, 0x08, 0x5C, 0x4E, 0x2B, 0xC0,
                        0x61, 0x9F, 0x7C, 0x8D, 0x5D, 0x34, 0x00, 0x56,
                        0xE7, 0x7B, 0x4E, 0xC0, 0xA4, 0xD6, 0xA7, 0x01,
                        0x14, 0x15, 0xA9, 0x93, 0x1F, 0x27, 0x2C, 0x8F } },
                    { "XorMagic", new byte[] {
                        0xE8, 0xDC, 0x3A, 0x66, 0xF7, 0xEF, 0x85, 0xE0,
                        0xBD, 0x4A, 0xA9, 0x73, 0x57, 0x99, 0x30, 0x8C,
                        0x94, 0x63, 0x59, 0xA8, 0xC9, 0xAE, 0xD9, 0x58,
                        0x7D, 0x51, 0xB0, 0x1E, 0xBE, 0xD0, 0x77, 0x43 } },
            }},
            { Platform.PS4, new Dictionary<string, byte[]>() {
                    { "PrefixMagic", new byte[] {
                        0xAD, 0x1E, 0x60, 0x4E, 0x42, 0x9E, 0xA9, 0x33, 
                        0xB2, 0xF5, 0x01, 0xE1, 0x02, 0x4D, 0x08, 0x75,
                        0xB1, 0xAD, 0x1A, 0x3D, 0xA1, 0x03, 0x6B, 0x1A, 
                        0x17, 0xE6, 0xEC, 0x0F, 0x60, 0x8D, 0xB4, 0xF9 } },
                    { "XorMagic", new byte[] {
                        0xBA, 0x0E, 0x86, 0x1D, 0x58, 0xE1, 0x92, 0x21, 
                        0x30, 0xD6, 0xCB, 0xF0, 0xD0, 0x82, 0xD5, 0x58,
                        0x36, 0x12, 0xE1, 0xF6, 0x39, 0x44, 0x88, 0xEA, 
                        0x4E, 0xFB, 0x04, 0x74, 0x07, 0x95, 0x3A, 0xA2 } },
            }},
        };

        public static void Encrypt(byte[] buffer, int offset, int length, Platform platform = Platform.PC) {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (offset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + length > buffer.Length) throw new ArgumentOutOfRangeException(nameof(length));
            if (length == 0) return;

            byte[] _PrefixMagic = EncryptionKeys[platform]["PrefixMagic"];
            byte[] _XorMagic = EncryptionKeys[platform]["XorMagic"];

            for (int i = 0, o = offset; i < length; i++, o++) {
                byte b = i < 32 ? _PrefixMagic[i] : buffer[o - 32];
                b ^= _XorMagic[o % 32];
                buffer[o] ^= b;
            }
        }

        public static void Decrypt(byte[] buffer, int offset, int length, Platform platform = Platform.PC) {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (offset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + length > buffer.Length) throw new ArgumentOutOfRangeException(nameof(length));
            if (length == 0) return;

            byte[] _PrefixMagic = EncryptionKeys[platform]["PrefixMagic"];
            byte[] _XorMagic = EncryptionKeys[platform]["XorMagic"];

            for (int i = length - 1, o = offset + i; i >= 0; i--, o--) {
                byte b = i < 32 ? _PrefixMagic[i] : buffer[o - 32];
                b ^= _XorMagic[o % 32];
                buffer[o] ^= b;
            }
        }
    }
}
