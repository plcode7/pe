﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Workshell.PE.Native;

namespace Workshell.PE
{

    internal class ExportContentProvider : ISectionContentProvider
    {

        #region Methods

        public SectionContent Create(DataDirectory directory, Section section)
        {
            return new ExportContent(directory,section);
        }

        #endregion

        #region Properties

        public DataDirectoryType DirectoryType
        {
            get
            {
                return DataDirectoryType.ExportTable;
            }
        }

        #endregion

    }

    public class Export
    {

        internal Export(ExportContent exportContent, int index, int nameIndex, uint entryPoint, string name, int ord, string forwardName)
        {
            Content = exportContent;
            Index = index;
            NameIndex = nameIndex;
            EntryPoint = entryPoint;
            Name = name;
            Ordinal = ord;
            ForwardName = forwardName;
        }

        #region Methods

        public override string ToString()
        {
            if (String.IsNullOrWhiteSpace(ForwardName))
            {
                return String.Format("0x{0:X8} {1:D4} {2}",EntryPoint,Ordinal,Name);
            }
            else
            {
                return String.Format("0x{0:X8} {1:D4} {2} -> {3}",EntryPoint,Ordinal,Name,ForwardName);
            }
        }

        #endregion

        #region Properties

        public ExportContent Content
        {
            get;
            private set;
        }

        public int Index
        {
            get;
            private set;
        }

        public int NameIndex
        {
            get;
            private set;
        }

        public uint EntryPoint
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public int Ordinal
        {
            get;
            private set;
        }

        public string ForwardName
        {
            get;
            private set;
        }

        #endregion

    }

    public class ExportContent : SectionContent, ILocationSupport, IEnumerable<Export>
    {

        private StreamLocation location;
        private List<Export> exports;
        private ExportDirectory directory;
        private ExportTable<uint> address_table;
        private ExportTable<uint> name_pointer_table;
        private ExportTable<ushort> ordinal_table;
        private GenericLocationSupport name_table;

        internal ExportContent(DataDirectory dataDirectory, Section section) : base(dataDirectory,section)
        {
            long offset = Convert.ToInt64(section.RVAToOffset(dataDirectory.VirtualAddress));

            location = new StreamLocation(offset,dataDirectory.Size);

            Stream stream = Section.Sections.Reader.Stream;

            exports = new List<Export>();

            LoadDirectory(stream);
            LoadAddressTable(stream);
            LoadNamePointerTable(stream);
            LoadOrdinalTable(stream);
            LoadNameTable(stream);
            LoadExports(stream);
        }

        #region Methods

