﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if !SIGNED
[assembly: InternalsVisibleTo("Workshell.PE.Resources")]
#else
[assembly: InternalsVisibleTo("Workshell.PE.Resources, PublicKey=0024000004800000940000000602000000240000525341310004000001000100259ed23116da6a496f873182c31284a428d040b37885524e9b53049cd99d5cc84feb00dbe77278afda8ebc9def14111b20b561f8d958e3f4aea2d492fed946245c528b16cad6ee785995ccfd7e6b7b34fe4be452a651069b2c0bbcf668bfb1dd9b99a7f30ab10d289525d61e82fd45e1ebcc11fc3d286e6096a1ee7edeee6091")]
#endif

[assembly: AssemblyTitle("Workshell.PE")]
[assembly: AssemblyDescription("A .NET class library for reading the PE executable format")]