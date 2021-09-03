using System.Net;

namespace BL3Tools.TMSUnpack {
    public static class TMSUnpacker {
        /// <summary>
        /// Downloads the given TMS from the gearbox servers to a byte array
        /// </summary>
        /// <param name="platform">Platform of the TMS file; Known Values: `pc`</param>
        /// <param name="marketplace">Marketplace of the TMS file; Known Values: `epic`/`steam`</param>
        /// <param name="type">"Branch" of the TMS file; Known values `prod`/`qa`</param>
        /// <returns>A <see cref="TMSArchive"/> representing the downloaded archive</returns>
        public static TMSArchive DownloadFromURL(string platform = "pc", string marketplace = "steam", string type = "prod") {
            return DownloadFromURL(string.Format("http://cdn.services.gearboxsoftware.com/sparktms/oak/{0}/{1}/OakTMS-{2}.cfg", platform, marketplace, type));
        }

        /// <summary>
        /// Downloads the given TMS from the given URL
        /// </summary>
        /// <returns>A <see cref="TMSArchive"/> representing the downloaded archive</returns>
        public static TMSArchive DownloadFromURL(string url) {
            var webClient = new WebClient();
            byte[] TMSData = webClient.DownloadData(url);

            return new TMSArchive(TMSData);
        }
    }
}
