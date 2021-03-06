﻿#region License
//  Copyright(c) Workshell Ltd
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workshell.PE.Extensions;

namespace Workshell.PE.Resources.Graphics
{
    public enum CursorGroupSaveFormat
    {
        Raw,
        Cursor
    }

    public sealed class CursorGroup : IEnumerable<CursorGroupEntry>
    {
        private readonly CursorGroupResource _resource;
        private readonly CursorGroupEntry[] _entries;

        internal CursorGroup(CursorGroupResource resource, ResourceLanguage language, CursorGroupEntry[] entries)
        {
            _resource = resource;
            _entries = entries;

            Language = language;
            Count = _entries.Length;
        }

        #region Methods

        public void Save(string fileName)
        {
            Save(fileName, ResourceLanguage.English.UnitedStates);
        }

        public void Save(string fileName, ResourceLanguage language)
        {
            Save(fileName, language, CursorGroupSaveFormat.Cursor);
        }

        public void Save(string fileName, ResourceLanguage language, CursorGroupSaveFormat saveFormat)
        {
            SaveAsync(fileName, language, saveFormat).GetAwaiter().GetResult();
        }

        public async Task SaveAsync(string fileName)
        {
            await SaveAsync(fileName, ResourceLanguage.English.UnitedStates);
        }

        public async Task SaveAsync(string fileName, ResourceLanguage language)
        {
            await SaveAsync(fileName, language, CursorGroupSaveFormat.Cursor);
        }

        public async Task SaveAsync(string fileName, ResourceLanguage language, CursorGroupSaveFormat saveFormat)
        {
            using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await SaveAsync(file, language, saveFormat).ConfigureAwait(false);
                await file.FlushAsync().ConfigureAwait(false);
            }
        }

        public void Save(Stream stream)
        {
            Save(stream, ResourceLanguage.English.UnitedStates);
        }

        public void Save(Stream stream, ResourceLanguage language)
        {
            Save(stream, language, CursorGroupSaveFormat.Cursor);
        }

        public void Save(Stream stream, ResourceLanguage language, CursorGroupSaveFormat saveFormat)
        {
            SaveAsync(stream, language, saveFormat).GetAwaiter().GetResult();
        }

        public async Task SaveAsync(Stream stream)
        {
            await SaveAsync(stream, ResourceLanguage.English.UnitedStates).ConfigureAwait(false);
        }

        public async Task SaveAsync(Stream stream, ResourceLanguage language)
        {
            await SaveAsync(stream, language, CursorGroupSaveFormat.Cursor).ConfigureAwait(false);
        }

        public async Task SaveAsync(Stream stream, ResourceLanguage language, CursorGroupSaveFormat saveFormat)
        {
            if (saveFormat == CursorGroupSaveFormat.Raw)
            {
                var buffer = await _resource.GetBytesAsync(language).ConfigureAwait(false);

                await stream.WriteBytesAsync(buffer).ConfigureAwait(false);
            }
            else
            {
                var group = await _resource.GetGroupAsync(language).ConfigureAwait(false);
                var cursorsInfo = new List<Tuple<ushort, ushort, ushort, byte[]>>(group.Count);

                for (var i = 0; i < group.Count; i++)
                {
                    var entry = group[i];
                    var cursors = _resource.Type.Resources.First(type => type.Id == ResourceType.Cursor);
                    var resource = cursors.First(cur => cur.Id == entry.CursorId);
                    var cursorData = await resource.GetBytesAsync(language).ConfigureAwait(false);
                    ushort hotspotX = 0;
                    ushort hotspotY = 0;
                    var dib = new byte[0];

                    using (var mem = new MemoryStream(cursorData))
                    {
                        hotspotX = await mem.ReadUInt16Async().ConfigureAwait(false);
                        hotspotY = await mem.ReadUInt16Async().ConfigureAwait(false);

                        using (var dibMem = new MemoryStream())
                        {
                            mem.CopyTo(dibMem, 4096);

                            dib = dibMem.ToArray();
                        }
                    }

                    cursorsInfo.Add(new Tuple<ushort, ushort, ushort, byte[]>(entry.CursorId, hotspotX, hotspotY, dib));
                }

                var offsets = new uint[group.Count];
                var offset = (6 + (16 * offsets.Length)).ToUInt32();

                for (var i = 0; i < group.Count; i++)
                {
                    var info = cursorsInfo[i];

                    offsets[i] = offset;
                    offset += info.Item4.Length.ToUInt32();
                }

                await stream.WriteUInt16Async(0).ConfigureAwait(false);
                await stream.WriteUInt16Async(2).ConfigureAwait(false);
                await stream.WriteUInt16Async(group.Count.ToUInt16()).ConfigureAwait(false);

                for (var i = 0; i < group.Count; i++)
                {
                    var entry = group[i];
                    var info = cursorsInfo[i];

                    await stream.WriteByteAsync(Convert.ToByte(entry.Width)).ConfigureAwait(false);
                    await stream.WriteByteAsync(Convert.ToByte(entry.Height)).ConfigureAwait(false);
                    await stream.WriteByteAsync(Convert.ToByte(entry.BitCount)).ConfigureAwait(false);
                    await stream.WriteByteAsync(0).ConfigureAwait(false);
                    await stream.WriteUInt16Async(info.Item2).ConfigureAwait(false);
                    await stream.WriteUInt16Async(info.Item3).ConfigureAwait(false);
                    await stream.WriteInt32Async(info.Item4.Length).ConfigureAwait(false);
                    await stream.WriteUInt32Async(offsets[i]).ConfigureAwait(false);
                }

                for (var i = 0; i < group.Count; i++)
                {
                    var info = cursorsInfo[i];

                    await stream.WriteBytesAsync(info.Item4).ConfigureAwait(false);
                }
            }
        }

        public IEnumerator<CursorGroupEntry> GetEnumerator()
        {
            foreach(var entry in _entries)
                yield return entry;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Properties

        public ResourceLanguage Language { get; }
        public int Count { get; }
        public CursorGroupEntry this[int index] => _entries[index];

        #endregion
    }
}