        public IEnumerator<Export> GetEnumerator()
        {
            return exports.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public byte[] GetBytes()
        {
            Stream stream = Section.Sections.Reader.Stream;
            byte[] buffer = new byte[location.Size];
            
            stream.Seek(location.Offset,SeekOrigin.Begin);
            stream.Read(buffer,0,buffer.Length);

            return buffer;
        }

        private void LoadDirectory(Stream stream)
        {
            long offset = Convert.ToInt64(Section.RVAToOffset(DataDirectory.VirtualAddress));

            stream.Seek(offset,SeekOrigin.Begin);

            IMAGE_EXPORT_DIRECTORY export_dir = Utils.Read<IMAGE_EXPORT_DIRECTORY>(stream,ExportDirectory.Size);
            StreamLocation location = new StreamLocation(offset,ExportDirectory.Size);

            directory = new ExportDirectory(this,export_dir,location);
        }

        private void LoadAddressTable(Stream stream)
        {
            List<uint> addresses = new List<uint>();
            long offset = Convert.ToInt64(Section.RVAToOffset(directory.AddressOfFunctions));
            
            stream.Seek(offset,SeekOrigin.Begin);

            for(var i = 0; i < directory.NumberOfFunctions; i++)
            {
                uint address = Utils.ReadUInt32(stream);

                addresses.Add(address);
            }

            address_table = new ExportTable<uint>(this,offset,addresses.Count * sizeof(uint),addresses);
        }

        private void LoadNamePointerTable(Stream stream)
        {
            List<uint> addresses = new List<uint>();
            long offset = Convert.ToInt64(Section.RVAToOffset(directory.AddressOfNames));
            
            stream.Seek(offset,SeekOrigin.Begin);

            for(var i = 0; i < directory.NumberOfNames; i++)
            {
                uint address = Utils.ReadUInt32(stream);

                addresses.Add(address);
            }

            name_pointer_table = new ExportTable<uint>(this,offset,addresses.Count * sizeof(uint),addresses);
        }

        private void LoadOrdinalTable(Stream stream)
        {
            List<ushort> ordinals = new List<ushort>();
            long offset = Convert.ToInt64(Section.RVAToOffset(directory.AddressOfNameOrdinals));
            
            stream.Seek(offset,SeekOrigin.Begin);

            for(var i = 0; i < directory.NumberOfNames; i++)
            {
                ushort ord = Utils.ReadUInt16(stream);

                ordinals.Add(ord);
            }

            ordinal_table = new ExportTable<ushort>(this,offset,ordinals.Count * sizeof(ushort),ordinals);
        }

        private void LoadNameTable(Stream stream)
        {
            long offset = Convert.ToInt64(Section.RVAToOffset(directory.Name));
            long size = (Directory.Location.Offset + DataDirectory.Size) - offset;
            
            name_table = new GenericLocationSupport(offset,size,this);
        }

        private void LoadExports(Stream stream)
        {
            uint[] function_addresses = directory.GetFunctionAddresses();
            uint[] name_addresses = directory.GetFunctionNameAddresses();
            ushort[] ordinals = directory.GetFunctionOrdinals();
            Dictionary<int,uint> function_addresses_dict = new Dictionary<int,uint>();

            for(int i = 0; i < function_addresses.Length; i++)
            {
                uint address = function_addresses[i];

                function_addresses_dict.Add(i,address);
            }

            Dictionary<int,Tuple<ushort,uint>> name_addresses_dict = new Dictionary<int,Tuple<ushort,uint>>();

            for(int i = 0; i < directory.NumberOfNames; i++)
            {
                ushort ord = ordinals[i];
                uint function_address = function_addresses[ord];

                name_addresses_dict.Add(i,new Tuple<ushort,uint>(ord,function_address));
            }

            Dictionary<int,uint> ord_addresses_dict = new Dictionary<int,uint>();

            for(int i = 0; i < function_addresses.Length; i++)
            {
                uint address = function_addresses[i];
                bool exists = false;

                foreach(KeyValuePair<int,Tuple<ushort,uint>> kvp in name_addresses_dict)
                {
                    if (kvp.Value.Item2 == address)
                    {
                        exists = true;

                        break;
                    }
                }

                if (!exists)
                    ord_addresses_dict.Add(i,address);
            }

            foreach(KeyValuePair<int,Tuple<ushort,uint>> kvp in name_addresses_dict)
            {
                Tuple<ushort,uint> tuple = kvp.Value;
                uint function_address = tuple.Item2;
                int name_idx = kvp.Key;
                uint name_address = name_addresses[name_idx];
                long name_offset = Convert.ToInt64(Section.RVAToOffset(name_address));
                string name = GetString(stream,name_offset);
                string fwd_name = String.Empty;

                if (!String.IsNullOrWhiteSpace(name))
                {
                    if (function_address >= DataDirectory.VirtualAddress && function_address <= (DataDirectory.VirtualAddress + DataDirectory.Size))
                    {
                        long fwd_offset = Convert.ToInt64(Section.RVAToOffset(function_address));

                        fwd_name = GetString(stream,fwd_offset);
                    }
                }

                int idx = Convert.ToInt32(tuple.Item1);
                int ord = Convert.ToInt32(idx + directory.Base);
                Export export = new Export(this,idx,name_idx,function_address,name,ord,fwd_name);
                
                exports.Add(export);
            }

            foreach(KeyValuePair<int,uint> kvp in ord_addresses_dict)
            {
                int idx = kvp.Key;
                int ord = Convert.ToInt32(idx + directory.Base);
                uint address = kvp.Value;
                Export export = new Export(this,idx,-1,address,String.Empty,ord,String.Empty);
                
                exports.Add(export);
            }

            exports = exports.OrderBy(e => e.Ordinal).ToList();
        }

        private string GetString(Stream stream, long offset)
        {
            stream.Seek(offset,SeekOrigin.Begin);

            return Utils.ReadString(stream);
        }

        #endregion

        #region Properties

        public StreamLocation Location
        {
            get
            {
                return location;
            }
        }

        public ExportDirectory Directory
        {
            get
            {
                return directory;
            }
        }

        public ExportTable<uint> AddressTable
        {
            get
            {
                return address_table;
            }
        }

        public ExportTable<uint> NamePointerTable
        {
            get
            {
                return name_pointer_table;
            }
        }

        public ExportTable<ushort> OrdinalTable
        {
            get
            {
                return ordinal_table;
            }
        }

        public GenericLocationSupport NameTable
        {
            get
            {
                return name_table;
            }
        }

        public int Count
        {
            get
            {
                return exports.Count;
            }
        }

        public Export this[int index]
        {
            get
            {
                return exports[index];
            }
        }

        public Export this[string name]
        {
            get
            {
                Export result = exports.FirstOrDefault(e => String.Compare(name,e.Name,StringComparison.OrdinalIgnoreCase) == 0);

                return result;
            }
        }

        public Export this[string name, string forwardName]
        {
            get
            {
                Export result = exports.FirstOrDefault(e => String.Compare(name,e.Name,StringComparison.OrdinalIgnoreCase) == 0 && String.Compare(forwardName,e.ForwardName,StringComparison.OrdinalIgnoreCase) == 0);

                return result;
            }
        }

        #endregion

    }

}
