﻿using Biohazrd.OutputGeneration;
using System.IO;

namespace Biohazrd.CSharp
{
    [ProvidesOutputSessionFactory]
    public partial class CSharpCodeWriter : CLikeCodeWriter
    {
        protected CSharpCodeWriter(OutputSession session, string filePath)
            : base(session, filePath)
        { }

        private static OutputSession.WriterFactory<CSharpCodeWriter> FactoryMethod => (session, filePath) => new CSharpCodeWriter(session, filePath);

        public LeftAdjustedScope DisableScope(bool disabled, string? message)
        {
            if (!disabled)
            { return default; }

            EnsureSeparation();

            LeftAdjustedScope ret;

            if (message is null)
            { ret = CreateLeftAdjustedScope("#if false", "#endif"); }
            else
            { ret = CreateLeftAdjustedScope($"#if false // {message}", "#endif"); }

            NoSeparationNeededBeforeNextLine();
            return ret;
        }

        public LeftAdjustedScope DisableScope(string? message = null)
            => DisableScope(true, message);

        public void WriteIdentifier(string identifier)
            => Write(SanitizeIdentifier(identifier));

        protected override void WriteBetweenHeaderAndCode(StreamWriter writer)
        {
            foreach (string usingNamespace in UsingNamespaces)
            { writer.WriteLine($"using {usingNamespace};"); }

            if (UsingNamespaces.Count > 0)
            { writer.WriteLine(); }
        }

        protected override void WriteOutHeaderComment(StreamWriter writer)
        {
            // Roslyn has special understanding of the <auto-generated> comment to indicate the file should not be auto-formatted or receive code analysis warnings
            // Unfortunately another part of this special understanding is that it automatically disables nullability even if it's enabled project-wide
            // Biohazrd does not generally generate anything that needs nullable annotations, but custom declarations might, so we enable it by default
            // See https://github.com/InfectedLibraries/Biohazrd/issues/149
            if (OutputSession.GeneratedFileHeaderLines.Length == 0)
            {
                writer.WriteLine("// <auto-generated />");
                base.WriteOutHeaderComment(writer); // Sanity
            }
            else
            {
                writer.WriteLine("// <auto-generated>");
                base.WriteOutHeaderComment(writer);
                writer.WriteLine("// </auto-generated>");
            }
            writer.WriteLine("#nullable enable");
        }
    }
}
