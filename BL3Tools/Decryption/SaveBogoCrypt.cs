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

namespace BL3Tools.Decryption {
    internal static class SaveBogoCrypt {
        private static readonly byte[] _PrefixMagic;
        private static readonly byte[] _XorMagic;

        static SaveBogoCrypt() {
            _PrefixMagic = new byte[]
            {
                0x71, 0x34, 0x36, 0xB3, 0x56, 0x63, 0x25, 0x5F,
                0xEA, 0xE2, 0x83, 0x73, 0xF4, 0x98, 0xB8, 0x18,
                0x2E, 0xE5, 0x42, 0x2E, 0x50, 0xA2, 0x0F, 0x49,
                0x87, 0x24, 0xE6, 0x65, 0x9A, 0xF0, 0x7C, 0xD7,
            };
            _XorMagic = new byte[]
            {
                0x7C, 0x07, 0x69, 0x83, 0x31, 0x7E, 0x0C, 0x82,
                0x5F, 0x2E, 0x36, 0x7F, 0x76, 0xB4, 0xA2, 0x71,
                0x38, 0x2B, 0x6E, 0x87, 0x39, 0x05, 0x02, 0xC6,
                0xCD, 0xD8, 0xB1, 0xCC, 0xA1, 0x33, 0xF9, 0xB6,
            };
        }

        public static void Encrypt(byte[] buffer, int offset, int length) {
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (length < 0) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (offset > buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (offset + length > buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (length == 0) {
                return;
            }

            for (int i = 0, o = offset; i < length; i++, o++) {
                byte b = i < 32 ? _PrefixMagic[i] : buffer[o - 32];
                b ^= _XorMagic[o % 32];
                buffer[o] ^= b;
            }
        }

        public static void Decrypt(byte[] buffer, int offset, int length) {
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (length < 0) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (offset > buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (offset + length > buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (length == 0) {
                return;
            }

            for (int i = length - 1, o = offset + i; i >= 0; i--, o--) {
                byte b = i < 32 ? _PrefixMagic[i] : buffer[o - 32];
                b ^= _XorMagic[o % 32];
                buffer[o] ^= b;
            }
        }
    }
}
