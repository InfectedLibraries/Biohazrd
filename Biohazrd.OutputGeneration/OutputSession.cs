﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Biohazrd.OutputGeneration
{
    public sealed class OutputSession : IDisposable
    {
        public bool AutoRenameConflictingFiles { get; set; } = true;

        private Dictionary<Type, Delegate> Factories = new();

        private Dictionary<string, object> Writers = new();

        private HashSet<string> _FilesWrittenMoreThanOnce = new();

        public IReadOnlyCollection<string> FilesWritten => Writers.Keys;
        public IReadOnlyCollection<string> FilesWrittenMoreThanOnce => _FilesWrittenMoreThanOnce;

        public string? GeneratedFileHeader { get; }
        private readonly string[] _GeneratedFileHeaderLines;
        public ReadOnlySpan<string> GeneratedFileHeaderLines => _GeneratedFileHeaderLines;
        public bool HasGeneratedFileHeader => GeneratedFileHeader is not null;

        public OutputSession()
            : this($"This file was automatically generated by {nameof(Biohazrd)} and should not be modified by hand!")
        { }

        public OutputSession(string? generatedFileHeader)
        {
            if (String.IsNullOrEmpty(generatedFileHeader))
            {
                GeneratedFileHeader = null;
                _GeneratedFileHeaderLines = Array.Empty<string>();
            }
            else
            {
                GeneratedFileHeader = generatedFileHeader.Replace("\r", "");
                _GeneratedFileHeaderLines = generatedFileHeader.Split('\n');
            }

            // Add default factories
            AddFactory((session, path) => new StreamWriter(path));
            AddFactory((session, path) => new FileStream(path, FileMode.Create));
        }

        public void WriteHeader(TextWriter writer, string linePrefix)
        {
            foreach (string line in GeneratedFileHeaderLines)
            { writer.WriteLine($"{linePrefix}{line}"); }
        }

        public delegate TWriter WriterFactory<TWriter>(OutputSession session, string filePath)
            where TWriter : class;

        public void AddFactory<TWriter>(WriterFactory<TWriter> factoryMethod)
            where TWriter : class
            => Factories.Add(typeof(TWriter), factoryMethod);

        private WriterFactory<TWriter> GetFactory<TWriter>()
            where TWriter : class
        {
            if (Factories.TryGetValue(typeof(TWriter), out Delegate? ret))
            { return (WriterFactory<TWriter>)ret; }

            // See if the type provides a factory method
            if (typeof(TWriter).GetCustomAttribute<ProvidesOutputSessionFactoryAttribute>() is not null)
            {
                const string factoryPropertyName = "FactoryMethod";
                PropertyInfo? factoryMethodProperty = typeof(TWriter).GetProperty
                (
                    factoryPropertyName,
                    BindingFlags.DoNotWrapExceptions | BindingFlags.Static | BindingFlags.NonPublic,
                    null,
                    typeof(WriterFactory<TWriter>),
                    Type.EmptyTypes,
                    Array.Empty<ParameterModifier>()
                );

                MethodInfo? factoryMethodGetter = factoryMethodProperty?.GetGetMethod();

                if (factoryMethodProperty is null || factoryMethodGetter is null)
                { throw new NotSupportedException($"{typeof(TWriter).FullName} is marked as providing a factory for {nameof(OutputSession)}, but it does not have a property matching the expected shape."); }

                WriterFactory<TWriter>? factory = (WriterFactory<TWriter>?)factoryMethodGetter.Invoke(null, null);

                if (factory is null)
                { throw new InvalidOperationException($"The {factoryPropertyName} property for {typeof(TWriter).FullName} returned null."); }

                Factories.Add(typeof(TWriter), factory);
                return factory;
            }

            // If there wasn't a value, throw
            throw new NotSupportedException($"This output session does not support creating {typeof(TWriter).FullName} instances.");
        }

        public TWriter Open<TWriter>(string filePath, WriterFactory<TWriter> factory)
            where TWriter : class
        {
            CheckDisposed();

            // Normalize the file path
            filePath = Path.GetFullPath(filePath);

            // Handle duplicate file paths
            if (Writers.ContainsKey(filePath))
            {
                if (!AutoRenameConflictingFiles)
                { throw new InvalidOperationException($"Tried to create file ({filePath}) more than once."); }

                _FilesWrittenMoreThanOnce.Add(filePath);

                string prefix = Path.Combine(Path.GetDirectoryName(filePath)!, Path.GetFileNameWithoutExtension(filePath) + "_");
                string suffix = Path.GetExtension(filePath);
                int i = 0;
                do
                {
                    filePath = $"{prefix}{i}{suffix}";
                    i++;
                }
                while (Writers.ContainsKey(filePath));
            }

            // Create the writer
            TWriter writer = factory(this, filePath);
            Writers.Add(filePath, writer);
            return writer;
        }

        public TWriter Open<TWriter>(string filePath)
            where TWriter : class
            => Open(filePath, GetFactory<TWriter>());

        private bool IsDisposed = false;
        private void CheckDisposed()
        {
            if (IsDisposed)
            { throw new ObjectDisposedException(nameof(OutputSession)); }
        }

        public void Dispose()
        {
            foreach (object writer in Writers.Values)
            {
                if (writer is IDisposable disposable)
                { disposable.Dispose(); }
            }
        }
    }
}
