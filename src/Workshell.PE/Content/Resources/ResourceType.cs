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
using System.Text;
using System.Threading.Tasks;

namespace Workshell.PE.Resources
{
    public sealed class ResourceType : IEnumerable<Resource>
    {
        public const ushort Cursor = 1;
        public const ushort Bitmap = 2;
        public const ushort Icon = 3;
        public const ushort Menu = 4;
        public const ushort Dialog = 5;
        public const ushort String = 6;
        public const ushort FontDirectory = 7;
        public const ushort Font = 8;
        public const ushort Accelerator = 9;
        public const ushort RCData = 10;
        public const ushort MessageTable = 11;
        public const ushort GroupCursor = 12;
        public const ushort GroupIcon = 14;
        public const ushort Version = 16;
        public const ushort DialogInclude = 17;
        public const ushort PlugAndPlay = 19;
        public const ushort VxD = 20;
        public const ushort AnimatedCursor = 21;
        public const ushort AnimatedIcon = 22;
        public const ushort HTML = 23;
        public const ushort Manifest = 24;

        private static readonly Dictionary<ResourceId, Type> _types;

        private Resource[] _resources;

        static ResourceType()
        {
            _types = new Dictionary<ResourceId, Type>();
        }

        private ResourceType(ResourceCollection resources, ResourceDirectoryEntry entry, ResourceId id)
        {
            _resources = new Resource[0];

            Resources = resources;
            Entry = entry;
            Id = id;
            Count = 0;
        }

        #region Static Methods

        internal static async Task<ResourceType> CreateAsync(PortableExecutableImage image, ResourceCollection resources, ResourceDirectoryEntry entry)
        {
            ResourceId id;

            if (entry.NameType == NameType.ID)
            {
                id = entry.GetId();
            }
            else
            {
                id = await entry.GetNameAsync().ConfigureAwait(false);
            }

            var type = new ResourceType(resources, entry, id);

            await type.LoadResourcesAsync(image).ConfigureAwait(false);

            return type;
        }

        public static bool Register<T>(ResourceId typeId)
            where T : Resource
        {
            if (_types.ContainsKey(typeId))
                return false;

            _types.Add(typeId, typeof(T));

            return true;
        }

        public static bool Unregister(ResourceId typeId)
        {
            return _types.Remove(typeId);
        }

        private static Type Get(ResourceId typeId)
        {
            if (_types.ContainsKey(typeId))
                return _types[typeId];

            return null;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            if (!Id.IsNumeric)
                return Id.Name;

            switch (Id.Id)
            {
                case Cursor:
                    return "CURSOR";
                case Bitmap:
                    return "BITMAP";
                case Icon:
                    return "ICON";
                case Menu:
                    return "MENU";
                case Dialog:
                    return "DIALOG";
                case String:
                    return "STRING";
                case FontDirectory:
                    return "FONTDIR";
                case Font:
                    return "FONT";
                case Accelerator:
                    return "ACCELERATOR";
                case RCData:
                    return "RCDATA";
                case MessageTable:
                    return "MESSAGETABLE";
                case GroupCursor:
                    return "GROUP_CURSOR";
                case GroupIcon:
                    return "GROUP_ICON";
                case Version:
                    return "VERSION";
                case DialogInclude:
                    return "DLGINCLUDE";
                case PlugAndPlay:
                    return "PLUGPLAY";
                case VxD:
                    return "VXD";
                case AnimatedCursor:
                    return "ANICURSOR";
                case AnimatedIcon:
                    return "ANIICON";
                case HTML:
                    return "HTML";
                case Manifest:
                    return "MANIFEST";
                default:
                    return $"#{Id}";
            }
        }

        public IEnumerator<Resource> GetEnumerator()
        {
            foreach (var resource in _resources)
                yield return resource;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal async Task LoadResourcesAsync(PortableExecutableImage image)
        {
            var resources = new List<Resource>();
            var directory = Entry.GetDirectory();

            foreach(var entry in directory)
            {
                var type = Get(Id);
                Resource resource = null;

                if (type != null)
                {
                    resource = await Resource.CreateAsync(image, this, entry, type).ConfigureAwait(false);
                }
                else
                {
                    resource = await Resource.CreateAsync(image, this, entry, typeof(Resource)).ConfigureAwait(false);
                }

                resources.Add(resource);
            }

            _resources = resources.ToArray();
            Count = resources.Count;
        }

        #endregion

        #region Properties

        public ResourceCollection Resources;
        public ResourceDirectoryEntry Entry { get; }
        public ResourceId Id { get; }
        public int Count { get; private set; }
        public Resource this[int index] => _resources[index];

        #endregion
    }
}
