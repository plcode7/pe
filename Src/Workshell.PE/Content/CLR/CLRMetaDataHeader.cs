﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Workshell.PE.Annotations;
using Workshell.PE.Extensions;

namespace Workshell.PE.Content
{
    public sealed class CLRMetaDataHeader : ISupportsLocation, ISupportsBytes
    {
        public const uint CLR_METADATA_SIGNATURE = 0x424A5342;

        private readonly PortableExecutableImage _image;

        internal CLRMetaDataHeader(PortableExecutableImage image, Location location)
        {
            _image = image;

            Location = location;
        }

        #region Static Methods

        public static async Task<CLRMetaDataHeader> LoadAsync(PortableExecutableImage image, Location mdLocation)
        {
            try
            {
                var stream = image.GetStream();
                var rva = mdLocation.RelativeVirtualAddress;
                var va = mdLocation.VirtualAddress;  
                var offset = mdLocation.FileOffset;
                var section = mdLocation.Section;

                stream.Seek(offset.ToInt64(), SeekOrigin.Begin);

                var signature = await stream.ReadUInt32Async().ConfigureAwait(false);

                if (signature != CLR_METADATA_SIGNATURE)
                    throw new PortableExecutableImageException(image, "Incorrect signature found in CLR meta-data header.");

                var majorVersion = await stream.ReadUInt16Async().ConfigureAwait(false);
                var minorVersion = await stream.ReadUInt16Async().ConfigureAwait(false);
                var reserved = await stream.ReadUInt32Async().ConfigureAwait(false);
                var versionLength = await stream.ReadInt32Async().ConfigureAwait(false);
                var version = await stream.ReadStringAsync(versionLength).ConfigureAwait(false);
                var flags = await stream.ReadUInt16Async().ConfigureAwait(false);
                var streams = await stream.ReadUInt16Async().ConfigureAwait(false);
                var size = sizeof(uint) + sizeof(ushort) + sizeof(ushort) + sizeof(uint) + sizeof(uint) + versionLength + sizeof(ushort) + sizeof(ushort);
                var location = new Location(offset, rva, va, size.ToUInt32(), size.ToUInt32(), section);
                var header = new CLRMetaDataHeader(image, location)
                {
                    Signature = signature,
                    MajorVersion = majorVersion,
                    MinorVersion = minorVersion,
                    Reserved = reserved,
                    VersionLength = versionLength,
                    Version = version,
                    Flags = flags,
                    Streams = streams
                };

                return header;
            }
            catch (Exception ex)
            {
                if (ex is PortableExecutableImageException)
                    throw;

                throw new PortableExecutableImageException(image, "Could not read CLR meta-data header from stream.", ex);
            }
        }

        #endregion

        #region Methods

        public byte[] GetBytes()
        {
            return GetBytesAsync().GetAwaiter().GetResult();
        }

        public async Task<byte[]> GetBytesAsync()
        {
            var stream = _image.GetStream();
            var buffer = await stream.ReadBytesAsync(Location).ConfigureAwait(false);

            return buffer;
        }

        #endregion

        #region Properties

        public Location Location { get; }

        [FieldAnnotation("Signature")]
        public uint Signature { get; private set; }

        [FieldAnnotation("Major Version")]
        public ushort MajorVersion { get; private set; }

        [FieldAnnotation("Minor Version")]
        public ushort MinorVersion { get; private set; }

        [FieldAnnotation("Reserved")]
        public uint Reserved { get; private set; }

        [FieldAnnotation("Version String Length")]
        public int VersionLength { get; private set; }

        [FieldAnnotation("Version String")]
        public string Version { get; private set; }

        [FieldAnnotation("Flags")]
        public ushort Flags { get; private set; }

        [FieldAnnotation("Number of Streams")]
        public ushort Streams { get; private set; }

        #endregion
    }
}
