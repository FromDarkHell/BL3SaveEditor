using System.Collections.Generic;

namespace BL3Tools.GVAS {
    public class GVASSave {
        public int sg { get; set; }
        public int pkg { get; set; }
        public short mj { get; set; }
        public short mn { get; set; }
        public short pa { get; set; }
        public uint eng { get; set; }
        public string build { get; set; }
        public int fmt { get; set; }
        public int fmtLength { get; set; }
        public Dictionary<byte[], int> fmtData { get; set; }
        public string sgType { get; set; }

        public GVASSave(int sg, int pkg, short mj, short mn, short pa, uint eng, string build, int fmt, int fmtlength, Dictionary<byte[], int> fmtData, string sgType) {
            this.sg = sg;
            this.pkg = pkg;
            this.mj = mj;
            this.mn = mn;
            this.pa = pa;
            this.eng = eng;
            this.build = build;
            this.fmt = fmt;
            this.fmtLength = fmtlength;
            this.fmtData = fmtData;
            this.sgType = sgType;
        }
    }
}
