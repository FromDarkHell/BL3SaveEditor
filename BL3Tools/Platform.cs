using System.ComponentModel;

namespace BL3Tools {

    /// <summary>
    /// Defines the platform that a given <see cref="BL3Save"/>/<see cref="BL3Profile"/> can have
    /// <para>Particularly used for encryption/decryption of the profile</para>
    /// </summary>
    public enum Platform {
        /// <summary>
        /// A PC save/profile
        /// </summary>
        [Description("PC")]
        PC = 0x01,

        /// <summary>
        /// A <b>decrypted</b> PS4 save.
        /// </summary>
        [Description("PS4")]
        PS4 = 0x02
    }
}
