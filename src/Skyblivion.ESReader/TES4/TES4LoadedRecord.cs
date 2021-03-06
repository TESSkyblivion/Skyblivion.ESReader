using Skyblivion.ESReader.Extensions.IDictionaryExtensions;
using Skyblivion.ESReader.Extensions.StreamExtensions;
using Skyblivion.ESReader.PHP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Skyblivion.ESReader.TES4
{
    public class TES4LoadedRecord : ITES4Record
    {
        public const int RECORD_HEADER_SIZE = 20;
        private readonly TES4File placedFile;
        private readonly int formid;
        private Nullable<int> expandedFormid;
        private readonly int flags;
        private int size;
        public TES4RecordType RecordType { get; private set; }
        private readonly Dictionary<string, List<byte[]>> data = new Dictionary<string, List<byte[]>>();
        private readonly Dictionary<string, int> dataAsFormidCache = new Dictionary<string, int>();
        /*
        * TES4LoadedRecord constructor.
        */
        public TES4LoadedRecord(TES4File placedFile, TES4RecordType type, int formid, int size, int flags)
        {
            this.placedFile = placedFile;
            this.RecordType = type;
            this.formid = formid;
            this.size = size;
            this.flags = flags;
        }

        public List<byte[]> GetSubrecords(string type)
        {
            return this.data.GetWithFallback(type, () => new List<byte[]>());
        }

        public string[] GetSubrecordsStrings(string type)
        {
            return GetSubrecords(type).Select(r=>GetSubrecordString(r)).ToArray();
        }

        public byte[] GetSubrecord(string type)
        {
            List<byte[]> list;
            if (!this.data.TryGetValue(type, out list) || !this.data[type].Any()) { return null; }
            return list[0];
        }

        private static string GetSubrecordString(byte[] bytes)
        {
            return TES4File.ISO_8859_1.Value.GetString(bytes);
        }
        public string GetSubrecordString(string type)
        {
            return GetSubrecordString(GetSubrecord(type));
        }

        public string GetSubrecordTrim(string type)
        {
            byte[] subrecord = GetSubrecord(type);
            if (subrecord == null) { return null; }
            string subrecordString = GetSubrecordString(subrecord);
            string trimmed = subrecordString.Trim('\0').Trim();
            return trimmed;
        }

        public string GetSubrecordTrimLower(string type)
        {
            string subrecord = GetSubrecordTrim(type);
            if (subrecord == null) { return null; }
            return subrecord.ToLower();
        }

        public Nullable<int> GetSubrecordAsFormid(string type)
        {
            int value;
            if(this.dataAsFormidCache.TryGetValue(type, out value)) { return value; }
            byte[] subrecord = this.GetSubrecord(type);
            if (subrecord == null || subrecord.Length < 4) { return null; }
            value = this.placedFile.Expand(PHPFunction.UnpackV(subrecord.Take(4).ToArray()));
            this.dataAsFormidCache.Add(type, value);
            return value;
        }

        public int GetFormId()
        {
            if (this.expandedFormid == null)
            {
                this.expandedFormid = this.placedFile.Expand(this.formid);
            }

            return this.expandedFormid.Value;
        }

        public void Load(Stream file, TES4RecordLoadScheme scheme)
        {
            if (this.size == 0)
            {
                return;
            }

            byte[] fileData = file.Read(this.size);
            //Decompression
            if ((this.flags & 0x00040000) == 0x00040000)
            {
                //Skip the uncompressed data size
                this.size = PHPFunction.UnpackV(fileData.Take(4).ToArray());
                fileData = PHPFunction.GZUncompress(fileData.Skip(4).ToArray());
            }

            int i = 0;
            while (i < this.size)
            {
                string subrecordType = TES4File.ISO_8859_1.Value.GetString(fileData, i, 4);
                int subrecordSize = PHPFunction.UnpackV(fileData.Skip(i + 4).Take(2).ToArray());
                if (scheme.shouldLoad(subrecordType))
                {
                    byte[] subrecordData = fileData.Skip(i+6).Take(subrecordSize).ToArray();
                    this.data.AddNewListIfNotContainsKeyAndAddValueToList(subrecordType, subrecordData);
                }

                i += (subrecordSize + 6);
            }
        }
    }
}