﻿#region License
//  Copyright(c) 2016, Workshell Ltd
//  All rights reserved.
//  
//  Redistribution and use in source and binary forms, with or without
//  modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Workshell Ltd nor the names of its contributors
//  may be used to endorse or promote products
//  derived from this software without specific prior written permission.
//  
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//  ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//  WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//  DISCLAIMED.IN NO EVENT SHALL WORKSHELL BE LIABLE FOR ANY
//  DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//  (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//  LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//  ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//  (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Workshell.PE.Annotations;
using Workshell.PE.Extensions;
using Workshell.PE.Native;
using Workshell.PE.Resources.Native;

namespace Workshell.PE.Resources
{

    [Flags]
    public enum AcceleratorFlags : ushort
    {
        [EnumAnnotation("FVIRTKEY")]
        VirtualKey = 0x0001,
        [EnumAnnotation("FNOINVERT")]
        NoInvert = 0x0002,
        [EnumAnnotation("FSHIFT")]
        Shift = 0x0004,
        [EnumAnnotation("FCONTROL")]
        Control = 0x0008,
        [EnumAnnotation("FALT")]
        Alt = 0x0010,
        End = 0x0080
    }

    public sealed class AcceleratorEntry
    {

        private ushort flags;
        private ushort key;
        private ushort id;

        internal AcceleratorEntry(ushort accelFlag, ushort accelKey, ushort accelId)
        {
            flags = accelFlag;
            key = accelKey;
            id = accelId;
        }

        #region Methods

        public override string ToString()
        {
            return String.Format("Id: {0}; Key: {1}; Flags: {2}", id, GetKey(), GetFlags());
        }

        public AcceleratorFlags GetFlags()
        {
            return (AcceleratorFlags)flags;
        }

        public Keys GetKey()
        {
            return (Keys)key;
        }

        #endregion

        #region Properties

        public ushort Flags
        {
            get
            {
                return flags;
            }
        }

        public ushort Key
        {
            get
            {
                return key;
            }
        }

        public ushort Id
        {
            get
            {
                return id;
            }
        }

        #endregion

    }

    public enum AcceleratorSaveFormat
    {
        Raw,
        Resource
    }

    public sealed class AcceleratorResource : IEnumerable<AcceleratorEntry>
    {

        private Resource resource;
        private uint language_id;
        private AcceleratorEntry[] accelerators;

        internal AcceleratorResource(Resource sourceResource, uint languageId, byte[] data)
        {
            resource = sourceResource;
            language_id = languageId;

            MemoryStream mem = resource.Type.Resources.Image.MemoryStreamProvider.GetStream(data);

            using (mem)
            {
                int size = Utils.SizeOf<ACCELTABLEENTRY>();
                long count = mem.Length / size;

                accelerators = new AcceleratorEntry[count];

                for (var i = 0; i < count; i++)
                {
                    ACCELTABLEENTRY table_entry = Utils.Read<ACCELTABLEENTRY>(mem, size);
                    AcceleratorEntry entry = new AcceleratorEntry(table_entry.fFlags, table_entry.wAnsi, table_entry.wId);

                    accelerators[i] = entry;
                }
            }
        }

        #region Static Methods

        public static AcceleratorResource Load(Resource resource)
        {
            return Load(resource, Resource.DEFAULT_LANGUAGE);
        }

        public static AcceleratorResource Load(Resource resource, uint language)
        {
            if (!resource.Languages.Contains(language))
                return null;

            if (resource.Type.Id != ResourceType.RT_ACCELERATOR)
                return null;

            byte[] data = resource.GetBytes(language);
            AcceleratorResource result = new AcceleratorResource(resource, language, data);

            return result;
        }

        #endregion

        #region Methods

        public IEnumerator<AcceleratorEntry> GetEnumerator()
        {
            for (var i = 0; i < accelerators.Length; i++)
                yield return accelerators[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Save(string fileName)
        {
            Save(fileName, AcceleratorSaveFormat.Raw);
        }

        public void Save(Stream stream)
        {
            Save(stream, AcceleratorSaveFormat.Raw);
        }

        public void Save(string fileName, AcceleratorSaveFormat format)
        {
            using (FileStream file = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Save(file, format);
                file.Flush();
            }
        }

        public void Save(Stream stream, AcceleratorSaveFormat format)
        {
            switch (format)
            {
                case AcceleratorSaveFormat.Raw:
                    SaveRaw(stream);
                    break;
                case AcceleratorSaveFormat.Resource:
                    SaveResource(stream);
                    break;
            }
        }

        private void SaveRaw(Stream stream)
        {
            byte[] data = resource.GetBytes(language_id);

            stream.Write(data, 0, data.Length);
        }

        private void SaveResource(Stream stream)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Properties

        public Resource Resource
        {
            get
            {
                return resource;
            }
        }

        public uint Language
        {
            get
            {
                return language_id;
            }
        }

        public int Count
        {
            get
            {
                return accelerators.Length;
            }
        }

        public AcceleratorEntry this[int index]
        {
            get
            {
                return accelerators[index];
            }
        }

        #endregion

    }

}