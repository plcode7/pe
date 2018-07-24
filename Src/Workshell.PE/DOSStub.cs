﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Workshell.PE.Extensions;

namespace Workshell.PE
{
    public sealed class DOSStub : ISupportsLocation, ISupportsBytes
    {
        private readonly PortableExecutableImage _image;

        internal DOSStub(PortableExecutableImage image, ulong stubOffset, uint stubSize, ulong imageBase)
        {
            _image = image;

            Location = new Location(image.GetCalculator(), stubOffset, Convert.ToUInt32(stubOffset), imageBase + stubOffset, stubSize, stubSize);
        }

        #region Methods

        public override string ToString()
        {
            return "MS-DOS Stub";
        }

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

        #endregion
    }
}
