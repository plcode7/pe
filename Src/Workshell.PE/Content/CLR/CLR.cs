﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Workshell.PE.Content
{
    public sealed class CLR : DataContent
    {
        internal CLR(PortableExecutableImage image, DataDirectory directory, Location location, CLRHeader header, CLRMetaData metaData) : base(image, directory, location)
        {
            Header = header;
            MetaData = metaData;
        }

        #region Static Methods

        internal static async Task<CLR> LoadAsync(PortableExecutableImage image)
        {
            if (!image.NTHeaders.DataDirectories.Exists(DataDirectoryType.CLRRuntimeHeader))
                return null;

            var dataDirectory = image.NTHeaders.DataDirectories[DataDirectoryType.CLRRuntimeHeader];

            if (DataDirectory.IsNullOrEmpty(dataDirectory))
                return null;

            var calc = image.GetCalculator();
            var section = calc.RVAToSection(dataDirectory.VirtualAddress);
            var fileOffset = calc.RVAToOffset(section, dataDirectory.VirtualAddress);
            var imageBase = image.NTHeaders.OptionalHeader.ImageBase;         
            var location = new Location(fileOffset, dataDirectory.VirtualAddress, imageBase + dataDirectory.VirtualAddress, dataDirectory.Size, dataDirectory.Size, section);
            var header = await CLRHeader.LoadAsync(image, location).ConfigureAwait(false);
            var metaData = await CLRMetaData.LoadAsync(image, header).ConfigureAwait(false);

            return new CLR(image, dataDirectory, location, header, metaData);
        }

        #endregion

        #region Properties

        public CLRHeader Header { get; }
        public CLRMetaData MetaData { get; }

        #endregion
    }
}
